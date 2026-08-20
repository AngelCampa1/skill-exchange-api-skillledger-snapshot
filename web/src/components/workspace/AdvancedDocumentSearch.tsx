'use client'

import { logger } from '@/utils/logger';

import React, { useState, useEffect, useCallback } from 'react';
import { 
  Search, 
  Filter, 
  X, 
  Calendar, 
  User, 
  Folder, 
  FileType, 
  Tag,
  HardDrive,
  SortAsc,
  SortDesc,
  RefreshCw
} from 'lucide-react';
import { AUTH_CONFIG } from '../../constants/auth';
import {
  AdvancedSearchFilter,
  DocumentSearchResult,
  SearchFacets,
  WorkspaceDocument,
  DocumentFolder
} from '@/types/document';

interface AdvancedDocumentSearchProps {
  workspaceId: string;
  onSearchResults: (results: DocumentSearchResult) => void;
  onError: (error: string) => void;
  className?: string;
}

export default function AdvancedDocumentSearch({
  workspaceId,
  onSearchResults,
  onError,
  className = ''
}: AdvancedDocumentSearchProps) {
  const [isAdvancedOpen, setIsAdvancedOpen] = useState(false);
  const [isLoading, setIsLoading] = useState(false);
  const [filters, setFilters] = useState<AdvancedSearchFilter>({
    query: '',
    sortBy: 'uploadedAt',
    sortOrder: 'desc'
  });

  const [facets, setFacets] = useState<SearchFacets>({
    fileTypes: {},
    uploaders: {},
    folders: {},
    dateRanges: {},
    fileSizes: {}
  });

  const [availableUploaders, setAvailableUploaders] = useState<Array<{ id: string; name: string }>>([]);
  const [availableFolders, setAvailableFolders] = useState<DocumentFolder[]>([]);

  const loadFilterOptions = useCallback(async () => {
    try {
      // BUG-FE-002 FIX: Removed localStorage token
      // Load uploaders and folders for filter options
      const [uploadersResponse, foldersResponse] = await Promise.all([
        fetch(`/api/documents/workspace/${workspaceId}/uploaders`, {
          credentials: AUTH_CONFIG.CREDENTIALS
        }),
        fetch(`/api/documents/workspace/${workspaceId}/folders`, {
          credentials: AUTH_CONFIG.CREDENTIALS
        })
      ]);

      if (uploadersResponse.ok) {
        const uploaders = await uploadersResponse.json();
        setAvailableUploaders(uploaders);
      }

      if (foldersResponse.ok) {
        const folders = await foldersResponse.json();
        setAvailableFolders(folders);
      }
    } catch (error) {
      logger.error('Failed to load filter options:', error);
    }
  }, [workspaceId]);

  // Load available filters data
  useEffect(() => {
    loadFilterOptions();
  }, [loadFilterOptions]);

  const hasActiveFilters = useCallback(() => {
    return Boolean(
      filters.fileTypes?.length ||
      filters.uploaderIds?.length ||
      filters.folderIds?.length ||
      filters.dateRange?.start ||
      filters.dateRange?.end ||
      filters.sizeRange?.min ||
      filters.sizeRange?.max ||
      filters.tags?.length ||
      filters.hasDescription !== undefined
    );
  }, [filters]);

  const performSearch = useCallback(async () => {
    if (!filters.query && !hasActiveFilters()) {
      // Don't search with empty query and no filters
      return;
    }

    setIsLoading(true);
    try {
      const searchParams = new URLSearchParams();
      
      if (filters.query) searchParams.append('q', filters.query);
      if (filters.fileTypes?.length) {
        filters.fileTypes.forEach(type => searchParams.append('fileTypes', type));
      }
      if (filters.uploaderIds?.length) {
        filters.uploaderIds.forEach(id => searchParams.append('uploaderIds', id));
      }
      if (filters.folderIds?.length) {
        filters.folderIds.forEach(id => searchParams.append('folderIds', id));
      }
      if (filters.dateRange?.start) {
        searchParams.append('startDate', filters.dateRange.start);
      }
      if (filters.dateRange?.end) {
        searchParams.append('endDate', filters.dateRange.end);
      }
      if (filters.sizeRange?.min) {
        searchParams.append('minSize', filters.sizeRange.min.toString());
      }
      if (filters.sizeRange?.max) {
        searchParams.append('maxSize', filters.sizeRange.max.toString());
      }
      if (filters.tags?.length) {
        filters.tags.forEach(tag => searchParams.append('tags', tag));
      }
      if (filters.hasDescription !== undefined) {
        searchParams.append('hasDescription', filters.hasDescription.toString());
      }
      if (filters.sortBy) {
        searchParams.append('sortBy', filters.sortBy);
      }
      if (filters.sortOrder) {
        searchParams.append('sortOrder', filters.sortOrder);
      }

      // BUG-FE-002 FIX: Removed localStorage token
      const response = await fetch(
        `/api/documents/workspace/${workspaceId}/search?${searchParams}`,
        {
          credentials: AUTH_CONFIG.CREDENTIALS
        }
      );

      if (!response.ok) {
        throw new Error('Search failed');
      }

      const results: DocumentSearchResult = await response.json();
      setFacets(results.facets);
      onSearchResults(results);
    } catch (error) {
      onError(error instanceof Error ? error.message : 'Search failed');
    } finally {
      setIsLoading(false);
    }
  }, [filters, workspaceId, onSearchResults, onError, hasActiveFilters]);

  const clearFilters = () => {
    setFilters({
      query: '',
      sortBy: 'uploadedAt',
      sortOrder: 'desc'
    });
  };

  const updateFilter = (key: keyof AdvancedSearchFilter, value: any) => {
    setFilters(prev => ({ ...prev, [key]: value }));
  };

  const addTag = (tag: string) => {
    if (tag && !filters.tags?.includes(tag)) {
      updateFilter('tags', [...(filters.tags || []), tag]);
    }
  };

  const removeTag = (tag: string) => {
    updateFilter('tags', filters.tags?.filter(t => t !== tag) || []);
  };

  // Trigger search on filter changes (debounced)
  useEffect(() => {
    const timeoutId = setTimeout(() => {
      performSearch();
    }, 300);

    return () => clearTimeout(timeoutId);
  }, [performSearch]);

  return (
    <div className={`bg-card border border-border rounded-lg ${className}`}>
      {/* Basic Search */}
      <div className="p-4 border-b border-border">
        <div className="flex items-center space-x-3">
          <div className="flex-1 relative">
            <Search className="absolute left-3 top-1/2 transform -translate-y-1/2 h-4 w-4 text-muted-foreground" />
            <input
              type="text"
              placeholder="Search documents and content..."
              value={filters.query || ''}
              onChange={(e) => updateFilter('query', e.target.value)}
              className="w-full pl-10 pr-4 py-2 border border-input rounded-lg focus:ring-ring focus:border-ring"
            />
          </div>
          <button
            onClick={() => setIsAdvancedOpen(!isAdvancedOpen)}
            className={`flex items-center px-3 py-2 text-sm border rounded-lg transition-colors ${
              isAdvancedOpen || hasActiveFilters()
                ? 'bg-primary/10 border-primary/20 text-primary'
                : 'bg-muted border-input text-foreground hover:bg-muted/80'
            }`}
          >
            <Filter className="h-4 w-4 mr-2" />
            Filters
            {hasActiveFilters() && (
              <span className="ml-2 px-2 py-0.5 bg-primary/10 text-primary text-xs rounded-full">
                {Object.values(filters).filter(v =>
                  Array.isArray(v) ? v.length > 0 : v !== undefined && v !== ''
                ).length}
              </span>
            )}
          </button>
          {isLoading && (
            <RefreshCw className="h-4 w-4 text-primary animate-spin" />
          )}
        </div>

        {/* Active Filter Tags */}
        {hasActiveFilters() && (
          <div className="flex flex-wrap items-center gap-2 mt-3">
            {filters.fileTypes?.map(type => (
              <span key={type} className="inline-flex items-center px-2 py-1 bg-info/10 text-info text-xs rounded-md">
                <FileType className="h-3 w-3 mr-1" />
                {type}
                <button
                  onClick={() => updateFilter('fileTypes', filters.fileTypes?.filter(t => t !== type))}
                  className="ml-1 hover:text-info/80"
                >
                  <X className="h-3 w-3" />
                </button>
              </span>
            ))}

            {filters.uploaderIds?.map(id => {
              const uploader = availableUploaders.find(u => u.id === id);
              return uploader ? (
                <span key={id} className="inline-flex items-center px-2 py-1 bg-success/10 text-success text-xs rounded-md">
                  <User className="h-3 w-3 mr-1" />
                  {uploader.name}
                  <button
                    onClick={() => updateFilter('uploaderIds', filters.uploaderIds?.filter(u => u !== id))}
                    className="ml-1 hover:text-success/80"
                  >
                    <X className="h-3 w-3" />
                  </button>
                </span>
              ) : null;
            })}

            {filters.tags?.map(tag => (
              <span key={tag} className="inline-flex items-center px-2 py-1 bg-primary/10 text-primary text-xs rounded-md">
                <Tag className="h-3 w-3 mr-1" />
                {tag}
                <button onClick={() => removeTag(tag)} className="ml-1 hover:text-primary/80">
                  <X className="h-3 w-3" />
                </button>
              </span>
            ))}

            <button
              onClick={clearFilters}
              className="text-xs text-muted-foreground hover:text-foreground underline"
            >
              Clear all
            </button>
          </div>
        )}
      </div>

      {/* Advanced Filters Panel */}
      {isAdvancedOpen && (
        <div className="p-4 bg-muted border-t border-border">
          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
            {/* File Types */}
            <div>
              <label className="block text-sm font-medium text-foreground mb-2">
                <FileType className="inline h-4 w-4 mr-1" />
                File Types
              </label>
              <div className="space-y-2 max-h-32 overflow-y-auto">
                {Object.entries(facets.fileTypes).map(([type, count]) => (
                  <label key={type} className="flex items-center">
                    <input
                      type="checkbox"
                      checked={filters.fileTypes?.includes(type) || false}
                      onChange={(e) => {
                        const newTypes = e.target.checked
                          ? [...(filters.fileTypes || []), type]
                          : filters.fileTypes?.filter(t => t !== type) || [];
                        updateFilter('fileTypes', newTypes);
                      }}
                      className="rounded text-primary focus:ring-ring"
                    />
                    <span className="ml-2 text-sm text-foreground">
                      {type} ({count})
                    </span>
                  </label>
                ))}
              </div>
            </div>

            {/* Uploaders */}
            <div>
              <label className="block text-sm font-medium text-foreground mb-2">
                <User className="inline h-4 w-4 mr-1" />
                Uploaded By
              </label>
              <div className="space-y-2 max-h-32 overflow-y-auto">
                {availableUploaders.map(uploader => (
                  <label key={uploader.id} className="flex items-center">
                    <input
                      type="checkbox"
                      checked={filters.uploaderIds?.includes(uploader.id) || false}
                      onChange={(e) => {
                        const newIds = e.target.checked
                          ? [...(filters.uploaderIds || []), uploader.id]
                          : filters.uploaderIds?.filter(id => id !== uploader.id) || [];
                        updateFilter('uploaderIds', newIds);
                      }}
                      className="rounded text-primary focus:ring-ring"
                    />
                    <span className="ml-2 text-sm text-foreground">
                      {uploader.name}
                    </span>
                  </label>
                ))}
              </div>
            </div>

            {/* Date Range */}
            <div>
              <label className="block text-sm font-medium text-foreground mb-2">
                <Calendar className="inline h-4 w-4 mr-1" />
                Date Range
              </label>
              <div className="space-y-2">
                <input
                  type="date"
                  value={filters.dateRange?.start || ''}
                  onChange={(e) => updateFilter('dateRange', {
                    ...filters.dateRange,
                    start: e.target.value
                  })}
                  className="w-full px-3 py-1 text-sm border border-input rounded focus:ring-ring focus:border-ring"
                  placeholder="Start date"
                />
                <input
                  type="date"
                  value={filters.dateRange?.end || ''}
                  onChange={(e) => updateFilter('dateRange', {
                    ...filters.dateRange,
                    end: e.target.value
                  })}
                  className="w-full px-3 py-1 text-sm border border-input rounded focus:ring-ring focus:border-ring"
                  placeholder="End date"
                />
              </div>
            </div>

            {/* File Size */}
            <div>
              <label className="block text-sm font-medium text-foreground mb-2">
                <HardDrive className="inline h-4 w-4 mr-1" />
                File Size (MB)
              </label>
              <div className="space-y-2">
                <input
                  type="number"
                  placeholder="Min size"
                  value={filters.sizeRange?.min || ''}
                  onChange={(e) => updateFilter('sizeRange', {
                    ...filters.sizeRange,
                    min: e.target.value ? parseInt(e.target.value) : undefined
                  })}
                  className="w-full px-3 py-1 text-sm border border-input rounded focus:ring-ring focus:border-ring"
                />
                <input
                  type="number"
                  placeholder="Max size"
                  value={filters.sizeRange?.max || ''}
                  onChange={(e) => updateFilter('sizeRange', {
                    ...filters.sizeRange,
                    max: e.target.value ? parseInt(e.target.value) : undefined
                  })}
                  className="w-full px-3 py-1 text-sm border border-input rounded focus:ring-ring focus:border-ring"
                />
              </div>
            </div>

            {/* Tags */}
            <div>
              <label className="block text-sm font-medium text-foreground mb-2">
                <Tag className="inline h-4 w-4 mr-1" />
                Tags
              </label>
              <div className="space-y-2">
                <input
                  type="text"
                  placeholder="Add tag and press Enter"
                  onKeyPress={(e) => {
                    if (e.key === 'Enter') {
                      addTag(e.currentTarget.value);
                      e.currentTarget.value = '';
                    }
                  }}
                  className="w-full px-3 py-1 text-sm border border-input rounded focus:ring-ring focus:border-ring"
                />
                {filters.tags && filters.tags.length > 0 && (
                  <div className="flex flex-wrap gap-1">
                    {filters.tags.map(tag => (
                      <span key={tag} className="inline-flex items-center px-2 py-1 bg-primary/10 text-primary text-xs rounded">
                        {tag}
                        <button onClick={() => removeTag(tag)} className="ml-1 hover:text-primary/80">
                          <X className="h-3 w-3" />
                        </button>
                      </span>
                    ))}
                  </div>
                )}
              </div>
            </div>

            {/* Sort Options */}
            <div>
              <label className="block text-sm font-medium text-foreground mb-2">
                Sort By
              </label>
              <div className="space-y-2">
                <select
                  value={filters.sortBy || 'uploadedAt'}
                  onChange={(e) => updateFilter('sortBy', e.target.value)}
                  className="w-full px-3 py-1 text-sm border border-input rounded focus:ring-ring focus:border-ring"
                >
                  <option value="uploadedAt">Upload Date</option>
                  <option value="name">Name</option>
                  <option value="size">File Size</option>
                  <option value="downloadCount">Download Count</option>
                  <option value="lastAccessedAt">Last Accessed</option>
                </select>
                <div className="flex items-center space-x-2">
                  <button
                    onClick={() => updateFilter('sortOrder', 'asc')}
                    className={`flex items-center px-2 py-1 text-xs rounded ${
                      filters.sortOrder === 'asc'
                        ? 'bg-primary/10 text-primary'
                        : 'bg-muted text-foreground'
                    }`}
                  >
                    <SortAsc className="h-3 w-3 mr-1" />
                    Ascending
                  </button>
                  <button
                    onClick={() => updateFilter('sortOrder', 'desc')}
                    className={`flex items-center px-2 py-1 text-xs rounded ${
                      filters.sortOrder === 'desc'
                        ? 'bg-primary/10 text-primary'
                        : 'bg-muted text-foreground'
                    }`}
                  >
                    <SortDesc className="h-3 w-3 mr-1" />
                    Descending
                  </button>
                </div>
              </div>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}