namespace SkillLedger.Infrastructure.Configuration;

public class AzureKeyVaultSettings
{
    public const string SectionName = "AzureKeyVault";

    public bool Enabled { get; set; } = false;
    public string? VaultUri { get; set; }
    public string? TenantId { get; set; }
    public string? ClientId { get; set; }
    public string? ClientSecret { get; set; }
    public bool UseManagedIdentity { get; set; } = true;
    public int CacheRefreshIntervalMinutes { get; set; } = 60;
}
