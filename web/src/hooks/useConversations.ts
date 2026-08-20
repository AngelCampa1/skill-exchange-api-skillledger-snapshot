'use client'

/**
 * useConversations Hook
 *
 * React hook for managing conversations/workspaces state.
 * Fetches user's conversations and provides selection/filtering functionality.
 */

import { useState, useEffect, useCallback, useMemo } from 'react'
import { messagingApiService } from '@/services/messagingApiService'
import { ConversationPreview, WorkspaceStatus } from '@/types/conversations'

interface UseConversationsReturn {
  /** All conversations */
  conversations: ConversationPreview[]
  /** Only active conversations */
  activeConversations: ConversationPreview[]
  /** Only archived conversations */
  archivedConversations: ConversationPreview[]
  /** Currently selected conversation ID */
  selectedId: string | null
  /** Currently selected conversation object */
  selectedConversation: ConversationPreview | null
  /** Initial loading state */
  isLoading: boolean
  /** Refresh in progress */
  isRefreshing: boolean
  /** Error message if any */
  error: string | null
  /** Select a conversation by ID */
  selectConversation: (id: string | null) => void
  /** Refresh the conversations list */
  refresh: () => Promise<void>
}

/**
 * Sort conversations by last activity (most recent first)
 */
function sortByActivity(conversations: ConversationPreview[]): ConversationPreview[] {
  return [...conversations].sort((a, b) => {
    const aDate = new Date(a.lastActivity || a.createdAt)
    const bDate = new Date(b.lastActivity || b.createdAt)
    return bDate.getTime() - aDate.getTime()
  })
}

export function useConversations(): UseConversationsReturn {
  const [conversations, setConversations] = useState<ConversationPreview[]>([])
  const [selectedId, setSelectedId] = useState<string | null>(null)
  const [isLoading, setIsLoading] = useState(true)
  const [isRefreshing, setIsRefreshing] = useState(false)
  const [error, setError] = useState<string | null>(null)

  /**
   * Fetch conversations from the API
   */
  const fetchConversations = useCallback(async (isRefresh = false) => {
    try {
      if (isRefresh) {
        setIsRefreshing(true)
      } else {
        setIsLoading(true)
      }
      setError(null)

      const data = await messagingApiService.getMyWorkspaces()
      const sorted = sortByActivity(data)
      setConversations(sorted)
    } catch (err) {
      const message = err instanceof Error ? err.message : 'Failed to load conversations'
      setError(message)
      if (!isRefresh) {
        setConversations([])
      }
    } finally {
      setIsLoading(false)
      setIsRefreshing(false)
    }
  }, [])

  /**
   * Fetch on mount
   */
  useEffect(() => {
    fetchConversations()
  }, [fetchConversations])

  /**
   * Select a conversation
   */
  const selectConversation = useCallback((id: string | null) => {
    setSelectedId(id)
  }, [])

  /**
   * Refresh conversations list
   */
  const refresh = useCallback(async () => {
    await fetchConversations(true)
  }, [fetchConversations])

  /**
   * Get selected conversation object
   */
  const selectedConversation = useMemo(() => {
    if (!selectedId) return null
    return conversations.find(c => c.id === selectedId) || null
  }, [selectedId, conversations])

  /**
   * Filter active conversations
   */
  const activeConversations = useMemo(() => {
    return conversations.filter(c => c.status === WorkspaceStatus.Active)
  }, [conversations])

  /**
   * Filter archived conversations
   */
  const archivedConversations = useMemo(() => {
    return conversations.filter(c => c.status === WorkspaceStatus.Archived)
  }, [conversations])

  return {
    conversations,
    activeConversations,
    archivedConversations,
    selectedId,
    selectedConversation,
    isLoading,
    isRefreshing,
    error,
    selectConversation,
    refresh,
  }
}
