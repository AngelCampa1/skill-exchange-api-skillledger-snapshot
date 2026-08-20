using SkillLedger.Tests.Infrastructure;
using System.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using SkillLedger.Core.Interfaces;
using SkillLedger.Infrastructure.Configuration;
using SkillLedger.Infrastructure.Services;
using Xunit;

namespace SkillLedger.Tests.Core.Services
{
    [UnitTest]
    [SecurityTest]
    public class VirusScanServiceTests
    {
        private readonly Mock<ILogger<VirusScanService>> _mockLogger;
        private readonly VirusScanService _virusScanService;
        private readonly MediaUploadConfiguration _config;

        public VirusScanServiceTests()
        {
            _mockLogger = new Mock<ILogger<VirusScanService>>();

            _config = new MediaUploadConfiguration
            {
                MaxFileSizeBytes = 10 * 1024 * 1024, // 10MB
                SecurityKey = "test-key"
            };

            var optionsWrapper = Options.Create(_config);

            _virusScanService = new VirusScanService(_mockLogger.Object, optionsWrapper);
        }

        [Fact]
        public async Task QuickScanAsync_AllowedFileType_ReturnsClean()
        {
            // Arrange
            string fileName = "document.pdf";
            string contentType = "application/pdf";
            long fileSize = 1024 * 1024; // 1MB

            // Act
            var result = await _virusScanService.QuickScanAsync(fileName, contentType, fileSize);

            // Assert
            Assert.True(result.IsClean);
            Assert.Empty(result.Threats);
            Assert.True(result.ScanCompleted);
            Assert.Equal("SkillLedger Quick Scanner", result.ScanEngine);
        }

        [Fact]
        public async Task QuickScanAsync_BlockedFileExtension_ReturnsThreat()
        {
            // Arrange
            string fileName = "malware.exe";
            string contentType = "application/octet-stream";
            long fileSize = 1024;

            // Act
            var result = await _virusScanService.QuickScanAsync(fileName, contentType, fileSize);

            // Assert
            Assert.False(result.IsClean);
            Assert.Single(result.Threats);
            Assert.Contains(".exe", result.Threats.First().ThreatName);
            Assert.Equal(ThreatSeverity.High, result.Threats.First().Severity);
        }

        [Fact]
        public async Task QuickScanAsync_FileTooLarge_ReturnsThreat()
        {
            // Arrange
            string fileName = "largefile.pdf";
            string contentType = "application/pdf";
            long fileSize = _config.MaxFileSizeBytes + 1;

            // Act
            var result = await _virusScanService.QuickScanAsync(fileName, contentType, fileSize);

            // Assert
            Assert.False(result.IsClean);
            Assert.Single(result.Threats);
            Assert.Contains("File size exceeded", result.Threats.First().ThreatName);
            Assert.Equal(ThreatSeverity.Low, result.Threats.First().Severity);
        }

        [Fact]
        public async Task QuickScanAsync_UnallowedMimeType_ReturnsThreat()
        {
            // Arrange
            string fileName = "script.php";
            string contentType = "application/x-httpd-php";
            long fileSize = 1024;

            // Act
            var result = await _virusScanService.QuickScanAsync(fileName, contentType, fileSize);

            // Assert
            Assert.False(result.IsClean);
            Assert.Contains(result.Threats, t => t.ThreatName.Contains("Suspicious MIME type"));
            Assert.Equal(ThreatSeverity.Medium, result.Threats.First(t => t.ThreatName.Contains("MIME")).Severity);
        }

        [Fact]
        public async Task QuickScanAsync_SuspiciousFilename_ReturnsThreat()
        {
            // Arrange
            string fileName = "autorun.inf";
            string contentType = "text/plain";
            long fileSize = 1024;

            // Act
            var result = await _virusScanService.QuickScanAsync(fileName, contentType, fileSize);

            // Assert
            Assert.False(result.IsClean);
            Assert.Contains(result.Threats, t => t.ThreatName.Contains("Suspicious filename pattern"));
            Assert.Equal(ThreatSeverity.Medium, result.Threats.First(t => t.ThreatName.Contains("filename")).Severity);
        }

