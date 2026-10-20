using DryCleaning.Api.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DryCleaning.Api.Data.Configurations;

public sealed class ProviderWorkingHourConfiguration : IEntityTypeConfiguration<ProviderWorkingHour>
{
    public void Configure(EntityTypeBuilder<ProviderWorkingHour> b)
    {
        b.ToTable("ProviderWorkingHours");
        b.HasOne(x => x.ServiceProvider).WithMany(p => p.WorkingHours).HasForeignKey(x => x.ServiceProviderId);
        b.HasIndex(x => new { x.ServiceProviderId, x.Day }).IsUnique();
    }
}
