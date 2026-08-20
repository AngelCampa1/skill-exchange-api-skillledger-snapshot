using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Azure.Extensions.AspNetCore.Configuration.Secrets;

namespace SkillLedger.Infrastructure.Configuration;

public class AzureKeyVaultConfiguration
{
    private readonly ILogger<AzureKeyVaultConfiguration>? _logger;
    private readonly IHostEnvironment? _environment;

    // Parameterless constructor for Options pattern
    public AzureKeyVaultConfiguration()
    {
    }

    public AzureKeyVaultConfiguration(
        ILogger<AzureKeyVaultConfiguration> logger,
        IHostEnvironment environment)
    {
        _logger = logger;
        _environment = environment;
    }

    // Configuration properties
    public bool Enabled { get; set; } = false;
    public string? VaultUri { get; set; }
    public string? TenantId { get; set; }
    public string? ClientId { get; set; }
    public string? ClientSecret { get; set; }
    public bool UseManagedIdentity { get; set; } = true;
    public int CacheRefreshIntervalMinutes { get; set; } = 60;
    public int KeyCacheDurationMinutes { get; set; } = 60;
    public string JwtPrivateKeyName { get; set; } = "jwt-private-key";
    public string JwtPublicKeyName { get; set; } = "jwt-public-key";

    public IConfigurationBuilder AddAzureKeyVault(IConfigurationBuilder builder)
    {
        var keyVaultEndpoint = Environment.GetEnvironmentVariable("AZURE_KEY_VAULT_ENDPOINT");

        if (string.IsNullOrEmpty(keyVaultEndpoint))
        {
            _logger?.LogWarning("Azure Key Vault endpoint not found. Skipping Key Vault integration.");
            return builder;
        }

        try
        {
            if (_environment?.IsProduction() == true)
            {
                // Use managed identity in production
                builder.AddAzureKeyVault(new Uri(keyVaultEndpoint), new DefaultAzureCredential());
                _logger?.LogInformation("Azure Key Vault configured with managed identity");
            }
            else
            {
                // Use service principal for development
                var clientId = Environment.GetEnvironmentVariable("AZURE_CLIENT_ID");
                var clientSecret = Environment.GetEnvironmentVariable("AZURE_CLIENT_SECRET");
                var tenantId = Environment.GetEnvironmentVariable("AZURE_TENANT_ID");

                if (!string.IsNullOrEmpty(clientId) && !string.IsNullOrEmpty(clientSecret) && !string.IsNullOrEmpty(tenantId))
                {
                    var credential = new ClientSecretCredential(tenantId, clientId, clientSecret);
                    builder.AddAzureKeyVault(new Uri(keyVaultEndpoint), credential);
                    _logger?.LogInformation("Azure Key Vault configured with service principal");
                }
                else
                {
                    _logger?.LogWarning("Azure Key Vault credentials not found. Skipping Key Vault integration.");
                }
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to configure Azure Key Vault");
            throw;
        }

        return builder;
    }

    public static async Task<IDictionary<string, string>> GetSecretsAsync(string keyVaultEndpoint, ILogger? logger)
    {
        var secrets = new Dictionary<string, string>();

        try
        {
            var client = new SecretClient(new Uri(keyVaultEndpoint), new DefaultAzureCredential());
            await foreach (var secret in client.GetPropertiesOfSecretsAsync())
            {
                try
                {
                    var secretValue = await client.GetSecretAsync(secret.Name);
                    secrets[secret.Name] = secretValue.Value.Value;
                }
                catch (Exception ex)
                {
                    logger?.LogWarning(ex, "Failed to retrieve secret: {SecretName}", secret.Name);
                }
            }

            logger?.LogInformation("Retrieved {Count} secrets from Azure Key Vault", secrets.Count);
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Failed to retrieve secrets from Azure Key Vault");
            throw;
        }

        return secrets;
    }

    public static async Task<string> GetSecretAsync(string keyVaultEndpoint, string secretName, ILogger? logger)
    {
        try
        {
            var client = new SecretClient(new Uri(keyVaultEndpoint), new DefaultAzureCredential());
            var secret = await client.GetSecretAsync(secretName);
            return secret.Value.Value;
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Failed to retrieve secret: {SecretName}", secretName);
            throw;
        }
    }

    public static async Task SetSecretAsync(string keyVaultEndpoint, string secretName, string secretValue, ILogger? logger)
    {
        try
        {
            var client = new SecretClient(new Uri(keyVaultEndpoint), new DefaultAzureCredential());
            await client.SetSecretAsync(secretName, secretValue);
            logger?.LogInformation("Successfully set secret: {SecretName}", secretName);
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Failed to set secret: {SecretName}", secretName);
            throw;
        }
    }
}

public static class ConfigurationExtensions
{
    public static IConfigurationBuilder AddAzureKeyVaultWithFallback(this IConfigurationBuilder builder, ILogger<AzureKeyVaultConfiguration> logger, IHostEnvironment environment)
    {
        var keyVaultConfig = new AzureKeyVaultConfiguration(logger, environment);
        return keyVaultConfig.AddAzureKeyVault(builder);
    }
}
