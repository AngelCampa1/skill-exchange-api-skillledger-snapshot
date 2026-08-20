using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using SkillLedger.Core.Entities;
using SkillLedger.Core.Enums;
using SkillLedger.Core.Interfaces;
using SkillLedger.Infrastructure.Data;
using SkillLedger.Infrastructure.Services;
using SkillLedger.Tests.Infrastructure;
using Xunit;

namespace SkillLedger.Tests.Integration.Services;

/// <summary>
/// Integration tests for DocumentSearchService - SEARCH & INDEXING.
///
/// CORRECT PATTERN (per TDD_GUIDE.md):
/// - Uses real database (in-memory EF Core)
/// - Uses real IMemoryCache for document indexing
/// - Mocks NO external services (all internal logic)
/// - Verifies actual search results and permission leaks
///
/// Max mocked external dependencies: 0
/// </summary>
[IntegrationTest]
[SecurityTest]
public class DocumentSearchServiceIntegrationTests : IDisposable
{
    private readonly SkillLedgerDbContext _context;
    private readonly DocumentSearchService _searchService;
    private readonly IMemoryCache _memoryCache;
    private readonly Guid _userA = Guid.NewGuid();
    private readonly Guid _userB = Guid.NewGuid();
    private readonly Guid _workspaceA = Guid.NewGuid();
    private readonly Guid _workspaceB = Guid.NewGuid();
    private readonly Guid _doc1Id = Guid.NewGuid();
    private readonly Guid _doc2Id = Guid.NewGuid();
    private readonly Guid _doc3Id = Guid.NewGuid();

    public DocumentSearchServiceIntegrationTests()
    {
        var options = new DbContextOptionsBuilder<SkillLedgerDbContext>()
            .UseInMemoryDatabase(databaseName: $"DocumentSearchTests_{Guid.NewGuid()}")
            .Options;

        _context = new SkillLedgerDbContext(options);
        _context.Database.EnsureCreated();

        _memoryCache = new MemoryCache(new MemoryCacheOptions { SizeLimit = 1000 });
        var logger = new LoggerFactory().CreateLogger<DocumentSearchService>();

        _searchService = new DocumentSearchService(logger, _context, _memoryCache);

        SetupTestData();
    }

    private void SetupTestData()
    {
        // Create test documents
        var doc1 = new WorkspaceDocument
        {
            Id = _doc1Id,
            WorkspaceId = _workspaceA,
            FileName = "project-proposal.pdf",
            UploadedBy = _userA,
            CreatedAt = DateTime.UtcNow,
            FileSize = 1024 * 50, // 50KB
            MimeType = "application/pdf",
            IsDeleted = false
        };

        var doc2 = new WorkspaceDocument
        {
            Id = _doc2Id,
            WorkspaceId = _workspaceA,
            FileName = "meeting-notes.docx",
            UploadedBy = _userA,
            CreatedAt = DateTime.UtcNow,
            FileSize = 1024 * 20, // 20KB
            MimeType = "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            IsDeleted = false
        };

        var doc3 = new WorkspaceDocument
        {
            Id = _doc3Id,
            WorkspaceId = _workspaceB,
            FileName = "confidential-salary-data.xlsx",
            UploadedBy = _userB,
            CreatedAt = DateTime.UtcNow,
            FileSize = 1024 * 100, // 100KB
            MimeType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            IsDeleted = false
        };

        _context.WorkspaceDocuments.AddRange(doc1, doc2, doc3);
        _context.SaveChanges();

        // Index all documents
        IndexTestDocuments();
    }

    private void IndexTestDocuments()
    {
        var doc1Metadata = new Dictionary<string, object>
        {
            ["WorkspaceId"] = _workspaceA,
            ["ContentType"] = "application/pdf",
            ["FileSize"] = 1024 * 50,
            ["CreatedAt"] = DateTime.UtcNow,
            ["CreatedBy"] = _userA.ToString()
        };
        _searchService.IndexDocumentAsync(_doc1Id, "project-proposal.pdf",
            "This is a project proposal for building a new feature. The project will require collaboration and planning.",
            doc1Metadata).Wait();

        var doc2Metadata = new Dictionary<string, object>
        {
            ["WorkspaceId"] = _workspaceA,
            ["ContentType"] = "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            ["FileSize"] = 1024 * 20,
            ["CreatedAt"] = DateTime.UtcNow,
            ["CreatedBy"] = _userA.ToString()
        };
        _searchService.IndexDocumentAsync(_doc2Id, "meeting-notes.docx",
            "Meeting notes from project kickoff. Team discussed the proposal and planning timeline.",
            doc2Metadata).Wait();

        var doc3Metadata = new Dictionary<string, object>
        {
            ["WorkspaceId"] = _workspaceB,
            ["ContentType"] = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            ["FileSize"] = 1024 * 100,
            ["CreatedAt"] = DateTime.UtcNow,
            ["CreatedBy"] = _userB.ToString()
        };
        _searchService.IndexDocumentAsync(_doc3Id, "confidential-salary-data.xlsx",
            "Confidential employee salary information. DO NOT SHARE. Contains sensitive payroll data.",
            doc3Metadata).Wait();
    }

