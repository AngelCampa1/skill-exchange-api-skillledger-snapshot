using SkillLedger.Core.DTOs;
using SkillLedger.Core.Enums;
using SkillLedger.Core.Interfaces;

namespace SkillLedger.Tests.Mocks;

/// <summary>
/// Mock financial export service for testing purposes that doesn't require external file generation
/// </summary>
public class MockFinancialExportService : IFinancialExportService
{
    public List<MockExport> Exports { get; } = new();
    private bool _shouldSucceed = true;

    public void SetupSuccess() => _shouldSucceed = true;
    public void SetupFailure() => _shouldSucceed = false;

    #region CSV Export

    public Task<string> ExportToCsvAsync(IEnumerable<TransactionSummary> data, bool includeHeaders = true)
    {
        var export = new MockExport
        {
            Format = "CSV",
            Type = "TransactionList",
            RecordCount = data.Count(),
            IncludeHeaders = includeHeaders,
            ExportedAt = DateTime.UtcNow
        };
        Exports.Add(export);

        return _shouldSucceed
            ? Task.FromResult("transaction_id,amount,type,date\n1,100.00,Earning,2024-01-01")
            : Task.FromResult(string.Empty);
    }

    public Task<string> ExportReportToCsvAsync(CreditSummaryReport report)
    {
        var export = new MockExport
        {
            Format = "CSV",
            Type = "CreditSummaryReport",
            UserId = report.UserId,
            RecordCount = 1,
            ExportedAt = DateTime.UtcNow
        };
        Exports.Add(export);

        return _shouldSucceed
            ? Task.FromResult($"user_id,total_earned,total_spent\n{report.UserId},{report.Summary.TotalEarned},{report.Summary.TotalSpent}")
            : Task.FromResult(string.Empty);
    }

    public Task<string> ExportDashboardToCsvAsync(UserDashboardData dashboardData)
    {
        var export = new MockExport
        {
            Format = "CSV",
            Type = "Dashboard",
            UserId = dashboardData.UserId,
            ExportedAt = DateTime.UtcNow
        };
        Exports.Add(export);

        return _shouldSucceed
            ? Task.FromResult("metric,value\ncurrent_balance,1000\ntotal_earned,5000")
            : Task.FromResult(string.Empty);
    }

    #endregion

    #region PDF Export

    public Task<byte[]> ExportReportToPdfAsync(CreditSummaryReport report, bool includeCharts = true)
    {
        var export = new MockExport
        {
            Format = "PDF",
            Type = "CreditSummaryReport",
            UserId = report.UserId,
            IncludeCharts = includeCharts,
            RecordCount = 1,
            ExportedAt = DateTime.UtcNow
        };
        Exports.Add(export);

        return _shouldSucceed
            ? Task.FromResult(System.Text.Encoding.UTF8.GetBytes($"PDF Report for User {report.UserId}"))
            : Task.FromResult(Array.Empty<byte>());
    }

    public Task<byte[]> ExportTransactionHistoryToPdfAsync(
        Guid userId,
        IEnumerable<TransactionSummary> transactions,
        DateTime startDate,
        DateTime endDate)
    {
        var export = new MockExport
        {
            Format = "PDF",
            Type = "TransactionHistory",
            UserId = userId,
            RecordCount = transactions.Count(),
            StartDate = startDate,
            EndDate = endDate,
            ExportedAt = DateTime.UtcNow
        };
        Exports.Add(export);

        return _shouldSucceed
            ? Task.FromResult(System.Text.Encoding.UTF8.GetBytes($"PDF Transaction History for User {userId}"))
            : Task.FromResult(Array.Empty<byte>());
    }

    public Task<byte[]> ExportDashboardToPdfAsync(UserDashboardData dashboardData, bool includeCharts = true)
    {
        var export = new MockExport
        {
            Format = "PDF",
            Type = "Dashboard",
            UserId = dashboardData.UserId,
            IncludeCharts = includeCharts,
            ExportedAt = DateTime.UtcNow
        };
        Exports.Add(export);

        return _shouldSucceed
            ? Task.FromResult(System.Text.Encoding.UTF8.GetBytes($"PDF Dashboard for User {dashboardData.UserId}"))
            : Task.FromResult(Array.Empty<byte>());
    }

    #endregion

    #region JSON Export

    public Task<string> ExportToJsonAsync<T>(T data, bool formatOutput = true)
    {
        var export = new MockExport
        {
            Format = "JSON",
            Type = typeof(T).Name,
            FormatOutput = formatOutput,
            ExportedAt = DateTime.UtcNow
        };
        Exports.Add(export);

        return _shouldSucceed
            ? Task.FromResult(System.Text.Json.JsonSerializer.Serialize(data, new System.Text.Json.JsonSerializerOptions { WriteIndented = formatOutput }))
            : Task.FromResult(string.Empty);
    }

