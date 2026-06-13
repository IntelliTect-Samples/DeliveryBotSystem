using Azure.Core;
using Azure.Identity;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using AgentService.DTOs;
using AgentService.Models;
using AgentService.Options;
using Microsoft.Extensions.Options;

namespace AgentService.Services;

public sealed class AzureOpenAiAgentService : IAgentService
{
    private static readonly TokenRequestContext AzureCognitiveServicesScope =
        new(["https://cognitiveservices.azure.com/.default"]);
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _httpClient;
    private readonly AzureOpenAiOptions _options;
    private readonly AgentIntegrationOptions _integrationOptions;
    private readonly TokenCredential _credential = new DefaultAzureCredential();

    public AzureOpenAiAgentService(
        HttpClient httpClient,
        IOptions<AzureOpenAiOptions> options,
        IOptions<AgentIntegrationOptions> integrationOptions)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _integrationOptions = integrationOptions.Value;
    }

    public async Task<AgentChatResponseDto> ChatAsync(
        AgentChatRequestDto request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Message))
        {
            throw new ArgumentException("Message is required.", nameof(request));
        }

        await EnrichRequestAsync(request, cancellationToken);

        if (string.IsNullOrWhiteSpace(_options.Endpoint) ||
            string.IsNullOrWhiteSpace(_options.Deployment))
        {
            throw new InvalidOperationException(
                "Azure OpenAI is not configured. Set AzureOpenAI:Endpoint and AzureOpenAI:Deployment, plus either AzureOpenAI:ApiKey or an Azure identity that can access the resource.");
        }

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, BuildRequestUri());
        httpRequest.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        await AuthorizeRequestAsync(httpRequest, cancellationToken);
        httpRequest.Content = new StringContent(
            JsonSerializer.Serialize(AzureOpenAiChatMapper.BuildRequestBody(request, _options)),
            Encoding.UTF8,
            "application/json");

        using var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"Azure OpenAI returned HTTP {(int)response.StatusCode}: {responseBody}");
        }

        using var document = JsonDocument.Parse(responseBody);

        return new AgentChatResponseDto
        {
            Reply = AzureOpenAiChatMapper.ExtractReply(document),
            Source = "azure-openai",
            Model = AzureOpenAiChatMapper.ExtractModel(document)
        };
    }

    private string BuildRequestUri()
    {
        var endpoint = _options.Endpoint.TrimEnd('/');
        return $"{endpoint}/openai/deployments/{_options.Deployment}/chat/completions?api-version={_options.ApiVersion}";
    }

    private async Task AuthorizeRequestAsync(
        HttpRequestMessage httpRequest,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            httpRequest.Headers.Add("api-key", _options.ApiKey);
            return;
        }

        var token = await _credential.GetTokenAsync(AzureCognitiveServicesScope, cancellationToken);
        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.Token);
    }

    private async Task EnrichRequestAsync(
        AgentChatRequestDto request,
        CancellationToken cancellationToken)
    {
        request.Context ??= new AgentChatContextDto();

        var notes = new List<string>();

        var liveOrder = await TryGetLiveOrderAsync(request, cancellationToken);
        if (liveOrder is not null)
        {
            request.Context.LatestOrder ??= new AgentLatestOrderDto();
            request.Context.LatestOrder.Id ??= liveOrder.Id;
            request.Context.LatestOrder.Status = liveOrder.Status ?? request.Context.LatestOrder.Status;
            request.Context.LatestOrder.AssignedBotId = liveOrder.AssignedBotId ?? request.Context.LatestOrder.AssignedBotId;
            request.Context.LatestOrder.DeliveryAddress = liveOrder.DeliveryAddress ?? request.Context.LatestOrder.DeliveryAddress;
            request.Context.LatestOrder.ItemsSummary ??= SummarizeItems(liveOrder.Items);

            notes.Add($"- Live order status: {liveOrder.Status ?? "Unknown"}");
            notes.Add($"- Live order assigned bot: {liveOrder.AssignedBotId ?? "None"}");
        }

        var liveBot = await TryGetLiveBotAsync(request.Context.LatestOrder?.AssignedBotId, cancellationToken);
        if (liveBot is not null)
        {
            notes.Add($"- Live bot status: {liveBot.Status ?? "Unknown"}");
            notes.Add($"- Live bot battery: {FormatBattery(liveBot.PowerLevel)}");
            notes.Add($"- Live bot queued orders: {liveBot.QueuedOrderCount?.ToString() ?? "Unknown"}");
        }

        if (notes.Count > 0)
        {
            request.Context.LiveDataSummary = string.Join(Environment.NewLine, notes);
        }
    }

    private async Task<LiveOrderSnapshot?> TryGetLiveOrderAsync(
        AgentChatRequestDto request,
        CancellationToken cancellationToken)
    {
        var orderId = request.Context?.LatestOrder?.Id;
        if (string.IsNullOrWhiteSpace(orderId) ||
            string.IsNullOrWhiteSpace(_integrationOptions.OrderServiceBaseUrl))
        {
            return null;
        }

        var baseUrl = _integrationOptions.OrderServiceBaseUrl.TrimEnd('/');
        var requestUri = $"{baseUrl}/api/orders/{orderId}";

        try
        {
            using var response = await _httpClient.GetAsync(requestUri, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            await using var responseStream = await response.Content.ReadAsStreamAsync(cancellationToken);
            return await JsonSerializer.DeserializeAsync<LiveOrderSnapshot>(
                responseStream,
                JsonOptions,
                cancellationToken);
        }
        catch
        {
            return null;
        }
    }

    private async Task<LiveBotSnapshot?> TryGetLiveBotAsync(
        string? botId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(botId) ||
            string.IsNullOrWhiteSpace(_integrationOptions.SimulatorBaseUrl))
        {
            return null;
        }

        var baseUrl = _integrationOptions.SimulatorBaseUrl.TrimEnd('/');
        var requestUri = $"{baseUrl}/bots/{botId}";

        try
        {
            using var response = await _httpClient.GetAsync(requestUri, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            await using var responseStream = await response.Content.ReadAsStreamAsync(cancellationToken);
            return await JsonSerializer.DeserializeAsync<LiveBotSnapshot>(
                responseStream,
                JsonOptions,
                cancellationToken);
        }
        catch
        {
            return null;
        }
    }

    private static string SummarizeItems(IEnumerable<LiveOrderItemSnapshot> items)
    {
        var materialized = items
            .Where(item => !string.IsNullOrWhiteSpace(item.ItemId))
            .Select(item => $"{item.ItemId} x{item.Quantity}")
            .ToList();

        return materialized.Count == 0 ? "Unknown" : string.Join(", ", materialized);
    }

    private static string FormatBattery(double? powerLevel)
    {
        return powerLevel is null ? "Unknown" : $"{Math.Round(powerLevel.Value)}%";
    }
}
