using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.EntityFrameworkCore;
using SkillLedger.Core.Interfaces;
using SkillLedger.Core.Entities;
using SkillLedger.Infrastructure.Data;
using SkillLedger.Infrastructure.Configuration;
using System.Text.Json;
using System.Text.RegularExpressions;
using Azure.AI.ContentSafety;
using Azure;

namespace SkillLedger.Infrastructure.Services;

/// <summary>
/// Content moderation service using Azure Content Safety API
/// </summary>
public class ContentModerationService : IContentModerationService
{
    private readonly ILogger<ContentModerationService> _logger;
    private readonly SkillLedgerDbContext _context;
    private readonly ContentSafetyClient _contentSafetyClient;
    private readonly ContentModerationConfiguration _config;
    private readonly IHttpClientFactory _httpClientFactory;

    // Custom blocklist for professional platform
    // NOTE: Only include terms that should ALWAYS be blocked regardless of context
    // Content analysis terms (hate, violence, etc.) should NOT be in blocklist
    // so they can be properly categorized and scored by AnalyzeTextContent
    private static readonly HashSet<string> ProfessionalBlocklist = new(StringComparer.OrdinalIgnoreCase)
    {
        // Professional misconduct
        "fake", "scam", "fraud", "cheat", "steal", "plagiarize", "bribe",
        "kickback", "under the table", "off the books", "tax evasion",

        // Inappropriate content
        "explicit", "nsfw", "adult content", "pornographic", "sexual services",

        // Spam indicators
        "get rich quick", "make money fast", "guaranteed income", "work from home",
        "mlm", "pyramid scheme", "ponzi", "cryptocurrency investment",

        // Harassment terms (specific actions only)
        "stalking", "doxxing", "blackmail", "extortion"
    };

    public ContentModerationService(
        ILogger<ContentModerationService> logger,
        SkillLedgerDbContext context,
        IOptions<ContentModerationConfiguration> config,
        IHttpClientFactory httpClientFactory)
    {
        _logger = logger;
        _context = context;
        _config = config.Value;
        _httpClientFactory = httpClientFactory;

        // Initialize Azure Content Safety client
        var credential = new AzureKeyCredential(_config.ApiKey);
        _contentSafetyClient = new ContentSafetyClient(_config.Endpoint, credential);
    }

    /// <summary>
    /// Analyze text content using Azure Content Safety
    /// </summary>
    public async Task<ContentModerationResult> AnalyzeTextAsync(string text, Guid? userId = null)
    {
        var result = new ContentModerationResult();

        try
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                result.IsApproved = true;
                result.RiskLevel = ContentRiskLevel.Safe;
                return result;
            }

            // Check custom blocklist first
            var blockedTerms = CheckCustomBlocklist(text);
            if (blockedTerms.Any())
            {
                result.IsApproved = false;
                result.RiskLevel = ContentRiskLevel.High;
                result.BlockedTerms = blockedTerms.ToArray();
                result.ReasonForRejection = "Content contains prohibited terms";
                await RecordModerationResultAsync(userId ?? Guid.Empty, ContentType.UserGeneratedContent, result);
                return result;
            }

            // Simplified content analysis (Azure Content Safety API integration would go here)
            // For now, we'll use basic pattern matching as a placeholder
            result.Scores = AnalyzeTextContent(text);
            result.RiskLevel = CalculateTextRiskLevel(result.Scores);
            result.FlaggedCategories = GetFlaggedTextCategories(text, result.Scores);

            // Apply business rules for professional platform
            result.IsApproved = await ShouldApproveContentAsync(result.RiskLevel, userId);
            result.RequiresHumanReview = ShouldRequireHumanReview(result.RiskLevel, result.FlaggedCategories);

            if (!result.IsApproved)
            {
                result.ReasonForRejection = GenerateRejectionReason(result.FlaggedCategories, result.RiskLevel);
            }

            result.AnalysisId = Guid.NewGuid().ToString();

