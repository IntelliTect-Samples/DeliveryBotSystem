// Bridge between the Order Service and Azure SQL.
// Tells EF Core what tables exist and how they relate to each other.
using Microsoft.EntityFrameworkCore;
using OrderService.Models;

namespace OrderService.Data;

public class OrderDbContext : DbContext
{
    public OrderDbContext(DbContextOptions<OrderDbContext> options) : base(options) { }

    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<OrderStatusUpdate> OrderStatusUpdates => Set<OrderStatusUpdate>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasKey(o => o.Id);
            entity.Property(o => o.CustomerId).IsRequired().HasMaxLength(100);
            entity.Property(o => o.DeliveryAddress).HasMaxLength(500);
            entity.Property(o => o.Status).HasConversion<string>();
            entity.HasMany(o => o.Items)
                  .WithOne(i => i.Order)
                  .HasForeignKey(i => i.OrderId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<OrderItem>(entity =>
        {
            entity.HasKey(i => i.Id);
            entity.Property(i => i.ItemId).IsRequired().HasMaxLength(100);
        });

        modelBuilder.Entity<OrderStatusUpdate>(entity =>
        {
            entity.HasKey(u => u.Id);
            entity.Property(u => u.Status).HasConversion<string>();
            entity.Property(u => u.Message).HasMaxLength(1000);
            entity.HasOne(u => u.Order)
                  .WithMany()
                  .HasForeignKey(u => u.OrderId)
                  .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
