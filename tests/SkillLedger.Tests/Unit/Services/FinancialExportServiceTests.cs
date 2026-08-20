using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using SkillLedger.Core.DTOs;
using SkillLedger.Core.Enums;
using SkillLedger.Core.Interfaces;
using SkillLedger.Infrastructure.Services;
using SkillLedger.Tests.Infrastructure;
using Xunit;
using ExportFormat = SkillLedger.Core.Enums.ExportFormat;

namespace SkillLedger.Tests.Unit.Services;

/// <summary>
/// Unit tests for FinancialExportService.
/// This is a stateless service with no database dependencies,
/// so unit tests are appropriate.
/// </summary>
[FinancialTest]
public class FinancialExportServiceTests
{
    private readonly FinancialExportService _exportService;
    private readonly Mock<ILogger<FinancialExportService>> _mockLogger;

    public FinancialExportServiceTests()
    {
        _mockLogger = new Mock<ILogger<FinancialExportService>>();
        _exportService = new FinancialExportService(_mockLogger.Object);
    }

    #region CSV Export Tests

    [Fact]
    public async Task ExportToCsv_WithTransactions_ShouldGenerateValidCsv()
    {
        // Arrange
        var transactions = new List<TransactionSummary>
        {
            new() { TransactionId = Guid.NewGuid(), Type = CreditTransactionType.ProjectPayment, Amount = 100, Description = "Project completion", Status = "Completed", CreatedAt = DateTime.UtcNow },
            new() { TransactionId = Guid.NewGuid(), Type = CreditTransactionType.Purchase, Amount = 50, Description = "Service purchase", Status = "Completed", CreatedAt = DateTime.UtcNow.AddDays(-1) }
        };

        // Act
        var result = await _exportService.ExportToCsvAsync(transactions, includeHeaders: true);

        // Assert
        result.Should().NotBeNullOrEmpty();
        result.Should().Contain("Date,Type,Amount,Description,Reference ID,Status");
        result.Should().Contain("ProjectPayment");
        result.Should().Contain("Purchase");
        result.Should().Contain("100");
        result.Should().Contain("50");
    }

    [Fact]
    public async Task ExportToCsv_WithoutHeaders_ShouldNotIncludeHeaderRow()
    {
        // Arrange
        var transactions = new List<TransactionSummary>
        {
            new() { TransactionId = Guid.NewGuid(), Type = CreditTransactionType.ProjectPayment, Amount = 100, Description = "Test", Status = "Completed", CreatedAt = DateTime.UtcNow }
        };

        // Act
        var result = await _exportService.ExportToCsvAsync(transactions, includeHeaders: false);

        // Assert
        result.Should().NotContain("Date,Type,Amount,Description,Reference ID,Status");
    }

    [Fact]
    public async Task ExportToCsv_WithDescriptionContainingCommas_ShouldEscapeProperly()
    {
        // Arrange
        var transactions = new List<TransactionSummary>
        {
            new() { TransactionId = Guid.NewGuid(), Type = CreditTransactionType.ProjectPayment, Amount = 100, Description = "Test, with comma", Status = "Completed", CreatedAt = DateTime.UtcNow }
        };

        // Act
        var result = await _exportService.ExportToCsvAsync(transactions, includeHeaders: false);

        // Assert - Description should be quoted
        result.Should().Contain("\"Test, with comma\"");
    }

    [Fact]
    public async Task ExportReportToCsv_WithCreditSummaryReport_ShouldGenerateValidCsv()
    {
        // Arrange
        var report = CreateTestCreditSummaryReport();

        // Act
        var result = await _exportService.ExportReportToCsvAsync(report);

        // Assert
        result.Should().NotBeNullOrEmpty();
        result.Should().Contain("Credit Summary Report");
        result.Should().Contain("Total Credits Earned");
        result.Should().Contain("Total Credits Spent");
        result.Should().Contain("1000"); // TotalEarned
        result.Should().Contain("500");  // TotalSpent
    }

    [Fact]
    public async Task ExportReportToCsv_WithCategoryBreakdowns_ShouldIncludeCategories()
    {
        // Arrange
        var report = CreateTestCreditSummaryReport();
        report.CategoryBreakdowns = new List<TransactionCategoryBreakdown>
        {
            new() { Category = CreditTransactionType.ProjectPayment, TotalAmount = 800, TransactionCount = 5 },
            new() { Category = CreditTransactionType.Purchase, TotalAmount = 200, TransactionCount = 3 }
        };

        // Act
        var result = await _exportService.ExportReportToCsvAsync(report);

        // Assert
        result.Should().Contain("Category Breakdown");
        result.Should().Contain("ProjectPayment");
        result.Should().Contain("Purchase");
    }

