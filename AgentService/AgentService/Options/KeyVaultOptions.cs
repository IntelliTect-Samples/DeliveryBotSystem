namespace AgentService.Options;

public sealed class KeyVaultOptions
{
    public const string SectionName = "KeyVault";

    public string VaultUri { get; set; } = "";
}
