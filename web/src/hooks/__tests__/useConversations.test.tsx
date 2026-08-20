/**
 * useConversations Hook Tests
 *
 * TDD approach: These tests are written BEFORE the implementation.
 * They should fail initially (RED), then pass after implementation (GREEN).
 */

import React from 'react'
import { renderHook, act, waitFor } from '@testing-library/react'
import { useConversations } from '../useConversations'
import { messagingApiService } from '@/services/messagingApiService'
import { ConversationPreview, WorkspaceStatus } from '@/types/conversations'

// Mock the messaging API service
jest.mock('@/services/messagingApiService', () => ({
  messagingApiService: {
    getMyWorkspaces: jest.fn(),
    getWorkspaceDetails: jest.fn(),
    getUnreadCount: jest.fn(),
  },
}))

// Mock AuthContext
jest.mock('@/contexts/AuthContext', () => ({
  useAuth: jest.fn(() => ({
    user: { id: 'user-123', email: 'test@example.com' },
    isAuthenticated: true,
    isLoading: false,
  })),
}))

const mockConversations: ConversationPreview[] = [
  {
    id: 'workspace-1',
    projectTitle: 'Website Redesign',
    otherParticipantName: 'John Doe',
    status: WorkspaceStatus.Active,
    createdAt: '2024-01-01T00:00:00Z',
    lastActivity: '2024-01-15T10:30:00Z',
    isClient: true,
  },
  {
    id: 'workspace-2',
    projectTitle: 'Mobile App Development',
    otherParticipantName: 'Jane Smith',
    status: WorkspaceStatus.Active,
    createdAt: '2024-01-05T00:00:00Z',
    lastActivity: '2024-01-14T15:45:00Z',
    isClient: false,
  },
  {
    id: 'workspace-3',
    projectTitle: 'Logo Design',
    otherParticipantName: 'Bob Wilson',
    status: WorkspaceStatus.Archived,
    createdAt: '2023-12-01T00:00:00Z',
    lastActivity: '2023-12-20T09:00:00Z',
    isClient: true,
  },
]