    public Task<string> ExportReportToJsonAsync(CreditSummaryReport report)
    {
        var export = new MockExport
        {
            Format = "JSON",
            Type = "CreditSummaryReport",
            UserId = report.UserId,
            RecordCount = 1,
            ExportedAt = DateTime.UtcNow
        };
        Exports.Add(export);

        return _shouldSucceed
            ? Task.FromResult(System.Text.Json.JsonSerializer.Serialize(report))
            : Task.FromResult(string.Empty);
    }

    public Task<string> ExportAnalyticsToJsonAsync(AnalyticsData analyticsData)
    {
        var export = new MockExport
        {
            Format = "JSON",
            Type = "Analytics",
            ExportedAt = DateTime.UtcNow
        };
        Exports.Add(export);

        return _shouldSucceed
            ? Task.FromResult(System.Text.Json.JsonSerializer.Serialize(analyticsData))
            : Task.FromResult(string.Empty);
    }

    #endregion

    #region XML Export

    public Task<string> ExportToXmlAsync<T>(T data, string rootElementName = "FinancialData")
    {
        var export = new MockExport
        {
            Format = "XML",
            Type = typeof(T).Name,
            RootElementName = rootElementName,
            ExportedAt = DateTime.UtcNow
        };
        Exports.Add(export);

        return _shouldSucceed
            ? Task.FromResult($"<?xml version=\"1.0\"?><{rootElementName}></{rootElementName}>")
            : Task.FromResult(string.Empty);
    }

    public Task<string> ExportReportToXmlAsync(CreditSummaryReport report)
    {
        var export = new MockExport
        {
            Format = "XML",
            Type = "CreditSummaryReport",
            UserId = report.UserId,
            RecordCount = 1,
            ExportedAt = DateTime.UtcNow
        };
        Exports.Add(export);

        return _shouldSucceed
            ? Task.FromResult($"<?xml version=\"1.0\"?><CreditSummaryReport><UserId>{report.UserId}</UserId></CreditSummaryReport>")
            : Task.FromResult(string.Empty);
    }

    public Task<string> ExportTransactionHistoryToXmlAsync(IEnumerable<TransactionSummary> transactions)
    {
        var export = new MockExport
        {
            Format = "XML",
            Type = "TransactionHistory",
            RecordCount = transactions.Count(),
            ExportedAt = DateTime.UtcNow
        };
        Exports.Add(export);

        return _shouldSucceed
            ? Task.FromResult($"<?xml version=\"1.0\"?><TransactionHistory><Count>{transactions.Count()}</Count></TransactionHistory>")
            : Task.FromResult(string.Empty);
    }

    #endregion

    #region Excel Export

    public Task<byte[]> ExportReportToExcelAsync(CreditSummaryReport report, bool includeCharts = true)
    {
        var export = new MockExport
        {
            Format = "Excel",
            Type = "CreditSummaryReport",
            UserId = report.UserId,
            IncludeCharts = includeCharts,
            RecordCount = 1,
            ExportedAt = DateTime.UtcNow
        };
        Exports.Add(export);

        return _shouldSucceed
            ? Task.FromResult(System.Text.Encoding.UTF8.GetBytes($"Excel Report for User {report.UserId}"))
            : Task.FromResult(Array.Empty<byte>());
    }

    public Task<byte[]> ExportTransactionHistoryToExcelAsync(
        Guid userId,
        IEnumerable<TransactionSummary> transactions,
        IEnumerable<TransactionCategoryBreakdown> categoryBreakdown,
        DateTime startDate,
        DateTime endDate)
    {
        var export = new MockExport
        {
            Format = "Excel",
            Type = "TransactionHistory",
            UserId = userId,
            RecordCount = transactions.Count(),
            StartDate = startDate,
            EndDate = endDate,
            ExportedAt = DateTime.UtcNow
        };
        Exports.Add(export);

        return _shouldSucceed
            ? Task.FromResult(System.Text.Encoding.UTF8.GetBytes($"Excel Transaction History for User {userId}"))
            : Task.FromResult(Array.Empty<byte>());
    }

    public Task<byte[]> ExportDashboardToExcelAsync(UserDashboardData dashboardData, AnalyticsData analyticsData)
    {
        var export = new MockExport
        {
            Format = "Excel",
            Type = "Dashboard",
            UserId = dashboardData.UserId,
            ExportedAt = DateTime.UtcNow
        };
        Exports.Add(export);

        return _shouldSucceed
            ? Task.FromResult(System.Text.Encoding.UTF8.GetBytes($"Excel Dashboard for User {dashboardData.UserId}"))
            : Task.FromResult(Array.Empty<byte>());
    }

    #endregion

    #region Template Management

    public Task<List<ExportTemplate>> GetAvailableTemplatesAsync(ExportFormat format)
    {
        var templates = new List<ExportTemplate>
        {
            new ExportTemplate
            {
                Id = "default",
                Name = "Default Template",
                Description = "Standard export template",
                Format = format,
                IsDefault = true
            }
        };
        return Task.FromResult(templates);
    }

