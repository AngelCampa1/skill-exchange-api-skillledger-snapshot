using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using SkillLedger.Core.DTOs;
using SkillLedger.Core.Interfaces;
using System.Security.Claims;

namespace SkillLedger.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
// [EnableRateLimiting("DefaultPolicy")] // Disabled for testing  
public class ExperienceController : ControllerBase
{
    private readonly IExperienceService _experienceService;

    public ExperienceController(IExperienceService experienceService)
    {
        _experienceService = experienceService;
    }

    /// <summary>
    /// Creates a new experience for the current user
    /// </summary>
    /// <param name="createExperienceDto">Experience creation data</param>
    /// <returns>Service response with created experience</returns>
    [HttpPost]
    // [EnableRateLimiting("ProfileCreationPolicy")] // Disabled for testing
    public async Task<ActionResult<ServiceResponseDto>> CreateExperience([FromBody] CreateExperienceDto createExperienceDto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized(new ServiceResponseDto
            {
                Success = false,
                Message = "User not authenticated"
            });
        }

        var result = await _experienceService.CreateExperienceAsync(userId.Value, createExperienceDto);

        if (!result.Success)
        {
            return BadRequest(result);
        }

        // Ensure Data is not null after successful creation
        if (result.Data == null)
        {
            return StatusCode(500, new ServiceResponseDto
            {
                Success = false,
                Message = "Experience creation succeeded but data is missing"
            });
        }

