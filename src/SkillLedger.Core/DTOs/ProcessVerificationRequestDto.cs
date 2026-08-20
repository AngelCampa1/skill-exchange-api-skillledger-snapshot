using System.ComponentModel.DataAnnotations;

namespace SkillLedger.Core.DTOs;

/// <summary>
/// DTO for processing a verification request
/// </summary>
public class ProcessVerificationRequestDto
{
    /// <summary>
    /// Whether the request is approved
    /// </summary>
    [Required]
    public bool Approved { get; set; }

    /// <summary>
    /// Notes from the reviewer
    /// </summary>
    [MaxLength(2000)]
    public string? ReviewNotes { get; set; }
}