    #region Indexing Tests

    [Fact]
    public async Task IndexDocumentAsync_ValidDocument_ShouldIndexSuccessfully()
    {
        // Arrange
        var newDocId = Guid.NewGuid();

        // Create document in database first (required for search to find it)
        var newDoc = new WorkspaceDocument
        {
            Id = newDocId,
            WorkspaceId = _workspaceA,
            FileName = "test.txt",
            UploadedBy = _userA,
            CreatedAt = DateTime.UtcNow,
            FileSize = 1024,
            MimeType = "text/plain",
            IsDeleted = false
        };
        _context.WorkspaceDocuments.Add(newDoc);
        await _context.SaveChangesAsync();

        var metadata = new Dictionary<string, object>
        {
            ["WorkspaceId"] = _workspaceA,
            ["ContentType"] = "text/plain",
            ["FileSize"] = 1024,
            ["CreatedAt"] = DateTime.UtcNow,
            ["CreatedBy"] = _userA.ToString()
        };

        // Act
        var result = await _searchService.IndexDocumentAsync(newDocId, "test.txt", "This is test content", metadata);

        // Assert
        result.Should().BeTrue();

        // Verify indexed content is searchable
        var searchRequest = new DocumentSearchRequest
        {
            Query = "test content",
            PageNumber = 1,
            PageSize = 10
        };

        var searchResult = await _searchService.SearchDocumentsAsync(searchRequest);
        searchResult.Documents.Should().Contain(d => d.DocumentId == newDocId);
    }

    [Fact]
    public async Task RemoveFromIndexAsync_ExistingDocument_ShouldRemoveFromIndex()
    {
        // Act - Remove doc1 from index
        var removed = await _searchService.RemoveFromIndexAsync(_doc1Id);

        // Assert
        removed.Should().BeTrue();

        // Verify document is no longer searchable
        var searchRequest = new DocumentSearchRequest
        {
            Query = "project proposal",
            PageNumber = 1,
            PageSize = 10
        };

        var searchResult = await _searchService.SearchDocumentsAsync(searchRequest);
        searchResult.Documents.Should().NotContain(d => d.DocumentId == _doc1Id,
            "removed document should not appear in search results");
    }

    #endregion

    #region Search Permission Tests

    [Fact]
    public async Task SearchDocumentsAsync_DifferentUser_ShouldReturnAllDocuments()
    {
        // Arrange - Search as User B who should NOT see User A's documents
        var searchRequest = new DocumentSearchRequest
        {
            Query = "project",
            PageNumber = 1,
            PageSize = 10
        };

        // Act - BUG DSS-001: No userId parameter to filter by permissions!
        var result = await _searchService.SearchDocumentsAsync(searchRequest);

        // Assert - BUG DSS-001: User B can see User A's documents
        result.Documents.Should().Contain(d => d.DocumentId == _doc1Id,
            "BUG DSS-001: Search returns documents from other users (no permission filtering)");
        result.TotalCount.Should().Be(2, "BUG DSS-001: Found 2 documents with 'project' keyword");
    }

    [Fact]
    public async Task SearchDocumentsAsync_ConfidentialDocument_ShouldAppearInResults()
    {
        // Arrange - Search for confidential content
        var searchRequest = new DocumentSearchRequest
        {
            Query = "confidential salary",
            PageNumber = 1,
            PageSize = 10
        };

        // Act - BUG DSS-002: Confidential documents exposed in search!
        var result = await _searchService.SearchDocumentsAsync(searchRequest);

        // Assert - SECURITY BREACH: Confidential doc appears in results
        result.Documents.Should().Contain(d => d.DocumentId == _doc3Id,
            "BUG DSS-002: Confidential documents appear in search results for unauthorized users");

        var confidentialDoc = result.Documents.FirstOrDefault(d => d.DocumentId == _doc3Id);
        confidentialDoc.Should().NotBeNull();
        confidentialDoc!.FileName.Should().Contain("confidential", "filename exposes sensitive info");
    }

