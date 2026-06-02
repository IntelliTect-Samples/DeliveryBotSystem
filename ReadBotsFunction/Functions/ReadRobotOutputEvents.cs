using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using Azure.Identity;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ReadableBotState.Projection;
using ReadableBotState.ReadModel;
using ReadableBotState.RobotEvents;
using ReadBotsFunction.Services;

namespace ReadBotsFunction.Functions;

public sealed class ReadRobotOutputEvents
{
    private const int MaxPreviewLength = 1000;

    private readonly Container _botsContainer;
    private readonly Container _diagnosticsContainer;
    private readonly BotReadModelProjector _projector;
    private readonly ILogger<ReadRobotOutputEvents> _logger;

    public ReadRobotOutputEvents(
        IConfiguration configuration,
        ILogger<ReadRobotOutputEvents> logger)
    {
        _logger = logger;
        _projector = new BotReadModelProjector();

        var endpoint = GetRequiredSetting(configuration, "ReadableBotNetwork:CosmosAccountEndpoint");
        var databaseName = GetRequiredSetting(configuration, "ReadableBotNetwork:CosmosDatabaseName");
        var botsContainerName = GetRequiredSetting(configuration, "ReadableBotNetwork:BotsContainerName");
        var diagnosticsContainerName = GetRequiredSetting(configuration, "ReadableBotNetwork:DiagnosticsContainerName");

        var cosmosClient = new CosmosClient(
            endpoint,
            new DefaultAzureCredential(),
            new CosmosClientOptions
            {
                Serializer = new SystemTextJsonCosmosSerializer(RobotEventJson.SerializerOptions)
            });

        _botsContainer = cosmosClient.GetContainer(databaseName, botsContainerName);
        _diagnosticsContainer = cosmosClient.GetContainer(databaseName, diagnosticsContainerName);
    }

    [Function(nameof(ReadRobotOutputEvents))]
    public async Task RunAsync(
        [EventHubTrigger(
            "%RobotOutputEventHubName%",
            Connection = "RobotOutputEventHubIdentity",
            ConsumerGroup = "%RobotOutputEventHubConsumerGroup%")]
        string[] events,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "ReadRobotOutputEvents received {EventCount} robot-output event(s).",
            events.Length);

        var retryRequired = false;

        foreach (var rawEvent in events)
        {
            retryRequired |= await ProcessOneEventAsync(rawEvent, cancellationToken);
        }

