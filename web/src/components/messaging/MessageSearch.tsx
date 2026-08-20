import { logger } from '@/utils/logger';
/**
 * MessageSearch - Full-text search through message history with highlighting
 */

import React, { useState, useEffect, useRef, useCallback, useMemo } from 'react';
import { Search, X, Clock, User, FileText, Image as ImageIcon, Mic } from 'lucide-react';
import { format } from 'date-fns';
import DOMPurify from 'dompurify';
import { Button } from '../ui/button';
import {
  Message,
  SearchMessagesRequest,
  MessageType,
  MessageSearchResult
} from '../../types/messaging';
import { messagingApiService } from '../../services/messagingApiService';

interface MessageSearchProps {
  workspaceId: string;
  onMessageSelect: (message: Message) => void;
  onClose?: () => void;
  /**
   * BUG-FE-020 FIX: Make debounce delay configurable
   * Default: 300ms (good balance between responsiveness and performance)
   */
  debounceDelay?: number;
}

/**
 * HighlightedText - Wrapper component for safely rendering HTML with search highlighting.
 * This is safe because the html prop is sanitized by highlightText() with multi-layer XSS protection.
 */
const HighlightedText: React.FC<{ html: string }> = ({ html }) => (
  // eslint-disable-next-line react/no-danger
  <div dangerouslySetInnerHTML={{ __html: html }} />
);

