using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Azure.Security.KeyVault.Keys;
using Azure.Security.KeyVault.Keys.Cryptography;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SkillLedger.Core.Interfaces;
using SkillLedger.Infrastructure.Configuration;
using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using System.Linq;

namespace SkillLedger.Infrastructure.Services;

/// <summary>
/// Enhanced service for Azure Key Vault operations including encryption keys
/// </summary>
public class AzureKeyVaultService : IAzureKeyVaultService, IDisposable
{
    private readonly AzureKeyVaultConfiguration _config;
    private readonly EncryptionConfiguration _encryptionConfig;
    private readonly IMemoryCache _memoryCache;
    private readonly ILogger<AzureKeyVaultService> _logger;
    private readonly SecretClient? _secretClient;
    private readonly KeyClient? _keyClient;
    private readonly ConcurrentDictionary<string, CryptographyClient> _cryptoClients = new();

    public AzureKeyVaultService(
        IOptions<AzureKeyVaultConfiguration> config,
        IOptions<EncryptionConfiguration> encryptionConfig,
        IMemoryCache memoryCache,
        ILogger<AzureKeyVaultService> logger)
    {
        _config = config.Value;
        _encryptionConfig = encryptionConfig.Value;
        _memoryCache = memoryCache;
        _logger = logger;

        if (_config.Enabled && !string.IsNullOrEmpty(_config.VaultUri))
        {
            try
            {
                var credential = CreateAzureCredential();
                var vaultUri = new Uri(_config.VaultUri);

                _secretClient = new SecretClient(vaultUri, credential);
                _keyClient = new KeyClient(vaultUri, credential);

                _logger.LogInformation("Azure Key Vault clients initialized for vault: {VaultUri}", _config.VaultUri);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize Azure Key Vault clients");
                throw new InvalidOperationException("Azure Key Vault initialization failed", ex);
            }
        }
        else
        {
            _logger.LogWarning("Azure Key Vault is disabled or not configured");
        }
    }

    /// <summary>
    /// Get data encryption key for PII encryption
    /// </summary>
    public async Task<byte[]> GetDataEncryptionKeyAsync()
    {
        const string cacheKey = "data_encryption_key";

        if (_memoryCache.TryGetValue(cacheKey, out byte[]? cachedKey) && cachedKey != null)
        {
            return cachedKey;
        }

        if (!_config.Enabled || _keyClient == null)
        {
            // Generate a deterministic key for development/testing to ensure consistency across operations
            var tempKey = new byte[32];
            var seedString = "SkillLedger-Test-DEK-Seed-For-Consistent-Encryption";
            var seedBytes = System.Text.Encoding.UTF8.GetBytes(seedString);
            using var sha256 = System.Security.Cryptography.SHA256.Create();
            var hash = sha256.ComputeHash(seedBytes);
            Array.Copy(hash, tempKey, 32);

            // Cache the key to ensure consistency within the same instance
            _memoryCache.Set(cacheKey, tempKey, _encryptionConfig.KeyCacheDuration);
            return tempKey;
        }

        try
        {
            var key = await _keyClient.GetKeyAsync(_encryptionConfig.MasterKeyName);
            var cryptoClient = _keyClient.GetCryptographyClient(key.Value.Name, key.Value.Properties.Version);

            // For AES keys, we need to derive a key from Key Vault
            // This is a simplified approach - in production, use proper key derivation
            var keyMaterial = Encoding.UTF8.GetBytes($"{_encryptionConfig.MasterKeyName}-data-{DateTime.UtcNow:yyyy-MM}");
            var hashedKey = SHA256.HashData(keyMaterial);

            _memoryCache.Set(cacheKey, hashedKey, _encryptionConfig.KeyCacheDuration);
            return hashedKey;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get data encryption key from Key Vault");
            throw new InvalidOperationException("Failed to retrieve data encryption key", ex);
        }
    }

