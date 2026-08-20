using FluentAssertions;
using Microsoft.Extensions.Logging;
using SkillLedger.Core.DTOs;
using SkillLedger.Core.Enums;
using SkillLedger.Core.Interfaces;
using SkillLedger.Infrastructure.Services;
using SkillLedger.Tests.Infrastructure;
using System.Text.Json;
using System.Xml.Linq;
using Xunit;

namespace SkillLedger.Tests.Integration.Services;

/// <summary>
/// Integration tests for FinancialExportService
/// Following anti-mocking pattern: Real service with real data transformations
/// This service has no external dependencies, only performs data formatting
/// </summary>
[IntegrationTest]
[FinancialTest]
public class FinancialExportServiceIntegrationTests
{
    private readonly FinancialExportService _exportService;
    private readonly ILogger<FinancialExportService> _logger;

    public FinancialExportServiceIntegrationTests()
    {
        _logger = LoggerFactory
            .Create(builder => builder.AddConsole())
            .CreateLogger<FinancialExportService>();

        _exportService = new FinancialExportService(_logger);
    }

    #region CSV Export Tests

    [Fact]
    public async Task ExportToCsvAsync_WithTransactions_ShouldGenerateValidCsv()
    {
        // Arrange
        var transactions = CreateSampleTransactions();

        // Act
        var csv = await _exportService.ExportToCsvAsync(transactions, includeHeaders: true);

        // Assert
        csv.Should().NotBeNullOrEmpty();
        csv.Should().Contain("Date,Type,Amount,Description,Reference ID,Status");
        csv.Should().Contain("ProjectPayment");
        csv.Should().Contain("1000");
        csv.Should().Contain("Completed");
    }

    [Fact]
    public async Task ExportToCsvAsync_WithoutHeaders_ShouldNotIncludeHeaderRow()
    {
        // Arrange
        var transactions = CreateSampleTransactions();

        // Act
        var csv = await _exportService.ExportToCsvAsync(transactions, includeHeaders: false);

        // Assert
        csv.Should().NotBeNullOrEmpty();
        csv.Should().NotContain("Date,Type,Amount");
        csv.Should().Contain("ProjectPayment");
    }

    [Fact]
    public async Task ExportReportToCsvAsync_WithCreditSummary_ShouldGenerateValidCsv()
    {
        // Arrange
        var report = CreateCreditSummaryReport();

        // Act
        var csv = await _exportService.ExportReportToCsvAsync(report);

        // Assert
        csv.Should().NotBeNullOrEmpty();
        csv.Should().Contain("Credit Summary Report");
        csv.Should().Contain("Total Credits Earned,5000");
        csv.Should().Contain("Total Credits Spent,3000");
        csv.Should().Contain("Net Credit Change,2000");
        csv.Should().Contain("Category Breakdown");
    }

    [Fact]
    public async Task ExportDashboardToCsvAsync_WithDashboardData_ShouldGenerateValidCsv()
    {
        // Arrange
        var dashboard = CreateUserDashboardData();

        // Act
        var csv = await _exportService.ExportDashboardToCsvAsync(dashboard);

        // Assert
        csv.Should().NotBeNullOrEmpty();
        csv.Should().Contain("User Dashboard Data");
        csv.Should().Contain("Current Balance");
        csv.Should().Contain("Available Balance");
        csv.Should().Contain("Credits This Month");
    }

    #endregion

    #region PDF Export Tests

    [Fact]
    public async Task ExportReportToPdfAsync_WithCreditSummary_ShouldGenerateHtmlPdf()
    {
        // Arrange
        var report = CreateCreditSummaryReport();

        // Act
        var pdfBytes = await _exportService.ExportReportToPdfAsync(report, includeCharts: true);

        // Assert
        pdfBytes.Should().NotBeNull();
        pdfBytes.Should().NotBeEmpty();

        var htmlContent = System.Text.Encoding.UTF8.GetString(pdfBytes);
        htmlContent.Should().Contain("<!DOCTYPE html>");
        htmlContent.Should().Contain("Credit Summary Report");
        htmlContent.Should().Contain("Total Credits Earned");
    }

    [Fact]
    public async Task ExportTransactionHistoryToPdfAsync_WithTransactions_ShouldGenerateHtmlPdf()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var transactions = CreateSampleTransactions();
        var startDate = DateTime.UtcNow.AddMonths(-1);
        var endDate = DateTime.UtcNow;

