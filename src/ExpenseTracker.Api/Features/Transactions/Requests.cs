using System.ComponentModel.DataAnnotations;

namespace ExpenseTracker.Api.Features.Transactions;

/// <summary>Request bodies for transaction endpoints.</summary>
public sealed class CreateTransactionRequest
{
    [Required]
    public string Type { get; set; } = string.Empty;

    [Range(0.0001, double.MaxValue)]
    public decimal Amount { get; set; }

    [Required]
    public string Currency { get; set; } = string.Empty;

    [Required]
    public string OccurredOn { get; set; } = string.Empty;

    public string? CategoryId { get; set; }

    [MaxLength(500)]
    public string? Memo { get; set; }
}

public sealed class UpdateTransactionRequest
{
    [Required]
    public string Type { get; set; } = string.Empty;

    [Range(0.0001, double.MaxValue)]
    public decimal Amount { get; set; }

    [Required]
    public string OccurredOn { get; set; } = string.Empty;

    public string? CategoryId { get; set; }

    [MaxLength(500)]
    public string? Memo { get; set; }
}

public sealed class VoidTransactionRequest
{
    [MaxLength(500)]
    public string? Reason { get; set; }
}