using ExpenseTracker.Api.Auth;
using ExpenseTracker.Api.Hal;
using ExpenseTracker.Domain;
using ExpenseTracker.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ExpenseTracker.Api.Features.Transfers;

public record CreateTransferRequest(Guid SourceAccountId, Guid DestinationAccountId, decimal Amount, string Currency, decimal? DestinationAmount, string? Memo);
public record VoidTransferRequest(string? Reason);

public static class TransferEndpoints
{
    public static IEndpointRouteBuilder MapTransfers(this IEndpointRouteBuilder app)
    {
        var tenantTransfers = app.MapGroup("/api/tenants/{tenantId}/transfers")
            .WithTags("Transfers")
            .RequireAuthorization();

        tenantTransfers.MapPost("/", CreateTransfer).WithName("CreateTransfer");
        tenantTransfers.MapGet("/", ListTransfersForTenant).WithName("ListTransfersForTenant");

        var accountTransfers = app.MapGroup("/api/accounts/{accountId}/transfers")
            .WithTags("Transfers")
            .RequireAuthorization();

        accountTransfers.MapGet("/", ListTransfersForAccount).WithName("ListTransfersForAccount");

        var transfers = app.MapGroup("/api/transfers")
            .WithTags("Transfers")
            .RequireAuthorization();

        transfers.MapGet("/{id}", GetTransfer).WithName("GetTransfer");
        transfers.MapPost("/{id}/void", VoidTransfer).WithName("VoidTransfer");

        return app;
    }

    private static async Task<IResult> CreateTransfer(Guid tenantId, CreateTransferRequest req, ExpenseTrackerDbContext db, ICurrentUserService currentUser, CancellationToken ct)
    {
        if (currentUser.ActiveTenantId?.Value != tenantId) return Results.Forbid();

        var sourceAccount = await db.Accounts.FindAsync(new object[] { new AccountId(req.SourceAccountId) }, ct);
        var destAccount = await db.Accounts.FindAsync(new object[] { new AccountId(req.DestinationAccountId) }, ct);

        if (sourceAccount == null || destAccount == null) return Results.NotFound("Account(s) not found.");

        var destAmt = req.DestinationAmount ?? req.Amount;
        var srcCurrency = CurrencyCode.From(req.Currency);
        var destCurrency = destAccount.Currency;

        FXRate? fx = null;
        if (srcCurrency != destCurrency)
        {
            var rate = destAmt / req.Amount;
            fx = FXRate.Of(srcCurrency, destCurrency, rate, DateTimeOffset.UtcNow, "Manual");
        }

        var transfer = Transfer.Create(
            new TenantId(tenantId),
            sourceAccount.Id,
            destAccount.Id,
            req.Amount,
            srcCurrency,
            destAmt,
            destCurrency,
            fx,
            DateOnly.FromDateTime(DateTime.UtcNow),
            req.Memo);

        db.Transfers.Add(transfer);
        await db.SaveChangesAsync(ct);

        return Results.Created($"/api/transfers/{transfer.Id.Value}", ToDocument(transfer));
    }

    private static async Task<IResult> ListTransfersForTenant(Guid tenantId, ExpenseTrackerDbContext db, ICurrentUserService currentUser, CancellationToken ct)
    {
        if (currentUser.ActiveTenantId?.Value != tenantId) return Results.Forbid();
        var items = await db.Transfers.AsNoTracking().Where(t => t.TenantId == new TenantId(tenantId)).OrderByDescending(t => t.OccurredOnUtc).ToListAsync(ct);
        return Results.Ok(new HalDocument().WithEmbedded("item", items.Select(t => ToDocument(t)).ToList()));
    }

    private static async Task<IResult> ListTransfersForAccount(Guid accountId, ExpenseTrackerDbContext db, ICurrentUserService currentUser, CancellationToken ct)
    {
        var aId = new AccountId(accountId);
        var items = await db.Transfers.AsNoTracking()
            .Where(t => t.SourceAccountId == aId || t.DestinationAccountId == aId)
            .OrderByDescending(t => t.OccurredOnUtc)
            .ToListAsync(ct);
        return Results.Ok(new HalDocument().WithEmbedded("item", items.Select(t => ToDocument(t)).ToList()));
    }

    private static async Task<IResult> GetTransfer(Guid id, ExpenseTrackerDbContext db, ICurrentUserService currentUser, CancellationToken ct)
    {
        var t = await db.Transfers.FindAsync(new object[] { new TransferId(id) }, ct);
        if (t == null) return Results.NotFound();
        return Results.Ok(ToDocument(t));
    }

    private static async Task<IResult> VoidTransfer(Guid id, VoidTransferRequest req, ExpenseTrackerDbContext db, ICurrentUserService currentUser, CancellationToken ct)
    {
        var t = await db.Transfers.FindAsync(new object[] { new TransferId(id) }, ct);
        if (t == null) return Results.NotFound();
        if (t.IsVoided) return Results.Conflict("Already voided.");
        
        t.Void(currentUser.UserId!.Value, DateTimeOffset.UtcNow);
        await db.SaveChangesAsync(ct);
        return Results.Ok(ToDocument(t));
    }

    private static HalDocument ToDocument(Transfer t)
    {
        return new HalDocument()
            .WithLink("self", Link.Get($"/api/transfers/{t.Id.Value}"))
            .WithLink("source", Link.Get($"/api/accounts/{t.SourceAccountId.Value}"))
            .WithLink("destination", Link.Get($"/api/accounts/{t.DestinationAccountId.Value}"))
            .WithLink("void", Link.Post($"/api/transfers/{t.Id.Value}/void"))
            .WithState("id", t.Id.Value)
            .WithState("sourceAccountId", t.SourceAccountId.Value)
            .WithState("destinationAccountId", t.DestinationAccountId.Value)
            .WithState("sourceAmount", t.SourceAmount)
            .WithState("sourceCurrency", t.SourceCurrency.Value)
            .WithState("destinationAmount", t.DestinationAmount)
            .WithState("destinationCurrency", t.DestinationCurrency.Value)
            .WithState("occurredOnUtc", t.OccurredOnUtc.ToString("yyyy-MM-dd"))
            .WithState("memo", t.Memo)
            .WithState("isVoided", t.IsVoided);
    }
}
