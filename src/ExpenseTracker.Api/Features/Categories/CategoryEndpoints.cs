using System.ComponentModel.DataAnnotations;
using ExpenseTracker.Api.Auth;
using ExpenseTracker.Api.Hal;
using ExpenseTracker.Domain;
using ExpenseTracker.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ExpenseTracker.Api.Features.Categories;

public static class CategoryEndpoints
{
    public static IEndpointRouteBuilder MapCategories(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/categories")
            .WithTags("Categories")
            .RequireAuthorization();

        group.MapGet("/", ListCategories);
        group.MapPost("/", CreateCategory);
        group.MapGet("/{id}", GetCategory);
        group.MapPatch("/{id}", UpdateCategory);
        group.MapPost("/{id}/move", MoveCategory);
        group.MapPost("/{id}/archive", ArchiveCategory);
        group.MapPost("/{id}/restore", RestoreCategory);

        return app;
    }

    private static async Task<IResult> ListCategories(
        bool? includeArchived,
        ExpenseTrackerDbContext db,
        CancellationToken ct)
    {
        var query = db.Categories.AsNoTracking();
        if (includeArchived != true)
        {
            query = query.Where(c => !c.IsArchived);
        }

        var categories = await query.OrderBy(c => c.SortOrder).ThenBy(c => c.Name).ToListAsync(ct);
        var embedded = categories.Select(ToCategoryDocument).ToList();

        var doc = new HalDocument()
            .WithLink("self", Link.Get("/api/categories"))
            .WithEmbedded("categories", embedded)
            .WithState("count", categories.Count);

        return Results.Extensions.Hal(doc);
    }

    private static async Task<IResult> CreateCategory(
        CreateCategoryRequest request,
        ExpenseTrackerDbContext db,
        ICurrentUserService currentUser,
        CancellationToken ct)
    {
        if (!currentUser.ActiveTenantId.HasValue)
            return Results.Problem("No active tenant.", statusCode: StatusCodes.Status400BadRequest);

        if (!Enum.TryParse<CategoryKind>(request.Kind, true, out var kind))
            return Results.Problem("Invalid category kind.", statusCode: StatusCodes.Status400BadRequest);

        CategoryId? parentId = request.ParentId.HasValue ? new CategoryId(request.ParentId.Value) : null;

        if (parentId.HasValue)
        {
            var parent = await db.Categories.FirstOrDefaultAsync(c => c.Id == parentId, ct);
            if (parent == null) return Results.Problem("Parent category not found.", statusCode: StatusCodes.Status400BadRequest);
            if (parent.Kind != CategoryKind.Either && parent.Kind != kind)
                return Results.Problem("Child category kind must match parent.", statusCode: StatusCodes.Status400BadRequest);
        }

        var category = Category.Create(
            currentUser.ActiveTenantId.Value,
            request.Name,
            kind,
            parentId,
            request.Icon,
            request.Color,
            request.SortOrder ?? 0,
            request.Notes
        );

        db.Categories.Add(category);
        await db.SaveChangesAsync(ct);

        return Results.Extensions.Hal(ToCategoryDocument(category), StatusCodes.Status201Created);
    }

    private static async Task<IResult> GetCategory(
        Guid id,
        ExpenseTrackerDbContext db,
        CancellationToken ct)
    {
        var categoryId = new CategoryId(id);
        var category = await db.Categories.AsNoTracking().FirstOrDefaultAsync(c => c.Id == categoryId, ct);
        if (category == null) return Results.Problem("Category not found.", statusCode: StatusCodes.Status404NotFound);

        return Results.Extensions.Hal(ToCategoryDocument(category));
    }

    private static async Task<IResult> UpdateCategory(
        Guid id,
        UpdateCategoryRequest request,
        ExpenseTrackerDbContext db,
        CancellationToken ct)
    {
        var categoryId = new CategoryId(id);
        var category = await db.Categories.FirstOrDefaultAsync(c => c.Id == categoryId, ct);
        if (category == null) return Results.Problem("Category not found.", statusCode: StatusCodes.Status404NotFound);

        if (request.Name != null) category.Rename(request.Name);
        if (request.Kind != null && Enum.TryParse<CategoryKind>(request.Kind, true, out var kind))
        {
            category.ChangeKind(kind);
        }
        
        category.UpdateDetails(
            request.Icon ?? category.Icon,
            request.Color ?? category.Color,
            request.SortOrder ?? category.SortOrder,
            request.Notes ?? category.Notes
        );

        await db.SaveChangesAsync(ct);
        return Results.Extensions.Hal(ToCategoryDocument(category));
    }

