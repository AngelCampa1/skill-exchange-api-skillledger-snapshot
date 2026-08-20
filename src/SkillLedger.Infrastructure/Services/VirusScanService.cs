using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SkillLedger.Core.Interfaces;
using SkillLedger.Infrastructure.Configuration;
using System.Diagnostics;

namespace SkillLedger.Infrastructure.Services
{
    /// <summary>
    /// Basic virus scanning service implementation
    /// Uses file signature analysis and blocked extension checking
    /// Can be extended to integrate with commercial antivirus solutions
    /// </summary>
    public class VirusScanService : IVirusScanService
    {
        private readonly ILogger<VirusScanService> _logger;
        private readonly MediaUploadConfiguration _config;

        // Common malicious file signatures (simplified for demo)
        private readonly Dictionary<string, string> _maliciousSignatures = new()
        {
            { "4D5A", "PE Executable" },
            { "504B0304", "ZIP Archive (potentially malicious)" },
            { "255044462D312E", "PDF with suspicious content" }
        };

        // Blocked file extensions
        private readonly HashSet<string> _blockedExtensions = new()
        {
            ".exe", ".bat", ".cmd", ".com", ".scr", ".pif", ".vbs", ".js", ".jar", ".msi",
            ".dll", ".sys", ".drv", ".ocx", ".cpl", ".hta", ".reg", ".inf", ".scf", ".lnk",
            ".ps1", ".psm1", ".psc1", ".sh", ".bin", ".app", ".deb", ".rpm", ".dmg", ".pkg"
        };

        // Allowed MIME types
        private readonly HashSet<string> _allowedMimeTypes = new()
        {
            "text/plain", "text/html", "text/css", "text/javascript", "text/xml",
            "application/json", "application/xml", "application/yaml",
            "image/jpeg", "image/png", "image/gif", "image/bmp", "image/svg+xml", "image/webp",
            "audio/mpeg", "audio/wav", "audio/ogg", "audio/mp4",
            "video/mp4", "video/mpeg", "video/quicktime", "video/x-msvideo",
            "application/pdf", "application/msword", "application/vnd.ms-excel", "application/vnd.ms-powerpoint",
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            "application/vnd.openxmlformats-officedocument.presentationml.presentation",
            "application/zip", "application/x-zip-compressed", "application/gzip", "application/x-tar",
            "application/vnd.rar", "application/x-7z-compressed"
        };

        public VirusScanService(ILogger<VirusScanService> logger, IOptions<MediaUploadConfiguration> config)
        {
            _logger = logger;
            _config = config.Value;
        }

