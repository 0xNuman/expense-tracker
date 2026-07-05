using ExpenseTracker.Api.Auth;
using ExpenseTracker.Domain;
using ExpenseTracker.Infrastructure;
using ExpenseTracker.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ExpenseTracker.Api.Features.RecurringTransactions;

public static class RecurringTransactionsEndpoints
{
    public static void MapRecurringTransactions(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/tenants/{tenantId:guid}/recurring-rules")
            .RequireAuthorization();

        group.MapGet("/", GetRules);
        group.MapPost("/", CreateRule);
        
        var ruleGroup = app.MapGroup("/api/recurring-rules/{ruleId:guid}")
            .RequireAuthorization(); // Add policy for specific rule access if needed

        ruleGroup.MapGet("/", GetRule);
        ruleGroup.MapPost("/pause", PauseRule);
        ruleGroup.MapPost("/resume", ResumeRule);
        ruleGroup.MapPost("/post-now", PostNow);
        ruleGroup.MapGet("/forecast", Forecast);
    }

    private static async Task<Ok<List<RecurringRuleDto>>> GetRules(
        Guid tenantId,
        [FromQuery] bool? enabledOnly,
        ExpenseTrackerDbContext db,
        CancellationToken ct)
    {
        var query = db.Set<RecurringRule>().Where(r => r.TenantId == new TenantId(tenantId));
        
        if (enabledOnly == true)
            query = query.Where(r => r.Enabled);

        var rules = await query.ToListAsync(ct);
        return TypedResults.Ok(rules.Select(ToDto).ToList());
    }

    private static async Task<Results<Created<RecurringRuleDto>, ValidationProblem>> CreateRule(
        Guid tenantId,
        [FromBody] CreateRecurringRuleRequest req,
        ExpenseTrackerDbContext db,
        CancellationToken ct)
    {
        if (req.DayOfMonth is < 1 or > 31)
            return TypedResults.ValidationProblem(new Dictionary<string, string[]> { { "DayOfMonth", new[] { "Must be 1-31" } } });

        var accountId = new AccountId(req.AccountId);
        var rule = RecurringRule.Create(
            new TenantId(tenantId),
            req.Name,
            Enum.Parse<RecurringRuleKind>(req.Kind, true),
            Enum.Parse<RecurringCadence>(req.Cadence, true),
            req.Interval ?? 1,
            req.StartDateUtc ?? DateOnly.FromDateTime(DateTime.UtcNow),
            accountId,
            Money.Of(req.Amount, Enum.Parse<CurrencyCode>(req.Currency, true))
        );

        if (req.DayOfMonth.HasValue) rule.SetDayOfMonth(req.DayOfMonth.Value);
        if (req.CategoryId.HasValue) rule.SetCategory(new CategoryId(req.CategoryId.Value));
        if (!string.IsNullOrEmpty(req.Memo)) rule.SetMemo(req.Memo);
        if (req.CounterpartAccountId.HasValue) rule.SetCounterpartAccount(new AccountId(req.CounterpartAccountId.Value));

        db.Add(rule);
        await db.SaveChangesAsync(ct);

        var dto = ToDto(rule);
        return TypedResults.Created($"/api/recurring-rules/{rule.Id.Value}", dto);
    }

    private static async Task<Results<Ok<RecurringRuleDto>, NotFound>> GetRule(
        Guid ruleId,
        ExpenseTrackerDbContext db,
        CancellationToken ct)
    {
        var rule = await db.Set<RecurringRule>().FindAsync(new object[] { new RecurringRuleId(ruleId) }, ct);
        if (rule == null) return TypedResults.NotFound();
        return TypedResults.Ok(ToDto(rule));
    }

    private static async Task<Results<Ok, NotFound>> PauseRule(
        Guid ruleId,
        ExpenseTrackerDbContext db,
        CancellationToken ct)
    {
        var rule = await db.Set<RecurringRule>().FindAsync(new object[] { new RecurringRuleId(ruleId) }, ct);
        if (rule == null) return TypedResults.NotFound();
        
        rule.Pause();
        await db.SaveChangesAsync(ct);
        return TypedResults.Ok();
    }

    private static async Task<Results<Ok, NotFound>> ResumeRule(
        Guid ruleId,
        ExpenseTrackerDbContext db,
        CancellationToken ct)
    {
        var rule = await db.Set<RecurringRule>().FindAsync(new object[] { new RecurringRuleId(ruleId) }, ct);
        if (rule == null) return TypedResults.NotFound();
        
        rule.Resume(DateOnly.FromDateTime(DateTime.UtcNow));
        await db.SaveChangesAsync(ct);
        return TypedResults.Ok();
    }

