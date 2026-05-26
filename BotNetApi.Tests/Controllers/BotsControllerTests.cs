using BotNetApi.Controllers;
using BotNetApi.DTOs;
using BotNetApi.Services;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace BotNetApi.Tests.Controllers;

public class BotsControllerTests
{
    private readonly Mock<IBotService> _mockService;
    private readonly BotsController _controller;

    public BotsControllerTests()
    {
        _mockService = new Mock<IBotService>();
        _controller = new BotsController(_mockService.Object);
    }

    // --- GetAll ---

    [Fact]
    public async Task GetAll_ReturnsOkWithBots()
    {
        var bots = new List<BotResponseDto>
        {
            new() { Id = 1, Name = "Bot A", BatteryLevel = 80, IsOnline = true },
            new() { Id = 2, Name = "Bot B", BatteryLevel = 50, IsOnline = false }
        };
        _mockService.Setup(s => s.GetAllAsync()).ReturnsAsync(bots);

        var result = await _controller.GetAll();

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(bots, ok.Value);
    }

    // --- GetById ---

    [Fact]
    public async Task GetById_ExistingId_ReturnsOk()
    {
        var bot = new BotResponseDto { Id = 1, Name = "Bot A", BatteryLevel = 80 };
        _mockService.Setup(s => s.GetByIdAsync(1)).ReturnsAsync(bot);

        var result = await _controller.GetById(1);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(bot, ok.Value);
    }

    [Fact]
    public async Task GetById_NonExistingId_ReturnsNotFound()
    {
        _mockService.Setup(s => s.GetByIdAsync(99)).ReturnsAsync((BotResponseDto?)null);

        var result = await _controller.GetById(99);

        Assert.IsType<NotFoundResult>(result);
    }

    // --- Create ---

    [Fact]
    public async Task Create_ValidDto_ReturnsCreatedAtAction()
    {
        var dto = new CreateBotDto { Name = "Bot A", BatteryLevel = 100, IsOnline = true };
        var created = new BotResponseDto { Id = 1, Name = "Bot A", BatteryLevel = 100, IsOnline = true };
        _mockService.Setup(s => s.CreateAsync(dto)).ReturnsAsync(created);

        var result = await _controller.Create(dto);

        var createdResult = Assert.IsType<CreatedAtActionResult>(result);
        Assert.Equal(nameof(_controller.GetById), createdResult.ActionName);
        Assert.Equal(1, ((BotResponseDto)createdResult.Value!).Id);
    }

    // --- Update ---

    [Fact]
    public async Task Update_ExistingId_ReturnsOk()
    {
        var dto = new UpdateBotDto { Name = "Updated", BatteryLevel = 60, IsOnline = true };
        var updated = new BotResponseDto { Id = 1, Name = "Updated", BatteryLevel = 60 };
        _mockService.Setup(s => s.UpdateAsync(1, dto)).ReturnsAsync(updated);

        var result = await _controller.Update(1, dto);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(updated, ok.Value);
    }

    [Fact]
    public async Task Update_NonExistingId_ReturnsNotFound()
    {
        var dto = new UpdateBotDto { Name = "Updated", BatteryLevel = 60 };
        _mockService.Setup(s => s.UpdateAsync(99, dto)).ReturnsAsync((BotResponseDto?)null);

        var result = await _controller.Update(99, dto);

        Assert.IsType<NotFoundResult>(result);
    }

    // --- Delete ---

    [Fact]
    public async Task Delete_ExistingId_ReturnsNoContent()
    {
        _mockService.Setup(s => s.DeleteAsync(1)).ReturnsAsync(true);

        var result = await _controller.Delete(1);

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task Delete_NonExistingId_ReturnsNotFound()
    {
        _mockService.Setup(s => s.DeleteAsync(99)).ReturnsAsync(false);

        var result = await _controller.Delete(99);

        Assert.IsType<NotFoundResult>(result);
    }

    // --- Recharge ---

    [Fact]
    public async Task Recharge_ExistingId_ReturnsOkWithFullBattery()
    {
        var recharged = new BotResponseDto { Id = 1, Name = "Bot A", BatteryLevel = 100 };
        _mockService.Setup(s => s.RechargeAsync(1)).ReturnsAsync(recharged);

        var result = await _controller.Recharge(1);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(100, ((BotResponseDto)ok.Value!).BatteryLevel);
    }

    [Fact]
    public async Task Recharge_NonExistingId_ReturnsNotFound()
    {
        _mockService.Setup(s => s.RechargeAsync(99)).ReturnsAsync((BotResponseDto?)null);

        var result = await _controller.Recharge(99);

        Assert.IsType<NotFoundResult>(result);
    }

    // --- UpdateServicingStatus ---

    [Fact]
    public async Task UpdateServicingStatus_ExistingId_ReturnsOk()
    {
        var dto = new UpdateServicingStatusDto { IsServicingCustomer = true };
        var updated = new BotResponseDto { Id = 1, IsServicingCustomer = true };
        _mockService.Setup(s => s.UpdateServicingStatusAsync(1, dto)).ReturnsAsync(updated);

        var result = await _controller.UpdateServicingStatus(1, dto);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.True(((BotResponseDto)ok.Value!).IsServicingCustomer);
    }

    [Fact]
    public async Task UpdateServicingStatus_NonExistingId_ReturnsNotFound()
    {
        var dto = new UpdateServicingStatusDto { IsServicingCustomer = true };
        _mockService.Setup(s => s.UpdateServicingStatusAsync(99, dto)).ReturnsAsync((BotResponseDto?)null);

        var result = await _controller.UpdateServicingStatus(99, dto);

        Assert.IsType<NotFoundResult>(result);
    }
}
