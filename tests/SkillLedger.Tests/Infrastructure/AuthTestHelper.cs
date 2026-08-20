using SkillLedger.Core.Entities;
using SkillLedger.Infrastructure.Data;
using System.Text;
using System.Text.Json;

namespace SkillLedger.Tests.Infrastructure;

/// <summary>
/// Optimized authentication helper for integration tests that caches tokens and minimizes auth overhead
/// </summary>
public static class AuthTestHelper
{
    private static readonly Dictionary<string, string> _tokenCache = new();
    private static readonly object _lock = new object();


    /// <summary>
    /// Gets or creates auth token using standard seeded users for faster test execution
    /// </summary>
    public static string GetStandardUserAuthToken(SkillLedgerDbContext context, int userIndex = 0)
    {
        var standardUsers = SimpleTestDataSeeder.GetStandardUsers(context);
        if (userIndex >= standardUsers.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(userIndex), $"Standard user index {userIndex} is out of range. Available users: 0-{standardUsers.Count - 1}");
        }

        return GetOrCreateAuthToken(standardUsers[userIndex]);
    }

    /// <summary>
    /// Adds authorization header to HttpClient for authenticated requests
    /// </summary>
    public static void AddAuthHeader(HttpClient client, string token)
    {
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
    }

    /// <summary>
    /// Adds authorization header using standard user token
    /// </summary>
    public static void AddStandardUserAuth(HttpClient client, SkillLedgerDbContext context, int userIndex = 0)
    {
        var token = GetStandardUserAuthToken(context, userIndex);
        AddAuthHeader(client, token);
    }

    /// <summary>
    /// Gets or creates a cached authentication token for the user (without roles)
    /// </summary>
    public static string GetOrCreateAuthToken(User user)
    {
        return GetOrCreateAuthToken(user, null);
    }

    /// <summary>
    /// Gets or creates a cached authentication token for the user with roles
    /// </summary>
    public static string GetOrCreateAuthToken(User user, string[]? roles)
    {
        var cacheKey = roles != null && roles.Length > 0
            ? $"{user.Id}_{user.Email}_{string.Join(",", roles)}"
            : $"{user.Id}_{user.Email}";

        lock (_lock)
        {
            if (_tokenCache.TryGetValue(cacheKey, out var cachedToken))
            {
                return cachedToken;
            }

            var token = CreateTestJwtToken(user, roles);
            _tokenCache[cacheKey] = token;
            return token;
        }
    }


    /// <summary>
    /// Creates a simple test JWT token without expensive cryptographic operations
    /// </summary>
    private static string CreateTestJwtToken(User user, string[]? roles = null)
    {
        // Create a minimal JWT structure for testing - the TestAuthenticationHandler will parse this
        var header = new { alg = "none", typ = "JWT" };
        var payload = new
        {
            sub = user.Id.ToString(),
            nameid = user.Id.ToString(),
            email = user.Email,
            jti = Guid.NewGuid().ToString(),
            iat = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            exp = DateTimeOffset.UtcNow.AddHours(1).ToUnixTimeSeconds(),
            role = roles
        };

        var headerBytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(header));
        var payloadBytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(payload));

        var headerBase64 = Convert.ToBase64String(headerBytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
        var payloadBase64 = Convert.ToBase64String(payloadBytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');

        // For test tokens, we don't need a real signature since TestAuthenticationHandler ignores it
        return $"{headerBase64}.{payloadBase64}.test-signature";
    }

    /// <summary>
    /// Clears the token cache - useful for tests that need fresh tokens
    /// </summary>
    public static void ClearTokenCache()
    {
        lock (_lock)
        {
            _tokenCache.Clear();
        }
    }

    /// <summary>
    /// Creates CSRF-protected HttpContent with auth header
    /// </summary>
    public static Task<HttpRequestMessage> CreateAuthenticatedRequest(HttpMethod method, string requestUri, SkillLedgerDbContext context, object? content = null, int userIndex = 0)
    {
        var request = new HttpRequestMessage(method, requestUri);

        // Add auth header
        var token = GetStandardUserAuthToken(context, userIndex);
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        // Add content if provided
        if (content != null)
        {
            var json = JsonSerializer.Serialize(content);
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");
        }

        return Task.FromResult(request);
    }
}