using DryCleaning.Api.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DryCleaning.Api.Data.Configurations;

public sealed class DriverJobConfiguration : IEntityTypeConfiguration<DriverJob>
{
    public void Configure(EntityTypeBuilder<DriverJob> b)
    {
        b.ToTable("DriverJobs");
        // Concurrency token so two drivers can't accept the same marketplace job.
        b.Property<byte[]>("RowVersion").IsRowVersion();
        b.HasOne(x => x.Order).WithMany(o => o.DriverJobs).HasForeignKey(x => x.OrderId);
        b.HasOne(x => x.Driver).WithMany(d => d.Jobs).HasForeignKey(x => x.DriverId);
        b.HasOne(x => x.PickupAddress).WithMany().HasForeignKey(x => x.PickupAddressId);
        b.HasOne(x => x.DeliveryAddress).WithMany().HasForeignKey(x => x.DeliveryAddressId);
        b.HasIndex(x => new { x.Status, x.JobType });
        b.HasIndex(x => x.DriverId);
        b.Property(x => x.Earning).HasPrecision(18, 2);
    }
}
