using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Azure.Core;
using Azure.Identity;
using AgentService.DTOs;
using AgentService.Models;
using AgentService.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AgentService.Services;

public sealed class AzureOpenAiAgentService : IAgentService
{
    private static readonly TokenRequestContext AzureCognitiveServicesScope =
        new(["https://cognitiveservices.azure.com/.default"]);
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private static readonly string[] EscalationKeywords =
    [
        "human",
        "support",
        "refund",
        "complaint",
        "late",
        "stuck",
        "broken",
        "missing",
        "failed",
        "cancel",
        "cancellation"
    ];

    private readonly HttpClient _httpClient;
    private readonly AzureOpenAiOptions _options;
    private readonly AgentIntegrationOptions _integrationOptions;
    private readonly IAzureOpenAiApiKeyProvider _apiKeyProvider;
    private readonly IAgentGroundingService _groundingService;
    private readonly IChatTranscriptArchive _chatTranscriptArchive;
    private readonly ISupportEscalationPublisher _supportEscalationPublisher;
    private readonly TokenCredential _credential;
    private readonly ILogger<AzureOpenAiAgentService> _logger;

    public AzureOpenAiAgentService(
        HttpClient httpClient,
        IOptions<AzureOpenAiOptions> options,
        IOptions<AgentIntegrationOptions> integrationOptions,
        IAzureOpenAiApiKeyProvider apiKeyProvider,
        IAgentGroundingService groundingService,
        IChatTranscriptArchive chatTranscriptArchive,
        ISupportEscalationPublisher supportEscalationPublisher,
        ILogger<AzureOpenAiAgentService> logger)
        : this(
            httpClient,
            options,
            integrationOptions,
            apiKeyProvider,
            groundingService,
            chatTranscriptArchive,
            supportEscalationPublisher,
            new DefaultAzureCredential(),
            logger)
    {
    }

