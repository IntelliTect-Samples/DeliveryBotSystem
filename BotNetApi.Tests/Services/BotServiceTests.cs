using BotNetApi.Data;
using BotNetApi.DTOs;
using BotNetApi.Models;
using BotNetApi.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace BotNetApi.Tests.Services;

public class BotServiceTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly BotService _service;

    public BotServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _db = new AppDbContext(options);
        _service = new BotService(_db);
    }

    public void Dispose() => _db.Dispose();

    private async Task<Bot> SeedBot(string name = "Bot A", int battery = 80, bool online = true)
    {
        var bot = new Bot
        {
            Name = name,
            BatteryLevel = battery,
            IsOnline = online,
            IsServicingCustomer = false,
            LastUpdated = DateTime.UtcNow
        };
        _db.Bots.Add(bot);
        await _db.SaveChangesAsync();
        return bot;
    }

    // --- GetAllAsync ---

    [Fact]
    public async Task GetAllAsync_ReturnsAllBots()
    {
        await SeedBot("Bot A");
        await SeedBot("Bot B");

        var result = await _service.GetAllAsync();

        Assert.Equal(2, result.Count());
    }

    [Fact]
    public async Task GetAllAsync_EmptyDb_ReturnsEmpty()
    {
        var result = await _service.GetAllAsync();

        Assert.Empty(result);
    }

    // --- GetByIdAsync ---

    [Fact]
    public async Task GetByIdAsync_ExistingId_ReturnsBot()
    {
        var bot = await SeedBot();

        var result = await _service.GetByIdAsync(bot.Id);

        Assert.NotNull(result);
        Assert.Equal("Bot A", result.Name);
        Assert.Equal(80, result.BatteryLevel);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistingId_ReturnsNull()
    {
        var result = await _service.GetByIdAsync(999);

        Assert.Null(result);
    }

    // --- CreateAsync ---

    [Fact]
    public async Task CreateAsync_ValidDto_ReturnsDtoWithId()
    {
        var dto = new CreateBotDto { Name = "New Bot", BatteryLevel = 90, IsOnline = true };

        var result = await _service.CreateAsync(dto);

        Assert.True(result.Id > 0);
        Assert.Equal("New Bot", result.Name);
        Assert.Equal(90, result.BatteryLevel);
        Assert.True(result.IsOnline);
        Assert.False(result.IsServicingCustomer);
    }

    [Fact]
    public async Task CreateAsync_PersistsBotInDatabase()
    {
        var dto = new CreateBotDto { Name = "New Bot", BatteryLevel = 50 };

        var result = await _service.CreateAsync(dto);

        var inDb = await _db.Bots.FindAsync(result.Id);
        Assert.NotNull(inDb);
        Assert.Equal("New Bot", inDb.Name);
    }

    // --- UpdateAsync ---

    [Fact]
    public async Task UpdateAsync_ExistingId_ReturnsUpdatedDto()
    {
        var bot = await SeedBot("Old Name", 50);
        var dto = new UpdateBotDto { Name = "New Name", BatteryLevel = 75, IsOnline = false, IsServicingCustomer = true };

        var result = await _service.UpdateAsync(bot.Id, dto);

        Assert.NotNull(result);
        Assert.Equal("New Name", result.Name);
        Assert.Equal(75, result.BatteryLevel);
        Assert.False(result.IsOnline);
        Assert.True(result.IsServicingCustomer);
    }

    [Fact]
    public async Task UpdateAsync_NonExistingId_ReturnsNull()
    {
        var dto = new UpdateBotDto { Name = "X", BatteryLevel = 50 };

        var result = await _service.UpdateAsync(999, dto);

        Assert.Null(result);
    }

    // --- DeleteAsync ---

    [Fact]
    public async Task DeleteAsync_ExistingId_ReturnsTrue()
    {
        var bot = await SeedBot();

        var result = await _service.DeleteAsync(bot.Id);

        Assert.True(result);
        Assert.Null(await _db.Bots.FindAsync(bot.Id));
    }

    [Fact]
    public async Task DeleteAsync_NonExistingId_ReturnsFalse()
    {
        var result = await _service.DeleteAsync(999);

        Assert.False(result);
    }

    // --- RechargeAsync ---

    [Fact]
    public async Task RechargeAsync_ExistingId_SetsBatteryTo100()
    {
        var bot = await SeedBot(battery: 20);

        var result = await _service.RechargeAsync(bot.Id);

        Assert.NotNull(result);
        Assert.Equal(100, result.BatteryLevel);
    }

    [Fact]
    public async Task RechargeAsync_NonExistingId_ReturnsNull()
    {
        var result = await _service.RechargeAsync(999);

        Assert.Null(result);
    }

    // --- UpdateServicingStatusAsync ---

    [Fact]
    public async Task UpdateServicingStatusAsync_ExistingId_UpdatesStatus()
    {
        var bot = await SeedBot();
        var dto = new UpdateServicingStatusDto { IsServicingCustomer = true };

        var result = await _service.UpdateServicingStatusAsync(bot.Id, dto);

        Assert.NotNull(result);
        Assert.True(result.IsServicingCustomer);
    }

    [Fact]
    public async Task UpdateServicingStatusAsync_NonExistingId_ReturnsNull()
    {
        var dto = new UpdateServicingStatusDto { IsServicingCustomer = true };

        var result = await _service.UpdateServicingStatusAsync(999, dto);

        Assert.Null(result);
    }
}