        [Fact]
        public async Task ScanFileAsync_CleanTextFile_ReturnsClean()
        {
            // Arrange
            string content = "This is a clean text file with no malicious content.";
            var fileStream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(content));
            string fileName = "clean.txt";
            string contentType = "text/plain";

            // Act
            var result = await _virusScanService.ScanFileAsync(fileStream, fileName, contentType);

            // Assert
            Assert.True(result.IsClean);
            Assert.Empty(result.Threats);
            Assert.True(result.ScanCompleted);
            Assert.Equal("SkillLedger Basic Scanner", result.ScanEngine);
            Assert.True(result.ScanDurationMs >= 0);
        }

        [Fact]
        public async Task ScanFileAsync_SuspiciousHtmlContent_ReturnsThreat()
        {
            // Arrange
            string content = "<html><script>alert('malicious');</script></html>";
            var fileStream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(content));
            string fileName = "suspicious.html";
            string contentType = "text/html";

            // Act
            var result = await _virusScanService.ScanFileAsync(fileStream, fileName, contentType);

            // Assert
            Assert.False(result.IsClean);
            Assert.Contains(result.Threats, t => t.ThreatName.Contains("<script"));
            Assert.Equal(ThreatSeverity.Medium, result.Threats.First().Severity);
        }

        [Fact]
        public async Task ScanFileAsync_JavaScriptWithSuspiciousPatterns_ReturnsThreat()
        {
            // Arrange
            string content = "function malicious() { eval(userInput); document.write(dangerousContent); }";
            var fileStream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(content));
            string fileName = "script.js";
            string contentType = "text/javascript";

            // Act
            var result = await _virusScanService.ScanFileAsync(fileStream, fileName, contentType);

            // Assert
            Assert.False(result.IsClean);
            Assert.True(result.Threats.Count >= 2); // Should detect both 'eval(' and 'document.write(' patterns plus extension block

            // Check for content pattern threats specifically (Medium severity)
            var contentThreats = result.Threats.Where(t => t.ThreatName.Contains("Suspicious content pattern")).ToList();
            Assert.True(contentThreats.Count >= 2); // Should detect both 'eval(' and 'document.write(' patterns
            Assert.All(contentThreats, t => Assert.Equal(ThreatSeverity.Medium, t.Severity));

            // Verify specific patterns are detected
            Assert.Contains(contentThreats, t => t.ThreatName.Contains("eval("));
            Assert.Contains(contentThreats, t => t.ThreatName.Contains("document.write("));
        }

        [Theory]
        [InlineData("document.pdf", "application/pdf", true)]
        [InlineData("image.jpg", "image/jpeg", true)]
        [InlineData("text.txt", "text/plain", true)]
        [InlineData("malware.exe", "application/octet-stream", false)]
        [InlineData("script.bat", "application/octet-stream", false)]
        [InlineData("archive.zip", "application/zip", true)]
        [InlineData("spreadsheet.xlsx", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", true)]
        public async Task IsFileTypeAllowedAsync_VariousFileTypes_ReturnsExpectedResult(
            string fileName, string contentType, bool expectedAllowed)
        {
            // Act
            var result = await _virusScanService.IsFileTypeAllowedAsync(fileName, contentType);

            // Assert
            Assert.Equal(expectedAllowed, result);
        }

        [Fact]
        public async Task GetBlockedFileExtensionsAsync_ReturnsExpectedExtensions()
        {
            // Act
            var blockedExtensions = await _virusScanService.GetBlockedFileExtensionsAsync();

            // Assert
            Assert.NotEmpty(blockedExtensions);
            Assert.Contains(".exe", blockedExtensions);
            Assert.Contains(".bat", blockedExtensions);
            Assert.Contains(".vbs", blockedExtensions);
            Assert.Contains(".js", blockedExtensions);
            Assert.Contains(".dll", blockedExtensions);
        }

        [Fact]
        public async Task GetAllowedMimeTypesAsync_ReturnsExpectedTypes()
        {
            // Act
            var allowedTypes = await _virusScanService.GetAllowedMimeTypesAsync();

            // Assert
            Assert.NotEmpty(allowedTypes);
            Assert.Contains("text/plain", allowedTypes);
            Assert.Contains("application/pdf", allowedTypes);
            Assert.Contains("image/jpeg", allowedTypes);
            Assert.Contains("application/json", allowedTypes);
            Assert.Contains("application/zip", allowedTypes);
        }

        [Fact]
        public async Task GetScanEngineInfoAsync_ReturnsValidInfo()
        {
            // Act
            var engineInfo = await _virusScanService.GetScanEngineInfoAsync();

            // Assert
            Assert.Equal("SkillLedger Basic Scanner", engineInfo.EngineName);
            Assert.Equal("1.0.0", engineInfo.Version);
            Assert.True(engineInfo.IsOperational);
            Assert.NotEmpty(engineInfo.Properties);
            Assert.Contains("ScanCapabilities", engineInfo.Properties.Keys);
            Assert.Contains("BlockedExtensions", engineInfo.Properties.Keys);
            Assert.Contains("AllowedMimeTypes", engineInfo.Properties.Keys);
        }

        [Fact]
        public async Task UpdateVirusDefinitionsAsync_AlwaysReturnsTrue()
        {
            // Act
            var result = await _virusScanService.UpdateVirusDefinitionsAsync();

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task ScanFileAsync_StreamResetAfterScan()
        {
            // Arrange
            string content = "test content";
            var fileStream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(content));
            string fileName = "test.txt";
            string contentType = "text/plain";

            // Move stream position to verify it gets reset
            fileStream.Position = 5;

            // Act
            var result = await _virusScanService.ScanFileAsync(fileStream, fileName, contentType);

            // Assert
            Assert.Equal(0, fileStream.Position); // Stream should be reset to beginning
            Assert.True(result.ScanCompleted);
        }

        [Fact]
        public async Task ScanFileAsync_LargeFile_HandlesGracefully()
        {
            // Arrange
            var largeContent = new byte[1024 * 1024]; // 1MB of zeros
            var fileStream = new MemoryStream(largeContent);
            string fileName = "largefile.bin";
            string contentType = "application/octet-stream";

            // Act
            var result = await _virusScanService.ScanFileAsync(fileStream, fileName, contentType);

            // Assert
            // Should complete scan even for larger files
            Assert.True(result.ScanCompleted);
            Assert.True(result.ScanDurationMs >= 0);

            // May or may not be clean depending on content and file type restrictions
            // But scan should complete without throwing
        }

        [Fact]
        public async Task ScanFileAsync_EmptyStream_HandlesGracefully()
        {
            // Arrange
            var fileStream = new MemoryStream();
            string fileName = "empty.txt";
            string contentType = "text/plain";

            // Act
            var result = await _virusScanService.ScanFileAsync(fileStream, fileName, contentType);

            // Assert
            Assert.True(result.ScanCompleted);
            Assert.True(result.ScanDurationMs >= 0);
        }

        [Fact]
        public async Task ScanFileByPathAsync_NonExistentFile_ReturnsError()
        {
            // Arrange
            string nonExistentPath = "/path/to/nonexistent/file.txt";

            // Act
            var result = await _virusScanService.ScanFileAsync(nonExistentPath);

            // Assert
            Assert.False(result.ScanCompleted);
            Assert.False(result.IsClean);
            Assert.NotNull(result.ErrorMessage);
            Assert.Contains("Error scanning file", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
        }
    }
}