    public Task<FinancialExportResult> ExportWithTemplateAsync<T>(
        T data,
        string templateId,
        Dictionary<string, object>? customParameters = null)
    {
        var export = new MockExport
        {
            Format = "Template",
            Type = typeof(T).Name,
            TemplateId = templateId,
            ExportedAt = DateTime.UtcNow
        };
        Exports.Add(export);

        var result = new FinancialExportResult
        {
            Success = _shouldSucceed,
            Content = _shouldSucceed ? "Template export content" : null,
            FileName = $"export_{DateTime.UtcNow:yyyyMMdd}.txt"
        };
        return Task.FromResult(result);
    }

    #endregion

    #region Batch Export

    public Task<BulkExportResult> ExportMultipleUsersAsync(
        List<Guid> userIds,
        ExportFormat format,
        DateTime startDate,
        DateTime endDate)
    {
        var export = new MockExport
        {
            Format = format.ToString(),
            Type = "BulkExport",
            RecordCount = userIds.Count,
            StartDate = startDate,
            EndDate = endDate,
            ExportedAt = DateTime.UtcNow
        };
        Exports.Add(export);

        var result = new BulkExportResult
        {
            Success = _shouldSucceed,
            TotalFiles = userIds.Count,
            ZipFileName = $"bulk_export_{DateTime.UtcNow:yyyyMMdd}.zip"
        };
        return Task.FromResult(result);
    }

    public Task<FinancialExportResult> ExportComprehensiveUserDataAsync(Guid userId, ExportFormat format)
    {
        var export = new MockExport
        {
            Format = format.ToString(),
            Type = "ComprehensiveExport",
            UserId = userId,
            ExportedAt = DateTime.UtcNow
        };
        Exports.Add(export);

        var result = new FinancialExportResult
        {
            Success = _shouldSucceed,
            Content = _shouldSucceed ? $"Comprehensive data for user {userId}" : null,
            FileName = $"comprehensive_{userId}_{DateTime.UtcNow:yyyyMMdd}.{format.ToString().ToLower()}"
        };
        return Task.FromResult(result);
    }

    #endregion

    #region Validation and Formatting

    public Task<ExportValidationResult> ValidateExportDataAsync<T>(T data)
    {
        var result = new ExportValidationResult
        {
            IsValid = _shouldSucceed,
            ValidatedAt = DateTime.UtcNow
        };

        if (!_shouldSucceed)
        {
            result.ValidationErrors.Add("Mock validation failure");
        }

        return Task.FromResult(result);
    }

    public Task<T> ApplyPrivacyFiltersAsync<T>(T data, ExportPrivacyLevel privacyLevel)
    {
        return Task.FromResult(data); // Return data as-is for mock
    }

    public string FormatCurrencyForExport(int amount, string currencyCode = "USD", string locale = "en-US")
    {
        return $"{currencyCode} {amount / 100.0:F2}";
    }

    #endregion

    #region Chart and Visualization Export

    public Task<byte[]> GenerateChartImageAsync<T>(T data, ChartType chartType, ChartOptions options)
    {
        var export = new MockExport
        {
            Format = "Chart",
            Type = chartType.ToString(),
            ExportedAt = DateTime.UtcNow
        };
        Exports.Add(export);

        return _shouldSucceed
            ? Task.FromResult(System.Text.Encoding.UTF8.GetBytes($"Chart Image: {chartType}"))
            : Task.FromResult(Array.Empty<byte>());
    }

    public Task<byte[]> GenerateSpendingBreakdownChartAsync(
        IEnumerable<TransactionCategoryBreakdown> categoryBreakdown,
        ChartOptions options)
    {
        var export = new MockExport
        {
            Format = "Chart",
            Type = "SpendingBreakdown",
            RecordCount = categoryBreakdown.Count(),
            ExportedAt = DateTime.UtcNow
        };
        Exports.Add(export);

        return _shouldSucceed
            ? Task.FromResult(System.Text.Encoding.UTF8.GetBytes("Spending Breakdown Chart"))
            : Task.FromResult(Array.Empty<byte>());
    }

    public Task<byte[]> GenerateEarningTrendsChartAsync(
        IEnumerable<PeriodTrendData> trendData,
        ChartOptions options)
    {
        var export = new MockExport
        {
            Format = "Chart",
            Type = "EarningTrends",
            RecordCount = trendData.Count(),
            ExportedAt = DateTime.UtcNow
        };
        Exports.Add(export);

        return _shouldSucceed
            ? Task.FromResult(System.Text.Encoding.UTF8.GetBytes("Earning Trends Chart"))
            : Task.FromResult(Array.Empty<byte>());
    }

    #endregion
}

/// <summary>
/// Mock export record for testing verification
/// </summary>
public class MockExport
{
    public string Format { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public Guid? UserId { get; set; }
    public int RecordCount { get; set; }
    public bool IncludeHeaders { get; set; }
    public bool IncludeCharts { get; set; }
    public bool FormatOutput { get; set; }
    public string? RootElementName { get; set; }
    public string? TemplateId { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public DateTime ExportedAt { get; set; }
}
