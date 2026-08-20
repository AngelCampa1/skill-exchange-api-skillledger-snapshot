using Microsoft.EntityFrameworkCore;
using SkillLedger.Core.DTOs;
using SkillLedger.Core.Entities;
using SkillLedger.Core.Interfaces;
using SkillLedger.Infrastructure.Data;

namespace SkillLedger.Infrastructure.Services;

public class ExperienceService : IExperienceService
{
    private readonly SkillLedgerDbContext _context;
    private readonly IAuditLogService _auditLogService;

    public ExperienceService(
        SkillLedgerDbContext context,
        IAuditLogService auditLogService)
    {
        _context = context;
        _auditLogService = auditLogService;
    }

    public async Task<ServiceResponseDto> CreateExperienceAsync(Guid userId, CreateExperienceDto dto)
    {
        try
        {
            // Check if user exists
            var userExists = await _context.Users.AnyAsync(u => u.Id == userId);
            if (!userExists)
            {
                return new ServiceResponseDto
                {
                    Success = false,
                    Message = "User not found"
                };
            }

            // Validate dates
            if (dto.EndDate.HasValue && dto.EndDate.Value < dto.StartDate)
            {
                return new ServiceResponseDto
                {
                    Success = false,
                    Message = "End date cannot be before start date"
                };
            }

            // If marked as current, end date should be null
            if (dto.IsCurrent && dto.EndDate.HasValue)
            {
                dto.EndDate = null;
            }

            // Get next display order
            var maxDisplayOrder = await _context.Experiences
                .Where(e => e.UserId == userId)
                .MaxAsync(e => (int?)e.DisplayOrder) ?? 0;

            var experience = new Experience
            {
                UserId = userId,
                Type = dto.Type,
                Title = dto.Title.Trim(),
                Organization = dto.Organization.Trim(),
                Location = dto.Location?.Trim(),
                Description = dto.Description?.Trim(),
                StartDate = dto.StartDate,
                EndDate = dto.EndDate,
                IsCurrent = dto.IsCurrent,
                IsVisible = dto.IsVisible,
                IsFeatured = dto.IsFeatured,
                DisplayOrder = maxDisplayOrder + 1
            };

            _context.Experiences.Add(experience);
            await _context.SaveChangesAsync();

            // Add skills to experience if provided
            if (dto.SkillIds?.Any() == true)
            {
                await AddSkillsToExperienceInternalAsync(experience.Id, dto.SkillIds);
            }

            await _auditLogService.LogEventAsync(
                userId,
                "EXPERIENCE_CREATED",
                "127.0.0.1",
                null,
                true,
                $"Created {dto.Type} experience '{dto.Title}' at '{dto.Organization}'"
            );

            return new ServiceResponseDto
            {
                Success = true,
                Message = "Experience created successfully",
                Data = await MapToExperienceDto(experience, true)
            };
        }
        catch (Exception ex)
        {
            await _auditLogService.LogEventAsync(
                null,
                "EXPERIENCE_CREATION_FAILED",
                "127.0.0.1",
                null,
                false,
                null,
                $"Failed to create experience '{dto.Title}' for user {userId}: {ex.Message}"
            );

            return new ServiceResponseDto
            {
                Success = false,
                Message = "Failed to create experience",
                ErrorDetails = ex.Message
            };
        }
    }

