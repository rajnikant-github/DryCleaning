using DryCleaning.Api.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DryCleaning.Api.Data.Configurations;

public sealed class ServiceItemConfiguration : IEntityTypeConfiguration<ServiceItem>
{
    public void Configure(EntityTypeBuilder<ServiceItem> b)
    {
        b.ToTable("ServiceItems");
        b.Property(x => x.Name).HasMaxLength(100).IsRequired();
        b.Property(x => x.Description).HasMaxLength(500);
        b.Property(x => x.ImageUrl).HasMaxLength(255);
        b.Property(x => x.Price).HasPrecision(10, 2);
        b.HasOne(x => x.Product).WithMany(p => p.ServiceItems).HasForeignKey(x => x.ProductId);
    }
}