    /// <summary>
    /// Get SSN/TIN encryption key for deterministic encryption
    /// </summary>
    public async Task<byte[]> GetSsnEncryptionKeyAsync()
    {
        const string cacheKey = "ssn_encryption_key";

        if (_memoryCache.TryGetValue(cacheKey, out byte[]? cachedKey) && cachedKey != null)
        {
            return cachedKey;
        }

        if (!_config.Enabled || _keyClient == null)
        {
            // BUG FIX KV-001: Generate a DETERMINISTIC key for development/testing
            // SSN encryption requires the same key every time to decrypt previously encrypted data
            // Random keys would make all previously encrypted SSNs unreadable after service restart
            var tempKey = new byte[32];
            var seedString = "SkillLedger-Test-SSN-Seed-For-Deterministic-Encryption";
            var seedBytes = System.Text.Encoding.UTF8.GetBytes(seedString);
            using var sha256 = System.Security.Cryptography.SHA256.Create();
            var hash = sha256.ComputeHash(seedBytes);
            Array.Copy(hash, tempKey, 32);

            // Cache the key to ensure consistency within the same instance
            _memoryCache.Set(cacheKey, tempKey, _encryptionConfig.KeyCacheDuration);
            return tempKey;
        }

        try
        {
            var key = await _keyClient.GetKeyAsync(_encryptionConfig.SsnEncryptionKeyName);

            // Derive SSN encryption key
            var keyMaterial = Encoding.UTF8.GetBytes($"{_encryptionConfig.SsnEncryptionKeyName}-ssn-{DateTime.UtcNow:yyyy-MM}");
            var hashedKey = SHA256.HashData(keyMaterial);

            _memoryCache.Set(cacheKey, hashedKey, _encryptionConfig.KeyCacheDuration);
            return hashedKey;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get SSN encryption key from Key Vault");
            throw new InvalidOperationException("Failed to retrieve SSN encryption key", ex);
        }
    }

    /// <summary>
    /// Encrypt data using Azure Key Vault managed key
    /// </summary>
    public async Task<string> EncryptAsync(string plainText, string keyName)
    {
        if (!_config.Enabled || _keyClient == null)
        {
            throw new InvalidOperationException("Azure Key Vault not configured");
        }

        try
        {
            // Thread-safe client retrieval or creation
            if (!_cryptoClients.TryGetValue(keyName, out var cryptoClient))
            {
                var key = await _keyClient.GetKeyAsync(keyName);
                cryptoClient = _cryptoClients.GetOrAdd(
                    keyName,
                    _keyClient.GetCryptographyClient(key.Value.Name, key.Value.Properties.Version)
                );
            }

            var plainBytes = Encoding.UTF8.GetBytes(plainText);
            var encryptResult = await cryptoClient.EncryptAsync(EncryptionAlgorithm.RsaOaep, plainBytes);

            return Convert.ToBase64String(encryptResult.Ciphertext);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to encrypt data with key {KeyName}", keyName);
            throw new InvalidOperationException($"Encryption failed for key {keyName}", ex);
        }
    }

    /// <summary>
    /// Decrypt data using Azure Key Vault managed key
    /// </summary>
    public async Task<string> DecryptAsync(string encryptedData, string keyName)
    {
        if (!_config.Enabled || _keyClient == null)
        {
            throw new InvalidOperationException("Azure Key Vault not configured");
        }

        try
        {
            // Thread-safe client retrieval or creation
            if (!_cryptoClients.TryGetValue(keyName, out var cryptoClient))
            {
                var key = await _keyClient.GetKeyAsync(keyName);
                cryptoClient = _cryptoClients.GetOrAdd(
                    keyName,
                    _keyClient.GetCryptographyClient(key.Value.Name, key.Value.Properties.Version)
                );
            }

            var cipherBytes = Convert.FromBase64String(encryptedData);
            var decryptResult = await cryptoClient.DecryptAsync(EncryptionAlgorithm.RsaOaep, cipherBytes);

            return Encoding.UTF8.GetString(decryptResult.Plaintext);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to decrypt data with key {KeyName}", keyName);
            throw new InvalidOperationException($"Decryption failed for key {keyName}", ex);
        }
    }

