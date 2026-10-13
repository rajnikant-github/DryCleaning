using DryCleaning.Api.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DryCleaning.Api.Data.Configurations;

public sealed class OrderStatusHistoryConfiguration : IEntityTypeConfiguration<OrderStatusHistory>
{
    public void Configure(EntityTypeBuilder<OrderStatusHistory> b)
    {
        b.ToTable("OrderStatusHistory");
        b.Property(x => x.Notes).HasMaxLength(500);
        b.HasOne(x => x.Order).WithMany(o => o.StatusHistory).HasForeignKey(x => x.OrderId);
        b.HasOne<User>().WithMany().HasForeignKey(x => x.ChangedByUserId);
    }
}
