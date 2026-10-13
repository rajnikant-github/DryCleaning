using DryCleaning.Api.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DryCleaning.Api.Data.Configurations;

public sealed class DriverConfiguration : IEntityTypeConfiguration<Driver>
{
    public void Configure(EntityTypeBuilder<Driver> b)
    {
        b.ToTable("Drivers");
        b.Property(x => x.LicenseNumber).HasMaxLength(50).IsRequired();
        b.HasOne(x => x.User).WithOne(u => u.Driver).HasForeignKey<Driver>(x => x.UserId);
        b.HasOne(x => x.Address).WithMany().HasForeignKey(x => x.AddressId);
        b.HasIndex(x => x.UserId).IsUnique();
    }
}