    public AzureOpenAiAgentService(
        HttpClient httpClient,
        IOptions<AzureOpenAiOptions> options,
        IOptions<AgentIntegrationOptions> integrationOptions,
        IAzureOpenAiApiKeyProvider apiKeyProvider,
        IAgentGroundingService groundingService,
        IChatTranscriptArchive chatTranscriptArchive,
        ISupportEscalationPublisher supportEscalationPublisher,
        TokenCredential credential,
        ILogger<AzureOpenAiAgentService> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _integrationOptions = integrationOptions.Value;
        _apiKeyProvider = apiKeyProvider;
        _groundingService = groundingService;
        _chatTranscriptArchive = chatTranscriptArchive;
        _supportEscalationPublisher = supportEscalationPublisher;
        _credential = credential;
        _logger = logger;
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
        await TryGroundRequestAsync(request, cancellationToken);

        if (string.IsNullOrWhiteSpace(_options.Endpoint) ||
            string.IsNullOrWhiteSpace(_options.Deployment))
        {
            throw new InvalidOperationException(
                "Azure OpenAI is not configured. Set AzureOpenAI:Endpoint and AzureOpenAI:Deployment, plus either AzureOpenAI:ApiKey, AzureOpenAI:ApiKeySecretName with KeyVault:VaultUri, or an Azure identity that can access the resource.");
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

        var result = new AgentChatResponseDto
        {
            Reply = AzureOpenAiChatMapper.ExtractReply(document),
            Source = "azure-openai",
            Model = AzureOpenAiChatMapper.ExtractModel(document)
        };

        await TryArchiveTranscriptAsync(request, result, cancellationToken);
        await TryPublishSupportEscalationAsync(request, result, cancellationToken);

        return result;
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
        var apiKey = await _apiKeyProvider.GetApiKeyAsync(cancellationToken);
        if (!string.IsNullOrWhiteSpace(apiKey))
        {
            httpRequest.Headers.Add("api-key", apiKey);
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

    private async Task TryGroundRequestAsync(
        AgentChatRequestDto request,
        CancellationToken cancellationToken)
    {
        try
        {
            await _groundingService.EnrichAsync(request, cancellationToken);
        }
        catch (AuthenticationFailedException error)
        {
            LogGroundingFailure(error);
        }
        catch (HttpRequestException error)
        {
            LogGroundingFailure(error);
        }
        catch (JsonException error)
        {
            LogGroundingFailure(error);
        }
        catch (NotSupportedException error)
        {
            LogGroundingFailure(error);
        }
        catch (InvalidOperationException error)
        {
            LogGroundingFailure(error);
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
        catch (HttpRequestException)
        {
            return null;
        }
        catch (JsonException)
        {
            return null;
        }
        catch (NotSupportedException)
        {
            return null;
        }
        catch (InvalidOperationException)
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
        catch (HttpRequestException)
        {
            return null;
        }
        catch (JsonException)
        {
            return null;
        }
        catch (NotSupportedException)
        {
            return null;
        }
        catch (InvalidOperationException)
        {
            return null;
        }
    }

    private async Task TryArchiveTranscriptAsync(
        AgentChatRequestDto request,
        AgentChatResponseDto response,
        CancellationToken cancellationToken)
    {
        try
        {
            await _chatTranscriptArchive.ArchiveAsync(
                new AgentChatTranscriptRecord
                {
                    ArchivedAtUtc = DateTimeOffset.UtcNow,
                    RelatedOrderId = request.Context?.LatestOrder?.Id,
                    Request = request,
                    Response = response
                },
                cancellationToken);
        }
        catch (AuthenticationFailedException error)
        {
            LogArchiveFailure(error);
        }
        catch (HttpRequestException error)
        {
            LogArchiveFailure(error);
        }
        catch (JsonException error)
        {
            LogArchiveFailure(error);
        }
        catch (NotSupportedException error)
        {
            LogArchiveFailure(error);
        }
        catch (UriFormatException error)
        {
            LogArchiveFailure(error);
        }
        catch (InvalidOperationException error)
        {
            LogArchiveFailure(error);
        }
    }

    private async Task TryPublishSupportEscalationAsync(
        AgentChatRequestDto request,
        AgentChatResponseDto response,
        CancellationToken cancellationToken)
    {
        var reason = DetermineEscalationReason(request);
        if (string.IsNullOrWhiteSpace(reason))
        {
            return;
        }

        try
        {
            await _supportEscalationPublisher.PublishAsync(
                new SupportEscalationRecord
                {
                    CreatedAtUtc = DateTimeOffset.UtcNow,
                    RelatedOrderId = request.Context?.LatestOrder?.Id,
                    Reason = reason,
                    Request = request,
                    Response = response
                },
                cancellationToken);
        }
        catch (AuthenticationFailedException error)
        {
            LogEscalationFailure(error);
        }
        catch (HttpRequestException error)
        {
            LogEscalationFailure(error);
        }
        catch (JsonException error)
        {
            LogEscalationFailure(error);
        }
        catch (NotSupportedException error)
        {
            LogEscalationFailure(error);
        }
        catch (UriFormatException error)
        {
            LogEscalationFailure(error);
        }
        catch (InvalidOperationException error)
        {
            LogEscalationFailure(error);
        }
    }

    private void LogGroundingFailure(Exception error) =>
        _logger.LogWarning(error, "Failed to enrich agent request with Azure AI Search grounding.");

    private void LogArchiveFailure(Exception error) =>
        _logger.LogWarning(error, "Failed to archive agent transcript.");

    private void LogEscalationFailure(Exception error) =>
        _logger.LogWarning(error, "Failed to publish support escalation.");

    private static string? DetermineEscalationReason(AgentChatRequestDto request)
    {
        var message = request.Message ?? "";
        if (EscalationKeywords.Any(keyword =>
            message.Contains(keyword, StringComparison.OrdinalIgnoreCase)))
        {
            return "customer-request";
        }

        var status = request.Context?.LatestOrder?.Status;
        if (!string.IsNullOrWhiteSpace(status) &&
            (status.Contains("fail", StringComparison.OrdinalIgnoreCase) ||
             status.Contains("cancel", StringComparison.OrdinalIgnoreCase)))
        {
            return "order-status";
        }

        return null;
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
