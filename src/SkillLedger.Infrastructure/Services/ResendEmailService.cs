using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Resend;
using SkillLedger.Core.Interfaces;

namespace SkillLedger.Infrastructure.Services;

/// <summary>
/// Email service implementation using Resend
/// </summary>
public class ResendEmailService : IEmailService
{
    private readonly IResend _resend;
    private readonly ILogger<ResendEmailService> _logger;
    private readonly string _fromEmail;
    private readonly string _fromDisplayName;

    public ResendEmailService(
        IResend resend,
        IConfiguration configuration,
        ILogger<ResendEmailService> logger)
    {
        _resend = resend ?? throw new ArgumentNullException(nameof(resend));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        if (configuration == null) throw new ArgumentNullException(nameof(configuration));

        _fromEmail = configuration["EmailSettings:FromEmail"]
            ?? throw new InvalidOperationException("EmailSettings:FromEmail not configured");

        _fromDisplayName = configuration["EmailSettings:FromDisplayName"] ?? "SkillLedger";
    }

    public async Task<bool> SendWelcomeEmailAsync(string toEmail, string userName)
    {
        if (string.IsNullOrWhiteSpace(toEmail)) return false;
        if (string.IsNullOrWhiteSpace(userName)) userName = "User";

        try
        {
            var subject = "Welcome to SkillLedger!";
            var htmlContent = GetWelcomeEmailHtmlTemplate(userName);
            var textContent = GetWelcomeEmailTextTemplate(userName);

            var result = await SendEmailInternalAsync(toEmail, subject, htmlContent, textContent);

            if (result)
            {
                _logger.LogInformation("Welcome email sent successfully to {Email}", toEmail);
            }
            else
            {
                _logger.LogWarning("Failed to send welcome email to {Email}", toEmail);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending welcome email to {Email}", toEmail);
            return false;
        }
    }

    public async Task<bool> SendPasswordResetEmailAsync(string toEmail, string userName, string resetToken, string baseUrl)
    {
        if (string.IsNullOrWhiteSpace(toEmail)) return false;
        if (string.IsNullOrWhiteSpace(resetToken)) return false;

        try
        {
            var resetUrl = $"{baseUrl}/reset-password?token={Uri.EscapeDataString(resetToken)}";
            var subject = "Reset Your Password - SkillLedger";
            var htmlContent = GetPasswordResetHtmlTemplate(userName ?? "User", resetUrl);
            var textContent = GetPasswordResetTextTemplate(userName ?? "User", resetUrl);

            var result = await SendEmailInternalAsync(toEmail, subject, htmlContent, textContent);

            if (result)
            {
                _logger.LogInformation("Password reset email sent successfully to {Email}", toEmail);
            }
            else
            {
                _logger.LogWarning("Failed to send password reset email to {Email}", toEmail);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending password reset email to {Email}", toEmail);
            return false;
        }
    }

    public async Task<bool> SendEmailAsync(string toEmail, string subject, string message)
    {
        if (string.IsNullOrWhiteSpace(toEmail)) return false;
        if (string.IsNullOrWhiteSpace(subject)) return false;
        if (string.IsNullOrWhiteSpace(message)) return false;

        try
        {
            // Detect if message is HTML
            bool isHtml = message.TrimStart().StartsWith("<", StringComparison.Ordinal) ||
                         message.Contains("<html", StringComparison.OrdinalIgnoreCase) ||
                         message.Contains("<body", StringComparison.OrdinalIgnoreCase);

            var result = isHtml
                ? await SendEmailInternalAsync(toEmail, subject, message, StripHtml(message))
                : await SendEmailInternalAsync(toEmail, subject, null, message);

            if (result)
            {
                _logger.LogInformation("Email sent successfully to {Email} with subject {Subject}", toEmail, subject);
            }
            else
            {
                _logger.LogWarning("Failed to send email to {Email} with subject {Subject}", toEmail, subject);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending email to {Email} with subject {Subject}", toEmail, subject);
            return false;
        }
    }

    private async Task<bool> SendEmailInternalAsync(string toEmail, string subject, string? htmlContent, string? textContent)
    {
        try
        {
            var message = new EmailMessage
            {
                From = $"{_fromDisplayName} <{_fromEmail}>",
                Subject = subject
            };
            message.To.Add(toEmail);

            if (!string.IsNullOrWhiteSpace(htmlContent))
            {
                message.HtmlBody = htmlContent;
            }

            if (!string.IsNullOrWhiteSpace(textContent))
            {
                message.TextBody = textContent;
            }

            var response = await _resend.EmailSendAsync(message);

            _logger.LogInformation("Email sent via Resend successfully");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email via Resend to {Email}", toEmail);
            return false;
        }
    }

    private static string StripHtml(string html)
    {
        // Simple HTML stripping - removes tags
        return System.Text.RegularExpressions.Regex.Replace(html, "<[^>]*>", " ")
            .Replace("&nbsp;", " ")
            .Replace("&amp;", "&")
            .Replace("&lt;", "<")
            .Replace("&gt;", ">")
            .Trim();
    }

    private string GetWelcomeEmailHtmlTemplate(string userName)
    {
        return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset=""utf-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>Welcome to SkillLedger!</title>
    <style>
        body {{
            font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, 'Helvetica Neue', Arial, sans-serif;
            line-height: 1.6;
            color: #333;
            max-width: 600px;
            margin: 0 auto;
            padding: 20px;
        }}
        .header {{
            background: linear-gradient(135deg, #10b981 0%, #059669 100%);
            color: white;
            padding: 30px;
            text-align: center;
            border-radius: 8px 8px 0 0;
        }}
        .content {{
            background: white;
            padding: 40px 30px;
            border: 1px solid #e1e5e9;
            border-radius: 0 0 8px 8px;
        }}
        .feature {{
            background: #f8fafc;
            padding: 20px;
            margin: 15px 0;
            border-left: 4px solid #3b82f6;
            border-radius: 4px;
        }}
        .footer {{
            text-align: center;
            font-size: 12px;
            color: #6b7280;
            margin-top: 30px;
            padding-top: 20px;
            border-top: 1px solid #e5e7eb;
        }}
    </style>
</head>
<body>
    <div class=""header"">
        <h1>🎉 Welcome to SkillLedger!</h1>
        <p>Your account is ready to go</p>
    </div>
    <div class=""content"">
        <h2>Welcome, {userName}!</h2>
        <p>Thank you for joining SkillLedger! Your account has been created and you're ready to start collaborating with professionals worldwide.</p>

        <h3>What's next?</h3>

        <div class=""feature"">
            <h4>📋 Create Your First Project</h4>
            <p>Start by posting a project or offering your skills to the community.</p>
        </div>

        <div class=""feature"">
            <h4>🏆 Build Your Reputation</h4>
            <p>Complete projects and earn ratings to establish your professional credibility.</p>
        </div>

        <div class=""feature"">
            <h4>💰 Earn Credits</h4>
            <p>Get paid in SkillCredits for your work and use them to hire others.</p>
        </div>

        <p>If you have any questions, our support team is here to help. Welcome aboard!</p>
    </div>
    <div class=""footer"">
        <p>&copy; {DateTime.UtcNow.Year} SkillLedger. All rights reserved.</p>
        <p>This email was sent from a notification-only address that cannot accept incoming email.</p>
    </div>
</body>
</html>";
    }

    private string GetWelcomeEmailTextTemplate(string userName)
    {
        return $@"
🎉 Welcome to SkillLedger!

Welcome, {userName}!

Thank you for joining SkillLedger! Your account has been created and you're ready to start collaborating with professionals worldwide.

What's next?

📋 Create Your First Project
Start by posting a project or offering your skills to the community.

🏆 Build Your Reputation
Complete projects and earn ratings to establish your professional credibility.

💰 Earn Credits
Get paid in SkillCredits for your work and use them to hire others.

If you have any questions, our support team is here to help. Welcome aboard!

--
© {DateTime.UtcNow.Year} SkillLedger. All rights reserved.
This email was sent from a notification-only address that cannot accept incoming email.
";
    }

    private string GetPasswordResetHtmlTemplate(string userName, string resetUrl)
    {
        return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset=""utf-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>Reset Your Password - SkillLedger</title>
    <style>
        body {{
            font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, 'Helvetica Neue', Arial, sans-serif;
            line-height: 1.6;
            color: #333;
            max-width: 600px;
            margin: 0 auto;
            padding: 20px;
        }}
        .header {{
            background: linear-gradient(135deg, #ef4444 0%, #dc2626 100%);
            color: white;
            padding: 30px;
            text-align: center;
            border-radius: 8px 8px 0 0;
        }}
        .content {{
            background: white;
            padding: 40px 30px;
            border: 1px solid #e1e5e9;
            border-radius: 0 0 8px 8px;
        }}
        .button {{
            display: inline-block;
            background: #ef4444;
            color: white;
            text-decoration: none;
            padding: 14px 30px;
            border-radius: 6px;
            font-weight: 600;
            text-align: center;
            margin: 20px 0;
        }}
        .button:hover {{ background: #dc2626; }}
        .alert {{
            background: #fef2f2;
            border: 1px solid #fecaca;
            color: #991b1b;
            padding: 15px;
            border-radius: 6px;
            margin: 20px 0;
        }}
        .footer {{
            text-align: center;
            font-size: 12px;
            color: #6b7280;
            margin-top: 30px;
            padding-top: 20px;
            border-top: 1px solid #e5e7eb;
        }}
    </style>
</head>
<body>
    <div class=""header"">
        <h1>🔒 Password Reset</h1>
        <p>Secure your account</p>
    </div>
    <div class=""content"">
        <h2>Reset Your Password</h2>
        <p>Hi {userName},</p>
        <p>We received a request to reset your password for your SkillLedger account. Click the button below to choose a new password.</p>

        <div style=""text-align: center;"">
            <a href=""{resetUrl}"" class=""button"">Reset Password</a>
        </div>

        <div class=""alert"">
            <strong>⚠️ Security Notice:</strong>
            <ul>
                <li>This link will expire in 1 hour</li>
                <li>The link can only be used once</li>
                <li>If you didn't request this reset, ignore this email</li>
            </ul>
        </div>

        <p>If the button doesn't work, copy and paste this link:</p>
        <p style=""word-break: break-all; color: #3b82f6;"">{resetUrl}</p>
    </div>
    <div class=""footer"">
        <p>&copy; {DateTime.UtcNow.Year} SkillLedger. All rights reserved.</p>
        <p>This email was sent from a notification-only address that cannot accept incoming email.</p>
    </div>
</body>
</html>";
    }

    private string GetPasswordResetTextTemplate(string userName, string resetUrl)
    {
        return $@"
🔒 Password Reset

Hi {userName},

We received a request to reset your password for your SkillLedger account. Click the link below to choose a new password.

Reset your password:
{resetUrl}

⚠️ Security Notice:
- This link will expire in 1 hour
- The link can only be used once
- If you didn't request this reset, ignore this email

--
© {DateTime.UtcNow.Year} SkillLedger. All rights reserved.
This email was sent from a notification-only address that cannot accept incoming email.
";
    }
}
