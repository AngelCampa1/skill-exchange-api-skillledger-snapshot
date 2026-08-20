using SkillLedger.Tests.Infrastructure;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SkillLedger.Core.DTOs;
using SkillLedger.Core.Interfaces;
using SkillLedger.Infrastructure.Configuration;
using SkillLedger.Infrastructure.Services;
using System.Text;
using Xunit;

namespace SkillLedger.Tests.Integration
{
    /// <summary>
    /// Integration tests for file storage operations
    /// Tests the LocalFileStorageService implementation
    /// </summary>
    [Collection("Integration Other")]
    [IntegrationTest]
    [StorageTest]
    public class FileStorageIntegrationTests : IDisposable
    {
        private readonly IFileStorageService _fileStorageService;
        private readonly string _testStoragePath;
        private readonly MediaUploadConfiguration _config;

        public FileStorageIntegrationTests()
        {
            _testStoragePath = Path.Combine(Path.GetTempPath(), "SkillLedgerTests", Guid.NewGuid().ToString());
            Directory.CreateDirectory(_testStoragePath);

            _config = new MediaUploadConfiguration
            {
                LocalStorageBasePath = _testStoragePath,
                SecurityKey = "test-security-key-for-testing-only",
                MaxFileSizeBytes = 10 * 1024 * 1024 // 10MB
            };

            var configOptions = Options.Create(_config);
            var logger = new LoggerFactory().CreateLogger<LocalFileStorageService>();

            _fileStorageService = new LocalFileStorageService(logger, configOptions);
        }

        [Fact]
        [FastTest]
        public async Task UploadFileAsync_WithValidFile_ShouldSucceed()
        {
            // Arrange
            var testContent = "This is a test file content for upload testing.";
            var testBytes = Encoding.UTF8.GetBytes(testContent);
            var fileStream = new MemoryStream(testBytes);

            var uploadRequest = new FileStorageUploadRequest
            {
                FileName = "test-document.txt",
                FileStream = fileStream,
                ContentType = "text/plain",
                FileSize = testBytes.Length,
                ContainerPath = "test-workspace/documents",
                Metadata = new Dictionary<string, string>
                {
                    ["uploadedBy"] = Guid.NewGuid().ToString(),
                    ["purpose"] = "unit-testing"
                }
            };

            // Act
            var result = await _fileStorageService.UploadFileAsync(uploadRequest);

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.FilePath);
            Assert.Contains("test-document.txt", result.FilePath);
            Assert.NotNull(result.Metadata);
            Assert.Equal("test-document.txt", result.Metadata.FileName);
            Assert.Equal("text/plain", result.Metadata.ContentType);
            Assert.Equal(testBytes.Length, result.Metadata.FileSize);

            // Verify file exists on disk
            var fullPath = Path.Combine(_testStoragePath, result.FilePath);
            Assert.True(File.Exists(fullPath));

            // Verify file content
            var savedContent = await File.ReadAllTextAsync(fullPath);
            Assert.Equal(testContent, savedContent);
        }

        [Fact]
        [FastTest]
        public async Task DownloadFileAsync_WithExistingFile_ShouldReturnStream()
        {
            // Arrange - First upload a file
            var testContent = "Download test content";
            var testBytes = Encoding.UTF8.GetBytes(testContent);
            var uploadStream = new MemoryStream(testBytes);

            var uploadRequest = new FileStorageUploadRequest
            {
                FileName = "download-test.txt",
                FileStream = uploadStream,
                ContentType = "text/plain",
                FileSize = testBytes.Length,
                ContainerPath = "test-downloads"
            };

            var uploadResult = await _fileStorageService.UploadFileAsync(uploadRequest);
            Assert.True(uploadResult.Success);

            // Act
            using var downloadStream = await _fileStorageService.DownloadFileAsync(uploadResult.FilePath!);

            // Assert
            Assert.NotNull(downloadStream);
            Assert.True(downloadStream.CanRead);

            using var reader = new StreamReader(downloadStream);
            var downloadedContent = await reader.ReadToEndAsync();
            Assert.Equal(testContent, downloadedContent);
        }