            // Record the moderation result
            if (userId.HasValue)
            {
                await RecordModerationResultAsync(userId.Value, ContentType.UserGeneratedContent, result);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing text content for user {UserId}", userId);

            // Fail safely - reject content if we can't moderate it
            return new ContentModerationResult
            {
                IsApproved = false,
                RiskLevel = ContentRiskLevel.Critical,
                RequiresHumanReview = true,
                ReasonForRejection = "Content moderation service unavailable"
            };
        }
    }

    /// <summary>
    /// Analyze image content using Azure Content Safety
    /// </summary>
    public async Task<ContentModerationResult> AnalyzeImageAsync(Stream imageStream, Guid? userId = null)
    {
        var result = new ContentModerationResult();

        try
        {
            if (imageStream == null || imageStream.Length == 0)
            {
                result.IsApproved = true;
                result.RiskLevel = ContentRiskLevel.Safe;
                return result;
            }

            // Validate image size and format
            if (imageStream.Length > _config.MaxImageSizeBytes)
            {
                result.IsApproved = false;
                result.RiskLevel = ContentRiskLevel.Medium;
                result.ReasonForRejection = $"Image size exceeds maximum allowed ({_config.MaxImageSizeBytes / 1024 / 1024}MB)";
                return result;
            }

            // Simplified image analysis (Azure Content Safety API integration would go here)
            // For now, basic image validation
            result.Scores = new ContentModerationScore(); // Safe scores
            result.RiskLevel = ContentRiskLevel.Safe;
            result.FlaggedCategories = Array.Empty<ContentCategory>();
            result.IsApproved = await ShouldApproveContentAsync(result.RiskLevel, userId);
            result.RequiresHumanReview = ShouldRequireHumanReview(result.RiskLevel, result.FlaggedCategories);

            if (!result.IsApproved)
            {
                result.ReasonForRejection = GenerateRejectionReason(result.FlaggedCategories, result.RiskLevel);
            }

            result.AnalysisId = Guid.NewGuid().ToString();

            // Record the moderation result
            if (userId.HasValue)
            {
                await RecordModerationResultAsync(userId.Value, ContentType.ProfilePhoto, result);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing image content for user {UserId}", userId);

            return new ContentModerationResult
            {
                IsApproved = false,
                RiskLevel = ContentRiskLevel.Critical,
                RequiresHumanReview = true,
                ReasonForRejection = "Image moderation service unavailable"
            };
        }
    }

    /// <summary>
    /// Analyze image from URL
    /// </summary>
    public async Task<ContentModerationResult> AnalyzeImageAsync(string imageUrl, Guid? userId = null)
    {
        try
        {
            // BUG-HIGH-003 FIX: Validate URL to prevent SSRF attacks
            if (!IsUrlSafeForFetch(imageUrl))
            {
                _logger.LogWarning("SSRF attempt blocked: Unsafe URL {ImageUrl}", imageUrl);
                return new ContentModerationResult
                {
                    IsApproved = false,
                    RiskLevel = ContentRiskLevel.High,
                    RequiresHumanReview = true,
                    ReasonForRejection = "Invalid or unsafe image URL"
                };
            }

            // BUG-NEW-008 FIX: Use IHttpClientFactory instead of new HttpClient() to prevent socket exhaustion
            using var httpClient = _httpClientFactory.CreateClient();
            httpClient.Timeout = TimeSpan.FromSeconds(30);

            var response = await httpClient.GetAsync(imageUrl);
            response.EnsureSuccessStatusCode();

            using var imageStream = await response.Content.ReadAsStreamAsync();
            return await AnalyzeImageAsync(imageStream, userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading and analyzing image from URL {ImageUrl}", imageUrl);

            return new ContentModerationResult
            {
                IsApproved = false,
                RiskLevel = ContentRiskLevel.High,
                RequiresHumanReview = true,
                ReasonForRejection = "Unable to analyze image from provided URL"
            };
        }
    }

    /// <summary>
    /// Check if user content should be auto-approved based on trust score
    /// </summary>
    public async Task<bool> CanAutoApproveUserContentAsync(Guid userId)
    {
        try
        {
            // Get user's moderation history
            var recentModerations = await _context.ContentModerationLogs
                .Where(cml => cml.UserId == userId && cml.CreatedAt > DateTime.UtcNow.AddDays(-30))
                .OrderByDescending(cml => cml.CreatedAt)
                .Take(50)
                .ToListAsync();

            if (!recentModerations.Any())
                return false; // New users need manual review

            // Calculate approval rate
            var approvedCount = recentModerations.Count(m => m.WasApproved);
            var approvalRate = (double)approvedCount / recentModerations.Count;

            // Check for recent violations
            var recentViolations = recentModerations.Count(m => !m.WasApproved && m.CreatedAt > DateTime.UtcNow.AddDays(-7));

            // Auto-approve if high trust score and no recent violations
            return approvalRate >= 0.95 && recentViolations == 0 && recentModerations.Count >= 10;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking auto-approval eligibility for user {UserId}", userId);
            return false; // Err on the side of caution
        }
    }

    /// <summary>
    /// Record moderation result and update user trust metrics
    /// </summary>
    public async Task<bool> RecordModerationResultAsync(Guid userId, ContentType contentType, ContentModerationResult result)
    {
        try
        {
            var log = new ContentModerationLog
            {
                UserId = userId,
                ContentType = (int)contentType,
                WasApproved = result.IsApproved,
                RiskLevel = (int)result.RiskLevel,
                RequiredHumanReview = result.RequiresHumanReview,
                FlaggedCategories = JsonSerializer.Serialize(result.FlaggedCategories),
                ModerationScores = JsonSerializer.Serialize(result.Scores),
                BlockedTerms = JsonSerializer.Serialize(result.BlockedTerms),
                ReasonForRejection = result.ReasonForRejection,
                AnalysisId = result.AnalysisId
            };

            _context.ContentModerationLogs.Add(log);
            await _context.SaveChangesAsync();

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recording moderation result for user {UserId}", userId);
            return false;
        }
    }

    /// <summary>
    /// Get organization's custom blocklist
    /// </summary>
    public async Task<IEnumerable<string>> GetCustomBlocklistAsync()
    {
        try
        {
            var customTerms = await _context.CustomBlocklistTerms
                .Where(cbt => cbt.IsActive && (cbt.ExpiresAt == null || cbt.ExpiresAt > DateTime.UtcNow))
                .Select(cbt => cbt.Term)
                .ToListAsync();

            return ProfessionalBlocklist.Concat(customTerms);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving custom blocklist");
            return ProfessionalBlocklist;
        }
    }

    /// <summary>
    /// Add terms to custom blocklist
    /// </summary>
    public async Task<bool> AddToBlocklistAsync(IEnumerable<string> terms, Guid addedByUserId)
    {
        try
        {
            var blocklistTerms = terms.Select(term => new CustomBlocklistTerm
            {
                Term = term.ToLowerInvariant(),
                AddedByUserId = addedByUserId,
                IsActive = true
            });

            _context.CustomBlocklistTerms.AddRange(blocklistTerms);
            await _context.SaveChangesAsync();

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding terms to blocklist");
            return false;
        }
    }

    private static List<string> CheckCustomBlocklist(string text)
    {
        var lowerText = text.ToLowerInvariant();
        return ProfessionalBlocklist.Where(term => lowerText.Contains(term)).ToList();
    }

    private static ContentModerationScore AnalyzeTextContent(string text)
    {
        // Simplified content analysis using pattern matching
        var scores = new ContentModerationScore();
        var lowerText = text.ToLowerInvariant();

        // Basic hate speech detection - use whole word matching to avoid false positives (e.g., "skilled" contains "kill")
        var hateTerms = new[] { "hate", "racist", "nazi", "kill", "murder" };
        scores.Hate = hateTerms.Any(term => ContainsWholeWord(lowerText, term)) ? 6.0 : 0.0;

        // Basic violence detection - use whole word matching
        var violenceTerms = new[] { "violence", "attack", "assault", "bomb", "weapon" };
        scores.Violence = violenceTerms.Any(term => ContainsWholeWord(lowerText, term)) ? 5.0 : 0.0;

        // Basic harassment detection - use whole word matching
        var harassmentTerms = new[] { "stalk", "harass", "threaten", "bully" };
        scores.Harassment = harassmentTerms.Any(term => ContainsWholeWord(lowerText, term)) ? 4.0 : 0.0;

        return scores;
    }

    /// <summary>
    /// Check if text contains a whole word (not as a substring)
    /// </summary>
    private static bool ContainsWholeWord(string text, string word)
    {
        // Use word boundaries to match whole words only
        var pattern = $@"\b{Regex.Escape(word)}\b";
        return Regex.IsMatch(text, pattern, RegexOptions.IgnoreCase);
    }

    private static ContentCategory[] GetFlaggedTextCategories(string text, ContentModerationScore scores)
    {
        var flagged = new List<ContentCategory>();

        if (scores.Hate >= 4.0) flagged.Add(ContentCategory.Hate);
        if (scores.Violence >= 4.0) flagged.Add(ContentCategory.Violence);
        if (scores.Harassment >= 3.0) flagged.Add(ContentCategory.Harassment);

        return flagged.ToArray();
    }

    private static ContentRiskLevel CalculateTextRiskLevel(ContentModerationScore scores)
    {
        var maxScore = Math.Max(Math.Max(scores.Hate, scores.SelfHarm), Math.Max(scores.Sexual, scores.Violence));

        return maxScore switch
        {
            >= 6 => ContentRiskLevel.Critical,
            >= 4 => ContentRiskLevel.High,
            >= 2 => ContentRiskLevel.Medium,
            > 0 => ContentRiskLevel.Low,
            _ => ContentRiskLevel.Safe
        };
    }

    private static ContentRiskLevel CalculateImageRiskLevel(ContentModerationScore scores)
    {
        // Images have stricter thresholds for professional platform
        var maxScore = Math.Max(Math.Max(scores.Hate, scores.SelfHarm), Math.Max(scores.Sexual, scores.Violence));

        return maxScore switch
        {
            >= 4 => ContentRiskLevel.Critical,
            >= 2 => ContentRiskLevel.High,
            >= 1 => ContentRiskLevel.Medium,
            > 0 => ContentRiskLevel.Low,
            _ => ContentRiskLevel.Safe
        };
    }


    private async Task<bool> ShouldApproveContentAsync(ContentRiskLevel riskLevel, Guid? userId)
    {
        // Critical and High risk content is always rejected
        if (riskLevel >= ContentRiskLevel.High)
            return false;

        // Safe content is always approved
        if (riskLevel == ContentRiskLevel.Safe)
            return true;

        // For medium and low risk, check user trust level
        if (userId.HasValue && await CanAutoApproveUserContentAsync(userId.Value))
            return true;

        // Default to rejecting medium/low risk content for new users
        return riskLevel == ContentRiskLevel.Low;
    }

    private static bool ShouldRequireHumanReview(ContentRiskLevel riskLevel, ContentCategory[] flaggedCategories)
    {
        // Always review high-risk content
        if (riskLevel >= ContentRiskLevel.High)
            return true;

        // Review if sensitive categories are flagged
        var sensitiveCategories = new[] { ContentCategory.Hate, ContentCategory.Violence, ContentCategory.Harassment };
        return flaggedCategories.Any(cat => sensitiveCategories.Contains(cat));
    }

    private static string GenerateRejectionReason(ContentCategory[] flaggedCategories, ContentRiskLevel riskLevel)
    {
        if (!flaggedCategories.Any())
            return "Content does not meet platform guidelines";

        var categoryNames = flaggedCategories.Select(cat => cat.ToString()).ToArray();
        return $"Content flagged for: {string.Join(", ", categoryNames)}. Risk level: {riskLevel}";
    }

    /// <summary>
    /// BUG-HIGH-003 FIX: Validates URL to prevent SSRF (Server-Side Request Forgery) attacks
    /// </summary>
    private static bool IsUrlSafeForFetch(string url)
    {
        // Validate URL format
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            return false;

        // Only allow HTTP/HTTPS schemes
        if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
            return false;

        // Block localhost and loopback
        if (uri.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase) ||
            uri.Host.Equals("127.0.0.1") ||
            uri.Host.Equals("::1"))
            return false;

        // Resolve hostname to IP and check for private ranges
        try
        {
            var addresses = System.Net.Dns.GetHostAddresses(uri.Host);
            foreach (var ip in addresses)
            {
                var bytes = ip.GetAddressBytes();

                // Block IPv4 private ranges
                if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                {
                    // 10.0.0.0/8
                    if (bytes[0] == 10)
                        return false;

                    // 172.16.0.0/12
                    if (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31)
                        return false;

                    // 192.168.0.0/16
                    if (bytes[0] == 192 && bytes[1] == 168)
                        return false;

                    // 127.0.0.0/8 (loopback)
                    if (bytes[0] == 127)
                        return false;

                    // 169.254.0.0/16 (link-local / AWS/Azure metadata)
                    if (bytes[0] == 169 && bytes[1] == 254)
                        return false;
                }

                // Block IPv6 private ranges
                if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6)
                {
                    // ::1 (loopback)
                    if (ip.IsIPv6LinkLocal || ip.IsIPv6SiteLocal)
                        return false;

                    // fe80::/10 (link-local)
                    if (bytes[0] == 0xfe && (bytes[1] & 0xc0) == 0x80)
                        return false;

                    // fc00::/7 (unique local)
                    if ((bytes[0] & 0xfe) == 0xfc)
                        return false;
                }
            }
        }
        catch
        {
            // DNS resolution failed - block the request
            return false;
        }

        return true;
    }
}

/// <summary>
/// Configuration for content moderation service
/// </summary>
public class ContentModerationConfiguration
{
    public Uri Endpoint { get; set; } = new Uri("https://example.cognitiveservices.azure.com/");
    public string ApiKey { get; set; } = string.Empty;
    public long MaxImageSizeBytes { get; set; } = 10 * 1024 * 1024; // 10MB
    public int MaxTextLength { get; set; } = 10000;
    public bool EnableImageModeration { get; set; } = true;
    public bool EnableTextModeration { get; set; } = true;
    public double AutoApprovalThreshold { get; set; } = 0.95;
}