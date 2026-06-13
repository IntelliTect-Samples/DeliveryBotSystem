namespace AgentService.Services;

public interface IAzureOpenAiApiKeyProvider
{
    Task<string?> GetApiKeyAsync(CancellationToken cancellationToken = default);
}
