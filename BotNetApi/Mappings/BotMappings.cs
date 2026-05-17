using BotNetApi.DTOs;
using BotNetApi.Models;

namespace BotNetApi.Mappings;

/// <summary>
/// Manual mapping extension methods — keeps things simple and easy to follow in class.
/// </summary>
public static class BotMappings
{
    public static BotResponseDto ToResponseDto(this Bot bot) =>
        new BotResponseDto
        {
            Id = bot.Id,
            Name = bot.Name,
            StockLevel = bot.StockLevel.ToString(),
            BatteryLevel = bot.BatteryLevel,
            Latitude = bot.Latitude,
            Longitude = bot.Longitude,
            LastUpdated = bot.LastUpdated,
            IsOnline = bot.IsOnline,
            IsServicingCustomer = bot.IsServicingCustomer
        };

    public static Bot ToEntity(this CreateBotDto dto) =>
        new Bot
        {
            Name = dto.Name,
            StockLevel = dto.StockLevel,
            BatteryLevel = dto.BatteryLevel,
            Latitude = dto.Latitude,
            Longitude = dto.Longitude,
            LastUpdated = DateTime.UtcNow,
            IsOnline = dto.IsOnline,
            IsServicingCustomer = false
        };
}
