using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using Azure.Core;
using Azure.Identity;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace ReadBotsFunction.Functions;

public sealed class ProcessSupportEscalations
{
    private static readonly TokenRequestContext StorageTokenRequest =
        new(["https://storage.azure.com/.default"]);

    private readonly ILogger<ProcessSupportEscalations> _logger;
    private readonly DefaultAzureCredential _credential = new();
    private readonly HttpClient _httpClient = new();

    public ProcessSupportEscalations(ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<ProcessSupportEscalations>();
    }

    [Function(nameof(ProcessSupportEscalations))]
    public async Task RunAsync(
        [ServiceBusTrigger("%SupportEscalationQueueName%", Connection = "SupportEscalationServiceBus")]
        string message,
        FunctionContext context,
        CancellationToken cancellationToken)
    {
        var blobServiceUri = Environment.GetEnvironmentVariable("EscalationArchive__BlobServiceUri");
        var containerName = Environment.GetEnvironmentVariable("EscalationArchive__ContainerName") ?? "support-escalations";

        if (string.IsNullOrWhiteSpace(blobServiceUri))
        {
            _logger.LogWarning("Support escalation archive is not configured.");
            return;
        }

        await EnsureContainerExistsAsync(blobServiceUri, containerName, cancellationToken);
        await WriteEscalationAsync(blobServiceUri, containerName, message, cancellationToken);
    }

    private async Task EnsureContainerExistsAsync(
        string blobServiceUri,
        string containerName,
        CancellationToken cancellationToken)
    {
        var containerUri = new Uri($"{blobServiceUri.TrimEnd('/')}/{containerName}?restype=container");
        using var request = await CreateAuthorizedRequestAsync(HttpMethod.Put, containerUri, cancellationToken);
        request.Content = new ByteArrayContent([]);

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        if (response.StatusCode != HttpStatusCode.Created &&
            response.StatusCode != HttpStatusCode.Conflict)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException(
                $"Blob Storage returned HTTP {(int)response.StatusCode} while creating the support escalation container: {body}");
        }
    }

    private async Task WriteEscalationAsync(
        string blobServiceUri,
        string containerName,
        string message,
        CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var blobName = FormattableString.Invariant($"{now:yyyy/MM/dd}/{now:HHmmssfff}-{Guid.NewGuid():N}.json");
        var blobUri = new Uri($"{blobServiceUri.TrimEnd('/')}/{containerName}/{blobName}");
        using var request = await CreateAuthorizedRequestAsync(HttpMethod.Put, blobUri, cancellationToken);
        request.Headers.Add("x-ms-blob-type", "BlockBlob");
        request.Content = new StringContent(message, Encoding.UTF8, "application/json");

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        if (response.StatusCode != HttpStatusCode.Created)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException(
                $"Blob Storage returned HTTP {(int)response.StatusCode} while writing a support escalation: {body}");
        }
    }

    private async Task<HttpRequestMessage> CreateAuthorizedRequestAsync(
        HttpMethod method,
        Uri uri,
        CancellationToken cancellationToken)
    {
        var accessToken = await _credential.GetTokenAsync(StorageTokenRequest, cancellationToken);
        var request = new HttpRequestMessage(method, uri);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken.Token);
        request.Headers.Add("x-ms-date", DateTimeOffset.UtcNow.ToString("R", CultureInfo.InvariantCulture));
        request.Headers.Add("x-ms-version", "2023-11-03");
        return request;
    }
}