        [Fact]
        [FastTest]
        public async Task DownloadFileAsync_WithNonExistentFile_ShouldReturnNull()
        {
            // Act
            var result = await _fileStorageService.DownloadFileAsync("non-existent/file.txt");

            // Assert
            Assert.Null(result);
        }

        [Fact]
        [FastTest]
        public async Task FileExistsAsync_WithExistingFile_ShouldReturnTrue()
        {
            // Arrange - Upload a test file
            var testBytes = Encoding.UTF8.GetBytes("Existence test");
            var uploadStream = new MemoryStream(testBytes);

            var uploadRequest = new FileStorageUploadRequest
            {
                FileName = "existence-test.txt",
                FileStream = uploadStream,
                ContentType = "text/plain",
                FileSize = testBytes.Length,
                ContainerPath = "existence-tests"
            };

            var uploadResult = await _fileStorageService.UploadFileAsync(uploadRequest);

            // Act
            var exists = await _fileStorageService.FileExistsAsync(uploadResult.FilePath!);

            // Assert
            Assert.True(exists);
        }

        [Fact]
        [FastTest]
        public async Task FileExistsAsync_WithNonExistentFile_ShouldReturnFalse()
        {
            // Act
            var exists = await _fileStorageService.FileExistsAsync("non-existent/file.txt");

            // Assert
            Assert.False(exists);
        }

        [Fact]
        [FastTest]
        public async Task DeleteFileAsync_WithExistingFile_ShouldSucceed()
        {
            // Arrange - Upload a file to delete
            var testBytes = Encoding.UTF8.GetBytes("File to be deleted");
            var uploadStream = new MemoryStream(testBytes);

            var uploadRequest = new FileStorageUploadRequest
            {
                FileName = "delete-test.txt",
                FileStream = uploadStream,
                ContentType = "text/plain",
                FileSize = testBytes.Length,
                ContainerPath = "deletion-tests"
            };

            var uploadResult = await _fileStorageService.UploadFileAsync(uploadRequest);
            Assert.True(await _fileStorageService.FileExistsAsync(uploadResult.FilePath!));

            // Act
            var deleteResult = await _fileStorageService.DeleteFileAsync(uploadResult.FilePath!);

            // Assert
            Assert.True(deleteResult);
            Assert.False(await _fileStorageService.FileExistsAsync(uploadResult.FilePath!));
        }

        [Fact]
        [FastTest]
        public async Task GetFileMetadataAsync_WithExistingFile_ShouldReturnMetadata()
        {
            // Arrange
            var testBytes = Encoding.UTF8.GetBytes("Metadata test content");
            var uploadStream = new MemoryStream(testBytes);

            var uploadRequest = new FileStorageUploadRequest
            {
                FileName = "metadata-test.txt",
                FileStream = uploadStream,
                ContentType = "text/plain",
                FileSize = testBytes.Length,
                ContainerPath = "metadata-tests",
                Metadata = new Dictionary<string, string>
                {
                    ["testKey"] = "testValue",
                    ["purpose"] = "metadata-testing"
                }
            };

            var uploadResult = await _fileStorageService.UploadFileAsync(uploadRequest);

            // Act
            var metadata = await _fileStorageService.GetFileMetadataAsync(uploadResult.FilePath!);

            // Assert
            Assert.NotNull(metadata);
            Assert.Equal("metadata-test.txt", metadata.FileName);
            Assert.Equal(testBytes.Length, metadata.FileSize);
            Assert.NotNull(metadata.ETag);
            Assert.True(metadata.CreatedAt > DateTime.MinValue);
            Assert.True(metadata.LastModified > DateTime.MinValue);
        }

