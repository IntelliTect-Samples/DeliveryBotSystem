using BotNetApi.Data;
using BotNetApi.DTOs;
using BotNetApi.Mappings;
using Microsoft.EntityFrameworkCore;

namespace BotNetApi.Services;

public class BotService : IBotService
{
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
        bot.BatteryLevel = dto.BatteryLevel;
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

    public async Task<BotResponseDto?> UpdateServicingStatusAsync(int id, UpdateServicingStatusDto dto)
    {
        var bot = await _db.Bots.FindAsync(id);
        if (bot is null) return null;

        bot.IsServicingCustomer = dto.IsServicingCustomer;
        bot.LastUpdated = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return bot.ToResponseDto();
    }
}
