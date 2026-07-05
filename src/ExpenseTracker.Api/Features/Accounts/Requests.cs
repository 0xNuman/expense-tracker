using ExpenseTracker.Api.Auth;
using ExpenseTracker.Api.Hal;
using ExpenseTracker.Domain;
using ExpenseTracker.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ExpenseTracker.Api.Features.Accounts;

/// <summary>Request bodies for account endpoints.</summary>
public sealed class CreateAccountRequest
{
    [System.ComponentModel.DataAnnotations.Required]
    public string Name { get; set; } = string.Empty;

    [System.ComponentModel.DataAnnotations.Required]
    public string Type { get; set; } = string.Empty;

    [System.ComponentModel.DataAnnotations.Required]
    public string Currency { get; set; } = string.Empty;

    public decimal OpeningBalance { get; set; }
}

public sealed class RenameAccountRequest
{
    [System.ComponentModel.DataAnnotations.Required]
    public string Name { get; set; } = string.Empty;
}