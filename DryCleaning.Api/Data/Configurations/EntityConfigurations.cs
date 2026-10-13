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
        b.HasIndex(x => x.Email).IsUnique().HasFilter("[Email] IS NOT NULL");
        b.HasIndex(x => x.Phone).IsUnique().HasFilter("[Phone] IS NOT NULL");
    }
}

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

public sealed class ProviderConfiguration : IEntityTypeConfiguration<Provider>
{
    public void Configure(EntityTypeBuilder<Provider> b)
    {
        b.ToTable("ServiceProviders");
        b.Property(x => x.BusinessName).HasMaxLength(100).IsRequired();
        b.Property(x => x.Description).HasMaxLength(500);
        b.Property(x => x.Phone).HasMaxLength(20);
        b.Property(x => x.Email).HasMaxLength(100);
        b.Property(x => x.LogoUrl).HasMaxLength(255);
        b.HasOne(x => x.User).WithOne(u => u.ServiceProvider).HasForeignKey<Provider>(x => x.UserId);
        b.HasOne(x => x.Address).WithMany().HasForeignKey(x => x.AddressId);
        b.HasIndex(x => x.UserId).IsUnique();
    }
}

public sealed class DriverConfiguration : IEntityTypeConfiguration<Driver>
{
    public void Configure(EntityTypeBuilder<Driver> b)
    {
        b.ToTable("Drivers");
        b.Property(x => x.LicenseNumber).HasMaxLength(50).IsRequired();
        b.HasOne(x => x.User).WithOne(u => u.Driver).HasForeignKey<Driver>(x => x.UserId);
        b.HasOne(x => x.Address).WithMany().HasForeignKey(x => x.AddressId);
        b.HasIndex(x => x.UserId).IsUnique();
    }
}

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

public sealed class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> b)
    {
        b.ToTable("Categories");
        b.Property(x => x.Name).HasMaxLength(100).IsRequired();
        b.Property(x => x.Description).HasMaxLength(500);
        b.Property(x => x.ImageUrl).HasMaxLength(255);
        b.HasIndex(x => x.Name).IsUnique();
    }
}

public sealed class ServiceProviderCategoryConfiguration : IEntityTypeConfiguration<ServiceProviderCategory>
{
    public void Configure(EntityTypeBuilder<ServiceProviderCategory> b)
    {
        b.ToTable("ServiceProviderCategories");
        b.HasOne(x => x.ServiceProvider).WithMany(p => p.Categories).HasForeignKey(x => x.ServiceProviderId);
        b.HasOne(x => x.Category).WithMany().HasForeignKey(x => x.CategoryId);
        b.HasIndex(x => new { x.ServiceProviderId, x.CategoryId }).IsUnique();
    }
}

public sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> b)
    {
        b.ToTable("Products");
        b.Property(x => x.Name).HasMaxLength(100).IsRequired();
        b.Property(x => x.Description).HasMaxLength(500);
        b.Property(x => x.ImageUrl).HasMaxLength(255);
        b.HasOne(x => x.ServiceProvider).WithMany(p => p.Products).HasForeignKey(x => x.ServiceProviderId);
        b.HasOne(x => x.Category).WithMany(c => c.Products).HasForeignKey(x => x.CategoryId);
        b.HasIndex(x => new { x.ServiceProviderId, x.CategoryId });
    }
}

public sealed class ServiceItemConfiguration : IEntityTypeConfiguration<ServiceItem>
{
    public void Configure(EntityTypeBuilder<ServiceItem> b)
    {
        b.ToTable("ServiceItems");
        b.Property(x => x.Name).HasMaxLength(100).IsRequired();
        b.Property(x => x.Description).HasMaxLength(500);
        b.Property(x => x.ImageUrl).HasMaxLength(255);
        b.Property(x => x.Price).HasPrecision(10, 2);
        b.HasOne(x => x.Product).WithMany(p => p.ServiceItems).HasForeignKey(x => x.ProductId);
    }
}

