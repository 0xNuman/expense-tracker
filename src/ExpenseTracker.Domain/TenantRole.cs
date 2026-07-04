namespace ExpenseTracker.Domain;

/// <summary>Role a user holds within a tenant workspace.</summary>
public enum TenantRole
{
    /// <summary>Member: read shared data; create/edit own transactions and transfers.</summary>
    Member = 0,

    /// <summary>Admin: Member abilities + manage members, categories, accounts, recurring rules, budgets.</summary>
    Admin = 1,

    /// <summary>Owner: Admin abilities + delete tenant, transfer ownership, manage owner-only settings.</summary>
    Owner = 2
}