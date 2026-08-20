using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Text.Json;
using SkillLedger.Core.DTOs;
using SkillLedger.Core.Interfaces;

namespace SkillLedger.Infrastructure.Services;

/// <summary>
/// Service for integrating with external professional networks and platforms
/// </summary>
public class ExternalIntegrationService : IExternalIntegrationService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ExternalIntegrationService> _logger;
    private readonly ConcurrentDictionary<string, DateTime> _rateLimitCache = new();

    public ExternalIntegrationService(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<ExternalIntegrationService> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
    }

    public Task<ExternalVerificationResult> VerifyLinkedInProfileAsync(string linkedInUrl)
    {
        try
        {
            if (!IsIntegrationEnabled("LinkedIn"))
            {
                return Task.FromResult(CreateFailedResult("LinkedIn", "LinkedIn integration is disabled"));
            }

            if (!IsValidLinkedInUrl(linkedInUrl))
            {
                return Task.FromResult(CreateFailedResult("LinkedIn", "Invalid LinkedIn URL format"));
            }

            // Check rate limiting
            if (IsRateLimited("LinkedIn"))
            {
                return Task.FromResult(CreateFailedResult("LinkedIn", "Rate limit exceeded for LinkedIn API"));
            }

            // For now, return a simulated successful verification
            // In production, this would connect to LinkedIn API
            var result = new ExternalVerificationResult
            {
                Platform = "LinkedIn",
                IsVerified = true,
                ConfidenceScore = 0.85m,
                VerifiedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddHours(GetCacheDurationHours()),
                ProfessionalData = new Dictionary<string, object>
                {
                    ["profileUrl"] = linkedInUrl,
                    ["verified"] = true,
                    ["profileCompleteness"] = 85,
                    ["connectionCount"] = 500,
                    ["endorsementCount"] = 25
                },
                ProfessionalScore = 85,
                ExperienceLevel = "Senior",
                ExtractedSkills = new List<string> { "Project Management", "Software Development", "Leadership" },
                Industry = "Technology",
                YearsOfExperience = 7
            };

            _logger.LogInformation("LinkedIn profile verification completed for URL: {Url}", linkedInUrl);
            return Task.FromResult(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying LinkedIn profile: {Url}", linkedInUrl);
            return Task.FromResult(CreateFailedResult("LinkedIn", $"Verification failed: {ex.Message}"));
        }
    }

    public Task<ExternalVerificationResult> VerifyGitHubContributionsAsync(string githubUsername)
    {
        try
        {
            if (!IsIntegrationEnabled("GitHub"))
            {
                return Task.FromResult(CreateFailedResult("GitHub", "GitHub integration is disabled"));
            }

            if (string.IsNullOrWhiteSpace(githubUsername))
            {
                return Task.FromResult(CreateFailedResult("GitHub", "GitHub username is required"));
            }

            // Check rate limiting
            if (IsRateLimited("GitHub"))
            {
                return Task.FromResult(CreateFailedResult("GitHub", "Rate limit exceeded for GitHub API"));
            }

            // For now, return a simulated successful verification
            // In production, this would connect to GitHub API
            var result = new ExternalVerificationResult
            {
                Platform = "GitHub",
                IsVerified = true,
                ConfidenceScore = 0.92m,
                VerifiedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddHours(GetCacheDurationHours()),
                ProfessionalData = new Dictionary<string, object>
                {
                    ["username"] = githubUsername,
                    ["verified"] = true,
                    ["publicRepos"] = 45,
                    ["followers"] = 120,
                    ["totalCommits"] = 1250,
                    ["activeContributionStreak"] = 30
                },
                ProfessionalScore = 92,
                ExperienceLevel = "Expert",
                ExtractedSkills = new List<string> { "JavaScript", "C#", "Python", "Docker", "Azure" },
                Industry = "Software Development",
                YearsOfExperience = 5
            };

            _logger.LogInformation("GitHub contributions verification completed for user: {Username}", githubUsername);
            return Task.FromResult(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying GitHub contributions: {Username}", githubUsername);
            return Task.FromResult(CreateFailedResult("GitHub", $"Verification failed: {ex.Message}"));
        }
    }

    public bool IsIntegrationEnabled(string platform)
    {
        var configKey = $"ExternalIntegration:{platform}Enabled";
        return _configuration.GetValue<bool>(configKey, false);
    }

    public async Task<ExternalVerificationResult?> GetCachedVerificationAsync(Guid userId, string platform)
    {
        // In a real implementation, this would query a cache store (Redis, database, etc.)
        // For now, return null to indicate no cached result
        await Task.CompletedTask;
        return null;
    }

    public async Task CacheVerificationResultAsync(Guid userId, string platform, ExternalVerificationResult result)
    {
        // In a real implementation, this would store the result in a cache store
        // For now, just log the action
        await Task.CompletedTask;
        _logger.LogInformation("Cached verification result for user {UserId} on platform {Platform}", userId, platform);
    }

    private static bool IsValidLinkedInUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return false;

        return url.Contains("linkedin.com/in/") || url.Contains("linkedin.com/pub/");
    }

    private bool IsRateLimited(string platform)
    {
        // Thread-safe check using TryGetValue for atomic read operation
        if (!_rateLimitCache.TryGetValue(platform, out var lastRequestTime))
            return false;

        // Simple rate limiting - one request per minute per platform
        return DateTime.UtcNow - lastRequestTime < TimeSpan.FromMinutes(1);
    }

    private int GetCacheDurationHours()
    {
        return _configuration.GetValue<int>("ExternalIntegration:CacheDurationHours", 24);
    }

    private static ExternalVerificationResult CreateFailedResult(string platform, string error)
    {
        return new ExternalVerificationResult
        {
            Platform = platform,
            IsVerified = false,
            ConfidenceScore = 0m,
            ErrorMessage = error,
            VerifiedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow
        };
    }
}