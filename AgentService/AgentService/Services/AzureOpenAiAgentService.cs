using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using AgentService.DTOs;
using AgentService.Options;
using Microsoft.Extensions.Options;

namespace AgentService.Services;

public sealed class AzureOpenAiAgentService : IAgentService
{
    private readonly HttpClient _httpClient;
    private readonly AzureOpenAiOptions _options;

    public AzureOpenAiAgentService(HttpClient httpClient, IOptions<AzureOpenAiOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
    }

    public async Task<AgentChatResponseDto> ChatAsync(
        AgentChatRequestDto request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Message))
        {
            throw new ArgumentException("Message is required.", nameof(request));
        }

        if (string.IsNullOrWhiteSpace(_options.Endpoint) ||
            string.IsNullOrWhiteSpace(_options.Deployment) ||
            string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            throw new InvalidOperationException(
                "Azure OpenAI is not configured. Set AzureOpenAI:Endpoint, AzureOpenAI:Deployment, and AzureOpenAI:ApiKey.");
        }

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, BuildRequestUri());
        httpRequest.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        httpRequest.Headers.Add("api-key", _options.ApiKey);
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
}
