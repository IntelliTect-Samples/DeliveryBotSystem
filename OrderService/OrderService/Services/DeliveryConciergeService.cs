// Azure AI Foundry (Azure OpenAI) implementation of the delivery concierge (#43).
//
// Auth is keyless: the Container App's system-assigned managed identity holds the
// "Cognitive Services OpenAI User" role on the Foundry account, so DefaultAzureCredential
// gets a token automatically. No API keys live in config, code, or secrets.
//
// If Foundry:Endpoint is unset (e.g. local dev), the service disables itself and the
// rest of the API keeps working.
using Azure.AI.OpenAI;
using Azure.Identity;
using OpenAI.Chat;
using OrderService.Models;

namespace OrderService.Services;

public sealed class DeliveryConciergeService : IDeliveryConciergeService
{
    private readonly ILogger<DeliveryConciergeService> _logger;
    private readonly ChatClient? _chat;

    public DeliveryConciergeService(IConfiguration config, ILogger<DeliveryConciergeService> logger)
    {
        _logger = logger;

        var endpoint = config["Foundry:Endpoint"];
        var deployment = config["Foundry:Deployment"] ?? "gpt-4o-mini";
        var apiKey = config["Foundry:ApiKey"];

        if (string.IsNullOrWhiteSpace(endpoint))
        {
            _logger.LogWarning(
                "Foundry:Endpoint not configured — DeliveryConciergeService disabled. " +
                "AI status messages and the assistant endpoint will be skipped.");
            return;
        }

        // Prefer keyless (managed identity). Fall back to an API key when one is supplied —
        // needed until the app's identity can be granted the "Cognitive Services OpenAI User"
        // role, which requires an org-level RBAC grant we don't currently control. Drop
        // Foundry:ApiKey and it reverts to managed identity automatically.
        var client = string.IsNullOrWhiteSpace(apiKey)
            ? new AzureOpenAIClient(new Uri(endpoint), new DefaultAzureCredential())
            : new AzureOpenAIClient(new Uri(endpoint), new System.ClientModel.ApiKeyCredential(apiKey));
        _chat = client.GetChatClient(deployment);
        _logger.LogInformation("DeliveryConciergeService enabled. Endpoint={Endpoint} Deployment={Deployment} Auth={Auth}",
            endpoint, deployment, string.IsNullOrWhiteSpace(apiKey) ? "ManagedIdentity" : "ApiKey");
    }

    public bool IsEnabled => _chat is not null;

    public async Task<string?> GenerateStatusMessageAsync(Order order, OrderStatus from, OrderStatus to, CancellationToken ct = default)
    {
        if (_chat is null)
            return null;

        const string system =
            "You are a friendly assistant for an autonomous robot food-delivery service in Spokane. " +
            "Write ONE upbeat sentence (max 200 characters, at most one emoji) telling the customer their " +
            "order status just changed. Do not invent details beyond what you are given.";
        var user =
            $"Order {order.Id} moved from {from} to {to}. " +
            $"Items: {Describe(order)}. Delivery address: {order.DeliveryAddress}.";

        try
        {
            var completion = await _chat.CompleteChatAsync(
                [new SystemChatMessage(system), new UserChatMessage(user)],
                cancellationToken: ct);
            return completion.Value.Content[0].Text.Trim();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Foundry status-message generation failed. OrderId={OrderId}", order.Id);
            return null;
        }
    }

    public async Task<string> AnswerQuestionAsync(Order order, string question, CancellationToken ct = default)
    {
        if (_chat is null)
            return "The delivery assistant isn't available right now.";

        const string system =
            "You are a delivery assistant. Answer the customer's question using ONLY the order facts provided. " +
            "If the answer isn't in the facts, say you don't have that information. Keep it under 3 sentences.";
        var facts =
            $"Order id: {order.Id}\n" +
            $"Status: {order.Status}\n" +
            $"Assigned bot: {order.AssignedBotId ?? "none yet"}\n" +
            $"Items: {Describe(order)}\n" +
            $"Delivery address: {order.DeliveryAddress}\n" +
            $"Placed at (UTC): {order.CreatedAt:u}";

        try
        {
            var completion = await _chat.CompleteChatAsync(
                [new SystemChatMessage(system), new UserChatMessage($"Order facts:\n{facts}\n\nQuestion: {question}")],
                cancellationToken: ct);
            return completion.Value.Content[0].Text.Trim();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Foundry assistant call failed. OrderId={OrderId}", order.Id);
            return "Sorry — I couldn't look that up right now.";
        }
    }

    private static string Describe(Order order) =>
        order.Items.Count == 0
            ? "no items listed"
            : string.Join(", ", order.Items.Select(i => $"{i.Quantity}x {i.ItemId}"));
}
