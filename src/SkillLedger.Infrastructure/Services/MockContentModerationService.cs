using Microsoft.Extensions.Logging;
using SkillLedger.Core.Interfaces;

namespace SkillLedger.Infrastructure.Services;

/// <summary>
/// Mock content moderation service for development/testing when Azure Content Safety is not configured.
/// Returns approved results for all content by default.
/// </summary>
public class MockContentModerationService : IContentModerationService
{
    private readonly ILogger<MockContentModerationService> _logger;

    public MockContentModerationService(ILogger<MockContentModerationService> logger)
    {
        _logger = logger;
        _logger.LogWarning("Using MockContentModerationService - content moderation is simulated (always approved)");
    }

    public Task<ContentModerationResult> AnalyzeTextAsync(string text, Guid? userId = null)
    {
        _logger.LogDebug("[MOCK MODERATION] Analyzing text (length: {Length}) for user {UserId} - auto-approved",
            text?.Length ?? 0, userId);

        return Task.FromResult(CreateApprovedResult("Text auto-approved by mock service"));
    }

    public Task<ContentModerationResult> AnalyzeImageAsync(Stream imageStream, Guid? userId = null)
    {
        _logger.LogDebug("[MOCK MODERATION] Analyzing image stream for user {UserId} - auto-approved", userId);

        return Task.FromResult(CreateApprovedResult("Image auto-approved by mock service"));
    }

    public Task<ContentModerationResult> AnalyzeImageAsync(string imageUrl, Guid? userId = null)
    {
        _logger.LogDebug("[MOCK MODERATION] Analyzing image URL {Url} for user {UserId} - auto-approved",
            imageUrl, userId);

        return Task.FromResult(CreateApprovedResult("Image auto-approved by mock service"));
    }

    public Task<bool> CanAutoApproveUserContentAsync(Guid userId)
    {
        _logger.LogDebug("[MOCK MODERATION] Checking auto-approve for user {UserId} - returning true", userId);
        return Task.FromResult(true);
    }

    public Task<bool> RecordModerationResultAsync(Guid userId, ContentType contentType, ContentModerationResult result)
    {
        _logger.LogDebug("[MOCK MODERATION] Recording moderation result for user {UserId}, type {ContentType}",
            userId, contentType);
        return Task.FromResult(true);
    }

    public Task<IEnumerable<string>> GetCustomBlocklistAsync()
    {
        _logger.LogDebug("[MOCK MODERATION] Getting custom blocklist - returning empty");
        return Task.FromResult<IEnumerable<string>>(Array.Empty<string>());
    }

    public Task<bool> AddToBlocklistAsync(IEnumerable<string> terms, Guid addedByUserId)
    {
        _logger.LogDebug("[MOCK MODERATION] Adding {Count} terms to blocklist by user {UserId}",
            terms?.Count() ?? 0, addedByUserId);
        return Task.FromResult(true);
    }

    private static ContentModerationResult CreateApprovedResult(string message)
    {
        return new ContentModerationResult
        {
            IsApproved = true,
            RequiresHumanReview = false,
            RiskLevel = ContentRiskLevel.Safe,
            FlaggedCategories = Array.Empty<ContentCategory>(),
            Scores = new ContentModerationScore(),
            BlockedTerms = Array.Empty<string>(),
            ReasonForRejection = null,
            AnalyzedAt = DateTime.UtcNow,
            AnalysisId = $"mock-{Guid.NewGuid():N}"
        };
    }
}
