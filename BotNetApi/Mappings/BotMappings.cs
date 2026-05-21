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
            BatteryLevel = bot.BatteryLevel,
            LastUpdated = bot.LastUpdated,
            IsOnline = bot.IsOnline,
            IsServicingCustomer = bot.IsServicingCustomer
        };

    public static Bot ToEntity(this CreateBotDto dto) =>
        new Bot
        {
            Name = dto.Name,
            BatteryLevel = dto.BatteryLevel,
            LastUpdated = DateTime.UtcNow,
            IsOnline = dto.IsOnline,
            IsServicingCustomer = false
        };
}