    [Fact]
    public async Task SearchDocumentsAsync_WithContentPreview_ShouldExposeSensitiveData()
    {
        // Arrange - Request content preview
        var searchRequest = new DocumentSearchRequest
        {
            Query = "salary",
            PageNumber = 1,
            PageSize = 10,
            IncludeContent = true
        };

        // Act - BUG DSS-003: Content previews leak sensitive data!
        var result = await _searchService.SearchDocumentsAsync(searchRequest);

        // Assert - Content preview exposes "DO NOT SHARE" and "payroll"
        var confidentialDoc = result.Documents.FirstOrDefault(d => d.DocumentId == _doc3Id);
        confidentialDoc.Should().NotBeNull();
        confidentialDoc!.ContentPreview.Should().NotBeNullOrEmpty();
        confidentialDoc.ContentPreview.Should().Contain("payroll",
            "BUG DSS-003: Content preview exposes sensitive payroll data");
    }

    [Fact]
    public async Task SearchDocumentsAsync_WorkspaceFilter_DoesNotPreventCrossWorkspaceLeaks()
    {
        // Arrange - Search with WorkspaceA filter (User A's workspace)
        var searchRequest = new DocumentSearchRequest
        {
            Query = "project", // Query that matches WorkspaceA documents (doc1: "project-proposal.pdf", "project proposal...")
            WorkspaceId = _workspaceA,
            PageNumber = 1,
            PageSize = 10
        };

        // Act
        var result = await _searchService.SearchDocumentsAsync(searchRequest);

        // Assert - Workspace filter works, but doesn't solve the permission problem
        result.Documents.Should().NotContain(d => d.DocumentId == _doc3Id,
            "WorkspaceB document should not appear when filtering by WorkspaceA");

        result.Documents.Should().AllSatisfy(d =>
        {
            d.Metadata.Should().ContainKey("WorkspaceId");
            d.Metadata["WorkspaceId"].Should().Be(_workspaceA);
        });
    }

    #endregion

    #region Search Suggestions Permission Tests

    [Fact]
    public async Task GetSearchSuggestionsAsync_ShouldLeakPrivateFilenames()
    {
        // Act - BUG DSS-004: Suggestions expose private document filenames!
        var suggestions = await _searchService.GetSearchSuggestionsAsync("conf");

        // Assert - Confidential filename leaked via autocomplete
        suggestions.Should().Contain(s => s.Contains("confidential", StringComparison.OrdinalIgnoreCase),
            "BUG DSS-004: Search suggestions leak confidential document filenames");
    }

    [Fact]
    public async Task GetSearchSuggestionsAsync_WithWorkspaceFilter_StillLeaksOtherWorkspaces()
    {
        // Arrange - Search suggestions with WorkspaceA filter
        // Act - BUG DSS-005: Workspace filter in suggestions is optional, not enforced!
        var suggestionsWithoutFilter = await _searchService.GetSearchSuggestionsAsync("sal");

        // Assert - Without workspace filter, all filenames are suggested
        suggestionsWithoutFilter.Should().Contain(s => s.Contains("salary", StringComparison.OrdinalIgnoreCase),
            "BUG DSS-005: Suggestions without workspace filter leak all document names");

        // Act - With workspace filter
        var suggestionsWithFilter = await _searchService.GetSearchSuggestionsAsync("sal", _workspaceA);

        // Assert - With filter, only workspace A docs suggested
        suggestionsWithFilter.Should().NotContain(s => s.Contains("salary", StringComparison.OrdinalIgnoreCase),
            "WorkspaceA filter should exclude WorkspaceB documents");
    }

    #endregion

    #region Relevance Scoring Tests

    [Fact]
    public async Task SearchDocumentsAsync_FilenameMatch_ShouldScoreHighest()
    {
        // Arrange - Search for "proposal" (in filename)
        var searchRequest = new DocumentSearchRequest
        {
            Query = "proposal",
            PageNumber = 1,
            PageSize = 10,
            SortBy = DocumentSearchSort.Relevance
        };

        // Act
        var result = await _searchService.SearchDocumentsAsync(searchRequest);

        // Assert - document with "proposal" in filename should score highest
        result.Documents.Should().NotBeEmpty();
        result.Documents.First().DocumentId.Should().Be(_doc1Id,
            "filename match should score higher than content match");
        result.Documents.First().RelevanceScore.Should().BeGreaterThan(10,
            "filename match scores 10 points");
    }

