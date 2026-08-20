using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using SkillLedger.Core.Interfaces;
using SkillLedger.Infrastructure.Data;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace SkillLedger.Infrastructure.Services
{
    /// <summary>
    /// Basic document search service with in-memory indexing
    /// In production, this would integrate with Elasticsearch or Azure Cognitive Search
    /// </summary>
    public class DocumentSearchService : IDocumentSearchService
    {
        private readonly ILogger<DocumentSearchService> _logger;
        private readonly SkillLedgerDbContext _context;
        // BUG-BE-004 FIX: Replaced static Dictionary with IMemoryCache to prevent unbounded memory growth
        // IMemoryCache provides automatic expiration and size limits, preventing OutOfMemoryException
        private readonly IMemoryCache _cache;

        public DocumentSearchService(ILogger<DocumentSearchService> logger, SkillLedgerDbContext context, IMemoryCache cache)
        {
            _logger = logger;
            _context = context;
            _cache = cache;
        }

        public Task<bool> IndexDocumentAsync(Guid documentId, string fileName, string content, Dictionary<string, object> metadata)
        {
            try
            {
                var tokens = TokenizeContent(content);
                var searchableText = ExtractSearchableText(content, fileName);

                var documentIndex = new DocumentIndex
                {
                    DocumentId = documentId,
                    FileName = fileName,
                    Content = content,
                    SearchableText = searchableText,
                    Tokens = tokens,
                    Metadata = metadata,
                    IndexedAt = DateTime.UtcNow
                };

                // BUG-BE-004 FIX: Use IMemoryCache with 24-hour sliding expiration (documents change rarely)
                var cacheKey = $"doc_index_{documentId}";
                _cache.Set(cacheKey, documentIndex, new MemoryCacheEntryOptions
                {
                    SlidingExpiration = TimeSpan.FromHours(24),
                    Size = 1
                });

                _logger.LogInformation("Document {DocumentId} indexed successfully with {TokenCount} tokens",
                    documentId, tokens.Count);

                return Task.FromResult(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error indexing document {DocumentId}", documentId);
                return Task.FromResult(false);
            }
        }

        public Task<bool> RemoveFromIndexAsync(Guid documentId)
        {
            try
            {
                // BUG-BE-004 FIX: Use IMemoryCache Remove method (thread-safe, no lock needed)
                var cacheKey = $"doc_index_{documentId}";
                _cache.Remove(cacheKey);

                _logger.LogInformation("Document {DocumentId} removed from search index", documentId);
                return Task.FromResult(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing document {DocumentId} from index", documentId);
                return Task.FromResult(false);
            }
        }

        public Task<DocumentSearchResult> SearchDocumentsAsync(DocumentSearchRequest request)
        {
            var stopwatch = Stopwatch.StartNew();

            try
            {
                var queryTokens = TokenizeContent(request.Query);
                var matches = new List<DocumentSearchMatch>();

                // BUG FIX DSS-001/002: Filter documents by user access permissions
                // Only return documents the requesting user has permission to access
                IQueryable<Core.Entities.WorkspaceDocument> documentsQuery = _context.WorkspaceDocuments
                    .Include(d => d.Workspace)
                        .ThenInclude(w => w.Project)
                    .Include(d => d.Shares)
                    .Where(d => !d.IsDeleted);

                // BUG FIX DSS-001: Apply permission filtering when RequestingUserId is provided
                if (request.RequestingUserId.HasValue)
                {
                    var userId = request.RequestingUserId.Value;
                    documentsQuery = documentsQuery.Where(d =>
                        // User uploaded the document
                        d.UploadedBy == userId ||
                        // User is project client or provider
                        d.Workspace.Project.ClientId == userId ||
                        d.Workspace.Project.ProviderId == userId ||
                        // User has explicit share access (not revoked and not expired)
                        d.Shares.Any(s => s.UserId == userId && s.IsActive && s.RevokedAt == null &&
                            (s.ExpiresAt == null || s.ExpiresAt > DateTime.UtcNow)));
                }

                var documentIds = documentsQuery
                    .Select(d => d.Id)
                    .ToList();

                foreach (var documentId in documentIds)
                {
                    var cacheKey = $"doc_index_{documentId}";
                    if (_cache.TryGetValue<DocumentIndex>(cacheKey, out var indexEntry) && indexEntry != null)
                    {
                        var relevanceScore = CalculateRelevanceScore(queryTokens, indexEntry);

                        if (relevanceScore > 0)
                        {
                            // Apply filters
                            if (request.WorkspaceId.HasValue &&
                                indexEntry.Metadata.TryGetValue("WorkspaceId", out var workspaceId) &&
                                !workspaceId.Equals(request.WorkspaceId.Value))
                                continue;

                            if (request.FileTypes.Any())
                            {
                                var fileExtension = Path.GetExtension(indexEntry.FileName).TrimStart('.');
                                if (!request.FileTypes.Contains(fileExtension, StringComparer.OrdinalIgnoreCase))
                                    continue;
                            }

                            var highlights = GenerateHighlights(request.Query, indexEntry.SearchableText);
                            var preview = request.IncludeContent ?
                                GenerateContentPreview(indexEntry.Content, queryTokens) : null;

                            matches.Add(new DocumentSearchMatch
                            {
                                DocumentId = indexEntry.DocumentId,
                                FileName = indexEntry.FileName,
                                ContentType = indexEntry.Metadata.TryGetValue("ContentType", out var ct) ? ct.ToString() ?? "" : "",
                                FileSize = indexEntry.Metadata.TryGetValue("FileSize", out var fs) && long.TryParse(fs.ToString(), out var size) ? size : 0,
                                CreatedAt = indexEntry.Metadata.TryGetValue("CreatedAt", out var ca) && DateTime.TryParse(ca.ToString(), out var created) ? created : DateTime.MinValue,
                                CreatedBy = indexEntry.Metadata.TryGetValue("CreatedBy", out var cb) ? cb.ToString() ?? "" : "",
                                RelevanceScore = relevanceScore,
                                MatchHighlights = highlights,
                                ContentPreview = preview,
                                Metadata = indexEntry.Metadata
                            });
                        }
                    }
                }

                // Sort results
                matches = request.SortBy switch
                {
                    DocumentSearchSort.Relevance => matches.OrderByDescending(m => m.RelevanceScore).ToList(),
                    DocumentSearchSort.CreatedDate => matches.OrderByDescending(m => m.CreatedAt).ToList(),
                    DocumentSearchSort.FileName => matches.OrderBy(m => m.FileName).ToList(),
                    DocumentSearchSort.FileSize => matches.OrderByDescending(m => m.FileSize).ToList(),
                    _ => matches.OrderByDescending(m => m.RelevanceScore).ToList()
                };

                // Apply pagination
                var totalCount = matches.Count;
                var pagedMatches = matches
                    .Skip((request.PageNumber - 1) * request.PageSize)
                    .Take(request.PageSize)
                    .ToList();

                // Generate facets
                var facets = GenerateFacets(matches);

                stopwatch.Stop();

                return Task.FromResult(new DocumentSearchResult
                {
                    Documents = pagedMatches,
                    TotalCount = totalCount,
                    PageNumber = request.PageNumber,
                    PageSize = request.PageSize,
                    SearchDuration = stopwatch.Elapsed,
                    Facets = facets
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching documents with query: {Query}", request.Query);
                stopwatch.Stop();

                return Task.FromResult(new DocumentSearchResult
                {
                    Documents = Enumerable.Empty<DocumentSearchMatch>(),
                    SearchDuration = stopwatch.Elapsed
                });
            }
        }

        public async Task<bool> UpdateDocumentIndexAsync(Guid documentId, string fileName, string content, Dictionary<string, object> metadata)
        {
            // Update is same as index for this implementation
            return await IndexDocumentAsync(documentId, fileName, content, metadata);
        }

        public Task<IEnumerable<string>> GetSearchSuggestionsAsync(string partialQuery, Guid? workspaceId = null, Guid? userId = null)
        {
            try
            {
                var suggestions = new HashSet<string>();
                var queryLower = partialQuery.ToLowerInvariant();

                // BUG FIX DSS-001/002: Apply permission filtering to suggestions
                IQueryable<Core.Entities.WorkspaceDocument> documentsQuery = _context.WorkspaceDocuments
                    .Include(d => d.Workspace)
                        .ThenInclude(w => w.Project)
                    .Include(d => d.Shares)
                    .Where(d => !d.IsDeleted);

                // BUG FIX DSS-001: Apply permission filtering when userId is provided
                if (userId.HasValue)
                {
                    var requestingUserId = userId.Value;
                    documentsQuery = documentsQuery.Where(d =>
                        d.UploadedBy == requestingUserId ||
                        d.Workspace.Project.ClientId == requestingUserId ||
                        d.Workspace.Project.ProviderId == requestingUserId ||
                        d.Shares.Any(s => s.UserId == requestingUserId && s.IsActive && s.RevokedAt == null &&
                            (s.ExpiresAt == null || s.ExpiresAt > DateTime.UtcNow)));
                }

                var documentIds = documentsQuery
                    .Select(d => d.Id)
                    .ToList();

                foreach (var documentId in documentIds)
                {
                    var cacheKey = $"doc_index_{documentId}";
                    if (_cache.TryGetValue<DocumentIndex>(cacheKey, out var indexEntry) && indexEntry != null)
                    {
                        // Filter by workspace if specified
                        if (workspaceId.HasValue &&
                            indexEntry.Metadata.TryGetValue("WorkspaceId", out var workspaceIdValue) &&
                            !workspaceIdValue.Equals(workspaceId.Value))
                            continue;

                        // Search in filename
                        if (indexEntry.FileName.ToLowerInvariant().Contains(queryLower))
                        {
                            suggestions.Add(indexEntry.FileName);
                        }

                        // Search in tokens
                        foreach (var token in indexEntry.Tokens)
                        {
                            if (token.StartsWith(queryLower, StringComparison.OrdinalIgnoreCase) &&
                                token.Length >= 3)
                            {
                                suggestions.Add(token);
                                if (suggestions.Count >= 10) break;
                            }
                        }
                    }
                }

                return Task.FromResult(suggestions.Take(10).OrderBy(s => s).AsEnumerable());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting search suggestions for: {Query}", partialQuery);
                return Task.FromResult(Enumerable.Empty<string>());
            }
        }

        public async Task<bool> RebuildIndexAsync()
        {
            try
            {
                _logger.LogInformation("Starting search index rebuild");

                // BUG-BE-004 FIX: Clear all document index entries from cache
                var documentIds = await _context.WorkspaceDocuments
                    .Where(d => !d.IsDeleted)
                    .Select(d => d.Id)
                    .ToListAsync();

                // Remove all existing cache entries
                foreach (var docId in documentIds)
                {
                    _cache.Remove($"doc_index_{docId}");
                }

                // In a real implementation, this would re-index all documents from the database
                var documents = await _context.WorkspaceDocuments
                    .Where(d => !d.IsDeleted)
                    .ToListAsync();

                var indexedCount = 0;
                foreach (var document in documents)
                {
                    // In production, we would extract content from the actual file
                    var metadata = new Dictionary<string, object>
                    {
                        ["WorkspaceId"] = document.WorkspaceId,
                        ["ContentType"] = document.MimeType,
                        ["FileSize"] = document.FileSize,
                        ["CreatedAt"] = document.CreatedAt,
                        ["CreatedBy"] = document.UploadedBy
                    };

                    // For demo, use filename as content
                    await IndexDocumentAsync(document.Id, document.FileName, document.FileName, metadata);
                    indexedCount++;
                }

                _logger.LogInformation("Search index rebuild completed. Indexed {Count} documents", indexedCount);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rebuilding search index");
                return false;
            }
        }

        private List<string> TokenizeContent(string content)
        {
            if (string.IsNullOrWhiteSpace(content))
                return new List<string>();

            // Simple tokenization - split on whitespace and punctuation
            // BUG DSS-006: No minimum query length filter - single letters will match
            var tokens = Regex.Split(content.ToLowerInvariant(), @"[\s\p{P}]+")
                .Where(token => !string.IsNullOrWhiteSpace(token))
                .Distinct()
                .ToList();

            return tokens;
        }

        private string ExtractSearchableText(string content, string fileName)
        {
            // Combine filename and content for searching
            return $"{Path.GetFileNameWithoutExtension(fileName)} {content}";
        }

        private double CalculateRelevanceScore(List<string> queryTokens, DocumentIndex indexEntry)
        {
            if (!queryTokens.Any()) return 0;

            double score = 0;
            var totalTokens = indexEntry.Tokens.Count;

            foreach (var queryToken in queryTokens)
            {
                // Exact matches in filename get high score
                if (indexEntry.FileName.ToLowerInvariant().Contains(queryToken))
                {
                    score += 10;
                }

                // Token matches
                var tokenMatches = indexEntry.Tokens.Count(t => t.Contains(queryToken));
                if (tokenMatches > 0)
                {
                    score += (double)tokenMatches / totalTokens * 5;
                }

                // Partial matches in content
                if (indexEntry.SearchableText.ToLowerInvariant().Contains(queryToken))
                {
                    score += 1;
                }
            }

            return score;
        }

        private IEnumerable<string> GenerateHighlights(string query, string content)
        {
            var highlights = new List<string>();
            var queryTokens = TokenizeContent(query);

            foreach (var token in queryTokens)
            {
                var index = content.IndexOf(token, StringComparison.OrdinalIgnoreCase);
                if (index >= 0)
                {
                    var start = Math.Max(0, index - 50);
                    var length = Math.Min(100, content.Length - start);
                    var excerpt = content.Substring(start, length);

                    // Highlight the matched term
                    excerpt = Regex.Replace(excerpt, Regex.Escape(token),
                        $"<mark>{token}</mark>", RegexOptions.IgnoreCase);

                    highlights.Add($"...{excerpt}...");
                }
            }

            return highlights.Take(3);
        }

        private string GenerateContentPreview(string content, List<string> queryTokens)
        {
            const int previewLength = 200;

            if (content.Length <= previewLength)
                return content;

            // Try to find content around the first query match
            foreach (var token in queryTokens)
            {
                var index = content.IndexOf(token, StringComparison.OrdinalIgnoreCase);
                if (index >= 0)
                {
                    var start = Math.Max(0, index - previewLength / 2);
                    var length = Math.Min(previewLength, content.Length - start);
                    return content.Substring(start, length) + (start + length < content.Length ? "..." : "");
                }
            }

            // Default to beginning of content
            return content.Substring(0, Math.Min(previewLength, content.Length)) + "...";
        }

        private Dictionary<string, int> GenerateFacets(List<DocumentSearchMatch> matches)
        {
            var facets = new Dictionary<string, int>();

            // File type facets
            var fileTypes = matches
                .GroupBy(m => Path.GetExtension(m.FileName).TrimStart('.').ToLowerInvariant())
                .Where(g => !string.IsNullOrEmpty(g.Key))
                .ToDictionary(g => g.Key, g => g.Count());

            foreach (var fileType in fileTypes)
            {
                facets[$"filetype_{fileType.Key}"] = fileType.Value;
            }

            // Size facets
            facets["small_files"] = matches.Count(m => m.FileSize < 1024 * 1024); // < 1MB
            facets["medium_files"] = matches.Count(m => m.FileSize >= 1024 * 1024 && m.FileSize < 10 * 1024 * 1024); // 1-10MB
            facets["large_files"] = matches.Count(m => m.FileSize >= 10 * 1024 * 1024); // > 10MB

            return facets;
        }

        private class DocumentIndex
        {
            public Guid DocumentId { get; set; }
            public string FileName { get; set; } = string.Empty;
            public string Content { get; set; } = string.Empty;
            public string SearchableText { get; set; } = string.Empty;
            public List<string> Tokens { get; set; } = new List<string>();
            public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
            public DateTime IndexedAt { get; set; }
        }
    }
}