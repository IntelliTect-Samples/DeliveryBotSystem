using System.Net.Http.Headers;
using System.Text.Json;
using Azure.Core;
using Azure.Identity;
using AgentService.Options;
using Microsoft.Extensions.Options;

namespace AgentService.Services;

public sealed class AzureOpenAiApiKeyProvider : IAzureOpenAiApiKeyProvider
{
    private static readonly TokenRequestContext KeyVaultTokenRequest =
        new(["https://vault.azure.net/.default"]);

    private readonly AzureOpenAiOptions _azureOpenAiOptions;
    private readonly KeyVaultOptions _keyVaultOptions;
    private readonly DefaultAzureCredential _credential = new();
    private readonly HttpClient _httpClient = new();
    private readonly SemaphoreSlim _loadLock = new(1, 1);
    private string? _cachedApiKey;

    public AzureOpenAiApiKeyProvider(
        IOptions<AzureOpenAiOptions> azureOpenAiOptions,
        IOptions<KeyVaultOptions> keyVaultOptions)
    {
        _azureOpenAiOptions = azureOpenAiOptions.Value;
        _keyVaultOptions = keyVaultOptions.Value;
    }

    public async Task<string?> GetApiKeyAsync(CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrWhiteSpace(_azureOpenAiOptions.ApiKey))
        {
            return _azureOpenAiOptions.ApiKey;
        }

        if (!string.IsNullOrWhiteSpace(_cachedApiKey))
        {
            return _cachedApiKey;
        }

        if (!Uri.TryCreate(_keyVaultOptions.VaultUri, UriKind.Absolute, out var vaultUri) ||
            string.IsNullOrWhiteSpace(_azureOpenAiOptions.ApiKeySecretName))
        {
            return null;
        }

        await _loadLock.WaitAsync(cancellationToken);

        try
        {
            if (!string.IsNullOrWhiteSpace(_cachedApiKey))
            {
                return _cachedApiKey;
            }

            try
            {
                var accessToken = await _credential.GetTokenAsync(
                    KeyVaultTokenRequest,
                    cancellationToken);
                using var request = new HttpRequestMessage(
                    HttpMethod.Get,
                    new Uri(
                        $"{vaultUri.ToString().TrimEnd('/')}/secrets/{Uri.EscapeDataString(_azureOpenAiOptions.ApiKeySecretName)}?api-version=7.5"));
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken.Token);

                using var response = await _httpClient.SendAsync(request, cancellationToken);
                var body = await response.Content.ReadAsStringAsync(cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    throw new InvalidOperationException(
                        $"Key Vault returned HTTP {(int)response.StatusCode}: {body}");
                }

                using var document = JsonDocument.Parse(body);
                _cachedApiKey = document.RootElement.GetProperty("value").GetString();

                if (string.IsNullOrWhiteSpace(_cachedApiKey))
                {
                    throw new InvalidOperationException(
                        $"Key Vault secret '{_azureOpenAiOptions.ApiKeySecretName}' did not contain a value.");
                }

                return _cachedApiKey;
            }
            catch (Exception error)
            {
                throw new InvalidOperationException(
                    $"Azure OpenAI API key could not be loaded from Key Vault secret '{_azureOpenAiOptions.ApiKeySecretName}'. {error.Message}",
                    error);
            }
        }
        finally
        {
            _loadLock.Release();
        }
    }
}