    public async Task<ServiceResponseDto> UpdateExperienceAsync(Guid userId, Guid experienceId, UpdateExperienceDto dto)
    {
        try
        {
            var experience = await _context.Experiences
                .FirstOrDefaultAsync(e => e.Id == experienceId && e.UserId == userId);

            if (experience == null)
            {
                return new ServiceResponseDto
                {
                    Success = false,
                    Message = "Experience not found"
                };
            }

            // Update properties
            if (dto.Type.HasValue)
                experience.Type = dto.Type.Value;
            if (!string.IsNullOrWhiteSpace(dto.Title))
                experience.Title = dto.Title.Trim();
            if (!string.IsNullOrWhiteSpace(dto.Organization))
                experience.Organization = dto.Organization.Trim();
            if (dto.Location != null)
                experience.Location = dto.Location.Trim();
            if (dto.Description != null)
                experience.Description = dto.Description.Trim();
            if (dto.StartDate.HasValue)
                experience.StartDate = dto.StartDate.Value;
            if (dto.EndDate != null) // Allow setting to null
                experience.EndDate = dto.EndDate;
            if (dto.IsCurrent.HasValue)
            {
                experience.IsCurrent = dto.IsCurrent.Value;
                if (dto.IsCurrent.Value)
                    experience.EndDate = null; // Current experiences have no end date
            }
            if (dto.IsVisible.HasValue)
                experience.IsVisible = dto.IsVisible.Value;
            if (dto.IsFeatured.HasValue)
                experience.IsFeatured = dto.IsFeatured.Value;
            if (dto.DisplayOrder.HasValue)
                experience.DisplayOrder = dto.DisplayOrder.Value;

            // Validate dates after updates
            if (experience.EndDate.HasValue && experience.EndDate.Value < experience.StartDate)
            {
                return new ServiceResponseDto
                {
                    Success = false,
                    Message = "End date cannot be before start date"
                };
            }

            experience.UpdatedAt = DateTime.UtcNow;

            // Update skills if provided
            if (dto.SkillIds != null)
            {
                // Remove existing skills
                var existingSkills = await _context.ExperienceSkills
                    .Where(es => es.ExperienceId == experienceId)
                    .ToListAsync();
                _context.ExperienceSkills.RemoveRange(existingSkills);

                // Add new skills
                await AddSkillsToExperienceInternalAsync(experienceId, dto.SkillIds);
            }

            await _context.SaveChangesAsync();

            await _auditLogService.LogEventAsync(
                userId,
                "EXPERIENCE_UPDATED",
                "127.0.0.1",
                null,
                true,
                $"Updated experience '{experience.Title}' at '{experience.Organization}'"
            );

            return new ServiceResponseDto
            {
                Success = true,
                Message = "Experience updated successfully",
                Data = await MapToExperienceDto(experience, true)
            };
        }
        catch (Exception ex)
        {
            await _auditLogService.LogEventAsync(
                userId,
                "EXPERIENCE_UPDATE_FAILED",
                "127.0.0.1",
                null,
                false,
                null,
                $"Failed to update experience {experienceId} for user {userId}: {ex.Message}"
            );

            return new ServiceResponseDto
            {
                Success = false,
                Message = "Failed to update experience",
                ErrorDetails = ex.Message
            };
        }
    }

    public async Task<ServiceResponseDto> DeleteExperienceAsync(Guid userId, Guid experienceId)
    {
        try
        {
            var experience = await _context.Experiences
                .FirstOrDefaultAsync(e => e.Id == experienceId && e.UserId == userId);

            if (experience == null)
            {
                return new ServiceResponseDto
                {
                    Success = false,
                    Message = "Experience not found"
                };
            }

            _context.Experiences.Remove(experience);
            await _context.SaveChangesAsync();

            await _auditLogService.LogEventAsync(
                userId,
                "EXPERIENCE_DELETED",
                "127.0.0.1",
                null,
                true,
                $"Deleted experience '{experience.Title}' at '{experience.Organization}'"
            );

            return new ServiceResponseDto
            {
                Success = true,
                Message = "Experience deleted successfully"
            };
        }
        catch (Exception ex)
        {
            await _auditLogService.LogEventAsync(
                userId,
                "EXPERIENCE_DELETION_FAILED",
                "127.0.0.1",
                null,
                false,
                null,
                $"Failed to delete experience {experienceId} for user {userId}: {ex.Message}"
            );

            return new ServiceResponseDto
            {
                Success = false,
                Message = "Failed to delete experience",
                ErrorDetails = ex.Message
            };
        }
    }