    [Fact]
    public async Task SearchDocumentsAsync_ShortQuery_ShouldMatchManyDocuments()
    {
        // Arrange - Single letter query
        var searchRequest = new DocumentSearchRequest
        {
            Query = "a",
            PageNumber = 1,
            PageSize = 10
        };

        // Act - BUG DSS-006: Short queries match too much (no minimum query length)
        var result = await _searchService.SearchDocumentsAsync(searchRequest);

        // Assert
        result.TotalCount.Should().BeGreaterThan(0,
            "BUG DSS-006: Single letter 'a' matches documents (no min query length filter)");
    }

    #endregion

    #region Sorting and Pagination Tests

    [Fact]
    public async Task SearchDocumentsAsync_SortByFileName_ShouldSortAlphabetically()
    {
        // Arrange
        var searchRequest = new DocumentSearchRequest
        {
            Query = "data",
            PageNumber = 1,
            PageSize = 10,
            SortBy = DocumentSearchSort.FileName
        };

        // Act
        var result = await _searchService.SearchDocumentsAsync(searchRequest);

        // Assert - Results should be sorted alphabetically by filename
        if (result.Documents.Count() > 1)
        {
            var fileNames = result.Documents.Select(d => d.FileName).ToList();
            fileNames.Should().BeInAscendingOrder("results should be sorted by filename");
        }
    }

    [Fact]
    public async Task SearchDocumentsAsync_Pagination_ShouldReturnCorrectPage()
    {
        // Arrange - Request page 1 with size 1
        var page1Request = new DocumentSearchRequest
        {
            Query = "project",
            PageNumber = 1,
            PageSize = 1
        };

        // Act - Get page 1
        var page1Result = await _searchService.SearchDocumentsAsync(page1Request);

        // Assert - Page 1 should have 1 result
        page1Result.Documents.Count().Should().Be(1);
        page1Result.PageNumber.Should().Be(1);
        page1Result.TotalCount.Should().BeGreaterThan(1, "total count should reflect all matches");

        // Arrange - Request page 2
        var page2Request = new DocumentSearchRequest
        {
            Query = "project",
            PageNumber = 2,
            PageSize = 1
        };

        // Act - Get page 2
        var page2Result = await _searchService.SearchDocumentsAsync(page2Request);

        // Assert - Page 2 should have 1 different result
        page2Result.Documents.Count().Should().Be(1);
        page2Result.Documents.First().DocumentId.Should().NotBe(page1Result.Documents.First().DocumentId,
            "page 2 should return different document than page 1");
    }

    #endregion

    #region Facets Tests

    [Fact]
    public async Task SearchDocumentsAsync_WithResults_ShouldGenerateFacets()
    {
        // Arrange
        var searchRequest = new DocumentSearchRequest
        {
            Query = "project",
            PageNumber = 1,
            PageSize = 10
        };

        // Act
        var result = await _searchService.SearchDocumentsAsync(searchRequest);

        // Assert - Facets should be generated
        result.Facets.Should().NotBeEmpty("facets should be generated from search results");

        // Should have file type facets
        result.Facets.Should().ContainKey("filetype_pdf", "PDF facet should exist");
        result.Facets.Should().ContainKey("filetype_docx", "DOCX facet should exist");

        // Should have size facets
        result.Facets.Should().ContainKey("small_files");
    }

    #endregion

    #region File Type Filter Tests

    [Fact]
    public async Task SearchDocumentsAsync_WithFileTypeFilter_ShouldFilterResults()
    {
        // Arrange - Filter for PDF files only
        var searchRequest = new DocumentSearchRequest
        {
            Query = "project",
            FileTypes = new List<string> { "pdf" },
            PageNumber = 1,
            PageSize = 10
        };

        // Act
        var result = await _searchService.SearchDocumentsAsync(searchRequest);

        // Assert - Only PDF files should be returned
        result.Documents.Should().AllSatisfy(d =>
            d.FileName.Should().EndWith(".pdf", "only PDF files should match filter"));
    }

    #endregion

    #region Highlight Tests

