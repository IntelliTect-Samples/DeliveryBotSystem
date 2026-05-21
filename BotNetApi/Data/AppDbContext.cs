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

            // Seed data — real bots around downtown Spokane, WA
            entity.HasData(
                new Bot
                {
                    Id = 1,
                    Name = "BOT-ALPHA",
                    BatteryLevel = 92,
                    LastUpdated = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    IsOnline = true,
                    IsServicingCustomer = false
                },
                new Bot
                {
                    Id = 2,
                    Name = "BOT-BRAVO",
                    BatteryLevel = 61,
                    LastUpdated = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    IsOnline = true,
                    IsServicingCustomer = true
                },
                new Bot
                {
                    Id = 3,
                    Name = "BOT-CHARLIE",
                    BatteryLevel = 8,
                    LastUpdated = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    IsOnline = false,
                    IsServicingCustomer = false
                },
                new Bot
                {
                    Id = 4,
                    Name = "BOT-DELTA",
                    BatteryLevel = 77,
                    LastUpdated = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    IsOnline = true,
                    IsServicingCustomer = false
                }
            );
        });
    }
}
