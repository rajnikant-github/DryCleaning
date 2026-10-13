using DryCleaning.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace DryCleaning.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public const string OrderNumberSequence = "OrderNumbers";

    public DbSet<User> Users => Set<User>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Provider> ServiceProviders => Set<Provider>();
    public DbSet<Driver> Drivers => Set<Driver>();
    public DbSet<Address> Addresses => Set<Address>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<ServiceProviderCategory> ServiceProviderCategories => Set<ServiceProviderCategory>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<ServiceItem> ServiceItems => Set<ServiceItem>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<OrderStatusHistory> OrderStatusHistory => Set<OrderStatusHistory>();
    public DbSet<OrderVerification> OrderVerifications => Set<OrderVerification>();
    public DbSet<OrderPhoto> OrderPhotos => Set<OrderPhoto>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<DriverJob> DriverJobs => Set<DriverJob>();
    public DbSet<Dispute> Disputes => Set<Dispute>();
    public DbSet<Notification> Notifications => Set<Notification>();

    protected override void ConfigureConventions(ModelConfigurationBuilder builder)
    {
        // Enums are stored as readable strings (e.g. "ReadyForDelivery").
        builder.Properties<Enum>().HaveConversion<string>().HaveMaxLength(30);
        builder.Properties<DateTime>().HaveColumnType("datetime2");
        builder.Properties<DateTime?>().HaveColumnType("datetime2");
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasSequence<int>(OrderNumberSequence).StartsAt(10001).IncrementsBy(1);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        // SQL Server rejects multiple cascade paths – never cascade deletes; soft-disable via IsActive instead.
        foreach (var foreignKey in modelBuilder.Model.GetEntityTypes().SelectMany(e => e.GetForeignKeys()))
            foreignKey.DeleteBehavior = DeleteBehavior.Restrict;
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        foreach (var entry in ChangeTracker.Entries<EntityBase>())
        {
            if (entry.State == EntityState.Modified)
                entry.Entity.UpdatedAt = DateTime.UtcNow;
        }

        return base.SaveChangesAsync(cancellationToken);
    }

    /// <summary>Next order number from the SQL sequence, e.g. EDC-10001.</summary>
    public async Task<string> NextOrderNumberAsync(CancellationToken ct)
    {
        var next = await Database
            .SqlQueryRaw<int>("SELECT NEXT VALUE FOR dbo." + OrderNumberSequence + " AS [Value]")
            .SingleAsync(ct);
        return $"EDC-{next}";
    }
}
