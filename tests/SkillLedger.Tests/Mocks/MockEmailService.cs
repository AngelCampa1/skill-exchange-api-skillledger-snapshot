using System.Collections.Concurrent;
using SkillLedger.Core.Interfaces;

namespace SkillLedger.Tests.Mocks;

/// <summary>
/// Mock email service for testing purposes that doesn't require Azure services
/// </summary>
public class MockEmailService : SkillLedger.Core.Interfaces.IEmailService
{
    public ConcurrentBag<MockSentEmail> SentEmails { get; } = new();

    /// <summary>
    /// Set to true to simulate email sending failure
    /// </summary>
    public bool ShouldFail { get; set; } = false;

    /// <summary>
    /// Set to true to simulate email service throwing an exception
    /// </summary>
    public bool ShouldThrowException { get; set; } = false;

    public Task<bool> SendWelcomeEmailAsync(string toEmail, string userName)
    {
        SentEmails.Add(new MockSentEmail
        {
            ToEmail = toEmail,
            Type = "Welcome",
            UserName = userName,
            SentAt = DateTime.UtcNow
        });
        return Task.FromResult(true);
    }

    public Task<bool> SendPasswordResetEmailAsync(string toEmail, string userName, string resetToken, string baseUrl)
    {
        SentEmails.Add(new MockSentEmail
        {
            ToEmail = toEmail,
            Type = "PasswordReset",
            UserName = userName,
            Token = resetToken,
            BaseUrl = baseUrl,
            Subject = "Password Reset Request",
            Body = $"Password reset link: {baseUrl}/reset-password?token={resetToken}",
            SentAt = DateTime.UtcNow
        });
        return Task.FromResult(true);
    }

    public Task<bool> SendEmailAsync(string toEmail, string subject, string message)
    {
        if (ShouldThrowException)
        {
            throw new InvalidOperationException("Simulated email service failure");
        }

        if (ShouldFail)
        {
            return Task.FromResult(false);
        }

        SentEmails.Add(new MockSentEmail
        {
            ToEmail = toEmail,
            Type = "Generic",
            Subject = subject,
            Body = message,
            SentAt = DateTime.UtcNow
        });
        return Task.FromResult(true);
    }
}

public class MockSentEmail
{
    public string ToEmail { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string? Token { get; set; }
    public string? BaseUrl { get; set; }
    public DateTime SentAt { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
}