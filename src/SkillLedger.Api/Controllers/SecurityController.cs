using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Logging;
using SkillLedger.Core.Entities;
using SkillLedger.Core.Interfaces;
using SkillLedger.Infrastructure.Data;
using SkillLedger.Infrastructure.Services;
using System.Security.Cryptography;
using System.Security.Claims;
using System.Text;

namespace SkillLedger.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
[EnableRateLimiting("SecurityPolicy")]
public class SecurityController : ControllerBase
{
    private readonly ILogger<SecurityController> _logger;
    private readonly IAuditLogService _auditLogService;
    private readonly IEncryptionService _encryptionService;
    private readonly SkillLedgerDbContext _context;

    public SecurityController(
        ILogger<SecurityController> logger,
        IAuditLogService auditLogService,
        IEncryptionService encryptionService,
        SkillLedgerDbContext context)
    {
        _logger = logger;
        _auditLogService = auditLogService;
        _encryptionService = encryptionService;
        _context = context;
    }

    /// <summary>
    /// Get current user's security settings
    /// </summary>
    [HttpGet("settings")]
    public async Task<IActionResult> GetSecuritySettings()
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var userGuid))
            {
                return Unauthorized();
            }

            var settings = new
            {
                ActiveSessions = 1,
                DataRetentionDays = 365
            };

            // BUG-NEW-009 FIX: Add null-coalescing for nullable ipAddress
            await _auditLogService.LogEventAsync(
                userGuid,
                "SECURITY_SETTINGS_ACCESSED",
                HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown",
                Request.Headers["User-Agent"].ToString(),
                true,
                "User accessed security settings"
            );

            return Ok(settings);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving security settings");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Update user's privacy consent (GDPR compliance)
    /// </summary>
    [HttpPost("consent")]
    public async Task<IActionResult> UpdateConsent([FromBody] ConsentRequest request)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var userGuid))
            {
                return Unauthorized();
            }

            if (request == null)
            {
                return BadRequest(new { error = "Invalid consent data" });
            }

            // Log consent change for GDPR compliance
            // BUG-NEW-009 FIX: Add null-coalescing for nullable ipAddress
            await _auditLogService.LogEventAsync(
                userGuid,
                "PRIVACY_CONSENT_UPDATED",
                HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown",
                Request.Headers["User-Agent"].ToString(),
                false,
                $"GDPR consent update requested but not persisted — feature pending implementation: ConsentGiven={request.ConsentGiven}, Purpose={request.Purpose}"
            );

            // BUG-33 FIX: IUserService has no UpdateConsentAsync method and no dedicated
            // GdprConsent table exists yet. Returning 501 so callers know the record was
            // NOT saved rather than silently succeeding with fabricated data.
            return StatusCode(501, new
            {
                error = "GDPR consent persistence is not yet implemented.",
                detail = "The consent update has been audit-logged but was not saved to the database. This feature requires a dedicated consent table and UpdateConsentAsync on IUserService."
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating privacy consent");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Request data export (GDPR Right to Data Portability)
    /// </summary>
    [HttpPost("data-export")]
    public async Task<IActionResult> RequestDataExport()
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var userGuid))
            {
                return Unauthorized();
            }

            var exportRequestId = Guid.NewGuid();
            var now = DateTime.UtcNow;
            var ipAddress = GetClientIpAddress();
            var userAgent = Request.Headers.UserAgent.ToString();

            var exportRequest = new PrivacyRequest
            {
                Id = exportRequestId,
                UserId = userGuid,
                RequestType = "DataExport",
                Status = "Processing",
                RequestedAt = now,
                DueAt = now.AddHours(24),
                RequestedFromIp = ipAddress,
                UserAgent = userAgent,
                ConfirmationRequired = false
            };

            _context.PrivacyRequests.Add(exportRequest);
            await _context.SaveChangesAsync();

            await _auditLogService.LogEventAsync(
                userGuid,
                "DATA_EXPORT_REQUESTED",
                ipAddress,
                userAgent,
                true,
                $"Data export requested: {exportRequestId}"
            );

            var response = new
            {
                ExportRequestId = exportRequestId,
                UserId = userGuid,
                exportRequest.Status,
                EstimatedCompletion = exportRequest.DueAt,
                Format = "JSON",
                Includes = new string[]
                {
                    "Profile", "Projects", "Transactions", "Reviews",
                    "Messages", "Documents", "AuditLogs"
                }
            };

            return Accepted(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing data export request");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Request account deletion (GDPR Right to Erasure)
    /// </summary>
    [HttpPost("account-deletion")]
    public async Task<IActionResult> RequestAccountDeletion([FromBody] AccountDeletionRequest request)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var userGuid))
            {
                return Unauthorized();
            }

            if (request == null || string.IsNullOrEmpty(request.Reason))
            {
                return BadRequest(new { error = "Deletion reason is required" });
            }

            var deletionRequestId = Guid.NewGuid();
            var now = DateTime.UtcNow;
            var confirmationToken = Guid.NewGuid().ToString("N");
            var ipAddress = GetClientIpAddress();
            var userAgent = Request.Headers.UserAgent.ToString();
            var encryptedReason = await _encryptionService.EncryptAsync(request.Reason);
            var encryptedNotes = string.IsNullOrWhiteSpace(request.AdditionalNotes)
                ? null
                : await _encryptionService.EncryptAsync(request.AdditionalNotes);

            var deletionRequest = new PrivacyRequest
            {
                Id = deletionRequestId,
                UserId = userGuid,
                RequestType = "AccountDeletion",
                Status = "Pending",
                RequestedAt = now,
                DueAt = now.AddDays(30),
                RequestedFromIp = ipAddress,
                UserAgent = userAgent,
                EncryptedReason = encryptedReason,
                EncryptedAdditionalNotes = encryptedNotes,
                ConfirmationRequired = true,
                ConfirmationTokenHash = HashConfirmationToken(confirmationToken)
            };

            _context.PrivacyRequests.Add(deletionRequest);
            await _context.SaveChangesAsync();

            await _auditLogService.LogEventAsync(
                userGuid,
                "ACCOUNT_DELETION_REQUESTED",
                ipAddress,
                userAgent,
                true,
                $"Account deletion requested: {deletionRequestId}, Reason: {request.Reason}"
            );

            var response = new
            {
                DeletionRequestId = deletionRequestId,
                UserId = userGuid,
                deletionRequest.Status,
                GracePeriodEnds = deletionRequest.DueAt,
                Reason = encryptedReason,
                deletionRequest.ConfirmationRequired,
                ConfirmationToken = confirmationToken[..8]
            };

            return Accepted(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing account deletion request");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Get security audit log for current user
    /// </summary>
    [HttpGet("audit-log")]
    public async Task<IActionResult> GetAuditLog([FromQuery] int page = 1, [FromQuery] int pageSize = 50)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var userGuid))
            {
                return Unauthorized();
            }

            var auditLogs = await _auditLogService.GetUserAuditLogsAsync(userGuid, page, pageSize);

            await _auditLogService.LogEventAsync(
                userGuid,
                "AUDIT_LOG_ACCESSED",
                HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown",
                Request.Headers["User-Agent"].ToString(),
                true,
                "User accessed their audit log"
            );

            return Ok(new
            {
                Logs = auditLogs.Select(log => new
                {
                    Id = log.Id,
                    Event = log.Action,
                    Timestamp = log.Timestamp,
                    IpAddress = log.IPAddress,
                    UserAgent = log.UserAgent,
                    Success = log.Success,
                    Details = log.Details
                }),
                Page = page,
                PageSize = pageSize,
                TotalCount = auditLogs.Count
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving audit log");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Report security incident
    /// </summary>
    [HttpPost("report-incident")]
    public async Task<IActionResult> ReportSecurityIncident([FromBody] SecurityIncidentReport incident)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var userGuid))
            {
                return Unauthorized();
            }

            if (incident == null || string.IsNullOrEmpty(incident.Description))
            {
                return BadRequest(new { error = "Incident description is required" });
            }

            var incidentId = Guid.NewGuid();

            // BUG-NEW-009 FIX: Add null-coalescing for nullable ipAddress
            await _auditLogService.LogEventAsync(
                userGuid,
                "SECURITY_INCIDENT_REPORTED",
                HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown",
                Request.Headers["User-Agent"].ToString(),
                true,
                $"Security incident reported: {incidentId}, Type: {incident.Type}"
            );

            // In production, notify security team
            var response = new
            {
                IncidentId = incidentId,
                ReporterId = userGuid,
                Type = incident.Type,
                Severity = incident.Severity,
                Description = await _encryptionService.EncryptAsync(incident.Description),
                ReportedAt = DateTime.UtcNow,
                Status = "Under Investigation",
                ReferenceNumber = $"INC-{DateTime.UtcNow:yyyyMMdd}-{incidentId.ToString("N")[..8].ToUpper()}"
            };

            return Accepted(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reporting security incident");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    private string GetClientIpAddress()
    {
        return TrustedClientIpResolver.GetClientIpAddress(HttpContext, "Unknown");
    }

    private static string HashConfirmationToken(string token)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(hash);
    }
}

public class ConsentRequest
{
    public bool ConsentGiven { get; set; }
    public string Purpose { get; set; } = string.Empty;
    public string? ConsentText { get; set; }
}

public class AccountDeletionRequest
{
    public string Reason { get; set; } = string.Empty;
    public bool ConfirmDataLoss { get; set; }
    public string? AdditionalNotes { get; set; }
}

public class SecurityIncidentReport
{
    public string Type { get; set; } = string.Empty;
    public string Severity { get; set; } = "Medium";
    public string Description { get; set; } = string.Empty;
    public DateTime? IncidentDate { get; set; }
    public string? AffectedData { get; set; }
    public bool ImmediateActionRequired { get; set; }
}
