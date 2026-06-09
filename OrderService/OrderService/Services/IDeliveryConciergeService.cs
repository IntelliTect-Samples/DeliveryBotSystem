// AI "concierge" backed by Azure AI Foundry (Azure OpenAI), used to generate
// customer-friendly copy and answer questions grounded in a single order (#43).
// All calls authenticate with the Container App's managed identity (no keys).
using OrderService.Models;

namespace OrderService.Services;

public interface IDeliveryConciergeService
{
    // True when a Foundry endpoint + deployment are configured. When false the
    // service is a no-op so the API still runs locally / without AI.
    bool IsEnabled { get; }

    // Writes a short, upbeat update for an order that just moved `from` -> `to`.
    // Returns null when AI is disabled or the call fails (best-effort).
    Task<string?> GenerateStatusMessageAsync(Order order, OrderStatus from, OrderStatus to, CancellationToken ct = default);

    // Answers a customer's free-text question using only the given order's data.
    Task<string> AnswerQuestionAsync(Order order, string question, CancellationToken ct = default);
}
