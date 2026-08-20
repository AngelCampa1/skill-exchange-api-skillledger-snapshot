using System.ComponentModel.DataAnnotations;
using System.Globalization;

namespace SkillLedger.Core.Attributes;

/// <summary>
/// Custom validation attribute for DateTime ranges that handles culture-invariant parsing
/// </summary>
public class DateTimeRangeAttribute : ValidationAttribute
{
    private readonly string _minimum;
    private readonly string _maximum;

    public DateTimeRangeAttribute(string minimum, string maximum)
    {
        _minimum = minimum;
        _maximum = maximum;

        // Set the error message if not provided
        if (ErrorMessage == null)
        {
            ErrorMessage = $"Date must be between {minimum} and {maximum}";
        }
    }

    /// <summary>
    /// Override of IsValid method to handle culture-invariant DateTime parsing
    /// </summary>
    public override bool IsValid(object? value)
    {
        if (value == null)
            return true; // RequiredAttribute should handle null validation

        if (value is DateTime dateTime)
        {
            // Parse the minimum and maximum values using culture-invariant format
            if (DateTime.TryParseExact(_minimum, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var minDate) &&
                DateTime.TryParseExact(_maximum, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var maxDate))
            {
                return dateTime >= minDate && dateTime <= maxDate;
            }
        }

        return false;
    }

    /// <summary>
    /// Get the minimum value as DateTime
    /// </summary>
    public DateTime? MinimumDate =>
        DateTime.TryParseExact(_minimum, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var minDate)
            ? minDate : null;

    /// <summary>
    /// Get the maximum value as DateTime
    /// </summary>
    public DateTime? MaximumDate =>
        DateTime.TryParseExact(_maximum, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var maxDate)
            ? maxDate : null;
}