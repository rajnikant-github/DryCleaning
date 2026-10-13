using DryCleaning.Api.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DryCleaning.Api.Data.Configurations;

public sealed class OrderPhotoConfiguration : IEntityTypeConfiguration<OrderPhoto>
{
    public void Configure(EntityTypeBuilder<OrderPhoto> b)
    {
        b.ToTable("OrderPhotos");
        b.Property(x => x.Url).HasMaxLength(500).IsRequired();
        b.HasOne(x => x.Order).WithMany(o => o.Photos).HasForeignKey(x => x.OrderId);
    }
}
