using SkillLedger.Core.Interfaces;
using System.Security.Cryptography;
using System.Text;

namespace SkillLedger.Tests.Mocks;

/// <summary>
/// Fast mock encryption service for tests - uses simple Base64 encoding instead of real encryption
/// This dramatically speeds up tests that don't actually need cryptographic security
/// </summary>
public class MockEncryptionService : IEncryptionService
{
    private const string PREFIX = "MOCK_ENC:";
    private const string SSN_PREFIX = "MOCK_SSN:";

    /// <summary>
    /// "Encrypt" using Base64 encoding (instant, no crypto overhead)
    /// </summary>
    public Task<string> EncryptAsync(string plainText)
    {
        if (string.IsNullOrEmpty(plainText))
            return Task.FromResult(plainText);

        var bytes = Encoding.UTF8.GetBytes(plainText);
        var encoded = Convert.ToBase64String(bytes);
        return Task.FromResult(PREFIX + encoded);
    }

    /// <summary>
    /// "Decrypt" by decoding Base64 (instant, no crypto overhead)
    /// </summary>
    public Task<string> DecryptAsync(string encryptedText)
    {
        if (string.IsNullOrEmpty(encryptedText))
            return Task.FromResult(encryptedText);

        if (!encryptedText.StartsWith(PREFIX))
            return Task.FromResult(encryptedText); // Already plain text

        var encoded = encryptedText.Substring(PREFIX.Length);
        var bytes = Convert.FromBase64String(encoded);
        var plainText = Encoding.UTF8.GetString(bytes);
        return Task.FromResult(plainText);
    }

    /// <summary>
    /// "Encrypt" SSN deterministically using simple hashing
    /// </summary>
    public Task<string> EncryptSsnAsync(string ssn)
    {
        if (string.IsNullOrEmpty(ssn))
            return Task.FromResult(ssn);

        // Simple deterministic hash for testing
        var hash = HashPii(ssn);
        return Task.FromResult(SSN_PREFIX + hash);
    }

    /// <summary>
    /// Hash PII using SHA256 (fast, but not cryptographically secure for production)
    /// </summary>
    public string HashPii(string data)
    {
        if (string.IsNullOrEmpty(data))
            return string.Empty;

        using var sha256 = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(data);
        var hashBytes = sha256.ComputeHash(bytes);
        return Convert.ToBase64String(hashBytes);
    }

    /// <summary>
    /// Generate secure random token (uses real crypto for security tests)
    /// </summary>
    public string GenerateSecureToken(int length = 32)
    {
        var bytes = new byte[length];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        return Convert.ToBase64String(bytes);
    }
}