    [Fact]
    public async Task ExportDashboardToCsv_WithDashboardData_ShouldGenerateValidCsv()
    {
        // Arrange
        var dashboard = CreateTestDashboardData();

        // Act
        var result = await _exportService.ExportDashboardToCsvAsync(dashboard);

        // Assert
        result.Should().NotBeNullOrEmpty();
        result.Should().Contain("User Dashboard Data");
        result.Should().Contain("Current Balance");
        result.Should().Contain("Available Balance");
    }

    #endregion

    #region PDF Export Tests

    [Fact]
    public async Task ExportReportToPdf_WithCreditSummaryReport_ShouldGenerateHtmlStructure()
    {
        // Arrange
        var report = CreateTestCreditSummaryReport();

        // Act
        var result = await _exportService.ExportReportToPdfAsync(report, includeCharts: true);

        // Assert
        result.Should().NotBeNull();
        result.Length.Should().BeGreaterThan(0);

        var html = System.Text.Encoding.UTF8.GetString(result);
        html.Should().Contain("<!DOCTYPE html>");
        html.Should().Contain("<h1>Credit Summary Report</h1>");
        html.Should().Contain("Total Credits Earned");
    }

    [Fact]
    public async Task ExportTransactionHistoryToPdf_ShouldGenerateFormattedReport()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var transactions = new List<TransactionSummary>
        {
            new() { TransactionId = Guid.NewGuid(), Type = CreditTransactionType.ProjectPayment, Amount = 100, Description = "Test", Status = "Completed", CreatedAt = DateTime.UtcNow },
            new() { TransactionId = Guid.NewGuid(), Type = CreditTransactionType.Purchase, Amount = 50, Description = "Test2", Status = "Completed", CreatedAt = DateTime.UtcNow.AddDays(-1) }
        };
        var startDate = DateTime.UtcNow.AddMonths(-1);
        var endDate = DateTime.UtcNow;

        // Act
        var result = await _exportService.ExportTransactionHistoryToPdfAsync(userId, transactions, startDate, endDate);

