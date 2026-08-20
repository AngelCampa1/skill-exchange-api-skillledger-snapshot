using SkillLedger.Core.DTOs;
using SkillLedger.Core.Enums;

namespace SkillLedger.Core.Interfaces;

/// <summary>
/// Service interface for exporting financial data in multiple formats
/// Supports CSV, PDF, JSON, XML, and Excel exports with customizable templates
/// </summary>
public interface IFinancialExportService
{
    #region CSV Export

    /// <summary>
    /// Export transaction data to CSV format
    /// </summary>
    /// <param name="data">Transaction data to export</param>
    /// <param name="includeHeaders">Whether to include column headers</param>
    /// <returns>CSV content as string</returns>
    Task<string> ExportToCsvAsync(IEnumerable<TransactionSummary> data, bool includeHeaders = true);

    /// <summary>
    /// Export credit summary report to CSV format
    /// </summary>
    /// <param name="report">Credit summary report to export</param>
    /// <returns>CSV content as string</returns>
    Task<string> ExportReportToCsvAsync(CreditSummaryReport report);

    /// <summary>
    /// Export user dashboard data to CSV format
    /// </summary>
    /// <param name="dashboardData">Dashboard data to export</param>
    /// <returns>CSV content as string</returns>
    Task<string> ExportDashboardToCsvAsync(UserDashboardData dashboardData);

    #endregion

    #region PDF Export

    /// <summary>
    /// Export credit summary report to PDF format
    /// </summary>
    /// <param name="report">Credit summary report to export</param>
    /// <param name="includeCharts">Whether to include charts and visualizations</param>
    /// <returns>PDF content as byte array</returns>
    Task<byte[]> ExportReportToPdfAsync(CreditSummaryReport report, bool includeCharts = true);

    /// <summary>
    /// Export transaction history to PDF format with formatting
    /// </summary>
    /// <param name="userId">User ID for report header</param>
    /// <param name="transactions">Transaction data</param>
    /// <param name="startDate">Report start date</param>
    /// <param name="endDate">Report end date</param>
    /// <returns>PDF content as byte array</returns>
    Task<byte[]> ExportTransactionHistoryToPdfAsync(
        Guid userId,
        IEnumerable<TransactionSummary> transactions,
        DateTime startDate,
        DateTime endDate);

    /// <summary>
    /// Export user dashboard to PDF format
    /// </summary>
    /// <param name="dashboardData">Dashboard data to export</param>
    /// <param name="includeCharts">Whether to include charts</param>
    /// <returns>PDF content as byte array</returns>
    Task<byte[]> ExportDashboardToPdfAsync(UserDashboardData dashboardData, bool includeCharts = true);

    #endregion

    #region JSON Export

    /// <summary>
    /// Export financial data to JSON format
    /// </summary>
    /// <param name="data">Data object to export</param>
    /// <param name="formatOutput">Whether to format JSON with indentation</param>
    /// <returns>JSON content as string</returns>
    Task<string> ExportToJsonAsync<T>(T data, bool formatOutput = true);

    /// <summary>
    /// Export credit summary report to structured JSON
    /// </summary>
    /// <param name="report">Credit summary report to export</param>
    /// <returns>JSON content as string</returns>
    Task<string> ExportReportToJsonAsync(CreditSummaryReport report);

    /// <summary>
    /// Export analytics data to JSON format
    /// </summary>
    /// <param name="analyticsData">Analytics data to export</param>
    /// <returns>JSON content as string</returns>
    Task<string> ExportAnalyticsToJsonAsync(AnalyticsData analyticsData);

    #endregion

    #region XML Export

    /// <summary>
    /// Export financial data to XML format
    /// </summary>
    /// <param name="data">Data object to export</param>
    /// <param name="rootElementName">Root XML element name</param>
    /// <returns>XML content as string</returns>
    Task<string> ExportToXmlAsync<T>(T data, string rootElementName = "FinancialData");

