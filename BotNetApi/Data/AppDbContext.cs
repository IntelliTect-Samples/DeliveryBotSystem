using BotNetApi.Models;
using Microsoft.EntityFrameworkCore;

namespace BotNetApi.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Bot> Bots => Set<Bot>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Bot>(entity =>
        {
            entity.HasKey(b => b.Id);
            entity.Property(b => b.Name).IsRequired().HasMaxLength(100);

            // Store enum as a readable string rather than an integer
            entity.Property(b => b.StockLevel).HasConversion<string>();

            // Seed data — real coordinates around downtown Spokane, WA
            entity.HasData(
                new Bot
                {
                    Id = 1,
                    Name = "BOT-ALPHA",
                    StockLevel = StockLevel.High,
                    BatteryLevel = 92,
                    Latitude = 47.6588,
                    Longitude = -117.4260,
                    LastUpdated = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    IsOnline = true,
                    IsServicingCustomer = false
                },
                new Bot
                {
                    Id = 2,
                    Name = "BOT-BRAVO",
                    StockLevel = StockLevel.Medium,
                    BatteryLevel = 61,
                    Latitude = 47.6721,
                    Longitude = -117.3982,
                    LastUpdated = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    IsOnline = true,
                    IsServicingCustomer = true  // busy — should be skipped in nearest-bot search
                },
                new Bot
                {
                    Id = 3,
                    Name = "BOT-CHARLIE",
                    StockLevel = StockLevel.Low,
                    BatteryLevel = 8,           // critically low — should be skipped
                    Latitude = 47.6543,
                    Longitude = -117.4390,
                    LastUpdated = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    IsOnline = false,
                    IsServicingCustomer = false
                },
                new Bot
                {
                    Id = 4,
                    Name = "BOT-DELTA",
                    StockLevel = StockLevel.High,
                    BatteryLevel = 77,
                    Latitude = 47.6489,
                    Longitude = -117.4143,
                    LastUpdated = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    IsOnline = true,
                    IsServicingCustomer = false
                }
            );
        });
    }
}
