using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SkillLedger.Core.DTOs;
using SkillLedger.Core.Interfaces;
using SkillLedger.Infrastructure.Configuration;
using System.Security.Cryptography;
using System.Text;

namespace SkillLedger.Infrastructure.Services
{
    /// <summary>
    /// Local file system implementation of file storage service
    /// Provides abstraction for future Azure Blob Storage migration
    /// </summary>
    public class LocalFileStorageService : IFileStorageService
    {
        private readonly ILogger<LocalFileStorageService> _logger;
        private readonly string _baseStoragePath;
        private readonly MediaUploadConfiguration _config;

        public LocalFileStorageService(
            ILogger<LocalFileStorageService> logger,
            IOptions<MediaUploadConfiguration> config)
        {
            _logger = logger;
            _config = config.Value;

            // Use configured path or default to App_Data/uploads
            _baseStoragePath = _config.LocalStorageBasePath ??
                Path.Combine(Directory.GetCurrentDirectory(), "App_Data", "uploads");

            // Ensure base directory exists
            Directory.CreateDirectory(_baseStoragePath);
        }

        public async Task<FileStorageResult> UploadFileAsync(FileStorageUploadRequest request)
        {
            var result = new FileStorageResult();

            try
            {
                // BUG-HIGH-011 FIX: Validate file size to prevent disk space exhaustion
                if (request.FileSize > _config.MaxFileSizeBytes)
                {
                    _logger.LogWarning("File upload rejected: size {FileSize} exceeds max {MaxSize} bytes",
                        request.FileSize, _config.MaxFileSizeBytes);
                    result.Success = false;
                    result.ErrorMessage = $"File size ({request.FileSize:N0} bytes) exceeds maximum allowed size ({_config.MaxFileSizeBytes:N0} bytes)";
                    return result;
                }

                // SECURITY: Validate container path to prevent path traversal
                var sanitizedContainerPath = SanitizePath(request.ContainerPath);

                // Create container directory
                if (!TryGetSafeFullPath(sanitizedContainerPath, out var containerFullPath))
                {
                    _logger.LogWarning("SECURITY: Path traversal attempt detected. Container: {Container}, Resolved: {Resolved}",
                        request.ContainerPath, sanitizedContainerPath);
                    throw new UnauthorizedAccessException("Invalid container path");
                }

                Directory.CreateDirectory(containerFullPath);

                // Generate safe file path
                var safeFileName = SanitizeFileName(request.FileName);
                var filePath = Path.Combine(containerFullPath, safeFileName);
                var relativeFilePath = Path.Combine(sanitizedContainerPath, safeFileName);

                // Handle file existence
                if (File.Exists(filePath) && !request.OverwriteIfExists)
                {
                    // Generate unique filename with safety limit to prevent infinite loops
                    var nameWithoutExt = Path.GetFileNameWithoutExtension(safeFileName);
                    var extension = Path.GetExtension(safeFileName);
                    var counter = 1;
                    const int maxAttempts = 1000; // Prevent infinite loop in case of issues

                    do
                    {
                        safeFileName = $"{nameWithoutExt}_{counter}{extension}";
                        filePath = Path.Combine(containerFullPath, safeFileName);
                        relativeFilePath = Path.Combine(sanitizedContainerPath, safeFileName);
                        counter++;

                        if (counter > maxAttempts)
                        {
                            throw new InvalidOperationException($"Could not generate unique filename after {maxAttempts} attempts");
                        }
                    } while (File.Exists(filePath));
                }

                // Write file to disk
                long bytesWritten = 0;
                using (var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write))
                {
                    // BUG-HIGH-011 FIX: Track bytes written and validate against actual size
                    var buffer = new byte[8192];
                    int bytesRead;
                    while ((bytesRead = await request.FileStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                    {
                        bytesWritten += bytesRead;

                        // BUG-HIGH-011 FIX: Stop immediately if file exceeds max size
                        if (bytesWritten > _config.MaxFileSizeBytes)
                        {
                            _logger.LogWarning("File upload aborted: actual size {BytesWritten} exceeds max {MaxSize} bytes",
                                bytesWritten, _config.MaxFileSizeBytes);

                            // Delete partial file
                            fileStream.Close();
                            if (File.Exists(filePath))
                                File.Delete(filePath);

                            result.Success = false;
                            result.ErrorMessage = $"File size exceeds maximum allowed size ({_config.MaxFileSizeBytes:N0} bytes)";
                            return result;
                        }

                        await fileStream.WriteAsync(buffer, 0, bytesRead);
                    }
                }

                // Set file attributes for security
                var fileInfo = new FileInfo(filePath);

                // Store metadata as extended attributes (Windows) or separate metadata file
                await StoreFileMetadataAsync(filePath, request);

                result.Success = true;
                result.FilePath = relativeFilePath;
                result.Metadata = new FileStorageMetadata
                {
                    FilePath = relativeFilePath,
                    FileName = request.FileName,
                    ContentType = request.ContentType,
                    FileSize = request.FileSize,
                    CreatedAt = fileInfo.CreationTimeUtc,
                    LastModified = fileInfo.LastWriteTimeUtc,
                    ETag = GenerateETag(filePath),
                    CustomMetadata = request.Metadata
                };

                _logger.LogInformation("File uploaded successfully: {FilePath}", relativeFilePath);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading file {FileName} to {ContainerPath}",
                    request.FileName, request.ContainerPath);
                result.Success = false;
                result.ErrorMessage = ex.Message;
                return result;
            }
        }

