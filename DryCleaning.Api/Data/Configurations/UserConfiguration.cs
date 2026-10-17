using DryCleaning.Api.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DryCleaning.Api.Data.Configurations;

// Column types and lengths follow the V1 schema diagram.

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> b)
    {
        b.ToTable("Users");
        b.Property(x => x.Name).HasMaxLength(100).IsRequired();
        b.Property(x => x.Email).HasMaxLength(100);
        b.Property(x => x.Phone).HasMaxLength(20);
        b.Property(x => x.PasswordHash).HasMaxLength(255).IsRequired();
        b.Property(x => x.PhotoUrl).HasMaxLength(500);
        b.HasIndex(x => x.Email).IsUnique().HasFilter("[Email] IS NOT NULL");
        b.HasIndex(x => x.Phone).IsUnique().HasFilter("[Phone] IS NOT NULL");
    }
}
