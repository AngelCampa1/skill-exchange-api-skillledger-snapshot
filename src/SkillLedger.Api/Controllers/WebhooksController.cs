using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using SkillLedger.Api.Attributes;
using SkillLedger.Core.Interfaces;

namespace SkillLedger.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class WebhooksController : ControllerBase
{
    private readonly IEmailService _emailService;
    private readonly ILogger<WebhooksController> _logger;
    private readonly IConfiguration _configuration;

    // Tolerance for timestamp validation (5 minutes)
    private const int TimestampToleranceSeconds = 300;

    // Allowed domains for this webhook handler (shared Resend account filters by recipient domain)
    private static readonly HashSet<string> SkillLedgerDomains = new(StringComparer.OrdinalIgnoreCase)
    {
        "skillledger.app"
    };

    public WebhooksController(
        IEmailService emailService,
        ILogger<WebhooksController> logger,
        IConfiguration configuration)
    {
        _emailService = emailService;
        _logger = logger;
        _configuration = configuration;
    }

    /// <summary>
    /// Webhook endpoint for receiving inbound emails from Resend and forwarding them to a configured email address.
    /// Requires webhook signature verification using Svix format.
    /// </summary>
    /// <returns>Status indicating whether the email was forwarded successfully</returns>
    [HttpPost("resend/inbound")]
    [EnableRateLimiting("GeneralApiPolicy")]
    [SkillLedger.Api.Attributes.IgnoreAntiforgeryToken]
    public async Task<IActionResult> HandleInboundEmail()
    {
        try
        {
            // Read raw body for signature verification
            Request.EnableBuffering();
            using var reader = new StreamReader(Request.Body, Encoding.UTF8, leaveOpen: true);
            var rawBody = await reader.ReadToEndAsync();
            Request.Body.Position = 0;

            var webhookSecret = _configuration["Resend:WebhookSecret"];
            if (string.IsNullOrWhiteSpace(webhookSecret))
            {
                _logger.LogError("Resend webhook secret is not configured; rejecting webhook");
                return Unauthorized(new { error = "Webhook signature verification is not configured" });
            }

            var signatureResult = VerifyWebhookSignature(rawBody, webhookSecret);
            if (!signatureResult.IsValid)
            {
                _logger.LogWarning("Invalid webhook signature: {Reason}", signatureResult.Reason);
                return Unauthorized(new { error = "Invalid webhook signature", reason = signatureResult.Reason });
            }
            _logger.LogDebug("Webhook signature verified successfully");

            // Deserialize payload
            var payload = JsonSerializer.Deserialize<ResendInboundPayload>(rawBody, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (payload == null)
            {
                _logger.LogWarning("Received null payload from Resend webhook");
                return BadRequest(new { error = "Payload is required" });
            }

            // Filter: only process emails to SkillLedger domains (shared Resend account)
            if (!IsSkillLedgerRecipient(payload.To))
            {
                _logger.LogDebug(
                    "Ignoring email to non-SkillLedger domain: {To}",
                    payload.To);
                return Ok(new { status = "ignored", reason = "not_skilledger_domain" });
            }

            var forwardTo = _configuration["EmailForwarding:ForwardTo"]
                ?? "support@skillledger.app";

            var enabled = _configuration.GetValue<bool>("EmailForwarding:Enabled", true);
            if (!enabled)
            {
                _logger.LogInformation("Email forwarding is disabled, ignoring inbound email");
                return Ok(new { status = "disabled" });
            }

            var subject = $"[FWD from {System.Web.HttpUtility.HtmlEncode(payload.To)}] {System.Web.HttpUtility.HtmlEncode(payload.Subject)}";
            var body = $"<div style='font-family: Arial, sans-serif;'>" +
                       $"<div style='background: #f0f0f0; padding: 10px; margin-bottom: 20px; border-left: 4px solid #0066cc;'>" +
                       $"<p style='margin: 5px 0;'><strong>Original From:</strong> {System.Web.HttpUtility.HtmlEncode(payload.From)}</p>" +
                       $"<p style='margin: 5px 0;'><strong>Original To:</strong> {System.Web.HttpUtility.HtmlEncode(payload.To)}</p>" +
                       $"<p style='margin: 5px 0;'><strong>Received:</strong> {payload.CreatedAt:yyyy-MM-dd HH:mm:ss UTC}</p>" +
                       $"</div>" +
                       $"<div style='margin-top: 20px;'><pre>{System.Web.HttpUtility.HtmlEncode(payload.Text ?? string.Empty)}</pre></div>" +
                       $"</div>";

            var success = await _emailService.SendEmailAsync(forwardTo, subject, body);

            if (success)
            {
                _logger.LogInformation(
                    "Successfully forwarded email from {From} (to {OriginalTo}) to {ForwardTo}",
                    payload.From, payload.To, forwardTo);
                return Ok(new { status = "forwarded", to = forwardTo });
            }
            else
            {
                _logger.LogError(
                    "Failed to forward email from {From} to {ForwardTo}",
                    payload.From, forwardTo);
                return StatusCode(500, new { error = "Failed to forward email" });
            }
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Invalid JSON payload in webhook request");
            return BadRequest(new { error = "Invalid JSON payload" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing inbound email webhook");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Verifies the webhook signature using Svix format (used by Resend).
    /// Headers: svix-id, svix-timestamp, svix-signature
    /// Signature format: v1,base64_encoded_hmac_sha256
    /// Message to sign: {id}.{timestamp}.{payload}
    /// </summary>
    private (bool IsValid, string? Reason) VerifyWebhookSignature(string payload, string secret)
    {
        // Get required headers
        var svixId = Request.Headers["svix-id"].FirstOrDefault();
        var svixTimestamp = Request.Headers["svix-timestamp"].FirstOrDefault();
        var svixSignature = Request.Headers["svix-signature"].FirstOrDefault();

        // Check for missing headers
        if (string.IsNullOrEmpty(svixId))
            return (false, "Missing svix-id header");
        if (string.IsNullOrEmpty(svixTimestamp))
            return (false, "Missing svix-timestamp header");
        if (string.IsNullOrEmpty(svixSignature))
            return (false, "Missing svix-signature header");

        // Validate timestamp (prevent replay attacks)
        if (!long.TryParse(svixTimestamp, out var timestamp))
            return (false, "Invalid svix-timestamp format");

        var webhookTime = DateTimeOffset.FromUnixTimeSeconds(timestamp);
        var now = DateTimeOffset.UtcNow;
        var timeDiff = Math.Abs((now - webhookTime).TotalSeconds);

        if (timeDiff > TimestampToleranceSeconds)
            return (false, $"Timestamp too old or in future: {timeDiff:F0}s difference");

        // Decode secret (Resend webhook secrets start with 'whsec_')
        var secretKey = secret;
        if (secretKey.StartsWith("whsec_"))
            secretKey = secretKey.Substring(6);

        byte[] secretBytes;
        try
        {
            secretBytes = Convert.FromBase64String(secretKey);
        }
        catch
        {
            // If not base64, use raw bytes
            secretBytes = Encoding.UTF8.GetBytes(secretKey);
        }

        // Build signed payload: {id}.{timestamp}.{payload}
        var signedPayload = $"{svixId}.{svixTimestamp}.{payload}";
        var signedPayloadBytes = Encoding.UTF8.GetBytes(signedPayload);

        // Compute HMAC-SHA256 signature
        using var hmac = new HMACSHA256(secretBytes);
        var expectedSignature = Convert.ToBase64String(hmac.ComputeHash(signedPayloadBytes));

        // Parse signatures from header (format: v1,sig1 v1,sig2 ...)
        var signatures = svixSignature.Split(' ');
        foreach (var sig in signatures)
        {
            var parts = sig.Split(',', 2);
            if (parts.Length == 2 && parts[0] == "v1")
            {
                // Constant-time comparison to prevent timing attacks
                if (CryptographicOperations.FixedTimeEquals(
                    Encoding.UTF8.GetBytes(parts[1]),
                    Encoding.UTF8.GetBytes(expectedSignature)))
                {
                    return (true, null);
                }
            }
        }

        return (false, "Signature mismatch");
    }

    /// <summary>
    /// Check if the recipient email address is for a SkillLedger domain.
    /// Used to filter shared Resend account webhooks to only process our emails.
    /// </summary>
    private static bool IsSkillLedgerRecipient(string? toAddress)
    {
        if (string.IsNullOrEmpty(toAddress))
            return false;

        // Extract domain from email address
        var atIndex = toAddress.LastIndexOf('@');
        if (atIndex <= 0 || atIndex >= toAddress.Length - 1)
            return false;

        var domain = toAddress.Substring(atIndex + 1);
        return SkillLedgerDomains.Contains(domain);
    }
}

/// <summary>
/// Payload structure for inbound emails from Resend
/// </summary>
public class ResendInboundPayload
{
    /// <summary>
    /// The sender's email address
    /// </summary>
    public string From { get; set; } = string.Empty;

    /// <summary>
    /// The recipient email address
    /// </summary>
    public string To { get; set; } = string.Empty;

    /// <summary>
    /// The email subject line
    /// </summary>
    public string Subject { get; set; } = string.Empty;

    /// <summary>
    /// The HTML content of the email (if available)
    /// </summary>
    public string? Html { get; set; }

    /// <summary>
    /// The plain text content of the email
    /// </summary>
    public string? Text { get; set; }

    /// <summary>
    /// Timestamp when the email was received by Resend
    /// </summary>
    public DateTime CreatedAt { get; set; }
}
