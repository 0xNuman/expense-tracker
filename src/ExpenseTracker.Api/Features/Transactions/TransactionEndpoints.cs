using System.ComponentModel.DataAnnotations;
using ExpenseTracker.Api.Auth;
using ExpenseTracker.Api.Hal;
using ExpenseTracker.Domain;
using ExpenseTracker.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ExpenseTracker.Api.Features.Transactions;

/// <summary>Registers all transaction endpoints under /api/transactions and /api/accounts/{accountId}/transactions.</summary>
public static class TransactionEndpoints
{
    public static IEndpointRouteBuilder MapTransactions(this IEndpointRouteBuilder app)
    {
        var accountTxns = app.MapGroup("/api/accounts/{accountId}/transactions")
            .WithTags("Transactions")
            .RequireAuthorization();

        accountTxns.MapPost("/", CreateTransaction)
                   .WithName("CreateTransaction")
                   .WithSummary("Record a new transaction against an account.");

        accountTxns.MapGet("/", ListTransactionsForAccount)
                   .WithName("ListTransactionsForAccount")
                   .WithSummary("List transactions for a single account.");

        var allTxns = app.MapGroup("/api/transactions")
            .WithTags("Transactions")
            .RequireAuthorization();

        allTxns.MapGet("/", ListAllTransactions)
                .WithName("ListTransactions")
                .WithSummary("List all transactions for the active tenant.");

        allTxns.MapPost("/{id}/void", VoidTransaction)
                .WithName("VoidTransaction")
                .WithSummary("Void a transaction (preserves audit history).");

        return app;
    }

    // ── POST /api/accounts/{accountId}/transactions ──────────────
    private static async Task<IResult> CreateTransaction(
        Guid accountId,
        CreateTransactionRequest request,
        ExpenseTrackerDbContext db,
        ICurrentUserService currentUser,
        CancellationToken ct)
    {
        if (!currentUser.UserId.HasValue)
            return Results.Problem("Not authenticated.", statusCode: StatusCodes.Status401Unauthorized);
        if (!currentUser.ActiveTenantId.HasValue)
            return Results.Problem("No active tenant.", statusCode: StatusCodes.Status400BadRequest);

        var account = await db.Accounts
            .AsTracking()
            .FirstOrDefaultAsync(a => a.Id == new AccountId(accountId), ct);
        if (account is null)
            return Results.Problem("Account not found.", statusCode: StatusCodes.Status404NotFound);

        TransactionType type;
        try { type = Enum.Parse<TransactionType>(request.Type, ignoreCase: true); }
        catch (ArgumentException) { return Results.Problem("Invalid transaction type. Use Income or Expense.", statusCode: StatusCodes.Status400BadRequest); }

        CurrencyCode currency;
        try { currency = CurrencyCode.From(request.Currency); }
        catch (ArgumentException) { return Results.Problem("Invalid currency code.", statusCode: StatusCodes.Status400BadRequest); }

        if (!string.Equals(currency.Value, account.Currency.Value, StringComparison.Ordinal))
            return Results.Problem($"Transaction currency '{currency.Value}' must match account currency '{account.Currency.Value}'.", statusCode: StatusCodes.Status400BadRequest);

        DateOnly occurredOn;
        if (!DateOnly.TryParse(request.OccurredOn, out occurredOn))
            return Results.Problem("occurredOn must be a valid date (YYYY-MM-DD).", statusCode: StatusCodes.Status400BadRequest);

        CategoryId? categoryId = null;
        if (!string.IsNullOrWhiteSpace(request.CategoryId) && Guid.TryParse(request.CategoryId, out var cid))
            categoryId = new CategoryId(cid);

        Transaction txn;
        try
        {
            txn = Transaction.Create(
                currentUser.ActiveTenantId.Value,
                account.Id,
                type,
                request.Amount,
                currency,
                occurredOn,
                currentUser.UserId.Value,
                categoryId,
                request.Memo);
        }
        catch (ArgumentException ex)
        {
            return Results.Problem(ex.Message, statusCode: StatusCodes.Status400BadRequest);
        }

        db.Transactions.Add(txn);
        await db.SaveChangesAsync(ct);

        var doc = ToTransactionDocument(txn);
        return Results.Extensions.Hal(doc, StatusCodes.Status201Created);
    }

