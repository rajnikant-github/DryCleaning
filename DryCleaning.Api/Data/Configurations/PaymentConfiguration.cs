using DryCleaning.Api.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DryCleaning.Api.Data.Configurations;

public sealed class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> b)
    {
        b.ToTable("Payments");
        b.Property(x => x.Amount).HasPrecision(10, 2);
        b.Property(x => x.TransactionId).HasMaxLength(100);
        b.HasIndex(x => x.TransactionId).IsUnique().HasFilter("[TransactionId] IS NOT NULL");
        b.HasOne(x => x.Order).WithMany(o => o.Payments).HasForeignKey(x => x.OrderId);
    }
}