describe('useConversations Hook', () => {
  beforeEach(() => {
    jest.clearAllMocks()
    ;(messagingApiService.getMyWorkspaces as jest.Mock).mockResolvedValue(mockConversations)
  })

  describe('Initial State', () => {
    it('should return initial loading state', () => {
      const { result } = renderHook(() => useConversations())

      expect(result.current.isLoading).toBe(true)
      expect(result.current.conversations).toEqual([])
      expect(result.current.error).toBeNull()
    })

    it('should return empty selected conversation initially', () => {
      const { result } = renderHook(() => useConversations())

      expect(result.current.selectedId).toBeNull()
    })
  })

  describe('Fetching Conversations', () => {
    it('should fetch conversations on mount', async () => {
      const { result } = renderHook(() => useConversations())

      await waitFor(() => {
        expect(result.current.isLoading).toBe(false)
      })

      expect(messagingApiService.getMyWorkspaces).toHaveBeenCalledTimes(1)
      expect(result.current.conversations).toEqual(mockConversations)
    })

    it('should set loading to false after fetch completes', async () => {
      const { result } = renderHook(() => useConversations())

      expect(result.current.isLoading).toBe(true)

      await waitFor(() => {
        expect(result.current.isLoading).toBe(false)
      })
    })

    it('should handle API errors gracefully', async () => {
      const errorMessage = 'Network error'
      ;(messagingApiService.getMyWorkspaces as jest.Mock).mockRejectedValue(
        new Error(errorMessage)
      )

      const { result } = renderHook(() => useConversations())

      await waitFor(() => {
        expect(result.current.isLoading).toBe(false)
      })

      expect(result.current.error).toBe(errorMessage)
      expect(result.current.conversations).toEqual([])
    })

    it('should handle empty conversations list', async () => {
      ;(messagingApiService.getMyWorkspaces as jest.Mock).mockResolvedValue([])

      const { result } = renderHook(() => useConversations())

      await waitFor(() => {
        expect(result.current.isLoading).toBe(false)
      })

      expect(result.current.conversations).toEqual([])
      expect(result.current.error).toBeNull()
    })
  })

  describe('Conversation Selection', () => {
    it('should update selectedId when selectConversation is called', async () => {
      const { result } = renderHook(() => useConversations())

      await waitFor(() => {
        expect(result.current.isLoading).toBe(false)
      })

      act(() => {
        result.current.selectConversation('workspace-1')
      })

      expect(result.current.selectedId).toBe('workspace-1')
    })

    it('should clear selection when null is passed', async () => {
      const { result } = renderHook(() => useConversations())

      await waitFor(() => {
        expect(result.current.isLoading).toBe(false)
      })

      act(() => {
        result.current.selectConversation('workspace-1')
      })

      expect(result.current.selectedId).toBe('workspace-1')

      act(() => {
        result.current.selectConversation(null)
      })

      expect(result.current.selectedId).toBeNull()
    })

    it('should return selected conversation object', async () => {
      const { result } = renderHook(() => useConversations())

      await waitFor(() => {
        expect(result.current.isLoading).toBe(false)
      })

      act(() => {
        result.current.selectConversation('workspace-2')
      })

      expect(result.current.selectedConversation).toEqual(mockConversations[1])
    })

    it('should return null for selectedConversation when nothing selected', async () => {
      const { result } = renderHook(() => useConversations())

      await waitFor(() => {
        expect(result.current.isLoading).toBe(false)
      })

      expect(result.current.selectedConversation).toBeNull()
    })
  })

  describe('Refresh Functionality', () => {
    it('should refresh conversations when refresh is called', async () => {
      const { result } = renderHook(() => useConversations())

      await waitFor(() => {
        expect(result.current.isLoading).toBe(false)
      })

      expect(messagingApiService.getMyWorkspaces).toHaveBeenCalledTimes(1)

      await act(async () => {
        await result.current.refresh()
      })

      expect(messagingApiService.getMyWorkspaces).toHaveBeenCalledTimes(2)
    })

    it('should set refreshing state during refresh', async () => {
      // Make the API call take some time
      let resolvePromise: (value: ConversationPreview[]) => void
      ;(messagingApiService.getMyWorkspaces as jest.Mock).mockImplementation(
        () => new Promise(resolve => {
          resolvePromise = resolve
        })
      )

      const { result } = renderHook(() => useConversations())

      // Wait for initial load to start
      await waitFor(() => {
        expect(messagingApiService.getMyWorkspaces).toHaveBeenCalledTimes(1)
      })

      // Resolve initial load
      resolvePromise!(mockConversations)

      await waitFor(() => {
        expect(result.current.isLoading).toBe(false)
      })

      // Setup new promise for refresh
      ;(messagingApiService.getMyWorkspaces as jest.Mock).mockImplementation(
        () => new Promise(resolve => {
          resolvePromise = resolve
        })
      )

      // Start refresh - use async act to properly handle state updates
      await act(async () => {
        result.current.refresh()
        // Allow microtask queue to flush so setIsRefreshing(true) takes effect
        await Promise.resolve()
      })

      // Check refreshing state (may already be true from the act above, or check via waitFor)
      // Since the mock Promise is pending, isRefreshing should be true
      await waitFor(() => {
        expect(result.current.isRefreshing).toBe(true)
      }, { timeout: 1000 })

      // Resolve refresh
      resolvePromise!(mockConversations)

      await waitFor(() => {
        expect(result.current.isRefreshing).toBe(false)
      })
    })

    it('should preserve selection after refresh', async () => {
      const { result } = renderHook(() => useConversations())

      await waitFor(() => {
        expect(result.current.isLoading).toBe(false)
      })

      act(() => {
        result.current.selectConversation('workspace-1')
      })

      await act(async () => {
        await result.current.refresh()
      })

      expect(result.current.selectedId).toBe('workspace-1')
    })
  })

  describe('Filtering', () => {
    it('should return only active conversations by default', async () => {
      const { result } = renderHook(() => useConversations())

      await waitFor(() => {
        expect(result.current.isLoading).toBe(false)
      })

      const activeConversations = result.current.activeConversations

      expect(activeConversations.every(c => c.status === WorkspaceStatus.Active)).toBe(true)
      expect(activeConversations.length).toBe(2)
    })

    it('should provide archived conversations separately', async () => {
      const { result } = renderHook(() => useConversations())

      await waitFor(() => {
        expect(result.current.isLoading).toBe(false)
      })

      const archivedConversations = result.current.archivedConversations

      expect(archivedConversations.every(c => c.status === WorkspaceStatus.Archived)).toBe(true)
      expect(archivedConversations.length).toBe(1)
    })
  })

  describe('Sorting', () => {
    it('should sort conversations by last activity (most recent first)', async () => {
      const { result } = renderHook(() => useConversations())

      await waitFor(() => {
        expect(result.current.isLoading).toBe(false)
      })

      const conversations = result.current.conversations

      // Should be sorted by lastActivity descending
      for (let i = 0; i < conversations.length - 1; i++) {
        const currentActivity = new Date(conversations[i].lastActivity || conversations[i].createdAt)
        const nextActivity = new Date(conversations[i + 1].lastActivity || conversations[i + 1].createdAt)
        expect(currentActivity.getTime()).toBeGreaterThanOrEqual(nextActivity.getTime())
      }
    })
  })

  describe('Hook Stability', () => {
    it('should return stable function references', async () => {
      const { result, rerender } = renderHook(() => useConversations())

      await waitFor(() => {
        expect(result.current.isLoading).toBe(false)
      })

      const firstSelectConversation = result.current.selectConversation
      const firstRefresh = result.current.refresh

      rerender()

      expect(result.current.selectConversation).toBe(firstSelectConversation)
      expect(result.current.refresh).toBe(firstRefresh)
    })
  })

  describe('Edge Cases', () => {
    it('should handle undefined lastActivity', async () => {
      const conversationsWithoutActivity = [
        {
          ...mockConversations[0],
          lastActivity: undefined,
        },
      ]
      ;(messagingApiService.getMyWorkspaces as jest.Mock).mockResolvedValue(
        conversationsWithoutActivity
      )

      const { result } = renderHook(() => useConversations())

      await waitFor(() => {
        expect(result.current.isLoading).toBe(false)
      })

      expect(result.current.conversations.length).toBe(1)
    })

    it('should handle multiple rapid refresh calls', async () => {
      const { result } = renderHook(() => useConversations())

      await waitFor(() => {
        expect(result.current.isLoading).toBe(false)
      })

      // Trigger multiple refreshes rapidly
      await act(async () => {
        result.current.refresh()
        result.current.refresh()
        result.current.refresh()
      })

      // Should handle gracefully without errors
      expect(result.current.error).toBeNull()
    })
  })
})