export const MessageSearch: React.FC<MessageSearchProps> = ({
  workspaceId,
  onMessageSelect,
  onClose,
  debounceDelay = 300
}) => {
  const [query, setQuery] = useState('');
  const [results, setResults] = useState<Message[]>([]);
  const [loading, setLoading] = useState(false);
  const [totalCount, setTotalCount] = useState(0);
  const [searchDuration, setSearchDuration] = useState('');
  const [selectedMessageType, setSelectedMessageType] = useState<MessageType | undefined>();
  const [dateFilter, setDateFilter] = useState<'today' | 'week' | 'month' | 'all'>('all');
  
  const searchInputRef = useRef<HTMLInputElement>(null);
  const searchTimeoutRef = useRef<NodeJS.Timeout | null>(null);

  // Focus search input on mount
  useEffect(() => {
    searchInputRef.current?.focus();
  }, []);

  const getDateRange = useCallback(() => {
    const now = new Date();
    switch (dateFilter) {
      case 'today':
        return {
          fromDate: new Date(now.getFullYear(), now.getMonth(), now.getDate()).toISOString()
        };
      case 'week':
        const weekAgo = new Date(now.getTime() - 7 * 24 * 60 * 60 * 1000);
        return { fromDate: weekAgo.toISOString() };
      case 'month':
        const monthAgo = new Date(now.getTime() - 30 * 24 * 60 * 60 * 1000);
        return { fromDate: monthAgo.toISOString() };
      default:
        return {};
    }
  }, [dateFilter]);

  const performSearch = useCallback(async () => {
    if (!query.trim()) return;

    setLoading(true);
    try {
      const request: SearchMessagesRequest = {
        workspaceId,
        query: query.trim(),
        pageSize: 20,
        messageType: selectedMessageType,
        ...getDateRange()
      };

      const response = await messagingApiService.searchMessages(request);
      setResults(response.messages);
      setTotalCount(response.totalCount);
      setSearchDuration(response.searchDuration);
    } catch (error) {
      logger.error('Search failed:', error);
      setResults([]);
      setTotalCount(0);
    } finally {
      setLoading(false);
    }
  }, [workspaceId, query, selectedMessageType, getDateRange]);

  /**
   * BUG-FE-020 FIX: Debounced search with configurable delay
   * Uses debounceDelay prop (default 300ms) for better performance control
   */
  useEffect(() => {
    if (searchTimeoutRef.current) {
      clearTimeout(searchTimeoutRef.current);
    }

    if (!query.trim()) {
      setResults([]);
      setTotalCount(0);
      setSearchDuration('');
      return;
    }

    searchTimeoutRef.current = setTimeout(() => {
      performSearch();
    }, debounceDelay);

    return () => {
      if (searchTimeoutRef.current) {
        clearTimeout(searchTimeoutRef.current);
      }
    };
  }, [query, selectedMessageType, dateFilter, performSearch, debounceDelay]);

  /**
   * BUG-CRIT-003 FIX: Highlight search query with multi-layer XSS protection
   *
   * Security layers:
   * 1. HTML entity escaping - Prevents all HTML injection from user content
   * 2. Regex escaping - Prevents ReDoS attacks from malicious search queries
   * 3. DOMPurify sanitization - Defense-in-depth with strict whitelist
   *
   * This function is safe to use with dangerouslySetInnerHTML
   */
  const highlightText = (text: string, searchQuery: string): string => {
    if (!searchQuery.trim() || !text) return text;

    // LAYER 1: Escape all HTML entities to prevent XSS injection
    const escaped = text
      .replace(/&/g, '&amp;')
      .replace(/</g, '&lt;')
      .replace(/>/g, '&gt;')
      .replace(/"/g, '&quot;')
      .replace(/'/g, '&#039;');

    // LAYER 2: Escape regex special characters to prevent ReDoS, then add safe highlighting
    const regex = new RegExp(`(${searchQuery.replace(/[.*+?^${}()|[\]\\]/g, '\\$&')})`, 'gi');
    const highlighted = escaped.replace(regex, '<mark class="bg-warning/30">$1</mark>');

    // LAYER 3: DOMPurify sanitization with strict whitelist (defense-in-depth)
    // Only allows <mark> and <span> tags with class attributes
    return DOMPurify.sanitize(highlighted, {
      ALLOWED_TAGS: ['mark', 'span'],
      ALLOWED_ATTR: ['class'],
    });
  };

  const getMessageTypeIcon = (messageType: MessageType) => {
    switch (messageType) {
      case MessageType.Image:
        return <ImageIcon className="h-4 w-4 text-info" />;
      case MessageType.File:
        return <FileText className="h-4 w-4 text-success" />;
      case MessageType.Voice:
        return <Mic className="h-4 w-4 text-primary" />;
      default:
        return null;
    }
  };

  const clearSearch = () => {
    setQuery('');
    setResults([]);
    setTotalCount(0);
    setSearchDuration('');
    searchInputRef.current?.focus();
  };

  return (
    <div className="p-4 space-y-4 bg-card">
      {/* Search Input */}
      <div className="relative">
        <Search className="absolute left-3 top-1/2 transform -translate-y-1/2 text-muted-foreground h-4 w-4" />
        <input
          ref={searchInputRef}
          type="text"
          value={query}
          onChange={(e) => setQuery(e.target.value)}
          placeholder="Search messages..."
          aria-label="Search messages"
          className="w-full pl-10 pr-10 py-2 border border-input rounded-lg focus:outline-none focus:ring-2 focus:ring-ring focus:border-transparent bg-background"
        />
        {query && (
          <Button
            size="icon"
            variant="ghost"
            onClick={clearSearch}
            aria-label="Clear search"
            className="absolute right-1 top-1/2 transform -translate-y-1/2 h-8 w-8"
          >
            <X className="h-4 w-4" />
          </Button>
        )}
      </div>

      {/* Filters */}
      <div className="flex items-center space-x-4">
        {/* Message Type Filter */}
        <div className="flex items-center space-x-2">
          <label className="text-sm text-muted-foreground">Type:</label>
          <select
            value={selectedMessageType ?? ''}
            onChange={(e) => setSelectedMessageType(e.target.value ? Number(e.target.value) as MessageType : undefined)}
            className="text-sm border border-input rounded px-2 py-1 focus:outline-none focus:ring-2 focus:ring-ring bg-background"
          >
            <option value="">All types</option>
            <option value={MessageType.Text}>Text</option>
            <option value={MessageType.Image}>Images</option>
            <option value={MessageType.File}>Files</option>
            <option value={MessageType.Voice}>Voice</option>
          </select>
        </div>

        {/* Date Filter */}
        <div className="flex items-center space-x-2">
          <label className="text-sm text-muted-foreground">When:</label>
          <select
            value={dateFilter}
            onChange={(e) => setDateFilter(e.target.value as 'today' | 'week' | 'month' | 'all')}
            className="text-sm border border-input rounded px-2 py-1 focus:outline-none focus:ring-2 focus:ring-ring bg-background"
          >
            <option value="all">All time</option>
            <option value="today">Today</option>
            <option value="week">Last week</option>
            <option value="month">Last month</option>
          </select>
        </div>

        {/* Close button */}
        {onClose && (
          <Button
            size="icon"
            variant="ghost"
            onClick={onClose}
            className="ml-auto h-8 w-8"
          >
            <X className="h-4 w-4" />
          </Button>
        )}
      </div>

      {/* Search Results */}
      <div className="space-y-2">
        {/* Search Stats */}
        {query && (
          <div className="flex items-center justify-between text-sm text-muted-foreground">
            <span>
              {loading ? 'Searching...' : `${totalCount} results${searchDuration ? ` (${searchDuration})` : ''}`}
            </span>
            {totalCount > results.length && (
              <span>Showing first {results.length} results</span>
            )}
          </div>
        )}

        {/* Loading */}
        {loading && (
          <div className="flex items-center justify-center py-8">
            <div className="animate-spin rounded-full h-6 w-6 border-b-2 border-primary"></div>
          </div>
        )}

        {/* No Results */}
        {!loading && query && results.length === 0 && (
          <div className="text-center py-8 text-muted-foreground">
            <Search className="h-12 w-12 mx-auto mb-4 text-muted-foreground/50" />
            <p className="text-lg font-medium">No messages found</p>
            <p className="text-sm">Try adjusting your search terms or filters</p>
          </div>
        )}

        {/* Results List */}
        {results.length > 0 && (
          <div className="space-y-2 max-h-96 overflow-y-auto">
            {results.map(message => (
              <div
                key={message.id}
                onClick={() => onMessageSelect(message)}
                className="p-3 border border-border rounded-lg cursor-pointer hover:bg-muted transition-colors"
              >
                <div className="flex items-start space-x-3">
                  {/* Message type icon */}
                  <div className="flex-shrink-0 mt-1">
                    {getMessageTypeIcon(message.messageType)}
                  </div>

                  <div className="flex-1 min-w-0">
                    {/* Sender and timestamp */}
                    <div className="flex items-center space-x-2 mb-1">
                      <span className="text-sm font-medium text-foreground">
                        {message.senderName}
                      </span>
                      <div className="flex items-center text-xs text-muted-foreground">
                        <Clock className="h-3 w-3 mr-1" />
                        {format(new Date(message.createdAt), 'MMM d, h:mm a')}
                      </div>
                    </div>

                    {/* Message content with highlighting */}
                    <div className="text-sm text-foreground">
                      {message.messageText ? (
                        /*
                         * BUG-CRIT-003 FIX: Safe to use dangerouslySetInnerHTML - highlightText() provides multi-layer XSS protection:
                         * 1. HTML entity escaping (all user content)
                         * 2. DOMPurify sanitization with strict whitelist (only <mark> and <span> with class attribute)
                         * 3. Regex escaping of search query to prevent ReDoS
                         */
                        <HighlightedText
                          html={highlightText(message.messageText, query)}
                        />
                      ) : (
                        <div className="flex items-center text-muted-foreground italic">
                          {getMessageTypeIcon(message.messageType)}
                          <span className="ml-2">
                            {message.messageType === MessageType.Image ? 'Image' :
                             message.messageType === MessageType.File ? message.attachmentFileName || 'File' :
                             message.messageType === MessageType.Voice ? 'Voice message' :
                             'Message'}
                          </span>
                        </div>
                      )}
                    </div>

                    {/* File name for attachments */}
                    {message.attachmentFileName && message.messageText && (
                      <div className="text-xs text-primary mt-1">
                        📎 {message.attachmentFileName}
                      </div>
                    )}
                  </div>
                </div>
              </div>
            ))}
          </div>
        )}
      </div>
    </div>
  );
};