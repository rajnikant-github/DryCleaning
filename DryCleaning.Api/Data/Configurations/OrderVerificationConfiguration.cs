using DryCleaning.Api.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DryCleaning.Api.Data.Configurations;

public sealed class OrderVerificationConfiguration : IEntityTypeConfiguration<OrderVerification>
{
    public void Configure(EntityTypeBuilder<OrderVerification> b)
    {
        b.ToTable("OrderVerifications");
        b.Property(x => x.OtpCode).HasMaxLength(10);
        b.HasOne(x => x.Order).WithMany(o => o.Verifications).HasForeignKey(x => x.OrderId);
        b.HasOne<User>().WithMany().HasForeignKey(x => x.VerifiedByUserId);
    }
}
