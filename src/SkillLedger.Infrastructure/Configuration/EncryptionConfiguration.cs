namespace SkillLedger.Infrastructure.Configuration;

/// <summary>
/// Configuration for encryption services
/// </summary>
public class EncryptionConfiguration
{
    // Parameterless constructor for Options pattern
    public EncryptionConfiguration()
    {
    }
    /// <summary>
    /// Configuration section name
    /// </summary>
    public const string SectionName = "Encryption";

    /// <summary>
    /// Azure Key Vault endpoint
    /// </summary>
    public Uri KeyVaultEndpoint { get; set; } = new Uri("https://example-keyvault.vault.azure.net/");

    /// <summary>
    /// Name of the master key for data encryption
    /// </summary>
    public string MasterKeyName { get; set; } = "skill-ledger-master-key";

    /// <summary>
    /// Name of the key for SSN/TIN deterministic encryption
    /// </summary>
    public string SsnEncryptionKeyName { get; set; } = "skill-ledger-ssn-key";

    /// <summary>
    /// Name of the key for JWT signing
    /// </summary>
    public string JwtSigningKeyName { get; set; } = "skill-ledger-jwt-key";

    /// <summary>
    /// Key size in bits for AES encryption
    /// </summary>
    public int KeySizeInBits { get; set; } = 256;

    /// <summary>
    /// Whether to use hardware security modules (HSM)
    /// </summary>
    public bool UseHsm { get; set; } = true;

    /// <summary>
    /// Cache duration for encryption keys in memory
    /// </summary>
    public TimeSpan KeyCacheDuration { get; set; } = TimeSpan.FromHours(1);

    /// <summary>
    /// Maximum number of encryption operations before key rotation
    /// </summary>
    public long MaxOperationsBeforeRotation { get; set; } = 1_000_000;

    /// <summary>
    /// Enable automatic key rotation
    /// </summary>
    public bool EnableKeyRotation { get; set; } = true;

    /// <summary>
    /// Key rotation schedule (cron expression)
    /// </summary>
    public string KeyRotationSchedule { get; set; } = "0 0 2 * * 0"; // Every Sunday at 2 AM
}