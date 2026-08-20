using System.ComponentModel.DataAnnotations;

namespace SkillLedger.Core.Attributes;

/// <summary>
/// Validation attribute for decimal range validation
/// </summary>
public class DecimalRangeAttribute : ValidationAttribute
{
    public decimal Minimum { get; }
    public decimal Maximum { get; }

    public DecimalRangeAttribute(double minimum, double maximum)
    {
        Minimum = (decimal)minimum;
        Maximum = (decimal)maximum;
    }

    public override bool IsValid(object? value)
    {
        if (value is decimal decimalValue)
        {
            return decimalValue >= Minimum && decimalValue <= Maximum;
        }

        return false;
    }

    public override string FormatErrorMessage(string name)
    {
        return $"The {name} field must be between {Minimum} and {Maximum}.";
    }
}