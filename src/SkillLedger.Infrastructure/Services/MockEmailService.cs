using Microsoft.Extensions.Logging;
using SkillLedger.Core.Interfaces;

namespace SkillLedger.Infrastructure.Services;

/// <summary>
/// Mock email service for development/testing when Azure Communication Services is not configured
/// </summary>
public class MockEmailService : IEmailService
{
    private readonly ILogger<MockEmailService> _logger;

    public MockEmailService(ILogger<MockEmailService> logger)
    {
        _logger = logger;
        _logger.LogWarning("Using MockEmailService - emails will be logged but not sent");
    }


    public Task<bool> SendWelcomeEmailAsync(string toEmail, string userName)
    {
        _logger.LogInformation(
            "[MOCK EMAIL] Welcome Email\n" +
            "To: {ToEmail}\n" +
            "Subject: Welcome to SkillLedger!\n" +
            "User: {UserName}",
            toEmail, userName);

        return Task.FromResult(true);
    }

    public Task<bool> SendPasswordResetEmailAsync(string toEmail, string userName, string resetToken, string baseUrl)
    {
        var resetUrl = $"{baseUrl}/reset-password?token={Uri.EscapeDataString(resetToken)}";

        _logger.LogInformation(
            "[MOCK EMAIL] Password Reset Email\n" +
            "To: {ToEmail}\n" +
            "Subject: Reset Your Password - SkillLedger\n" +
            "User: {UserName}\n" +
            "Reset URL: {ResetUrl}\n" +
            "Token: {Token}",
            toEmail, userName, resetUrl, resetToken);

        return Task.FromResult(true);
    }

    public Task<bool> SendEmailAsync(string toEmail, string subject, string message)
    {
        _logger.LogInformation(
            "[MOCK EMAIL] Generic Email\n" +
            "To: {ToEmail}\n" +
            "Subject: {Subject}\n" +
            "Message: {Message}",
            toEmail, subject, message);

        return Task.FromResult(true);
    }
}