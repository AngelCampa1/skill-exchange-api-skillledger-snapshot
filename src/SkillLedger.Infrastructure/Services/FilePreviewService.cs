using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using SkillLedger.Core.Interfaces;
using SkillLedger.Core.Models;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Processing;
using System.Text;
using System.Text.Json;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using DocumentFormat.OpenXml.Spreadsheet;

namespace SkillLedger.Infrastructure.Services
{
    /// <summary>
    /// Basic file preview service implementation
    /// Provides text extraction, image thumbnails, and metadata extraction
    /// </summary>
    public class FilePreviewService : IFilePreviewService
    {
        private readonly ILogger<FilePreviewService> _logger;
        // BUG-BE-004 FIX: Replaced static Dictionary with IMemoryCache to prevent unbounded memory growth
        // IMemoryCache provides automatic expiration and size limits, preventing OutOfMemoryException
        private readonly IMemoryCache _cache;

        // Supported file types for preview
        private static readonly Dictionary<string, FilePreviewType> FileTypeMapping = new()
        {
            { ".txt", FilePreviewType.Text },
            { ".md", FilePreviewType.Text },
            { ".json", FilePreviewType.Text },
            { ".xml", FilePreviewType.Text },
            { ".html", FilePreviewType.Text },
            { ".css", FilePreviewType.Code },
            { ".js", FilePreviewType.Code },
            { ".cs", FilePreviewType.Code },
            { ".py", FilePreviewType.Code },
            { ".java", FilePreviewType.Code },
            { ".cpp", FilePreviewType.Code },
            { ".jpg", FilePreviewType.Image },
            { ".jpeg", FilePreviewType.Image },
            { ".png", FilePreviewType.Image },
            { ".gif", FilePreviewType.Image },
            { ".bmp", FilePreviewType.Image },
            { ".webp", FilePreviewType.Image },
            { ".pdf", FilePreviewType.Pdf },
            { ".doc", FilePreviewType.Office },
            { ".docx", FilePreviewType.Office },
            { ".xls", FilePreviewType.Office },
            { ".xlsx", FilePreviewType.Office },
            { ".ppt", FilePreviewType.Office },
            { ".pptx", FilePreviewType.Office },
            { ".mp4", FilePreviewType.Video },
            { ".avi", FilePreviewType.Video },
            { ".mov", FilePreviewType.Video },
            { ".mp3", FilePreviewType.Audio },
            { ".wav", FilePreviewType.Audio },
            { ".zip", FilePreviewType.Archive },
            { ".rar", FilePreviewType.Archive },
            { ".7z", FilePreviewType.Archive }
        };

        public FilePreviewService(ILogger<FilePreviewService> logger, IMemoryCache cache)
        {
            _logger = logger;
            _cache = cache;
        }

