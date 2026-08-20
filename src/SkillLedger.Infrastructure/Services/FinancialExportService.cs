using System.Globalization;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using SkillLedger.Core.DTOs;
using SkillLedger.Core.Enums;
using SkillLedger.Core.Interfaces;
using ExportFormat = SkillLedger.Core.Enums.ExportFormat;
using ExportTemplate = SkillLedger.Core.Interfaces.ExportTemplate;

namespace SkillLedger.Infrastructure.Services;

/// <summary>
/// Service for exporting financial reports in multiple formats
/// Supports CSV, PDF, JSON, XML, and Excel exports with template management
/// </summary>
public class FinancialExportService : IFinancialExportService
{
    private readonly ILogger<FinancialExportService> _logger;

    public FinancialExportService(ILogger<FinancialExportService> logger)
    {
        _logger = logger;
    }

    #region CSV Export

    /// <summary>
    /// Export transaction data to CSV format
    /// </summary>
    public Task<string> ExportToCsvAsync(IEnumerable<TransactionSummary> data, bool includeHeaders = true)
    {
        try
        {
            _logger.LogInformation("Starting CSV export for {Count} transactions", data.Count());

            var csv = new StringBuilder();

            if (includeHeaders)
            {
                csv.AppendLine("Date,Type,Amount,Description,Reference ID,Status");
            }

            foreach (var transaction in data.OrderByDescending(t => t.CreatedAt))
            {
                csv.AppendLine($"{transaction.CreatedAt:yyyy-MM-dd HH:mm:ss}," +
                              $"{transaction.Type}," +
                              $"{transaction.Amount}," +
                              $"\"{transaction.Description?.Replace("\"", "\"\"")}\"," +
                              $"{transaction.TransactionId}," +
                              $"{transaction.Status}");
            }

