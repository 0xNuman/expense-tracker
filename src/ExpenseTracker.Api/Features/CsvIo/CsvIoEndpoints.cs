using System.Globalization;
using ExpenseTracker.Domain;
using ExpenseTracker.Infrastructure.Persistence;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Routing;
using System.IO;
using ExpenseTracker.Api.Auth;

namespace ExpenseTracker.Api.Features.CsvIo;

public static class CsvIoEndpoints
{
    public static void MapCsvIoEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api");

        group.MapPost("/accounts/{accountId:guid}/import/preview", PreviewImport)
            .RequireAuthorization();

        group.MapPost("/accounts/{accountId:guid}/import", ImportTransactions)
            .RequireAuthorization();

        group.MapGet("/tenants/{tenantId:guid}/export", ExportTransactions)
            .RequireAuthorization();

        group.MapPost("/import-batches/{batchId:guid}/void", VoidImportBatch)
            .RequireAuthorization();
    }

    public record PreviewRequest(string CsvBase64);
    public record PreviewResponse(Guid DryRunId, List<string> Headers, List<PreviewRow> Rows);
    public record PreviewRow(int Index, List<string> Values);

    private static async Task<IResult> PreviewImport(
        Guid accountId, 
        [FromBody] PreviewRequest request, 
        ExpenseTrackerDbContext db,
        HttpContext http)
    {
        var bytes = Convert.FromBase64String(request.CsvBase64);
        using var stream = new MemoryStream(bytes);
        using var reader = new StreamReader(stream);
        
        var headerLine = await reader.ReadLineAsync();
        if (headerLine == null) return Results.BadRequest("Empty CSV");
        
        var headers = headerLine.Split(',').Select(h => h.Trim()).ToList();
        var rows = new List<PreviewRow>();
        
        int index = 0;
        while (index < 10 && await reader.ReadLineAsync() is string line)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            rows.Add(new PreviewRow(index++, line.Split(',').Select(v => v.Trim()).ToList()));
        }
        
        var dryRunId = Guid.NewGuid();
        return Results.Ok(new PreviewResponse(dryRunId, headers, rows));
    }

    public record ColumnMapping(
        int? OccurredOnCol,
        int? AmountCol,
        int? TypeCol,
        int? MemoCol,
        int? CategoryCol,
        int? CounterAccountCol,
        int? TagsCol
    );

    public record ImportRequest(string CsvBase64, ColumnMapping Mapping);
    public record ImportResponse(int TotalImported, int VoidedDuplicates, int Errors);

    private static async Task<IResult> ImportTransactions(
        Guid accountId, 
        [FromBody] ImportRequest request, 
        ExpenseTrackerDbContext db,
        ICurrentUserService currentUser)
    {
        var tenantId = currentUser.ActiveTenantId;
        var userId = currentUser.UserId;
        if (!tenantId.HasValue || !userId.HasValue) return Results.Forbid();
        
        var account = await db.Set<Account>().FirstOrDefaultAsync(a => a.Id == new AccountId(accountId) && a.TenantId == tenantId.Value);
        if (account == null) return Results.NotFound("Account not found");

        var bytes = Convert.FromBase64String(request.CsvBase64);
        using var stream = new MemoryStream(bytes);
        using var reader = new StreamReader(stream);
        
        var headerLine = await reader.ReadLineAsync();
        
        int imported = 0;
        int duplicates = 0;
        int errors = 0;
        var batchId = ImportBatchId.New();

        var existingTxns = await db.Set<Transaction>()
            .Where(t => t.AccountId == new AccountId(accountId) && t.TenantId == tenantId && !t.IsVoided)
            .ToListAsync();

        while (await reader.ReadLineAsync() is string line)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            var parts = line.Split(',');
            
            try 
            {
                var occurredOnStr = request.Mapping.OccurredOnCol.HasValue && request.Mapping.OccurredOnCol.Value < parts.Length 
                    ? parts[request.Mapping.OccurredOnCol.Value] : null;
                var amountStr = request.Mapping.AmountCol.HasValue && request.Mapping.AmountCol.Value < parts.Length 
                    ? parts[request.Mapping.AmountCol.Value] : null;
                var typeStr = request.Mapping.TypeCol.HasValue && request.Mapping.TypeCol.Value < parts.Length 
                    ? parts[request.Mapping.TypeCol.Value] : null;
                var memo = request.Mapping.MemoCol.HasValue && request.Mapping.MemoCol.Value < parts.Length 
                    ? parts[request.Mapping.MemoCol.Value] : null;
                
                if (!DateOnly.TryParse(occurredOnStr, out var occurredOn) || !decimal.TryParse(amountStr, out var amount))
                {
                    errors++;
                    continue;
                }

                var type = TransactionType.Expense;
                if (!string.IsNullOrEmpty(typeStr))
                {
                    if (typeStr.Contains("Income", StringComparison.OrdinalIgnoreCase)) type = TransactionType.Income;
                }
                else
                {
                    if (amount > 0) type = TransactionType.Income;
                    else type = TransactionType.Expense;
                }
                
                amount = Math.Abs(amount);

                bool isDuplicate = existingTxns.Any(t => 
                    t.OccurredOn == occurredOn && 
                    t.Amount == amount && 
                    t.Memo == memo);

                var tags = new List<string>();
                if (isDuplicate) 
                {
                    tags.Add("csv-duplicate");
                    duplicates++;
                }

                var txn = Transaction.Create(
                    tenantId.Value, 
                    new AccountId(accountId), 
                    type, 
                    amount, 
                    account.Currency, 
                    occurredOn, 
                    userId.Value, 
                    null,
                    memo,
                    batchId, 
                    tags
                );
                
                db.Set<Transaction>().Add(txn);
                imported++;
            }
            catch { errors++; }
        }

        await db.SaveChangesAsync();
        return Results.Ok(new ImportResponse(imported, duplicates, errors));
    }

    private static async Task<IResult> ExportTransactions(
        Guid tenantId, 
        [FromQuery] string? type, 
        [FromQuery] string? format,
        ExpenseTrackerDbContext db,
        ICurrentUserService currentUser)
    {
        if (currentUser.ActiveTenantId != new TenantId(tenantId)) return Results.Forbid();
        
        var txns = await db.Set<Transaction>()
            .Where(t => t.TenantId == new TenantId(tenantId) && !t.IsVoided)
            .ToListAsync();
            
        var csv = new System.Text.StringBuilder();
        csv.AppendLine("Id,AccountId,Type,Amount,Currency,OccurredOn,Memo,Tags");
        foreach(var t in txns)
        {
            var tagsJoined = string.Join(";", t.Tags);
            var safeMemo = t.Memo?.Replace(",", " ");
            csv.AppendLine($"{t.Id.Value},{t.AccountId.Value},{t.Type},{t.Amount},{t.Currency.Value},{t.OccurredOn},{safeMemo},{tagsJoined}");
        }
        
        return Results.File(System.Text.Encoding.UTF8.GetBytes(csv.ToString()), "text/csv", "export.csv");
    }

    private static async Task<IResult> VoidImportBatch(
        Guid batchId,
        ExpenseTrackerDbContext db,
        ITenantContext tenantContext)
    {
        var tenantId = tenantContext.ActiveTenantId;
        var txns = await db.Set<Transaction>()
            .Where(t => t.TenantId == tenantId && t.ImportBatchId == new ImportBatchId(batchId) && !t.IsVoided)
            .ToListAsync();
            
        foreach (var t in txns)
        {
            t.Void(DateTimeOffset.UtcNow);
        }
        await db.SaveChangesAsync();
        
        return Results.Ok(new { VoidedCount = txns.Count });
    }
}
