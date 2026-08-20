namespace SkillLedger.Core.Interfaces;

/// <summary>
/// Interface for Azure Key Vault operations
/// </summary>
public interface IAzureKeyVaultService
{
    /// <summary>
    /// Get data encryption key for PII encryption
    /// </summary>
    /// <returns>256-bit encryption key</returns>
    Task<byte[]> GetDataEncryptionKeyAsync();

    /// <summary>
    /// Get SSN/TIN encryption key for deterministic encryption
    /// </summary>
    /// <returns>256-bit encryption key</returns>
    Task<byte[]> GetSsnEncryptionKeyAsync();

    /// <summary>
    /// Get JWT signing keys (private and public)
    /// </summary>
    /// <returns>Tuple containing (privateKey, publicKey) in PEM format</returns>
    Task<(string privateKey, string publicKey)> GetJwtKeysAsync();

    /// <summary>
    /// Encrypt data using Azure Key Vault key
    /// </summary>
    /// <param name="plainText">Data to encrypt</param>
    /// <param name="keyName">Name of key in Key Vault</param>
    /// <returns>Encrypted data</returns>
    Task<string> EncryptAsync(string plainText, string keyName);

    /// <summary>
    /// Decrypt data using Azure Key Vault key
    /// </summary>
    /// <param name="encryptedData">Data to decrypt</param>
    /// <param name="keyName">Name of key in Key Vault</param>
    /// <returns>Decrypted data</returns>
    Task<string> DecryptAsync(string encryptedData, string keyName);

    /// <summary>
    /// Generate and store new encryption keys
    /// </summary>
    /// <param name="keyName">Name of key to create</param>
    /// <param name="keyType">Type of key (RSA, AES, etc.)</param>
    /// <param name="keySize">Key size in bits</param>
    /// <returns>Success indicator</returns>
    Task<bool> CreateKeyAsync(string keyName, string keyType, int keySize = 256);

    /// <summary>
    /// Check if Key Vault is accessible
    /// </summary>
    /// <returns>True if healthy</returns>
    Task<bool> IsHealthyAsync();

    /// <summary>
    /// Rotate encryption keys
    /// </summary>
    /// <param name="keyName">Name of key to rotate</param>
    /// <returns>Success indicator</returns>
    Task<bool> RotateKeyAsync(string keyName);
}