using BotNetApi.Data;
using BotNetApi.DTOs;
using BotNetApi.Mappings;
using Microsoft.EntityFrameworkCore;

namespace BotNetApi.Services;

public class BotService : IBotService
{
    // Bots below this battery level are excluded from nearest-bot searches
    private const int MinBatteryThreshold = 15;

    private readonly AppDbContext _db;

    public BotService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IEnumerable<BotResponseDto>> GetAllAsync()
    {
        var bots = await _db.Bots.ToListAsync();
        return bots.Select(b => b.ToResponseDto());
    }

    public async Task<BotResponseDto?> GetByIdAsync(int id)
    {
        var bot = await _db.Bots.FindAsync(id);
        return bot?.ToResponseDto();
    }

    public async Task<BotResponseDto> CreateAsync(CreateBotDto dto)
    {
        var bot = dto.ToEntity();
        _db.Bots.Add(bot);
        await _db.SaveChangesAsync();
        return bot.ToResponseDto();
    }

    public async Task<BotResponseDto?> UpdateAsync(int id, UpdateBotDto dto)
    {
        var bot = await _db.Bots.FindAsync(id);
        if (bot is null) return null;

        bot.Name = dto.Name;
        bot.StockLevel = dto.StockLevel;
        bot.BatteryLevel = dto.BatteryLevel;
        bot.Latitude = dto.Latitude;
        bot.Longitude = dto.Longitude;
        bot.IsOnline = dto.IsOnline;
        bot.IsServicingCustomer = dto.IsServicingCustomer;
        bot.LastUpdated = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return bot.ToResponseDto();
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var bot = await _db.Bots.FindAsync(id);
        if (bot is null) return false;

        _db.Bots.Remove(bot);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<BotResponseDto?> RechargeAsync(int id)
    {
        var bot = await _db.Bots.FindAsync(id);
        if (bot is null) return null;

        bot.BatteryLevel = 100;
        bot.LastUpdated = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return bot.ToResponseDto();
    }

    public async Task<BotResponseDto?> UpdateStockAsync(int id, UpdateStockDto dto)
    {
        var bot = await _db.Bots.FindAsync(id);
        if (bot is null) return null;

        bot.StockLevel = dto.StockLevel;
        bot.LastUpdated = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return bot.ToResponseDto();
    }

    public async Task<BotResponseDto?> UpdateLocationAsync(int id, UpdateLocationDto dto)
    {
        var bot = await _db.Bots.FindAsync(id);
        if (bot is null) return null;

        bot.Latitude = dto.Latitude;
        bot.Longitude = dto.Longitude;
        bot.LastUpdated = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return bot.ToResponseDto();
    }

    public async Task<BotResponseDto?> UpdateServicingStatusAsync(int id, UpdateServicingStatusDto dto)
    {
        var bot = await _db.Bots.FindAsync(id);
        if (bot is null) return null;

        bot.IsServicingCustomer = dto.IsServicingCustomer;
        bot.LastUpdated = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return bot.ToResponseDto();
    }

    public async Task<BotResponseDto?> FindNearestAvailableAsync(double latitude, double longitude)
    {
        var bots = await _db.Bots.ToListAsync();

        // Filter: must be online, not busy, and have enough battery
        var nearest = bots
            .Where(b => b.IsOnline && !b.IsServicingCustomer && b.BatteryLevel >= MinBatteryThreshold)
            .OrderBy(b => CalculateDistanceKm(latitude, longitude, b.Latitude, b.Longitude))
            .FirstOrDefault();

        return nearest?.ToResponseDto();
    }

    // Haversine formula — calculates straight-line distance between two coordinates in kilometers
    private static double CalculateDistanceKm(double lat1, double lon1, double lat2, double lon2)
    {
        const double EarthRadiusKm = 6371.0;

        var dLat = ToRadians(lat2 - lat1);
        var dLon = ToRadians(lon2 - lon1);

        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2)
              + Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2))
              * Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

        return EarthRadiusKm * c;
    }

    private static double ToRadians(double degrees) => degrees * Math.PI / 180.0;
}
