using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Azure.Core;
using Azure.Identity;
using AgentService.Options;

namespace AgentService.Services;

public sealed class BlobChatTranscriptArchive : IChatTranscriptArchive
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    private static readonly TokenRequestContext StorageTokenRequest =
        new(["https://storage.azure.com/.default"]);

    private readonly TranscriptArchiveOptions _options;
    private readonly DefaultAzureCredential _credential = new();
    private readonly HttpClient _httpClient = new();
    private readonly SemaphoreSlim _containerLock = new(1, 1);
    private bool _containerExists;

    public BlobChatTranscriptArchive(TranscriptArchiveOptions options)
    {
        _options = options;
    }

    public async Task ArchiveAsync(
        AgentChatTranscriptRecord transcript,
        CancellationToken cancellationToken = default)
    {
        await EnsureContainerExistsAsync(cancellationToken);

        var blobUri = new Uri(
            $"{_options.BlobServiceUri.TrimEnd('/')}/{_options.ContainerName}/{BuildBlobName(transcript)}");
        using var request = await CreateAuthorizedRequestAsync(HttpMethod.Put, blobUri, cancellationToken);
        request.Headers.Add("x-ms-blob-type", "BlockBlob");
        request.Content = new StringContent(
            JsonSerializer.Serialize(transcript, JsonOptions),
            Encoding.UTF8,
            "application/json");

        using var response = await _httpClient.SendAsync(request, cancellationToken);

        if (response.StatusCode != HttpStatusCode.Created)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException(
                $"Blob Storage returned HTTP {(int)response.StatusCode} while writing the transcript blob: {body}");
        }
    }

    private static string BuildBlobName(AgentChatTranscriptRecord transcript)
    {
        var archivedAt = transcript.ArchivedAtUtc == default
            ? DateTimeOffset.UtcNow
            : transcript.ArchivedAtUtc;
        var orderSegment = SanitizePathSegment(transcript.RelatedOrderId, "no-order");

        return FormattableString.Invariant(
            $"{archivedAt:yyyy/MM/dd}/{orderSegment}/{archivedAt:HHmmssfff}-{Guid.NewGuid():N}.json");
    }

    private static string SanitizePathSegment(string? value, string fallback)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return fallback;
        }

        var builder = new char[value.Length];
        var length = 0;

        foreach (var character in value.Trim())
        {
            if (char.IsLetterOrDigit(character))
            {
                builder[length++] = char.ToLowerInvariant(character);
                continue;
            }

            if (length == 0 || builder[length - 1] == '-')
            {
                continue;
            }

            builder[length++] = '-';
        }

        if (length == 0)
        {
            return fallback;
        }

        return new string(builder, 0, length).Trim('-');
    }

    private async Task EnsureContainerExistsAsync(CancellationToken cancellationToken)
    {
        if (ContainerExists())
        {
            return;
        }

        await _containerLock.WaitAsync(cancellationToken);

        try
        {
            if (ContainerExists())
            {
                return;
            }

            var containerUri = new Uri(
                $"{_options.BlobServiceUri.TrimEnd('/')}/{_options.ContainerName}?restype=container");
            using var request = await CreateAuthorizedRequestAsync(HttpMethod.Put, containerUri, cancellationToken);
            request.Content = new ByteArrayContent([]);

            using var response = await _httpClient.SendAsync(request, cancellationToken);

            if (response.StatusCode != HttpStatusCode.Created &&
                response.StatusCode != HttpStatusCode.Conflict)
            {
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                throw new InvalidOperationException(
                    $"Blob Storage returned HTTP {(int)response.StatusCode} while creating the transcript container: {body}");
            }

            MarkContainerExists();
        }
        finally
        {
            _containerLock.Release();
        }
    }

    private bool ContainerExists() => System.Threading.Volatile.Read(ref _containerExists);

    private void MarkContainerExists() => System.Threading.Volatile.Write(ref _containerExists, true);

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
