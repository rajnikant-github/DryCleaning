using DryCleaning.Api.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DryCleaning.Api.Data.Configurations;

public sealed class RatingConfiguration : IEntityTypeConfiguration<Rating>
{
    public void Configure(EntityTypeBuilder<Rating> b)
    {
        b.ToTable("Ratings");
        b.Property(x => x.Comment).HasMaxLength(500);
        b.HasIndex(x => x.OrderId).IsUnique();
        b.HasIndex(x => x.ServiceProviderId);
        b.HasOne(x => x.Order).WithOne(o => o.Rating).HasForeignKey<Rating>(x => x.OrderId);
        b.HasOne(x => x.Customer).WithMany().HasForeignKey(x => x.CustomerId);
        b.HasOne(x => x.ServiceProvider).WithMany().HasForeignKey(x => x.ServiceProviderId);
    }
}