public sealed class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> b)
    {
        b.ToTable("Orders");
        b.Property(x => x.OrderNumber).HasMaxLength(50).IsRequired();
        b.HasIndex(x => x.OrderNumber).IsUnique();
        b.Property(x => x.Status);
        b.Property(x => x.SubTotal).HasPrecision(10, 2);
        b.Property(x => x.Tax).HasPrecision(10, 2);
        b.Property(x => x.DeliveryFee).HasPrecision(10, 2);
        b.Property(x => x.TotalAmount).HasPrecision(10, 2);
        b.Property(x => x.PickupDate).HasColumnType("date");
        b.Property(x => x.PickupSlot).HasMaxLength(50);
        b.HasOne(x => x.Customer).WithMany(c => c.Orders).HasForeignKey(x => x.CustomerId);
        b.HasOne(x => x.ServiceProvider).WithMany(p => p.Orders).HasForeignKey(x => x.ServiceProviderId);
        b.HasOne(x => x.PickupAddress).WithMany().HasForeignKey(x => x.PickupAddressId);
        b.HasIndex(x => new { x.ServiceProviderId, x.Status });
        b.HasIndex(x => new { x.CustomerId, x.Status });
    }
}

public sealed class OrderItemConfiguration : IEntityTypeConfiguration<OrderItem>
{
    public void Configure(EntityTypeBuilder<OrderItem> b)
    {
        b.ToTable("OrderItems");
        b.Property(x => x.UnitPrice).HasPrecision(10, 2);
        b.Property(x => x.TotalPrice).HasPrecision(10, 2);
        b.Property(x => x.Notes).HasMaxLength(500);
        b.HasOne(x => x.Order).WithMany(o => o.Items).HasForeignKey(x => x.OrderId);
        b.HasOne(x => x.ServiceItem).WithMany().HasForeignKey(x => x.ServiceItemId);
        b.HasOne(x => x.Product).WithMany().HasForeignKey(x => x.ProductId);
    }
}

public sealed class OrderStatusHistoryConfiguration : IEntityTypeConfiguration<OrderStatusHistory>
{
    public void Configure(EntityTypeBuilder<OrderStatusHistory> b)
    {
        b.ToTable("OrderStatusHistory");
        b.Property(x => x.Notes).HasMaxLength(500);
        b.HasOne(x => x.Order).WithMany(o => o.StatusHistory).HasForeignKey(x => x.OrderId);
        b.HasOne<User>().WithMany().HasForeignKey(x => x.ChangedByUserId);
    }
}

public sealed class OrderVerificationConfiguration : IEntityTypeConfiguration<OrderVerification>
{
    public void Configure(EntityTypeBuilder<OrderVerification> b)
    {
        b.ToTable("OrderVerifications");
        b.Property(x => x.OtpCode).HasMaxLength(10);
        b.HasOne(x => x.Order).WithMany(o => o.Verifications).HasForeignKey(x => x.OrderId);
        b.HasOne<User>().WithMany().HasForeignKey(x => x.VerifiedByUserId);
    }
}

public sealed class OrderPhotoConfiguration : IEntityTypeConfiguration<OrderPhoto>
{
    public void Configure(EntityTypeBuilder<OrderPhoto> b)
    {
        b.ToTable("OrderPhotos");
        b.Property(x => x.Url).HasMaxLength(500).IsRequired();
        b.HasOne(x => x.Order).WithMany(o => o.Photos).HasForeignKey(x => x.OrderId);
    }
}

public sealed class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> b)
    {
        b.ToTable("Payments");
        b.Property(x => x.Amount).HasPrecision(10, 2);
        b.Property(x => x.TransactionId).HasMaxLength(100);
        b.HasIndex(x => x.TransactionId).IsUnique().HasFilter("[TransactionId] IS NOT NULL");
        b.HasOne(x => x.Order).WithMany(o => o.Payments).HasForeignKey(x => x.OrderId);
    }
}

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
    }
}

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