        return CreatedAtAction(nameof(GetExperience), new { experienceId = ((ExperienceDto)result.Data).Id }, result);
    }

    /// <summary>
    /// Updates an existing experience for the current user
    /// </summary>
    /// <param name="experienceId">Experience ID</param>
    /// <param name="updateExperienceDto">Experience update data</param>
    /// <returns>Service response with updated experience</returns>
    [HttpPut("{experienceId:guid}")]
    public async Task<ActionResult<ServiceResponseDto>> UpdateExperience(Guid experienceId, [FromBody] UpdateExperienceDto updateExperienceDto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized(new ServiceResponseDto
            {
                Success = false,
                Message = "User not authenticated"
            });
        }

        var result = await _experienceService.UpdateExperienceAsync(userId.Value, experienceId, updateExperienceDto);

        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Deletes an experience for the current user
    /// </summary>
    /// <param name="experienceId">Experience ID</param>
    /// <returns>Service response</returns>
    [HttpDelete("{experienceId:guid}")]
    public async Task<ActionResult<ServiceResponseDto>> DeleteExperience(Guid experienceId)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized(new ServiceResponseDto
            {
                Success = false,
                Message = "User not authenticated"
            });
        }

        var result = await _experienceService.DeleteExperienceAsync(userId.Value, experienceId);

        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Gets a specific experience for the current user
    /// </summary>
    /// <param name="experienceId">Experience ID</param>
    /// <param name="includeSkills">Whether to include skills</param>
    /// <returns>Experience data</returns>
    [HttpGet("my-experience/{experienceId:guid}")]
    public async Task<ActionResult<ExperienceDto>> GetExperience(Guid experienceId, [FromQuery] bool includeSkills = true)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized();
        }

        var experience = await _experienceService.GetExperienceByIdAsync(userId.Value, experienceId, includeSkills);

        if (experience == null)
        {
            return NotFound(new { Message = "Experience not found" });
        }

        return Ok(experience);
    }

    /// <summary>
    /// Gets all experiences for the current user
    /// </summary>
    /// <param name="includeSkills">Whether to include skills</param>
    /// <returns>List of user experiences</returns>
    [HttpGet("my-experiences")]
    public async Task<ActionResult<List<ExperienceDto>>> GetMyExperiences([FromQuery] bool includeSkills = true)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized();
        }

        var experiences = await _experienceService.GetUserExperiencesAsync(userId.Value, visibleOnly: false, includeSkills);
        return Ok(experiences);
    }

    /// <summary>
    /// Gets experiences for a specific user (public profile view)
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="includeSkills">Whether to include skills</param>
    /// <param name="type">Filter by experience type</param>
    /// <param name="skillName">Filter by skill name</param>
    /// <returns>List of visible user experiences</returns>
    [HttpGet("user/{userId:guid}")]
    [AllowAnonymous]
    public async Task<ActionResult<List<ExperienceDto>>> GetUserExperiences(
        Guid userId,
        [FromQuery] bool includeSkills = true,
        [FromQuery] string? type = null,
        [FromQuery] string? skillName = null,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 50)
    {
        // BUG-MED-001 FIX: Add pagination to prevent memory exhaustion
        // Limit pageSize to prevent abuse
        pageSize = Math.Min(pageSize, 100);
        pageNumber = Math.Max(pageNumber, 1);

        var experiences = await _experienceService.GetUserExperiencesAsync(userId, visibleOnly: true, includeSkills);

        // Apply filters if provided
        if (!string.IsNullOrEmpty(type) && Enum.TryParse<SkillLedger.Core.Enums.ExperienceType>(type, true, out var experienceType))
        {
            experiences = experiences.Where(e => e.Type == experienceType).ToList();
        }

        if (!string.IsNullOrEmpty(skillName))
        {
            experiences = experiences.Where(e => e.Skills.Any(s => s.Name.Contains(skillName, StringComparison.OrdinalIgnoreCase))).ToList();
        }

        // BUG-MED-001 FIX: Apply pagination
        var totalCount = experiences.Count;
        var paginatedExperiences = experiences
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        // Return pagination metadata in response headers
        Response.Headers.Append("X-Total-Count", totalCount.ToString());
        Response.Headers.Append("X-Page-Number", pageNumber.ToString());
        Response.Headers.Append("X-Page-Size", pageSize.ToString());
        Response.Headers.Append("X-Total-Pages", ((int)Math.Ceiling(totalCount / (double)pageSize)).ToString());

        return Ok(paginatedExperiences);
    }

    /// <summary>
    /// Gets experiences for a specific user (alternative route for compatibility)
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="includeSkills">Whether to include skills</param>
    /// <param name="type">Filter by experience type</param>
    /// <param name="skillName">Filter by skill name</param>
    /// <returns>List of visible user experiences</returns>
    [HttpGet("{userId:guid}")]
    [AllowAnonymous]
    public async Task<ActionResult<List<ExperienceDto>>> GetExperiencesForUser(
        Guid userId,
        [FromQuery] bool includeSkills = true,
        [FromQuery] string? type = null,
        [FromQuery] string? skillName = null,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 50)
    {
        // BUG-MED-001 FIX: Add pagination to prevent memory exhaustion (same as GetUserExperiences)
        pageSize = Math.Min(pageSize, 100);
        pageNumber = Math.Max(pageNumber, 1);

        var experiences = await _experienceService.GetUserExperiencesAsync(userId, visibleOnly: true, includeSkills);

        // Apply filters if provided
        if (!string.IsNullOrEmpty(type) && Enum.TryParse<SkillLedger.Core.Enums.ExperienceType>(type, true, out var experienceType))
        {
            experiences = experiences.Where(e => e.Type == experienceType).ToList();
        }

        if (!string.IsNullOrEmpty(skillName))
        {
            experiences = experiences.Where(e => e.Skills.Any(s => s.Name.Contains(skillName, StringComparison.OrdinalIgnoreCase))).ToList();
        }

        // BUG-MED-001 FIX: Apply pagination
        var totalCount = experiences.Count;
        var paginatedExperiences = experiences
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        // Return pagination metadata in response headers
        Response.Headers.Append("X-Total-Count", totalCount.ToString());
        Response.Headers.Append("X-Page-Number", pageNumber.ToString());
        Response.Headers.Append("X-Page-Size", pageSize.ToString());
        Response.Headers.Append("X-Total-Pages", ((int)Math.Ceiling(totalCount / (double)pageSize)).ToString());

        return Ok(paginatedExperiences);
    }

    /// <summary>
    /// Gets experience timeline for a specific user (chronological order)
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <returns>Timeline of user experiences</returns>
    [HttpGet("user/{userId:guid}/timeline")]
    [AllowAnonymous]
    public async Task<ActionResult<List<ExperienceDto>>> GetExperienceTimeline(Guid userId)
    {
        var timeline = await _experienceService.GetExperienceTimelineAsync(userId, visibleOnly: true);
        return Ok(timeline);
    }

    /// <summary>
    /// Gets experience timeline for the current user
    /// </summary>
    /// <returns>Timeline of user experiences</returns>
    [HttpGet("my-experiences/timeline")]
    public async Task<ActionResult<List<ExperienceDto>>> GetMyExperienceTimeline()
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized();
        }

        var timeline = await _experienceService.GetExperienceTimelineAsync(userId.Value, visibleOnly: false);
        return Ok(timeline);
    }

    /// <summary>
    /// Gets featured experiences for a specific user
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <returns>List of featured experiences</returns>
    [HttpGet("user/{userId:guid}/featured")]
    [AllowAnonymous]
    public async Task<ActionResult<List<ExperienceDto>>> GetFeaturedExperiences(Guid userId)
    {
        var experiences = await _experienceService.GetFeaturedExperiencesAsync(userId, visibleOnly: true);
        return Ok(experiences);
    }

    /// <summary>
    /// Gets featured experiences for the current user
    /// </summary>
    /// <returns>List of featured experiences</returns>
    [HttpGet("my-experiences/featured")]
    public async Task<ActionResult<List<ExperienceDto>>> GetMyFeaturedExperiences()
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized();
        }

        var experiences = await _experienceService.GetFeaturedExperiencesAsync(userId.Value, visibleOnly: false);
        return Ok(experiences);
    }

    /// <summary>
    /// Gets current (ongoing) experiences for a specific user
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <returns>List of current experiences</returns>
    [HttpGet("user/{userId:guid}/current")]
    [AllowAnonymous]
    public async Task<ActionResult<List<ExperienceDto>>> GetCurrentExperiences(Guid userId)
    {
        var experiences = await _experienceService.GetCurrentExperiencesAsync(userId, visibleOnly: true);
        return Ok(experiences);
    }

    /// <summary>
    /// Gets current (ongoing) experiences for the current user
    /// </summary>
    /// <returns>List of current experiences</returns>
    [HttpGet("my-experiences/current")]
    public async Task<ActionResult<List<ExperienceDto>>> GetMyCurrentExperiences()
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized();
        }

        var experiences = await _experienceService.GetCurrentExperiencesAsync(userId.Value, visibleOnly: false);
        return Ok(experiences);
    }

    /// <summary>
    /// Searches experiences with filtering and pagination
    /// </summary>
    /// <param name="searchDto">Search criteria</param>
    /// <returns>Paginated list of experiences</returns>
    [HttpPost("search")]
    public async Task<ActionResult<object>> SearchExperiences([FromBody] ExperienceSearchDto searchDto)
    {
        // If no user ID provided, use current user for authenticated requests
        if (!searchDto.UserId.HasValue && User.Identity?.IsAuthenticated == true)
        {
            searchDto.UserId = GetCurrentUserId();
        }

        var (experiences, totalCount) = await _experienceService.SearchExperiencesAsync(searchDto);

        return Ok(new
        {
            Experiences = experiences,
            TotalCount = totalCount,
            Page = (searchDto.Skip / searchDto.Take) + 1,
            PageSize = searchDto.Take,
            TotalPages = (int)Math.Ceiling((double)totalCount / searchDto.Take)
        });
    }

    /// <summary>
    /// Updates the display order of experiences for the current user
    /// </summary>
    /// <param name="experienceIds">Ordered list of experience IDs</param>
    /// <returns>Service response</returns>
    [HttpPut("my-experiences/reorder")]
    public async Task<ActionResult<ServiceResponseDto>> UpdateExperienceOrder([FromBody] List<Guid> experienceIds)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized(new ServiceResponseDto
            {
                Success = false,
                Message = "User not authenticated"
            });
        }

        var result = await _experienceService.UpdateExperienceOrderAsync(userId.Value, experienceIds);

        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Adds skills to an experience
    /// </summary>
    /// <param name="experienceId">Experience ID</param>
    /// <param name="skillIds">List of skill IDs to add</param>
    /// <returns>Service response</returns>
    [HttpPost("{experienceId:guid}/skills")]
    public async Task<ActionResult<ServiceResponseDto>> AddSkillsToExperience(Guid experienceId, [FromBody] List<Guid> skillIds)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized(new ServiceResponseDto
            {
                Success = false,
                Message = "User not authenticated"
            });
        }

        var result = await _experienceService.AddSkillsToExperienceAsync(userId.Value, experienceId, skillIds);

        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Removes skills from an experience
    /// </summary>
    /// <param name="experienceId">Experience ID</param>
    /// <param name="skillIds">List of skill IDs to remove</param>
    /// <returns>Service response</returns>
    [HttpDelete("{experienceId:guid}/skills")]
    public async Task<ActionResult<ServiceResponseDto>> RemoveSkillsFromExperience(Guid experienceId, [FromBody] List<Guid> skillIds)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized(new ServiceResponseDto
            {
                Success = false,
                Message = "User not authenticated"
            });
        }

        var result = await _experienceService.RemoveSkillsFromExperienceAsync(userId.Value, experienceId, skillIds);

        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    // Test compatibility endpoints - Allow operations for specific users

    /// <summary>
    /// Creates a new experience for a specific user (admin/test compatibility endpoint)
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="createExperienceDto">Experience creation data</param>
    /// <returns>Service response with created experience</returns>
    [HttpPost("{userId:guid}")]
    [Authorize]
    // BUG-MEDIUM-003 FIX: Enable rate limiting in non-DEBUG builds
