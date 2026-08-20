using SkillLedger.Core.Interfaces;

namespace SkillLedger.Tests.Mocks;

/// <summary>
/// Mock content moderation service for testing - external AI service (OK to mock)
/// Returns safe/approved results by default for all content
/// </summary>
public class MockContentModerationService : IContentModerationService
{
    private bool _shouldApprove = true;
    private ContentRiskLevel _riskLevel = ContentRiskLevel.Safe;

    /// <summary>
    /// Configure mock to reject content
    /// </summary>
    public void ConfigureRejection(ContentRiskLevel riskLevel = ContentRiskLevel.High)
    {
        _shouldApprove = false;
        _riskLevel = riskLevel;
    }

    /// <summary>
    /// Configure mock to approve content (default behavior)
    /// </summary>
    public void ConfigureApproval()
    {
        _shouldApprove = true;
        _riskLevel = ContentRiskLevel.Safe;
    }

    public Task<ContentModerationResult> AnalyzeTextAsync(string text, Guid? userId = null)
    {
        return Task.FromResult(new ContentModerationResult
        {
            IsApproved = _shouldApprove,
            RequiresHumanReview = !_shouldApprove,
            RiskLevel = _riskLevel,
            FlaggedCategories = _shouldApprove ? Array.Empty<ContentCategory>() : new[] { ContentCategory.ProfessionalMisconduct },
            Scores = new ContentModerationScore(),
            BlockedTerms = Array.Empty<string>(),
            ReasonForRejection = _shouldApprove ? null : "Content flagged by moderation",
            AnalyzedAt = DateTime.UtcNow,
            AnalysisId = Guid.NewGuid().ToString()
        });
    }

    public Task<ContentModerationResult> AnalyzeImageAsync(Stream imageStream, Guid? userId = null)
    {
        return Task.FromResult(new ContentModerationResult
        {
            IsApproved = _shouldApprove,
            RequiresHumanReview = !_shouldApprove,
            RiskLevel = _riskLevel,
            FlaggedCategories = _shouldApprove ? Array.Empty<ContentCategory>() : new[] { ContentCategory.ProfessionalMisconduct },
            Scores = new ContentModerationScore(),
            BlockedTerms = Array.Empty<string>(),
            ReasonForRejection = _shouldApprove ? null : "Image flagged by moderation",
            AnalyzedAt = DateTime.UtcNow,
            AnalysisId = Guid.NewGuid().ToString()
        });
    }

    public Task<ContentModerationResult> AnalyzeImageAsync(string imageUrl, Guid? userId = null)
    {
        return Task.FromResult(new ContentModerationResult
        {
            IsApproved = _shouldApprove,
            RequiresHumanReview = !_shouldApprove,
            RiskLevel = _riskLevel,
            FlaggedCategories = _shouldApprove ? Array.Empty<ContentCategory>() : new[] { ContentCategory.ProfessionalMisconduct },
            Scores = new ContentModerationScore(),
            BlockedTerms = Array.Empty<string>(),
            ReasonForRejection = _shouldApprove ? null : "Image flagged by moderation",
            AnalyzedAt = DateTime.UtcNow,
            AnalysisId = Guid.NewGuid().ToString()
        });
    }

    public Task<bool> CanAutoApproveUserContentAsync(Guid userId)
    {
        return Task.FromResult(true); // Always approve for testing
    }

    public Task<bool> RecordModerationResultAsync(Guid userId, ContentType contentType, ContentModerationResult result)
    {
        return Task.FromResult(true); // Recording always succeeds in tests
    }

    public Task<IEnumerable<string>> GetCustomBlocklistAsync()
    {
        return Task.FromResult<IEnumerable<string>>(Array.Empty<string>());
    }

    public Task<bool> AddToBlocklistAsync(IEnumerable<string> terms, Guid addedByUserId)
    {
        return Task.FromResult(true);
    }
}
