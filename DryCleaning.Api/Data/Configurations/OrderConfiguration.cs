using DryCleaning.Api.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DryCleaning.Api.Data.Configurations;

public sealed class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> b)
    {
        b.ToTable("Orders");
        b.Property(x => x.OrderNumber).HasMaxLength(50).IsRequired();
        b.HasIndex(x => x.OrderNumber).IsUnique();
        b.Property(x => x.Status);
        b.Property(x => x.SubTotal).HasPrecision(10, 2);
        b.Property(x => x.Tax).HasPrecision(10, 2);
        b.Property(x => x.DeliveryFee).HasPrecision(10, 2);
        b.Property(x => x.TotalAmount).HasPrecision(10, 2);
        b.Property(x => x.PickupDate).HasColumnType("date");
        b.Property(x => x.PickupSlot).HasMaxLength(50);
        b.HasOne(x => x.Customer).WithMany(c => c.Orders).HasForeignKey(x => x.CustomerId);
        b.HasOne(x => x.ServiceProvider).WithMany(p => p.Orders).HasForeignKey(x => x.ServiceProviderId);
        b.HasOne(x => x.PickupAddress).WithMany().HasForeignKey(x => x.PickupAddressId);
        b.HasIndex(x => new { x.ServiceProviderId, x.Status });
        b.HasIndex(x => new { x.CustomerId, x.Status });
    }
}
