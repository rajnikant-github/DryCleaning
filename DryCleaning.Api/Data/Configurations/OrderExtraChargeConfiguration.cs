using DryCleaning.Api.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DryCleaning.Api.Data.Configurations;

public sealed class OrderExtraChargeConfiguration : IEntityTypeConfiguration<OrderExtraCharge>
{
    public void Configure(EntityTypeBuilder<OrderExtraCharge> b)
    {
        b.ToTable("OrderExtraCharges");
        b.Property(x => x.Name).HasMaxLength(100).IsRequired();
        b.Property(x => x.Amount).HasPrecision(10, 2);
        b.HasOne(x => x.Order).WithMany(o => o.ExtraCharges).HasForeignKey(x => x.OrderId);
        b.HasIndex(x => x.OrderId);
    }
}
