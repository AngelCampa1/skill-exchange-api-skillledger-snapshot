using SkillLedger.Core.Enums;
using System.ComponentModel.DataAnnotations;

namespace SkillLedger.Core.DTOs;

public class CreateReviewDto
{
    [Required(ErrorMessage = "Project ID is required")]
    public Guid ProjectId { get; set; }

    [Required(ErrorMessage = "Reviewee ID is required")]
    public Guid RevieweeId { get; set; }

    [Required(ErrorMessage = "Review type is required")]
    public ProjectReviewType Type { get; set; }

    [Required(ErrorMessage = "Overall rating is required")]
    [Range(1, 10, ErrorMessage = "Overall rating must be between 1 and 10")]
    public int OverallRating { get; set; }

    [Range(1, 10, ErrorMessage = "Quality rating must be between 1 and 10")]
    public int? QualityRating { get; set; }

    [Range(1, 10, ErrorMessage = "Communication rating must be between 1 and 10")]
    public int? CommunicationRating { get; set; }

    [Range(1, 10, ErrorMessage = "Timeliness rating must be between 1 and 10")]
    public int? TimelinessRating { get; set; }

    [Range(1, 10, ErrorMessage = "Professionalism rating must be between 1 and 10")]
    public int? ProfessionalismRating { get; set; }

    [Required(ErrorMessage = "Review text is required")]
    [MinLength(25, ErrorMessage = "Review text must be at least 25 characters long")]
    [MaxLength(2000, ErrorMessage = "Review text cannot exceed 2000 characters")]
    public string ReviewText { get; set; } = null!;

    /// <summary>
    /// List of photo file IDs to attach to the review
    /// </summary>
    public List<Guid> PhotoAttachmentIds { get; set; } = new List<Guid>();
}

public class ReviewResponseDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public Guid? ReviewId { get; set; }
    public ProjectReviewStatus? Status { get; set; }
}

public class ReviewDisplayDto
{
    public Guid Id { get; set; }
    public Guid ProjectId { get; set; }
    public string ProjectTitle { get; set; } = string.Empty;
    public Guid ReviewerId { get; set; }
    public string ReviewerName { get; set; } = string.Empty;
    public Guid RevieweeId { get; set; }
    public string RevieweeName { get; set; } = string.Empty;
    public ProjectReviewType Type { get; set; }
    public int OverallRating { get; set; }
    public int? QualityRating { get; set; }
    public int? CommunicationRating { get; set; }
    public int? TimelinessRating { get; set; }
    public int? ProfessionalismRating { get; set; }
    public double CalculatedAverageRating { get; set; }
    public string ReviewText { get; set; } = string.Empty;
    public string? ResponseText { get; set; }
    public ProjectReviewStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? PublishedAt { get; set; }
    public bool HasPhotoAttachments { get; set; }
    public int PhotoAttachmentCount { get; set; }
    public List<ReviewPhotoDto> PhotoAttachments { get; set; } = new List<ReviewPhotoDto>();
}

public class ReviewPhotoDto
{
    public Guid Id { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public long FileSize { get; set; }
}

public class AddReviewResponseDto
{
    [Required(ErrorMessage = "Review ID is required")]
    public Guid ReviewId { get; set; }

    [Required(ErrorMessage = "Response text is required")]
    [MinLength(10, ErrorMessage = "Response text must be at least 10 characters long")]
    [MaxLength(1000, ErrorMessage = "Response text cannot exceed 1000 characters")]
    public string ResponseText { get; set; } = null!;
}

public class ReviewSummaryDto
{
    public Guid UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public int TotalReviewsReceived { get; set; }
    public double AverageOverallRating { get; set; }
    public double AverageQualityRating { get; set; }
    public double AverageCommunicationRating { get; set; }
    public double AverageTimelinessRating { get; set; }
    public double AverageProfessionalismRating { get; set; }
    public int ClientReviewsCount { get; set; }
    public int ProviderReviewsCount { get; set; }
    public DateTime? MostRecentReviewDate { get; set; }
}

public class BlindReviewStatusDto
{
    public Guid ProjectId { get; set; }
    public ProjectReviewType ReviewType { get; set; }
    public bool HasUserSubmittedReview { get; set; }
    public bool HasCounterpartSubmittedReview { get; set; }
    public bool AreReviewsPublished { get; set; }
    public DateTime? UserSubmissionDate { get; set; }
    public DateTime? CounterpartSubmissionDate { get; set; }
    public DateTime? PublishDate { get; set; }
}

/// <summary>
/// DTO for filtering user reviews
/// </summary>
public class ReviewFilterDto
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public ProjectReviewType? ReviewType { get; set; }
    public string SortBy { get; set; } = "CreatedAt";
    public bool SortDescending { get; set; } = true;
    public bool PublicOnly { get; set; } = true;
}

/// <summary>
/// DTO for review evidence file uploads
/// </summary>
// Note: ReviewEvidenceUploadDto moved to API layer due to IFormFile dependency

/// <summary>
/// DTO for paginated review results
/// </summary>
public class PaginatedReviewsDto
{
    public List<ReviewDisplayDto> Reviews { get; set; } = new List<ReviewDisplayDto>();
    public int TotalCount { get; set; }
    public ReviewSummaryDto? Statistics { get; set; }
}

/// <summary>
/// DTO for project review results
/// </summary>
public class ProjectReviewsDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public List<ReviewDisplayDto> Reviews { get; set; } = new List<ReviewDisplayDto>();
    public bool CanSubmitClientReview { get; set; }
    public bool CanSubmitProviderReview { get; set; }
}

/// <summary>
/// DTO for file upload results
/// </summary>
public class FileUploadResultDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public List<Guid> FileIds { get; set; } = new List<Guid>();
}