        /// <summary>
        /// Downloads a file as a stream
        /// </summary>
        /// <param name="filePath">Relative path to the file</param>
        /// <returns>A FileStream that MUST be disposed by the caller using a 'using' statement</returns>
        /// <remarks>
        /// BUG-CRIT-006 FIX: Proper stream disposal documentation
        ///
        /// IMPORTANT: The returned stream must be disposed properly to avoid memory leaks and file handle exhaustion.
        ///
        /// CORRECT USAGE PATTERNS:
        /// 1. For ASP.NET controllers returning files:
        ///    return File(await DownloadFileAsync(path), contentType, fileName);
        ///    // ASP.NET's File() method automatically disposes the stream after sending
        ///
        /// 2. For processing file contents:
        ///    await using var stream = await DownloadFileAsync(path);
        ///    // Stream is automatically disposed at end of scope
        ///
        /// 3. For synchronous disposal:
        ///    using (var stream = await DownloadFileAsync(path)) { ... }
        ///
        /// INCORRECT: Forgetting to dispose will cause file handle leaks!
        ///    var stream = await DownloadFileAsync(path); // WRONG - no disposal
        /// </remarks>
        public Task<Stream?> DownloadFileAsync(string filePath)
        {
            try
            {
                if (!TryGetSafeFullPath(filePath, out var fullPath))
                {
                    _logger.LogWarning("Path traversal attempt detected: {FilePath}", filePath);
                    return Task.FromResult<Stream?>(null);
                }

                if (!File.Exists(fullPath))
                {
                    _logger.LogWarning("File not found: {FilePath}", filePath);
                    return Task.FromResult<Stream?>(null);
                }

                // Return file stream - CALLER MUST DISPOSE
                return Task.FromResult<Stream?>(new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading file: {FilePath}", filePath);
                return Task.FromResult<Stream?>(null);
            }
        }

        public Task<string?> GetSecureUrlAsync(string filePath, int expirationMinutes = 60, FileAccessPermission permission = FileAccessPermission.Read)
        {
            try
            {
                if (!TryGetSafeFullPath(filePath, out var fullPath))
                {
                    _logger.LogWarning("Path traversal attempt detected: {FilePath}", filePath);
                    return Task.FromResult<string?>(null);
                }

                if (!File.Exists(fullPath))
                    return Task.FromResult<string?>(null);

                // For local storage, generate a secure token-based URL
                var token = GenerateSecureAccessToken(filePath, expirationMinutes, permission);

                // In a real implementation, you would store this token in a cache/database
                // and validate it when the file is requested
                return Task.FromResult<string?>($"/api/files/secure/{Uri.EscapeDataString(filePath)}?token={token}&expires={DateTimeOffset.UtcNow.AddMinutes(expirationMinutes):O}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating secure URL for file: {FilePath}", filePath);
                return Task.FromResult<string?>(null);
            }
        }

        public Task<bool> DeleteFileAsync(string filePath)
        {
            try
            {
                if (!TryGetSafeFullPath(filePath, out var fullPath))
                {
                    _logger.LogWarning("Path traversal attempt detected: {FilePath}", filePath);
                    return Task.FromResult(false);
                }

                if (!File.Exists(fullPath))
                    return Task.FromResult(false);

                File.Delete(fullPath);

                // Delete metadata file if exists
                var metadataPath = GetMetadataFilePath(fullPath);
                if (File.Exists(metadataPath))
                    File.Delete(metadataPath);

                _logger.LogInformation("File deleted successfully: {FilePath}", filePath);
                return Task.FromResult(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting file: {FilePath}", filePath);
                return Task.FromResult(false);
            }
        }

        public Task<bool> FileExistsAsync(string filePath)
        {
            try
            {
                if (!TryGetSafeFullPath(filePath, out var fullPath))
                {
                    _logger.LogWarning("Path traversal attempt detected: {FilePath}", filePath);
                    return Task.FromResult(false);
                }

                return Task.FromResult(File.Exists(fullPath));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking file existence: {FilePath}", filePath);
                return Task.FromResult(false);
            }
        }

        public async Task<FileStorageMetadata?> GetFileMetadataAsync(string filePath)
        {
            try
            {
                if (!TryGetSafeFullPath(filePath, out var fullPath))
                {
                    _logger.LogWarning("Path traversal attempt detected: {FilePath}", filePath);
                    return null;
                }

                if (!File.Exists(fullPath))
                    return null;

                var fileInfo = new FileInfo(fullPath);
                var customMetadata = await LoadFileMetadataAsync(fullPath);

                return new FileStorageMetadata
                {
                    FilePath = filePath,
                    FileName = fileInfo.Name,
                    FileSize = fileInfo.Length,
                    CreatedAt = fileInfo.CreationTimeUtc,
                    LastModified = fileInfo.LastWriteTimeUtc,
                    ETag = GenerateETag(fullPath),
                    CustomMetadata = customMetadata
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting file metadata: {FilePath}", filePath);
                return null;
            }
        }

        public Task<bool> CopyFileAsync(string sourcePath, string destinationPath)
        {
            try
            {
                if (!TryGetSafeFullPath(sourcePath, out var sourceFullPath) ||
                    !TryGetSafeFullPath(destinationPath, out var destFullPath))
                {
                    _logger.LogWarning("Path traversal attempt detected while copying from {SourcePath} to {DestinationPath}",
                        sourcePath, destinationPath);
                    return Task.FromResult(false);
                }

                if (!File.Exists(sourceFullPath))
                    return Task.FromResult(false);

                // Create destination directory if needed
                var destDir = Path.GetDirectoryName(destFullPath);
                if (!string.IsNullOrEmpty(destDir))
                    Directory.CreateDirectory(destDir);

                File.Copy(sourceFullPath, destFullPath, true);

                // Copy metadata
                var sourceMetadataPath = GetMetadataFilePath(sourceFullPath);
                var destMetadataPath = GetMetadataFilePath(destFullPath);

                if (File.Exists(sourceMetadataPath))
                    File.Copy(sourceMetadataPath, destMetadataPath, true);

                return Task.FromResult(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error copying file from {SourcePath} to {DestinationPath}", sourcePath, destinationPath);
                return Task.FromResult(false);
            }
        }

        public Task<bool> MoveFileAsync(string sourcePath, string destinationPath)
        {
            try
            {
                if (!TryGetSafeFullPath(sourcePath, out var sourceFullPath) ||
                    !TryGetSafeFullPath(destinationPath, out var destFullPath))
                {
                    _logger.LogWarning("Path traversal attempt detected while moving from {SourcePath} to {DestinationPath}",
                        sourcePath, destinationPath);
                    return Task.FromResult(false);
                }

                if (!File.Exists(sourceFullPath))
                    return Task.FromResult(false);

                // Create destination directory if needed
                var destDir = Path.GetDirectoryName(destFullPath);
                if (!string.IsNullOrEmpty(destDir))
                    Directory.CreateDirectory(destDir);

                File.Move(sourceFullPath, destFullPath);

                // Move metadata
                var sourceMetadataPath = GetMetadataFilePath(sourceFullPath);
                var destMetadataPath = GetMetadataFilePath(destFullPath);

                if (File.Exists(sourceMetadataPath))
                    File.Move(sourceMetadataPath, destMetadataPath);

                return Task.FromResult(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error moving file from {SourcePath} to {DestinationPath}", sourcePath, destinationPath);
                return Task.FromResult(false);
            }
        }

        public Task<List<string>> ListFilesAsync(string containerPath, string? prefix = null)
        {
            try
            {
                if (!TryGetSafeFullPath(containerPath, out var containerFullPath) || !IsSafeSearchPrefix(prefix))
                {
                    _logger.LogWarning("Path traversal attempt detected while listing {ContainerPath} with prefix {Prefix}",
                        containerPath, prefix);
                    return Task.FromResult(new List<string>());
                }

                if (!Directory.Exists(containerFullPath))
                    return Task.FromResult(new List<string>());

                var searchPattern = string.IsNullOrEmpty(prefix) ? "*" : $"{prefix}*";
                var files = Directory.GetFiles(containerFullPath, searchPattern, SearchOption.TopDirectoryOnly)
                    .Where(f => !Path.GetFileName(f).EndsWith(".metadata")) // Exclude metadata files
                    .Select(f => Path.GetRelativePath(_baseStoragePath, f).Replace('\\', '/'))
                    .ToList();

                return Task.FromResult(files);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error listing files in container: {ContainerPath}", containerPath);
                return Task.FromResult(new List<string>());
            }
        }

        public Task<FileStorageStats> GetStorageStatsAsync(string containerPath)
        {
            try
            {
                if (!TryGetSafeFullPath(containerPath, out var containerFullPath))
                {
                    _logger.LogWarning("Path traversal attempt detected while reading storage stats for {ContainerPath}", containerPath);
                    return Task.FromResult(new FileStorageStats { ContainerPath = containerPath });
                }

                var stats = new FileStorageStats
                {
                    ContainerPath = containerPath
                };

                if (!Directory.Exists(containerFullPath))
                    return Task.FromResult(stats);

                var files = Directory.GetFiles(containerFullPath, "*", SearchOption.AllDirectories)
                    .Where(f => !Path.GetFileName(f).EndsWith(".metadata"));

                stats.FileCount = files.Count();
                stats.TotalSizeBytes = files.Sum(f => new FileInfo(f).Length);

                if (files.Any())
                {
                    stats.LastModified = files.Max(f => new FileInfo(f).LastWriteTimeUtc);

                    stats.FileTypeDistribution = files
                        .GroupBy(f => Path.GetExtension(f).ToLowerInvariant())
                        .ToDictionary(g => g.Key, g => g.Count());
                }

                return Task.FromResult(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting storage stats for container: {ContainerPath}", containerPath);
                return Task.FromResult(new FileStorageStats { ContainerPath = containerPath });
            }
        }

        public Task<FileStoragePreviewResult> GeneratePreviewAsync(string filePath, FilePreviewOptions previewOptions)
        {
            var result = new FileStoragePreviewResult();

            try
            {
                if (!TryGetSafeFullPath(filePath, out var fullPath))
                {
                    _logger.LogWarning("Path traversal attempt detected: {FilePath}", filePath);
                    result.ErrorMessage = "Invalid file path";
                    return Task.FromResult(result);
                }

                if (!File.Exists(fullPath))
                {
                    result.ErrorMessage = "Source file not found";
                    return Task.FromResult(result);
                }

                // For now, return success without actually generating previews
                // In a real implementation, you would use libraries like ImageSharp for images
                // and other tools for document previews
                result.Success = true;
                result.PreviewPaths = new Dictionary<string, string>();

                if (previewOptions.GenerateThumbnail)
                {
                    var thumbnailPath = $"{filePath}_thumbnail.jpg";
                    result.PreviewPaths["thumbnail"] = thumbnailPath;
                }

                if (previewOptions.GeneratePreview)
                {
                    var previewPath = $"{filePath}_preview.jpg";
                    result.PreviewPaths["preview"] = previewPath;
                }

                return Task.FromResult(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating preview for file: {FilePath}", filePath);
                result.ErrorMessage = ex.Message;
                return Task.FromResult(result);
            }
        }

        /// <summary>
        /// Validates that the resolved path is within the base storage directory
        /// Prevents path traversal attacks (e.g., ../../etc/passwd)
        /// </summary>
        private bool TryGetSafeFullPath(string relativePath, out string fullPath)
        {
            fullPath = string.Empty;
            try
            {
                if (string.IsNullOrWhiteSpace(relativePath))
                    relativePath = string.Empty;

                if (Path.IsPathFullyQualified(relativePath))
                    return false;

                var normalizedBasePath = Path.GetFullPath(_baseStoragePath);
                var baseWithSeparator = EnsureTrailingDirectorySeparator(normalizedBasePath);
                var candidate = Path.GetFullPath(Path.Combine(normalizedBasePath, relativePath));

                if (!candidate.Equals(normalizedBasePath, StringComparison.OrdinalIgnoreCase) &&
                    !candidate.StartsWith(baseWithSeparator, StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }

                if (ContainsReparsePoint(candidate, normalizedBasePath))
                    return false;

                fullPath = candidate;
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static string EnsureTrailingDirectorySeparator(string path)
        {
            return Path.EndsInDirectorySeparator(path) ? path : path + Path.DirectorySeparatorChar;
        }

        private static bool ContainsReparsePoint(string candidatePath, string basePath)
        {
            if ((File.Exists(candidatePath) || Directory.Exists(candidatePath)) &&
                (File.GetAttributes(candidatePath) & FileAttributes.ReparsePoint) == FileAttributes.ReparsePoint)
            {
                return true;
            }

            var current = Directory.Exists(candidatePath)
                ? candidatePath
                : Path.GetDirectoryName(candidatePath);

            while (!string.IsNullOrEmpty(current))
            {
                if (File.Exists(current) || Directory.Exists(current))
                {
                    var attributes = File.GetAttributes(current);
                    if ((attributes & FileAttributes.ReparsePoint) == FileAttributes.ReparsePoint)
                        return true;
                }

                if (string.Equals(Path.GetFullPath(current), Path.GetFullPath(basePath), StringComparison.OrdinalIgnoreCase))
                    break;

                current = Path.GetDirectoryName(current);
            }

            return false;
        }

        private static bool IsSafeSearchPrefix(string? prefix)
        {
            if (string.IsNullOrEmpty(prefix))
                return true;

            return !prefix.Contains("..", StringComparison.Ordinal) &&
                   !prefix.Contains('/', StringComparison.Ordinal) &&
                   !prefix.Contains('\\', StringComparison.Ordinal) &&
                   prefix.IndexOfAny(Path.GetInvalidFileNameChars()) < 0 &&
                   prefix.IndexOfAny(new[] { '*', '?' }) < 0;
        }

        /// <summary>
        /// SECURITY: Sanitizes a file path to prevent path traversal attacks
        /// </summary>
        private string SanitizePath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return string.Empty;

            // Remove dangerous path components
            var sanitized = path.Replace("..", "")
                                .Replace("~", "")
                                .Trim()
                                .TrimStart('/', '\\');

            // Normalize path separators
            sanitized = sanitized.Replace('\\', '/');

            // Remove any double slashes
            while (sanitized.Contains("//"))
                sanitized = sanitized.Replace("//", "/");

            return sanitized;
        }

        private string SanitizeFileName(string fileName)
        {
            // SECURITY: Remove path traversal attempts and invalid characters
            var invalidChars = Path.GetInvalidFileNameChars();
            var sanitized = new string(fileName.Where(c => !invalidChars.Contains(c)).ToArray());

            // Remove path separators and traversal sequences
            sanitized = sanitized.Replace("..", "").Replace("/", "").Replace("\\", "");

            return string.IsNullOrWhiteSpace(sanitized) ? "file" : sanitized;
        }

        private string GenerateETag(string filePath)
        {
            // BUG-CRITICAL-003 FIX: Replace MD5 with SHA256 for file integrity
            // MD5 is cryptographically broken; use SHA256 for better security and FIPS compliance
            using var sha256 = SHA256.Create();
            var fileInfo = new FileInfo(filePath);
            var hashInput = $"{filePath}_{fileInfo.LastWriteTimeUtc:O}_{fileInfo.Length}";
            var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(hashInput));
            return Convert.ToHexString(hashBytes);
        }

        private string GenerateSecureAccessToken(string filePath, int expirationMinutes, FileAccessPermission permission)
        {
            // SECURITY FIX: Throw exception if SecurityKey is not configured instead of using insecure fallback
            if (string.IsNullOrEmpty(_config.SecurityKey))
            {
                throw new InvalidOperationException("FileStorage:SecurityKey must be configured in application settings for secure file access token generation.");
            }

            var payload = $"{filePath}|{DateTime.UtcNow.AddMinutes(expirationMinutes):O}|{permission}";
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_config.SecurityKey));
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
            return Convert.ToBase64String(hash);
        }

        private string GetMetadataFilePath(string filePath)
        {
            return $"{filePath}.metadata";
        }

        private async Task StoreFileMetadataAsync(string filePath, FileStorageUploadRequest request)
        {
            try
            {
                var metadataPath = GetMetadataFilePath(filePath);
                var metadata = new
                {
                    OriginalFileName = request.FileName,
                    ContentType = request.ContentType,
                    FileSize = request.FileSize,
                    UploadedAt = DateTime.UtcNow,
                    CustomMetadata = request.Metadata
                };

                var json = System.Text.Json.JsonSerializer.Serialize(metadata);
                await File.WriteAllTextAsync(metadataPath, json);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error storing metadata for file: {FilePath}", filePath);
            }
        }

        private async Task<Dictionary<string, string>> LoadFileMetadataAsync(string filePath)
        {
            try
            {
                var metadataPath = GetMetadataFilePath(filePath);

                if (!File.Exists(metadataPath))
                    return new Dictionary<string, string>();

                var json = await File.ReadAllTextAsync(metadataPath);

                // SECURITY FIX: Use strongly-typed deserialization instead of dynamic
                // to prevent potential deserialization vulnerabilities
                var metadata = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, System.Text.Json.JsonElement>>(json);

                // Extract custom metadata if available
                if (metadata != null && metadata.ContainsKey("CustomMetadata"))
                {
                    var customMeta = metadata["CustomMetadata"];
                    if (customMeta.ValueKind == System.Text.Json.JsonValueKind.Object)
                    {
                        var result = new Dictionary<string, string>();
                        foreach (var prop in customMeta.EnumerateObject())
                        {
                            result[prop.Name] = prop.Value.ToString();
                        }
                        return result;
                    }
                }

                return new Dictionary<string, string>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading metadata for file: {FilePath}", filePath);
                return new Dictionary<string, string>();
            }
        }
    }
}