    /// <summary>
    /// Export credit summary report to XML format
    /// </summary>
    /// <param name="report">Credit summary report to export</param>
    /// <returns>XML content as string</returns>
    Task<string> ExportReportToXmlAsync(CreditSummaryReport report);

    /// <summary>
    /// Export transaction history to XML format
    /// </summary>
    /// <param name="transactions">Transaction data to export</param>
    /// <returns>XML content as string</returns>
    Task<string> ExportTransactionHistoryToXmlAsync(IEnumerable<TransactionSummary> transactions);

    #endregion

    #region Excel Export

    /// <summary>
    /// Export financial data to Excel format (.xlsx)
    /// </summary>
    /// <param name="report">Credit summary report to export</param>
    /// <param name="includeCharts">Whether to include charts in Excel</param>
    /// <returns>Excel content as byte array</returns>
    Task<byte[]> ExportReportToExcelAsync(CreditSummaryReport report, bool includeCharts = true);

    /// <summary>
    /// Export transaction history to Excel with multiple sheets
    /// </summary>
    /// <param name="userId">User ID for report metadata</param>
    /// <param name="transactions">Transaction data</param>
    /// <param name="categoryBreakdown">Category breakdown data</param>
    /// <param name="startDate">Report start date</param>
    /// <param name="endDate">Report end date</param>
    /// <returns>Excel content as byte array</returns>
    Task<byte[]> ExportTransactionHistoryToExcelAsync(
        Guid userId,
        IEnumerable<TransactionSummary> transactions,
        IEnumerable<TransactionCategoryBreakdown> categoryBreakdown,
        DateTime startDate,
        DateTime endDate);

    /// <summary>
    /// Export analytics dashboard to Excel with multiple worksheets
    /// </summary>
    /// <param name="dashboardData">Dashboard data to export</param>
    /// <param name="analyticsData">Additional analytics data</param>
    /// <returns>Excel content as byte array</returns>
    Task<byte[]> ExportDashboardToExcelAsync(UserDashboardData dashboardData, AnalyticsData analyticsData);

    #endregion

    #region Template Management

    /// <summary>
    /// Get available export templates for a format
    /// </summary>
    /// <param name="format">Export format</param>
    /// <returns>List of available templates</returns>
    Task<List<ExportTemplate>> GetAvailableTemplatesAsync(ExportFormat format);

    /// <summary>
    /// Export using a custom template
    /// </summary>
    /// <param name="data">Data to export</param>
    /// <param name="templateId">Template ID to use</param>
    /// <param name="customParameters">Custom parameters for template</param>
    /// <returns>Export result with formatted content</returns>
    Task<FinancialExportResult> ExportWithTemplateAsync<T>(
        T data,
        string templateId,
        Dictionary<string, object>? customParameters = null);

    #endregion

    #region Batch Export

    /// <summary>
    /// Export multiple users' financial data in bulk
    /// </summary>
    /// <param name="userIds">List of user IDs to export</param>
    /// <param name="format">Export format</param>
    /// <param name="startDate">Report start date</param>
    /// <param name="endDate">Report end date</param>
    /// <returns>Bulk export result with multiple files</returns>
    Task<BulkExportResult> ExportMultipleUsersAsync(
        List<Guid> userIds,
        ExportFormat format,
        DateTime startDate,
        DateTime endDate);

    /// <summary>
    /// Export all financial reports for a user (comprehensive export)
    /// </summary>
    /// <param name="userId">User ID to export all data for</param>
    /// <param name="format">Export format</param>
    /// <returns>Comprehensive export result</returns>
    Task<FinancialExportResult> ExportComprehensiveUserDataAsync(Guid userId, ExportFormat format);

    #endregion

    #region Validation and Formatting

    /// <summary>
    /// Validate export data before processing
    /// </summary>
    /// <param name="data">Data to validate</param>
    /// <returns>Validation result</returns>
    Task<ExportValidationResult> ValidateExportDataAsync<T>(T data);

