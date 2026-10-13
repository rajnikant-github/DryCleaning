using DryCleaning.Api.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DryCleaning.Api.Data.Configurations;

public sealed class ProviderConfiguration : IEntityTypeConfiguration<Provider>
{
    public void Configure(EntityTypeBuilder<Provider> b)
    {
        b.ToTable("ServiceProviders");
        b.Property(x => x.BusinessName).HasMaxLength(100).IsRequired();
        b.Property(x => x.Description).HasMaxLength(500);
        b.Property(x => x.Phone).HasMaxLength(20);
        b.Property(x => x.Email).HasMaxLength(100);
        b.Property(x => x.LogoUrl).HasMaxLength(255);
        b.HasOne(x => x.User).WithOne(u => u.ServiceProvider).HasForeignKey<Provider>(x => x.UserId);
        b.HasOne(x => x.Address).WithMany().HasForeignKey(x => x.AddressId);
        b.HasIndex(x => x.UserId).IsUnique();
    }
}