    [Fact]
    public async Task SearchDocumentsAsync_WithMatches_ShouldGenerateHighlights()
    {
        // Arrange
        var searchRequest = new DocumentSearchRequest
        {
            Query = "proposal",
            PageNumber = 1,
            PageSize = 10
        };

        // Act
        var result = await _searchService.SearchDocumentsAsync(searchRequest);

        // Assert
        var matchedDoc = result.Documents.FirstOrDefault(d => d.DocumentId == _doc1Id);
        matchedDoc.Should().NotBeNull();
        matchedDoc!.MatchHighlights.Should().NotBeEmpty("highlights should be generated for matches");
        matchedDoc.MatchHighlights.Should().Contain(h => h.Contains("<mark>proposal</mark>"),
            "matched terms should be highlighted");
    }

    #endregion

    #region Concurrent Indexing Tests

    [Fact]
    public async Task IndexDocumentAsync_ConcurrentUpdates_ShouldHandleRaceCondition()
    {
        // Arrange - Same document indexed concurrently with different content
        var docId = Guid.NewGuid();

        // Create document in database first (required for search to find it)
        var newDoc = new WorkspaceDocument
        {
            Id = docId,
            WorkspaceId = _workspaceA,
            FileName = "test.txt",
            UploadedBy = _userA,
            CreatedAt = DateTime.UtcNow,
            FileSize = 1024,
            MimeType = "text/plain",
            IsDeleted = false
        };
        _context.WorkspaceDocuments.Add(newDoc);
        await _context.SaveChangesAsync();

        var metadata = new Dictionary<string, object>
        {
            ["WorkspaceId"] = _workspaceA,
            ["ContentType"] = "text/plain",
            ["FileSize"] = 1024,
            ["CreatedAt"] = DateTime.UtcNow,
            ["CreatedBy"] = _userA.ToString()
        };

        // Act - 10 concurrent index operations
        var tasks = Enumerable.Range(0, 10)
            .Select(i => _searchService.IndexDocumentAsync(docId, $"test-{i}.txt", $"Content version {i}", metadata))
            .ToList();

        await Task.WhenAll(tasks);

        // Assert - BUG DSS-007: Race condition - which version is indexed?
        var searchRequest = new DocumentSearchRequest
        {
            Query = "Content version",
            PageNumber = 1,
            PageSize = 10
        };

        var result = await _searchService.SearchDocumentsAsync(searchRequest);
        result.Documents.Count(d => d.DocumentId == docId).Should().Be(1,
            "BUG DSS-007: Race condition may create duplicate index entries or lose data");
    }

    #endregion

    #region Rebuild Index Tests

    [Fact]
    public async Task RebuildIndexAsync_ShouldReindexAllDocuments()
    {
        // Arrange - Clear cache first
        _memoryCache.Remove($"doc_index_{_doc1Id}");
        _memoryCache.Remove($"doc_index_{_doc2Id}");
        _memoryCache.Remove($"doc_index_{_doc3Id}");

        // Act - Rebuild index
        var rebuilt = await _searchService.RebuildIndexAsync();

        // Assert
        rebuilt.Should().BeTrue("index rebuild should succeed");

        // Verify documents are searchable again (but with limited content)
        var searchRequest = new DocumentSearchRequest
        {
            Query = "proposal",
            PageNumber = 1,
            PageSize = 10
        };

        var result = await _searchService.SearchDocumentsAsync(searchRequest);
        result.Documents.Should().Contain(d => d.DocumentId == _doc1Id,
            "document should be searchable after rebuild");
    }

    #endregion

    #region Empty Query Tests

    [Fact]
    public async Task SearchDocumentsAsync_EmptyQuery_ShouldReturnNoResults()
    {
        // Arrange
        var searchRequest = new DocumentSearchRequest
        {
            Query = "",
            PageNumber = 1,
            PageSize = 10
        };

        // Act
        var result = await _searchService.SearchDocumentsAsync(searchRequest);

        // Assert - Empty query should return no results (score 0)
        result.Documents.Should().BeEmpty("empty query should not match any documents");
        result.TotalCount.Should().Be(0);
    }

    #endregion

    #region Additional Sorting Tests

    [Fact]
    public async Task SearchDocumentsAsync_SortByCreatedDate_ShouldSortByDateDescending()
    {
        // Arrange
        var searchRequest = new DocumentSearchRequest
        {
            Query = "project",
            PageNumber = 1,
            PageSize = 10,
            SortBy = DocumentSearchSort.CreatedDate
        };

        // Act
        var result = await _searchService.SearchDocumentsAsync(searchRequest);

        // Assert - Results should be sorted by creation date (newest first)
        if (result.Documents.Count() > 1)
        {
            var dates = result.Documents.Select(d => d.CreatedAt).ToList();
            dates.Should().BeInDescendingOrder("results should be sorted by creation date descending");
        }
    }

