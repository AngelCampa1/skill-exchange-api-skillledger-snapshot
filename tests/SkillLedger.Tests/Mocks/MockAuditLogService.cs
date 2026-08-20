using Microsoft.EntityFrameworkCore;
using SkillLedger.Core.Entities;
using SkillLedger.Core.Interfaces;
using SkillLedger.Infrastructure.Data;

namespace SkillLedger.Tests.Mocks;

public class MockAuditLogService : IAuditLogService
{
    private readonly SkillLedgerDbContext _context;
    public List<MockAuditEvent> LoggedEvents { get; } = new();

    public MockAuditLogService(SkillLedgerDbContext context)
    {
        _context = context;
    }

    public async Task LogEventAsync(Guid? userId, string action, string ipAddress, string? userAgent, bool success, string? details = null, string? errorMessage = null)
    {
        // Store in memory for test verification
        LoggedEvents.Add(new MockAuditEvent
        {
            UserId = userId,
            Action = action,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            Success = success,
            Details = details,
            ErrorMessage = errorMessage,
            Timestamp = DateTime.UtcNow
        });

        // Also save to database for tests that check database records
        try
        {
            var auditLog = new AuditLog
            {
                UserId = userId,
                Action = action,
                IPAddress = ipAddress,
                UserAgent = userAgent,
                Success = success,
                Details = details,
                ErrorMessage = errorMessage,
                Timestamp = DateTime.UtcNow
            };

            _context.AuditLogs.Add(auditLog);
            await _context.SaveChangesAsync();
        }
        catch (Exception)
        {
            // Ignore database save errors for test environment
        }
    }

    public Task<List<AuditLog>> GetUserAuditLogsAsync(Guid userId, int pageNumber = 1, int pageSize = 50)
    {
        var logs = LoggedEvents
            .Where(e => e.UserId == userId)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(e => new AuditLog
            {
                Id = Guid.NewGuid(),
                UserId = e.UserId,
                Action = e.Action,
                IPAddress = e.IpAddress,
                UserAgent = e.UserAgent,
                Success = e.Success,
                Details = e.Details,
                ErrorMessage = e.ErrorMessage,
                Timestamp = e.Timestamp
            })
            .ToList();

        return Task.FromResult(logs);
    }

    public Task<int> GetRecentFailedAttemptsAsync(string ipAddress, int hoursBack = 1)
    {
        var cutoff = DateTime.UtcNow.AddHours(-hoursBack);
        var count = LoggedEvents
            .Count(e => e.IpAddress == ipAddress && !e.Success && e.Timestamp >= cutoff);

        return Task.FromResult(count);
    }
}

public class MockAuditEvent
{
    public Guid? UserId { get; set; }
    public required string Action { get; set; }
    public required string IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public bool Success { get; set; }
    public string? Details { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime Timestamp { get; set; }
}