        // Assert
        result.Should().NotBeNull();
        var html = System.Text.Encoding.UTF8.GetString(result);
        html.Should().Contain("<h1>Transaction History</h1>");
        html.Should().Contain(userId.ToString());
    }

    [Fact]
    public async Task ExportDashboardToPdf_ShouldGenerateFormattedReport()
    {
        // Arrange
        var dashboard = CreateTestDashboardData();

        // Act
        var result = await _exportService.ExportDashboardToPdfAsync(dashboard, includeCharts: true);

        // Assert
        result.Should().NotBeNull();
        var html = System.Text.Encoding.UTF8.GetString(result);
        html.Should().Contain("<h1>Financial Dashboard</h1>");
        html.Should().Contain("Current Balance");
    }

    #endregion

    #region JSON Export Tests

    [Fact]
    public async Task ExportToJson_WithFormattedOutput_ShouldBeIndented()
    {
        // Arrange
        var data = new { Name = "Test", Value = 123 };

        // Act
        var result = await _exportService.ExportToJsonAsync(data, formatOutput: true);

        // Assert
        result.Should().NotBeNullOrEmpty();
        result.Should().Contain("\n"); // Indented JSON has newlines
        result.Should().Contain("name"); // camelCase
        result.Should().Contain("value");
    }

    [Fact]
    public async Task ExportToJson_WithoutFormattedOutput_ShouldBeCompact()
    {
        // Arrange
        var data = new { Name = "Test", Value = 123 };

        // Act
        var result = await _exportService.ExportToJsonAsync(data, formatOutput: false);

        // Assert
        result.Should().NotBeNullOrEmpty();
        // Compact JSON is on a single line (minimal whitespace)
        result.Split('\n').Length.Should().BeLessOrEqualTo(2); // May have trailing newline
    }

    [Fact]
    public async Task ExportReportToJson_WithCreditSummaryReport_ShouldIncludeStructuredData()
    {
        // Arrange
        var report = CreateTestCreditSummaryReport();

        // Act
        var result = await _exportService.ExportReportToJsonAsync(report);

        // Assert
        result.Should().NotBeNullOrEmpty();
        result.Should().Contain("reportInfo");
        result.Should().Contain("summary");
        result.Should().Contain("totalCreditsEarned");
        result.Should().Contain("totalCreditsSpent");
    }

    [Fact]
    public async Task ExportAnalyticsToJson_ShouldSerializeAnalyticsData()
    {
        // Arrange
        var analytics = new AnalyticsData
        {
            UserId = Guid.NewGuid(),
            CurrentBalance = new BalanceAnalytics { CurrentBalance = 1000 },
            Spending = new SpendingAnalytics { TotalSpent = 500 },
            Earnings = new EarningAnalytics { TotalEarned = 1500 }
        };

        // Act
        var result = await _exportService.ExportAnalyticsToJsonAsync(analytics);

        // Assert
        result.Should().NotBeNullOrEmpty();
        result.Should().Contain("userId");
    }

    #endregion

    #region XML Export Tests

    [Fact]
    public async Task ExportToXml_ShouldGenerateValidXmlStructure()
    {
        // Arrange
        var data = new { Name = "Test", Value = 123 };

        // Act
        var result = await _exportService.ExportToXmlAsync(data, "TestData");

        // Assert
        result.Should().NotBeNullOrEmpty();
        result.Should().Contain("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
        result.Should().Contain("<TestData>");
        result.Should().Contain("</TestData>");
        result.Should().Contain("<ExportedAt>");
    }

    [Fact]
    public async Task ExportReportToXml_WithCreditSummaryReport_ShouldGenerateValidXml()
    {
        // Arrange
        var report = CreateTestCreditSummaryReport();

        // Act
        var result = await _exportService.ExportReportToXmlAsync(report);

        // Assert
        result.Should().NotBeNullOrEmpty();
        result.Should().Contain("<CreditSummaryReport>");
        result.Should().Contain("<UserId>");
        result.Should().Contain("<TotalCreditsEarned>");
        result.Should().Contain("<TotalCreditsSpent>");
    }

    [Fact]
    public async Task ExportReportToXml_WithCategoryBreakdowns_ShouldIncludeCategories()
    {
        // Arrange
        var report = CreateTestCreditSummaryReport();
        report.CategoryBreakdowns = new List<TransactionCategoryBreakdown>
        {
            new() { Category = CreditTransactionType.ProjectPayment, TotalAmount = 800, TransactionCount = 5 }
        };

        // Act
        var result = await _exportService.ExportReportToXmlAsync(report);

        // Assert
        result.Should().Contain("<CategoryBreakdown>");
        result.Should().Contain("<Category>");
        result.Should().Contain("<TransactionType>ProjectPayment</TransactionType>");
    }

    [Fact]
    public async Task ExportTransactionHistoryToXml_ShouldGenerateValidXml()
    {
        // Arrange
        var transactions = new List<TransactionSummary>
        {
            new() { TransactionId = Guid.NewGuid(), Type = CreditTransactionType.ProjectPayment, Amount = 100, Description = "Test", Status = "Completed", CreatedAt = DateTime.UtcNow }
        };

        // Act
        var result = await _exportService.ExportTransactionHistoryToXmlAsync(transactions);

        // Assert
        result.Should().Contain("<TransactionHistory>");
        result.Should().Contain("<Transactions>");
        result.Should().Contain("<Transaction>");
        result.Should().Contain("<TransactionCount>1</TransactionCount>");
    }

    [Fact]
    public async Task ExportTransactionHistoryToXml_WithSpecialCharacters_ShouldEscapeProperly()
    {
        // Arrange
        var transactions = new List<TransactionSummary>
        {
            new() { TransactionId = Guid.NewGuid(), Type = CreditTransactionType.ProjectPayment, Amount = 100, Description = "Test & <special> chars", Status = "Completed", CreatedAt = DateTime.UtcNow }
        };

        // Act
        var result = await _exportService.ExportTransactionHistoryToXmlAsync(transactions);

        // Assert - Should escape special characters
        result.Should().NotContain("Test & <special> chars"); // Raw unescaped
        result.Should().Contain("&amp;"); // Escaped ampersand
    }

    #endregion

    #region Excel Export Tests

    [Fact]
    public async Task ExportReportToExcel_ShouldReturnNonEmptyBytes()
    {
        // Arrange
        var report = CreateTestCreditSummaryReport();

        // Act
        var result = await _exportService.ExportReportToExcelAsync(report, includeCharts: true);

        // Assert
        result.Should().NotBeNull();
        result.Length.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task ExportTransactionHistoryToExcel_ShouldReturnNonEmptyBytes()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var transactions = new List<TransactionSummary>
        {
            new() { TransactionId = Guid.NewGuid(), Type = CreditTransactionType.ProjectPayment, Amount = 100, Description = "Test", Status = "Completed", CreatedAt = DateTime.UtcNow }
        };
        var categoryBreakdown = new List<TransactionCategoryBreakdown>();

        // Act
        var result = await _exportService.ExportTransactionHistoryToExcelAsync(
            userId, transactions, categoryBreakdown, DateTime.UtcNow.AddMonths(-1), DateTime.UtcNow);

        // Assert
        result.Should().NotBeNull();
        result.Length.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task ExportDashboardToExcel_ShouldReturnNonEmptyBytes()
    {
        // Arrange
        var dashboard = CreateTestDashboardData();
        var analytics = new AnalyticsData
        {
            UserId = Guid.NewGuid(),
            CurrentBalance = new BalanceAnalytics { CurrentBalance = 1000 },
            Spending = new SpendingAnalytics { TotalSpent = 500 },
            Earnings = new EarningAnalytics { TotalEarned = 1500 }
        };

        // Act
        var result = await _exportService.ExportDashboardToExcelAsync(dashboard, analytics);

        // Assert
        result.Should().NotBeNull();
        result.Length.Should().BeGreaterThan(0);
    }

    #endregion

    #region Template Management Tests

    [Theory]
    [InlineData(ExportFormat.CSV)]
    [InlineData(ExportFormat.PDF)]
    [InlineData(ExportFormat.JSON)]
    [InlineData(ExportFormat.XML)]
    [InlineData(ExportFormat.Excel)]
    public async Task GetAvailableTemplates_ForAllFormats_ShouldReturnTemplates(ExportFormat format)
    {
        // Act
        var templates = await _exportService.GetAvailableTemplatesAsync(format);

        // Assert
        templates.Should().NotBeEmpty();
        templates.Should().AllSatisfy(t =>
        {
            t.Format.Should().Be(format);
            t.Id.Should().NotBeNullOrEmpty();
            t.Name.Should().NotBeNullOrEmpty();
        });
    }

    [Fact]
    public async Task GetAvailableTemplates_ShouldHaveOneDefaultPerFormat()
    {
        // Act
        var csvTemplates = await _exportService.GetAvailableTemplatesAsync(ExportFormat.CSV);
        var pdfTemplates = await _exportService.GetAvailableTemplatesAsync(ExportFormat.PDF);

        // Assert
        csvTemplates.Count(t => t.IsDefault).Should().Be(1);
        pdfTemplates.Count(t => t.IsDefault).Should().Be(1);
    }

    [Fact]
    public async Task ExportWithTemplate_WithCsvTemplate_ShouldReturnCsvResult()
    {
        // Arrange
        var data = new { Test = "Value" };

        // Act
        var result = await _exportService.ExportWithTemplateAsync(data, "csv-basic");

        // Assert
        result.Success.Should().BeTrue();
        result.ContentType.Should().Be("text/csv");
        result.FileName.Should().EndWith(".csv");
    }

    [Fact]
    public async Task ExportWithTemplate_WithPdfTemplate_ShouldReturnPdfResult()
    {
        // Arrange
        var data = new { Test = "Value" };

        // Act
        var result = await _exportService.ExportWithTemplateAsync(data, "pdf-summary");

        // Assert
        result.Success.Should().BeTrue();
        result.ContentType.Should().Be("application/pdf");
        result.FileName.Should().EndWith(".pdf");
    }

    [Fact]
    public async Task ExportWithTemplate_WithJsonTemplate_ShouldReturnJsonResult()
    {
        // Arrange
        var data = new { Test = "Value" };

        // Act
        var result = await _exportService.ExportWithTemplateAsync(data, "json-structured");

        // Assert
        result.Success.Should().BeTrue();
        result.ContentType.Should().Be("application/json");
        result.FileName.Should().EndWith(".json");
    }

    [Fact]
    public async Task ExportWithTemplate_WithXmlTemplate_ShouldReturnXmlResult()
    {
        // Arrange
        var data = new { Test = "Value" };

        // Act
        var result = await _exportService.ExportWithTemplateAsync(data, "xml-standard");

        // Assert
        result.Success.Should().BeTrue();
        result.ContentType.Should().Be("application/xml");
        result.FileName.Should().EndWith(".xml");
    }

    #endregion

    #region Validation Tests

    [Fact]
    public async Task ValidateExportData_WithNullData_ShouldReturnInvalid()
    {
        // Act
        var result = await _exportService.ValidateExportDataAsync<object>(null!);

        // Assert
        result.IsValid.Should().BeFalse();
        result.ValidationErrors.Should().Contain("Data cannot be null");
    }

    [Fact]
    public async Task ValidateExportData_WithValidCreditSummaryReport_ShouldReturnValid()
    {
        // Arrange
        var report = CreateTestCreditSummaryReport();

        // Act
        var result = await _exportService.ValidateExportDataAsync(report);

        // Assert
        result.IsValid.Should().BeTrue();
        result.ValidationErrors.Should().BeEmpty();
    }

    [Fact]
    public async Task ValidateExportData_WithEmptyUserId_ShouldReturnInvalid()
    {
        // Arrange
        var report = CreateTestCreditSummaryReport();
        report.UserId = Guid.Empty;

        // Act
        var result = await _exportService.ValidateExportDataAsync(report);

        // Assert
        result.IsValid.Should().BeFalse();
        result.ValidationErrors.Should().Contain("User ID cannot be empty");
    }

    [Fact]
    public async Task ValidateExportData_WithInvalidDateRange_ShouldReturnInvalid()
    {
        // Arrange
        var report = CreateTestCreditSummaryReport();
        report.Period = new ReportPeriodInfo
        {
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(-30) // End before start
        };

        // Act
        var result = await _exportService.ValidateExportDataAsync(report);

        // Assert
        result.IsValid.Should().BeFalse();
        result.ValidationErrors.Should().Contain("Start date cannot be after end date");
    }

    [Fact]
    public async Task ValidateExportData_WithEmptyTransactions_ShouldReturnWarning()
    {
        // Arrange
        var transactions = new List<TransactionSummary>();

        // Act
        var result = await _exportService.ValidateExportDataAsync(transactions.AsEnumerable());

        // Assert
        result.IsValid.Should().BeTrue();
        result.Warnings.Should().Contain("No transactions to export");
    }

    [Fact]
    public async Task ValidateExportData_WithInvalidTransactions_ShouldReturnInvalid()
    {
        // Arrange
        var transactions = new List<TransactionSummary>
        {
            new() { TransactionId = Guid.Empty, Type = CreditTransactionType.ProjectPayment, Amount = 100, Status = "Completed", CreatedAt = default }
        };

        // Act
        var result = await _exportService.ValidateExportDataAsync(transactions.AsEnumerable());

        // Assert
        result.IsValid.Should().BeFalse();
        result.ValidationErrors.Should().Contain(e => e.Contains("invalid data"));
    }

    [Fact]
    public async Task ValidateExportData_WithLargeNegativeBalance_ShouldReturnWarning()
    {
        // Arrange
        var report = CreateTestCreditSummaryReport();
        report.Summary.EndingBalance = -50000;

        // Act
        var result = await _exportService.ValidateExportDataAsync(report);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Warnings.Should().Contain("Large negative balance detected");
    }

    #endregion

    #region Privacy Filter Tests

    [Fact]
    public async Task ApplyPrivacyFilters_ShouldReturnData()
    {
        // Arrange
        var data = new { Name = "Test", Value = 123 };

        // Act
        var result = await _exportService.ApplyPrivacyFiltersAsync(data, ExportPrivacyLevel.Full);

        // Assert
        result.Should().NotBeNull();
    }

    #endregion

    #region Batch Export Tests

    [Fact]
    public async Task ExportMultipleUsers_ShouldReturnResultsForAllUsers()
    {
        // Arrange
        var userIds = new List<Guid> { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() };

        // Act
        var result = await _exportService.ExportMultipleUsersAsync(
            userIds, ExportFormat.CSV, DateTime.UtcNow.AddMonths(-1), DateTime.UtcNow);

        // Assert
        result.Success.Should().BeTrue();
        result.TotalFiles.Should().Be(3);
        result.ExportResults.Should().HaveCount(3);
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public async Task ExportMultipleUsers_ShouldSetCorrectZipFileName()
    {
        // Arrange
        var userIds = new List<Guid> { Guid.NewGuid() };

        // Act
        var result = await _exportService.ExportMultipleUsersAsync(
            userIds, ExportFormat.JSON, DateTime.UtcNow.AddMonths(-1), DateTime.UtcNow);

        // Assert
        result.ZipFileName.Should().StartWith("bulk_export_");
        result.ZipFileName.Should().EndWith(".zip");
    }

    [Fact]
    public async Task ExportComprehensiveUserData_ShouldReturnSuccessResult()
    {
        // Arrange
        var userId = Guid.NewGuid();

        // Act
        var result = await _exportService.ExportComprehensiveUserDataAsync(userId, ExportFormat.PDF);

        // Assert
        result.Success.Should().BeTrue();
        result.FileName.Should().Contain(userId.ToString());
        result.ContentType.Should().Be("application/pdf");
    }

    #endregion

    #region Formatting Tests

    [Fact]
    public void FormatCurrencyForExport_ShouldFormatWithLocale()
    {
        // Act
        var result = _exportService.FormatCurrencyForExport(1000, "USD", "en-US");

        // Assert
        result.Should().Be("1,000");
    }

    [Fact]
    public void FormatCurrencyForExport_WithInvalidLocale_ShouldReturnDefaultFormat()
    {
        // Act
        var result = _exportService.FormatCurrencyForExport(1000, "USD", "invalid-locale");

        // Assert
        result.Should().Be("1000"); // Default format without thousands separator
    }

    #endregion

    #region Chart Generation Tests

    [Fact]
    public async Task GenerateChartImage_ShouldReturnNonEmptyBytes()
    {
        // Arrange
        var data = new { Values = new[] { 10, 20, 30 } };
        var options = new ChartOptions { Width = 800, Height = 600 };

        // Act
        var result = await _exportService.GenerateChartImageAsync(data, ChartType.BarChart, options);

        // Assert
        result.Should().NotBeNull();
        result.Length.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GenerateSpendingBreakdownChart_ShouldReturnNonEmptyBytes()
    {
        // Arrange
        var categoryBreakdown = new List<TransactionCategoryBreakdown>
        {
            new() { Category = CreditTransactionType.ProjectPayment, TotalAmount = 500, TransactionCount = 5 },
            new() { Category = CreditTransactionType.Purchase, TotalAmount = 300, TransactionCount = 3 }
        };
        var options = new ChartOptions { Width = 800, Height = 600 };

        // Act
        var result = await _exportService.GenerateSpendingBreakdownChartAsync(categoryBreakdown, options);

        // Assert
        result.Should().NotBeNull();
        result.Length.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GenerateEarningTrendsChart_ShouldReturnNonEmptyBytes()
    {
        // Arrange
        var trendData = new List<PeriodTrendData>
        {
            new() { PeriodStart = DateTime.UtcNow.AddMonths(-2), Earnings = 500 },
            new() { PeriodStart = DateTime.UtcNow.AddMonths(-1), Earnings = 700 }
        };
        var options = new ChartOptions { Width = 800, Height = 600 };

        // Act
        var result = await _exportService.GenerateEarningTrendsChartAsync(trendData, options);

        // Assert
        result.Should().NotBeNull();
        result.Length.Should().BeGreaterThan(0);
    }

    #endregion

    #region Helper Methods

    private static CreditSummaryReport CreateTestCreditSummaryReport()
    {
        return new CreditSummaryReport
        {
            UserId = Guid.NewGuid(),
            Period = new ReportPeriodInfo
            {
                StartDate = DateTime.UtcNow.AddMonths(-1),
                EndDate = DateTime.UtcNow,
                Type = ReportPeriodType.Monthly
            },
            GeneratedAt = DateTime.UtcNow,
            Summary = new FinancialSummary
            {
                TotalEarned = 1000,
                TotalSpent = 500,
                StartingBalance = 100,
                EndingBalance = 600,
                TransactionCount = 10,
                AverageTransactionSize = 50
            },
            CategoryBreakdowns = new List<TransactionCategoryBreakdown>()
        };
    }

    private static UserDashboardData CreateTestDashboardData()
    {
        return new UserDashboardData
        {
            UserId = Guid.NewGuid(),
            Wallet = new WalletDashboardSummary
            {
                CurrentBalance = 1000,
                AvailableBalance = 800
            },
            MonthlyStats = new MonthlyPerformance
            {
                CurrentMonthEarnings = 500,
                PreviousMonthEarnings = 400
            },
            Goals = new GoalProgress
            {
                GoalStatus = "On Track"
            }
        };
    }

    #endregion
}