        public async Task<VirusScanResult> ScanFileAsync(Stream fileStream, string fileName, string contentType)
        {
            var stopwatch = Stopwatch.StartNew();
            var result = new VirusScanResult
            {
                ScanEngine = "SkillLedger Basic Scanner",
                EngineVersion = "1.0.0",
                ScanDate = DateTime.UtcNow
            };

            try
            {
                _logger.LogInformation("Starting virus scan for file: {FileName}", fileName);

                // Quick validation checks
                var quickScan = await QuickScanAsync(fileName, contentType, fileStream.Length);
                if (!quickScan.IsClean)
                {
                    result.IsClean = false;
                    result.Threats.AddRange(quickScan.Threats);
                }

                // File signature analysis
                if (result.Threats.Count == 0 && fileStream.CanSeek)
                {
                    fileStream.Position = 0;
                    var signatureThreats = await ScanFileSignatureAsync(fileStream, fileName);
                    result.Threats.AddRange(signatureThreats);
                }

                // Content-based scanning (always run for text-based files to get detailed threat analysis)
                if (fileStream.CanRead)
                {
                    fileStream.Position = 0;
                    var contentThreats = await ScanFileContentAsync(fileStream, fileName, contentType);
                    result.Threats.AddRange(contentThreats);
                }

                result.IsClean = result.Threats.Count == 0;
                result.ScanCompleted = true;

                if (!result.IsClean)
                {
                    _logger.LogWarning("Threats detected in file {FileName}: {Threats}",
                        fileName, string.Join(", ", result.Threats.Select(t => t.ThreatName)));
                }
                else
                {
                    _logger.LogInformation("File {FileName} passed virus scan", fileName);
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error scanning file {FileName}", fileName);
                result.ScanCompleted = false;
                result.ErrorMessage = ex.Message;
                result.IsClean = false; // Fail safe - reject if scan fails
                return result;
            }
            finally
            {
                stopwatch.Stop();
                result.ScanDurationMs = stopwatch.ElapsedMilliseconds;
                fileStream.Position = 0; // Reset stream position
            }
        }

        public async Task<VirusScanResult> ScanFileAsync(string filePath)
        {
            try
            {
                using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
                var fileName = Path.GetFileName(filePath);
                var contentType = GetContentTypeFromExtension(Path.GetExtension(filePath));

                return await ScanFileAsync(fileStream, fileName, contentType);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error scanning file at path {FilePath}", filePath);
                return new VirusScanResult
                {
                    IsClean = false,
                    ScanCompleted = false,
                    ErrorMessage = $"Error scanning file: {ex.Message}",
                    ScanEngine = "SkillLedger Basic Scanner",
                    EngineVersion = "1.0.0"
                };
            }
        }

        public async Task<VirusScanResult> QuickScanAsync(string fileName, string contentType, long fileSize)
        {
            await Task.CompletedTask;
            var result = new VirusScanResult
            {
                IsClean = true,
                ScanCompleted = true,
                ScanEngine = "SkillLedger Quick Scanner",
                EngineVersion = "1.0.0",
                ScanDate = DateTime.UtcNow
            };

            try
            {
                // Check file extension
                var extension = Path.GetExtension(fileName).ToLowerInvariant();
                if (_blockedExtensions.Contains(extension))
                {
                    result.Threats.Add(new ThreatInfo
                    {
                        ThreatName = $"Blocked file extension: {extension}",
                        Severity = ThreatSeverity.High,
                        Description = "File extension is not allowed for security reasons",
                        Action = "Block upload"
                    });
                }

                // Check MIME type (only if extension is not already blocked)
                if (!_blockedExtensions.Contains(extension) && !_allowedMimeTypes.Contains(contentType.ToLowerInvariant()))
                {
                    result.Threats.Add(new ThreatInfo
                    {
                        ThreatName = $"Suspicious MIME type: {contentType}",
                        Severity = ThreatSeverity.Medium,
                        Description = "MIME type is not in the allowed list",
                        Action = "Require additional validation"
                    });
                }

                // Check file size limits
                if (fileSize > _config.MaxFileSizeBytes)
                {
                    result.Threats.Add(new ThreatInfo
                    {
                        ThreatName = "File size exceeded",
                        Severity = ThreatSeverity.Low,
                        Description = $"File size {fileSize} bytes exceeds maximum allowed {_config.MaxFileSizeBytes} bytes",
                        Action = "Block upload"
                    });
                }

                // Check for suspicious file names
                var suspiciousPatterns = new[] { "autorun", "setup", "install", "update", "patch", "crack", "keygen" };
                if (suspiciousPatterns.Any(pattern => fileName.ToLowerInvariant().Contains(pattern)))
                {
                    result.Threats.Add(new ThreatInfo
                    {
                        ThreatName = "Suspicious filename pattern",
                        Severity = ThreatSeverity.Medium,
                        Description = "Filename contains suspicious patterns",
                        Action = "Flag for review"
                    });
                }

                result.IsClean = result.Threats.Count == 0;
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in quick scan for {FileName}", fileName);
                result.IsClean = false;
                result.ErrorMessage = ex.Message;
                return result;
            }
        }

        public async Task<ScanEngineInfo> GetScanEngineInfoAsync()
        {
            await Task.CompletedTask;
            return new ScanEngineInfo
            {
                EngineName = "SkillLedger Basic Scanner",
                Version = "1.0.0",
                DefinitionsDate = DateTime.UtcNow.Date, // Static for basic implementation
                IsOperational = true,
                Properties = new Dictionary<string, object>
                {
                    { "ScanCapabilities", "File signatures, Extensions, MIME types" },
                    { "BlockedExtensions", _blockedExtensions.Count },
                    { "AllowedMimeTypes", _allowedMimeTypes.Count },
                    { "MaliciousSignatures", _maliciousSignatures.Count }
                }
            };
        }

        public async Task<bool> UpdateVirusDefinitionsAsync()
        {
            // In a real implementation, this would download and update virus definitions
            // For the basic implementation, return true (no updates needed)
            await Task.CompletedTask;
            _logger.LogInformation("Virus definitions update requested - basic scanner uses static definitions");
            return true;
        }

        // WARNING-001 FIX: Remove async keyword - method has no await operations
        public Task<bool> IsFileTypeAllowedAsync(string fileName, string contentType)
        {
            try
            {
                var extension = Path.GetExtension(fileName).ToLowerInvariant();

                // Check blocked extensions
                if (_blockedExtensions.Contains(extension))
                {
                    return Task.FromResult(false);
                }

                // Check allowed MIME types
                if (!_allowedMimeTypes.Contains(contentType.ToLowerInvariant()))
                {
                    return Task.FromResult(false);
                }

                return Task.FromResult(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking file type allowance for {FileName}", fileName);
                return Task.FromResult(false); // Fail safe
            }
        }

        public async Task<List<string>> GetBlockedFileExtensionsAsync()
        {
            await Task.CompletedTask;
            return _blockedExtensions.ToList();
        }

        public async Task<List<string>> GetAllowedMimeTypesAsync()
        {
            await Task.CompletedTask;
            return _allowedMimeTypes.ToList();
        }

        private async Task<List<ThreatInfo>> ScanFileSignatureAsync(Stream fileStream, string fileName)
        {
            var threats = new List<ThreatInfo>();

            try
            {
                fileStream.Position = 0;
                var buffer = new byte[16]; // Read first 16 bytes for signature analysis
                var bytesRead = await fileStream.ReadAsync(buffer, 0, buffer.Length);

                if (bytesRead > 0)
                {
                    var signature = BitConverter.ToString(buffer, 0, Math.Min(8, bytesRead)).Replace("-", "");

                    foreach (var maliciousSignature in _maliciousSignatures)
                    {
                        if (signature.StartsWith(maliciousSignature.Key))
                        {
                            threats.Add(new ThreatInfo
                            {
                                ThreatName = $"Suspicious file signature: {maliciousSignature.Value}",
                                Severity = ThreatSeverity.High,
                                Description = $"File contains signature matching: {maliciousSignature.Value}",
                                Action = "Block upload",
                                Details = new Dictionary<string, object>
                                {
                                    { "Signature", signature },
                                    { "MatchedPattern", maliciousSignature.Key }
                                }
                            });
                        }
                    }
                }

                return threats;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error scanning file signature for {FileName}", fileName);
                return threats;
            }
        }

        private async Task<List<ThreatInfo>> ScanFileContentAsync(Stream fileStream, string fileName, string contentType)
        {
            var threats = new List<ThreatInfo>();

            try
            {
                fileStream.Position = 0;

                // For text files, scan content for suspicious patterns
                var extension = Path.GetExtension(fileName).ToLowerInvariant();
                _logger.LogInformation("ScanFileContentAsync: fileName={FileName}, extension={Extension}, contentType={ContentType}", fileName, extension, contentType);

                if (extension == ".txt" || extension == ".html" || extension == ".js" || extension == ".css" ||
                    contentType.Contains("javascript", StringComparison.OrdinalIgnoreCase) ||
                    contentType.Contains("text/", StringComparison.OrdinalIgnoreCase))
                {
                    using var reader = new StreamReader(fileStream, leaveOpen: true);
                    var content = await reader.ReadToEndAsync();

                    _logger.LogInformation("Scanning content: {Content}", content.Length > 100 ? content[..100] + "..." : content);

                    var suspiciousPatterns = new[]
                    {
                        "eval(", "document.write(", "innerHTML", "outerHTML", "javascript:",
                        "vbscript:", "onload=", "onerror=", "onclick=", "<script", "alert(",
                        "confirm(", "prompt(", "window.open(", "location.href"
                    };

                    foreach (var pattern in suspiciousPatterns)
                    {
                        if (content.Contains(pattern, StringComparison.OrdinalIgnoreCase))
                        {
                            _logger.LogWarning("Found suspicious pattern: {Pattern} in file {FileName}", pattern, fileName);
                            threats.Add(new ThreatInfo
                            {
                                ThreatName = $"Suspicious content pattern: {pattern}",
                                Severity = ThreatSeverity.Medium,
                                Description = "File contains potentially malicious content patterns",
                                Action = "Flag for review",
                                Details = new Dictionary<string, object>
                                {
                                    { "Pattern", pattern },
                                    { "FileType", extension }
                                }
                            });
                        }
                    }
                }

                return threats;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error scanning file content for {FileName}", fileName);
                return threats;
            }
        }

        private string GetContentTypeFromExtension(string extension)
        {
            return extension.ToLowerInvariant() switch
            {
                ".txt" => "text/plain",
                ".html" => "text/html",
                ".css" => "text/css",
                ".js" => "text/javascript",
                ".json" => "application/json",
                ".xml" => "application/xml",
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".gif" => "image/gif",
                ".pdf" => "application/pdf",
                ".zip" => "application/zip",
                ".doc" => "application/msword",
                ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                ".xls" => "application/vnd.ms-excel",
                ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                _ => "application/octet-stream"
            };
        }
    }
}