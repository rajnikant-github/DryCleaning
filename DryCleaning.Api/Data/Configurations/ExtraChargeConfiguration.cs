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
    }
}
