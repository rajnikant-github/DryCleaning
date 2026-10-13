using DryCleaning.Api.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DryCleaning.Api.Data.Configurations;

public sealed class AddressConfiguration : IEntityTypeConfiguration<Address>
{
    public void Configure(EntityTypeBuilder<Address> b)
    {
        b.ToTable("Addresses");
        b.Property(x => x.GooglePlaceId).HasMaxLength(255);
        b.Property(x => x.FormattedAddress).HasMaxLength(1000).IsRequired();
        b.Property(x => x.AddressLine1).HasMaxLength(255);
        b.Property(x => x.AddressLine2).HasMaxLength(255);
        b.Property(x => x.StreetNumber).HasMaxLength(50);
        b.Property(x => x.Route).HasMaxLength(255);
        b.Property(x => x.SubLocality).HasMaxLength(255);
        b.Property(x => x.Locality).HasMaxLength(255);
        b.Property(x => x.AdministrativeAreaLevel1).HasMaxLength(255);
        b.Property(x => x.AdministrativeAreaLevel2).HasMaxLength(255);
        b.Property(x => x.Country).HasMaxLength(255);
        b.Property(x => x.CountryCode).HasMaxLength(10);
        b.Property(x => x.PostalCode).HasMaxLength(20);
        b.Property(x => x.Latitude).HasPrecision(10, 7);
        b.Property(x => x.Longitude).HasPrecision(10, 7);
        b.Property(x => x.PlusCode).HasMaxLength(255);
    }
}
