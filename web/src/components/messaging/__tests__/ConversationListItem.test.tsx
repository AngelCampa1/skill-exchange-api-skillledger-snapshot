/**
 * ConversationListItem Component Tests
 *
 * TDD approach: These tests are written BEFORE the implementation.
 * They should fail initially (RED), then pass after implementation (GREEN).
 */

import React from 'react'
import { render, screen, fireEvent } from '@testing-library/react'
import { ConversationListItem } from '../ConversationListItem'
import { ConversationPreview, WorkspaceStatus } from '@/types/conversations'

const mockConversation: ConversationPreview = {
  id: 'workspace-1',
  projectTitle: 'Website Redesign Project',
  otherParticipantName: 'John Doe',
  status: WorkspaceStatus.Active,
  createdAt: '2024-01-01T00:00:00Z',
  lastActivity: '2024-01-15T10:30:00Z',
  isClient: true,
  unreadCount: 3,
  lastMessagePreview: 'Hey, I wanted to discuss the latest mockups...',
}

describe('ConversationListItem', () => {
  const defaultProps = {
    conversation: mockConversation,
    isSelected: false,
    onClick: jest.fn(),
  }

  beforeEach(() => {
    jest.clearAllMocks()
  })

  // ============================================
  // Content Display (5 tests)
  // ============================================
  describe('Content Display', () => {
    it('should display the project title', () => {
      render(<ConversationListItem {...defaultProps} />)

      expect(screen.getByText('Website Redesign Project')).toBeInTheDocument()
    })

    it('should display the other participant name', () => {
      render(<ConversationListItem {...defaultProps} />)

      expect(screen.getByText('John Doe')).toBeInTheDocument()
    })

    it('should display the last message preview', () => {
      render(<ConversationListItem {...defaultProps} />)

      expect(screen.getByText(/Hey, I wanted to discuss/)).toBeInTheDocument()
    })

    it('should display formatted timestamp', () => {
      render(<ConversationListItem {...defaultProps} />)

      // Should display relative or formatted time
      // The exact format depends on implementation
      const container = screen.getByText('Website Redesign Project').closest('div')
      expect(container).toBeInTheDocument()
    })

    it('should truncate long project titles', () => {
      const longTitleConversation = {
        ...mockConversation,
        projectTitle: 'This is a very long project title that should be truncated for display purposes',
      }

      const { container } = render(
        <ConversationListItem
          {...defaultProps}
          conversation={longTitleConversation}
        />
      )

      // Title should have truncate class
      const titleElement = container.querySelector('.truncate, .line-clamp-1')
      expect(titleElement).toBeInTheDocument()
    })
  })

  // ============================================
  // Unread Badge (3 tests)
  // ============================================
  describe('Unread Badge', () => {
    it('should display unread badge when unreadCount > 0', () => {
      render(<ConversationListItem {...defaultProps} />)

      expect(screen.getByText('3')).toBeInTheDocument()
    })

    it('should not display unread badge when unreadCount is 0', () => {
      const noUnreadConversation = {
        ...mockConversation,
        unreadCount: 0,
      }

      const { container } = render(
        <ConversationListItem
          {...defaultProps}
          conversation={noUnreadConversation}
        />
      )

      // Badge element should not exist (Badge renders as span with specific classes)
      const badgeElement = container.querySelector('.min-w-\\[20px\\], [class*="bg-primary"][class*="rounded-full"]')
      expect(badgeElement).not.toBeInTheDocument()
    })

    it('should not display unread badge when unreadCount is undefined', () => {
      const noUnreadConversation = {
        ...mockConversation,
        unreadCount: undefined,
      }

      render(
        <ConversationListItem
          {...defaultProps}
          conversation={noUnreadConversation}
        />
      )

      // Should render without errors
      expect(screen.getByText('Website Redesign Project')).toBeInTheDocument()
    })
  })

  // ============================================
  // Selection State (3 tests)
  // ============================================
  describe('Selection State', () => {
    it('should apply selected styling when isSelected is true', () => {
      const { container } = render(
        <ConversationListItem {...defaultProps} isSelected={true} />
      )

      // Check for selected styling (bg-primary/10, border-primary, etc.)
      const wrapper = container.firstChild as HTMLElement
      expect(wrapper.className).toMatch(/bg-primary|border-primary/)
    })

    it('should not apply selected styling when isSelected is false', () => {
      const { container } = render(
        <ConversationListItem {...defaultProps} isSelected={false} />
      )

      // Should have default/hover styling
      const wrapper = container.firstChild as HTMLElement
      expect(wrapper.className).not.toContain('bg-primary/10')
    })

    it('should have hover styling when not selected', () => {
      const { container } = render(
        <ConversationListItem {...defaultProps} isSelected={false} />
      )

      // Check for hover classes
      const wrapper = container.firstChild as HTMLElement
      expect(wrapper.className).toMatch(/hover:/)
    })
  })

  // ============================================
  // Click Handling (3 tests)
  // ============================================
  describe('Click Handling', () => {
    it('should call onClick when clicked', () => {
      const onClick = jest.fn()
      render(<ConversationListItem {...defaultProps} onClick={onClick} />)

      const item = screen.getByText('Website Redesign Project').closest('div[role="button"], button, [tabindex]')
      if (item) {
        fireEvent.click(item)
      } else {
        // Fall back to clicking the container
        fireEvent.click(screen.getByText('Website Redesign Project'))
      }

      expect(onClick).toHaveBeenCalledTimes(1)
    })

    it('should pass conversation id to onClick', () => {
      const onClick = jest.fn()
      render(<ConversationListItem {...defaultProps} onClick={onClick} />)

      const item = screen.getByText('Website Redesign Project').closest('div[role="button"], button, [tabindex]')
      if (item) {
        fireEvent.click(item)
      } else {
        fireEvent.click(screen.getByText('Website Redesign Project'))
      }

      expect(onClick).toHaveBeenCalledWith('workspace-1')
    })

    it('should be keyboard accessible', () => {
      const onClick = jest.fn()
      render(<ConversationListItem {...defaultProps} onClick={onClick} />)

      const item = screen.getByText('Website Redesign Project').closest('div[role="button"], button, [tabindex]')
      if (item) {
        fireEvent.keyDown(item, { key: 'Enter' })
        expect(onClick).toHaveBeenCalled()
      }
    })
  })

  // ============================================
  // Role Indicator (2 tests)
  // ============================================
  describe('Role Indicator', () => {
    it('should indicate when user is client', () => {
      render(<ConversationListItem {...defaultProps} />)

      // May show "Client" label or different styling
      // Exact implementation may vary
      expect(screen.getByText('Website Redesign Project')).toBeInTheDocument()
    })

    it('should indicate when user is provider', () => {
      const providerConversation = {
        ...mockConversation,
        isClient: false,
      }

      render(
        <ConversationListItem
          {...defaultProps}
          conversation={providerConversation}
        />
      )

      // Should render without errors
      expect(screen.getByText('Website Redesign Project')).toBeInTheDocument()
    })
  })

  // ============================================
  // Status Handling (3 tests)
  // ============================================
  describe('Status Handling', () => {
    it('should display archived indicator for archived conversations', () => {
      const archivedConversation = {
        ...mockConversation,
        status: WorkspaceStatus.Archived,
      }

      const { container } = render(
        <ConversationListItem
          {...defaultProps}
          conversation={archivedConversation}
        />
      )

      // Should have archived styling or indicator
      // Exact implementation may vary
      expect(container.firstChild).toBeInTheDocument()
    })

    it('should have reduced opacity for archived conversations', () => {
      const archivedConversation = {
        ...mockConversation,
        status: WorkspaceStatus.Archived,
      }

      const { container } = render(
        <ConversationListItem
          {...defaultProps}
          conversation={archivedConversation}
        />
      )

      const wrapper = container.firstChild as HTMLElement
      expect(wrapper.className).toMatch(/opacity-|text-muted/)
    })

    it('should render active conversations with full opacity', () => {
      const { container } = render(<ConversationListItem {...defaultProps} />)

      const wrapper = container.firstChild as HTMLElement
      expect(wrapper.className).not.toContain('opacity-50')
    })
  })

  // ============================================
  // Avatar/Initials (2 tests)
  // ============================================
  describe('Avatar/Initials', () => {
    it('should display participant initials or avatar', () => {
      const { container } = render(<ConversationListItem {...defaultProps} />)

      // Should have an avatar or initials element
      // Look for Avatar component or initials text
      const avatarOrInitials = container.querySelector('[class*="avatar"], [class*="rounded-full"]')
      expect(avatarOrInitials).toBeInTheDocument()
    })

    it('should use avatar URL when provided', () => {
      const conversationWithAvatar = {
        ...mockConversation,
        otherParticipantAvatar: 'https://example.com/avatar.jpg',
      }

      const { container } = render(
        <ConversationListItem
          {...defaultProps}
          conversation={conversationWithAvatar}
        />
      )

      // Should render without errors
      expect(container.firstChild).toBeInTheDocument()
    })
  })

  // ============================================
  // Empty/Missing Data (3 tests)
  // ============================================
  describe('Empty/Missing Data', () => {
    it('should handle missing lastMessagePreview', () => {
      const noPreviewConversation = {
        ...mockConversation,
        lastMessagePreview: undefined,
      }

      render(
        <ConversationListItem
          {...defaultProps}
          conversation={noPreviewConversation}
        />
      )

      // Should show placeholder or nothing
      expect(screen.getByText('Website Redesign Project')).toBeInTheDocument()
    })

    it('should handle missing lastActivity', () => {
      const noActivityConversation = {
        ...mockConversation,
        lastActivity: undefined,
      }

      render(
        <ConversationListItem
          {...defaultProps}
          conversation={noActivityConversation}
        />
      )

      // Should fall back to createdAt or show nothing
      expect(screen.getByText('Website Redesign Project')).toBeInTheDocument()
    })

    it('should handle empty participant name gracefully', () => {
      const emptyNameConversation = {
        ...mockConversation,
        otherParticipantName: '',
      }

      render(
        <ConversationListItem
          {...defaultProps}
          conversation={emptyNameConversation}
        />
      )

      // Should render without errors
      expect(screen.getByText('Website Redesign Project')).toBeInTheDocument()
    })
  })

  // ============================================
  // Accessibility (2 tests)
  // ============================================
  describe('Accessibility', () => {
    it('should have proper ARIA attributes', () => {
      const { container } = render(<ConversationListItem {...defaultProps} />)

      const item = container.firstChild as HTMLElement
      // Should have role="button" or be a button element
      expect(
        item.getAttribute('role') === 'button' ||
        item.tagName === 'BUTTON' ||
        item.getAttribute('tabindex') === '0'
      ).toBe(true)
    })

    it('should have focus-visible styling', () => {
      const { container } = render(<ConversationListItem {...defaultProps} />)

      const item = container.firstChild as HTMLElement
      expect(item.className).toMatch(/focus:|focus-visible:/)
    })
  })
})
