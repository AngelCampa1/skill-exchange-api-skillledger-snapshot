using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SkillLedger.Core.DTOs;
using SkillLedger.Core.Entities;
using SkillLedger.Core.Enums;
using SkillLedger.Core.Interfaces;
using SkillLedger.Infrastructure.Data;
using System.Text;

namespace SkillLedger.Infrastructure.Services;

public class ProjectApplicationService : IProjectApplicationService
{
    private readonly SkillLedgerDbContext _context;
    private readonly ILogger<ProjectApplicationService> _logger;
    private readonly IAuditLogService _auditLogService;
    private readonly IEmailService _emailService;

    public ProjectApplicationService(
        SkillLedgerDbContext context,
        ILogger<ProjectApplicationService> logger,
        IAuditLogService auditLogService,
        IEmailService emailService)
    {
        _context = context;
        _logger = logger;
        _auditLogService = auditLogService;
        _emailService = emailService;
    }

    public async Task<ServiceResponseDto> SubmitApplicationAsync(CreateProjectApplicationDto createDto, Guid providerId, string ipAddress)
    {
        try
        {
            _logger.LogInformation("Provider {ProviderId} submitting application for project {ProjectId}",
                providerId, createDto.ProjectId);

            // Validate that the project exists and is accepting applications
            var project = await _context.Projects
                .Include(p => p.Client)
                .FirstOrDefaultAsync(p => p.Id == createDto.ProjectId);

            if (project == null)
            {
                return new ServiceResponseDto
                {
                    Success = false,
                    Message = "Project not found or no longer available."
                };
            }

            // Check if project is in correct status
            if (project.Status != ProjectStatus.Published)
            {
                return new ServiceResponseDto
                {
                    Success = false,
                    Message = "This project is not currently accepting applications."
                };
            }

            // Check if provider can apply
            var canApply = await CanProviderApplyToProjectAsync(createDto.ProjectId, providerId);
            if (!canApply)
            {
                return new ServiceResponseDto
                {
                    Success = false,
                    Message = "You have already applied to this project or are not eligible to apply."
                };
            }

            // Calculate skill match score
            var skillMatchScore = await CalculateSkillMatchScoreAsync(createDto.ProjectId, providerId);

            // Create the application
            var application = new ProjectApplication
            {
                ProjectId = createDto.ProjectId,
                ProviderId = providerId,
                CoverLetter = createDto.CoverLetter,
                ProposedTimeline = createDto.ProposedTimeline,
                IsAvailableImmediately = createDto.IsAvailableImmediately,
                ProposedBudget = createDto.ProposedBudget,
                SkillMatchScore = skillMatchScore,
                Status = ApplicationStatus.Pending,
                SubmittedFromIP = ipAddress
            };

            // Create attachments if any
            if (createDto.Attachments != null && createDto.Attachments.Any())
            {
                foreach (var attachmentDto in createDto.Attachments)
                {
                    var attachment = new ProjectApplicationAttachment
                    {
                        ProjectApplicationId = application.Id,
                        FileName = attachmentDto.FileName,
                        ContentType = attachmentDto.ContentType,
                        FileSize = attachmentDto.FileSize,
                        StorageUrl = attachmentDto.StorageUrl,
                        Description = attachmentDto.Description,
                        IsVirusScanned = false, // Will be scanned asynchronously
                        IsSafe = false
                    };
                    application.Attachments.Add(attachment);
                }
            }

            _context.ProjectApplications.Add(application);
            await _context.SaveChangesAsync();

            // Log the application submission
            await _auditLogService.LogEventAsync(
                providerId,
                "PROJECT_APPLICATION_SUBMITTED",
                ipAddress,
                string.Empty,
                true,
                $"{{\"ApplicationId\":\"{application.Id}\",\"ProjectId\":\"{createDto.ProjectId}\",\"SkillMatchScore\":{skillMatchScore}}}",
                "Project application submitted successfully"
            );

            // Send notification to project owner (async)
            _ = Task.Run(async () =>
            {
                try
                {
                    // BUG-BE-003 FIX: Add error handling to prevent silent failures
                    await SendNewApplicationNotificationAsync(application.Id);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send new application notification for application {ApplicationId}, project {ProjectId}", application.Id, createDto.ProjectId);
                }
            });

            _logger.LogInformation("Application {ApplicationId} submitted successfully for project {ProjectId} by provider {ProviderId}",
                application.Id, createDto.ProjectId, providerId);

            return new ServiceResponseDto
            {
                Success = true,
                Message = "Your application has been submitted successfully. You will be notified when the client reviews your application.",
                Data = application.Id
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error submitting application for project {ProjectId} by provider {ProviderId}",
                createDto.ProjectId, providerId);

            await _auditLogService.LogEventAsync(
                providerId,
                "PROJECT_APPLICATION_SUBMISSION_FAILED",
                ipAddress,
                string.Empty,
                false,
                $"{{\"ProjectId\":\"{createDto.ProjectId}\",\"Error\":\"{ex.Message}\"}}",
                "Project application submission failed"
            );

            return new ServiceResponseDto
            {
                Success = false,
                Message = "An error occurred while submitting your application. Please try again."
            };
        }
    }

