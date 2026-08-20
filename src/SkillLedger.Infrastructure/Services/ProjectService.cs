using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SkillLedger.Core.DTOs;
using SkillLedger.Core.Entities;
using SkillLedger.Core.Enums;
using SkillLedger.Core.Interfaces;
using SkillLedger.Infrastructure.Data;
using System.Text.Json;
using System.Text.RegularExpressions;
using AuditActions = SkillLedger.Core.Constants.AuditActions;

namespace SkillLedger.Infrastructure.Services;

public class ProjectService : IProjectService
{
    private readonly SkillLedgerDbContext _context;
    private readonly IAuditLogService _auditLogService;
    private readonly ILogger<ProjectService> _logger;

    public ProjectService(
        SkillLedgerDbContext context,
        IAuditLogService auditLogService,
        ILogger<ProjectService> logger)
    {
        _context = context;
        _auditLogService = auditLogService;
        _logger = logger;
    }

    public async Task<ProjectResponseDto> CreateProjectAsync(CreateProjectDto createDto, Guid clientId, string ipAddress)
    {
        // BUG-002 FIX: Use execution strategy pattern for NpgsqlRetryingExecutionStrategy compatibility
        // When using a retrying execution strategy, you must wrap transactions in ExecuteAsync
        var providerName = _context.Database.ProviderName?.ToLowerInvariant();
        var isInMemoryDatabase = providerName?.Contains("inmemory") == true;

        _logger.LogInformation("Starting project creation for client: {ClientId}", clientId);

        // Pre-validation outside transaction - these are read-only checks
        var client = await _context.Users.FindAsync(clientId);
        if (client == null)
        {
            return new ProjectResponseDto
            {
                Success = false,
                Message = "Client not found"
            };
        }

        if (client.Status != UserStatus.Active && client.Status != UserStatus.PhoneVerified && client.Status != UserStatus.TaxCompliant)
        {
            return new ProjectResponseDto
            {
                Success = false,
                Message = "Account must be active to create projects"
            };
        }

        // Validate skills exist (read-only, can be done outside transaction)
        var skillIds = createDto.RequiredSkills.Select(s => s.SkillId).ToList();
        var existingSkills = await _context.Skills
            .Where(s => skillIds.Contains(s.Id) && s.IsActive)
            .Select(s => s.Id)
            .ToListAsync();

        if (existingSkills.Count != skillIds.Count)
        {
            return new ProjectResponseDto
            {
                Success = false,
                Message = "One or more selected skills are invalid or inactive"
            };
        }

        // Create project entity for validation
        var project = new Project
        {
            Id = Guid.NewGuid(),
            ClientId = clientId,
            Title = createDto.Title.Trim(),
            Description = createDto.Description.Trim(),
            CreditBudget = createDto.CreditBudget,
            StartDate = createDto.StartDate,
            EndDate = createDto.EndDate,
            Status = ProjectStatus.Draft,
            ModerationStatus = ModerationStatus.Pending,
            CreatedFromIP = ipAddress,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Validate business rules
        var validationResult = await ValidateProjectRulesAsync(project);
        if (!validationResult.Success)
        {
            await _auditLogService.LogEventAsync(
                clientId,
                AuditActions.PROJECT_CREATE,
                ipAddress,
                null,
                false,
                JsonSerializer.Serialize(new { ProjectTitle = createDto.Title, Errors = validationResult.Message }),
                "Project validation failed"
            );

            return new ProjectResponseDto
            {
                Success = false,
                Message = validationResult.Message
            };
        }

        try
        {
            // For in-memory database (testing), run without transaction
            if (isInMemoryDatabase)
            {
                return await ExecuteProjectCreationAsync(project, createDto, clientId, ipAddress);
            }

            // BUG-002 FIX: Wrap transaction in execution strategy for NpgsqlRetryingExecutionStrategy
            var strategy = _context.Database.CreateExecutionStrategy();
            return await strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    var result = await ExecuteProjectCreationAsync(project, createDto, clientId, ipAddress);

                    if (result.Success)
                    {
                        await transaction.CommitAsync();
                    }
                    // No explicit rollback needed - disposing uncommitted transaction auto-rolls back

                    return result;
                }
                catch
                {
                    // Transaction will auto-rollback on dispose
                    throw;
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating project for client: {ClientId}. Transaction will be rolled back.", clientId);

            // Log failure (fire and forget, don't let audit log failures affect response)
            _ = Task.Run(async () =>
            {
                try
                {
                    await _auditLogService.LogEventAsync(
                        clientId,
                        AuditActions.PROJECT_CREATE,
                        ipAddress,
                        null,
                        false,
                        JsonSerializer.Serialize(new { Error = ex.Message, ErrorType = ex.GetType().Name }),
                        "Project creation failed with exception"
                    );
                }
                catch (Exception auditEx)
                {
                    _logger.LogWarning(auditEx, "Failed to log audit event for failed project creation. ClientId: {ClientId}, IP: {IpAddress}", clientId, ipAddress);
                }
            });

            return new ProjectResponseDto
            {
                Success = false,
                Message = "An error occurred while creating the project"
            };
        }
    }

    /// <summary>
    /// BUG-002 FIX: Helper method to execute project creation logic (used inside and outside transactions)
    /// </summary>
    private async Task<ProjectResponseDto> ExecuteProjectCreationAsync(Project project, CreateProjectDto createDto, Guid clientId, string ipAddress)
    {
        // Create project
        _context.Projects.Add(project);

        // Add deliverables
        foreach (var deliverableDto in createDto.Deliverables)
        {
            var deliverable = new ProjectDeliverable
            {
                Id = Guid.NewGuid(),
                ProjectId = project.Id,
                Description = deliverableDto.Description.Trim(),
                OrderIndex = deliverableDto.OrderIndex,
                IsRequired = deliverableDto.IsRequired,
                CreatedAt = DateTime.UtcNow
            };
            _context.ProjectDeliverables.Add(deliverable);
        }

        // Add required skills
        foreach (var skillDto in createDto.RequiredSkills)
        {
            var projectSkill = new ProjectSkill
            {
                ProjectId = project.Id,
                SkillId = skillDto.SkillId,
                ProficiencyRequired = (SkillProficiency)skillDto.ProficiencyRequired,
                Weight = skillDto.Weight,
                CreatedAt = DateTime.UtcNow
            };
            _context.ProjectSkills.Add(projectSkill);
        }

        // Save all changes
        await _context.SaveChangesAsync();

        // Audit log
        await _auditLogService.LogEventAsync(
            clientId,
            AuditActions.PROJECT_CREATE,
            ipAddress,
            null,
            true,
            JsonSerializer.Serialize(new { ProjectId = project.Id, ProjectTitle = project.Title }),
            "Project created successfully"
        );

        _logger.LogInformation("Project created successfully: {ProjectId} by client: {ClientId}", project.Id, clientId);

        // Return project details
        var projectDto = await GetProjectByIdAsync(project.Id, clientId);
        return new ProjectResponseDto
        {
            Success = true,
            Message = "Project created successfully",
            Project = projectDto
        };
    }

    public async Task<ProjectResponseDto> UpdateProjectAsync(Guid projectId, UpdateProjectDto updateDto, Guid clientId, string ipAddress)
    {
        // BUG-002 FIX: Use execution strategy pattern for NpgsqlRetryingExecutionStrategy compatibility
        var providerName = _context.Database.ProviderName?.ToLowerInvariant();
        var isInMemoryDatabase = providerName?.Contains("inmemory") == true;

        _logger.LogInformation("Updating project: {ProjectId} by client: {ClientId}", projectId, clientId);

        // Pre-validation outside transaction - these are read-only checks
        // PERFORMANCE FIX: Use AsSplitQuery to prevent cartesian explosion
        var project = await _context.Projects
            .Include(p => p.Deliverables)
            .Include(p => p.ProjectSkills)
            .AsSplitQuery()
            .FirstOrDefaultAsync(p => p.Id == projectId);

        if (project == null)
        {
            return new ProjectResponseDto
            {
                Success = false,
                Message = "Project not found"
            };
        }

        // Check permissions
        if (!await CanUserModifyProjectAsync(projectId, clientId))
        {
            await _auditLogService.LogEventAsync(
                clientId,
                AuditActions.PROJECT_UPDATE,
                ipAddress,
                null,
                false,
                JsonSerializer.Serialize(new { ProjectId = projectId }),
                "Unauthorized project update attempt"
            );

            return new ProjectResponseDto
            {
                Success = false,
                Message = "You don't have permission to modify this project"
            };
        }

        if (!project.IsEditable)
        {
            return new ProjectResponseDto
            {
                Success = false,
                Message = "This project is not editable in its current state"
            };
        }

        // Validate skills if provided (read-only check, can be done outside transaction)
        if (updateDto.RequiredSkills != null)
        {
            var skillIds = updateDto.RequiredSkills.Select(s => s.SkillId).ToList();
            var existingSkills = await _context.Skills
                .Where(s => skillIds.Contains(s.Id) && s.IsActive)
                .Select(s => s.Id)
                .ToListAsync();

            if (existingSkills.Count != skillIds.Count)
            {
                return new ProjectResponseDto
                {
                    Success = false,
                    Message = "One or more selected skills are invalid or inactive"
                };
            }
        }

        try
        {
            // For in-memory database (testing), run without transaction
            if (isInMemoryDatabase)
            {
                return await ExecuteProjectUpdateAsync(project, updateDto, clientId, ipAddress);
            }

            // BUG-002 FIX: Wrap transaction in execution strategy for NpgsqlRetryingExecutionStrategy
            var strategy = _context.Database.CreateExecutionStrategy();
            return await strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    var result = await ExecuteProjectUpdateAsync(project, updateDto, clientId, ipAddress);

                    if (result.Success)
                    {
                        await transaction.CommitAsync();
                    }

                    return result;
                }
                catch
                {
                    throw;
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating project: {ProjectId} by client: {ClientId}. Transaction will be rolled back.", projectId, clientId);

            return new ProjectResponseDto
            {
                Success = false,
                Message = "An error occurred while updating the project"
            };
        }
    }

    /// <summary>
    /// BUG-002 FIX: Helper method to execute project update logic (used inside and outside transactions)
    /// </summary>
    private async Task<ProjectResponseDto> ExecuteProjectUpdateAsync(Project project, UpdateProjectDto updateDto, Guid clientId, string ipAddress)
    {
        // Update fields
        if (!string.IsNullOrWhiteSpace(updateDto.Title))
            project.Title = updateDto.Title.Trim();

        if (!string.IsNullOrWhiteSpace(updateDto.Description))
            project.Description = updateDto.Description.Trim();

        if (updateDto.CreditBudget.HasValue)
            project.CreditBudget = updateDto.CreditBudget.Value;

        if (updateDto.StartDate.HasValue)
            project.StartDate = updateDto.StartDate;

        if (updateDto.EndDate.HasValue)
            project.EndDate = updateDto.EndDate;

        project.UpdatedAt = DateTime.UtcNow;
        project.ModerationStatus = ModerationStatus.Pending; // Re-require moderation after updates

        // Validate updated project
        var validationResult = await ValidateProjectRulesAsync(project);
        if (!validationResult.Success)
        {
            return new ProjectResponseDto
            {
                Success = false,
                Message = validationResult.Message
            };
        }

        // Update deliverables if provided
        if (updateDto.Deliverables != null)
        {
            // Remove existing deliverables
            _context.ProjectDeliverables.RemoveRange(project.Deliverables);

            // Add new deliverables
            foreach (var deliverableDto in updateDto.Deliverables)
            {
                var deliverable = new ProjectDeliverable
                {
                    Id = Guid.NewGuid(),
                    ProjectId = project.Id,
                    Description = deliverableDto.Description.Trim(),
                    OrderIndex = deliverableDto.OrderIndex,
                    IsRequired = deliverableDto.IsRequired,
                    CreatedAt = DateTime.UtcNow
                };
                _context.ProjectDeliverables.Add(deliverable);
            }
        }

        // Update skills if provided
        if (updateDto.RequiredSkills != null)
        {
            // Remove existing skills
            _context.ProjectSkills.RemoveRange(project.ProjectSkills);

            // Add new skills
            foreach (var skillDto in updateDto.RequiredSkills)
            {
                var projectSkill = new ProjectSkill
                {
                    ProjectId = project.Id,
                    SkillId = skillDto.SkillId,
                    ProficiencyRequired = (SkillProficiency)skillDto.ProficiencyRequired,
                    Weight = skillDto.Weight,
                    CreatedAt = DateTime.UtcNow
                };
                _context.ProjectSkills.Add(projectSkill);
            }
        }

        await _context.SaveChangesAsync();

        // Audit log
        await _auditLogService.LogEventAsync(
            clientId,
            AuditActions.PROJECT_UPDATE,
            ipAddress,
            null,
            true,
            JsonSerializer.Serialize(new { ProjectId = project.Id, ProjectTitle = project.Title }),
            "Project updated successfully"
        );

        _logger.LogInformation("Project updated successfully: {ProjectId} by client: {ClientId}", project.Id, clientId);

        var projectDto = await GetProjectByIdAsync(project.Id, clientId);
        return new ProjectResponseDto
        {
            Success = true,
            Message = "Project updated successfully",
            Project = projectDto
        };
    }

    public async Task<ProjectResponseDto> SaveProjectDraftAsync(SaveDraftProjectDto saveDraftDto, Guid clientId, string ipAddress)
    {
        try
        {
            // Create minimal project for draft
            var project = new Project
            {
                Id = Guid.NewGuid(),
                ClientId = clientId,
                Title = saveDraftDto.Title?.Trim() ?? "Untitled Project",
                Description = saveDraftDto.Description?.Trim() ?? string.Empty,
                CreditBudget = saveDraftDto.CreditBudget ?? 100, // Default minimum
                StartDate = saveDraftDto.StartDate,
                EndDate = saveDraftDto.EndDate,
                Status = ProjectStatus.Draft,
                ModerationStatus = ModerationStatus.Pending,
                CreatedFromIP = ipAddress,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Projects.Add(project);

            // Add deliverables if provided
            if (saveDraftDto.Deliverables != null)
            {
                foreach (var deliverableDto in saveDraftDto.Deliverables)
                {
                    var deliverable = new ProjectDeliverable
                    {
                        Id = Guid.NewGuid(),
                        ProjectId = project.Id,
                        Description = deliverableDto.Description.Trim(),
                        OrderIndex = deliverableDto.OrderIndex,
                        IsRequired = deliverableDto.IsRequired,
                        CreatedAt = DateTime.UtcNow
                    };
                    _context.ProjectDeliverables.Add(deliverable);
                }
            }

            // Add skills if provided
            if (saveDraftDto.RequiredSkills != null)
            {
                // Validate skills exist
                var skillIds = saveDraftDto.RequiredSkills.Select(s => s.SkillId).ToList();
                var existingSkills = await _context.Skills
                    .Where(s => skillIds.Contains(s.Id) && s.IsActive)
                    .Select(s => s.Id)
                    .ToListAsync();

                if (existingSkills.Count == skillIds.Count) // Only add if all skills are valid
                {
                    foreach (var skillDto in saveDraftDto.RequiredSkills)
                    {
                        var projectSkill = new ProjectSkill
                        {
                            ProjectId = project.Id,
                            SkillId = skillDto.SkillId,
                            ProficiencyRequired = (SkillProficiency)skillDto.ProficiencyRequired,
                            Weight = skillDto.Weight,
                            CreatedAt = DateTime.UtcNow
                        };
                        _context.ProjectSkills.Add(projectSkill);
                    }
                }
            }

            await _context.SaveChangesAsync();

            // Audit log
            await _auditLogService.LogEventAsync(
                clientId,
                AuditActions.PROJECT_DRAFT_SAVE,
                ipAddress,
                null,
                true,
                JsonSerializer.Serialize(new { ProjectId = project.Id, ProjectTitle = project.Title }),
                "Project draft saved"
            );

            _logger.LogInformation("Project draft saved: {ProjectId} by client: {ClientId}", project.Id, clientId);

            var projectDto = await GetProjectByIdAsync(project.Id, clientId);
            return new ProjectResponseDto
            {
                Success = true,
                Message = "Draft saved successfully",
                Project = projectDto
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving project draft for client: {ClientId}", clientId);

            return new ProjectResponseDto
            {
                Success = false,
                Message = "An error occurred while saving the draft"
            };
        }
    }

    public async Task<ProjectResponseDto> UpdateProjectDraftAsync(Guid projectId, SaveDraftProjectDto saveDraftDto, Guid clientId, string ipAddress)
    {
        var updateDto = new UpdateProjectDto
        {
            Title = saveDraftDto.Title,
            Description = saveDraftDto.Description,
            CreditBudget = saveDraftDto.CreditBudget,
            StartDate = saveDraftDto.StartDate,
            EndDate = saveDraftDto.EndDate,
            Deliverables = saveDraftDto.Deliverables,
            RequiredSkills = saveDraftDto.RequiredSkills
        };

        return await UpdateProjectAsync(projectId, updateDto, clientId, ipAddress);
    }

    public async Task<ServiceResponseDto> PublishProjectAsync(Guid projectId, Guid clientId, string ipAddress)
    {
        try
        {
            // PERFORMANCE FIX: Use AsSplitQuery to prevent cartesian explosion
            var project = await _context.Projects
                .Include(p => p.Deliverables)
                .Include(p => p.ProjectSkills)
                .AsSplitQuery()
                .FirstOrDefaultAsync(p => p.Id == projectId);

            if (project == null)
            {
                return new ServiceResponseDto
                {
                    Success = false,
                    Message = "Project not found"
                };
            }

            if (!await CanUserModifyProjectAsync(projectId, clientId))
            {
                return new ServiceResponseDto
                {
                    Success = false,
                    Message = "You don't have permission to publish this project"
                };
            }

            if (!project.CanBePublished)
            {
                var issues = new List<string>();

                if (string.IsNullOrWhiteSpace(project.Title))
                    issues.Add("Title is required");

                if (string.IsNullOrWhiteSpace(project.Description))
                    issues.Add("Description is required");

                if (!project.Deliverables.Any())
                    issues.Add("At least one deliverable is required");

                if (!project.ProjectSkills.Any())
                    issues.Add("At least one required skill is needed");

                if (!project.HasValidTimeline)
                    issues.Add("Valid start and end dates are required");

                return new ServiceResponseDto
                {
                    Success = false,
                    Message = $"Project cannot be published: {string.Join(", ", issues)}"
                };
            }

            // Change status to published (subject to moderation)
            project.Status = ProjectStatus.Published;
            project.ModerationStatus = ModerationStatus.Pending;
            project.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // Audit log
            await _auditLogService.LogEventAsync(
                clientId,
                AuditActions.PROJECT_PUBLISH,
                ipAddress,
                null,
                true,
                JsonSerializer.Serialize(new { ProjectId = project.Id, ProjectTitle = project.Title }),
                "Project published for moderation"
            );

            _logger.LogInformation("Project published: {ProjectId} by client: {ClientId}", projectId, clientId);

            return new ServiceResponseDto
            {
                Success = true,
                Message = "Project published successfully and is now under review"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error publishing project: {ProjectId} by client: {ClientId}", projectId, clientId);

            return new ServiceResponseDto
            {
                Success = false,
                Message = "An error occurred while publishing the project"
            };
        }
    }

    /// <summary>
    /// SECURITY FIX: Get project by ID with proper authorization at database level
    /// This prevents IDOR vulnerabilities by filtering unauthorized projects in the query
    /// </summary>
    /// <param name="projectId">Project ID to retrieve</param>
    /// <param name="requestingUserId">ID of user requesting the project (null for anonymous)</param>
    /// <returns>Project DTO if authorized, null otherwise</returns>
    public async Task<ProjectDto?> GetProjectByIdAsync(Guid projectId, Guid? requestingUserId = null)
    {
        // PERFORMANCE FIX: Use AsSplitQuery to prevent cartesian explosion
        // (multiple Include statements cause row multiplication without split queries)
        var query = _context.Projects
            .Include(p => p.Client)
            .ThenInclude(c => c.Profile)
            .Include(p => p.Deliverables)
            .Include(p => p.ProjectSkills)
            .ThenInclude(ps => ps.Skill)
            .AsSplitQuery()
            .Where(p => p.Id == projectId);

        // SECURITY FIX: Authorization filter applied at database level
        // Only return projects that the user is authorized to see
        if (requestingUserId.HasValue)
        {
            // Authenticated user can see:
            // 1. All published and approved projects
            // 2. Their own projects (any status)
            query = query.Where(p =>
                (p.Status == ProjectStatus.Published && p.ModerationStatus == ModerationStatus.Approved) ||
                p.ClientId == requestingUserId.Value);
        }
        else
        {
            // Anonymous user can only see published and approved projects
            query = query.Where(p =>
                p.Status == ProjectStatus.Published &&
                p.ModerationStatus == ModerationStatus.Approved);
        }

        var project = await query.FirstOrDefaultAsync();

        if (project == null)
        {
            // Don't leak information about whether project exists
            _logger.LogDebug("Project {ProjectId} not found or not authorized for user {UserId}",
                projectId, requestingUserId);
            return null;
        }

        return MapProjectToDto(project);
    }

    // BUG-020 FIX: Removed deprecated overload to eliminate confusion
    // All callers should use GetProjectByIdAsync(Guid projectId, Guid? requestingUserId)

    public async Task<List<ProjectDto>> GetProjectsByClientAsync(Guid clientId, bool includeNonPublic = false, int skip = 0, int take = 20)
    {
        // SECURITY FIX: Validate pagination parameters
        const int MAX_TAKE = 100;
        const int MAX_SKIP = 10000;

        if (take <= 0 || take > MAX_TAKE)
        {
            take = Math.Min(Math.Max(take, 1), MAX_TAKE);
        }

        if (skip < 0)
        {
            skip = 0;
        }

        if (skip > MAX_SKIP)
        {
            throw new ArgumentException(
                $"Skip value {skip} exceeds maximum allowed value of {MAX_SKIP}",
                nameof(skip));
        }

        // PERFORMANCE FIX: Use AsSplitQuery to prevent cartesian explosion
        var query = _context.Projects
            .Include(p => p.Client)
            .ThenInclude(c => c.Profile)
            .Include(p => p.Deliverables)
            .Include(p => p.ProjectSkills)
            .ThenInclude(ps => ps.Skill)
            .AsSplitQuery()
            .Where(p => p.ClientId == clientId);

        if (!includeNonPublic)
        {
            query = query.Where(p => p.Status == ProjectStatus.Published && p.ModerationStatus == ModerationStatus.Approved);
        }

        var projects = await query
            .OrderByDescending(p => p.CreatedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync();

        return projects.Select(MapProjectToDto).ToList();
    }

    public async Task<List<ProjectSummaryDto>> SearchProjectsAsync(ProjectSearchDto searchDto)
    {
        // SECURITY FIX: Validate and limit pagination parameters to prevent DoS attacks
        const int MAX_TAKE = 100;
        const int MAX_SKIP = 10000;
        const int DEFAULT_TAKE = 20;

        // Validate and sanitize pagination parameters
        var take = searchDto.Take;
        var skip = searchDto.Skip;

        if (take <= 0 || take > MAX_TAKE)
        {
            _logger.LogWarning("Invalid Take parameter: {Take}. Limiting to {MaxTake}", take, MAX_TAKE);
            take = Math.Min(take, MAX_TAKE);
            if (take <= 0) take = DEFAULT_TAKE;
        }

        if (skip < 0)
        {
            _logger.LogWarning("Negative Skip parameter: {Skip}. Setting to 0", skip);
            skip = 0;
        }

        if (skip > MAX_SKIP)
        {
            _logger.LogWarning("Skip parameter too large: {Skip}. Maximum allowed is {MaxSkip}", skip, MAX_SKIP);
            throw new ArgumentException(
                $"Skip value {skip} exceeds maximum allowed value of {MAX_SKIP}. " +
                "Please use more specific filters to narrow your search results.",
                nameof(searchDto.Skip));
        }

        // PERFORMANCE FIX: Use AsSplitQuery to prevent cartesian explosion
        var query = _context.Projects
            .Include(p => p.Client)
            .ThenInclude(c => c.Profile)
            .Include(p => p.Deliverables)
            .Include(p => p.ProjectSkills)
            .ThenInclude(ps => ps.Skill)
            .AsSplitQuery()
            .AsQueryable();

        // Apply filters
        if (searchDto.PublishedOnly)
        {
            query = query.Where(p => p.Status == ProjectStatus.Published && p.ModerationStatus == ModerationStatus.Approved);
        }

        if (!string.IsNullOrWhiteSpace(searchDto.Query))
        {
            // P1 SECURITY FIX: Enhanced SQL injection prevention and case-insensitive search
            // 1. Trim and limit length
            var sanitizedQuery = searchDto.Query.Trim();
            if (sanitizedQuery.Length > 200)
            {
                sanitizedQuery = sanitizedQuery.Substring(0, 200);
                _logger.LogWarning("Search query truncated from {Original} to 200 characters", searchDto.Query.Length);
            }

            // 2. Remove SQL special characters and dangerous patterns
            sanitizedQuery = SanitizeSearchQuery(sanitizedQuery);

            // 3. Use EF Core with case-insensitive comparison (parameterized automatically)
            // EF Core converts this to parameterized SQL, preventing injection
            query = query.Where(p =>
                EF.Functions.Like(p.Title, $"%{sanitizedQuery}%") ||
                EF.Functions.Like(p.Description, $"%{sanitizedQuery}%"));
        }

        if (!string.IsNullOrWhiteSpace(searchDto.Status))
        {
            if (Enum.TryParse<ProjectStatus>(searchDto.Status, true, out var status))
            {
                query = query.Where(p => p.Status == status);
            }
        }

        // E2E-003 FIX: Convert skill names to skill IDs if provided
        if (!string.IsNullOrWhiteSpace(searchDto.SkillNames))
        {
            var skillNameList = searchDto.SkillNames
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Take(10) // Limit to 10 skills
                .ToList();

            if (skillNameList.Any())
            {
                // Look up skill IDs by name (case-insensitive)
                var skillIdsFromNames = await _context.Skills
                    .Where(s => skillNameList.Any(name =>
                        EF.Functions.Like(s.Name, name)))
                    .Select(s => s.Id)
                    .ToListAsync();

                if (skillIdsFromNames.Any())
                {
                    // Merge with any existing SkillIds or use as the filter
                    if (searchDto.SkillIds == null)
                    {
                        searchDto.SkillIds = skillIdsFromNames;
                    }
                    else
                    {
                        searchDto.SkillIds = searchDto.SkillIds.Union(skillIdsFromNames).ToList();
                    }
                }
            }
        }

        if (searchDto.SkillIds != null && searchDto.SkillIds.Any())
        {
            // BUG-012 FIX: Enforce hard limit on skill IDs to prevent DoS
            if (searchDto.SkillIds.Count > 10)
            {
                _logger.LogWarning("Too many skill IDs in search: {Count}. Maximum is 10", searchDto.SkillIds.Count);
                throw new ArgumentException(
                    $"Maximum of 10 skill IDs allowed in search. Received {searchDto.SkillIds.Count}.",
                    nameof(searchDto.SkillIds));
            }
            query = query.Where(p => p.ProjectSkills.Any(ps => searchDto.SkillIds.Contains(ps.SkillId)));
        }

        if (searchDto.MinBudget.HasValue)
        {
            query = query.Where(p => p.CreditBudget >= searchDto.MinBudget.Value);
        }

        if (searchDto.MaxBudget.HasValue)
        {
            query = query.Where(p => p.CreditBudget <= searchDto.MaxBudget.Value);
        }

        if (searchDto.ClientId.HasValue)
        {
            query = query.Where(p => p.ClientId == searchDto.ClientId.Value);
        }

        // Apply sorting
        query = searchDto.SortBy.ToLower() switch
        {
            "budget" => searchDto.SortDirection.ToLower() == "asc"
                ? query.OrderBy(p => p.CreditBudget)
                : query.OrderByDescending(p => p.CreditBudget),
            "enddate" => searchDto.SortDirection.ToLower() == "asc"
                ? query.OrderBy(p => p.EndDate)
                : query.OrderByDescending(p => p.EndDate),
            "title" => searchDto.SortDirection.ToLower() == "asc"
                ? query.OrderBy(p => p.Title)
                : query.OrderByDescending(p => p.Title),
            _ => searchDto.SortDirection.ToLower() == "asc"
                ? query.OrderBy(p => p.CreatedAt)
                : query.OrderByDescending(p => p.CreatedAt)
        };

        // Use validated pagination parameters
        var projects = await query
            .Skip(skip)
            .Take(take)
            .ToListAsync();

        _logger.LogInformation("Project search completed: {ResultCount} results (Skip: {Skip}, Take: {Take})",
            projects.Count, skip, take);

        return projects.Select(MapProjectToSummaryDto).ToList();
    }

    public async Task<int> CountProjectsAsync(ProjectSearchDto searchDto)
    {
        var query = _context.Projects.AsQueryable();

        // Apply same filters as SearchProjectsAsync
        if (searchDto.PublishedOnly)
        {
            query = query.Where(p => p.Status == ProjectStatus.Published && p.ModerationStatus == ModerationStatus.Approved);
        }

        if (!string.IsNullOrWhiteSpace(searchDto.Query))
        {
            // P1 SECURITY FIX: Same sanitization as SearchProjectsAsync
            var sanitizedQuery = SanitizeSearchQuery(searchDto.Query.Trim());
            if (sanitizedQuery.Length > 200)
            {
                sanitizedQuery = sanitizedQuery.Substring(0, 200);
            }
            query = query.Where(p =>
                EF.Functions.Like(p.Title, $"%{sanitizedQuery}%") ||
                EF.Functions.Like(p.Description, $"%{sanitizedQuery}%"));
        }

        if (!string.IsNullOrWhiteSpace(searchDto.Status))
        {
            if (Enum.TryParse<ProjectStatus>(searchDto.Status, true, out var status))
            {
                query = query.Where(p => p.Status == status);
            }
        }

        if (searchDto.SkillIds != null && searchDto.SkillIds.Any())
        {
            query = query.Where(p => p.ProjectSkills.Any(ps => searchDto.SkillIds.Contains(ps.SkillId)));
        }

        if (searchDto.MinBudget.HasValue)
        {
            query = query.Where(p => p.CreditBudget >= searchDto.MinBudget.Value);
        }

        if (searchDto.MaxBudget.HasValue)
        {
            query = query.Where(p => p.CreditBudget <= searchDto.MaxBudget.Value);
        }

        if (searchDto.ClientId.HasValue)
        {
            query = query.Where(p => p.ClientId == searchDto.ClientId.Value);
        }

        return await query.CountAsync();
    }

    public async Task<ServiceResponseDto> DeleteProjectAsync(Guid projectId, Guid clientId, string ipAddress)
    {
        try
        {
            // Validate client exists (for consistency with other methods)
            var client = await _context.Users.FindAsync(clientId);
            if (client == null)
            {
                return new ServiceResponseDto
                {
                    Success = false,
                    Message = "Client not found"
                };
            }

            var project = await _context.Projects.FindAsync(projectId);

            if (project == null)
            {
                return new ServiceResponseDto
                {
                    Success = false,
                    Message = "Project not found"
                };
            }

            if (!await CanUserModifyProjectAsync(projectId, clientId))
            {
                await _auditLogService.LogEventAsync(
                    clientId,
                    AuditActions.PROJECT_DELETE,
                    ipAddress,
                    null,
                    false,
                    JsonSerializer.Serialize(new { ProjectId = projectId }),
                    "Unauthorized project deletion attempt"
                );

                return new ServiceResponseDto
                {
                    Success = false,
                    Message = "You don't have permission to delete this project"
                };
            }

            // Soft delete by changing status
            if (project.Status == ProjectStatus.InProgress)
            {
                return new ServiceResponseDto
                {
                    Success = false,
                    Message = "Cannot delete a project that is in progress"
                };
            }

            project.Status = ProjectStatus.Cancelled;
            project.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // Audit log
            await _auditLogService.LogEventAsync(
                clientId,
                AuditActions.PROJECT_DELETE,
                ipAddress,
                null,
                true,
                JsonSerializer.Serialize(new { ProjectId = project.Id, ProjectTitle = project.Title }),
                "Project deleted (cancelled)"
            );

            _logger.LogInformation("Project deleted: {ProjectId} by client: {ClientId}", projectId, clientId);

            return new ServiceResponseDto
            {
                Success = true,
                Message = "Project deleted successfully"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting project: {ProjectId} by client: {ClientId}", projectId, clientId);

            return new ServiceResponseDto
            {
                Success = false,
                Message = "An error occurred while deleting the project"
            };
        }
    }

    public async Task<ServiceResponseDto> ValidateProjectRulesAsync(Project project)
    {
        var errors = new List<string>();

        // Title validation
        if (string.IsNullOrWhiteSpace(project.Title))
        {
            errors.Add("Project title is required");
        }
        else if (project.Title.Length > 100)
        {
            errors.Add("Project title cannot exceed 100 characters");
        }

        // Description validation
        if (string.IsNullOrWhiteSpace(project.Description))
        {
            errors.Add("Project description is required");
        }
        else if (project.Description.Length > 5000)
        {
            errors.Add("Project description cannot exceed 5000 characters");
        }

        // BUG-026 FIX: Increased budget limits for enterprise use
        // Budget validation with higher limits (was 5000, now 50000)
        if (project.CreditBudget < 50 || project.CreditBudget > 50000)
        {
            errors.Add("Credit budget must be between 50 and 50,000 credits");
        }

        // BUG-024 FIX: Ensure all dates are normalized to UTC for consistent comparison
        // Timeline validation
        if (project.StartDate.HasValue && project.EndDate.HasValue)
        {
            // Normalize dates to UTC for consistent timezone handling
            var startDateUtc = project.StartDate.Value.Kind == DateTimeKind.Utc
                ? project.StartDate.Value
                : project.StartDate.Value.ToUniversalTime();

            var endDateUtc = project.EndDate.Value.Kind == DateTimeKind.Utc
                ? project.EndDate.Value
                : project.EndDate.Value.ToUniversalTime();

            if (endDateUtc <= startDateUtc)
            {
                errors.Add("End date must be after start date");
            }

            // Compare with current UTC time
            if (endDateUtc <= DateTime.UtcNow)
            {
                errors.Add("End date must be in the future");
            }

            var duration = (endDateUtc - startDateUtc).Days;
            if (duration > 365)
            {
                errors.Add("Project duration cannot exceed 365 days");
            }
        }

        return new ServiceResponseDto
        {
            Success = errors.Count == 0,
            Message = errors.Count > 0 ? string.Join(", ", errors) : "Validation passed"
        };
    }

    /// <summary>
    /// P1 SECURITY FIX: Sanitize search queries to prevent SQL injection and special character exploits
    /// </summary>
    private string SanitizeSearchQuery(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return string.Empty;

        // Remove SQL-like patterns that could cause issues
        // Note: EF Core already parameterizes queries, but this adds defense in depth
        var sanitized = query;

        // Remove SQL comment markers
        sanitized = sanitized.Replace("--", "").Replace("/*", "").Replace("*/", "");

        // Remove potentially dangerous SQL keywords (case-insensitive)
        var dangerousPatterns = new[] {
            @"\bDROP\b", @"\bDELETE\b", @"\bINSERT\b", @"\bUPDATE\b",
            @"\bEXEC\b", @"\bEXECUTE\b", @"\bSELECT\b", @"\bUNION\b",
            @"\bCREATE\b", @"\bALTER\b", @"\bTRUNCATE\b", @"\bSCRIPT\b"
        };

        foreach (var pattern in dangerousPatterns)
        {
            sanitized = Regex.Replace(sanitized, pattern, "", RegexOptions.IgnoreCase);
        }

        // Remove special characters that could be used for injection
        // Keep: letters, numbers, spaces, basic punctuation
        sanitized = Regex.Replace(sanitized, @"[^\w\s\-.,!?@#&()]", "");

        // Remove excessive whitespace
        sanitized = Regex.Replace(sanitized, @"\s+", " ").Trim();

        return sanitized;
    }

    public async Task<bool> CanUserModifyProjectAsync(Guid projectId, Guid userId)
    {
        var project = await _context.Projects.FindAsync(projectId);
        if (project == null) return false;

        // Owner can always modify (if project is editable)
        if (project.ClientId == userId) return true;

        // BUG-031 FIX: Check if user has admin/moderator permissions using UserRoles join
        // Note: Full RBAC with granular permissions (project.moderate, project.manage_all)
        // would require additional tables/relationships. For now, we check Admin/Moderator roles only.
        var hasPermission = await _context.UserRoles
            .Where(ur => ur.UserId == userId)
            .Join(_context.Roles, ur => ur.RoleId, r => r.Id, (ur, r) => r)
            .AnyAsync(r => r.Name == "Admin" || r.Name == "Moderator");

        if (hasPermission)
        {
            return true;
        }

        return false;
    }

    public async Task<object> GetProjectStatisticsAsync(Guid? clientId = null)
    {
        var query = _context.Projects.AsQueryable();

        if (clientId.HasValue)
        {
            query = query.Where(p => p.ClientId == clientId.Value);
        }

        var stats = await query.GroupBy(p => p.Status)
            .Select(g => new { Status = g.Key.ToString(), Count = g.Count() })
            .ToListAsync();

        var totalProjects = await query.CountAsync();
        var totalBudget = await query.SumAsync(p => p.CreditBudget);
        var averageBudget = totalProjects > 0 ? (double)totalBudget / totalProjects : 0;

        return new
        {
            TotalProjects = totalProjects,
            TotalBudget = totalBudget,
            AverageBudget = Math.Round(averageBudget, 2),
            StatusBreakdown = stats,
            ModerationQueue = await _context.Projects.CountAsync(p => p.ModerationStatus == ModerationStatus.Pending)
        };
    }

    public async Task<ServiceResponseDto> ModerateProjectAsync(Guid projectId, string moderationStatus, Guid moderatorId, string? notes, string ipAddress)
    {
        try
        {
            var project = await _context.Projects.FindAsync(projectId);

            if (project == null)
            {
                return new ServiceResponseDto
                {
                    Success = false,
                    Message = "Project not found"
                };
            }

            if (!Enum.TryParse<ModerationStatus>(moderationStatus, true, out var status))
            {
                return new ServiceResponseDto
                {
                    Success = false,
                    Message = "Invalid moderation status"
                };
            }

            project.ModerationStatus = status;
            project.ModerationNotes = notes;
            project.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // Audit log
            await _auditLogService.LogEventAsync(
                moderatorId,
                AuditActions.PROJECT_MODERATE,
                ipAddress,
                null,
                true,
                JsonSerializer.Serialize(new { ProjectId = project.Id, ModerationStatus = status.ToString(), Notes = notes }),
                $"Project moderated: {status}"
            );

            _logger.LogInformation("Project moderated: {ProjectId} by moderator: {ModeratorId} - Status: {Status}", projectId, moderatorId, status);

            return new ServiceResponseDto
            {
                Success = true,
                Message = $"Project moderation updated to {status}"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error moderating project: {ProjectId} by moderator: {ModeratorId}", projectId, moderatorId);

            return new ServiceResponseDto
            {
                Success = false,
                Message = "An error occurred while moderating the project"
            };
        }
    }

    private ProjectDto MapProjectToDto(Project project)
    {
        return new ProjectDto
        {
            Id = project.Id,
            ClientId = project.ClientId,
            Client = new UserSummaryDto
            {
                Id = project.Client.Id,
                DisplayName = GetDisplayName(project.Client),
                Title = project.Client.Profile?.Title,
                Company = project.Client.Profile?.Company,
                AvatarUrl = project.Client.Profile?.AvatarUrl
            },
            Title = project.Title,
            Description = project.Description,
            Status = project.Status.ToString(),
            CreditBudget = project.CreditBudget,
            StartDate = project.StartDate,
            EndDate = project.EndDate,
            ModerationStatus = project.ModerationStatus.ToString(),
            ModerationNotes = project.ModerationNotes,
            CreatedAt = project.CreatedAt,
            UpdatedAt = project.UpdatedAt,
            Deliverables = project.Deliverables.OrderBy(d => d.OrderIndex).Select(d => new ProjectDeliverableDto
            {
                Id = d.Id,
                ProjectId = d.ProjectId,
                Description = d.Description,
                OrderIndex = d.OrderIndex,
                IsRequired = d.IsRequired,
                IsCompleted = d.IsCompleted,
                CompletedAt = d.CompletedAt,
                CreatedAt = d.CreatedAt
            }).ToList(),
            RequiredSkills = project.ProjectSkills.Select(ps => new ProjectSkillDto
            {
                ProjectId = ps.ProjectId,
                Skill = new SkillDto
                {
                    Id = ps.Skill.Id,
                    Name = ps.Skill.Name,
                    Description = ps.Skill.Description,
                    Category = ps.Skill.Category,
                    IsSystemManaged = ps.Skill.IsSystemManaged,
                    IsActive = ps.Skill.IsActive,
                    CreatedAt = ps.Skill.CreatedAt,
                    UpdatedAt = ps.Skill.UpdatedAt
                },
                ProficiencyRequired = (int)ps.ProficiencyRequired,
                Weight = ps.Weight,
                CreatedAt = ps.CreatedAt
            }).ToList(),
            HasValidTimeline = project.HasValidTimeline,
            IsEditable = project.IsEditable,
            CanBePublished = project.CanBePublished
        };
    }

    private ProjectSummaryDto MapProjectToSummaryDto(Project project)
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
                DisplayName = GetDisplayName(project.Client),
                Title = project.Client.Profile?.Title,
                Company = project.Client.Profile?.Company,
                AvatarUrl = project.Client.Profile?.AvatarUrl
            },
            Status = project.Status.ToString(),
            CreditBudget = project.CreditBudget,
            CreatedAt = project.CreatedAt,
            EndDate = project.EndDate,
            DeliverableCount = project.Deliverables.Count,
            RequiredSkillNames = project.ProjectSkills.Select(ps => ps.Skill.Name).ToList()
        };
    }

    private static string GetDisplayName(User user)
    {
        if (user.Profile != null && !string.IsNullOrWhiteSpace(user.Profile.FirstName) && !string.IsNullOrWhiteSpace(user.Profile.LastName))
        {
            return $"{user.Profile.FirstName.Trim()} {user.Profile.LastName.Trim()}";
        }

        // BUG-030 FIX: Add fallback for null UserName and Email
        return user.UserName ?? user.Email ?? "Unknown User";
    }
}