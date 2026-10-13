using DryCleaning.Api.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DryCleaning.Api.Data.Configurations;

public sealed class ServiceProviderCategoryConfiguration : IEntityTypeConfiguration<ServiceProviderCategory>
{
    public void Configure(EntityTypeBuilder<ServiceProviderCategory> b)
    {
        b.ToTable("ServiceProviderCategories");
        b.HasOne(x => x.ServiceProvider).WithMany(p => p.Categories).HasForeignKey(x => x.ServiceProviderId);
        b.HasOne(x => x.Category).WithMany().HasForeignKey(x => x.CategoryId);
        b.HasIndex(x => new { x.ServiceProviderId, x.CategoryId }).IsUnique();
    }
}
