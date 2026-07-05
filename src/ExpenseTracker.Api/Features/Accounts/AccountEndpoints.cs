using ExpenseTracker.Api.Auth;
using ExpenseTracker.Api.Hal;
using ExpenseTracker.Domain;
using ExpenseTracker.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ExpenseTracker.Api.Features.Accounts;

/// <summary>Registers all account endpoints under /api/accounts.</summary>
public static class AccountEndpoints
{
    public static IEndpointRouteBuilder MapAccounts(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/accounts")
            .WithTags("Accounts")
            .RequireAuthorization();

        group.MapGet("/", ListAccounts)
             .WithName("ListAccounts")
             .WithSummary("List accounts for the active tenant.");

        group.MapPost("/", CreateAccount)
             .WithName("CreateAccount")
             .WithSummary("Create a new account in the active tenant.");

        group.MapGet("/{id}", GetAccount)
             .WithName("GetAccount")
             .WithSummary("Get a single account by id.");

        group.MapPatch("/{id}", UpdateAccount)
             .WithName("UpdateAccount")
             .WithSummary("Update an existing account.");

        group.MapPost("/{id}/archive", ArchiveAccount)
             .WithName("ArchiveAccount")
             .WithSummary("Archive an existing account.");

        return app;
    }

    // ── GET /api/accounts ──────────────────────────────────────────
    private static async Task<IResult> ListAccounts(
        ExpenseTrackerDbContext db,
        CancellationToken ct)
    {
        var accounts = await db.Accounts
            .AsNoTracking()
            .OrderBy(a => a.Name)
            .ToListAsync(ct);

        var balances = await ComputeBalancesAsync(db, accounts.Select(a => a.Id).ToList(), ct);
        var embedded = accounts.Select(a => ToAccountDocument(a, balances.GetValueOrDefault(a.Id, a.OpeningBalance.Amount))).ToList();

        var doc = new HalDocument()
            .WithLink("self", Link.Get("/api/accounts"))
            .WithLink("et:create-account", Link.Post("/api/accounts"))
            .WithEmbedded("item", embedded)
            .WithState("count", accounts.Count);

        return Results.Extensions.Hal(doc);
    }

    // ── POST /api/accounts ────────────────────────────────────────
    private static async Task<IResult> CreateAccount(
        CreateAccountRequest request,
        ExpenseTrackerDbContext db,
        ICurrentUserService currentUser,
        CancellationToken ct)
    {
        if (!currentUser.ActiveTenantId.HasValue)
            return Results.Problem("No active tenant.", statusCode: StatusCodes.Status400BadRequest);

        AccountType type;
        try { type = Enum.Parse<AccountType>(request.Type, ignoreCase: true); }
        catch (ArgumentException) { return Results.Problem("Invalid account type.", statusCode: StatusCodes.Status400BadRequest); }

        CurrencyCode currency;
        try { currency = CurrencyCode.From(request.Currency); }
        catch (ArgumentException) { return Results.Problem("Invalid currency code.", statusCode: StatusCodes.Status400BadRequest); }

        Account account;
        try
        {
            account = Account.Create(
                currentUser.ActiveTenantId.Value,
                request.Name,
                type,
                currency,
                request.OpeningBalance);
        }
        catch (ArgumentException ex)
        {
            return Results.Problem(ex.Message, statusCode: StatusCodes.Status400BadRequest);
        }

        db.Accounts.Add(account);
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex)
        {
            return Results.Problem($"Could not save account: {ex.InnerException?.Message ?? ex.Message}", statusCode: StatusCodes.Status409Conflict);
        }