    /// <summary>
    /// Create a new key in Azure Key Vault
    /// </summary>
    public async Task<bool> CreateKeyAsync(string keyName, string keyType, int keySize = 256)
    {
        if (!_config.Enabled || _keyClient == null)
        {
            _logger.LogWarning("Azure Key Vault not configured, cannot create key {KeyName}", keyName);
            return false;
        }

        try
        {
            KeyVaultKey key;

            if (keyType.ToUpperInvariant() == "RSA")
            {
                var options = new CreateRsaKeyOptions(keyName)
                {
                    KeySize = keySize
                };
                options.Tags.Add("created-by", "skill-ledger");
                options.Tags.Add("created-at", DateTime.UtcNow.ToString("O"));
                options.Tags.Add("key-purpose", "encryption");

                key = await _keyClient.CreateRsaKeyAsync(options);
            }
            else if (keyType.ToUpperInvariant() == "AES")
            {
                var options = new CreateKeyOptions();
                options.Tags.Add("created-by", "skill-ledger");
                options.Tags.Add("created-at", DateTime.UtcNow.ToString("O"));
                options.Tags.Add("key-purpose", "encryption");

                key = await _keyClient.CreateKeyAsync(keyName, KeyType.Oct, options);
            }
            else
            {
                throw new ArgumentException($"Unsupported key type: {keyType}");
            }

            _logger.LogInformation("Successfully created key {KeyName} of type {KeyType}", keyName, keyType);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create key {KeyName} of type {KeyType}", keyName, keyType);
            return false;
        }
    }

    /// <summary>
    /// Rotate an encryption key
    /// </summary>
    public async Task<bool> RotateKeyAsync(string keyName)
    {
        if (!_config.Enabled || _keyClient == null)
        {
            return false;
        }

        try
        {
            // Create a new version of the existing key
            var existingKey = await _keyClient.GetKeyAsync(keyName);
            var rotateOperation = await _keyClient.RotateKeyAsync(keyName);

            // Clear any cached crypto clients for this key
            _cryptoClients.TryRemove(keyName, out _);

            _logger.LogInformation("Successfully rotated key {KeyName}", keyName);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to rotate key {KeyName}", keyName);
            return false;
        }
    }

