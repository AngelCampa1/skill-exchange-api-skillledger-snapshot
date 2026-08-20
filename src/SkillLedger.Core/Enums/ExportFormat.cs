namespace SkillLedger.Core.Enums;

/// <summary>
/// Supported export formats for financial reports and data
/// </summary>
public enum ExportFormat
{
    /// <summary>
    /// No format specified (invalid)
    /// </summary>
    None = 0,

    /// <summary>
    /// Comma-separated values format
    /// Suitable for spreadsheet applications
    /// </summary>
    CSV = 1,

    /// <summary>
    /// Portable Document Format
    /// Suitable for formal reports and presentations
    /// </summary>
    PDF = 2,

    /// <summary>
    /// JavaScript Object Notation
    /// Suitable for API integrations and data exchange
    /// </summary>
    JSON = 3,

    /// <summary>
    /// Extensible Markup Language
    /// Suitable for structured data exchange and legacy systems
    /// </summary>
    XML = 4,

    /// <summary>
    /// Microsoft Excel format
    /// Suitable for advanced spreadsheet analysis
    /// </summary>
    Excel = 5
}