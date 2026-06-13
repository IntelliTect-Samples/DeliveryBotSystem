using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Azure.Core;
using Azure.Identity;
using AgentService.DTOs;
using AgentService.Options;

namespace AgentService.Services;

public sealed class AzureAiSearchGroundingService : IAgentGroundingService
{
    private const string ApiVersion = "2024-07-01";
    private static readonly TokenRequestContext SearchTokenRequest =
        new(["https://search.azure.com/.default"]);
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    private readonly AzureAiSearchOptions _options;
    private readonly DefaultAzureCredential _credential = new();
    private readonly HttpClient _httpClient = new();
    private readonly SemaphoreSlim _indexLock = new(1, 1);
    private bool _indexReady;

    public AzureAiSearchGroundingService(AzureAiSearchOptions options)
    {
        _options = options;
    }

    public async Task EnrichAsync(
        AgentChatRequestDto request,
        CancellationToken cancellationToken = default)
    {
        if (!_options.IsConfigured() || string.IsNullOrWhiteSpace(request.Message))
        {
            return;
        }

        await EnsureIndexReadyAsync(cancellationToken);

        var searchUri = BuildUri($"/indexes/{_options.IndexName}/docs/search");
        using var searchRequest = await CreateAuthorizedRequestAsync(HttpMethod.Post, searchUri, cancellationToken);
        searchRequest.Content = new StringContent(
            JsonSerializer.Serialize(
                new
                {
                    search = request.Message,
                    top = Math.Clamp(_options.Top, 1, 5),
                    select = "title,source,category,content"
                },
                JsonOptions),
            Encoding.UTF8,
            "application/json");

        using var response = await _httpClient.SendAsync(searchRequest, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return;
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        var result = await JsonSerializer.DeserializeAsync<SearchResponse>(stream, JsonOptions, cancellationToken);
        var documents = result?.Value
            ?.Where(document => !string.IsNullOrWhiteSpace(document.Content))
            .Take(Math.Clamp(_options.Top, 1, 5))
            .ToList() ?? [];

        if (documents.Count == 0)
        {
            return;
        }

        request.Context ??= new AgentChatContextDto();
        request.Context.GroundingSummary = string.Join(
            Environment.NewLine,
            documents.Select((document, index) =>
                $"- [{index + 1}] {document.Title ?? "DeliveryBot knowledge"} ({document.Source ?? document.Category ?? "knowledge base"}): {document.Content}"));
    }

    private async Task EnsureIndexReadyAsync(CancellationToken cancellationToken)
    {
        if (IsIndexReady())
        {
            return;
        }

        await _indexLock.WaitAsync(cancellationToken);

        try
        {
            if (IsIndexReady())
            {
                return;
            }

            await CreateOrUpdateIndexAsync(cancellationToken);
            await SeedDocumentsAsync(cancellationToken);
            MarkIndexReady();
        }
        finally
        {
            _indexLock.Release();
        }
    }

    private async Task CreateOrUpdateIndexAsync(CancellationToken cancellationToken)
    {
        using var request = await CreateAuthorizedRequestAsync(
            HttpMethod.Put,
            BuildUri($"/indexes/{_options.IndexName}"),
            cancellationToken);
        request.Content = new StringContent(
            JsonSerializer.Serialize(
                new
                {
                    name = _options.IndexName,
                    fields = new object[]
                    {
                        new { name = "id", type = "Edm.String", key = true, filterable = true },
                        new { name = "title", type = "Edm.String", searchable = true },
                        new { name = "source", type = "Edm.String", searchable = true, filterable = true },
                        new { name = "category", type = "Edm.String", searchable = true, filterable = true },
                        new { name = "content", type = "Edm.String", searchable = true }
                    }
                },
                JsonOptions),
            Encoding.UTF8,
            "application/json");

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException(
                $"Azure AI Search returned HTTP {(int)response.StatusCode} while preparing the knowledge index: {body}");
        }
    }

    private async Task SeedDocumentsAsync(CancellationToken cancellationToken)
    {
        var seedPath = ResolveSeedPath();
        if (!File.Exists(seedPath))
        {
            return;
        }

        await using var seedStream = File.OpenRead(seedPath);
        var documents = await JsonSerializer.DeserializeAsync<List<SearchSeedDocument>>(
            seedStream,
            JsonOptions,
            cancellationToken) ?? [];

        if (documents.Count == 0)
        {
            return;
        }

        var uploadDocuments = documents
            .Where(document => !string.IsNullOrWhiteSpace(document.Id))
            .Select(document => document with { Action = "mergeOrUpload" })
            .ToList();

        if (uploadDocuments.Count == 0)
        {
            return;
        }

        using var request = await CreateAuthorizedRequestAsync(
            HttpMethod.Post,
            BuildUri($"/indexes/{_options.IndexName}/docs/index"),
            cancellationToken);
        request.Content = new StringContent(
            JsonSerializer.Serialize(new { value = uploadDocuments }, JsonOptions),
            Encoding.UTF8,
            "application/json");

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        if (response.StatusCode != HttpStatusCode.OK &&
            response.StatusCode != HttpStatusCode.Created &&
            response.StatusCode != HttpStatusCode.NoContent)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException(
                $"Azure AI Search returned HTTP {(int)response.StatusCode} while seeding knowledge documents: {body}");
        }
    }

    private string ResolveSeedPath()
    {
        if (Path.IsPathRooted(_options.SeedDocumentsPath))
        {
            return _options.SeedDocumentsPath;
        }

        return Path.GetFullPath(_options.SeedDocumentsPath, AppContext.BaseDirectory);
    }

    private bool IsIndexReady() => System.Threading.Volatile.Read(ref _indexReady);

    private void MarkIndexReady() => System.Threading.Volatile.Write(ref _indexReady, true);

    private Uri BuildUri(string path)
    {
        var endpoint = _options.Endpoint.TrimEnd('/');
        return new Uri($"{endpoint}{path}?api-version={ApiVersion}");
    }

    private async Task<HttpRequestMessage> CreateAuthorizedRequestAsync(
        HttpMethod method,
        Uri uri,
        CancellationToken cancellationToken)
    {
        var accessToken = await _credential.GetTokenAsync(SearchTokenRequest, cancellationToken);
        var request = new HttpRequestMessage(method, uri);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken.Token);
        return request;
    }

    private sealed class SearchResponse
    {
        public List<SearchDocument>? Value { get; set; }
    }

    private sealed class SearchDocument
    {
        public string? Title { get; set; }
        public string? Source { get; set; }
        public string? Category { get; set; }
        public string? Content { get; set; }
    }

    private sealed record SearchSeedDocument(
        string Id,
        string Title,
        string Source,
        string Category,
        string Content)
    {
        [JsonPropertyName("@search.action")]
        public string Action { get; init; } = "mergeOrUpload";
    }
}