        if (retryRequired)
        {
            throw new InvalidOperationException(
                "One or more robot-output events failed during Cosmos DB processing. See function diagnostics for details.");
        }
    }

    private async Task<bool> ProcessOneEventAsync(string rawEvent, CancellationToken cancellationToken)
    {
        RobotEventEnvelope? envelope;

        try
        {
            envelope = JsonSerializer.Deserialize<RobotEventEnvelope>(
                rawEvent,
                RobotEventJson.SerializerOptions);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Robot-output event rejected because it is not valid JSON.");
            await WriteFailureDiagnosticAsync(
                rawEvent,
                envelope: null,
                reason: "Invalid JSON event body.",
                errorType: ex.GetType().Name,
                shouldRetry: false,
                cancellationToken);

            return false;
        }

        if (envelope is null)
        {
            _logger.LogError("Robot-output event rejected because deserialization returned null.");
            await WriteFailureDiagnosticAsync(
                rawEvent,
                envelope: null,
                reason: "Null event envelope.",
                errorType: null,
                shouldRetry: false,
                cancellationToken);

            return false;
        }

        _logger.LogInformation(
            "Robot-output event identified. EventType={EventType}, BotId={BotId}, TimestampUtc={TimestampUtc}, SchemaVersion={SchemaVersion}, EventId={EventId}",
            envelope.EventType,
            envelope.BotId,
            envelope.TimestampUtc,
            envelope.SchemaVersion,
            envelope.EventId);

        if (string.IsNullOrWhiteSpace(envelope.BotId))
        {
            await RejectEventAsync(rawEvent, envelope, "Event rejected: missing botId.", cancellationToken);
            return false;
        }

        var botId = envelope.BotId.Trim();

        try
        {
            var updateResult = await ApplyWithRetryAsync(botId, envelope, cancellationToken);

            if (updateResult.IsRejected)
            {
                await RejectEventAsync(rawEvent, envelope, updateResult.Message, cancellationToken);
                return false;
            }

            _logger.LogInformation(
                "Robot-output event processed. EventId={EventId}, BotId={BotId}, Result={Result}",
                envelope.EventId,
                botId,
                updateResult.Message);

            return false;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(
                ex,
                "Robot-output event failed while updating Cosmos DB. EventId={EventId}, BotId={BotId}, EventType={EventType}",
                envelope.EventId,
                botId,
                envelope.EventType);

            await WriteFailureDiagnosticAsync(
                rawEvent,
                envelope,
                "Cosmos DB processing failed.",
                ex.GetType().Name,
                shouldRetry: true,
                cancellationToken);

            return true;
        }
    }

    private async Task<BotUpdateResult> ApplyWithRetryAsync(
        string botId,
        RobotEventEnvelope envelope,
        CancellationToken cancellationToken)
    {
        const int maxAttempts = 3;

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            var (currentDocument, etag) = await ReadCurrentDocumentAsync(botId, cancellationToken);
            var projection = _projector.Apply(currentDocument, envelope);

            if (IsRejected(projection))
            {
                return BotUpdateResult.Rejected(projection.Message);
            }

            if (!projection.ShouldPersist || projection.Document is null)
            {
                return BotUpdateResult.Ignored(projection.Message);
            }

            try
            {
                if (etag is null)
                {
                    await _botsContainer.CreateItemAsync(
                        projection.Document,
                        new PartitionKey(projection.Document.BotId),
                        cancellationToken: cancellationToken);
                }
                else
                {
                    await _botsContainer.ReplaceItemAsync(
                        projection.Document,
                        projection.Document.Id,
                        new PartitionKey(projection.Document.BotId),
                        new ItemRequestOptions
                        {
                            IfMatchEtag = etag
                        },
                        cancellationToken);
                }

                return BotUpdateResult.Applied(projection.Message);
            }
            catch (CosmosException ex) when (
                (ex.StatusCode == HttpStatusCode.Conflict ||
                 ex.StatusCode == HttpStatusCode.PreconditionFailed) &&
                attempt < maxAttempts)
            {
                _logger.LogWarning(
                    ex,
                    "Retrying bot read-model update after concurrent Cosmos write. EventId={EventId}, BotId={BotId}, Attempt={Attempt}",
                    envelope.EventId,
                    botId,
                    attempt);
            }
        }

        throw new InvalidOperationException(
            $"Could not update bot read model for bot '{botId}' after concurrent write retries.");
    }

    private async Task<(BotReadModel? Document, string? Etag)> ReadCurrentDocumentAsync(
        string botId,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await _botsContainer.ReadItemAsync<BotReadModel>(
                botId,
                new PartitionKey(botId),
                cancellationToken: cancellationToken);

            return (response.Resource, response.ETag);
        }
        catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            return (null, null);
        }
    }

    private async Task RejectEventAsync(
        string rawEvent,
        RobotEventEnvelope envelope,
        string reason,
        CancellationToken cancellationToken)
    {
        _logger.LogError(
            "Robot-output event rejected. EventId={EventId}, BotId={BotId}, EventType={EventType}, Reason={Reason}",
            envelope.EventId,
            envelope.BotId,
            envelope.EventType,
            reason);

        await WriteFailureDiagnosticAsync(
            rawEvent,
            envelope,
            reason,
            errorType: null,
            shouldRetry: false,
            cancellationToken);
    }

    private async Task WriteFailureDiagnosticAsync(
        string rawEvent,
        RobotEventEnvelope? envelope,
        string reason,
        string? errorType,
        bool shouldRetry,
        CancellationToken cancellationToken)
    {
        var document = new RobotEventFailureDiagnostic
        {
            Id = $"robot-output-failure-{DateTimeOffset.UtcNow:yyyyMMddHHmmssfffffff}-{Guid.NewGuid():N}",
            EventId = envelope?.EventId,
            EventType = envelope?.EventType,
            BotId = envelope?.BotId,
            TimestampUtc = envelope?.TimestampUtc,
            SchemaVersion = envelope?.SchemaVersion,
            Reason = reason,
            ErrorType = errorType,
            ShouldRetry = shouldRetry,
            RawEventPreview = GetPreview(rawEvent),
            CreatedAtUtc = DateTimeOffset.UtcNow
        };

        try
        {
            await _diagnosticsContainer.CreateItemAsync(
                document,
                new PartitionKey(document.Id),
                cancellationToken: cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(
                ex,
                "Could not write robot-output failure diagnostic. OriginalReason={OriginalReason}",
                reason);
        }
    }

    private static bool IsRejected(ProjectionResult projection)
    {
        return projection.Message.StartsWith(
            "Event rejected:",
            StringComparison.OrdinalIgnoreCase);
    }

    private static string GetRequiredSetting(IConfiguration configuration, string key)
    {
        var value = configuration[key];
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException($"{key} is required.");
        }

        return value;
    }

    private static string? GetPreview(string? eventBody)
    {
        if (string.IsNullOrEmpty(eventBody))
        {
            return eventBody;
        }

        return eventBody.Length <= MaxPreviewLength
            ? eventBody
            : eventBody[..MaxPreviewLength];
    }
}

public sealed record BotUpdateResult(
    bool ShouldPersist,
    bool IsRejected,
    string Message)
{
    public static BotUpdateResult Applied(string message) => new(true, false, message);
    public static BotUpdateResult Ignored(string message) => new(false, false, message);
    public static BotUpdateResult Rejected(string message) => new(false, true, message);
}

public sealed class RobotEventFailureDiagnostic
{
    [JsonPropertyName("id")]
    public string Id { get; init; } = string.Empty;

    [JsonPropertyName("eventId")]
    public string? EventId { get; init; }

    [JsonPropertyName("eventType")]
    public string? EventType { get; init; }

    [JsonPropertyName("botId")]
    public string? BotId { get; init; }

    [JsonPropertyName("timestampUtc")]
    public DateTimeOffset? TimestampUtc { get; init; }

    [JsonPropertyName("schemaVersion")]
    public string? SchemaVersion { get; init; }

    [JsonPropertyName("reason")]
    public string Reason { get; init; } = string.Empty;

    [JsonPropertyName("errorType")]
    public string? ErrorType { get; init; }

    [JsonPropertyName("shouldRetry")]
    public bool ShouldRetry { get; init; }

    [JsonPropertyName("rawEventPreview")]
    public string? RawEventPreview { get; init; }

    [JsonPropertyName("createdAtUtc")]
    public DateTimeOffset CreatedAtUtc { get; init; }
}
