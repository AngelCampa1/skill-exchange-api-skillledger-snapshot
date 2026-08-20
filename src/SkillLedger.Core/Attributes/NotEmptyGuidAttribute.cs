using System.ComponentModel.DataAnnotations;

namespace SkillLedger.Core.Attributes;

/// <summary>
/// Validation attribute to ensure a Guid is not empty
/// </summary>
public class NotEmptyGuidAttribute : ValidationAttribute
{
    public override bool IsValid(object? value)
    {
        if (value is Guid guid)
        {
            return guid != Guid.Empty;
        }

        return false;
    }

    public override string FormatErrorMessage(string name)
    {
        return $"The {name} field cannot be empty.";
    }
}