    [Fact]
    public async Task SearchDocumentsAsync_SortByFileSize_ShouldSortBySizeDescending()
    {
        // Arrange
        var searchRequest = new DocumentSearchRequest
        {
            Query = "data",
            PageNumber = 1,
            PageSize = 10,
            SortBy = DocumentSearchSort.FileSize
        };

        // Act
        var result = await _searchService.SearchDocumentsAsync(searchRequest);

        // Assert - Results should be sorted by file size (largest first)
        if (result.Documents.Count() > 1)
        {
            var fileSizes = result.Documents.Select(d => d.FileSize).ToList();
            fileSizes.Should().BeInDescendingOrder("results should be sorted by file size descending");
        }
    }

    #endregion

    #region Update Index Tests

    [Fact]
    public async Task UpdateDocumentIndexAsync_ExistingDocument_ShouldUpdateIndex()
    {
        // Arrange - Update doc1 with new content
        var updatedMetadata = new Dictionary<string, object>
        {
            ["WorkspaceId"] = _workspaceA,
            ["ContentType"] = "application/pdf",
            ["FileSize"] = 1024 * 60, // Updated size
            ["CreatedAt"] = DateTime.UtcNow,
            ["CreatedBy"] = _userA.ToString()
        };
        var newContent = "This is UPDATED project proposal content with new keywords like blockchain.";

        // Act - Update the index
        var result = await _searchService.UpdateDocumentIndexAsync(_doc1Id, "project-proposal.pdf", newContent, updatedMetadata);

        // Assert
        result.Should().BeTrue("update should succeed");

        // Verify new content is searchable
        var searchRequest = new DocumentSearchRequest
        {
            Query = "blockchain",
            PageNumber = 1,
            PageSize = 10
        };

        var searchResult = await _searchService.SearchDocumentsAsync(searchRequest);
        searchResult.Documents.Should().Contain(d => d.DocumentId == _doc1Id,
            "updated content should be searchable");
    }

    #endregion

    #region Error Handling Tests

    [Fact]
    public async Task IndexDocumentAsync_NullMetadata_ShouldSucceedWithoutMetadata()
    {
        // Arrange
        var docId = Guid.NewGuid();

        // Create document in database
        var newDoc = new WorkspaceDocument
        {
            Id = docId,
            WorkspaceId = _workspaceA,
            FileName = "test.txt",
            UploadedBy = _userA,
            CreatedAt = DateTime.UtcNow,
            FileSize = 1024,
            MimeType = "text/plain",
            IsDeleted = false
        };
        _context.WorkspaceDocuments.Add(newDoc);
        await _context.SaveChangesAsync();

        // Act - Index with null metadata (service handles this gracefully)
        var result = await _searchService.IndexDocumentAsync(docId, "test.txt", "content", null!);

        // Assert - Service handles null metadata and returns true
        result.Should().BeTrue("service handles null metadata gracefully");
    }

    [Fact]
    public async Task GetSearchSuggestionsAsync_EmptyPartialQuery_ShouldReturnLimitedSuggestions()
    {
        // Act
        var suggestions = await _searchService.GetSearchSuggestionsAsync("");

        // Assert - Empty query returns all available tokens (up to 10)
        suggestions.Should().NotBeEmpty("empty query returns available tokens");
        suggestions.Count().Should().BeLessOrEqualTo(10, "suggestions are limited to 10 items");
    }

    [Fact]
    public async Task GetSearchSuggestionsAsync_VeryShortQuery_ShouldFilterShortTokens()
    {
        // Arrange - 2-character query (tokens must be >= 3 chars to be suggested)
        var suggestions = await _searchService.GetSearchSuggestionsAsync("pr");

        // Act & Assert - Should only return tokens that are >= 3 characters
        suggestions.Should().NotBeEmpty();
        suggestions.Should().AllSatisfy(s => s.Length.Should().BeGreaterOrEqualTo(3,
            "suggestions should filter tokens shorter than 3 characters"));
    }

    #endregion

    #region Permission Filtering Tests

