using DryCleaning.Api.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DryCleaning.Api.Data.Configurations;

public sealed class ExtraChargeConfiguration : IEntityTypeConfiguration<ExtraCharge>
{
    public void Configure(EntityTypeBuilder<ExtraCharge> b)
    {
        b.ToTable("ExtraCharges");
        b.Property(x => x.Name).HasMaxLength(100).IsRequired();
        b.Property(x => x.Description).HasMaxLength(200);
        b.Property(x => x.Amount).HasPrecision(10, 2);
        b.Property(x => x.Percent).HasPrecision(5, 2);
        b.Property(x => x.Type).HasDefaultValue(ExtraChargeType.Fixed);
        b.Property(x => x.PercentBase).HasDefaultValue(ChargeBase.ItemsSubtotal);
    }
}
