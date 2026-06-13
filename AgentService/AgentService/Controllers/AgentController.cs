using AgentService.DTOs;
using AgentService.Services;
using Microsoft.AspNetCore.Mvc;

namespace AgentService.Controllers;

[ApiController]
[Route("")]
[Route("api/agent")]
public sealed class AgentController : ControllerBase
{
    private readonly IAgentService _agentService;

    public AgentController(IAgentService agentService)
    {
        _agentService = agentService;
    }

    [HttpPost("chat")]
    public async Task<ActionResult<AgentChatResponseDto>> Chat(
        [FromBody] AgentChatRequestDto request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Message))
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Invalid request",
                Detail = "Message is required."
            });
        }

        try
        {
            var response = await _agentService.ChatAsync(request, cancellationToken);
            return Ok(response);
        }
        catch (InvalidOperationException error)
        {
            return StatusCode(StatusCodes.Status502BadGateway, new ProblemDetails
            {
                Title = "Agent request failed",
                Detail = error.Message
            });
        }
    }
}
