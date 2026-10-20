using DryCleaning.Api.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DryCleaning.Api.Data.Configurations;

public sealed class DeliveryPricingConfiguration : IEntityTypeConfiguration<DeliveryPricing>
{
    public void Configure(EntityTypeBuilder<DeliveryPricing> b)
    {
        b.ToTable("DeliveryPricing");
        b.Property(x => x.BaseFee).HasPrecision(10, 2);
        b.Property(x => x.PricePerKm).HasPrecision(10, 2);
        b.Property(x => x.IncludedKm).HasPrecision(8, 2);
        b.Property(x => x.MinFee).HasPrecision(10, 2);
        b.Property(x => x.MaxFee).HasPrecision(10, 2);
    }
}
