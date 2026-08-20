using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace SkillLedger.Core.Validators;

/// <summary>
/// Validates that experience date range is valid:
/// - StartDate is not in the future
/// - EndDate (if provided) is after StartDate
/// - EndDate is not in the future (unless IsCurrent is true)
/// </summary>
public class ExperienceDateRangeAttribute : ValidationAttribute
{
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        var instance = validationContext.ObjectInstance;
        var type = instance.GetType();

        // Get properties
        var startDateProp = type.GetProperty("StartDate");
        var endDateProp = type.GetProperty("EndDate");
        var isCurrentProp = type.GetProperty("IsCurrent");

        if (startDateProp == null)
        {
            return new ValidationResult("StartDate property not found.");
        }

        var startDate = startDateProp.GetValue(instance) as DateTime?;
        if (!startDate.HasValue)
        {
            return new ValidationResult("Start date is required.");
        }

        var now = DateTime.UtcNow;

        // Validate StartDate is not in the future
        if (startDate.Value > now)
        {
            return new ValidationResult("Start date cannot be in the future.");
        }

        // Get EndDate and IsCurrent
        var endDate = endDateProp?.GetValue(instance) as DateTime?;
        var isCurrent = (isCurrentProp?.GetValue(instance) as bool?) ?? false;

        // If not current, EndDate should be provided
        if (!isCurrent && endDate.HasValue)
        {
            // Validate EndDate is after StartDate
            if (endDate.Value <= startDate.Value)
            {
                return new ValidationResult("End date must be after start date.");
            }

            // Validate EndDate is not in the future for completed experiences
            if (endDate.Value > now)
            {
                return new ValidationResult("End date cannot be in the future for completed experiences.");
            }
        }

        // If current, EndDate should be null or empty
        if (isCurrent && endDate.HasValue)
        {
            return new ValidationResult("Current experiences should not have an end date.");
        }

        return ValidationResult.Success;
    }
}