    public async Task<ProjectApplicationDto?> GetApplicationByIdAsync(Guid applicationId, Guid requestingUserId)
    {
        try
        {
            // PERFORMANCE FIX: Use AsSplitQuery to prevent cartesian explosion
            var application = await _context.ProjectApplications
                .Include(pa => pa.Project)
                    .ThenInclude(p => p.Client)
                        .ThenInclude(c => c.Profile)
                .Include(pa => pa.Provider)
                    .ThenInclude(p => p.Profile)
                .Include(pa => pa.Attachments)
                .AsSplitQuery()
                .FirstOrDefaultAsync(pa => pa.Id == applicationId);

            if (application == null)
                return null;

            // Check authorization
            var hasAccess = await HasUserAccessToApplicationAsync(applicationId, requestingUserId);
            if (!hasAccess)
                return null;

            return MapToDto(application);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving application {ApplicationId} for user {UserId}",
                applicationId, requestingUserId);
            return null;
        }
    }

    public async Task<ApplicationSearchResultDto> GetProjectApplicationsAsync(Guid projectId, Guid clientId, ApplicationSearchDto searchDto)
    {
        try
        {
            // Verify the user owns the project
            var project = await _context.Projects.FirstOrDefaultAsync(p => p.Id == projectId && p.ClientId == clientId);
            if (project == null)
            {
                return new ApplicationSearchResultDto();
            }

            // PERFORMANCE FIX: Use AsSplitQuery to prevent cartesian explosion
            var query = _context.ProjectApplications
                .Include(pa => pa.Project)
                    .ThenInclude(p => p.Client)
                        .ThenInclude(c => c.Profile)
                .Include(pa => pa.Provider)
                    .ThenInclude(p => p.Profile)
                .Include(pa => pa.Attachments)
                .AsSplitQuery()
                .Where(pa => pa.ProjectId == projectId);

            return await ExecuteApplicationSearchAsync(query, searchDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving applications for project {ProjectId} by client {ClientId}",
                projectId, clientId);
            return new ApplicationSearchResultDto();
        }
    }

    public async Task<ApplicationSearchResultDto> GetProviderApplicationsAsync(Guid providerId, ApplicationSearchDto searchDto)
    {
        try
        {
            // PERFORMANCE FIX: Use AsSplitQuery to prevent cartesian explosion
            var query = _context.ProjectApplications
                .Include(pa => pa.Project)
                    .ThenInclude(p => p.Client)
                        .ThenInclude(c => c.Profile)
                .Include(pa => pa.Provider)
                    .ThenInclude(p => p.Profile)
                .Include(pa => pa.Attachments)
                .AsSplitQuery()
                .Where(pa => pa.ProviderId == providerId);

            return await ExecuteApplicationSearchAsync(query, searchDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving applications for provider {ProviderId}", providerId);
            return new ApplicationSearchResultDto();
        }
    }

    public async Task<ServiceResponseDto> UpdateApplicationStatusAsync(Guid applicationId, UpdateApplicationStatusDto updateDto, Guid clientId, string ipAddress)
    {
        try
        {
            // PERFORMANCE FIX: Use AsSplitQuery to prevent cartesian explosion
            var application = await _context.ProjectApplications
                .Include(pa => pa.Project)
                .Include(pa => pa.Provider)
                .AsSplitQuery()
                .FirstOrDefaultAsync(pa => pa.Id == applicationId);

            if (application == null)
            {
                return new ServiceResponseDto
                {
                    Success = false,
                    Message = "Application not found."
                };
            }

            // Verify the client owns the project
            if (application.Project.ClientId != clientId)
            {
                return new ServiceResponseDto
                {
                    Success = false,
                    Message = "You don't have permission to update this application."
                };
            }

            // Parse and validate the new status
            if (!Enum.TryParse<ApplicationStatus>(updateDto.Status, out var newStatus))
            {
                return new ServiceResponseDto
                {
                    Success = false,
                    Message = "Invalid application status."
                };
            }

            var oldStatus = application.Status;
            application.Status = newStatus;
            application.ClientFeedback = updateDto.ClientFeedback;
            application.ReviewedAt = DateTime.UtcNow;
            application.UpdatedAt = DateTime.UtcNow;

            // If accepting an application, create workspace and update project status
            if (newStatus == ApplicationStatus.Accepted && oldStatus != ApplicationStatus.Accepted)
            {
                // Reject all other pending applications for this project
                var otherApplications = await _context.ProjectApplications
                    .Where(pa => pa.ProjectId == application.ProjectId &&
                                 pa.Id != applicationId &&
                                 pa.Status == ApplicationStatus.Pending)
                    .ToListAsync();

                foreach (var otherApp in otherApplications)
                {
                    otherApp.Status = ApplicationStatus.Rejected;
                    otherApp.ClientFeedback = "Another provider was selected for this project.";
                    otherApp.ReviewedAt = DateTime.UtcNow;
                    otherApp.UpdatedAt = DateTime.UtcNow;
                }

                // Create workspace for the project
                var workspace = new Core.Entities.ProjectWorkspace
                {
                    Id = Guid.NewGuid(),
                    ProjectId = application.ProjectId,
                    ClientId = application.Project.ClientId,
                    ProviderId = application.ProviderId,
                    Status = Core.Enums.WorkspaceStatus.Active,
                    CreatedAt = DateTime.UtcNow
                };

                _context.ProjectWorkspaces.Add(workspace);

                // Update project status to InProgress
                application.Project.Status = Core.Enums.ProjectStatus.InProgress;

                _logger.LogInformation("Created workspace {WorkspaceId} for project {ProjectId} with provider {ProviderId}",
                    workspace.Id, application.ProjectId, application.ProviderId);
            }

            await _context.SaveChangesAsync();

            // Log the status update
            await _auditLogService.LogEventAsync(
                clientId,
                "PROJECT_APPLICATION_STATUS_UPDATED",
                ipAddress,
                string.Empty,
                true,
                $"{{\"ApplicationId\":\"{applicationId}\",\"OldStatus\":\"{oldStatus}\",\"NewStatus\":\"{newStatus}\",\"ProviderId\":\"{application.ProviderId}\"}}",
                "Application status updated"
            );

            // Send notification to provider (async)
            _ = Task.Run(async () =>
            {
                try
                {
                    // BUG-BE-003 FIX: Add error handling to prevent silent failures
                    await SendApplicationStatusNotificationAsync(applicationId, newStatus.ToString(), updateDto.ClientFeedback);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send application status notification for application {ApplicationId}, status {Status}, provider {ProviderId}",
                        applicationId, newStatus, application.ProviderId);
                }
            });

            _logger.LogInformation("Application {ApplicationId} status updated from {OldStatus} to {NewStatus} by client {ClientId}",
                applicationId, oldStatus, newStatus, clientId);

            return new ServiceResponseDto
            {
                Success = true,
                Message = "Application status updated successfully."
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating application {ApplicationId} status by client {ClientId}",
                applicationId, clientId);

            return new ServiceResponseDto
            {
                Success = false,
                Message = "An error occurred while updating the application status. Please try again."
            };
        }
    }

    public async Task<ServiceResponseDto> WithdrawApplicationAsync(Guid applicationId, Guid providerId, string? reason, string ipAddress)
    {
        try
        {
            var application = await _context.ProjectApplications
                .Include(pa => pa.Project)
                .FirstOrDefaultAsync(pa => pa.Id == applicationId && pa.ProviderId == providerId);

            if (application == null)
            {
                return new ServiceResponseDto
                {
                    Success = false,
                    Message = "Application not found or you don't have permission to withdraw it."
                };
            }

            if (!application.CanBeWithdrawn)
            {
                return new ServiceResponseDto
                {
                    Success = false,
                    Message = "This application cannot be withdrawn at this time."
                };
            }

            var oldStatus = application.Status;
            application.Status = ApplicationStatus.Withdrawn;
            application.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // Log the withdrawal
            await _auditLogService.LogEventAsync(
                providerId,
                "PROJECT_APPLICATION_WITHDRAWN",
                ipAddress,
                string.Empty,
                true,
                $"{{\"ApplicationId\":\"{applicationId}\",\"ProjectId\":\"{application.ProjectId}\",\"Reason\":\"{reason ?? "No reason provided"}\"}}",
                "Application withdrawn by provider"
            );

            _logger.LogInformation("Application {ApplicationId} withdrawn by provider {ProviderId}",
                applicationId, providerId);

            return new ServiceResponseDto
            {
                Success = true,
                Message = "Your application has been withdrawn successfully."
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error withdrawing application {ApplicationId} by provider {ProviderId}",
                applicationId, providerId);

            return new ServiceResponseDto
            {
                Success = false,
                Message = "An error occurred while withdrawing your application. Please try again."
            };
        }
    }

    public async Task<decimal> CalculateSkillMatchScoreAsync(Guid projectId, Guid providerId)
    {
        try
        {
            // Get project required skills
            var projectSkills = await _context.ProjectSkills
                .Include(ps => ps.Skill)
                .Where(ps => ps.ProjectId == projectId)
                .ToListAsync();

            if (!projectSkills.Any())
                return 0.5m; // Default score when no skills specified

            // Get provider skills
            var providerSkills = await _context.UserSkills
                .Include(us => us.Skill)
                .Where(us => us.UserId == providerId)
                .ToListAsync();

            if (!providerSkills.Any())
                return 0.0m; // No skills match

            // Calculate match score
            var totalWeight = projectSkills.Sum(ps => ps.Weight);
            decimal weightedMatches = 0m;

            foreach (var projectSkill in projectSkills)
            {
                var providerSkill = providerSkills.FirstOrDefault(us => us.SkillId == projectSkill.SkillId);
                if (providerSkill != null)
                {
                    // Base match score
                    var baseScore = 1.0m;
                    var weight = projectSkill.Weight;

                    // Adjust score based on proficiency match
                    var requiredLevel = (int)projectSkill.ProficiencyRequired;
                    var providerLevel = (int)providerSkill.Proficiency;

                    if (providerLevel >= requiredLevel)
                    {
                        baseScore = 1.0m; // Perfect match or exceed requirements
                    }
                    else
                    {
                        // Partial score for lower proficiency
                        baseScore = (decimal)providerLevel / requiredLevel * 0.8m;
                    }

                    weightedMatches += baseScore * weight;
                }
            }

            var finalScore = Math.Min(1.0m, weightedMatches / totalWeight);

            _logger.LogDebug("Calculated skill match score {Score} for provider {ProviderId} and project {ProjectId}",
                finalScore, providerId, projectId);

            return Math.Round(finalScore, 2);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating skill match score for provider {ProviderId} and project {ProjectId}",
                providerId, projectId);
            return 0.0m;
        }
    }

    public async Task<ApplicationStatisticsDto> GetProviderApplicationStatisticsAsync(Guid providerId)
    {
        try
        {
            // PERFORMANCE FIX: Use database-side aggregations instead of loading all applications into memory
            var baseQuery = _context.ProjectApplications
                .AsNoTracking()
                .Where(pa => pa.ProviderId == providerId);

            var stats = new ApplicationStatisticsDto
            {
                TotalApplications = await baseQuery.CountAsync(),
                ApplicationsByStatus = await baseQuery
                    .GroupBy(pa => pa.Status)
                    .Select(g => new { Status = g.Key.ToString(), Count = g.Count() })
                    .ToDictionaryAsync(x => x.Status, x => x.Count),
                AverageSkillMatchScore = (decimal?)(await baseQuery
                    .Where(pa => pa.SkillMatchScore.HasValue)
                    .AverageAsync(pa => (double?)pa.SkillMatchScore)),
                ApplicationsThisMonth = await baseQuery
                    .Where(pa => pa.CreatedAt >= DateTime.UtcNow.AddMonths(-1))
                    .CountAsync()
            };

            // Calculate success rate (database-side)
            var totalApplications = stats.TotalApplications;
            var acceptedApplications = await baseQuery
                .Where(pa => pa.Status == ApplicationStatus.Accepted)
                .CountAsync();
            stats.SuccessRate = totalApplications > 0 ? (decimal)acceptedApplications / totalApplications : 0m;

            // Calculate average response time for reviewed applications (database-side)
            var avgResponseTime = await baseQuery
                .Where(pa => pa.ReviewedAt.HasValue)
                .Select(pa => (pa.ReviewedAt!.Value - pa.CreatedAt).Days)
                .AverageAsync();

            if (avgResponseTime > 0)
            {
                stats.AverageResponseTimeDays = avgResponseTime;
            }

            return stats;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting application statistics for provider {ProviderId}", providerId);
            return new ApplicationStatisticsDto();
        }
    }

    public async Task<ApplicationStatisticsDto> GetClientApplicationStatisticsAsync(Guid clientId)
    {
        try
        {
            // PERFORMANCE FIX: Use database-side aggregations + AsNoTracking instead of loading all applications
            var baseQuery = _context.ProjectApplications
                .AsNoTracking()
                .Where(pa => pa.Project.ClientId == clientId);

            var stats = new ApplicationStatisticsDto
            {
                TotalApplications = await baseQuery.CountAsync(),
                ApplicationsByStatus = await baseQuery
                    .GroupBy(pa => pa.Status)
                    .Select(g => new { Status = g.Key.ToString(), Count = g.Count() })
                    .ToDictionaryAsync(x => x.Status, x => x.Count),
                AverageSkillMatchScore = (decimal?)(await baseQuery
                    .Where(pa => pa.SkillMatchScore.HasValue)
                    .AverageAsync(pa => (double?)pa.SkillMatchScore)),
                ApplicationsThisMonth = await baseQuery
                    .Where(pa => pa.CreatedAt >= DateTime.UtcNow.AddMonths(-1))
                    .CountAsync()
            };

            // For clients, success rate is percentage of applications they accept (database-side)
            var totalApplications = stats.TotalApplications;
            var acceptedApplications = await baseQuery
                .Where(pa => pa.Status == ApplicationStatus.Accepted)
                .CountAsync();
            stats.SuccessRate = totalApplications > 0 ? (decimal)acceptedApplications / totalApplications : 0m;

            return stats;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting application statistics for client {ClientId}", clientId);
            return new ApplicationStatisticsDto();
        }
    }

    public async Task<bool> CanProviderApplyToProjectAsync(Guid projectId, Guid providerId)
    {
        try
        {
            // PERFORMANCE FIX: Use AsNoTracking for read-only existence checks
            var existingApplication = await _context.ProjectApplications
                .AsNoTracking()
                .AnyAsync(pa => pa.ProjectId == projectId && pa.ProviderId == providerId);

            if (existingApplication)
                return false;

            // Check if provider is the project owner (can't apply to own project)
            var project = await _context.Projects
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == projectId);
            if (project == null || project.ClientId == providerId)
                return false;

            // Additional business rules can be added here
            // e.g., check if provider meets minimum requirements, is verified, etc.

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if provider {ProviderId} can apply to project {ProjectId}",
                providerId, projectId);
            return false;
        }
    }

    public async Task<List<ProjectSummaryDto>> GetRecommendedProjectsForProviderAsync(Guid providerId, int take = 10)
    {
        try
        {
            // PERFORMANCE FIX: Use AsNoTracking for read-only recommendation queries
            var providerSkills = await _context.UserSkills
                .AsNoTracking()
                .Where(us => us.UserId == providerId)
                .Select(us => us.SkillId)
                .ToListAsync();

            if (!providerSkills.Any())
                return new List<ProjectSummaryDto>();

            // Find projects that match provider skills and haven't been applied to
            var appliedProjectIds = await _context.ProjectApplications
                .AsNoTracking()
                .Where(pa => pa.ProviderId == providerId)
                .Select(pa => pa.ProjectId)
                .ToListAsync();

            // PERFORMANCE FIX: Use AsSplitQuery to prevent cartesian explosion
            var recommendedProjects = await _context.Projects
                .Include(p => p.Client)
                    .ThenInclude(c => c.Profile)
                .Include(p => p.Deliverables)
                .Include(p => p.ProjectSkills)
                    .ThenInclude(ps => ps.Skill)
                .AsSplitQuery()
                .Where(p => p.Status == ProjectStatus.Published &&
                           p.ClientId != providerId &&
                           !appliedProjectIds.Contains(p.Id) &&
                           p.ProjectSkills.Any(ps => providerSkills.Contains(ps.SkillId)))
                .OrderByDescending(p => p.ProjectSkills.Count(ps => providerSkills.Contains(ps.SkillId))) // Most matching skills first
                .ThenByDescending(p => p.CreatedAt)
                .Take(take)
                .ToListAsync();

            return recommendedProjects.Select(MapToSummaryDto).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting recommended projects for provider {ProviderId}", providerId);
            return new List<ProjectSummaryDto>();
        }
    }

    public async Task<int> ExpireOldApplicationsAsync(int expiredAfterDays = 30)
    {
        try
        {
            var cutoffDate = DateTime.UtcNow.AddDays(-expiredAfterDays);

            var oldApplications = await _context.ProjectApplications
                .Where(pa => pa.Status == ApplicationStatus.Pending &&
                           pa.CreatedAt < cutoffDate)
                .ToListAsync();

            foreach (var application in oldApplications)
            {
                application.Status = ApplicationStatus.Expired;
                application.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("Expired {Count} old applications that were older than {Days} days",
                oldApplications.Count, expiredAfterDays);

            return oldApplications.Count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error expiring old applications");
            return 0;
        }
    }

    public async Task<bool> SendApplicationStatusNotificationAsync(Guid applicationId, string newStatus, string? feedback)
    {
        try
        {
            // PERFORMANCE FIX: Use AsSplitQuery to prevent cartesian explosion
            var application = await _context.ProjectApplications
                .Include(pa => pa.Project)
                .Include(pa => pa.Provider)
                .AsSplitQuery()
                .FirstOrDefaultAsync(pa => pa.Id == applicationId);

            if (application?.Provider?.Email == null)
                return false;

            var subject = $"Update on your application for '{application.Project.Title}'";
            var message = BuildStatusNotificationMessage(application.Project.Title, newStatus, feedback);

            await _emailService.SendEmailAsync(application.Provider.Email, subject, message);

            _logger.LogInformation("Status notification sent for application {ApplicationId} to {Email}",
                applicationId, application.Provider.Email);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending application status notification for application {ApplicationId}",
                applicationId);
            return false;
        }
    }

    public async Task<ServiceResponseDto> ValidateApplicationRulesAsync(ProjectApplication application)
    {
        var errors = new List<string>();

        // Validate cover letter
        if (string.IsNullOrWhiteSpace(application.CoverLetter) || application.CoverLetter.Length < 100)
        {
            errors.Add("Cover letter must be at least 100 characters long.");
        }

        // Validate timeline
        if (application.ProposedTimeline.HasValue && (application.ProposedTimeline < 1 || application.ProposedTimeline > 365))
        {
            errors.Add("Proposed timeline must be between 1 and 365 days.");
        }

        // Validate budget
        if (application.ProposedBudget.HasValue && (application.ProposedBudget < 50 || application.ProposedBudget > 5000))
        {
            errors.Add("Proposed budget must be between 50 and 5000 credits.");
        }

        // Check if project exists and is accepting applications
        var project = await _context.Projects.FirstOrDefaultAsync(p => p.Id == application.ProjectId);
        if (project == null)
        {
            errors.Add("Project does not exist.");
        }
        else if (project.Status != ProjectStatus.Published)
        {
            errors.Add("Project is not currently accepting applications.");
        }

        return new ServiceResponseDto
        {
            Success = !errors.Any(),
            Message = errors.Any() ? string.Join(" ", errors) : "Validation passed."
        };
    }

    public async Task<bool> HasUserAccessToApplicationAsync(Guid applicationId, Guid userId)
    {
        try
        {
            var application = await _context.ProjectApplications
                .Include(pa => pa.Project)
                .FirstOrDefaultAsync(pa => pa.Id == applicationId);

            if (application == null)
                return false;

            // Provider can access their own applications
            if (application.ProviderId == userId)
                return true;

            // Client can access applications to their projects
            if (application.Project.ClientId == userId)
                return true;

            // LOW-PRIORITY FIX: Check for admin/moderator role using UserRoles join
            var hasAdminRole = await _context.UserRoles
                .Join(_context.Roles,
                    ur => ur.RoleId,
                    r => r.Id,
                    (ur, r) => new { ur.UserId, r.Name })
                .AnyAsync(ur => ur.UserId == userId && (ur.Name == "Admin" || ur.Name == "Moderator"));

            if (hasAdminRole)
            {
                _logger.LogInformation("Admin/Moderator user {UserId} accessing application {ApplicationId}", userId, applicationId);
                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking user access for application {ApplicationId} and user {UserId}",
                applicationId, userId);
            return false;
        }
    }

    #region Private Helper Methods

    private async Task<ApplicationSearchResultDto> ExecuteApplicationSearchAsync(IQueryable<ProjectApplication> query, ApplicationSearchDto searchDto)
    {
        // Apply filters
        if (searchDto.Status != null && searchDto.Status.Any())
        {
            var statusEnums = searchDto.Status
                .Where(s => Enum.TryParse<ApplicationStatus>(s, out _))
                .Select(s => Enum.Parse<ApplicationStatus>(s))
                .ToList();

            if (statusEnums.Any())
            {
                query = query.Where(pa => statusEnums.Contains(pa.Status));
            }
        }

        if (searchDto.MinSkillMatchScore.HasValue)
        {
            query = query.Where(pa => pa.SkillMatchScore >= searchDto.MinSkillMatchScore);
        }

        if (searchDto.SubmittedFrom.HasValue)
        {
            query = query.Where(pa => pa.CreatedAt >= searchDto.SubmittedFrom);
        }

        if (searchDto.SubmittedTo.HasValue)
        {
            query = query.Where(pa => pa.CreatedAt <= searchDto.SubmittedTo);
        }

        if (searchDto.AvailableImmediately.HasValue)
        {
            query = query.Where(pa => pa.IsAvailableImmediately == searchDto.AvailableImmediately);
        }

        // Get total count before pagination
        var totalCount = await query.CountAsync();

        // Apply sorting
        query = searchDto.SortBy?.ToLower() switch
        {
            "skillmatch" => searchDto.SortDirection?.ToLower() == "asc"
                ? query.OrderBy(pa => pa.SkillMatchScore ?? 0)
                : query.OrderByDescending(pa => pa.SkillMatchScore ?? 0),
            "status" => searchDto.SortDirection?.ToLower() == "asc"
                ? query.OrderBy(pa => pa.Status)
                : query.OrderByDescending(pa => pa.Status),
            _ => searchDto.SortDirection?.ToLower() == "asc"
                ? query.OrderBy(pa => pa.CreatedAt)
                : query.OrderByDescending(pa => pa.CreatedAt)
        };

        // Apply pagination
        var applications = await query
            .Skip(searchDto.Skip)
            .Take(searchDto.Take)
            .ToListAsync();

        // Calculate pagination info
        var currentPage = (searchDto.Skip / searchDto.Take) + 1;
        var totalPages = (int)Math.Ceiling((double)totalCount / searchDto.Take);

        return new ApplicationSearchResultDto
        {
            Applications = applications.Select(MapToDto).ToList(),
            TotalCount = totalCount,
            CurrentPage = currentPage,
            PageSize = searchDto.Take,
            TotalPages = totalPages,
            HasNextPage = currentPage < totalPages,
            HasPreviousPage = currentPage > 1
        };
    }

    private async Task SendNewApplicationNotificationAsync(Guid applicationId)
    {
        // PERFORMANCE FIX: Use AsSplitQuery to prevent cartesian explosion
        var application = await _context.ProjectApplications
            .Include(pa => pa.Project)
                .ThenInclude(p => p.Client)
            .Include(pa => pa.Provider)
                .ThenInclude(p => p.Profile)
            .AsSplitQuery()
            .FirstOrDefaultAsync(pa => pa.Id == applicationId);

        if (application?.Project?.Client?.Email == null)
            return;

        var subject = $"New application received for '{application.Project.Title}'";
        var message = BuildNewApplicationNotificationMessage(
            application.Project.Title,
            application.Provider.Profile?.FirstName ?? "A provider",
            application.SkillMatchScore ?? 0m);

        await _emailService.SendEmailAsync(application.Project.Client.Email, subject, message);
    }

    private ProjectApplicationDto MapToDto(ProjectApplication application)
    {
        return new ProjectApplicationDto
        {
            Id = application.Id,
            Project = MapToSummaryDto(application.Project),
            Provider = new UserSummaryDto
            {
                Id = application.Provider.Id,
                DisplayName = GetUserDisplayName(application.Provider),
                Email = application.Provider.Email,
                UserName = application.Provider.UserName,
                FirstName = application.Provider.Profile?.FirstName,
                LastName = application.Provider.Profile?.LastName,
                Title = application.Provider.Profile?.Title,
                Company = application.Provider.Profile?.Company,
                Location = application.Provider.Profile?.Location
            },
            CoverLetter = application.CoverLetter,
            ProposedTimeline = application.ProposedTimeline,
            SkillMatchScore = application.SkillMatchScore,
            Status = application.Status.ToString(),
            CreatedAt = application.CreatedAt,
            UpdatedAt = application.UpdatedAt,
            ReviewedAt = application.ReviewedAt,
            ClientFeedback = application.ClientFeedback,
            IsAvailableImmediately = application.IsAvailableImmediately,
            ProposedBudget = application.ProposedBudget,
            Attachments = application.Attachments?.Select(att => new ApplicationAttachmentDto
            {
                Id = att.Id,
                FileName = att.FileName,
                ContentType = att.ContentType,
                FileSize = att.FileSize,
                Url = att.StorageUrl,
                Description = att.Description,
                IsSafe = att.IsSafe,
                UploadedAt = att.UploadedAt
            }).ToList() ?? new List<ApplicationAttachmentDto>(),
            DaysSinceSubmitted = (DateTime.UtcNow - application.CreatedAt).Days,
            CanBeWithdrawn = application.CanBeWithdrawn
        };
    }

    private ProjectSummaryDto MapToSummaryDto(Project project)
    {
        return new ProjectSummaryDto
        {
            Id = project.Id,
            Title = project.Title,
            ShortDescription = project.Description.Length > 200
                ? project.Description.Substring(0, 200) + "..."
                : project.Description,
            Client = new UserSummaryDto
            {
                Id = project.Client.Id,
                DisplayName = GetUserDisplayName(project.Client),
                Email = project.Client.Email,
                UserName = project.Client.UserName,
                FirstName = project.Client.Profile?.FirstName,
                LastName = project.Client.Profile?.LastName,
                Title = project.Client.Profile?.Title,
                Company = project.Client.Profile?.Company,
                Location = project.Client.Profile?.Location
            },
            Status = project.Status.ToString(),
            CreditBudget = project.CreditBudget,
            CreatedAt = project.CreatedAt,
            EndDate = project.EndDate,
            DeliverableCount = project.Deliverables?.Count ?? 0,
            RequiredSkillNames = project.ProjectSkills?.Select(ps => ps.Skill.Name).ToList() ?? new List<string>(),
            DurationDisplay = project.HasValidTimeline
                ? $"{(project.EndDate!.Value - project.StartDate!.Value).Days} days"
                : null
        };
    }

    private string BuildStatusNotificationMessage(string projectTitle, string newStatus, string? feedback)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Your application for the project '{projectTitle}' has been updated.");
        sb.AppendLine();
        sb.AppendLine($"New Status: {newStatus}");

        if (!string.IsNullOrWhiteSpace(feedback))
        {
            sb.AppendLine();
            sb.AppendLine("Client Feedback:");
            sb.AppendLine(feedback);
        }

        sb.AppendLine();
        sb.AppendLine("You can view your application details by logging into your SkillLedger account.");

        return sb.ToString();
    }

    private string BuildNewApplicationNotificationMessage(string projectTitle, string providerName, decimal skillMatchScore)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"You have received a new application for your project '{projectTitle}'.");
        sb.AppendLine();
        sb.AppendLine($"Applicant: {providerName}");
        sb.AppendLine($"Skill Match Score: {skillMatchScore:P0}");
        sb.AppendLine();
        sb.AppendLine("Please log into your SkillLedger account to review the application and respond to the candidate.");

        return sb.ToString();
    }

    private string GetUserDisplayName(User user)
    {
        if (user.Profile != null)
        {
            var firstName = user.Profile.FirstName?.Trim();
            var lastName = user.Profile.LastName?.Trim();

            if (!string.IsNullOrEmpty(firstName) && !string.IsNullOrEmpty(lastName))
            {
                return $"{firstName} {lastName}";
            }
            else if (!string.IsNullOrEmpty(firstName))
            {
                return firstName;
            }
            else if (!string.IsNullOrEmpty(lastName))
            {
                return lastName;
            }
        }

        // Fallback to email or username
        return user.Email ?? user.UserName ?? "Unknown User";
    }

    #endregion
}