        var doc = ToAccountDocument(account, account.OpeningBalance.Amount);
        return Results.Extensions.Hal(doc, StatusCodes.Status201Created);
    }

    // ── GET /api/accounts/{id} ────────────────────────────────────
    private static async Task<IResult> GetAccount(
        Guid id,
        ExpenseTrackerDbContext db,
        CancellationToken ct)
    {
        var accountId = new AccountId(id);
        var account = await db.Accounts.AsNoTracking().FirstOrDefaultAsync(a => a.Id == accountId, ct);
        if (account is null)
            return Results.Problem("Account not found.", statusCode: StatusCodes.Status404NotFound);

        var balances = await ComputeBalancesAsync(db, new[] { accountId }, ct);
        var balance = balances.GetValueOrDefault(accountId, account.OpeningBalance.Amount);

        var doc = ToAccountDocument(account, balance);
        return Results.Extensions.Hal(doc);
    }

    // ── PATCH /api/accounts/{id} ──────────────────────────────────
    private static async Task<IResult> UpdateAccount(
        Guid id,
        UpdateAccountRequest request,
        ExpenseTrackerDbContext db,
        CancellationToken ct)
    {
        var accountId = new AccountId(id);
        var account = await db.Accounts.FirstOrDefaultAsync(a => a.Id == accountId, ct);
        if (account is null)
            return Results.Problem("Account not found.", statusCode: StatusCodes.Status404NotFound);

        AccountType type;
        try { type = Enum.Parse<AccountType>(request.Type, ignoreCase: true); }
        catch (ArgumentException) { return Results.Problem("Invalid account type.", statusCode: StatusCodes.Status400BadRequest); }

        try
        {
            CurrencyCode? newCurrency = string.IsNullOrWhiteSpace(request.Currency) ? null : CurrencyCode.From(request.Currency);
            bool hasTransactions = false;
            
            if (newCurrency.HasValue && newCurrency.Value != account.Currency)
            {
                hasTransactions = await db.Transactions.AnyAsync(t => t.AccountId == accountId, ct) 
                                  || await db.Transfers.AnyAsync(t => t.SourceAccountId == accountId || t.DestinationAccountId == accountId, ct);
            }

            account.Update(request.Name, type, newCurrency, hasTransactions);
        }
        catch (ArgumentException ex)
        {
            return Results.Problem(ex.Message, statusCode: StatusCodes.Status400BadRequest);
        }

        await db.SaveChangesAsync(ct);

        var balances = await ComputeBalancesAsync(db, new[] { accountId }, ct);
        var balance = balances.GetValueOrDefault(accountId, account.OpeningBalance.Amount);

        var doc = ToAccountDocument(account, balance);
        return Results.Extensions.Hal(doc);
    }

    // ── POST /api/accounts/{id}/archive ──────────────────────────
    private static async Task<IResult> ArchiveAccount(
        Guid id,
        ExpenseTrackerDbContext db,
        CancellationToken ct)
    {
        var accountId = new AccountId(id);
        var account = await db.Accounts.FirstOrDefaultAsync(a => a.Id == accountId, ct);
        if (account is null)
            return Results.Problem("Account not found.", statusCode: StatusCodes.Status404NotFound);

        try
        {
            account.Archive();
        }
        catch (InvalidOperationException ex)
        {
            return Results.Problem(ex.Message, statusCode: StatusCodes.Status409Conflict);
        }

        await db.SaveChangesAsync(ct);

        var balances = await ComputeBalancesAsync(db, new[] { accountId }, ct);
        var balance = balances.GetValueOrDefault(accountId, account.OpeningBalance.Amount);

        var doc = ToAccountDocument(account, balance);
        return Results.Extensions.Hal(doc);
    }

    // ── Helpers ───────────────────────────────────────────────────
    private static async Task<Dictionary<AccountId, decimal>> ComputeBalancesAsync(
        ExpenseTrackerDbContext db,
        IReadOnlyCollection<AccountId> accountIds,
        CancellationToken ct)
    {
        if (accountIds.Count == 0)
            return new Dictionary<AccountId, decimal>();

        // Keep as List<AccountId> so EF Core's value converter handles the Contains translation.
        var idList = accountIds.ToList();

        var movements = await db.Transactions
            .AsNoTracking()
            .Where(t => idList.Contains(t.AccountId) && !t.IsVoided)
            .GroupBy(t => new { t.AccountId, t.Type })
            .Select(g => new { g.Key.AccountId, g.Key.Type, Sum = g.Sum(t => t.Amount) })
            .ToListAsync(ct);

        var result = new Dictionary<AccountId, decimal>();
        foreach (var accountId in accountIds)
            result[accountId] = 0m;

        foreach (var m in movements)
        {
            if (!result.ContainsKey(m.AccountId)) result[m.AccountId] = 0m;
            if (m.Type == TransactionType.Income)
                result[m.AccountId] += m.Sum;
            else
                result[m.AccountId] -= m.Sum;
        }

        var transfersOut = await db.Transfers
            .AsNoTracking()
            .Where(t => idList.Contains(t.SourceAccountId) && !t.IsVoided)
            .GroupBy(t => t.SourceAccountId)
            .Select(g => new { AccountId = g.Key, Sum = g.Sum(t => t.SourceAmount) })
            .ToListAsync(ct);

        var transfersIn = await db.Transfers
            .AsNoTracking()
            .Where(t => idList.Contains(t.DestinationAccountId) && !t.IsVoided)
            .GroupBy(t => t.DestinationAccountId)
            .Select(g => new { AccountId = g.Key, Sum = g.Sum(t => t.DestinationAmount) })
            .ToListAsync(ct);

        foreach (var tOut in transfersOut)
        {
            if (!result.ContainsKey(tOut.AccountId)) result[tOut.AccountId] = 0m;
            result[tOut.AccountId] -= tOut.Sum;
        }

        foreach (var tIn in transfersIn)
        {
            if (!result.ContainsKey(tIn.AccountId)) result[tIn.AccountId] = 0m;
            result[tIn.AccountId] += tIn.Sum;
        }

        return result;
    }

    private static HalDocument ToAccountDocument(Account account, decimal balance)
    {
        return new HalDocument()
            .WithLink("self", Link.Get($"/api/accounts/{account.Id}"))
            .WithLink("et:transactions", Link.Get($"/api/accounts/{account.Id}/transactions"))
            .WithLink("et:update-account", new Link
            {
                Href = $"/api/accounts/{account.Id}",
                Method = "PATCH",
                Title = "Update this account"
            })
            .WithState("id", account.Id.ToString())
            .WithState("name", account.Name)
            .WithState("type", account.Type.ToString())
            .WithState("currency", account.Currency.Value)
            .WithState("openingBalance", account.OpeningBalance.Amount)
            .WithState("balance", balance)
            .WithState("isArchived", account.IsArchived)
            .WithState("openedAtUtc", account.OpenedAtUtc)
            .WithState("closedAtUtc", account.ClosedAtUtc);
    }
}