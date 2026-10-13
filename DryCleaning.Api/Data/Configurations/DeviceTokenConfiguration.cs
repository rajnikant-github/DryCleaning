using DryCleaning.Api.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DryCleaning.Api.Data.Configurations;

public sealed class DeviceTokenConfiguration : IEntityTypeConfiguration<DeviceToken>
{
    public void Configure(EntityTypeBuilder<DeviceToken> b)
    {
        b.ToTable("DeviceTokens");
        b.Property(x => x.Token).HasMaxLength(512).IsRequired();
        b.HasIndex(x => x.Token).IsUnique();
        b.HasIndex(x => x.UserId);
        b.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId);
    }
}
