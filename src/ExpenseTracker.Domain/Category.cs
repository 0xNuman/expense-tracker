namespace ExpenseTracker.Domain;

public sealed class Category : AggregateRoot
{
    public CategoryId Id { get; private set; }
    public TenantId TenantId { get; private set; }
    public CategoryId? ParentId { get; private set; }
    public string Name { get; private set; }
    public CategoryKind Kind { get; private set; }
    public string? Icon { get; private set; }
    public string? Color { get; private set; }
    public int SortOrder { get; private set; }
    public bool IsArchived { get; private set; }
    public string? Notes { get; private set; }

    private Category() { Name = null!; }

    public static Category Create(
        TenantId tenantId,
        string name,
        CategoryKind kind,
        CategoryId? parentId = null,
        string? icon = null,
        string? color = null,
        int sortOrder = 0,
        string? notes = null)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Name is required.", nameof(name));
        if (name.Length > 60) throw new ArgumentException("Name cannot exceed 60 characters.", nameof(name));
        
        return new Category
        {
            Id = CategoryId.New(),
            TenantId = tenantId,
            ParentId = parentId,
            Name = name,
            Kind = kind,
            Icon = icon,
            Color = color,
            SortOrder = sortOrder,
            IsArchived = false,
            Notes = notes
        };
    }

    public void Rename(string newName)
    {
        if (string.IsNullOrWhiteSpace(newName)) throw new ArgumentException("Name is required.", nameof(newName));
        if (newName.Length > 60) throw new ArgumentException("Name cannot exceed 60 characters.", nameof(newName));
        Name = newName;
    }

    public void ChangeKind(CategoryKind newKind)
    {
        Kind = newKind;
    }

    public void MoveTo(CategoryId? newParentId, int sortOrder = 0)
    {
        ParentId = newParentId;
        SortOrder = sortOrder;
    }
    
    public void UpdateDetails(string? icon, string? color, int sortOrder, string? notes)
    {
        Icon = icon;
        Color = color;
        SortOrder = sortOrder;
        Notes = notes;
    }

    public void Archive() => IsArchived = true;
    public void Restore() => IsArchived = false;
}
