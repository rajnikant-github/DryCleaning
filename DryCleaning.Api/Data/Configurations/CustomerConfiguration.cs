using DryCleaning.Api.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DryCleaning.Api.Data.Configurations;

public sealed class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> b)
    {
        b.ToTable("Customers");
        b.HasOne(x => x.User).WithOne(u => u.Customer).HasForeignKey<Customer>(x => x.UserId);
        b.HasOne(x => x.Address).WithMany().HasForeignKey(x => x.AddressId);
        b.HasIndex(x => x.UserId).IsUnique();
    }
}