        [Fact]
        [FastTest]
        public async Task CopyFileAsync_WithExistingFile_ShouldSucceed()
        {
            // Arrange
            var testContent = "Content to be copied";
            var testBytes = Encoding.UTF8.GetBytes(testContent);
            var uploadStream = new MemoryStream(testBytes);

            var uploadRequest = new FileStorageUploadRequest
            {
                FileName = "source-file.txt",
                FileStream = uploadStream,
                ContentType = "text/plain",
                FileSize = testBytes.Length,
                ContainerPath = "copy-tests"
            };

            var uploadResult = await _fileStorageService.UploadFileAsync(uploadRequest);
            var destinationPath = "copy-tests/destination-file.txt";

            // Act
            var copyResult = await _fileStorageService.CopyFileAsync(uploadResult.FilePath!, destinationPath);

            // Assert
            Assert.True(copyResult);
            Assert.True(await _fileStorageService.FileExistsAsync(uploadResult.FilePath!));
            Assert.True(await _fileStorageService.FileExistsAsync(destinationPath));

            // Verify content is identical
            using var originalStream = await _fileStorageService.DownloadFileAsync(uploadResult.FilePath!);
            using var copiedStream = await _fileStorageService.DownloadFileAsync(destinationPath);

            var originalContent = await new StreamReader(originalStream!).ReadToEndAsync();
            var copiedContent = await new StreamReader(copiedStream!).ReadToEndAsync();

            Assert.Equal(originalContent, copiedContent);
        }

        [Fact]
        [FastTest]
        public async Task MoveFileAsync_WithExistingFile_ShouldSucceed()
        {
            // Arrange
            var testContent = "Content to be moved";
            var testBytes = Encoding.UTF8.GetBytes(testContent);
            var uploadStream = new MemoryStream(testBytes);

            var uploadRequest = new FileStorageUploadRequest
            {
                FileName = "move-source.txt",
                FileStream = uploadStream,
                ContentType = "text/plain",
                FileSize = testBytes.Length,
                ContainerPath = "move-tests"
            };

            var uploadResult = await _fileStorageService.UploadFileAsync(uploadRequest);
            var destinationPath = "move-tests/moved-file.txt";

            // Act
            var moveResult = await _fileStorageService.MoveFileAsync(uploadResult.FilePath!, destinationPath);

            // Assert
            Assert.True(moveResult);
            Assert.False(await _fileStorageService.FileExistsAsync(uploadResult.FilePath!));
            Assert.True(await _fileStorageService.FileExistsAsync(destinationPath));

            // Verify moved content
            using var movedStream = await _fileStorageService.DownloadFileAsync(destinationPath);
            var movedContent = await new StreamReader(movedStream!).ReadToEndAsync();
            Assert.Equal(testContent, movedContent);
        }

        [Fact]
        [SecurityTest]
        public async Task CopyMoveListStats_WithTraversalPaths_ShouldStayInsideStorageRoot()
        {
            var testBytes = Encoding.UTF8.GetBytes("Sensitive test content");
            var uploadResult = await _fileStorageService.UploadFileAsync(new FileStorageUploadRequest
            {
                FileName = "source.txt",
                FileStream = new MemoryStream(testBytes),
                ContentType = "text/plain",
                FileSize = testBytes.Length,
                ContainerPath = "safe"
            });

            Assert.True(uploadResult.Success);

            var outsideDirectory = Path.Combine(Path.GetDirectoryName(_testStoragePath)!, $"{Path.GetFileName(_testStoragePath)}-outside");
            Directory.CreateDirectory(outsideDirectory);
            var traversalDestination = Path.Combine("..", Path.GetFileName(outsideDirectory), "copied.txt");

            var copyResult = await _fileStorageService.CopyFileAsync(uploadResult.FilePath!, traversalDestination);
            var moveResult = await _fileStorageService.MoveFileAsync(uploadResult.FilePath!, traversalDestination);
            var listedFiles = await _fileStorageService.ListFilesAsync(Path.Combine("..", Path.GetFileName(outsideDirectory)));
            var stats = await _fileStorageService.GetStorageStatsAsync(Path.Combine("..", Path.GetFileName(outsideDirectory)));

            Assert.False(copyResult);
            Assert.False(moveResult);
            Assert.Empty(listedFiles);
            Assert.Equal(0, stats.FileCount);
            Assert.False(File.Exists(Path.Combine(outsideDirectory, "copied.txt")));
        }

