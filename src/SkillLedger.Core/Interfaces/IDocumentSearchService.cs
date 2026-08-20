using SkillLedger.Core.DTOs;
using SkillLedger.Core.Models;

namespace SkillLedger.Core.Interfaces
{
    /// <summary>
    /// Service for document search and indexing
    /// </summary>
    public interface IDocumentSearchService
    {
        /// <summary>
        /// Indexes a document for full-text search
        /// </summary>
        Task<bool> IndexDocumentAsync(Guid documentId, string fileName, string content, Dictionary<string, object> metadata);

        /// <summary>
        /// Removes a document from the search index
        /// </summary>
        Task<bool> RemoveFromIndexAsync(Guid documentId);

        /// <summary>
        /// Searches documents by content and metadata
        /// </summary>
        Task<DocumentSearchResult> SearchDocumentsAsync(DocumentSearchRequest request);

        /// <summary>
        /// Updates document index when content changes
        /// </summary>
        Task<bool> UpdateDocumentIndexAsync(Guid documentId, string fileName, string content, Dictionary<string, object> metadata);

        /// <summary>
        /// Gets search suggestions based on partial query
        /// BUG FIX DSS-001/002: Added userId parameter to filter suggestions by user access permissions
        /// </summary>
        Task<IEnumerable<string>> GetSearchSuggestionsAsync(string partialQuery, Guid? workspaceId = null, Guid? userId = null);

        /// <summary>
        /// Rebuilds the entire search index
        /// </summary>
        Task<bool> RebuildIndexAsync();
    }

    /// <summary>
    /// Search request parameters
    /// </summary>
    public class DocumentSearchRequest
    {
        public string Query { get; set; } = string.Empty;

        /// <summary>
        /// BUG FIX DSS-001/002: Required field to filter documents by user access permissions.
        /// Only documents the user can access will be returned.
        /// </summary>
        public Guid? RequestingUserId { get; set; }

        public Guid? WorkspaceId { get; set; }
        public IEnumerable<string> FileTypes { get; set; } = Enumerable.Empty<string>();
        public DateTime? CreatedAfter { get; set; }
        public DateTime? CreatedBefore { get; set; }
        public string? CreatedBy { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public DocumentSearchSort SortBy { get; set; } = DocumentSearchSort.Relevance;
        public bool IncludeContent { get; set; } = false;
    }

    /// <summary>
    /// Search result with documents and metadata
    /// </summary>
    public class DocumentSearchResult
    {
        public IEnumerable<DocumentSearchMatch> Documents { get; set; } = Enumerable.Empty<DocumentSearchMatch>();
        public int TotalCount { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public TimeSpan SearchDuration { get; set; }
        public Dictionary<string, int> Facets { get; set; } = new Dictionary<string, int>();
    }

    /// <summary>
    /// Individual document search match
    /// </summary>
    public class DocumentSearchMatch
    {
        public Guid DocumentId { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public DateTime CreatedAt { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public double RelevanceScore { get; set; }
        public IEnumerable<string> MatchHighlights { get; set; } = Enumerable.Empty<string>();
        public string? ContentPreview { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// Document search sorting options
    /// </summary>
    public enum DocumentSearchSort
    {
        Relevance,
        CreatedDate,
        ModifiedDate,
        FileName,
        FileSize
    }
}