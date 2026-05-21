using BotNetApi.DTOs;

namespace BotNetApi.Services;

public interface IBotService
{
    Task<IEnumerable<BotResponseDto>> GetAllAsync();
    Task<BotResponseDto?> GetByIdAsync(int id);
    Task<BotResponseDto> CreateAsync(CreateBotDto dto);
    Task<BotResponseDto?> UpdateAsync(int id, UpdateBotDto dto);
    Task<bool> DeleteAsync(int id);

    // Bot actions
    Task<BotResponseDto?> RechargeAsync(int id);
    Task<BotResponseDto?> UpdateServicingStatusAsync(int id, UpdateServicingStatusDto dto);
}