            return Task.FromResult(csv.ToString());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting transactions to CSV");
            throw;
        }
    }

    /// <summary>
    /// Export credit summary report to CSV format
    /// </summary>
    public Task<string> ExportReportToCsvAsync(CreditSummaryReport report)
    {
        try
        {
            _logger.LogInformation("Starting CSV export for credit summary report for user {UserId}", report.UserId);

            var csv = new StringBuilder();
            csv.AppendLine("Credit Summary Report");
            csv.AppendLine($"User ID,{report.UserId}");
            csv.AppendLine($"Period,{report.Period.StartDate:yyyy-MM-dd} to {report.Period.EndDate:yyyy-MM-dd}");
            csv.AppendLine($"Generated,{report.GeneratedAt:yyyy-MM-dd HH:mm:ss}");
            csv.AppendLine();

            csv.AppendLine("Metric,Value");
            csv.AppendLine($"Total Credits Earned,{report.Summary.TotalEarned}");
            csv.AppendLine($"Total Credits Spent,{report.Summary.TotalSpent}");
            csv.AppendLine($"Net Credit Change,{report.Summary.NetChange}");
            csv.AppendLine($"Starting Balance,{report.Summary.StartingBalance}");
            csv.AppendLine($"Ending Balance,{report.Summary.EndingBalance}");
            csv.AppendLine($"Transaction Count,{report.Summary.TransactionCount}");
            csv.AppendLine($"Average Transaction Size,{report.Summary.AverageTransactionSize:F2}");
            csv.AppendLine();

            if (report.CategoryBreakdowns?.Any() == true)
            {
                csv.AppendLine("Category Breakdown");
                csv.AppendLine("Transaction Type,Credit Amount,Transaction Count");
                foreach (var category in report.CategoryBreakdowns)
                {
                    csv.AppendLine($"{category.Category},{category.TotalAmount},{category.TransactionCount}");
                }
            }

            return Task.FromResult(csv.ToString());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting credit summary report to CSV for user {UserId}", report.UserId);
            throw;
        }
    }

    /// <summary>
    /// Export user dashboard data to CSV format
    /// </summary>
    public Task<string> ExportDashboardToCsvAsync(UserDashboardData dashboardData)
    {
        try
        {
            _logger.LogInformation("Starting CSV export for dashboard data for user {UserId}", dashboardData.UserId);

            var csv = new StringBuilder();
            csv.AppendLine("User Dashboard Data");
            csv.AppendLine($"User ID,{dashboardData.UserId}");
            csv.AppendLine($"Generated,{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}");
            csv.AppendLine();

            csv.AppendLine("Metric,Value");
            csv.AppendLine($"Current Balance,{dashboardData.Wallet.CurrentBalance}");
            csv.AppendLine($"Available Balance,{dashboardData.Wallet.AvailableBalance}");
            csv.AppendLine($"Credits This Month,{dashboardData.MonthlyStats.CurrentMonthEarnings}");
            csv.AppendLine($"Previous Month Credits,{dashboardData.MonthlyStats.PreviousMonthEarnings}");
            csv.AppendLine($"Goal Status,{dashboardData.Goals.GoalStatus}");

            return Task.FromResult(csv.ToString());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting dashboard data to CSV for user {UserId}", dashboardData.UserId);
            throw;
        }
    }

    #endregion

    #region PDF Export

    /// <summary>
    /// Export credit summary report to PDF format
    /// </summary>
    public Task<byte[]> ExportReportToPdfAsync(CreditSummaryReport report, bool includeCharts = true)
    {
        try
        {
            _logger.LogInformation("Starting PDF export for credit summary report for user {UserId}", report.UserId);

            // Create HTML for PDF conversion (placeholder implementation)
            var html = new StringBuilder();
            html.AppendLine("<!DOCTYPE html>");
            html.AppendLine("<html><head>");
            html.AppendLine("<title>Credit Summary Report</title>");
            html.AppendLine("<style>");
            html.AppendLine("body { font-family: Arial, sans-serif; margin: 40px; }");
            html.AppendLine("table { border-collapse: collapse; width: 100%; }");
            html.AppendLine("th, td { border: 1px solid #ddd; padding: 8px; text-align: left; }");
            html.AppendLine("th { background-color: #f2f2f2; }");
            html.AppendLine(".header { text-align: center; margin-bottom: 30px; }");
            html.AppendLine("</style>");
            html.AppendLine("</head><body>");

            html.AppendLine("<div class='header'>");
            html.AppendLine("<h1>Credit Summary Report</h1>");
            html.AppendLine($"<p>Period: {report.Period.StartDate:yyyy-MM-dd} to {report.Period.EndDate:yyyy-MM-dd}</p>");
            html.AppendLine($"<p>Generated: {report.GeneratedAt:yyyy-MM-dd HH:mm:ss}</p>");
            html.AppendLine("</div>");

            html.AppendLine("<h2>Summary</h2>");
            html.AppendLine("<table>");
            html.AppendLine("<tr><th>Metric</th><th>Value</th></tr>");
            html.AppendLine($"<tr><td>Total Credits Earned</td><td>{report.Summary.TotalEarned:N0}</td></tr>");
            html.AppendLine($"<tr><td>Total Credits Spent</td><td>{report.Summary.TotalSpent:N0}</td></tr>");
            html.AppendLine($"<tr><td>Net Credit Change</td><td>{report.Summary.NetChange:N0}</td></tr>");
            html.AppendLine($"<tr><td>Starting Balance</td><td>{report.Summary.StartingBalance:N0}</td></tr>");
            html.AppendLine($"<tr><td>Ending Balance</td><td>{report.Summary.EndingBalance:N0}</td></tr>");
            html.AppendLine("</table>");

            if (report.CategoryBreakdowns?.Any() == true)
            {
                html.AppendLine("<h2>Category Breakdown</h2>");
                html.AppendLine("<table>");
                html.AppendLine("<tr><th>Transaction Type</th><th>Total Amount</th><th>Count</th></tr>");
                foreach (var category in report.CategoryBreakdowns)
                {
                    html.AppendLine($"<tr><td>{category.Category}</td><td>{category.TotalAmount:N0}</td><td>{category.TransactionCount}</td></tr>");
                }
                html.AppendLine("</table>");
            }

            html.AppendLine("</body></html>");

            return Task.FromResult(Encoding.UTF8.GetBytes(html.ToString()));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting credit summary report to PDF for user {UserId}", report.UserId);
            throw;
        }
    }

    /// <summary>
    /// Export transaction history to PDF format with formatting
    /// </summary>
    public Task<byte[]> ExportTransactionHistoryToPdfAsync(
        Guid userId,
        IEnumerable<TransactionSummary> transactions,
        DateTime startDate,
        DateTime endDate)
    {
        try
        {
            _logger.LogInformation("Starting PDF export for transaction history for user {UserId}", userId);

            var html = new StringBuilder();
            html.AppendLine("<!DOCTYPE html>");
            html.AppendLine("<html><head>");
            html.AppendLine("<title>Transaction History</title>");
            html.AppendLine("<style>");
            html.AppendLine("body { font-family: Arial, sans-serif; margin: 40px; }");
            html.AppendLine("table { border-collapse: collapse; width: 100%; }");
            html.AppendLine("th, td { border: 1px solid #ddd; padding: 8px; text-align: left; }");
            html.AppendLine("th { background-color: #f2f2f2; }");
            html.AppendLine(".header { text-align: center; margin-bottom: 30px; }");
            html.AppendLine("</style>");
            html.AppendLine("</head><body>");

            html.AppendLine("<div class='header'>");
            html.AppendLine("<h1>Transaction History</h1>");
            html.AppendLine($"<p>Period: {startDate:yyyy-MM-dd} to {endDate:yyyy-MM-dd}</p>");
            html.AppendLine($"<p>User ID: {userId}</p>");
            html.AppendLine("</div>");

            html.AppendLine("<table>");
            html.AppendLine("<tr><th>Date</th><th>Type</th><th>Amount</th><th>Description</th><th>Status</th></tr>");

            foreach (var transaction in transactions.OrderByDescending(t => t.CreatedAt).Take(100))
            {
                html.AppendLine($"<tr>");
                html.AppendLine($"<td>{transaction.CreatedAt:yyyy-MM-dd}</td>");
                html.AppendLine($"<td>{transaction.Type}</td>");
                html.AppendLine($"<td>{transaction.Amount:N0}</td>");
                html.AppendLine($"<td>{transaction.Description}</td>");
                html.AppendLine($"<td>{transaction.Status}</td>");
                html.AppendLine($"</tr>");
            }

            html.AppendLine("</table>");
            html.AppendLine("</body></html>");

            return Task.FromResult(Encoding.UTF8.GetBytes(html.ToString()));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting transaction history to PDF for user {UserId}", userId);
            throw;
        }
    }

    /// <summary>
    /// Export user dashboard to PDF format
    /// </summary>
    public Task<byte[]> ExportDashboardToPdfAsync(UserDashboardData dashboardData, bool includeCharts = true)
    {
        try
        {
            _logger.LogInformation("Starting PDF export for dashboard data for user {UserId}", dashboardData.UserId);

            var html = new StringBuilder();
            html.AppendLine("<!DOCTYPE html>");
            html.AppendLine("<html><head>");
            html.AppendLine("<title>Financial Dashboard</title>");
            html.AppendLine("<style>");
            html.AppendLine("body { font-family: Arial, sans-serif; margin: 40px; }");
            html.AppendLine("table { border-collapse: collapse; width: 100%; }");
            html.AppendLine("th, td { border: 1px solid #ddd; padding: 8px; text-align: left; }");
            html.AppendLine("th { background-color: #f2f2f2; }");
            html.AppendLine(".header { text-align: center; margin-bottom: 30px; }");
            html.AppendLine("</style>");
            html.AppendLine("</head><body>");

            html.AppendLine("<div class='header'>");
            html.AppendLine("<h1>Financial Dashboard</h1>");
            html.AppendLine($"<p>Generated: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}</p>");
            html.AppendLine("</div>");

            html.AppendLine("<h2>Current Status</h2>");
            html.AppendLine("<table>");
            html.AppendLine("<tr><th>Metric</th><th>Value</th></tr>");
            html.AppendLine($"<tr><td>Current Balance</td><td>{dashboardData.Wallet.CurrentBalance:N0}</td></tr>");
            html.AppendLine($"<tr><td>Available Balance</td><td>{dashboardData.Wallet.AvailableBalance:N0}</td></tr>");
            html.AppendLine($"<tr><td>Credits This Month</td><td>{dashboardData.MonthlyStats.CurrentMonthEarnings:N0}</td></tr>");
            html.AppendLine($"<tr><td>Previous Month Credits</td><td>{dashboardData.MonthlyStats.PreviousMonthEarnings:N0}</td></tr>");
            html.AppendLine($"<tr><td>Goal Status</td><td>{dashboardData.Goals.GoalStatus}</td></tr>");
            html.AppendLine("</table>");

            html.AppendLine("</body></html>");

            return Task.FromResult(Encoding.UTF8.GetBytes(html.ToString()));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting dashboard data to PDF for user {UserId}", dashboardData.UserId);
            throw;
        }
    }

    #endregion

    #region JSON Export

    /// <summary>
    /// Export financial data to JSON format
    /// </summary>
    public Task<string> ExportToJsonAsync<T>(T data, bool formatOutput = true)
    {
        try
        {
            _logger.LogInformation("Starting JSON export for data type {DataType}", typeof(T).Name);

            var options = new JsonSerializerOptions
            {
                WriteIndented = formatOutput,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
            };

            return Task.FromResult(JsonSerializer.Serialize(data, options));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting data to JSON for type {DataType}", typeof(T).Name);
            throw;
        }
    }

    /// <summary>
    /// Export credit summary report to structured JSON
    /// </summary>
    public async Task<string> ExportReportToJsonAsync(CreditSummaryReport report)
    {
        try
        {
            _logger.LogInformation("Starting JSON export for credit summary report for user {UserId}", report.UserId);

            var exportData = new
            {
                ReportInfo = new
                {
                    report.UserId,
                    StartDate = report.Period.StartDate,
                    EndDate = report.Period.EndDate,
                    report.GeneratedAt,
                    ReportType = "CreditSummary"
                },
                Summary = new
                {
                    TotalCreditsEarned = report.Summary.TotalEarned,
                    TotalCreditsSpent = report.Summary.TotalSpent,
                    NetCreditChange = report.Summary.NetChange,
                    StartingBalance = report.Summary.StartingBalance,
                    EndingBalance = report.Summary.EndingBalance,
                    TransactionCount = report.Summary.TransactionCount,
                    AverageTransactionSize = report.Summary.AverageTransactionSize
                },
                CategoryBreakdown = report.CategoryBreakdowns
            };

            return await ExportToJsonAsync(exportData, true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting credit summary report to JSON for user {UserId}", report.UserId);
            throw;
        }
    }

    /// <summary>
    /// Export analytics data to JSON format
    /// </summary>
    public async Task<string> ExportAnalyticsToJsonAsync(AnalyticsData analyticsData)
    {
        try
        {
            _logger.LogInformation("Starting JSON export for analytics data for user {UserId}", analyticsData.UserId);

            return await ExportToJsonAsync(analyticsData, true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting analytics data to JSON for user {UserId}", analyticsData.UserId);
            throw;
        }
    }

    #endregion

    #region XML Export

    /// <summary>
    /// Export financial data to XML format
    /// </summary>
    public Task<string> ExportToXmlAsync<T>(T data, string rootElementName = "FinancialData")
    {
        try
        {
            _logger.LogInformation("Starting XML export for data type {DataType}", typeof(T).Name);

            var xml = new StringBuilder();
            xml.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
            xml.AppendLine($"<{rootElementName}>");
            xml.AppendLine($"  <ExportedAt>{DateTime.UtcNow:yyyy-MM-ddTHH:mm:ssZ}</ExportedAt>");
            xml.AppendLine($"  <DataType>{typeof(T).Name}</DataType>");

            // Convert to JSON first, then to XML structure (simplified approach)
            var jsonData = JsonSerializer.Serialize(data, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            xml.AppendLine("  <Data>");
            xml.AppendLine($"    <JsonData><![CDATA[{jsonData}]]></JsonData>");
            xml.AppendLine("  </Data>");
            xml.AppendLine($"</{rootElementName}>");

            return Task.FromResult(xml.ToString());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting data to XML for type {DataType}", typeof(T).Name);
            throw;
        }
    }

    /// <summary>
    /// Export credit summary report to XML format
    /// </summary>
    public Task<string> ExportReportToXmlAsync(CreditSummaryReport report)
    {
        try
        {
            _logger.LogInformation("Starting XML export for credit summary report for user {UserId}", report.UserId);

            var xml = new StringBuilder();
            xml.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
            xml.AppendLine("<CreditSummaryReport>");
            xml.AppendLine($"  <UserId>{report.UserId}</UserId>");
            xml.AppendLine($"  <StartDate>{report.Period.StartDate:yyyy-MM-dd}</StartDate>");
            xml.AppendLine($"  <EndDate>{report.Period.EndDate:yyyy-MM-dd}</EndDate>");
            xml.AppendLine($"  <GeneratedAt>{report.GeneratedAt:yyyy-MM-ddTHH:mm:ssZ}</GeneratedAt>");
            xml.AppendLine($"  <TotalCreditsEarned>{report.Summary.TotalEarned}</TotalCreditsEarned>");
            xml.AppendLine($"  <TotalCreditsSpent>{report.Summary.TotalSpent}</TotalCreditsSpent>");
            xml.AppendLine($"  <NetCreditChange>{report.Summary.NetChange}</NetCreditChange>");
            xml.AppendLine($"  <StartingBalance>{report.Summary.StartingBalance}</StartingBalance>");
            xml.AppendLine($"  <EndingBalance>{report.Summary.EndingBalance}</EndingBalance>");
            xml.AppendLine($"  <TransactionCount>{report.Summary.TransactionCount}</TransactionCount>");
            xml.AppendLine($"  <AverageTransactionSize>{report.Summary.AverageTransactionSize:F2}</AverageTransactionSize>");

            if (report.CategoryBreakdowns?.Any() == true)
            {
                xml.AppendLine("  <CategoryBreakdown>");
                foreach (var category in report.CategoryBreakdowns)
                {
                    xml.AppendLine("    <Category>");
                    xml.AppendLine($"      <TransactionType>{category.Category}</TransactionType>");
                    xml.AppendLine($"      <TotalAmount>{category.TotalAmount}</TotalAmount>");
                    xml.AppendLine($"      <TransactionCount>{category.TransactionCount}</TransactionCount>");
                    xml.AppendLine("    </Category>");
                }
                xml.AppendLine("  </CategoryBreakdown>");
            }

            xml.AppendLine("</CreditSummaryReport>");

            return Task.FromResult(xml.ToString());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting credit summary report to XML for user {UserId}", report.UserId);
            throw;
        }
    }

    /// <summary>
    /// Export transaction history to XML format
    /// </summary>
    public Task<string> ExportTransactionHistoryToXmlAsync(IEnumerable<TransactionSummary> transactions)
    {
        try
        {
            _logger.LogInformation("Starting XML export for {Count} transactions", transactions.Count());

            var xml = new StringBuilder();
            xml.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
            xml.AppendLine("<TransactionHistory>");
            xml.AppendLine($"  <ExportedAt>{DateTime.UtcNow:yyyy-MM-ddTHH:mm:ssZ}</ExportedAt>");
            xml.AppendLine($"  <TransactionCount>{transactions.Count()}</TransactionCount>");
            xml.AppendLine("  <Transactions>");

            foreach (var transaction in transactions.OrderByDescending(t => t.CreatedAt))
            {
                xml.AppendLine("    <Transaction>");
                xml.AppendLine($"      <Date>{transaction.CreatedAt:yyyy-MM-ddTHH:mm:ssZ}</Date>");
                xml.AppendLine($"      <TransactionType>{transaction.Type}</TransactionType>");
                xml.AppendLine($"      <Amount>{transaction.Amount}</Amount>");
                xml.AppendLine($"      <Description>{System.Security.SecurityElement.Escape(transaction.Description ?? "")}</Description>");
                xml.AppendLine($"      <ReferenceId>{transaction.TransactionId}</ReferenceId>");
                xml.AppendLine($"      <Status>{transaction.Status}</Status>");
                xml.AppendLine("    </Transaction>");
            }

            xml.AppendLine("  </Transactions>");
            xml.AppendLine("</TransactionHistory>");

            return Task.FromResult(xml.ToString());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting transaction history to XML");
            throw;
        }
    }

    #endregion

    #region Excel Export

    /// <summary>
    /// Export financial data to Excel format (.xlsx)
    /// </summary>
    public async Task<byte[]> ExportReportToExcelAsync(CreditSummaryReport report, bool includeCharts = true)
    {
        try
        {
            _logger.LogInformation("Starting Excel export for credit summary report for user {UserId}", report.UserId);

            // For now, return CSV format as placeholder (would integrate with EPPlus in production)
            var csvData = await ExportReportToCsvAsync(report);
            return Encoding.UTF8.GetBytes(csvData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting credit summary report to Excel for user {UserId}", report.UserId);
            throw;
        }
    }

    /// <summary>
    /// Export transaction history to Excel with multiple sheets
    /// </summary>
    public async Task<byte[]> ExportTransactionHistoryToExcelAsync(
        Guid userId,
        IEnumerable<TransactionSummary> transactions,
        IEnumerable<TransactionCategoryBreakdown> categoryBreakdown,
        DateTime startDate,
        DateTime endDate)
    {
        try
        {
            _logger.LogInformation("Starting Excel export for transaction history for user {UserId}", userId);

            // For now, return CSV format as placeholder
            var csvData = await ExportToCsvAsync(transactions, true);
            return Encoding.UTF8.GetBytes(csvData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting transaction history to Excel for user {UserId}", userId);
            throw;
        }
    }

    /// <summary>
    /// Export analytics dashboard to Excel with multiple worksheets
    /// </summary>
    public async Task<byte[]> ExportDashboardToExcelAsync(UserDashboardData dashboardData, AnalyticsData analyticsData)
    {
        try
        {
            _logger.LogInformation("Starting Excel export for dashboard data for user {UserId}", dashboardData.UserId);

            // For now, return CSV format as placeholder
            var csvData = await ExportDashboardToCsvAsync(dashboardData);
            return Encoding.UTF8.GetBytes(csvData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting dashboard data to Excel for user {UserId}", dashboardData.UserId);
            throw;
        }
    }

    #endregion

    #region Template Management

    /// <summary>
    /// Get available export templates for a format
    /// </summary>
    public Task<List<ExportTemplate>> GetAvailableTemplatesAsync(ExportFormat format)
    {
        try
        {
            _logger.LogInformation("Getting available templates for format {Format}", format);

            // Return default templates (in production, would query database)
            var templates = new List<ExportTemplate>();

            switch (format)
            {
                case ExportFormat.CSV:
                    templates.AddRange(new[]
                    {
                        new ExportTemplate { Id = "csv-basic", Name = "Basic CSV Export", Format = format, IsDefault = true },
                        new ExportTemplate { Id = "csv-detailed", Name = "Detailed CSV with Metadata", Format = format }
                    });
                    break;
                case ExportFormat.PDF:
                    templates.AddRange(new[]
                    {
                        new ExportTemplate { Id = "pdf-summary", Name = "Summary Report PDF", Format = format, IsDefault = true },
                        new ExportTemplate { Id = "pdf-detailed", Name = "Detailed PDF with Charts", Format = format }
                    });
                    break;
                case ExportFormat.JSON:
                    templates.Add(new ExportTemplate { Id = "json-structured", Name = "Structured JSON Export", Format = format, IsDefault = true });
                    break;
                case ExportFormat.XML:
                    templates.Add(new ExportTemplate { Id = "xml-standard", Name = "Standard XML Export", Format = format, IsDefault = true });
                    break;
                case ExportFormat.Excel:
                    templates.AddRange(new[]
                    {
                        new ExportTemplate { Id = "excel-basic", Name = "Basic Excel Workbook", Format = format, IsDefault = true },
                        new ExportTemplate { Id = "excel-charts", Name = "Excel with Charts", Format = format }
                    });
                    break;
            }

            return Task.FromResult(templates);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting available templates for format {Format}", format);
            throw;
        }
    }

    /// <summary>
    /// Export using a custom template
    /// </summary>
    public async Task<FinancialExportResult> ExportWithTemplateAsync<T>(
        T data,
        string templateId,
        Dictionary<string, object>? customParameters = null)
    {
        try
        {
            _logger.LogInformation("Starting template export with template {TemplateId} for data type {DataType}",
                templateId, typeof(T).Name);

            // In production, would load template from database and apply customizations
            var result = new FinancialExportResult
            {
                Success = true
            };

            // Determine format based on template ID (simplified logic)
            if (templateId.StartsWith("csv"))
            {
                result.Content = await ExportToJsonAsync(data); // Placeholder
                result.ContentType = "text/csv";
                result.FileName = $"export_{templateId}_{DateTime.UtcNow:yyyyMMdd}.csv";
            }
            else if (templateId.StartsWith("pdf"))
            {
                var jsonContent = await ExportToJsonAsync(data); // Placeholder
                result.Content = Convert.ToBase64String(Encoding.UTF8.GetBytes(jsonContent));
                result.ContentType = "application/pdf";
                result.FileName = $"export_{templateId}_{DateTime.UtcNow:yyyyMMdd}.pdf";
            }
            else if (templateId.StartsWith("json"))
            {
                result.Content = await ExportToJsonAsync(data);
                result.ContentType = "application/json";
                result.FileName = $"export_{templateId}_{DateTime.UtcNow:yyyyMMdd}.json";
            }
            else if (templateId.StartsWith("xml"))
            {
                result.Content = await ExportToXmlAsync(data);
                result.ContentType = "application/xml";
                result.FileName = $"export_{templateId}_{DateTime.UtcNow:yyyyMMdd}.xml";
            }
            else
            {
                result.Content = await ExportToJsonAsync(data); // Default
                result.ContentType = "application/json";
                result.FileName = $"export_{templateId}_{DateTime.UtcNow:yyyyMMdd}.json";
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting with template {TemplateId}", templateId);
            return new FinancialExportResult
            {
                Success = false,
                ErrorMessage = ex.Message,
                FileName = $"export_error_{templateId}_{DateTime.UtcNow:yyyyMMdd}.txt"
            };
        }
    }

    #endregion

    #region Batch Export

    /// <summary>
    /// Export multiple users' financial data in bulk
    /// </summary>
    public Task<BulkExportResult> ExportMultipleUsersAsync(
        List<Guid> userIds,
        ExportFormat format,
        DateTime startDate,
        DateTime endDate)
    {
        try
        {
            _logger.LogInformation("Starting bulk export for {Count} users in format {Format}", userIds.Count, format);

            var result = new BulkExportResult
            {
                Success = true,
                ZipFileName = $"bulk_export_{DateTime.UtcNow:yyyyMMdd_HHmmss}.zip",
                TotalFiles = userIds.Count
            };

            foreach (var userId in userIds)
            {
                try
                {
                    // In production, would generate actual reports for each user
                    var userResult = new FinancialExportResult
                    {
                        Success = true,
                        Content = $"Export data for user {userId}",
                        ContentType = GetContentTypeForFormat(format),
                        FileName = $"user_export_{userId}_{DateTime.UtcNow:yyyyMMdd}{GetFileExtensionForFormat(format)}"
                    };

                    result.ExportResults.Add(userResult);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to export data for user {UserId}", userId);
                    result.Errors.Add($"Failed to export data for user {userId}: {ex.Message}");
                }
            }

            result.Success = result.ExportResults.Any();
            return Task.FromResult(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in bulk export for {Count} users", userIds.Count);
            return Task.FromResult(new BulkExportResult
            {
                Success = false,
                Errors = { ex.Message }
            });
        }
    }

    /// <summary>
    /// Export all financial reports for a user (comprehensive export)
    /// </summary>
    public Task<FinancialExportResult> ExportComprehensiveUserDataAsync(Guid userId, ExportFormat format)
    {
        try
        {
            _logger.LogInformation("Starting comprehensive export for user {UserId} in format {Format}", userId, format);

            // In production, would gather all financial data for the user
            var result = new FinancialExportResult
            {
                Success = true,
                Content = $"Comprehensive export data for user {userId}",
                ContentType = GetContentTypeForFormat(format),
                FileName = $"comprehensive_export_{userId}_{DateTime.UtcNow:yyyyMMdd}{GetFileExtensionForFormat(format)}"
            };

            return Task.FromResult(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in comprehensive export for user {UserId}", userId);
            return Task.FromResult(new FinancialExportResult
            {
                Success = false,
                ErrorMessage = ex.Message,
                FileName = $"export_error_{userId}_{DateTime.UtcNow:yyyyMMdd}.txt"
            });
        }
    }

    #endregion

    #region Validation and Formatting

    /// <summary>
    /// Validate export data before processing
    /// </summary>
    public Task<ExportValidationResult> ValidateExportDataAsync<T>(T data)
    {
        try
        {
            _logger.LogInformation("Validating export data of type {DataType}", typeof(T).Name);

            var result = new ExportValidationResult
            {
                IsValid = true,
                ValidatedAt = DateTime.UtcNow
            };

            if (data == null)
            {
                result.IsValid = false;
                result.ValidationErrors.Add("Data cannot be null");
                return Task.FromResult(result);
            }

            // Type-specific validations
            switch (data)
            {
                case CreditSummaryReport report:
                    ValidateCreditSummaryReport(report, result);
                    break;
                case IEnumerable<TransactionSummary> transactions:
                    ValidateTransactions(transactions, result);
                    break;
                case UserDashboardData dashboard:
                    ValidateDashboardData(dashboard, result);
                    break;
            }

            return Task.FromResult(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating export data of type {DataType}", typeof(T).Name);
            return Task.FromResult(new ExportValidationResult
            {
                IsValid = false,
                ValidationErrors = { ex.Message },
                ValidatedAt = DateTime.UtcNow
            });
        }
    }

    /// <summary>
    /// Apply privacy filters to export data
    /// </summary>
    public Task<T> ApplyPrivacyFiltersAsync<T>(T data, ExportPrivacyLevel privacyLevel)
    {
        try
        {
            _logger.LogInformation("Applying privacy level {PrivacyLevel} to data type {DataType}",
                privacyLevel, typeof(T).Name);

            // In production, would implement actual privacy filtering logic
            // For now, return data as-is (placeholder implementation)
            return Task.FromResult(data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error applying privacy filters to data type {DataType}", typeof(T).Name);
            throw;
        }
    }

    /// <summary>
    /// Format currency values for export
    /// </summary>
    public string FormatCurrencyForExport(int amount, string currencyCode = "USD", string locale = "en-US")
    {
        try
        {
            var culture = CultureInfo.GetCultureInfo(locale);

            // Credits are integer values, so format accordingly
            return amount.ToString("N0", culture);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error formatting currency for amount {Amount}, using default format", amount);
            return amount.ToString();
        }
    }

    #endregion

    #region Chart and Visualization Export

    /// <summary>
    /// Generate chart images for PDF/Excel export
    /// </summary>
    public Task<byte[]> GenerateChartImageAsync<T>(T data, ChartType chartType, ChartOptions options)
    {
        try
        {
            _logger.LogInformation("Generating {ChartType} chart for data type {DataType}", chartType, typeof(T).Name);

            // Placeholder implementation - would integrate with charting library in production
            var placeholder = $"Chart placeholder: {chartType} for {typeof(T).Name}";
            return Task.FromResult(Encoding.UTF8.GetBytes(placeholder));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating chart image for type {DataType}", typeof(T).Name);
            throw;
        }
    }

    /// <summary>
    /// Generate spending breakdown pie chart
    /// </summary>
    public Task<byte[]> GenerateSpendingBreakdownChartAsync(
        IEnumerable<TransactionCategoryBreakdown> categoryBreakdown,
        ChartOptions options)
    {
        try
        {
            _logger.LogInformation("Generating spending breakdown chart");

            // Placeholder implementation
            var placeholder = "Spending breakdown pie chart placeholder";
            return Task.FromResult(Encoding.UTF8.GetBytes(placeholder));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating spending breakdown chart");
            throw;
        }
    }

    /// <summary>
    /// Generate earning trends line chart
    /// </summary>
    public Task<byte[]> GenerateEarningTrendsChartAsync(IEnumerable<PeriodTrendData> trendData, ChartOptions options)
    {
        try
        {
            _logger.LogInformation("Generating earning trends chart");

            // Placeholder implementation
            var placeholder = "Earning trends line chart placeholder";
            return Task.FromResult(Encoding.UTF8.GetBytes(placeholder));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating earning trends chart");
            throw;
        }
    }

    #endregion

    #region Private Helper Methods

    private void ValidateCreditSummaryReport(CreditSummaryReport report, ExportValidationResult result)
    {
        if (report.UserId == Guid.Empty)
        {
            result.ValidationErrors.Add("User ID cannot be empty");
        }

        if (report.Period.StartDate > report.Period.EndDate)
        {
            result.ValidationErrors.Add("Start date cannot be after end date");
        }

        if (report.Summary.EndingBalance < 0 && Math.Abs(report.Summary.EndingBalance) > 10000)
        {
            result.Warnings.Add("Large negative balance detected");
        }

        result.RecordCount = 1;
        result.IsValid = !result.ValidationErrors.Any();
    }

    private void ValidateTransactions(IEnumerable<TransactionSummary> transactions, ExportValidationResult result)
    {
        var transactionList = transactions.ToList();
        result.RecordCount = transactionList.Count;

        if (!transactionList.Any())
        {
            result.Warnings.Add("No transactions to export");
        }

        var invalidTransactions = transactionList.Where(t =>
            t.CreatedAt == default ||
            t.TransactionId == Guid.Empty).ToList();

        if (invalidTransactions.Any())
        {
            result.ValidationErrors.Add($"{invalidTransactions.Count} transactions have invalid data");
        }

        result.IsValid = !result.ValidationErrors.Any();
    }

    private void ValidateDashboardData(UserDashboardData dashboard, ExportValidationResult result)
    {
        if (dashboard.UserId == Guid.Empty)
        {
            result.ValidationErrors.Add("User ID cannot be empty");
        }

        if (dashboard.Wallet.CurrentBalance < 0 && Math.Abs(dashboard.Wallet.CurrentBalance) > 10000)
        {
            result.Warnings.Add("Large negative balance detected in dashboard");
        }

        result.RecordCount = 1;
        result.IsValid = !result.ValidationErrors.Any();
    }

    private string GetContentTypeForFormat(ExportFormat format)
    {
        return format switch
        {
            ExportFormat.CSV => "text/csv",
            ExportFormat.PDF => "application/pdf",
            ExportFormat.JSON => "application/json",
            ExportFormat.XML => "application/xml",
            ExportFormat.Excel => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            _ => "application/octet-stream"
        };
    }

    private string GetFileExtensionForFormat(ExportFormat format)
    {
        return format switch
        {
            ExportFormat.CSV => ".csv",
            ExportFormat.PDF => ".pdf",
            ExportFormat.JSON => ".json",
            ExportFormat.XML => ".xml",
            ExportFormat.Excel => ".xlsx",
            _ => ".txt"
        };
    }

    #endregion
}