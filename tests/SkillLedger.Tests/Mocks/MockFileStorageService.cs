using SkillLedger.Core.Interfaces;
using System.Collections.Concurrent;

namespace SkillLedger.Tests.Mocks;

public class MockFileStorageService : IFileStorageService
{
    private readonly ConcurrentDictionary<string, MockStoredFile> _storage = new();

    public Task<FileStorageResult> UploadFileAsync(FileStorageUploadRequest request)
    {
        using var memoryStream = new MemoryStream();
        request.FileStream.CopyTo(memoryStream);
        var fileData = memoryStream.ToArray();

        var filePath = $"{request.ContainerPath}/{request.FileName}";

        if (!request.OverwriteIfExists && _storage.ContainsKey(filePath))
        {
            return Task.FromResult(new FileStorageResult
            {
                Success = false,
                ErrorMessage = "File already exists"
            });
        }

        var storedFile = new MockStoredFile
        {
            Data = fileData,
            ContentType = request.ContentType,
            FileSize = request.FileSize,
            FileName = request.FileName,
            CreatedAt = DateTime.UtcNow,
            LastModified = DateTime.UtcNow,
            Metadata = request.Metadata
        };

        _storage[filePath] = storedFile;

        return Task.FromResult(new FileStorageResult
        {
            Success = true,
            FilePath = filePath,
            PublicUrl = $"https://mock-storage.test/{filePath}",
            Metadata = new FileStorageMetadata
            {
                FilePath = filePath,
                FileName = request.FileName,
                ContentType = request.ContentType,
                FileSize = request.FileSize,
                CreatedAt = storedFile.CreatedAt,
                LastModified = storedFile.LastModified,
                CustomMetadata = request.Metadata
            }
        });
    }

    public Task<Stream?> DownloadFileAsync(string filePath)
    {
        if (_storage.TryGetValue(filePath, out var file))
        {
            return Task.FromResult<Stream?>(new MemoryStream(file.Data));
        }
        return Task.FromResult<Stream?>(null);
    }

    public Task<string?> GetSecureUrlAsync(string filePath, int expirationMinutes = 60, FileAccessPermission permission = FileAccessPermission.Read)
    {
        if (_storage.ContainsKey(filePath))
        {
            return Task.FromResult<string?>($"https://mock-storage.test/{filePath}?expires={expirationMinutes}");
        }
        return Task.FromResult<string?>(null);
    }

    public Task<bool> DeleteFileAsync(string filePath)
    {
        return Task.FromResult(_storage.TryRemove(filePath, out _));
    }

    public Task<bool> FileExistsAsync(string filePath)
    {
        return Task.FromResult(_storage.ContainsKey(filePath));
    }

    public Task<FileStorageMetadata?> GetFileMetadataAsync(string filePath)
    {
        if (_storage.TryGetValue(filePath, out var file))
        {
            return Task.FromResult<FileStorageMetadata?>(new FileStorageMetadata
            {
                FilePath = filePath,
                FileName = file.FileName,
                ContentType = file.ContentType,
                FileSize = file.FileSize,
                CreatedAt = file.CreatedAt,
                LastModified = file.LastModified,
                CustomMetadata = file.Metadata
            });
        }
        return Task.FromResult<FileStorageMetadata?>(null);
    }

    public Task<bool> CopyFileAsync(string sourcePath, string destinationPath)
    {
        if (_storage.TryGetValue(sourcePath, out var sourceFile))
        {
            var copy = new MockStoredFile
            {
                Data = (byte[])sourceFile.Data.Clone(),
                ContentType = sourceFile.ContentType,
                FileSize = sourceFile.FileSize,
                FileName = sourceFile.FileName,
                CreatedAt = DateTime.UtcNow,
                LastModified = DateTime.UtcNow,
                Metadata = new Dictionary<string, string>(sourceFile.Metadata)
            };
            _storage[destinationPath] = copy;
            return Task.FromResult(true);
        }
        return Task.FromResult(false);
    }

    public Task<bool> MoveFileAsync(string sourcePath, string destinationPath)
    {
        if (_storage.TryGetValue(sourcePath, out var file))
        {
            _storage[destinationPath] = file;
            _storage.TryRemove(sourcePath, out _);
            return Task.FromResult(true);
        }
        return Task.FromResult(false);
    }

    public Task<List<string>> ListFilesAsync(string containerPath, string? prefix = null)
    {
        var files = _storage.Keys
            .Where(k => k.StartsWith(containerPath))
            .Where(k => string.IsNullOrEmpty(prefix) || k.Contains(prefix))
            .ToList();
        return Task.FromResult(files);
    }

    public Task<FileStorageStats> GetStorageStatsAsync(string containerPath)
    {
        var files = _storage
            .Where(kvp => kvp.Key.StartsWith(containerPath))
            .ToList();

        var stats = new FileStorageStats
        {
            ContainerPath = containerPath,
            FileCount = files.Count,
            TotalSizeBytes = files.Sum(f => f.Value.FileSize),
            LastModified = files.Any() ? files.Max(f => f.Value.LastModified) : DateTime.MinValue
        };

        return Task.FromResult(stats);
    }

    public Task<FileStoragePreviewResult> GeneratePreviewAsync(string filePath, FilePreviewOptions previewOptions)
    {
        if (_storage.ContainsKey(filePath))
        {
            return Task.FromResult(new FileStoragePreviewResult
            {
                Success = true,
                PreviewPaths = new Dictionary<string, string>
                {
                    ["thumbnail"] = $"{filePath}_thumb.jpg",
                    ["preview"] = $"{filePath}_preview.jpg"
                }
            });
        }

        return Task.FromResult(new FileStoragePreviewResult
        {
            Success = false,
            ErrorMessage = "File not found"
        });
    }

    private class MockStoredFile
    {
        public byte[] Data { get; set; } = Array.Empty<byte>();
        public string ContentType { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public string FileName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime LastModified { get; set; }
        public Dictionary<string, string> Metadata { get; set; } = new();
    }
}
