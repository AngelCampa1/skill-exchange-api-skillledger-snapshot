/**
 * ConversationList - List of conversations with search and filtering
 */

import React, { useState, useMemo } from 'react'
import { Search, RefreshCw, MessageSquare, Archive } from 'lucide-react'
import { Input } from '../ui/input'
import { Button } from '../ui/button'
import { Skeleton } from '../ui/skeleton'
import { ConversationListItem } from './ConversationListItem'
import { ConversationPreview, WorkspaceStatus } from '@/types/conversations'

interface ConversationListProps {
  conversations: ConversationPreview[]
  selectedId: string | null
  onSelect: (id: string) => void
  isLoading?: boolean
  isRefreshing?: boolean
  onRefresh?: () => void
  className?: string
}

/**
 * Skeleton loader for conversation items
 */
const ConversationSkeleton: React.FC = () => (
  <div className="p-3 flex items-start gap-3 border-b border-border">
    <Skeleton variant="circular" className="h-10 w-10 flex-shrink-0" />
    <div className="flex-1 space-y-2">
      <div className="flex justify-between">
        <Skeleton variant="text" className="h-4 w-32" />
        <Skeleton variant="text" className="h-3 w-16" />
      </div>
      <Skeleton variant="text" className="h-3 w-24" />
      <Skeleton variant="text" className="h-3 w-full" />
    </div>
  </div>
)

/**
 * Empty state component
 */
const EmptyState: React.FC<{ hasSearch: boolean }> = ({ hasSearch }) => (
  <div className="flex flex-col items-center justify-center p-8 text-center">
    <div className="w-16 h-16 rounded-full bg-muted flex items-center justify-center mb-4">
      <MessageSquare className="h-8 w-8 text-muted-foreground" />
    </div>
    {hasSearch ? (
      <>
        <h3 className="text-sm font-semibold text-foreground mb-1">
          No conversations found
        </h3>
        <p className="text-xs text-muted-foreground">
          Try adjusting your search terms
        </p>
      </>
    ) : (
      <>
        <h3 className="text-sm font-semibold text-foreground mb-1">
          No conversations yet
        </h3>
        <p className="text-xs text-muted-foreground">
          Start a project to begin a conversation
        </p>
      </>
    )}
  </div>
)

export const ConversationList: React.FC<ConversationListProps> = ({
  conversations,
  selectedId,
  onSelect,
  isLoading = false,
  isRefreshing = false,
  onRefresh,
  className = '',
}) => {
  const [searchQuery, setSearchQuery] = useState('')

  // Filter conversations based on search query
  const filteredConversations = useMemo(() => {
    if (!searchQuery.trim()) return conversations

    const query = searchQuery.toLowerCase()
    return conversations.filter(
      (c) =>
        c.projectTitle.toLowerCase().includes(query) ||
        c.otherParticipantName.toLowerCase().includes(query)
    )
  }, [conversations, searchQuery])

  // Separate active and archived conversations
  const activeConversations = useMemo(
    () => filteredConversations.filter((c) => c.status === WorkspaceStatus.Active),
    [filteredConversations]
  )

  const archivedConversations = useMemo(
    () => filteredConversations.filter((c) => c.status === WorkspaceStatus.Archived),
    [filteredConversations]
  )

  const hasArchivedConversations = archivedConversations.length > 0

  // Handle search input change
  const handleSearchChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    setSearchQuery(e.target.value)
  }

  // Loading state
  if (isLoading) {
    return (
      <div className={`flex flex-col h-full ${className}`}>
        {/* Search skeleton */}
        <div className="p-3 border-b border-border">
          <Skeleton variant="rectangular" className="h-9 w-full rounded-md" />
        </div>
        {/* Conversation skeletons */}
        <div className="flex-1 overflow-y-auto">
          {[1, 2, 3, 4, 5].map((i) => (
            <ConversationSkeleton key={i} />
          ))}
        </div>
      </div>
    )
  }

  return (
    <div className={`flex flex-col h-full ${className}`}>
      {/* Search header */}
      <div className="p-3 border-b border-border">
        <div className="relative flex items-center gap-2">
          <div className="relative flex-1">
            <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-muted-foreground" />
            <Input
              type="text"
              placeholder="Search conversations..."
              value={searchQuery}
              onChange={handleSearchChange}
              className="pl-9 h-9"
            />
          </div>
          {onRefresh && (
            <Button
              variant="ghost"
              size="icon"
              onClick={onRefresh}
              disabled={isRefreshing}
              className="h-9 w-9 flex-shrink-0"
              aria-label="Refresh conversations"
            >
              <RefreshCw
                className={`h-4 w-4 ${isRefreshing ? 'animate-spin' : ''}`}
              />
            </Button>
          )}
        </div>
      </div>

      {/* Conversation list */}
      <div className="flex-1 overflow-y-auto" role="list">
        {filteredConversations.length === 0 ? (
          <EmptyState hasSearch={searchQuery.length > 0} />
        ) : (
          <>
            {/* Active conversations */}
            {activeConversations.length > 0 && (
              <div>
                {hasArchivedConversations && (
                  <div className="px-3 py-2 bg-muted/50 border-b border-border">
                    <span className="text-xs font-medium text-muted-foreground uppercase tracking-wider">
                      Active
                    </span>
                  </div>
                )}
                {activeConversations.map((conversation) => (
                  <ConversationListItem
                    key={conversation.id}
                    conversation={conversation}
                    isSelected={selectedId === conversation.id}
                    onClick={onSelect}
                  />
                ))}
              </div>
            )}

            {/* Archived conversations */}
            {hasArchivedConversations && (
              <div>
                <div className="px-3 py-2 bg-muted/50 border-b border-border flex items-center gap-2">
                  <Archive className="h-3 w-3 text-muted-foreground" />
                  <span className="text-xs font-medium text-muted-foreground uppercase tracking-wider">
                    Archived
                  </span>
                </div>
                {archivedConversations.map((conversation) => (
                  <ConversationListItem
                    key={conversation.id}
                    conversation={conversation}
                    isSelected={selectedId === conversation.id}
                    onClick={onSelect}
                  />
                ))}
              </div>
            )}
          </>
        )}
      </div>
    </div>
  )
}

ConversationList.displayName = 'ConversationList'