    private static async Task<IResult> MoveCategory(
        Guid id,
        MoveCategoryRequest request,
        ExpenseTrackerDbContext db,
        CancellationToken ct)
    {
        var categoryId = new CategoryId(id);
        var category = await db.Categories.FirstOrDefaultAsync(c => c.Id == categoryId, ct);
        if (category == null) return Results.Problem("Category not found.", statusCode: StatusCodes.Status404NotFound);

        CategoryId? newParentId = request.NewParentId.HasValue ? new CategoryId(request.NewParentId.Value) : null;
        
        if (newParentId.HasValue)
        {
            var parent = await db.Categories.FirstOrDefaultAsync(c => c.Id == newParentId, ct);
            if (parent == null) return Results.Problem("Parent category not found.", statusCode: StatusCodes.Status404NotFound);
        }

        category.MoveTo(newParentId, request.NewSortOrder ?? category.SortOrder);
        await db.SaveChangesAsync(ct);
        return Results.Extensions.Hal(ToCategoryDocument(category));
    }

    private static async Task<IResult> ArchiveCategory(
        Guid id,
        ExpenseTrackerDbContext db,
        CancellationToken ct)
    {
        var categoryId = new CategoryId(id);
        var category = await db.Categories.FirstOrDefaultAsync(c => c.Id == categoryId, ct);
        if (category == null) return Results.Problem("Category not found.", statusCode: StatusCodes.Status404NotFound);

        category.Archive();
        await db.SaveChangesAsync(ct);
        return Results.Extensions.Hal(ToCategoryDocument(category));
    }

    private static async Task<IResult> RestoreCategory(
        Guid id,
        ExpenseTrackerDbContext db,
        CancellationToken ct)
    {
        var categoryId = new CategoryId(id);
        var category = await db.Categories.FirstOrDefaultAsync(c => c.Id == categoryId, ct);
        if (category == null) return Results.Problem("Category not found.", statusCode: StatusCodes.Status404NotFound);

        category.Restore();
        await db.SaveChangesAsync(ct);
        return Results.Extensions.Hal(ToCategoryDocument(category));
    }

    private static HalDocument ToCategoryDocument(Category c)
    {
        var doc = new HalDocument()
            .WithLink("self", Link.Get($"/api/categories/{c.Id}"))
            .WithLink("et:update-category", new Link { Href = $"/api/categories/{c.Id}", Method = "PATCH" })
            .WithLink("et:archive-category", new Link { Href = $"/api/categories/{c.Id}/archive", Method = "POST" })
            .WithState("id", c.Id.ToString())
            .WithState("parentId", c.ParentId?.ToString())
            .WithState("name", c.Name)
            .WithState("kind", c.Kind.ToString())
            .WithState("icon", c.Icon)
            .WithState("color", c.Color)
            .WithState("sortOrder", c.SortOrder)
            .WithState("isArchived", c.IsArchived)
            .WithState("notes", c.Notes);

        if (c.ParentId.HasValue)
            doc.WithLink("et:parent", Link.Get($"/api/categories/{c.ParentId}"));
            
        return doc;
    }
}

public class CreateCategoryRequest
{
    public Guid? ParentId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Kind { get; set; } = string.Empty;
    public string? Icon { get; set; }
    public string? Color { get; set; }
    public int? SortOrder { get; set; }
    public string? Notes { get; set; }
}

public class UpdateCategoryRequest
{
    public string? Name { get; set; }
    public string? Kind { get; set; }
    public string? Icon { get; set; }
    public string? Color { get; set; }
    public int? SortOrder { get; set; }
    public string? Notes { get; set; }
}

public class MoveCategoryRequest
{
    public Guid? NewParentId { get; set; }
    public int? NewSortOrder { get; set; }
}
