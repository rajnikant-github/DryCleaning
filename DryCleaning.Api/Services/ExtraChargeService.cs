using DryCleaning.Api.Common;
using DryCleaning.Api.Data;
using DryCleaning.Api.Domain;
using DryCleaning.Api.Dtos;
using Microsoft.EntityFrameworkCore;

namespace DryCleaning.Api.Services;

/// <summary>Admin-managed extra charges added to every new order.</summary>
public sealed class ExtraChargeService(AppDbContext db)
{
    private const int MaxCharges = 50;

    public async Task<IReadOnlyList<ExtraChargeDto>> ListAsync(CancellationToken ct) =>
        (await db.ExtraCharges.AsNoTracking().OrderBy(c => c.DisplayOrder).ThenBy(c => c.CreatedAt).ToListAsync(ct)).Select(ToDto).ToList();

    /// <summary>The charges that are switched on, in display order (what new orders get).</summary>
    public Task<List<ExtraCharge>> ActiveAsync(CancellationToken ct) =>
        db.ExtraCharges.AsNoTracking().Where(c => c.IsActive).OrderBy(c => c.DisplayOrder).ThenBy(c => c.CreatedAt).ToListAsync(ct);

    public async Task<ExtraChargeDto> CreateAsync(ExtraChargeRequest request, CancellationToken ct)
    {
        if (await db.ExtraCharges.CountAsync(ct) >= MaxCharges)
            throw new ConflictException($"You can have at most {MaxCharges} extra charges. Delete one you no longer need.");

        var charge = new ExtraCharge();
        Apply(charge, request);
        db.ExtraCharges.Add(charge);
        await db.SaveChangesAsync(ct);
        return ToDto(charge);
    }

    public async Task<ExtraChargeDto> UpdateAsync(Guid id, ExtraChargeRequest request, CancellationToken ct)
    {
        var charge = await FindAsync(id, ct);
        Apply(charge, request);
        charge.Touch();
        await db.SaveChangesAsync(ct);
        return ToDto(charge);
    }

    public async Task SetActiveAsync(Guid id, bool isActive, CancellationToken ct)
    {
        var charge = await FindAsync(id, ct);
        charge.IsActive = isActive;
        charge.Touch();
        await db.SaveChangesAsync(ct);
    }

    /// <summary>Removes the charge. Orders already placed keep their own copy of it.</summary>
    public async Task DeleteAsync(Guid id, CancellationToken ct)
    {
        var charge = await FindAsync(id, ct);
        db.ExtraCharges.Remove(charge);
        await db.SaveChangesAsync(ct);
    }

    private async Task<ExtraCharge> FindAsync(Guid id, CancellationToken ct) =>
        await db.ExtraCharges.FirstOrDefaultAsync(c => c.Id == id, ct) ?? throw new NotFoundException("Extra charge not found.");

    private static void Apply(ExtraCharge charge, ExtraChargeRequest request)
    {
        charge.Name = request.Name.Trim();
        charge.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();
        charge.Amount = Math.Round(request.Amount, 2, MidpointRounding.AwayFromZero);
        charge.IsActive = request.IsActive;
        charge.DisplayOrder = request.DisplayOrder;
    }

    private static ExtraChargeDto ToDto(ExtraCharge c) => new()
    {
        Id = c.Id, Name = c.Name, Description = c.Description, Amount = c.Amount, IsActive = c.IsActive, DisplayOrder = c.DisplayOrder
    };
}