    private static async Task<Results<Ok, NotFound>> PostNow(
        Guid ruleId,
        [FromBody] PostNowRequest req,
        ExpenseTrackerDbContext db,
        CancellationToken ct)
    {
        var rule = await db.Set<RecurringRule>().FindAsync(new object[] { new RecurringRuleId(ruleId) }, ct);
        if (rule == null) return TypedResults.NotFound();

        // In a real implementation this would invoke the posting logic 
        // that creates the transaction and log. For phase 1 we can do a simple mock or logic sharing.
        // I will let the worker handle real posting, but for manual trigger we'd create the Transaction here.
        var account = await db.Set<Account>().FindAsync(new object[] { rule.AccountId }, ct);
        if (account != null)
        {
            var txnType = rule.RuleKind == RecurringRuleKind.Income ? TransactionType.Income : TransactionType.Expense;
            var txn = Transaction.Create(
                rule.TenantId,
                rule.AccountId,
                txnType,
                rule.AmountAccountCurrency.Amount,
                rule.AmountAccountCurrency.Currency,
                req.AsOfUtc ?? DateOnly.FromDateTime(DateTime.UtcNow),
                new UserId(Guid.Empty), // Should come from Claims
                rule.CategoryId,
                rule.MemoPattern
            );
            db.Add(txn);
            
            var log = RecurringExecutionLog.Create(rule.Id, rule.NextRunUtc, txn.Id, RecurringExecutionStatus.Posted);
            db.Add(log);
            
            rule.RecordRun(txn.Id, DateTimeOffset.UtcNow);
        }

        await db.SaveChangesAsync(ct);
        return TypedResults.Ok();
    }

    private static async Task<Results<Ok<List<ForecastDto>>, NotFound>> Forecast(
        Guid ruleId,
        [FromQuery] int? horizonDays,
        ExpenseTrackerDbContext db,
        CancellationToken ct)
    {
        var rule = await db.Set<RecurringRule>().FindAsync(new object[] { new RecurringRuleId(ruleId) }, ct);
        if (rule == null) return TypedResults.NotFound();

        int days = horizonDays ?? 90;
        var limitDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(days));
        var results = new List<ForecastDto>();
        
        var tempRule = RecurringRule.Create(
            rule.TenantId, rule.Name, rule.RuleKind, rule.Cadence, rule.Interval, rule.NextRunUtc, rule.AccountId, rule.AmountAccountCurrency);
        if (rule.EndDateUtc.HasValue) tempRule = RecurringRule.Create(
            rule.TenantId, rule.Name, rule.RuleKind, rule.Cadence, rule.Interval, rule.NextRunUtc, rule.AccountId, rule.AmountAccountCurrency); // Hacky clone

        var current = rule.NextRunUtc;
        while (current <= limitDate)
        {
            results.Add(new ForecastDto(current, rule.AmountAccountCurrency.Amount));
            // Advance logic
            current = rule.Cadence switch
            {
                RecurringCadence.Daily => current.AddDays(rule.Interval),
                RecurringCadence.Weekly => current.AddDays(7 * rule.Interval),
                RecurringCadence.Monthly => current.AddMonths(rule.Interval),
                RecurringCadence.Yearly => current.AddYears(rule.Interval),
                _ => current.AddDays(rule.Interval)
            };
            if (rule.EndDateUtc.HasValue && current > rule.EndDateUtc.Value) break;
        }

        return TypedResults.Ok(results);
    }

    private static RecurringRuleDto ToDto(RecurringRule r) => new(
        r.Id.Value, r.Name, r.Enabled, r.RuleKind.ToString(), r.Cadence.ToString(), 
        r.Interval, r.NextRunUtc, r.AmountAccountCurrency.Amount, r.AmountAccountCurrency.Currency.ToString());
}

public record CreateRecurringRuleRequest(
    string Name, string Kind, string Cadence, int? Interval, DateOnly? StartDateUtc, 
    Guid AccountId, decimal Amount, string Currency, int? DayOfMonth, Guid? CategoryId, string? Memo, Guid? CounterpartAccountId);

public record PostNowRequest(DateOnly? AsOfUtc);

public record RecurringRuleDto(Guid Id, string Name, bool Enabled, string Kind, string Cadence, int Interval, DateOnly NextRunUtc, decimal Amount, string Currency);

public record ForecastDto(DateOnly Date, decimal Amount);