#if !DEBUG
    [EnableRateLimiting("ProfileCreationPolicy")]
#endif
    public async Task<ActionResult<ExperienceDto>> CreateExperienceForUser(Guid userId, [FromBody] CreateExperienceDto createExperienceDto)
    {
        if (!CanActForUser(userId))
        {
            return Forbid();
        }

        var result = await _experienceService.CreateExperienceAsync(userId, createExperienceDto);

        if (!result.Success)
        {
            return BadRequest(result);
        }

        // Ensure Data is not null after successful creation
        if (result.Data == null)
        {
            return StatusCode(500, new ServiceResponseDto
            {
                Success = false,
                Message = "Experience creation succeeded but data is missing"
            });
        }

        return CreatedAtAction(nameof(GetExperienceForUser), new { userId, experienceId = ((ExperienceDto)result.Data).Id }, result.Data);
    }

    /// <summary>
    /// Updates an existing experience for a specific user (admin/test compatibility endpoint)
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="experienceId">Experience ID</param>
    /// <param name="updateExperienceDto">Experience update data</param>
    /// <returns>Service response with updated experience</returns>
    [HttpPut("{userId:guid}/{experienceId:guid}")]
    [Authorize]
    public async Task<ActionResult<ExperienceDto>> UpdateExperienceForUser(Guid userId, Guid experienceId, [FromBody] UpdateExperienceDto updateExperienceDto)
    {
        if (!CanActForUser(userId))
        {
            return Forbid();
        }

        var result = await _experienceService.UpdateExperienceAsync(userId, experienceId, updateExperienceDto);

        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result.Data);
    }

    /// <summary>
    /// Deletes an experience for a specific user (admin/test compatibility endpoint)
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="experienceId">Experience ID</param>
    /// <returns>Service response</returns>
    [HttpDelete("{userId:guid}/{experienceId:guid}")]
    [Authorize]
    public async Task<ActionResult> DeleteExperienceForUser(Guid userId, Guid experienceId)
    {
        if (!CanActForUser(userId))
        {
            return Forbid();
        }

        var result = await _experienceService.DeleteExperienceAsync(userId, experienceId);

        if (!result.Success)
        {
            return BadRequest(result);
        }

        return NoContent();
    }

    /// <summary>
    /// Gets a specific experience for a specific user (admin/test compatibility endpoint)
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="experienceId">Experience ID</param>
    /// <param name="includeSkills">Whether to include skills</param>
    /// <returns>Experience data</returns>
    [HttpGet("{userId:guid}/{experienceId:guid}")]
    [Authorize]
    public async Task<ActionResult<ExperienceDto>> GetExperienceForUser(Guid userId, Guid experienceId, [FromQuery] bool includeSkills = true)
    {
        if (!CanActForUser(userId))
        {
            return Forbid();
        }

        var experience = await _experienceService.GetExperienceByIdAsync(userId, experienceId, includeSkills);

        if (experience == null)
        {
            return NotFound(new { Message = "Experience not found" });
        }

        return Ok(experience);
    }

    private Guid? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(userIdClaim, out var userId) ? userId : null;
    }

    private bool CanActForUser(Guid targetUserId)
    {
        var currentUserId = GetCurrentUserId();
        return currentUserId == targetUserId || User.IsInRole("Admin");
    }
}