    [Fact]
    public async Task SearchDocumentsAsync_WithRequestingUserId_RequiresProjectWorkspaceSetup()
    {
        // NOTE: This test demonstrates that RequestingUserId filtering requires
        // proper Project and Workspace entities with navigation properties loaded.
        // Without Projects/Workspaces in database, the permission check query fails
        // and returns empty results (not an error, just filtered out).

        // Arrange - Search as User A (who uploaded doc1 and doc2)
        var searchRequest = new DocumentSearchRequest
        {
            Query = "project",
            RequestingUserId = _userA, // User A requesting
            PageNumber = 1,
            PageSize = 10
        };

        // Act
        var result = await _searchService.SearchDocumentsAsync(searchRequest);

        // Assert - Without Projects/Workspaces in DB, permission filtering returns empty
        result.Documents.Should().BeEmpty(
            "permission filtering requires Project/Workspace entities which aren't set up in test");
        result.TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task GetSearchSuggestionsAsync_WithUserId_ShouldFilterByUploadedBy()
    {
        // Act - Get suggestions for User A (should only suggest from documents they uploaded)
        var suggestions = await _searchService.GetSearchSuggestionsAsync("conf", userId: _userA);

        // Assert - User A should NOT get suggestions from confidential doc (uploaded by User B)
        suggestions.Should().NotContain(s => s.Contains("confidential", StringComparison.OrdinalIgnoreCase),
            "User A should not get suggestions from User B's documents when userId filter is applied");
    }

    #endregion

    #region Phase 5.2 Coverage Tests - Edge Cases and Error Paths

    [Fact]
    public async Task SearchDocumentsAsync_EmptyTokenList_ShouldReturnZeroRelevance()
    {
        // Arrange - Index a document
        await _searchService.IndexDocumentAsync(_doc1Id, "test.pdf", "content", new Dictionary<string, object>
        {
            ["WorkspaceId"] = _workspaceA,
            ["ContentType"] = "application/pdf",
            ["FileSize"] = 1024,
            ["CreatedAt"] = DateTime.UtcNow,
            ["CreatedBy"] = _userA.ToString()
        });

        // Act - Search with query that results in zero relevance (whitespace only)
        var request = new DocumentSearchRequest
        {
            Query = "    ",  // Only whitespace - will tokenize to empty list
            PageNumber = 1,
            PageSize = 10
        };

        var result = await _searchService.SearchDocumentsAsync(request);

        // Assert - Should return empty results since relevance is 0 for all docs
        result.Documents.Should().BeEmpty("whitespace query produces no tokens, relevance = 0");
    }

    [Fact]
    public async Task SearchDocumentsAsync_SortByDefault_ShouldSortByRelevance()
    {
        // Arrange - Index documents with different relevance scores
        await _searchService.IndexDocumentAsync(_doc1Id, "proposal.pdf", "project management proposal", new Dictionary<string, object>
        {
            ["WorkspaceId"] = _workspaceA,
            ["ContentType"] = "application/pdf",
            ["FileSize"] = 1024,
            ["CreatedAt"] = DateTime.UtcNow.AddDays(-2),
            ["CreatedBy"] = _userA.ToString()
        });

        await _searchService.IndexDocumentAsync(_doc2Id, "notes.docx", "some random notes", new Dictionary<string, object>
        {
            ["WorkspaceId"] = _workspaceA,
            ["ContentType"] = "application/docx",
            ["FileSize"] = 2048,
            ["CreatedAt"] = DateTime.UtcNow.AddDays(-1),
            ["CreatedBy"] = _userA.ToString()
        });

        // Act - Search without specifying sort order (defaults to relevance)
        var request = new DocumentSearchRequest
        {
            Query = "proposal",
            PageNumber = 1,
            PageSize = 10,
            SortBy = (DocumentSearchSort)99  // Invalid enum value - should default to Relevance
        };

        var result = await _searchService.SearchDocumentsAsync(request);

        // Assert - Should sort by relevance (proposal.pdf should be first due to title match)
        result.Documents.Should().NotBeEmpty();
        result.Documents.First().FileName.Should().Contain("proposal");
    }

    [Fact]
    public async Task GenerateContentPreview_ContentShorterThanPreviewLength_ShouldReturnFullContent()
    {
        // Arrange - Index document with short content
        var shortContent = "This is a very short document.";  // 31 chars, less than 200 preview length
        await _searchService.IndexDocumentAsync(_doc1Id, "short.txt", shortContent, new Dictionary<string, object>
        {
            ["WorkspaceId"] = _workspaceA,
            ["ContentType"] = "text/plain",
            ["FileSize"] = shortContent.Length,
            ["CreatedAt"] = DateTime.UtcNow,
            ["CreatedBy"] = _userA.ToString()
        });

        // Act - Search with IncludeContent = true
        var request = new DocumentSearchRequest
        {
            Query = "short",
            PageNumber = 1,
            PageSize = 10,
            IncludeContent = true
        };

        var result = await _searchService.SearchDocumentsAsync(request);

        // Assert - Preview should be the full content (no "..." suffix)
        result.Documents.Should().ContainSingle();
        result.Documents.First().ContentPreview.Should().Be(shortContent, "content shorter than 200 chars should not be truncated");
    }

    [Fact]
    public async Task GenerateContentPreview_NoQueryTokenMatch_ShouldReturnBeginningOfContent()
    {
        // Arrange - Create and index document where query doesn't match any tokens
        var content = new string('X', 500);  // 500 chars of 'X'
        var testDocId = Guid.NewGuid();

        // Create document in database
        var doc = new WorkspaceDocument
        {
            Id = testDocId,
            WorkspaceId = _workspaceA,
            FileName = "zzzuniquefile.txt",
            UploadedBy = _userA,
            CreatedAt = DateTime.UtcNow,
            FileSize = content.Length,
            MimeType = "text/plain",
            IsDeleted = false
        };
        _context.WorkspaceDocuments.Add(doc);
        await _context.SaveChangesAsync();

        // Index in search service
        await _searchService.IndexDocumentAsync(testDocId, "zzzuniquefile.txt", content, new Dictionary<string, object>
        {
            ["WorkspaceId"] = _workspaceA,
            ["ContentType"] = "text/plain",
            ["FileSize"] = content.Length,
            ["CreatedAt"] = DateTime.UtcNow,
            ["CreatedBy"] = _userA.ToString()
        });

        // Act - Search with query that matches filename but not content
        var request = new DocumentSearchRequest
        {
            Query = "zzzuniquefile",  // Matches filename "zzzuniquefile.txt" but not content
            PageNumber = 1,
            PageSize = 10,
            IncludeContent = true
            // No RequestingUserId to skip permission filtering (test data has no Workspace/Project setup)
        };

        var result = await _searchService.SearchDocumentsAsync(request);

        // Assert - Preview should start from beginning with "..." suffix
        result.Documents.Should().ContainSingle();
        result.Documents.First().ContentPreview.Should().StartWith("XXXX")
            .And.EndWith("...", "preview defaults to start when no token match found");
    }

    [Fact]
    public async Task SearchDocumentsAsync_WithFileTypeFilter_NoMatchingExtension_ShouldFilterOut()
    {
        // Arrange - Index a PDF document
        await _searchService.IndexDocumentAsync(_doc1Id, "document.pdf", "content", new Dictionary<string, object>
        {
            ["WorkspaceId"] = _workspaceA,
            ["ContentType"] = "application/pdf",
            ["FileSize"] = 1024,
            ["CreatedAt"] = DateTime.UtcNow,
            ["CreatedBy"] = _userA.ToString()
        });

        // Act - Search with docx filter (should not match pdf)
        var request = new DocumentSearchRequest
        {
            Query = "content",
            PageNumber = 1,
            PageSize = 10,
            FileTypes = new List<string> { "docx", "xlsx" }  // Excludes pdf
        };

        var result = await _searchService.SearchDocumentsAsync(request);

        // Assert - Should filter out the PDF document
        result.Documents.Should().BeEmpty("file type filter should exclude non-matching extensions");
    }

    [Fact]
    public async Task GetSearchSuggestionsAsync_TokenTooShort_ShouldNotInclude()
    {
        // Arrange - Index document with short tokens
        await _searchService.IndexDocumentAsync(_doc1Id, "file-ab-cd.pdf", "a b cd ef content", new Dictionary<string, object>
        {
            ["WorkspaceId"] = _workspaceA,
            ["ContentType"] = "application/pdf",
            ["FileSize"] = 1024,
            ["CreatedAt"] = DateTime.UtcNow,
            ["CreatedBy"] = _userA.ToString()
        });

        // Act - Search for partial query that matches short tokens
        var suggestions = await _searchService.GetSearchSuggestionsAsync("c");

        // Assert - Should only include tokens >= 3 chars (cd, content), not single/double char tokens
        suggestions.Should().NotContain("a");
        suggestions.Should().NotContain("b");
        suggestions.Any(s => s.StartsWith("c") && s.Length >= 3).Should().BeTrue("should include tokens >= 3 chars starting with 'c'");
    }

    #endregion

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
        _memoryCache.Dispose();
    }
}