    /// <summary>
    /// Retrieve JWT RSA key pair from Azure Key Vault
    /// </summary>
    /// <returns>Tuple containing (privateKey, publicKey) in PEM format</returns>
    public async Task<(string privateKey, string publicKey)> GetJwtKeysAsync()
    {
        if (!_config.Enabled || _secretClient == null)
        {
            _logger.LogWarning("Azure Key Vault is disabled, returning empty keys");
            return (string.Empty, string.Empty);
        }

        try
        {
            var cacheKey = "jwt_rsa_keys";

            // Check cache first
            if (_memoryCache.TryGetValue(cacheKey, out (string, string) cachedKeys))
            {
                _logger.LogDebug("Retrieved JWT keys from cache");
                return cachedKeys;
            }

            _logger.LogInformation("Retrieving JWT keys from Azure Key Vault");

            // Retrieve keys from Key Vault
            var privateKeyTask = _secretClient.GetSecretAsync(_config.JwtPrivateKeyName);
            var publicKeyTask = _secretClient.GetSecretAsync(_config.JwtPublicKeyName);

            await Task.WhenAll(privateKeyTask, publicKeyTask);

            var privateKeyResponse = await privateKeyTask;
            var publicKeyResponse = await publicKeyTask;

            var privateKey = privateKeyResponse.Value.Value;
            var publicKey = publicKeyResponse.Value.Value;

            // Validate keys
            if (!IsValidRsaKey(privateKey, true) || !IsValidRsaKey(publicKey, false))
            {
                throw new InvalidOperationException("Retrieved keys are not valid RSA keys");
            }

            // Cache the keys
            var cacheOptions = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(_config.KeyCacheDurationMinutes),
                Priority = CacheItemPriority.High
            };

            _memoryCache.Set(cacheKey, (privateKey, publicKey), cacheOptions);

            _logger.LogInformation("Successfully retrieved and cached JWT keys from Azure Key Vault");
            return (privateKey, publicKey);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve JWT keys from Azure Key Vault");
            throw new InvalidOperationException("Failed to retrieve JWT keys from Azure Key Vault", ex);
        }
    }

    /// <summary>
    /// Generate and store new RSA key pair in Azure Key Vault
    /// This method should only be used during initial setup or key rotation
    /// </summary>
    /// <param name="keySize">RSA key size in bits (recommended: 2048 or 4096)</param>
    /// <returns>True if keys were successfully generated and stored</returns>
    public async Task<bool> GenerateAndStoreJwtKeysAsync(int keySize = 2048)
    {
        if (!_config.Enabled || _secretClient == null)
        {
            _logger.LogWarning("Azure Key Vault is disabled, cannot generate keys");
            return false;
        }

        try
        {
            _logger.LogInformation("Generating new RSA key pair with size: {KeySize}", keySize);

            using var rsa = RSA.Create(keySize);

            // Export keys in PEM format
            var privateKeyPem = rsa.ExportRSAPrivateKeyPem();
            var publicKeyPem = rsa.ExportRSAPublicKeyPem();

            // Store in Azure Key Vault
            var privateKeySecret = new KeyVaultSecret(_config.JwtPrivateKeyName, privateKeyPem);
            var publicKeySecret = new KeyVaultSecret(_config.JwtPublicKeyName, publicKeyPem);

            // Add metadata
            privateKeySecret.Properties.Tags.Add("purpose", "jwt-signing");
            privateKeySecret.Properties.Tags.Add("key-size", keySize.ToString());
            privateKeySecret.Properties.Tags.Add("generated-at", DateTime.UtcNow.ToString("O"));

            publicKeySecret.Properties.Tags.Add("purpose", "jwt-verification");
            publicKeySecret.Properties.Tags.Add("key-size", keySize.ToString());
            publicKeySecret.Properties.Tags.Add("generated-at", DateTime.UtcNow.ToString("O"));

            await Task.WhenAll(
                _secretClient.SetSecretAsync(privateKeySecret),
                _secretClient.SetSecretAsync(publicKeySecret)
            );

            // Clear cache to force reload of new keys
            _memoryCache.Remove("jwt_rsa_keys");

            _logger.LogInformation("Successfully generated and stored new JWT RSA key pair in Azure Key Vault");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate and store JWT keys in Azure Key Vault");
            return false;
        }
    }

    /// <summary>
    /// Check if Azure Key Vault connection is healthy
    /// </summary>
    /// <returns>True if Key Vault is accessible</returns>
    public async Task<bool> IsHealthyAsync()
    {
        if (!_config.Enabled || _secretClient == null)
        {
            return false;
        }

        try
        {
            // Try to get vault properties (minimal operation to test connectivity)
            await foreach (var page in _secretClient.GetPropertiesOfSecretsAsync().AsPages())
            {
                // Successfully got first page, connection is working
                break;
            }
            return true;
        }
        catch
        {
            return false;
        }
    }

    private DefaultAzureCredential CreateAzureCredential()
    {
        var options = new DefaultAzureCredentialOptions();

        if (_config.UseManagedIdentity)
        {
            _logger.LogInformation("Using managed identity for Azure Key Vault authentication");
            return new DefaultAzureCredential(options);
        }

        if (!string.IsNullOrEmpty(_config.TenantId) &&
            !string.IsNullOrEmpty(_config.ClientId) &&
            !string.IsNullOrEmpty(_config.ClientSecret))
        {
            _logger.LogInformation("Using service principal for Azure Key Vault authentication");
            options.TenantId = _config.TenantId;
            return new DefaultAzureCredential(options);
        }

        _logger.LogInformation("Using default Azure credential chain for Key Vault authentication");
        return new DefaultAzureCredential(options);
    }

    private bool IsValidRsaKey(string keyPem, bool isPrivateKey)
    {
        try
        {
            using var rsa = RSA.Create();

            if (isPrivateKey)
            {
                rsa.ImportFromPem(keyPem);
                // Verify we can export both private and public key
                _ = rsa.ExportRSAPrivateKeyPem();
                _ = rsa.ExportRSAPublicKeyPem();
            }
            else
            {
                rsa.ImportFromPem(keyPem);
                // Verify we can export the public key
                _ = rsa.ExportRSAPublicKeyPem();
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Key validation failed for {KeyType} key", isPrivateKey ? "private" : "public");
            return false;
        }
    }

    public void Dispose()
    {
        // SecretClient doesn't implement IDisposable, but we keep this for future extensibility
        GC.SuppressFinalize(this);
    }
}