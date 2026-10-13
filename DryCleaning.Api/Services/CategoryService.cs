using DryCleaning.Api.Common;
using DryCleaning.Api.Data;
using DryCleaning.Api.Domain;
using DryCleaning.Api.Dtos;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace DryCleaning.Api.Services;

/// <summary>Global categories – managed by admins only.</summary>
public sealed class CategoryService(AppDbContext db)
{
    public async Task<IReadOnlyList<CategoryDto>> GetAsync(bool includeInactive, CancellationToken ct) =>
        (await db.Categories
            .Where(c => includeInactive || c.IsActive)
            .OrderBy(c => c.DisplayOrder).ThenBy(c => c.Name)
            .ToListAsync(ct))
        .Select(c => c.ToDto()).ToList();

    public async Task<CategoryDto> GetByIdAsync(Guid id, CancellationToken ct) =>
        (await db.Categories.FindAsync([id], ct) ?? throw new NotFoundException("Category not found.")).ToDto();

    public async Task<CategoryDto> CreateAsync(CategoryRequest request, CancellationToken ct)
    {
        await EnsureUniqueNameAsync(request.Name, null, ct);
        var category = new Category();
        Apply(category, request);
        db.Categories.Add(category);
        await db.SaveChangesAsync(ct);
        return category.ToDto();
    }

    public async Task<CategoryDto> UpdateAsync(Guid id, CategoryRequest request, CancellationToken ct)
    {
        var category = await db.Categories.FindAsync([id], ct) ?? throw new NotFoundException("Category not found.");
        await EnsureUniqueNameAsync(request.Name, id, ct);
        Apply(category, request);
        await db.SaveChangesAsync(ct);
        return category.ToDto();
    }

    public async Task SetActiveAsync(Guid id, bool isActive, CancellationToken ct)
    {
        var category = await db.Categories.FindAsync([id], ct) ?? throw new NotFoundException("Category not found.");
        category.IsActive = isActive;
        await db.SaveChangesAsync(ct);
    }

    public async Task ReorderAsync(ReorderRequest request, CancellationToken ct)
    {
        var categories = await db.Categories.Where(c => request.CategoryIds.Contains(c.Id)).ToListAsync(ct);
        for (var i = 0; i < request.CategoryIds.Count; i++)
        {
            var category = categories.FirstOrDefault(c => c.Id == request.CategoryIds[i]);
            if (category is not null)
                category.DisplayOrder = i + 1;
        }

        await db.SaveChangesAsync(ct);
    }

    private static void Apply(Category category, CategoryRequest request)
    {
        category.Name = request.Name.Trim();
        category.Description = request.Description;
        category.ImageUrl = request.ImageUrl;
        category.DisplayOrder = request.DisplayOrder;
        category.IsActive = request.IsActive;
    }

    private async Task EnsureUniqueNameAsync(string name, Guid? exceptId, CancellationToken ct)
    {
        var trimmed = name.Trim();
        if (await db.Categories.AnyAsync(c => c.Name == trimmed && c.Id != exceptId, ct))
            throw new ConflictException($"A category named '{trimmed}' already exists.");
    }
}