    /// <summary>
    /// Apply privacy filters to export data
    /// </summary>
    /// <param name="data">Data to filter</param>
    /// <param name="privacyLevel">Privacy level to apply</param>
    /// <returns>Filtered data</returns>
    Task<T> ApplyPrivacyFiltersAsync<T>(T data, ExportPrivacyLevel privacyLevel);

    /// <summary>
    /// Format currency values for export
    /// </summary>
    /// <param name="amount">Amount to format</param>
    /// <param name="currencyCode">Currency code</param>
    /// <param name="locale">Locale for formatting</param>
    /// <returns>Formatted currency string</returns>
    string FormatCurrencyForExport(int amount, string currencyCode = "USD", string locale = "en-US");

    #endregion

    #region Chart and Visualization Export

    /// <summary>
    /// Generate chart images for PDF/Excel export
    /// </summary>
    /// <param name="data">Data for chart generation</param>
    /// <param name="chartType">Type of chart to generate</param>
    /// <param name="options">Chart generation options</param>
    /// <returns>Chart image as byte array</returns>
    Task<byte[]> GenerateChartImageAsync<T>(T data, ChartType chartType, ChartOptions options);

    /// <summary>
    /// Generate spending breakdown pie chart
    /// </summary>
    /// <param name="categoryBreakdown">Category breakdown data</param>
    /// <param name="options">Chart options</param>
    /// <returns>Chart image as byte array</returns>
    Task<byte[]> GenerateSpendingBreakdownChartAsync(
        IEnumerable<TransactionCategoryBreakdown> categoryBreakdown,
        ChartOptions options);

    /// <summary>
    /// Generate earning trends line chart
    /// </summary>
    /// <param name="trendData">Trend data for chart</param>
    /// <param name="options">Chart options</param>
    /// <returns>Chart image as byte array</returns>
    Task<byte[]> GenerateEarningTrendsChartAsync(IEnumerable<PeriodTrendData> trendData, ChartOptions options);

    #endregion
}

#region Supporting Classes

/// <summary>
/// Export template information
/// </summary>
public class ExportTemplate
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public ExportFormat Format { get; set; }
    public bool IsDefault { get; set; }
    public Dictionary<string, object> Parameters { get; set; } = new();
}

/// <summary>
/// Bulk export result
/// </summary>
public class BulkExportResult
{
    public bool Success { get; set; }
    public List<FinancialExportResult> ExportResults { get; set; } = new();
    public string? ZipFileContent { get; set; } // Base64 encoded ZIP file
    public string ZipFileName { get; set; } = string.Empty;
    public int TotalFiles { get; set; }
    public long TotalSizeBytes { get; set; }
    public List<string> Errors { get; set; } = new();
}

/// <summary>
/// Export validation result
/// </summary>
public class ExportValidationResult
{
    public bool IsValid { get; set; }
    public List<string> ValidationErrors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
    public int RecordCount { get; set; }
    public DateTime ValidatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Chart generation options
/// </summary>
public class ChartOptions
{
    public int Width { get; set; } = 800;
    public int Height { get; set; } = 600;
    public string Title { get; set; } = string.Empty;
    public string BackgroundColor { get; set; } = "#FFFFFF";
    public bool ShowLegend { get; set; } = true;
    public bool ShowDataLabels { get; set; } = true;
    public string ColorScheme { get; set; } = "Default";
    public Dictionary<string, object> CustomOptions { get; set; } = new();
}

/// <summary>
/// Chart type enumeration
/// </summary>
public enum ChartType
{
    PieChart,
    LineChart,
    BarChart,
    ColumnChart,
    AreaChart,
    DonutChart
}

/// <summary>
/// Export privacy levels
/// </summary>
public enum ExportPrivacyLevel
{
    Full,           // All data included
    Limited,        // Personal info excluded
    Anonymous,      // All identifying info removed
    Aggregate       // Only aggregate statistics
}

#endregion