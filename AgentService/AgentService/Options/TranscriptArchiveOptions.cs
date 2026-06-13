namespace AgentService.Options;

public sealed class TranscriptArchiveOptions
{
    public const string SectionName = "TranscriptArchive";

    public bool Enabled { get; set; }
    public string BlobServiceUri { get; set; } = "";
    public string ContainerName { get; set; } = "";

    public bool IsConfigured()
    {
        if (!Enabled || string.IsNullOrWhiteSpace(ContainerName))
        {
            return false;
        }

        return !string.IsNullOrWhiteSpace(BlobServiceUri);
    }
}