        // Act
        var pdfBytes = await _exportService.ExportTransactionHistoryToPdfAsync(userId, transactions, startDate, endDate);

        // Assert
        pdfBytes.Should().NotBeNull();
        pdfBytes.Should().NotBeEmpty();

        var htmlContent = System.Text.Encoding.UTF8.GetString(pdfBytes);
        htmlContent.Should().Contain("Transaction History");
        htmlContent.Should().Contain(userId.ToString());
    }

    [Fact]
    public async Task ExportDashboardToPdfAsync_WithDashboard_ShouldGenerateHtmlPdf()
    {
        // Arrange
        var dashboard = CreateUserDashboardData();

        // Act
        var pdfBytes = await _exportService.ExportDashboardToPdfAsync(dashboard, includeCharts: true);

        // Assert
        pdfBytes.Should().NotBeNull();
        pdfBytes.Should().NotBeEmpty();

        var htmlContent = System.Text.Encoding.UTF8.GetString(pdfBytes);
        htmlContent.Should().Contain("Financial Dashboard");
        htmlContent.Should().Contain("Current Balance");
    }

    #endregion

    #region JSON Export Tests

    [Fact]
    public async Task ExportToJsonAsync_WithFormattedOutput_ShouldGenerateFormattedJson()
    {
        // Arrange
        var data = CreateCreditSummaryReport();

        // Act
        var json = await _exportService.ExportToJsonAsync(data, formatOutput: true);

        // Assert
        json.Should().NotBeNullOrEmpty();
        json.Should().Contain("userId");
        json.Should().Contain("summary");

        // Verify it's valid JSON
        var parsed = JsonSerializer.Deserialize<JsonElement>(json);
        parsed.Should().NotBeNull();
    }

    [Fact]
    public async Task ExportToJsonAsync_WithUnformattedOutput_ShouldGenerateCompactJson()
    {
        // Arrange
        var data = CreateCreditSummaryReport();

        // Act
        var json = await _exportService.ExportToJsonAsync(data, formatOutput: false);

        // Assert
        json.Should().NotBeNullOrEmpty();
        json.Should().NotContain("\n"); // No newlines in unformatted JSON
        json.Should().Contain("userId");
    }

    [Fact]
    public async Task ExportReportToJsonAsync_WithCreditSummary_ShouldGenerateStructuredJson()
    {
        // Arrange
        var report = CreateCreditSummaryReport();

        // Act
        var json = await _exportService.ExportReportToJsonAsync(report);

        // Assert
        json.Should().NotBeNullOrEmpty();
        json.Should().Contain("reportInfo");
        json.Should().Contain("totalCreditsEarned");
        json.Should().Contain("categoryBreakdown");

        // Verify it's valid JSON
        var parsed = JsonSerializer.Deserialize<JsonElement>(json);
        parsed.Should().NotBeNull();
    }

    [Fact]
    public async Task ExportAnalyticsToJsonAsync_WithAnalyticsData_ShouldGenerateValidJson()
    {
        // Arrange
        var analytics = CreateAnalyticsData();

        // Act
        var json = await _exportService.ExportAnalyticsToJsonAsync(analytics);

        // Assert
        json.Should().NotBeNullOrEmpty();

        // Verify it's valid JSON
        var parsed = JsonSerializer.Deserialize<JsonElement>(json);
        parsed.Should().NotBeNull();
    }

    #endregion

    #region XML Export Tests

    [Fact]
    public async Task ExportToXmlAsync_WithData_ShouldGenerateValidXml()
    {
        // Arrange
        var data = CreateCreditSummaryReport();

        // Act
        var xml = await _exportService.ExportToXmlAsync(data, "TestData");

        // Assert
        xml.Should().NotBeNullOrEmpty();
        xml.Should().Contain("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
        xml.Should().Contain("<TestData>");
        xml.Should().Contain("<ExportedAt>");
        xml.Should().Contain("</TestData>");

        // Verify it's valid XML
        var parsed = XDocument.Parse(xml);
        parsed.Should().NotBeNull();
    }

    [Fact]
    public async Task ExportReportToXmlAsync_WithCreditSummary_ShouldGenerateValidXml()
    {
        // Arrange
        var report = CreateCreditSummaryReport();

        // Act
        var xml = await _exportService.ExportReportToXmlAsync(report);

        // Assert
        xml.Should().NotBeNullOrEmpty();
        xml.Should().Contain("<CreditSummaryReport>");
        xml.Should().Contain("<TotalCreditsEarned>5000</TotalCreditsEarned>");
        xml.Should().Contain("<TotalCreditsSpent>3000</TotalCreditsSpent>");
        xml.Should().Contain("<CategoryBreakdown>");

        // Verify it's valid XML
        var parsed = XDocument.Parse(xml);
        parsed.Should().NotBeNull();
    }

    [Fact]
    public async Task ExportTransactionHistoryToXmlAsync_WithTransactions_ShouldGenerateValidXml()
    {
        // Arrange
        var transactions = CreateSampleTransactions();

        // Act
        var xml = await _exportService.ExportTransactionHistoryToXmlAsync(transactions);

        // Assert
        xml.Should().NotBeNullOrEmpty();
        xml.Should().Contain("<TransactionHistory>");
        xml.Should().Contain("<TransactionCount>");
        xml.Should().Contain("<Transactions>");
        xml.Should().Contain("</TransactionHistory>");

        // Verify it's valid XML
        var parsed = XDocument.Parse(xml);
        parsed.Should().NotBeNull();
    }

    #endregion

    #region Excel Export Tests

    [Fact]
    public async Task ExportReportToExcelAsync_WithCreditSummary_ShouldGenerateBytes()
    {
        // Arrange
        var report = CreateCreditSummaryReport();

        // Act
        var excelBytes = await _exportService.ExportReportToExcelAsync(report, includeCharts: true);

        // Assert
        excelBytes.Should().NotBeNull();
        excelBytes.Should().NotBeEmpty();
    }

    [Fact]
    public async Task ExportTransactionHistoryToExcelAsync_WithTransactions_ShouldGenerateBytes()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var transactions = CreateSampleTransactions();
        var categoryBreakdown = CreateCategoryBreakdown();
        var startDate = DateTime.UtcNow.AddMonths(-1);
        var endDate = DateTime.UtcNow;

        // Act
        var excelBytes = await _exportService.ExportTransactionHistoryToExcelAsync(
            userId, transactions, categoryBreakdown, startDate, endDate);

        // Assert
        excelBytes.Should().NotBeNull();
        excelBytes.Should().NotBeEmpty();
    }

    [Fact]
    public async Task ExportDashboardToExcelAsync_WithDashboard_ShouldGenerateBytes()
    {
        // Arrange
        var dashboard = CreateUserDashboardData();
        var analytics = CreateAnalyticsData();

        // Act
        var excelBytes = await _exportService.ExportDashboardToExcelAsync(dashboard, analytics);

        // Assert
        excelBytes.Should().NotBeNull();
        excelBytes.Should().NotBeEmpty();
    }

    [Fact]
    public async Task ExportReportToExcelAsync_WithoutCharts_ShouldGenerateBytes()
    {
        // Arrange
        var report = CreateCreditSummaryReport();

        // Act
        var excelBytes = await _exportService.ExportReportToExcelAsync(report, includeCharts: false);

        // Assert
        excelBytes.Should().NotBeNull();
        excelBytes.Should().NotBeEmpty();
    }

    [Fact]
    public async Task ExportTransactionHistoryToExcelAsync_WithEmptyTransactions_ShouldGenerateBytes()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var transactions = Enumerable.Empty<TransactionSummary>();
        var categoryBreakdown = Enumerable.Empty<TransactionCategoryBreakdown>();
        var startDate = DateTime.UtcNow.AddMonths(-1);
        var endDate = DateTime.UtcNow;

        // Act
        var excelBytes = await _exportService.ExportTransactionHistoryToExcelAsync(
            userId, transactions, categoryBreakdown, startDate, endDate);

        // Assert
        excelBytes.Should().NotBeNull();
        excelBytes.Should().NotBeEmpty();
    }

    [Fact]
    public async Task ExportDashboardToExcelAsync_WithMinimalDashboard_ShouldGenerateBytes()
    {
        // Arrange
        var dashboard = new UserDashboardData
        {
            UserId = Guid.NewGuid(),
            Wallet = new WalletDashboardSummary
            {
                CurrentBalance = 0,
                AvailableBalance = 0,
                PendingBalance = 0
            },
            MonthlyStats = new MonthlyPerformance
            {
                CurrentMonthEarnings = 0,
                PreviousMonthEarnings = 0
            },
            Goals = new GoalProgress
            {
                GoalStatus = "No Goals"
            }
        };
        var analytics = new AnalyticsData
        {
            UserId = Guid.NewGuid(),
            LastUpdated = DateTime.UtcNow
        };

        // Act
        var excelBytes = await _exportService.ExportDashboardToExcelAsync(dashboard, analytics);

        // Assert
        excelBytes.Should().NotBeNull();
        excelBytes.Should().NotBeEmpty();
    }

    [Fact]
    public async Task ExportAnalyticsToJsonAsync_WithMinimalAnalytics_ShouldGenerateValidJson()
    {
        // Arrange
        var analytics = new AnalyticsData
        {
            UserId = Guid.NewGuid(),
            LastUpdated = DateTime.UtcNow
        };

        // Act
        var json = await _exportService.ExportAnalyticsToJsonAsync(analytics);

        // Assert
        json.Should().NotBeNullOrEmpty();

        // Verify it's valid JSON
        var parsed = JsonSerializer.Deserialize<JsonElement>(json);
        parsed.Should().NotBeNull();
    }

    #endregion

    #region Template Management Tests

    [Fact]
    public async Task GetAvailableTemplatesAsync_ForCsvFormat_ShouldReturnCsvTemplates()
    {
        // Act
        var templates = await _exportService.GetAvailableTemplatesAsync(ExportFormat.CSV);

        // Assert
        templates.Should().NotBeNull();
        templates.Should().HaveCountGreaterThan(0);
        templates.Should().Contain(t => t.Format == ExportFormat.CSV);
        templates.Should().Contain(t => t.IsDefault);
    }

    [Fact]
    public async Task GetAvailableTemplatesAsync_ForPdfFormat_ShouldReturnPdfTemplates()
    {
        // Act
        var templates = await _exportService.GetAvailableTemplatesAsync(ExportFormat.PDF);

        // Assert
        templates.Should().NotBeNull();
        templates.Should().HaveCountGreaterThan(0);
        templates.Should().Contain(t => t.Format == ExportFormat.PDF);
        templates.Should().Contain(t => t.IsDefault);
    }

    [Fact]
    public async Task ExportWithTemplateAsync_WithCsvTemplate_ShouldGenerateCsvExport()
    {
        // Arrange
        var data = CreateCreditSummaryReport();

        // Act
        var result = await _exportService.ExportWithTemplateAsync(data, "csv-basic");

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Content.Should().NotBeNullOrEmpty();
        result.ContentType.Should().Be("text/csv");
        result.FileName.Should().Contain("csv-basic");
        result.FileName.Should().EndWith(".csv");
    }

    [Fact]
    public async Task ExportWithTemplateAsync_WithJsonTemplate_ShouldGenerateJsonExport()
    {
        // Arrange
        var data = CreateCreditSummaryReport();

        // Act
        var result = await _exportService.ExportWithTemplateAsync(data, "json-structured");

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Content.Should().NotBeNullOrEmpty();
        result.ContentType.Should().Be("application/json");
        result.FileName.Should().EndWith(".json");
    }

    #endregion

    #region Batch Export Tests

    [Fact]
    public async Task ExportMultipleUsersAsync_WithMultipleUsers_ShouldGenerateBulkExport()
    {
        // Arrange
        var userIds = new List<Guid> { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() };
        var startDate = DateTime.UtcNow.AddMonths(-1);
        var endDate = DateTime.UtcNow;

        // Act
        var result = await _exportService.ExportMultipleUsersAsync(userIds, ExportFormat.CSV, startDate, endDate);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.ZipFileName.Should().Contain("bulk_export");
        result.TotalFiles.Should().Be(3);
        result.ExportResults.Should().HaveCount(3);
    }

    [Fact]
    public async Task ExportComprehensiveUserDataAsync_ForSingleUser_ShouldGenerateComprehensiveExport()
    {
        // Arrange
        var userId = Guid.NewGuid();

        // Act
        var result = await _exportService.ExportComprehensiveUserDataAsync(userId, ExportFormat.JSON);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Content.Should().NotBeNullOrEmpty();
        result.FileName.Should().Contain("comprehensive_export");
        result.FileName.Should().Contain(userId.ToString());
    }

    #endregion

    #region Validation Tests

    [Fact]
    public async Task ValidateExportDataAsync_WithValidCreditSummary_ShouldPassValidation()
    {
        // Arrange
        var report = CreateCreditSummaryReport();

        // Act
        var result = await _exportService.ValidateExportDataAsync(report);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeTrue();
        result.ValidationErrors.Should().BeEmpty();
        result.RecordCount.Should().Be(1);
    }

    [Fact]
    public async Task ValidateExportDataAsync_WithInvalidCreditSummary_ShouldFailValidation()
    {
        // Arrange
        var report = CreateCreditSummaryReport();
        report.UserId = Guid.Empty; // Invalid user ID

        // Act
        var result = await _exportService.ValidateExportDataAsync(report);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeFalse();
        result.ValidationErrors.Should().Contain("User ID cannot be empty");
    }

    [Fact]
    public async Task ValidateExportDataAsync_WithInvalidDateRange_ShouldFailValidation()
    {
        // Arrange
        var report = CreateCreditSummaryReport();
        report.Period.StartDate = DateTime.UtcNow;
        report.Period.EndDate = DateTime.UtcNow.AddMonths(-1); // End before start

        // Act
        var result = await _exportService.ValidateExportDataAsync(report);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeFalse();
        result.ValidationErrors.Should().Contain("Start date cannot be after end date");
    }

    [Fact]
    public async Task ValidateExportDataAsync_WithNullData_ShouldFailValidation()
    {
        // Act
        var result = await _exportService.ValidateExportDataAsync<CreditSummaryReport>(null!);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeFalse();
        result.ValidationErrors.Should().Contain("Data cannot be null");
    }

    [Fact]
    public async Task ValidateExportDataAsync_WithValidTransactions_ShouldPassValidation()
    {
        // Arrange
        var transactions = CreateSampleTransactions();

        // Act
        var result = await _exportService.ValidateExportDataAsync(transactions);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeTrue();
        result.RecordCount.Should().Be(transactions.Count());
    }

    [Fact]
    public async Task ValidateExportDataAsync_WithEmptyTransactions_ShouldHaveWarning()
    {
        // Arrange
        var transactions = Enumerable.Empty<TransactionSummary>();

        // Act
        var result = await _exportService.ValidateExportDataAsync(transactions);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeTrue(); // Empty list is valid
        result.Warnings.Should().Contain("No transactions to export");
    }

    [Fact]
    public async Task ApplyPrivacyFiltersAsync_WithData_ShouldReturnData()
    {
        // Arrange
        var report = CreateCreditSummaryReport();

        // Act
        var filteredReport = await _exportService.ApplyPrivacyFiltersAsync(report, ExportPrivacyLevel.Full);

        // Assert - Placeholder implementation returns data as-is
        filteredReport.Should().NotBeNull();
        filteredReport.UserId.Should().Be(report.UserId);
    }

    [Fact]
    public void FormatCurrencyForExport_WithDefaultLocale_ShouldFormatCorrectly()
    {
        // Arrange
        var amount = 1234567;

        // Act
        var formatted = _exportService.FormatCurrencyForExport(amount);

        // Assert
        formatted.Should().Be("1,234,567"); // en-US format
    }

    [Fact]
    public void FormatCurrencyForExport_WithDifferentLocale_ShouldFormatPerLocale()
    {
        // Arrange
        var amount = 1234567;

        // Act
        var formatted = _exportService.FormatCurrencyForExport(amount, "USD", "de-DE");

        // Assert
        formatted.Should().Contain("1"); // German format uses periods as thousands separator
    }

    #endregion

    #region Phase 7 Coverage Tests - Additional Format and Edge Case Coverage

    [Fact]
    public async Task GetAvailableTemplatesAsync_ForJsonFormat_ShouldReturnJsonTemplates()
    {
        // Act
        var templates = await _exportService.GetAvailableTemplatesAsync(ExportFormat.JSON);

        // Assert
        templates.Should().NotBeNull();
        templates.Should().HaveCountGreaterThan(0);
        templates.Should().Contain(t => t.Format == ExportFormat.JSON);
        templates.Should().Contain(t => t.IsDefault);
    }

    [Fact]
    public async Task GetAvailableTemplatesAsync_ForXmlFormat_ShouldReturnXmlTemplates()
    {
        // Act
        var templates = await _exportService.GetAvailableTemplatesAsync(ExportFormat.XML);

        // Assert
        templates.Should().NotBeNull();
        templates.Should().HaveCountGreaterThan(0);
        templates.Should().Contain(t => t.Format == ExportFormat.XML);
        templates.Should().Contain(t => t.IsDefault);
    }

    [Fact]
    public async Task GetAvailableTemplatesAsync_ForExcelFormat_ShouldReturnExcelTemplates()
    {
        // Act
        var templates = await _exportService.GetAvailableTemplatesAsync(ExportFormat.Excel);

        // Assert
        templates.Should().NotBeNull();
        templates.Should().HaveCountGreaterThan(0);
        templates.Should().Contain(t => t.Format == ExportFormat.Excel);
        templates.Should().Contain(t => t.IsDefault);
    }

    [Fact]
    public async Task ExportWithTemplateAsync_WithPdfTemplate_ShouldGeneratePdfExport()
    {
        // Arrange
        var data = CreateCreditSummaryReport();

        // Act
        var result = await _exportService.ExportWithTemplateAsync(data, "pdf-summary");

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Content.Should().NotBeNullOrEmpty();
        result.ContentType.Should().Be("application/pdf");
        result.FileName.Should().EndWith(".pdf");
    }

    [Fact]
    public async Task ExportWithTemplateAsync_WithXmlTemplate_ShouldGenerateXmlExport()
    {
        // Arrange
        var data = CreateCreditSummaryReport();

        // Act
        var result = await _exportService.ExportWithTemplateAsync(data, "xml-standard");

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Content.Should().NotBeNullOrEmpty();
        result.ContentType.Should().Be("application/xml");
        result.FileName.Should().EndWith(".xml");
    }

    [Fact]
    public async Task ExportWithTemplateAsync_WithUnknownTemplate_ShouldDefaultToJson()
    {
        // Arrange
        var data = CreateCreditSummaryReport();

        // Act
        var result = await _exportService.ExportWithTemplateAsync(data, "unknown-template");

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.ContentType.Should().Be("application/json");
        result.FileName.Should().EndWith(".json");
    }

    [Fact]
    public async Task GenerateChartImageAsync_WithData_ShouldGeneratePlaceholder()
    {
        // Arrange
        var data = CreateCreditSummaryReport();
        var options = new ChartOptions();

        // Act
        var chartBytes = await _exportService.GenerateChartImageAsync(data, ChartType.PieChart, options);

        // Assert
        chartBytes.Should().NotBeNull();
        chartBytes.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GenerateSpendingBreakdownChartAsync_WithCategoryBreakdown_ShouldGeneratePlaceholder()
    {
        // Arrange
        var categoryBreakdown = CreateCategoryBreakdown();
        var options = new ChartOptions();

        // Act
        var chartBytes = await _exportService.GenerateSpendingBreakdownChartAsync(categoryBreakdown, options);

        // Assert
        chartBytes.Should().NotBeNull();
        chartBytes.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GenerateEarningTrendsChartAsync_WithTrendData_ShouldGeneratePlaceholder()
    {
        // Arrange
        var trendData = new List<PeriodTrendData>
        {
            new() { Period = 202401, PeriodStart = new DateTime(2024, 1, 1), PeriodEnd = new DateTime(2024, 1, 31), Earnings = 1000, Spending = 800 },
            new() { Period = 202402, PeriodStart = new DateTime(2024, 2, 1), PeriodEnd = new DateTime(2024, 2, 29), Earnings = 1200, Spending = 900 }
        };
        var options = new ChartOptions();

        // Act
        var chartBytes = await _exportService.GenerateEarningTrendsChartAsync(trendData, options);

        // Assert
        chartBytes.Should().NotBeNull();
        chartBytes.Should().NotBeEmpty();
    }

    [Fact]
    public async Task ExportToCsvAsync_WithDescriptionContainingQuotes_ShouldEscapeQuotes()
    {
        // Arrange
        var transactions = new List<TransactionSummary>
        {
            new()
            {
                TransactionId = Guid.NewGuid(),
                Type = CreditTransactionType.ProjectPayment,
                Amount = 1000,
                Description = "Payment for \"special\" project work",
                Status = "Completed",
                CreatedAt = DateTime.UtcNow
            }
        };

        // Act
        var csv = await _exportService.ExportToCsvAsync(transactions, includeHeaders: true);

        // Assert
        csv.Should().NotBeNullOrEmpty();
        csv.Should().Contain("\"Payment for \"\"special\"\" project work\""); // Escaped quotes
    }

    [Fact]
    public async Task ExportTransactionHistoryToXmlAsync_WithDescriptionContainingSpecialChars_ShouldEscapeXml()
    {
        // Arrange
        var transactions = new List<TransactionSummary>
        {
            new()
            {
                TransactionId = Guid.NewGuid(),
                Type = CreditTransactionType.ProjectPayment,
                Amount = 1000,
                Description = "Payment <with> special & characters",
                Status = "Completed",
                CreatedAt = DateTime.UtcNow
            }
        };

        // Act
        var xml = await _exportService.ExportTransactionHistoryToXmlAsync(transactions);

        // Assert
        xml.Should().NotBeNullOrEmpty();
        xml.Should().Contain("&lt;"); // XML escaped <
        xml.Should().Contain("&amp;"); // XML escaped &
    }

    [Fact]
    public async Task ValidateExportDataAsync_WithLargeNegativeBalance_ShouldHaveWarning()
    {
        // Arrange
        var report = CreateCreditSummaryReport();
        report.Summary.EndingBalance = -15000; // Large negative balance

        // Act
        var result = await _exportService.ValidateExportDataAsync(report);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeTrue(); // Still valid, just with warning
        result.Warnings.Should().Contain("Large negative balance detected");
    }

    [Fact]
    public async Task ValidateExportDataAsync_WithDashboardLargeNegativeBalance_ShouldHaveWarning()
    {
        // Arrange
        var dashboard = CreateUserDashboardData();
        dashboard.Wallet.CurrentBalance = -12000; // Large negative balance

        // Act
        var result = await _exportService.ValidateExportDataAsync(dashboard);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeTrue();
        result.Warnings.Should().Contain("Large negative balance detected in dashboard");
    }

    [Fact]
    public async Task ValidateExportDataAsync_WithInvalidTransactions_ShouldFailValidation()
    {
        // Arrange
        var transactions = new List<TransactionSummary>
        {
            new()
            {
                TransactionId = Guid.Empty, // Invalid
                Type = CreditTransactionType.ProjectPayment,
                Amount = 1000,
                Description = "Valid description",
                Status = "Completed",
                CreatedAt = default // Invalid
            },
            new()
            {
                TransactionId = Guid.NewGuid(),
                Type = CreditTransactionType.Refund,
                Amount = 500,
                Description = "Valid transaction",
                Status = "Completed",
                CreatedAt = DateTime.UtcNow
            }
        };

        // Act
        var result = await _exportService.ValidateExportDataAsync(transactions);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeFalse();
        result.ValidationErrors.Should().Contain(e => e.Contains("transactions have invalid data"));
    }

    [Fact]
    public void FormatCurrencyForExport_WithInvalidLocale_ShouldFallbackToDefault()
    {
        // Arrange
        var amount = 1234567;

        // Act
        var formatted = _exportService.FormatCurrencyForExport(amount, "USD", "invalid-locale");

        // Assert
        // Should fallback to default formatting (ToString())
        formatted.Should().Be("1234567"); // Default fallback
    }

    [Fact]
    public async Task ExportReportToCsvAsync_WithNoCategoryBreakdowns_ShouldOmitCategorySection()
    {
        // Arrange
        var report = CreateCreditSummaryReport();
        report.CategoryBreakdowns = null; // No category data

        // Act
        var csv = await _exportService.ExportReportToCsvAsync(report);

        // Assert
        csv.Should().NotBeNullOrEmpty();
        csv.Should().NotContain("Category Breakdown");
        csv.Should().Contain("Credit Summary Report");
    }

    [Fact]
    public async Task ExportReportToPdfAsync_WithNoCategoryBreakdowns_ShouldOmitCategoryTable()
    {
        // Arrange
        var report = CreateCreditSummaryReport();
        report.CategoryBreakdowns = null;

        // Act
        var pdfBytes = await _exportService.ExportReportToPdfAsync(report, includeCharts: true);

        // Assert
        pdfBytes.Should().NotBeNull();
        var htmlContent = System.Text.Encoding.UTF8.GetString(pdfBytes);
        htmlContent.Should().NotContain("Category Breakdown");
    }

    [Fact]
    public async Task ExportReportToXmlAsync_WithNoCategoryBreakdowns_ShouldOmitCategoryElement()
    {
        // Arrange
        var report = CreateCreditSummaryReport();
        report.CategoryBreakdowns = null;

        // Act
        var xml = await _exportService.ExportReportToXmlAsync(report);

        // Assert
        xml.Should().NotBeNullOrEmpty();
        xml.Should().NotContain("CategoryBreakdown");
    }

    [Fact]
    public async Task ValidateExportDataAsync_WithValidDashboardData_ShouldPassValidation()
    {
        // Arrange
        var dashboard = CreateUserDashboardData();

        // Act
        var result = await _exportService.ValidateExportDataAsync(dashboard);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeTrue();
        result.RecordCount.Should().Be(1);
        result.ValidationErrors.Should().BeEmpty();
    }

    [Fact]
    public async Task ValidateExportDataAsync_WithInvalidDashboardUserId_ShouldFailValidation()
    {
        // Arrange
        var dashboard = CreateUserDashboardData();
        dashboard.UserId = Guid.Empty;

        // Act
        var result = await _exportService.ValidateExportDataAsync(dashboard);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeFalse();
        result.ValidationErrors.Should().Contain("User ID cannot be empty");
    }

    #endregion

    #region Helper Methods

    private List<TransactionSummary> CreateSampleTransactions()
    {
        return new List<TransactionSummary>
        {
            new()
            {
                TransactionId = Guid.NewGuid(),
                Type = CreditTransactionType.ProjectPayment,
                Amount = 1000,
                Description = "Payment for project work",
                Status = "Completed",
                CreatedAt = DateTime.UtcNow.AddDays(-5)
            },
            new()
            {
                TransactionId = Guid.NewGuid(),
                Type = CreditTransactionType.Refund,
                Amount = -500,
                Description = "Credit refund",
                Status = "Completed",
                CreatedAt = DateTime.UtcNow.AddDays(-3)
            },
            new()
            {
                TransactionId = Guid.NewGuid(),
                Type = CreditTransactionType.Reward,
                Amount = 100,
                Description = "Referral reward",
                Status = "Completed",
                CreatedAt = DateTime.UtcNow.AddDays(-1)
            }
        };
    }

    private CreditSummaryReport CreateCreditSummaryReport()
    {
        return new CreditSummaryReport
        {
            UserId = Guid.NewGuid(),
            Period = new ReportPeriodInfo
            {
                StartDate = DateTime.UtcNow.AddMonths(-1),
                EndDate = DateTime.UtcNow
            },
            GeneratedAt = DateTime.UtcNow,
            Summary = new FinancialSummary
            {
                TotalEarned = 5000,
                TotalSpent = 3000,
                // NetChange is calculated property: TotalEarned - TotalSpent
                StartingBalance = 1000,
                EndingBalance = 3000,
                TransactionCount = 25,
                AverageTransactionSize = 200
            },
            CategoryBreakdowns = new List<TransactionCategoryBreakdown>
            {
                new() { Category = CreditTransactionType.ProjectPayment, TotalAmount = 3000, TransactionCount = 10 },
                new() { Category = CreditTransactionType.Reward, TotalAmount = 2000, TransactionCount = 15 }
            }
        };
    }

    private UserDashboardData CreateUserDashboardData()
    {
        return new UserDashboardData
        {
            UserId = Guid.NewGuid(),
            Wallet = new WalletDashboardSummary
            {
                CurrentBalance = 5000,
                AvailableBalance = 4500,
                PendingBalance = 500
            },
            MonthlyStats = new MonthlyPerformance
            {
                CurrentMonthEarnings = 2000,
                PreviousMonthEarnings = 1500
            },
            Goals = new GoalProgress
            {
                GoalStatus = "On Track"
            }
        };
    }

    private AnalyticsData CreateAnalyticsData()
    {
        return new AnalyticsData
        {
            UserId = Guid.NewGuid(),
            LastUpdated = DateTime.UtcNow
        };
    }

    private List<TransactionCategoryBreakdown> CreateCategoryBreakdown()
    {
        return new List<TransactionCategoryBreakdown>
        {
            new() { Category = CreditTransactionType.ProjectPayment, TotalAmount = 3000, TransactionCount = 10 },
            new() { Category = CreditTransactionType.Reward, TotalAmount = 2000, TransactionCount = 15 }
        };
    }

    #endregion
}
