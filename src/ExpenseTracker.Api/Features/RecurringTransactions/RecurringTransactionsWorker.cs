using ExpenseTracker.Domain;
using ExpenseTracker.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ExpenseTracker.Api.Features.RecurringTransactions;

public class RecurringTransactionsWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<RecurringTransactionsWorker> _logger;

    public RecurringTransactionsWorker(IServiceProvider serviceProvider, ILogger<RecurringTransactionsWorker> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(1));
        
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await ProcessDueRules(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing recurring transactions");
            }
        }
    }

    private async Task ProcessDueRules(CancellationToken stoppingToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ExpenseTrackerDbContext>();
        
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        
        var dueRules = await db.Set<RecurringRule>()
            .Where(r => r.Enabled && !r.Completed && r.NextRunUtc <= today)
            .ToListAsync(stoppingToken);

        foreach (var rule in dueRules)
        {
            // Acquire advisory lock per rule (Postgres specific) - phase 1 mock:
            // await db.Database.ExecuteSqlRawAsync($"SELECT pg_advisory_xact_lock({rule.Id.Value.GetHashCode()})", stoppingToken);
            
            try 
            {
                var txnType = rule.RuleKind == RecurringRuleKind.Income ? TransactionType.Income : TransactionType.Expense;
                var txn = Transaction.Create(
                    rule.TenantId,
                    rule.AccountId,
                    txnType,
                    rule.AmountAccountCurrency.Amount,
                    rule.AmountAccountCurrency.Currency,
                    today,
                    new UserId(Guid.Empty), // System user
                    rule.CategoryId,
                    rule.MemoPattern
                );
                
                db.Add(txn);
                
                var log = RecurringExecutionLog.Create(rule.Id, rule.NextRunUtc, txn.Id, RecurringExecutionStatus.Posted);
                db.Add(log);
                
                rule.RecordRun(txn.Id, DateTimeOffset.UtcNow);
                
                await db.SaveChangesAsync(stoppingToken);
                _logger.LogInformation($"Posted transaction {txn.Id} for rule {rule.Id}");
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, $"Failed to post for rule {rule.Id}");
                var log = RecurringExecutionLog.Create(rule.Id, rule.NextRunUtc, null, RecurringExecutionStatus.Error, ex.Message);
                db.Add(log);
                rule.Pause(); // Pause on error
                await db.SaveChangesAsync(stoppingToken);
            }
        }
    }
}
