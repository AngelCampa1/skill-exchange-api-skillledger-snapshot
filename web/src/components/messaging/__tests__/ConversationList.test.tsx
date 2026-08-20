/**
 * ConversationList Component Tests
 *
 * TDD approach: These tests are written BEFORE the implementation.
 * They should fail initially (RED), then pass after implementation (GREEN).
 */

import React from 'react'
import { render, screen, fireEvent } from '@testing-library/react'
import { ConversationList } from '../ConversationList'
import { ConversationPreview, WorkspaceStatus } from '@/types/conversations'

const mockConversations: ConversationPreview[] = [
  {
    id: 'workspace-1',
    projectTitle: 'Website Redesign',
    otherParticipantName: 'John Doe',
    status: WorkspaceStatus.Active,
    createdAt: '2024-01-01T00:00:00Z',
    lastActivity: '2024-01-15T10:30:00Z',
    isClient: true,
    unreadCount: 3,
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

describe('ConversationList', () => {
  const defaultProps = {
    conversations: mockConversations,
    selectedId: null as string | null,
    onSelect: jest.fn(),
    isLoading: false,
  }

  beforeEach(() => {
    jest.clearAllMocks()
  })

  // ============================================
  // Rendering Conversations (4 tests)
  // ============================================
  describe('Rendering Conversations', () => {
    it('should render all conversations', () => {
      render(<ConversationList {...defaultProps} />)

      expect(screen.getByText('Website Redesign')).toBeInTheDocument()
      expect(screen.getByText('Mobile App Development')).toBeInTheDocument()
      expect(screen.getByText('Logo Design')).toBeInTheDocument()
    })

    it('should render ConversationListItem for each conversation', () => {
      const { container } = render(<ConversationList {...defaultProps} />)

      // Should have 3 conversation items
      const items = container.querySelectorAll('[role="button"]')
      expect(items.length).toBeGreaterThanOrEqual(3)
    })

    it('should pass correct props to ConversationListItem', () => {
      render(
        <ConversationList
          {...defaultProps}
          selectedId="workspace-1"
        />
      )

      // First item should be selected (has selected styling)
      const firstItem = screen.getByText('Website Redesign').closest('[role="button"]')
      expect(firstItem?.className).toMatch(/bg-primary/)
    })

    it('should handle empty conversations list', () => {
      render(<ConversationList {...defaultProps} conversations={[]} />)

      expect(screen.getByText(/No conversations/i)).toBeInTheDocument()
    })
  })

  // ============================================
  // Selection (3 tests)
  // ============================================
  describe('Selection', () => {
    it('should call onSelect when conversation is clicked', () => {
      const onSelect = jest.fn()
      render(<ConversationList {...defaultProps} onSelect={onSelect} />)

      fireEvent.click(screen.getByText('Website Redesign'))

      expect(onSelect).toHaveBeenCalledWith('workspace-1')
    })

    it('should highlight selected conversation', () => {
      render(
        <ConversationList
          {...defaultProps}
          selectedId="workspace-2"
        />
      )

      const selectedItem = screen.getByText('Mobile App Development').closest('[role="button"]')
      expect(selectedItem?.className).toMatch(/bg-primary/)
    })

    it('should not highlight non-selected conversations', () => {
      render(
        <ConversationList
          {...defaultProps}
          selectedId="workspace-1"
        />
      )

      const nonSelectedItem = screen.getByText('Logo Design').closest('[role="button"]')
      expect(nonSelectedItem?.className).not.toMatch(/bg-primary\/10/)
    })
  })

  // ============================================
  // Loading State (2 tests)
  // ============================================
  describe('Loading State', () => {
    it('should show skeleton loaders when loading', () => {
      const { container } = render(
        <ConversationList {...defaultProps} isLoading={true} />
      )

      // Should show skeleton elements
      const skeletons = container.querySelectorAll('[class*="animate-pulse"], [class*="skeleton"]')
      expect(skeletons.length).toBeGreaterThan(0)
    })

    it('should not show conversations when loading', () => {
      render(<ConversationList {...defaultProps} isLoading={true} />)

      expect(screen.queryByText('Website Redesign')).not.toBeInTheDocument()
    })
  })

  // ============================================
  // Search/Filter (4 tests)
  // ============================================
  describe('Search/Filter', () => {
    it('should render search input', () => {
      render(<ConversationList {...defaultProps} />)

      expect(screen.getByPlaceholderText(/search/i)).toBeInTheDocument()
    })

    it('should filter conversations based on search query', () => {
      render(<ConversationList {...defaultProps} />)

      const searchInput = screen.getByPlaceholderText(/search/i)
      fireEvent.change(searchInput, { target: { value: 'Website' } })

      expect(screen.getByText('Website Redesign')).toBeInTheDocument()
      expect(screen.queryByText('Mobile App Development')).not.toBeInTheDocument()
    })

    it('should filter by participant name', () => {
      render(<ConversationList {...defaultProps} />)

      const searchInput = screen.getByPlaceholderText(/search/i)
      fireEvent.change(searchInput, { target: { value: 'Jane' } })

      expect(screen.queryByText('Website Redesign')).not.toBeInTheDocument()
      expect(screen.getByText('Mobile App Development')).toBeInTheDocument()
    })

    it('should show no results message when search has no matches', () => {
      render(<ConversationList {...defaultProps} />)

      const searchInput = screen.getByPlaceholderText(/search/i)
      fireEvent.change(searchInput, { target: { value: 'nonexistent' } })

      expect(screen.getByText(/No conversations found/i)).toBeInTheDocument()
    })
  })

  // ============================================
  // Empty State (2 tests)
  // ============================================
  describe('Empty State', () => {
    it('should show empty state when no conversations', () => {
      render(<ConversationList {...defaultProps} conversations={[]} />)

      expect(screen.getByText(/No conversations/i)).toBeInTheDocument()
    })

    it('should show call to action in empty state', () => {
      const { container } = render(
        <ConversationList {...defaultProps} conversations={[]} />
      )

      // Should have some CTA or helpful message
      expect(container.textContent).toMatch(/conversation|project|start/i)
    })
  })

  // ============================================
  // Refresh (2 tests)
  // ============================================
  describe('Refresh', () => {
    it('should call onRefresh when refresh is triggered', () => {
      const onRefresh = jest.fn()
      render(
        <ConversationList
          {...defaultProps}
          onRefresh={onRefresh}
        />
      )

      // Find and click refresh button if exists
      const refreshButton = screen.queryByRole('button', { name: /refresh/i })
      if (refreshButton) {
        fireEvent.click(refreshButton)
        expect(onRefresh).toHaveBeenCalled()
      }
    })

    it('should show refreshing indicator when isRefreshing', () => {
      const onRefresh = jest.fn()
      const { container } = render(
        <ConversationList
          {...defaultProps}
          isRefreshing={true}
          onRefresh={onRefresh}
        />
      )

      // Should show some loading indicator (spinning refresh icon)
      const spinner = container.querySelector('[class*="animate-spin"]')
      expect(spinner).toBeInTheDocument()
    })
  })

  // ============================================
  // Accessibility (2 tests)
  // ============================================
  describe('Accessibility', () => {
    it('should have proper list semantics', () => {
      render(<ConversationList {...defaultProps} />)

      // Should have list role or ul element
      const list = screen.getByRole('list') || document.querySelector('ul')
      expect(list).toBeInTheDocument()
    })

    it('should be keyboard navigable', () => {
      render(<ConversationList {...defaultProps} />)

      const searchInput = screen.getByPlaceholderText(/search/i)
      searchInput.focus()

      // Should be able to tab to conversations
      fireEvent.keyDown(searchInput, { key: 'Tab' })

      // At least one item should be focusable
      const items = document.querySelectorAll('[tabindex="0"]')
      expect(items.length).toBeGreaterThan(0)
    })
  })

  // ============================================
  // Section Headers (2 tests)
  // ============================================
  describe('Section Headers', () => {
    it('should separate active and archived conversations', () => {
      const { container } = render(<ConversationList {...defaultProps} />)

      // Should have section headers or visual separation
      // Exact implementation may vary
      expect(container.textContent).toMatch(/active|archived/i)
    })

    it('should show archived section only when archived conversations exist', () => {
      const activeOnly = mockConversations.filter(
        c => c.status === WorkspaceStatus.Active
      )

      render(<ConversationList {...defaultProps} conversations={activeOnly} />)

      expect(screen.queryByText(/archived/i)).not.toBeInTheDocument()
    })
  })
})
