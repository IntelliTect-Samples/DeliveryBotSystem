using System.Text.Json;
using AgentService.DTOs;
using AgentService.Options;

namespace AgentService.Services;

public static class AzureOpenAiChatMapper
{
    public static string BuildUserPrompt(AgentChatRequestDto request)
    {
        var message = request.Message.Trim();
        var latestOrder = request.Context?.LatestOrder;
        var route = request.Context?.Route;
        var history = request.History
            .Where(entry => !string.IsNullOrWhiteSpace(entry.Text))
            .TakeLast(8)
            .ToList();

        var lines = new List<string>
        {
            "Customer question:",
            message,
            "",
            "Latest order context:"
        };

        if (latestOrder is null)
        {
            lines.Add("- No latest order is available.");
        }
        else
        {
            lines.Add($"- Order ID: {latestOrder.Id ?? "Unknown"}");
            lines.Add($"- Status: {latestOrder.Status ?? "Unknown"}");
            lines.Add($"- Assigned bot: {latestOrder.AssignedBotId ?? "None"}");
            lines.Add($"- Delivery address: {latestOrder.DeliveryAddress ?? "Unknown"}");
            lines.Add($"- Items: {latestOrder.ItemsSummary ?? "Unknown"}");
        }

        lines.Add("");
        lines.Add("Route context:");

        if (route is null)
        {
            lines.Add("- No active route is available.");
        }
        else
        {
            lines.Add($"- Distance: {route.Distance ?? "Unknown"}");
            lines.Add($"- ETA: {route.Eta ?? "Unknown"}");
            lines.Add($"- Source: {route.Source ?? "Unknown"}");
        }

        lines.Add("");
        lines.Add("Recent conversation:");

        if (history.Count == 0)
        {
            lines.Add("- No earlier conversation is available.");
        }
        else
        {
            foreach (var entry in history)
            {
                lines.Add($"- {entry.Role}: {entry.Text}");
            }
        }

        lines.Add("");
        lines.Add("Live service data:");

        if (string.IsNullOrWhiteSpace(request.Context?.LiveDataSummary))
        {
            lines.Add("- No live service enrichment is available.");
        }
        else
        {
            lines.Add(request.Context.LiveDataSummary);
        }

        lines.Add("");
        lines.Add("Answer the customer directly in plain language.");
        lines.Add("If a detail is missing, say that directly instead of guessing.");

        return string.Join(Environment.NewLine, lines);
    }

    public static object BuildRequestBody(AgentChatRequestDto request, AzureOpenAiOptions options) =>
        new
        {
            messages = new object[]
            {
                new
                {
                    role = "system",
                    content = options.SystemPrompt
                },
                new
                {
                    role = "user",
                    content = BuildUserPrompt(request)
                }
            },
            temperature = 0.2,
            max_tokens = 220
        };

    public static string ExtractReply(JsonDocument document)
    {
        if (!document.RootElement.TryGetProperty("choices", out var choices) ||
            choices.ValueKind != JsonValueKind.Array ||
            choices.GetArrayLength() == 0)
        {
            throw new InvalidOperationException("Azure OpenAI returned no choices.");
        }

        var firstChoice = choices[0];
        if (!firstChoice.TryGetProperty("message", out var messageElement))
        {
            throw new InvalidOperationException("Azure OpenAI returned no message.");
        }

        if (!messageElement.TryGetProperty("content", out var contentElement))
        {
            throw new InvalidOperationException("Azure OpenAI returned no content.");
        }

        var reply = contentElement.GetString()?.Trim();
        if (string.IsNullOrWhiteSpace(reply))
        {
            throw new InvalidOperationException("Azure OpenAI returned an empty reply.");
        }

        return reply;
    }

    public static string? ExtractModel(JsonDocument document) =>
        document.RootElement.TryGetProperty("model", out var modelElement)
            ? modelElement.GetString()
            : null;
}
