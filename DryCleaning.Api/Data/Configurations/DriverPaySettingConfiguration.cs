using DryCleaning.Api.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DryCleaning.Api.Data.Configurations;

public sealed class DriverPaySettingConfiguration : IEntityTypeConfiguration<DriverPaySetting>
{
    public void Configure(EntityTypeBuilder<DriverPaySetting> b)
    {
        b.ToTable("DriverPaySettings");
        b.Property(x => x.PayPerJob).HasPrecision(18, 2);
    }
}
