using ExpenseTracker.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using ExpenseTracker.Domain;
using ExpenseTracker.Api.Hal;

namespace ExpenseTracker.Api.Features.Reports;

public static class ReportsEndpoints
{
    public static IEndpointRouteBuilder MapReports(this IEndpointRouteBuilder app)
    {
        var reports = app.MapGroup("/api/reports")
            .WithTags("Reports")
            .RequireAuthorization();

        reports.MapGet("/spending", GetSpending)
            .WithName("GetSpending")
            .WithSummary("Get spending report grouped by category, day, or month.");

        return app;
    }

    private static async Task<IResult> GetSpending(
        ExpenseTrackerDbContext db,
        HttpRequest req,
        CancellationToken ct)
    {
        DateOnly? from = null, to = null;
        if (DateOnly.TryParse(req.Query["from"], out var f)) from = f;
        if (DateOnly.TryParse(req.Query["to"], out var t)) to = t;
        var groupBy = req.Query["groupBy"].ToString().ToLowerInvariant();

        var query = db.Transactions
            .AsNoTracking()
            .Where(x => !x.IsVoided && x.Type == TransactionType.Expense);

        if (from.HasValue) query = query.Where(x => x.OccurredOn >= from.Value);
        if (to.HasValue) query = query.Where(x => x.OccurredOn <= to.Value);

        var results = new List<SpendingGroupDocument>();

        if (groupBy == "category")
        {
            var groups = await query
                .GroupBy(x => x.CategoryId)
                .Select(g => new { CategoryId = g.Key, Total = g.Sum(x => x.Amount) })
                .ToListAsync(ct);

            foreach (var g in groups)
            {
                var idStr = g.CategoryId?.Value.ToString() ?? "uncategorized";
                var doc = new SpendingGroupDocument(idStr, g.Total);
                var drillDown = $"/api/transactions?type=Expense&categoryId={idStr}";
                if (from.HasValue) drillDown += $"&from={from.Value:yyyy-MM-dd}";
                if (to.HasValue) drillDown += $"&to={to.Value:yyyy-MM-dd}";
                
                doc.WithLink("drill-down", Link.Get(drillDown));
                results.Add(doc);
            }
        }
        else if (groupBy == "month")
        {
            var data = await query.Select(x => new { x.OccurredOn, x.Amount }).ToListAsync(ct);
            var groups = data
                .GroupBy(x => new { x.OccurredOn.Year, x.OccurredOn.Month })
                .Select(g => new { 
                    Period = $"{g.Key.Year}-{g.Key.Month:D2}",
                    Total = g.Sum(x => x.Amount),
                    Start = new DateOnly(g.Key.Year, g.Key.Month, 1),
                    End = new DateOnly(g.Key.Year, g.Key.Month, DateTime.DaysInMonth(g.Key.Year, g.Key.Month))
                })
                .OrderBy(x => x.Period);

            foreach (var g in groups)
            {
                var doc = new SpendingGroupDocument(g.Period, g.Total);
                var drillDown = $"/api/transactions?type=Expense&from={g.Start:yyyy-MM-dd}&to={g.End:yyyy-MM-dd}";
                doc.WithLink("drill-down", Link.Get(drillDown));
                results.Add(doc);
            }
        }
        else
        {
            var groups = await query
                .GroupBy(x => x.OccurredOn)
                .Select(g => new { Date = g.Key, Total = g.Sum(x => x.Amount) })
                .OrderBy(x => x.Date)
                .ToListAsync(ct);

            foreach (var g in groups)
            {
                var doc = new SpendingGroupDocument(g.Date.ToString("yyyy-MM-dd"), g.Total);
                var drillDown = $"/api/transactions?type=Expense&from={g.Date:yyyy-MM-dd}&to={g.Date:yyyy-MM-dd}";
                doc.WithLink("drill-down", Link.Get(drillDown));
                results.Add(doc);
            }
        }

        var response = new HalDocument()
            .WithState("from", from)
            .WithState("to", to)
            .WithState("groupBy", groupBy)
            .WithEmbedded("item", results);

        return Results.Extensions.Hal(response);
    }

    public class SpendingGroupDocument : HalDocument
    {
        public SpendingGroupDocument(string groupId, decimal totalAmount)
        {
            this.WithState("groupId", groupId);
            this.WithState("totalAmount", totalAmount);
        }
    }
}
