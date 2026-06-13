namespace AgentService.Options;

public sealed class AzureAiSearchOptions
{
    public const string SectionName = "Search";

    public bool Enabled { get; set; }
    public string Endpoint { get; set; } = "";
    public string IndexName { get; set; } = "delivery-agent-knowledge";
    public int Top { get; set; } = 3;
    public string SeedDocumentsPath { get; set; } = "KnowledgeBase/search-documents.json";

    public bool IsConfigured() =>
        Enabled &&
        !string.IsNullOrWhiteSpace(Endpoint) &&
        !string.IsNullOrWhiteSpace(IndexName);
}