    public async Task<ExperienceDto?> GetExperienceByIdAsync(Guid userId, Guid experienceId, bool includeSkills = true)
    {
        var experience = await _context.Experiences
            .FirstOrDefaultAsync(e => e.Id == experienceId && e.UserId == userId);

        return experience != null ? await MapToExperienceDto(experience, includeSkills) : null;
    }

    public async Task<List<ExperienceDto>> GetUserExperiencesAsync(Guid userId, bool visibleOnly = true, bool includeSkills = true)
    {
        var query = _context.Experiences
            .Where(e => e.UserId == userId);

        if (visibleOnly)
        {
            query = query.Where(e => e.IsVisible);
        }

        var experiences = await query
            .OrderByDescending(e => e.StartDate)
            .ThenBy(e => e.DisplayOrder)
            .ToListAsync();

        var experienceDtos = new List<ExperienceDto>();
        foreach (var experience in experiences)
        {
            experienceDtos.Add(await MapToExperienceDto(experience, includeSkills));
        }

        return experienceDtos;
    }

    public async Task<(List<ExperienceDto> Experiences, int TotalCount)> SearchExperiencesAsync(ExperienceSearchDto searchDto)
    {
        var query = _context.Experiences.AsQueryable();

        // Apply filters
        if (searchDto.UserId.HasValue)
        {
            query = query.Where(e => e.UserId == searchDto.UserId.Value);
        }

        if (searchDto.VisibleOnly)
        {
            query = query.Where(e => e.IsVisible);
        }

        if (searchDto.Type.HasValue)
        {
            query = query.Where(e => e.Type == searchDto.Type.Value);
        }

        if (searchDto.CurrentOnly.HasValue)
        {
            query = query.Where(e => e.IsCurrent == searchDto.CurrentOnly.Value);
        }

        if (searchDto.FeaturedOnly.HasValue)
        {
            query = query.Where(e => e.IsFeatured == searchDto.FeaturedOnly.Value);
        }

        if (!string.IsNullOrWhiteSpace(searchDto.Query))
        {
            var searchQuery = searchDto.Query.ToLower();
            query = query.Where(e =>
                e.Title.ToLower().Contains(searchQuery) ||
                e.Organization.ToLower().Contains(searchQuery) ||
                (e.Description != null && e.Description.ToLower().Contains(searchQuery))
            );
        }

        var totalCount = await query.CountAsync();

        var experiences = await query
            .OrderBy(e => e.DisplayOrder)
            .ThenByDescending(e => e.StartDate)
            .Skip(searchDto.Skip)
            .Take(searchDto.Take)
            .ToListAsync();

        var experienceDtos = new List<ExperienceDto>();
        foreach (var experience in experiences)
        {
            experienceDtos.Add(await MapToExperienceDto(experience, searchDto.IncludeSkills));
        }

        return (experienceDtos, totalCount);
    }

