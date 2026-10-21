using DryCleaning.Api.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DryCleaning.Api.Data.Configurations;

public sealed class VatSettingConfiguration : IEntityTypeConfiguration<VatSetting>
{
    public void Configure(EntityTypeBuilder<VatSetting> b)
    {
        b.ToTable("VatSettings");
        b.Property(x => x.Rate).HasPrecision(5, 2);
    }
}
