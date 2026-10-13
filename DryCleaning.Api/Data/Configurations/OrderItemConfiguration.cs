using DryCleaning.Api.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DryCleaning.Api.Data.Configurations;

public sealed class OrderItemConfiguration : IEntityTypeConfiguration<OrderItem>
{
    public void Configure(EntityTypeBuilder<OrderItem> b)
    {
        b.ToTable("OrderItems");
        b.Property(x => x.UnitPrice).HasPrecision(10, 2);
        b.Property(x => x.TotalPrice).HasPrecision(10, 2);
        b.Property(x => x.Notes).HasMaxLength(500);
        b.HasOne(x => x.Order).WithMany(o => o.Items).HasForeignKey(x => x.OrderId);
        b.HasOne(x => x.ServiceItem).WithMany().HasForeignKey(x => x.ServiceItemId);
        b.HasOne(x => x.Product).WithMany().HasForeignKey(x => x.ProductId);
    }
}
