using SkillLedger.Core.DTOs;
using SkillLedger.Core.Entities;

namespace SkillLedger.Core.Interfaces;

public interface IExperienceService
{
    /// <summary>
    /// Creates a new experience for a user
    /// </summary>
    Task<ServiceResponseDto> CreateExperienceAsync(Guid userId, CreateExperienceDto dto);

    /// <summary>
    /// Updates an existing experience
    /// </summary>
    Task<ServiceResponseDto> UpdateExperienceAsync(Guid userId, Guid experienceId, UpdateExperienceDto dto);

    /// <summary>
    /// Deletes an experience
    /// </summary>
    Task<ServiceResponseDto> DeleteExperienceAsync(Guid userId, Guid experienceId);

    /// <summary>
    /// Gets a specific experience by ID
    /// </summary>
    Task<ExperienceDto?> GetExperienceByIdAsync(Guid userId, Guid experienceId, bool includeSkills = true);

    /// <summary>
    /// Gets all experiences for a user
    /// </summary>
    Task<List<ExperienceDto>> GetUserExperiencesAsync(Guid userId, bool visibleOnly = true, bool includeSkills = true);

    /// <summary>
    /// Searches experiences with filtering and pagination
    /// </summary>
    Task<(List<ExperienceDto> Experiences, int TotalCount)> SearchExperiencesAsync(ExperienceSearchDto searchDto);

    /// <summary>
    /// Updates the display order of experiences for a user
    /// </summary>
    Task<ServiceResponseDto> UpdateExperienceOrderAsync(Guid userId, List<Guid> experienceIds);

    /// <summary>
    /// Adds skills to an experience
    /// </summary>
    Task<ServiceResponseDto> AddSkillsToExperienceAsync(Guid userId, Guid experienceId, List<Guid> skillIds);

    /// <summary>
    /// Removes skills from an experience
    /// </summary>
    Task<ServiceResponseDto> RemoveSkillsFromExperienceAsync(Guid userId, Guid experienceId, List<Guid> skillIds);

    /// <summary>
    /// Gets the timeline of experiences for a user (ordered by date)
    /// </summary>
    Task<List<ExperienceDto>> GetExperienceTimelineAsync(Guid userId, bool visibleOnly = true);

    /// <summary>
    /// Gets featured experiences for a user
    /// </summary>
    Task<List<ExperienceDto>> GetFeaturedExperiencesAsync(Guid userId, bool visibleOnly = true);

    /// <summary>
    /// Gets current (ongoing) experiences for a user
    /// </summary>
    Task<List<ExperienceDto>> GetCurrentExperiencesAsync(Guid userId, bool visibleOnly = true);
}