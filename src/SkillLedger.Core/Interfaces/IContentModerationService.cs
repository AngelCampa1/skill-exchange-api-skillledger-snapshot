namespace SkillLedger.Core.Interfaces;

/// <summary>
/// Service for content moderation using Azure Content Safety
/// </summary>
public interface IContentModerationService
{
    /// <summary>
    /// Analyze text content for harmful content
    /// </summary>
    /// <param name="text">Text content to analyze</param>
    /// <param name="userId">User who created the content</param>
    /// <returns>Content moderation result</returns>
    Task<ContentModerationResult> AnalyzeTextAsync(string text, Guid? userId = null);

    /// <summary>
    /// Analyze image content for harmful material
    /// </summary>
    /// <param name="imageStream">Image stream to analyze</param>
    /// <param name="userId">User who uploaded the image</param>
    /// <returns>Image moderation result</returns>
    Task<ContentModerationResult> AnalyzeImageAsync(Stream imageStream, Guid? userId = null);

    /// <summary>
    /// Analyze image from URL
    /// </summary>
    /// <param name="imageUrl">URL of image to analyze</param>
    /// <param name="userId">User who provided the URL</param>
    /// <returns>Image moderation result</returns>
    Task<ContentModerationResult> AnalyzeImageAsync(string imageUrl, Guid? userId = null);

    /// <summary>
    /// Check if user content should be auto-approved based on reputation
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <returns>True if content can be auto-approved</returns>
    Task<bool> CanAutoApproveUserContentAsync(Guid userId);

    /// <summary>
    /// Report content moderation result and update user trust score
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="contentType">Type of content moderated</param>
    /// <param name="result">Moderation result</param>
    /// <returns>Success indicator</returns>
    Task<bool> RecordModerationResultAsync(Guid userId, ContentType contentType, ContentModerationResult result);

    /// <summary>
    /// Get custom blocklist for organization
    /// </summary>
    /// <returns>List of blocked terms/patterns</returns>
    Task<IEnumerable<string>> GetCustomBlocklistAsync();

    /// <summary>
    /// Add terms to custom blocklist
    /// </summary>
    /// <param name="terms">Terms to block</param>
    /// <param name="addedByUserId">User adding the terms</param>
    /// <returns>Success indicator</returns>
    Task<bool> AddToBlocklistAsync(IEnumerable<string> terms, Guid addedByUserId);
}

/// <summary>
/// Content moderation analysis result
/// </summary>
public class ContentModerationResult
{
    public bool IsApproved { get; set; }
    public bool RequiresHumanReview { get; set; }
    public ContentRiskLevel RiskLevel { get; set; }
    public ContentCategory[] FlaggedCategories { get; set; } = Array.Empty<ContentCategory>();
    public ContentModerationScore Scores { get; set; } = new();
    public string[] BlockedTerms { get; set; } = Array.Empty<string>();
    public string? ReasonForRejection { get; set; }
    public DateTime AnalyzedAt { get; set; } = DateTime.UtcNow;
    public string? AnalysisId { get; set; }
}

/// <summary>
/// Content moderation severity scores (0-1)
/// </summary>
public class ContentModerationScore
{
    public double Hate { get; set; }
    public double SelfHarm { get; set; }
    public double Sexual { get; set; }
    public double Violence { get; set; }
    public double Harassment { get; set; }
    public double ProfessionalRisk { get; set; }
    public double SpamRisk { get; set; }
}

/// <summary>
/// Content risk levels for moderation decisions
/// </summary>
public enum ContentRiskLevel
{
    Safe = 0,
    Low = 1,
    Medium = 2,
    High = 3,
    Critical = 4
}

/// <summary>
/// Categories of harmful content
/// </summary>
public enum ContentCategory
{
    Safe = 0,
    Hate = 1,
    SelfHarm = 2,
    Sexual = 3,
    Violence = 4,
    Harassment = 5,
    PII = 6,
    Spam = 7,
    Scam = 8,
    Malware = 9,
    Copyright = 10,
    ProfessionalMisconduct = 11,
    Misinformation = 12
}

/// <summary>
/// Types of content being moderated
/// </summary>
public enum ContentType
{
    ProfileBio = 0,
    ProfilePhoto = 1,
    SkillDescription = 2,
    ExperienceDescription = 3,
    ProjectTitle = 4,
    ProjectDescription = 5,
    ProjectAttachment = 6,
    ChatMessage = 7,
    Review = 8,
    Comment = 9,
    UserGeneratedContent = 10
}