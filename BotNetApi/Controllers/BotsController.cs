using BotNetApi.DTOs;
using BotNetApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace BotNetApi.Controllers;

[ApiController]
[Route("api/bots")]
public class BotsController : ControllerBase
{
    private readonly IBotService _botService;

    public BotsController(IBotService botService)
    {
        _botService = botService;
    }

    // GET /api/bots
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var bots = await _botService.GetAllAsync();
        return Ok(bots);
    }

    // GET /api/bots/{id}
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var bot = await _botService.GetByIdAsync(id);
        return bot is null ? NotFound() : Ok(bot);
    }

    // POST /api/bots
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateBotDto dto)
    {
        var created = await _botService.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    // PUT /api/bots/{id}
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateBotDto dto)
    {
        var updated = await _botService.UpdateAsync(id, dto);
        return updated is null ? NotFound() : Ok(updated);
    }

    // DELETE /api/bots/{id}
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var deleted = await _botService.DeleteAsync(id);
        return deleted ? NoContent() : NotFound();
    }

    // PUT /api/bots/{id}/recharge
    [HttpPut("{id:int}/recharge")]
    public async Task<IActionResult> Recharge(int id)
    {
        var updated = await _botService.RechargeAsync(id);
        return updated is null ? NotFound() : Ok(updated);
    }

    // PUT /api/bots/{id}/servicing-status
    [HttpPut("{id:int}/servicing-status")]
    public async Task<IActionResult> UpdateServicingStatus(int id, [FromBody] UpdateServicingStatusDto dto)
    {
        var updated = await _botService.UpdateServicingStatusAsync(id, dto);
        return updated is null ? NotFound() : Ok(updated);
    }
}