    // ── GET /api/accounts/{accountId}/transactions ────────────────
    private static async Task<IResult> ListTransactionsForAccount(
        Guid accountId,
        ExpenseTrackerDbContext db,
        HttpRequest httpRequest,
        CancellationToken ct)
    {
        var query = ParseTransactionQuery(httpRequest);
        return await ListTransactionsAsync(db, ct, query, new AccountId(accountId));
    }

    // ── GET /api/transactions ─────────────────────────────────────
    private static async Task<IResult> ListAllTransactions(
        ExpenseTrackerDbContext db,
        HttpRequest httpRequest,
        CancellationToken ct)
    {
        AccountId? filterAccountId = null;
        if (Guid.TryParse(httpRequest.Query["accountId"], out var aId))
            filterAccountId = new AccountId(aId);

        var query = ParseTransactionQuery(httpRequest);
        return await ListTransactionsAsync(db, ct, query, filterAccountId);
    }

    // ── POST /api/transactions/{id}/void ──────────────────────────
    private static async Task<IResult> VoidTransaction(
        Guid id,
        VoidTransactionRequest? request,
        ExpenseTrackerDbContext db,
        CancellationToken ct)
    {
        var txn = await db.Transactions.FirstOrDefaultAsync(t => t.Id == new TransactionId(id), ct);
        if (txn is null)
            return Results.Problem("Transaction not found.", statusCode: StatusCodes.Status404NotFound);

        try
        {
            txn.Void(DateTimeOffset.UtcNow);
        }
        catch (InvalidOperationException ex)
        {
            return Results.Problem(ex.Message, statusCode: StatusCodes.Status409Conflict);
        }

        await db.SaveChangesAsync(ct);

        var doc = ToTransactionDocument(txn, reason: request?.Reason);
        return Results.Extensions.Hal(doc);
    }

    // ── Helpers ────────────────────────────────────────────────────
    private static TransactionQuery ParseTransactionQuery(HttpRequest req)
    {
        int page = 1, pageSize = 25;
        if (int.TryParse(req.Query["page"], out var p) && p > 0) page = p;
        if (int.TryParse(req.Query["pageSize"], out var ps) && ps > 0 && ps <= 200) pageSize = ps;

        TransactionType? type = null;
        if (Enum.TryParse<TransactionType>(req.Query["type"], ignoreCase: true, out var t))
            type = t;

        DateOnly? from = null, to = null;
        if (DateOnly.TryParse(req.Query["from"], out var f)) from = f;
        if (DateOnly.TryParse(req.Query["to"], out var to2)) to = to2;

        var sort = req.Query["sort"].ToString();
        if (string.IsNullOrEmpty(sort)) sort = "date";

        CategoryId? categoryId = null;
        var cidStr = req.Query["categoryId"].ToString();
        if (string.Equals(cidStr, "uncategorized", StringComparison.OrdinalIgnoreCase))
            categoryId = new CategoryId(Guid.Empty); // Special marker for unassigned
        else if (Guid.TryParse(cidStr, out var c))
            categoryId = new CategoryId(c);

        return new TransactionQuery(page, pageSize, type, from, to, sort, categoryId);
    }

