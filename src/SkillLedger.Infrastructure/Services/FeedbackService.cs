using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SkillLedger.Core.DTOs;
using SkillLedger.Core.Interfaces;

namespace SkillLedger.Infrastructure.Services;

/// <summary>
/// Service for handling user feedback submissions via email
/// </summary>
public class FeedbackService : IFeedbackService
{
    private readonly IEmailService _emailService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<FeedbackService> _logger;
    private readonly string _adminEmail;
    private readonly string _subjectPrefix;

    public FeedbackService(
        IEmailService emailService,
        IConfiguration configuration,
        ILogger<FeedbackService> logger)
    {
        _emailService = emailService;
        _configuration = configuration;
        _logger = logger;

        _adminEmail = configuration["FeedbackSettings:AdminEmail"]
            ?? "angel.campa@skillledger.app";

        _subjectPrefix = configuration["FeedbackSettings:EmailSubjectPrefix"]
            ?? "[SkillLedger Feedback]";
    }

    public async Task<bool> SubmitFeedbackAsync(SubmitFeedbackDto dto, string? userId, string? userEmail)
    {
        try
        {
            var categoryDisplay = GetCategoryDisplayName(dto.Category);
            var subject = $"{_subjectPrefix} {categoryDisplay}";
            var htmlContent = BuildFeedbackEmailHtml(dto, userId, userEmail);

            var result = await _emailService.SendEmailAsync(_adminEmail, subject, htmlContent);

            if (result)
            {
                _logger.LogInformation(
                    "Feedback submitted successfully. Category: {Category}, UserId: {UserId}",
                    dto.Category,
                    userId ?? "Anonymous");
            }
            else
            {
                _logger.LogWarning(
                    "Failed to send feedback email. Category: {Category}, UserId: {UserId}",
                    dto.Category,
                    userId ?? "Anonymous");
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error submitting feedback. Category: {Category}, UserId: {UserId}",
                dto.Category,
                userId ?? "Anonymous");
            return false;
        }
    }

    private static string GetCategoryDisplayName(FeedbackCategory category)
    {
        return category switch
        {
            FeedbackCategory.General => "General Feedback",
            FeedbackCategory.Bug => "Bug Report",
            FeedbackCategory.FeatureRequest => "Feature Request",
            FeedbackCategory.Other => "Other",
            _ => "Feedback"
        };
    }

    private string BuildFeedbackEmailHtml(SubmitFeedbackDto dto, string? userId, string? userEmail)
    {
        var categoryDisplay = GetCategoryDisplayName(dto.Category);
        var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss UTC");
        var replyTo = dto.ReplyToEmail ?? userEmail ?? "Not provided";
        var userInfo = !string.IsNullOrEmpty(userId)
            ? $"User ID: {userId}<br/>User Email: {userEmail ?? "N/A"}"
            : "Anonymous User";

        var categoryColor = dto.Category switch
        {
            FeedbackCategory.Bug => "#ef4444",
            FeedbackCategory.FeatureRequest => "#3b82f6",
            FeedbackCategory.General => "#10b981",
            _ => "#6b7280"
        };

        return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset=""utf-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>User Feedback - SkillLedger</title>
    <style>
        body {{
            font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, 'Helvetica Neue', Arial, sans-serif;
            line-height: 1.6;
            color: #333;
            max-width: 600px;
            margin: 0 auto;
            padding: 20px;
            background-color: #f5f5f5;
        }}
        .container {{
            background: white;
            border-radius: 8px;
            box-shadow: 0 2px 4px rgba(0,0,0,0.1);
            overflow: hidden;
        }}
        .header {{
            background: {categoryColor};
            color: white;
            padding: 20px 30px;
        }}
        .header h1 {{
            margin: 0;
            font-size: 20px;
        }}
        .category-badge {{
            display: inline-block;
            background: rgba(255,255,255,0.2);
            padding: 4px 12px;
            border-radius: 12px;
            font-size: 12px;
            margin-top: 8px;
        }}
        .content {{
            padding: 30px;
        }}
        .info-row {{
            display: flex;
            margin-bottom: 15px;
            border-bottom: 1px solid #e5e7eb;
            padding-bottom: 15px;
        }}
        .info-label {{
            font-weight: 600;
            color: #6b7280;
            width: 120px;
            flex-shrink: 0;
        }}
        .info-value {{
            color: #111827;
        }}
        .message-box {{
            background: #f9fafb;
            border: 1px solid #e5e7eb;
            border-radius: 6px;
            padding: 20px;
            margin-top: 20px;
            white-space: pre-wrap;
            word-wrap: break-word;
        }}
        .message-label {{
            font-weight: 600;
            color: #374151;
            margin-bottom: 10px;
        }}
        .footer {{
            background: #f9fafb;
            padding: 15px 30px;
            font-size: 12px;
            color: #6b7280;
            border-top: 1px solid #e5e7eb;
        }}
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""header"">
            <h1>New Feedback Received</h1>
            <span class=""category-badge"">{categoryDisplay}</span>
        </div>
        <div class=""content"">
            <div class=""info-row"">
                <span class=""info-label"">Timestamp:</span>
                <span class=""info-value"">{timestamp}</span>
            </div>
            <div class=""info-row"">
                <span class=""info-label"">User:</span>
                <span class=""info-value"">{userInfo}</span>
            </div>
            <div class=""info-row"">
                <span class=""info-label"">Reply To:</span>
                <span class=""info-value"">{System.Net.WebUtility.HtmlEncode(replyTo)}</span>
            </div>

            <div class=""message-label"">Feedback Message:</div>
            <div class=""message-box"">{System.Net.WebUtility.HtmlEncode(dto.Message)}</div>
        </div>
        <div class=""footer"">
            This feedback was submitted via SkillLedger.
        </div>
    </div>
</body>
</html>";
    }
}
