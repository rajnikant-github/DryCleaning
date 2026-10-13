using DryCleaning.Api.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DryCleaning.Api.Data.Configurations;

public sealed class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> b)
    {
        b.ToTable("Notifications");
        b.Property(x => x.Title).HasMaxLength(100).IsRequired();
        b.Property(x => x.Message).HasMaxLength(500).IsRequired();
        b.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId);
        b.HasOne(x => x.Order).WithMany().HasForeignKey(x => x.OrderId);
        b.HasIndex(x => new { x.UserId, x.IsRead });
    }
}