        [Fact]
        [SecurityTest]
        public async Task DownloadFileAsync_WithSiblingPrefixTraversal_ShouldReturnNull()
        {
            var siblingPath = $"{_testStoragePath}_evil";
            Directory.CreateDirectory(siblingPath);
            var secretPath = Path.Combine(siblingPath, "secret.txt");
            await File.WriteAllTextAsync(secretPath, "outside storage root");

            var traversalPath = Path.Combine("..", Path.GetFileName(siblingPath), "secret.txt");

            using var stream = await _fileStorageService.DownloadFileAsync(traversalPath);

            Assert.Null(stream);
        }

        [Fact]
        [SlowTest]
        public async Task ListFilesAsync_WithFilesInContainer_ShouldReturnFileList()
        {
            // Arrange
            var containerPath = "list-tests";
            var fileNames = new[] { "file1.txt", "file2.pdf", "file3.docx" };

            foreach (var fileName in fileNames)
            {
                var testBytes = Encoding.UTF8.GetBytes($"Content of {fileName}");
                var uploadStream = new MemoryStream(testBytes);

                var uploadRequest = new FileStorageUploadRequest
                {
                    FileName = fileName,
                    FileStream = uploadStream,
                    ContentType = "text/plain",
                    FileSize = testBytes.Length,
                    ContainerPath = containerPath
                };

                await _fileStorageService.UploadFileAsync(uploadRequest);
            }

            // Act
            var fileList = await _fileStorageService.ListFilesAsync(containerPath);

            // Assert
            Assert.Equal(3, fileList.Count);
            Assert.All(fileNames, fileName =>
                Assert.Contains(fileList, f => f.Contains(fileName)));
        }

        [Fact]
        [SlowTest]
        public async Task GetStorageStatsAsync_WithFilesInContainer_ShouldReturnStats()
        {
            // Arrange
            var containerPath = "stats-tests";
            var files = new[]
            {
                ("small.txt", "Small file content"),
                ("medium.txt", "Medium file with more content than the small one"),
                ("large.txt", new string('X', 1000))
            };

            foreach (var (fileName, content) in files)
            {
                var testBytes = Encoding.UTF8.GetBytes(content);
                var uploadStream = new MemoryStream(testBytes);

                var uploadRequest = new FileStorageUploadRequest
                {
                    FileName = fileName,
                    FileStream = uploadStream,
                    ContentType = "text/plain",
                    FileSize = testBytes.Length,
                    ContainerPath = containerPath
                };

                await _fileStorageService.UploadFileAsync(uploadRequest);
            }

            // Act
            var stats = await _fileStorageService.GetStorageStatsAsync(containerPath);

            // Assert
            Assert.Equal(containerPath, stats.ContainerPath);
            Assert.Equal(3, stats.FileCount);
            Assert.True(stats.TotalSizeBytes > 0);
            Assert.True(stats.LastModified > DateTime.MinValue);
            Assert.Contains(".txt", stats.FileTypeDistribution.Keys);
        }

        [Fact]
        [SecurityTest]
        public async Task GetSecureUrlAsync_WithExistingFile_ShouldReturnUrl()
        {
            // Arrange
            var testBytes = Encoding.UTF8.GetBytes("Secure URL test");
            var uploadStream = new MemoryStream(testBytes);

            var uploadRequest = new FileStorageUploadRequest
            {
                FileName = "secure-test.txt",
                FileStream = uploadStream,
                ContentType = "text/plain",
                FileSize = testBytes.Length,
                ContainerPath = "secure-tests"
            };

            var uploadResult = await _fileStorageService.UploadFileAsync(uploadRequest);

            // Act
            var secureUrl = await _fileStorageService.GetSecureUrlAsync(uploadResult.FilePath!, 60, FileAccessPermission.Read);

            // Assert
            Assert.NotNull(secureUrl);
            Assert.Contains("/api/files/secure/", secureUrl);
            Assert.Contains("token=", secureUrl);
            Assert.Contains("expires=", secureUrl);
        }

        public void Dispose()
        {
            try
            {
                if (Directory.Exists(_testStoragePath))
                {
                    Directory.Delete(_testStoragePath, true);
                }
            }
            catch (Exception)
            {
                // Ignore cleanup errors in tests
            }
        }
    }
}