    private static async Task<IResult> ListTransactionsAsync(
        ExpenseTrackerDbContext db,
        CancellationToken ct,
        TransactionQuery q,
        AccountId? filterAccountId)
    {
        var items = db.Transactions.AsNoTracking();

        if (filterAccountId.HasValue)
            items = items.Where(t => t.AccountId == filterAccountId.Value);
        if (q.Type.HasValue)
            items = items.Where(t => t.Type == q.Type.Value);
        if (q.From.HasValue)
            items = items.Where(t => t.OccurredOn >= q.From.Value);
        if (q.To.HasValue)
            items = items.Where(t => t.OccurredOn <= q.To.Value);
            
        if (q.CategoryId.HasValue)
        {
            if (q.CategoryId.Value.Value == Guid.Empty)
                items = items.Where(t => t.CategoryId == null);
            else
                items = items.Where(t => t.CategoryId == q.CategoryId.Value);
        }

        items = string.Equals(q.Sort, "-date", StringComparison.OrdinalIgnoreCase)
            ? items.OrderByDescending(t => t.OccurredOn).ThenByDescending(t => t.CreatedAtUtc)
            : items.OrderByDescending(t => t.OccurredOn).ThenByDescending(t => t.CreatedAtUtc);

        var total = await items.CountAsync(ct);
        var pageItems = await items
            .Skip((q.Page - 1) * q.PageSize)
            .Take(q.PageSize)
            .ToListAsync(ct);

        var embedded = pageItems.Select(t => ToTransactionDocument(t)).ToList();

        var selfBase = filterAccountId.HasValue
            ? $"/api/accounts/{filterAccountId.Value}/transactions"
            : "/api/transactions";

        var selfHref = AppendQuery(selfBase, q);

        var doc = new HalDocument()
            .WithLink("self", Link.Get(selfHref))
            .WithLink("first", Link.Get(AppendQuery(selfBase, q with { Page = 1 })))
            .WithLink("last", Link.Get(AppendQuery(selfBase, q with { Page = LastPage(total, q.PageSize) })))
            .WithEmbedded("item", embedded)
            .WithState("page", q.Page)
            .WithState("pageSize", q.PageSize)
            .WithState("total", total)
            .WithState("totalPages", LastPage(total, q.PageSize));

        if (q.Page > 1)
            doc.WithLink("prev", Link.Get(AppendQuery(selfBase, q with { Page = q.Page - 1 })));
        if (q.Page < LastPage(total, q.PageSize))
            doc.WithLink("next", Link.Get(AppendQuery(selfBase, q with { Page = q.Page + 1 })));

        return Results.Extensions.Hal(doc);
    }

    private static int LastPage(int total, int pageSize) =>
        pageSize <= 0 ? 1 : Math.Max(1, (int)Math.Ceiling(total / (double)pageSize));

    private static string AppendQuery(string baseHref, TransactionQuery q)
    {
        var sb = new System.Text.StringBuilder(baseHref);
        sb.Append("?page=").Append(q.Page);
        sb.Append("&pageSize=").Append(q.PageSize);
        if (q.Type.HasValue) sb.Append("&type=").Append(q.Type.Value.ToString());
        if (q.From.HasValue) sb.Append("&from=").Append(q.From.Value.ToString("yyyy-MM-dd"));
        if (q.To.HasValue) sb.Append("&to=").Append(q.To.Value.ToString("yyyy-MM-dd"));
        if (q.CategoryId.HasValue) sb.Append("&categoryId=").Append(q.CategoryId.Value.Value == Guid.Empty ? "uncategorized" : q.CategoryId.Value.ToString());
        if (!string.IsNullOrEmpty(q.Sort)) sb.Append("&sort=").Append(Uri.EscapeDataString(q.Sort));
        return sb.ToString();
    }

    private static HalDocument ToTransactionDocument(Transaction txn, string? reason = null)
    {
        var doc = new HalDocument()
            .WithLink("self", Link.Get($"/api/transactions/{txn.Id}"))
            .WithLink("et:account", Link.Get($"/api/accounts/{txn.AccountId}"))
            .WithLink("et:void", Link.Post($"/api/transactions/{txn.Id}/void"))
            .WithState("id", txn.Id.ToString())
            .WithState("accountId", txn.AccountId.ToString())
            .WithState("type", txn.Type.ToString())
            .WithState("amount", txn.Amount)
            .WithState("currency", txn.Currency.Value)
            .WithState("memo", txn.Memo)
            .WithState("occurredOn", txn.OccurredOn.ToString("yyyy-MM-dd"))
            .WithState("createdAtUtc", txn.CreatedAtUtc)
            .WithState("isVoided", txn.IsVoided)
            .WithState("voidedAtUtc", txn.VoidedAtUtc);

        if (reason is not null)
            doc.WithState("voidReason", reason);

        return doc;
    }

    private sealed record TransactionQuery(
        int Page,
        int PageSize,
        TransactionType? Type,
        DateOnly? From,
        DateOnly? To,
        string Sort,
        CategoryId? CategoryId);
}