    public async Task<ServiceResponseDto> UpdateExperienceOrderAsync(Guid userId, List<Guid> experienceIds)
    {
        try
        {
            var experiences = await _context.Experiences
                .Where(e => e.UserId == userId && experienceIds.Contains(e.Id))
                .ToListAsync();

            if (experiences.Count != experienceIds.Count)
            {
                return new ServiceResponseDto
                {
                    Success = false,
                    Message = "One or more experiences not found"
                };
            }

            for (int i = 0; i < experienceIds.Count; i++)
            {
                var experience = experiences.First(e => e.Id == experienceIds[i]);
                experience.DisplayOrder = i + 1;
                experience.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();

            await _auditLogService.LogEventAsync(
                null,
                "EXPERIENCE_ORDER_UPDATED",
                "127.0.0.1",
                null,
                true,
                $"Updated display order for {experienceIds.Count} experiences"
            );

            return new ServiceResponseDto
            {
                Success = true,
                Message = "Experience order updated successfully"
            };
        }
        catch (Exception ex)
        {
            await _auditLogService.LogEventAsync(
                null,
                "EXPERIENCE_ORDER_UPDATE_FAILED",
                "127.0.0.1",
                null,
                false,
                null,
                $"Failed to update experience order for user {userId}: {ex.Message}"
            );

            return new ServiceResponseDto
            {
                Success = false,
                Message = "Failed to update experience order",
                ErrorDetails = ex.Message
            };
        }
    }

    public async Task<ServiceResponseDto> AddSkillsToExperienceAsync(Guid userId, Guid experienceId, List<Guid> skillIds)
    {
        try
        {
            var experience = await _context.Experiences
                .FirstOrDefaultAsync(e => e.Id == experienceId && e.UserId == userId);

            if (experience == null)
            {
                return new ServiceResponseDto
                {
                    Success = false,
                    Message = "Experience not found"
                };
            }

            await AddSkillsToExperienceInternalAsync(experienceId, skillIds);
            await _context.SaveChangesAsync();

            await _auditLogService.LogEventAsync(
                userId,
                "SKILLS_ADDED_TO_EXPERIENCE",
                "127.0.0.1",
                null,
                true,
                $"Added {skillIds.Count} skills to experience '{experience.Title}'"
            );

            return new ServiceResponseDto
            {
                Success = true,
                Message = "Skills added to experience successfully"
            };
        }
        catch (Exception ex)
        {
            await _auditLogService.LogEventAsync(
                userId,
                "SKILLS_ADDITION_TO_EXPERIENCE_FAILED",
                "127.0.0.1",
                null,
                false,
                null,
                $"Failed to add skills to experience {experienceId} for user {userId}: {ex.Message}"
            );

            return new ServiceResponseDto
            {
                Success = false,
                Message = "Failed to add skills to experience",
                ErrorDetails = ex.Message
            };
        }
    }

    public async Task<ServiceResponseDto> RemoveSkillsFromExperienceAsync(Guid userId, Guid experienceId, List<Guid> skillIds)
    {
        try
        {
            var experience = await _context.Experiences
                .FirstOrDefaultAsync(e => e.Id == experienceId && e.UserId == userId);

            if (experience == null)
            {
                return new ServiceResponseDto
                {
                    Success = false,
                    Message = "Experience not found"
                };
            }

            var experienceSkills = await _context.ExperienceSkills
                .Where(es => es.ExperienceId == experienceId && skillIds.Contains(es.SkillId))
                .ToListAsync();

            _context.ExperienceSkills.RemoveRange(experienceSkills);
            await _context.SaveChangesAsync();

            await _auditLogService.LogEventAsync(
                userId,
                "SKILLS_REMOVED_FROM_EXPERIENCE",
                "127.0.0.1",
                null,
                true,
                $"Removed {experienceSkills.Count} skills from experience '{experience.Title}'"
            );

            return new ServiceResponseDto
            {
                Success = true,
                Message = "Skills removed from experience successfully"
            };
        }
        catch (Exception ex)
        {
            await _auditLogService.LogEventAsync(
                userId,
                "SKILLS_REMOVAL_FROM_EXPERIENCE_FAILED",
                "127.0.0.1",
                null,
                false,
                null,
                $"Failed to remove skills from experience {experienceId} for user {userId}: {ex.Message}"
            );

            return new ServiceResponseDto
            {
                Success = false,
                Message = "Failed to remove skills from experience",
                ErrorDetails = ex.Message
            };
        }
    }

    public async Task<List<ExperienceDto>> GetExperienceTimelineAsync(Guid userId, bool visibleOnly = true)
    {
        var query = _context.Experiences
            .Where(e => e.UserId == userId);

        if (visibleOnly)
        {
            query = query.Where(e => e.IsVisible);
        }

        var experiences = await query
            .OrderByDescending(e => e.StartDate)
            .ThenByDescending(e => e.EndDate)
            .ToListAsync();

        var experienceDtos = new List<ExperienceDto>();
        foreach (var experience in experiences)
        {
            experienceDtos.Add(await MapToExperienceDto(experience, true));
        }

        return experienceDtos;
    }

    public async Task<List<ExperienceDto>> GetFeaturedExperiencesAsync(Guid userId, bool visibleOnly = true)
    {
        var query = _context.Experiences
            .Where(e => e.UserId == userId && e.IsFeatured);

        if (visibleOnly)
        {
            query = query.Where(e => e.IsVisible);
        }

        var experiences = await query
            .OrderByDescending(e => e.StartDate)
            .ThenBy(e => e.DisplayOrder)
            .ToListAsync();

        var experienceDtos = new List<ExperienceDto>();
        foreach (var experience in experiences)
        {
            experienceDtos.Add(await MapToExperienceDto(experience, true));
        }

        return experienceDtos;
    }

    public async Task<List<ExperienceDto>> GetCurrentExperiencesAsync(Guid userId, bool visibleOnly = true)
    {
        var query = _context.Experiences
            .Where(e => e.UserId == userId && e.IsCurrent);

        if (visibleOnly)
        {
            query = query.Where(e => e.IsVisible);
        }

        var experiences = await query
            .OrderByDescending(e => e.StartDate)
            .ThenBy(e => e.DisplayOrder)
            .ToListAsync();

        var experienceDtos = new List<ExperienceDto>();
        foreach (var experience in experiences)
        {
            experienceDtos.Add(await MapToExperienceDto(experience, true));
        }

        return experienceDtos;
    }

    private async Task AddSkillsToExperienceInternalAsync(Guid experienceId, List<Guid> skillIds)
    {
        if (!skillIds.Any()) return;

        // Validate that skills exist
        var validSkills = await _context.Skills
            .Where(s => skillIds.Contains(s.Id) && s.IsActive)
            .Select(s => s.Id)
            .ToListAsync();

        foreach (var skillId in validSkills)
        {
            // Check if skill is already associated with experience
            var exists = await _context.ExperienceSkills
                .AnyAsync(es => es.ExperienceId == experienceId && es.SkillId == skillId);

            if (!exists)
            {
                var experienceSkill = new ExperienceSkill
                {
                    ExperienceId = experienceId,
                    SkillId = skillId
                };

                _context.ExperienceSkills.Add(experienceSkill);
            }
        }
    }

    private async Task<ExperienceDto> MapToExperienceDto(Experience experience, bool includeSkills = true)
    {
        var dto = new ExperienceDto
        {
            Id = experience.Id,
            UserId = experience.UserId,
            Type = experience.Type,
            Title = experience.Title,
            Organization = experience.Organization,
            Location = experience.Location,
            Description = experience.Description,
            StartDate = experience.StartDate,
            EndDate = experience.EndDate,
            IsCurrent = experience.IsCurrent,
            IsVisible = experience.IsVisible,
            IsFeatured = experience.IsFeatured,
            DisplayOrder = experience.DisplayOrder,
            CreatedAt = experience.CreatedAt,
            UpdatedAt = experience.UpdatedAt,
            Skills = new List<SkillDto>()
        };

        if (includeSkills)
        {
            var skills = await _context.ExperienceSkills
                .Where(es => es.ExperienceId == experience.Id)
                .Include(es => es.Skill)
                .Select(es => es.Skill)
                .OrderBy(s => s.Name)
                .ToListAsync();

            foreach (var skill in skills)
            {
                dto.Skills.Add(new SkillDto
                {
                    Id = skill.Id,
                    Name = skill.Name,
                    Description = skill.Description,
                    Category = skill.Category,
                    IsSystemManaged = skill.IsSystemManaged,
                    IsActive = skill.IsActive,
                    CreatedAt = skill.CreatedAt,
                    UpdatedAt = skill.UpdatedAt
                });
            }
        }

        return dto;
    }
}