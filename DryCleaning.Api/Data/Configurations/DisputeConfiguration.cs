using DryCleaning.Api.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DryCleaning.Api.Data.Configurations;

public sealed class DisputeConfiguration : IEntityTypeConfiguration<Dispute>
{
    public void Configure(EntityTypeBuilder<Dispute> b)
    {
        b.ToTable("Disputes");
        b.Property(x => x.Reason).HasMaxLength(500).IsRequired();
        b.Property(x => x.Description).HasMaxLength(1000);
        b.Property(x => x.Resolution).HasMaxLength(1000);
        b.HasOne(x => x.Order).WithMany(o => o.Disputes).HasForeignKey(x => x.OrderId);
        b.HasOne(x => x.RaisedByUser).WithMany().HasForeignKey(x => x.RaisedByUserId);
    }
}