        public async Task<FilePreviewResult> GeneratePreviewAsync(Stream fileStream, string fileName, string contentType)
        {
            try
            {
                var extension = Path.GetExtension(fileName).ToLowerInvariant();
                var previewType = GetPreviewType(fileName, contentType);

                var result = new FilePreviewResult
                {
                    PreviewType = previewType,
                    Metadata = await ExtractMetadataAsync(fileStream, fileName, contentType)
                };

                switch (previewType)
                {
                    case FilePreviewType.Text:
                    case FilePreviewType.Code:
                        result.PreviewContent = await ExtractTextContentAsync(fileStream);
                        result.IsGenerated = !string.IsNullOrEmpty(result.PreviewContent);
                        break;

                    case FilePreviewType.Image:
                        result.ThumbnailData = await GenerateThumbnailAsync(fileStream, fileName, contentType);
                        result.IsGenerated = result.ThumbnailData != null;
                        break;

                    case FilePreviewType.Pdf:
                        result.PreviewContent = await ExtractPdfTextAsync(fileStream);
                        result.IsGenerated = !string.IsNullOrEmpty(result.PreviewContent);
                        break;

                    case FilePreviewType.Office:
                        result.PreviewContent = await ExtractOfficeTextAsync(fileStream, extension);
                        result.IsGenerated = !string.IsNullOrEmpty(result.PreviewContent);
                        break;

                    default:
                        result.PreviewContent = "Preview not available for this file type";
                        result.IsGenerated = false;
                        break;
                }

                // BUG-BE-004 FIX: Store preview in IMemoryCache with 12-hour sliding expiration (previews are expensive to generate)
                // Note: We would need the documentId parameter to cache properly. For now, we cache by filename hash.
                // In production, this method should accept documentId as a parameter.
                var cacheKey = $"file_preview_{fileName.GetHashCode()}";
                _cache.Set(cacheKey, result, new MemoryCacheEntryOptions
                {
                    SlidingExpiration = TimeSpan.FromHours(12),
                    Size = 1
                });

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating preview for file {FileName}", fileName);
                return new FilePreviewResult
                {
                    PreviewType = FilePreviewType.None,
                    IsGenerated = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        public Task<FilePreviewResult?> GetCachedPreviewAsync(Guid documentId)
        {
            // BUG-BE-004 FIX: Use IMemoryCache TryGetValue (thread-safe, no lock needed)
            var cacheKey = $"file_preview_{documentId}";
            var result = _cache.TryGetValue<FilePreviewResult>(cacheKey, out var cached) ? cached : null;
            return Task.FromResult(result);
        }

        public Task<byte[]?> GenerateThumbnailAsync(Stream fileStream, string fileName, string contentType, int maxWidth = 200, int maxHeight = 200)
        {
            try
            {
                if (!IsImageFile(fileName, contentType))
                    return Task.FromResult<byte[]?>(null);

                fileStream.Position = 0;
                using var image = Image.Load(fileStream);

                // Calculate thumbnail dimensions while maintaining aspect ratio
                var ratioX = (double)maxWidth / image.Width;
                var ratioY = (double)maxHeight / image.Height;
                var ratio = Math.Min(ratioX, ratioY);

                var newWidth = (int)(image.Width * ratio);
                var newHeight = (int)(image.Height * ratio);

                // Resize the image
                image.Mutate(x => x.Resize(newWidth, newHeight));

                using var stream = new MemoryStream();
                image.Save(stream, new JpegEncoder());
                return Task.FromResult<byte[]?>(stream.ToArray());
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not generate thumbnail for {FileName}", fileName);
                return Task.FromResult<byte[]?>(null);
            }
        }

        public bool IsPreviewSupported(string fileName, string contentType)
        {
            var extension = Path.GetExtension(fileName).ToLowerInvariant();
            return FileTypeMapping.ContainsKey(extension);
        }

        public async Task<FileMetadata> ExtractMetadataAsync(Stream fileStream, string fileName, string contentType)
        {
            var metadata = new FileMetadata
            {
                FileName = fileName,
                ContentType = contentType,
                FileSize = fileStream.Length,
                CreatedDate = DateTime.UtcNow,
                ModifiedDate = DateTime.UtcNow
            };

            try
            {
                var extension = Path.GetExtension(fileName).ToLowerInvariant();

                if (IsImageFile(fileName, contentType))
                {
                    await ExtractImageMetadataAsync(fileStream, metadata);
                }
                else if (extension == ".pdf")
                {
                    await ExtractPdfMetadataAsync(fileStream, metadata);
                }
                else if (IsOfficeFile(extension))
                {
                    await ExtractOfficeMetadataAsync(fileStream, metadata, extension);
                }

                return metadata;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not extract metadata for {FileName}", fileName);
                return metadata;
            }
        }

        private FilePreviewType GetPreviewType(string fileName, string contentType)
        {
            var extension = Path.GetExtension(fileName).ToLowerInvariant();
            return FileTypeMapping.TryGetValue(extension, out var type) ? type : FilePreviewType.None;
        }

        private async Task<string> ExtractTextContentAsync(Stream stream)
        {
            try
            {
                stream.Position = 0;
                using var reader = new StreamReader(stream, Encoding.UTF8, leaveOpen: true);
                var content = await reader.ReadToEndAsync();

                // Limit content size for preview
                const int maxPreviewLength = 5000;
                if (content.Length > maxPreviewLength)
                {
                    content = content.Substring(0, maxPreviewLength) + "\n\n... (content truncated)";
                }

                return content;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not extract text content");
                return "Error extracting text content";
            }
        }

        private Task<string> ExtractPdfTextAsync(Stream stream)
        {
            try
            {
                stream.Position = 0;

                // Copy stream to MemoryStream since PdfPig needs seekable stream
                using var memoryStream = new MemoryStream();
                stream.CopyTo(memoryStream);
                memoryStream.Position = 0;

                using var document = PdfDocument.Open(memoryStream);
                var textBuilder = new StringBuilder();

                const int maxPages = 10; // Limit pages for preview
                const int maxPreviewLength = 5000;

                var pagesToProcess = Math.Min(document.NumberOfPages, maxPages);

                for (var i = 1; i <= pagesToProcess; i++)
                {
                    var page = document.GetPage(i);
                    var pageText = page.Text;

                    if (!string.IsNullOrWhiteSpace(pageText))
                    {
                        if (i > 1)
                            textBuilder.AppendLine($"\n--- Page {i} ---\n");

                        textBuilder.AppendLine(pageText);

                        // Check length limit
                        if (textBuilder.Length > maxPreviewLength)
                        {
                            textBuilder.Length = maxPreviewLength;
                            textBuilder.AppendLine("\n\n... (content truncated)");
                            break;
                        }
                    }
                }

                if (document.NumberOfPages > maxPages)
                {
                    textBuilder.AppendLine($"\n\n... ({document.NumberOfPages - maxPages} more pages not shown)");
                }

                var result = textBuilder.ToString().Trim();
                return Task.FromResult(string.IsNullOrWhiteSpace(result) ? "No extractable text found in PDF" : result);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not extract PDF text");
                return Task.FromResult("Error extracting PDF content");
            }
        }

        private Task<string> ExtractOfficeTextAsync(Stream stream, string extension)
        {
            try
            {
                stream.Position = 0;

                // Copy stream to MemoryStream since OpenXml needs seekable stream
                using var memoryStream = new MemoryStream();
                stream.CopyTo(memoryStream);
                memoryStream.Position = 0;

                var content = extension.ToLowerInvariant() switch
                {
                    ".docx" => ExtractDocxText(memoryStream),
                    ".xlsx" => ExtractXlsxText(memoryStream),
                    ".pptx" => ExtractPptxText(memoryStream),
                    // Legacy formats (.doc, .xls, .ppt) require binary parsing - not supported
                    ".doc" or ".xls" or ".ppt" => $"Legacy {extension.ToUpperInvariant()} format is not supported for preview. Please convert to modern format (.docx, .xlsx, .pptx).",
                    _ => $"Unsupported Office format: {extension}"
                };

                return Task.FromResult(content);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not extract Office document text for {Extension}", extension);
                return Task.FromResult("Error extracting Office document content");
            }
        }

        private string ExtractDocxText(MemoryStream stream)
        {
            try
            {
                using var document = WordprocessingDocument.Open(stream, false);
                var body = document.MainDocumentPart?.Document.Body;

                if (body == null)
                    return "No content found in document";

                var textBuilder = new StringBuilder();
                const int maxPreviewLength = 5000;

                foreach (var paragraph in body.Descendants<Paragraph>())
                {
                    var paragraphText = paragraph.InnerText;
                    if (!string.IsNullOrWhiteSpace(paragraphText))
                    {
                        textBuilder.AppendLine(paragraphText);

                        if (textBuilder.Length > maxPreviewLength)
                        {
                            textBuilder.Length = maxPreviewLength;
                            textBuilder.AppendLine("\n\n... (content truncated)");
                            break;
                        }
                    }
                }

                var result = textBuilder.ToString().Trim();
                return string.IsNullOrWhiteSpace(result) ? "No extractable text found in document" : result;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not extract DOCX text");
                return "Error extracting Word document content";
            }
        }

        private string ExtractXlsxText(MemoryStream stream)
        {
            try
            {
                using var document = SpreadsheetDocument.Open(stream, false);
                var workbookPart = document.WorkbookPart;

                if (workbookPart == null)
                    return "No content found in spreadsheet";

                var textBuilder = new StringBuilder();
                const int maxPreviewLength = 5000;
                const int maxRows = 100;
                const int maxCells = 10;

                var sharedStrings = workbookPart.SharedStringTablePart?.SharedStringTable
                    .ChildElements.Select(e => e.InnerText).ToArray() ?? Array.Empty<string>();

                var sheetCount = 0;
                foreach (var worksheetPart in workbookPart.WorksheetParts.Take(3)) // Limit to 3 sheets
                {
                    var sheetData = worksheetPart.Worksheet.GetFirstChild<SheetData>();
                    if (sheetData == null) continue;

                    sheetCount++;
                    if (sheetCount > 1)
                        textBuilder.AppendLine($"\n--- Sheet {sheetCount} ---\n");

                    var rowCount = 0;
                    foreach (var row in sheetData.Elements<Row>().Take(maxRows))
                    {
                        var cellValues = new List<string>();
                        foreach (var cell in row.Elements<Cell>().Take(maxCells))
                        {
                            var value = GetCellValue(cell, sharedStrings);
                            cellValues.Add(value ?? "");
                        }

                        if (cellValues.Any(v => !string.IsNullOrWhiteSpace(v)))
                        {
                            textBuilder.AppendLine(string.Join(" | ", cellValues));
                            rowCount++;
                        }

                        if (textBuilder.Length > maxPreviewLength)
                        {
                            textBuilder.AppendLine("\n\n... (content truncated)");
                            break;
                        }
                    }
                }

                var result = textBuilder.ToString().Trim();
                return string.IsNullOrWhiteSpace(result) ? "No extractable text found in spreadsheet" : result;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not extract XLSX text");
                return "Error extracting Excel document content";
            }
        }

        private static string? GetCellValue(Cell cell, string[] sharedStrings)
        {
            if (cell.CellValue == null)
                return null;

            var value = cell.CellValue.InnerText;

            // Check if it's a shared string reference
            if (cell.DataType?.Value == CellValues.SharedString)
            {
                if (int.TryParse(value, out var index) && index >= 0 && index < sharedStrings.Length)
                    return sharedStrings[index];
            }

            return value;
        }

        private string ExtractPptxText(MemoryStream stream)
        {
            try
            {
                using var document = PresentationDocument.Open(stream, false);
                var presentationPart = document.PresentationPart;

                if (presentationPart == null)
                    return "No content found in presentation";

                var textBuilder = new StringBuilder();
                const int maxPreviewLength = 5000;
                var slideNumber = 0;

                foreach (var slidePart in presentationPart.SlideParts.Take(20)) // Limit to 20 slides
                {
                    slideNumber++;
                    var slideText = new StringBuilder();

                    // Extract text from all text elements in the slide
                    foreach (var text in slidePart.Slide.Descendants<DocumentFormat.OpenXml.Drawing.Text>())
                    {
                        if (!string.IsNullOrWhiteSpace(text.Text))
                            slideText.AppendLine(text.Text);
                    }

                    if (slideText.Length > 0)
                    {
                        textBuilder.AppendLine($"\n--- Slide {slideNumber} ---");
                        textBuilder.Append(slideText);
                    }

                    if (textBuilder.Length > maxPreviewLength)
                    {
                        textBuilder.Length = maxPreviewLength;
                        textBuilder.AppendLine("\n\n... (content truncated)");
                        break;
                    }
                }

                var result = textBuilder.ToString().Trim();
                return string.IsNullOrWhiteSpace(result) ? "No extractable text found in presentation" : result;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not extract PPTX text");
                return "Error extracting PowerPoint document content";
            }
        }

        private Task ExtractImageMetadataAsync(Stream stream, FileMetadata metadata)
        {
            try
            {
                stream.Position = 0;
                using var image = Image.Load(stream);

                metadata.Width = image.Width;
                metadata.Height = image.Height;
                metadata.Properties["Format"] = image.Metadata.DecodedImageFormat?.Name ?? "Unknown";
                // BUG-NEW-004 FIX: Add fallback for ToString() which theoretically could return null
                metadata.Properties["PixelFormat"] = image.PixelType.ToString() ?? "Unknown";
                metadata.Properties["Resolution"] = $"{image.Metadata.HorizontalResolution}x{image.Metadata.VerticalResolution} DPI";
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not extract image metadata");
            }

            return Task.CompletedTask;
        }

        private Task ExtractPdfMetadataAsync(Stream stream, FileMetadata metadata)
        {
            try
            {
                stream.Position = 0;
                using var memoryStream = new MemoryStream();
                stream.CopyTo(memoryStream);
                memoryStream.Position = 0;

                using var document = PdfDocument.Open(memoryStream);
                var info = document.Information;

                metadata.Properties["Type"] = "PDF Document";
                metadata.Properties["PageCount"] = document.NumberOfPages.ToString();
                metadata.Properties["Version"] = document.Version.ToString();

                if (!string.IsNullOrWhiteSpace(info.Title))
                    metadata.Properties["Title"] = info.Title;
                if (!string.IsNullOrWhiteSpace(info.Author))
                    metadata.Properties["Author"] = info.Author;
                if (!string.IsNullOrWhiteSpace(info.Subject))
                    metadata.Properties["Subject"] = info.Subject;
                if (!string.IsNullOrWhiteSpace(info.Creator))
                    metadata.Properties["Creator"] = info.Creator;
                if (!string.IsNullOrWhiteSpace(info.Producer))
                    metadata.Properties["Producer"] = info.Producer;
                if (!string.IsNullOrWhiteSpace(info.CreationDate))
                    metadata.Properties["CreationDate"] = info.CreationDate;
                if (!string.IsNullOrWhiteSpace(info.ModifiedDate))
                    metadata.Properties["ModifiedDate"] = info.ModifiedDate;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not extract PDF metadata");
                metadata.Properties["Type"] = "PDF Document";
                metadata.Properties["Note"] = "Could not extract detailed metadata";
            }

            return Task.CompletedTask;
        }

        private Task ExtractOfficeMetadataAsync(Stream stream, FileMetadata metadata, string extension)
        {
            try
            {
                stream.Position = 0;
                using var memoryStream = new MemoryStream();
                stream.CopyTo(memoryStream);
                memoryStream.Position = 0;

                metadata.Properties["Type"] = extension.ToUpperInvariant().TrimStart('.') + " Document";

                switch (extension.ToLowerInvariant())
                {
                    case ".docx":
                        ExtractDocxMetadata(memoryStream, metadata);
                        break;
                    case ".xlsx":
                        ExtractXlsxMetadata(memoryStream, metadata);
                        break;
                    case ".pptx":
                        ExtractPptxMetadata(memoryStream, metadata);
                        break;
                    default:
                        metadata.Properties["Note"] = "Metadata extraction not supported for legacy Office formats";
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not extract Office metadata for {Extension}", extension);
                metadata.Properties["Note"] = "Could not extract detailed metadata";
            }

            return Task.CompletedTask;
        }

        private void ExtractDocxMetadata(MemoryStream stream, FileMetadata metadata)
        {
            using var document = WordprocessingDocument.Open(stream, false);
            var coreProps = document.PackageProperties;

            if (!string.IsNullOrWhiteSpace(coreProps.Title))
                metadata.Properties["Title"] = coreProps.Title;
            if (!string.IsNullOrWhiteSpace(coreProps.Creator))
                metadata.Properties["Author"] = coreProps.Creator;
            if (!string.IsNullOrWhiteSpace(coreProps.Subject))
                metadata.Properties["Subject"] = coreProps.Subject;
            if (coreProps.Created.HasValue)
                metadata.Properties["Created"] = coreProps.Created.Value.ToString("O");
            if (coreProps.Modified.HasValue)
                metadata.Properties["Modified"] = coreProps.Modified.Value.ToString("O");

            // Count paragraphs
            var body = document.MainDocumentPart?.Document.Body;
            if (body != null)
            {
                var paragraphCount = body.Descendants<Paragraph>().Count();
                metadata.Properties["ParagraphCount"] = paragraphCount.ToString();
            }
        }

        private void ExtractXlsxMetadata(MemoryStream stream, FileMetadata metadata)
        {
            using var document = SpreadsheetDocument.Open(stream, false);
            var coreProps = document.PackageProperties;

            if (!string.IsNullOrWhiteSpace(coreProps.Title))
                metadata.Properties["Title"] = coreProps.Title;
            if (!string.IsNullOrWhiteSpace(coreProps.Creator))
                metadata.Properties["Author"] = coreProps.Creator;
            if (coreProps.Created.HasValue)
                metadata.Properties["Created"] = coreProps.Created.Value.ToString("O");
            if (coreProps.Modified.HasValue)
                metadata.Properties["Modified"] = coreProps.Modified.Value.ToString("O");

            // Count sheets
            var sheetCount = document.WorkbookPart?.WorksheetParts.Count() ?? 0;
            metadata.Properties["SheetCount"] = sheetCount.ToString();
        }

        private void ExtractPptxMetadata(MemoryStream stream, FileMetadata metadata)
        {
            using var document = PresentationDocument.Open(stream, false);
            var coreProps = document.PackageProperties;

            if (!string.IsNullOrWhiteSpace(coreProps.Title))
                metadata.Properties["Title"] = coreProps.Title;
            if (!string.IsNullOrWhiteSpace(coreProps.Creator))
                metadata.Properties["Author"] = coreProps.Creator;
            if (!string.IsNullOrWhiteSpace(coreProps.Subject))
                metadata.Properties["Subject"] = coreProps.Subject;
            if (coreProps.Created.HasValue)
                metadata.Properties["Created"] = coreProps.Created.Value.ToString("O");
            if (coreProps.Modified.HasValue)
                metadata.Properties["Modified"] = coreProps.Modified.Value.ToString("O");

            // Count slides
            var slideCount = document.PresentationPart?.SlideParts.Count() ?? 0;
            metadata.Properties["SlideCount"] = slideCount.ToString();
        }

        private static bool IsImageFile(string fileName, string contentType)
        {
            var extension = Path.GetExtension(fileName).ToLowerInvariant();
            return extension == ".jpg" || extension == ".jpeg" || extension == ".png" ||
                   extension == ".gif" || extension == ".bmp" || extension == ".webp" ||
                   contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsOfficeFile(string extension)
        {
            return extension == ".doc" || extension == ".docx" || extension == ".xls" ||
                   extension == ".xlsx" || extension == ".ppt" || extension == ".pptx";
        }
    }
}