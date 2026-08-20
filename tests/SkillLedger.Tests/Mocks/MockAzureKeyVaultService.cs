using SkillLedger.Core.Interfaces;
using System.Security.Cryptography;

namespace SkillLedger.Tests.Mocks;

/// <summary>
/// Mock Azure Key Vault service for testing encryption without real Azure connection
/// </summary>
public class MockAzureKeyVaultService : IAzureKeyVaultService
{
    private readonly byte[] _dataEncryptionKey;
    private readonly byte[] _ssnEncryptionKey;
    private readonly Dictionary<string, byte[]> _keys = new();
    private readonly List<string> _operationLog = new();

    public MockAzureKeyVaultService()
    {
        // Generate deterministic keys for testing
        _dataEncryptionKey = DeriveKey("mock-data-encryption-key", 32);
        _ssnEncryptionKey = DeriveKey("mock-ssn-encryption-key", 32);

        _keys["skill-ledger-master-key"] = _dataEncryptionKey;
        _keys["skill-ledger-ssn-key"] = _ssnEncryptionKey;
    }

    public List<string> OperationLog => _operationLog;

    public Task<byte[]> GetDataEncryptionKeyAsync()
    {
        _operationLog.Add("GetDataEncryptionKey");
        return Task.FromResult(_dataEncryptionKey);
    }

    public Task<byte[]> GetSsnEncryptionKeyAsync()
    {
        _operationLog.Add("GetSsnEncryptionKey");
        return Task.FromResult(_ssnEncryptionKey);
    }

    public Task<(string privateKey, string publicKey)> GetJwtKeysAsync()
    {
        _operationLog.Add("GetJwtKeys");
        // Mock JWT keys in PEM format
        const string mockPrivateKey = @"-----BEGIN PRIVATE KEY-----
MIIEvQIBADANBgkqhkiG9w0BAQEFAASCBKcwggSjAgEAAoIBAQC7VJTUt9Us8cKj
MzEfYyjiWA4R4/M2bS1+fWIcPm15A8vIcTGVyVQmOQy9VjXOQqJpXXDJmNvjLtmN
-----END PRIVATE KEY-----";
        const string mockPublicKey = @"-----BEGIN PUBLIC KEY-----
MIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEAu1SU1LfVLPHCozMxH2Mo
4lgOEePzNm0tfn1iHD5teQPLyHExlclUJjkMvVY1zkKiaV1wyZjb4y7ZjQ==
-----END PUBLIC KEY-----";

        return Task.FromResult((mockPrivateKey, mockPublicKey));
    }

    public Task<string> EncryptAsync(string plainText, string keyName)
    {
        _operationLog.Add($"Encrypt:{keyName}");

        if (!_keys.TryGetValue(keyName, out var key))
            throw new InvalidOperationException($"Key not found: {keyName}");

        // Simple encryption for mock
        var plainBytes = System.Text.Encoding.UTF8.GetBytes(plainText);
        var encrypted = XorEncrypt(plainBytes, key);
        return Task.FromResult(Convert.ToBase64String(encrypted));
    }

    public Task<string> DecryptAsync(string encryptedData, string keyName)
    {
        _operationLog.Add($"Decrypt:{keyName}");

        if (!_keys.TryGetValue(keyName, out var key))
            throw new InvalidOperationException($"Key not found: {keyName}");

        // Simple decryption for mock (XOR is symmetric)
        var encryptedBytes = Convert.FromBase64String(encryptedData);
        var decrypted = XorEncrypt(encryptedBytes, key);
        return Task.FromResult(System.Text.Encoding.UTF8.GetString(decrypted));
    }

    public Task<bool> CreateKeyAsync(string keyName, string keyType, int keySize = 256)
    {
        _operationLog.Add($"CreateKey:{keyName}:{keyType}:{keySize}");

        var key = DeriveKey(keyName, keySize / 8);
        _keys[keyName] = key;

        return Task.FromResult(true);
    }

    public Task<bool> IsHealthyAsync()
    {
        _operationLog.Add("IsHealthy");
        return Task.FromResult(true);
    }

    public Task<bool> RotateKeyAsync(string keyName)
    {
        _operationLog.Add($"RotateKey:{keyName}");

        if (_keys.ContainsKey(keyName))
        {
            // Generate new key
            var newKey = DeriveKey($"{keyName}-rotated-{DateTime.UtcNow.Ticks}", 32);
            _keys[keyName] = newKey;
            return Task.FromResult(true);
        }

        return Task.FromResult(false);
    }

    public void ClearOperationLog()
    {
        _operationLog.Clear();
    }

    private static byte[] DeriveKey(string seed, int length)
    {
        using var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(seed));

        if (length <= hash.Length)
            return hash[..length];

        // For larger keys, concatenate multiple hashes
        var result = new byte[length];
        var offset = 0;
        var iteration = 0;

        while (offset < length)
        {
            var iterationHash = sha256.ComputeHash(
                System.Text.Encoding.UTF8.GetBytes($"{seed}-{iteration}"));
            var copyLength = Math.Min(iterationHash.Length, length - offset);
            Array.Copy(iterationHash, 0, result, offset, copyLength);
            offset += copyLength;
            iteration++;
        }

        return result;
    }

    private static byte[] XorEncrypt(byte[] data, byte[] key)
    {
        var result = new byte[data.Length];
        for (int i = 0; i < data.Length; i++)
        {
            result[i] = (byte)(data[i] ^ key[i % key.Length]);
        }
        return result;
    }
}
