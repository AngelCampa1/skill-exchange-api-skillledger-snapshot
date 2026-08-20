namespace SkillLedger.Core.Interfaces;

/// <summary>
/// Service for sending emails using Resend
/// </summary>
public interface IEmailService
{
    /// <summary>
    /// Sends a welcome email after successful registration
    /// </summary>
    /// <param name="toEmail">Recipient email address</param>
    /// <param name="userName">User's name for personalization</param>
    /// <returns>True if email was sent successfully</returns>
    Task<bool> SendWelcomeEmailAsync(string toEmail, string userName);

    /// <summary>
    /// Sends a password reset email
    /// </summary>
    /// <param name="toEmail">Recipient email address</param>
    /// <param name="userName">User's name for personalization</param>
    /// <param name="resetToken">Password reset token</param>
    /// <param name="baseUrl">Base URL for the application</param>
    /// <returns>True if email was sent successfully</returns>
    Task<bool> SendPasswordResetEmailAsync(string toEmail, string userName, string resetToken, string baseUrl);

    /// <summary>
    /// Sends a generic email with subject and message
    /// </summary>
    /// <param name="toEmail">Recipient email address</param>
    /// <param name="subject">Email subject</param>
    /// <param name="message">Email message content</param>
    /// <returns>True if email was sent successfully</returns>
    Task<bool> SendEmailAsync(string toEmail, string subject, string message);
}