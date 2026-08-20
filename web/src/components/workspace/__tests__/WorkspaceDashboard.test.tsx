/**
 * Tests for WorkspaceDashboard
 *
 * Comprehensive test suite for the workspace dashboard component
 * Coverage target: 90%+ (320 lines)
 */

import React from 'react'
import { render, screen, fireEvent, waitFor } from '@testing-library/react'
import '@testing-library/jest-dom'
import { WorkspaceDashboard } from '../WorkspaceDashboard'

// Mock logger
jest.mock('@/utils/logger', () => ({
  logger: {
    error: jest.fn(),
    debug: jest.fn(),
  },
}))

// Mock next/navigation
const mockPush = jest.fn()
jest.mock('next/navigation', () => ({
  useRouter: () => ({
    push: mockPush,
    replace: jest.fn(),
    prefetch: jest.fn(),
  }),
}))

const mockWorkspaceData = {
  workspaceId: 'workspace-123',
  projectTitle: 'Test Project',
  projectDescription: 'A test project description',
  clientName: 'John Doe',
  providerName: 'Jane Smith',
  status: 'Active' as const,
  createdAt: '2024-01-01T00:00:00Z',
  timelineData: JSON.stringify({ milestones: ['Phase 1', 'Phase 2'] }),
  milestoneData: JSON.stringify({ completed: 2, total: 5 }),
  integrationStatus: 'initialized',
  lastSyncedAt: '2024-01-10T12:00:00Z',
}

describe('WorkspaceDashboard', () => {
  let mockFetch: jest.Mock

  beforeEach(() => {
    mockFetch = jest.fn()
    global.fetch = mockFetch
    jest.clearAllMocks()
  })

  afterEach(() => {
    jest.restoreAllMocks()
  })

  describe('Loading State', () => {
    it('displays loading spinner while fetching workspace data', () => {
      mockFetch.mockImplementation(() => new Promise(() => {})) // Never resolves

      render(
        <WorkspaceDashboard
          workspaceId="workspace-123"
          currentUserId="user-1"
          isClient={true}
        />
      )

      expect(screen.getByText('Loading workspace...')).toBeInTheDocument()
      // Check for Loader2 icon via class or text
      const loadingIndicators = screen.getByText('Loading workspace...').parentElement
      expect(loadingIndicators).toBeInTheDocument()
    })
  })

  describe('Error State', () => {
    it('displays error message when API call fails', async () => {
      mockFetch.mockResolvedValueOnce({
        ok: false,
        status: 500,
      })

      render(
        <WorkspaceDashboard
          workspaceId="workspace-123"
          currentUserId="user-1"
          isClient={true}
        />
      )

      await waitFor(() => {
        expect(screen.getByText('Failed to load workspace data')).toBeInTheDocument()
      })

      expect(screen.getByRole('button', { name: /retry/i })).toBeInTheDocument()
    })

    it('displays error message when network error occurs', async () => {
      mockFetch.mockRejectedValueOnce(new Error('Network error'))

      render(
        <WorkspaceDashboard
          workspaceId="workspace-123"
          currentUserId="user-1"
          isClient={true}
        />
      )

      await waitFor(() => {
        expect(screen.getByText('An error occurred while loading workspace data')).toBeInTheDocument()
      })
    })

    it('retries fetching data when Retry button is clicked', async () => {
      // First call fails
      mockFetch.mockResolvedValueOnce({
        ok: false,
        status: 500,
      })

      render(
        <WorkspaceDashboard
          workspaceId="workspace-123"
          currentUserId="user-1"
          isClient={true}
        />
      )

      await waitFor(() => {
        expect(screen.getByText('Failed to load workspace data')).toBeInTheDocument()
      })

      // Second call succeeds
      mockFetch.mockResolvedValueOnce({
        ok: true,
        json: async () => mockWorkspaceData,
      })

      const retryButton = screen.getByText('Retry')
      fireEvent.click(retryButton)

      await waitFor(() => {
        expect(screen.getByText('Test Project')).toBeInTheDocument()
      })
    })
  })

  describe('No Data State', () => {
    it('displays "Workspace not found" when data is null', async () => {
      mockFetch.mockResolvedValueOnce({
        ok: true,
        json: async () => null,
      })

      render(
        <WorkspaceDashboard
          workspaceId="workspace-123"
          currentUserId="user-1"
          isClient={true}
        />
      )

      await waitFor(() => {
        expect(screen.getByText('Workspace not found')).toBeInTheDocument()
      })
    })
  })

  describe('Successful Data Load', () => {
    beforeEach(() => {
      mockFetch.mockResolvedValueOnce({
        ok: true,
        json: async () => mockWorkspaceData,
      })
    })

    it('renders workspace header with project details', async () => {
      render(
        <WorkspaceDashboard
          workspaceId="workspace-123"
          currentUserId="user-1"
          isClient={true}
        />
      )

      await waitFor(() => {
        expect(screen.getByText('Test Project')).toBeInTheDocument()
      })

      expect(screen.getByText('A test project description')).toBeInTheDocument()
      expect(screen.getByText(/Client:/)).toBeInTheDocument()
      expect(screen.getByText('John Doe')).toBeInTheDocument()
      expect(screen.getByText(/Provider:/)).toBeInTheDocument()
      expect(screen.getByText('Jane Smith')).toBeInTheDocument()
    })

    it('displays Active status badge', async () => {
      render(
        <WorkspaceDashboard
          workspaceId="workspace-123"
          currentUserId="user-1"
          isClient={true}
        />
      )

      await waitFor(() => {
        expect(screen.getByText('Active')).toBeInTheDocument()
      })
    })

    it('displays formatted creation date', async () => {
      render(
        <WorkspaceDashboard
          workspaceId="workspace-123"
          currentUserId="user-1"
          isClient={true}
        />
      )

      await waitFor(() => {
        expect(screen.getByText(/Created:/)).toBeInTheDocument()
      })

      // Check that date is formatted (will be locale-specific, just check for "Created:")
      expect(screen.getByText(/Created:/)).toBeInTheDocument()
    })
  })

  describe('Status Badges', () => {
    it('displays Archived status badge correctly', async () => {
      mockFetch.mockResolvedValueOnce({
        ok: true,
        json: async () => ({
          ...mockWorkspaceData,
          status: 'Archived',
        }),
      })

      render(
        <WorkspaceDashboard
          workspaceId="workspace-123"
          currentUserId="user-1"
          isClient={true}
        />
      )

      await waitFor(() => {
        expect(screen.getByText('Archived')).toBeInTheDocument()
      })
    })

    it('displays Deleted status badge correctly', async () => {
      mockFetch.mockResolvedValueOnce({
        ok: true,
        json: async () => ({
          ...mockWorkspaceData,
          status: 'Deleted',
        }),
      })

      render(
        <WorkspaceDashboard
          workspaceId="workspace-123"
          currentUserId="user-1"
          isClient={true}
        />
      )

      await waitFor(() => {
        expect(screen.getByText('Deleted')).toBeInTheDocument()
      })
    })

    it('displays default badge for unknown status', async () => {
      mockFetch.mockResolvedValueOnce({
        ok: true,
        json: async () => ({
          ...mockWorkspaceData,
          status: 'Unknown',
        }),
      })

      render(
        <WorkspaceDashboard
          workspaceId="workspace-123"
          currentUserId="user-1"
          isClient={true}
        />
      )

      await waitFor(() => {
        expect(screen.getByText('Unknown')).toBeInTheDocument()
      })
    })
  })

  describe('Timeline and Milestones', () => {
    beforeEach(() => {
      mockFetch.mockResolvedValueOnce({
        ok: true,
        json: async () => mockWorkspaceData,
      })
    })

    it('displays timeline data when available', async () => {
      render(
        <WorkspaceDashboard
          workspaceId="workspace-123"
          currentUserId="user-1"
          isClient={true}
        />
      )

      await waitFor(() => {
        expect(screen.getByText('Timeline')).toBeInTheDocument()
      })

      // Timeline data should be displayed as JSON
      expect(screen.getByText(/"milestones"/)).toBeInTheDocument()
    })

    it('displays milestone data when available', async () => {
      render(
        <WorkspaceDashboard
          workspaceId="workspace-123"
          currentUserId="user-1"
          isClient={true}
        />
      )

      await waitFor(() => {
        expect(screen.getByText('Milestones')).toBeInTheDocument()
      })

      // Milestone data should be displayed as JSON
      expect(screen.getByText(/"completed"/)).toBeInTheDocument()
    })

    it('displays "No timeline data available" when timeline is missing', async () => {
      const dataWithoutTimeline = {
        workspaceId: mockWorkspaceData.workspaceId,
        projectTitle: mockWorkspaceData.projectTitle,
        projectDescription: mockWorkspaceData.projectDescription,
        clientName: mockWorkspaceData.clientName,
        providerName: mockWorkspaceData.providerName,
        status: mockWorkspaceData.status,
        createdAt: mockWorkspaceData.createdAt,
        // timelineData is omitted (undefined)
        milestoneData: mockWorkspaceData.milestoneData,
        integrationStatus: mockWorkspaceData.integrationStatus,
        lastSyncedAt: mockWorkspaceData.lastSyncedAt,
      }

      // Clear beforeEach mock to use test-specific data
      mockFetch.mockReset()
      mockFetch.mockResolvedValueOnce({
        ok: true,
        json: async () => dataWithoutTimeline,
      })

      render(
        <WorkspaceDashboard
          workspaceId="workspace-123"
          currentUserId="user-1"
          isClient={true}
        />
      )

      await waitFor(() => {
        expect(screen.getByText(/No timeline data available/i)).toBeInTheDocument()
      })
    })

    it('displays "No milestone data available" when milestones are missing', async () => {
      const dataWithoutMilestones = {
        workspaceId: mockWorkspaceData.workspaceId,
        projectTitle: mockWorkspaceData.projectTitle,
        projectDescription: mockWorkspaceData.projectDescription,
        clientName: mockWorkspaceData.clientName,
        providerName: mockWorkspaceData.providerName,
        status: mockWorkspaceData.status,
        createdAt: mockWorkspaceData.createdAt,
        timelineData: mockWorkspaceData.timelineData,
        // milestoneData is omitted (undefined)
        integrationStatus: mockWorkspaceData.integrationStatus,
        lastSyncedAt: mockWorkspaceData.lastSyncedAt,
      }

      // Clear beforeEach mock to use test-specific data
      mockFetch.mockReset()
      mockFetch.mockResolvedValueOnce({
        ok: true,
        json: async () => dataWithoutMilestones,
      })

      render(
        <WorkspaceDashboard
          workspaceId="workspace-123"
          currentUserId="user-1"
          isClient={true}
        />
      )

      await waitFor(() => {
        expect(screen.getByText(/No milestone data available/i)).toBeInTheDocument()
      })
    })
  })

  describe('Safe JSON Parsing', () => {
    it('handles malformed JSON gracefully in timeline data', async () => {
      const { logger } = require('@/utils/logger')

      mockFetch.mockResolvedValueOnce({
        ok: true,
        json: async () => ({
          ...mockWorkspaceData,
          timelineData: 'invalid json {{{',
        }),
      })

      render(
        <WorkspaceDashboard
          workspaceId="workspace-123"
          currentUserId="user-1"
          isClient={true}
        />
      )

      await waitFor(() => {
        expect(screen.getByText('Timeline')).toBeInTheDocument()
      })

      // Should log error for failed parse
      await waitFor(() => {
        expect(logger.error).toHaveBeenCalledWith(
          'Failed to parse timelineData JSON',
          expect.any(Object)
        )
      })

      // Should render something (malformed JSON returns null, displays as {})
      expect(screen.getByText('Timeline')).toBeInTheDocument()
    })

    it('handles malformed JSON gracefully in milestone data', async () => {
      const { logger } = require('@/utils/logger')

      mockFetch.mockResolvedValueOnce({
        ok: true,
        json: async () => ({
          ...mockWorkspaceData,
          milestoneData: 'not valid json',
        }),
      })

      render(
        <WorkspaceDashboard
          workspaceId="workspace-123"
          currentUserId="user-1"
          isClient={true}
        />
      )

      await waitFor(() => {
        expect(screen.getByText('Milestones')).toBeInTheDocument()
      })

      // Should log error for failed parse
      await waitFor(() => {
        expect(logger.error).toHaveBeenCalledWith(
          'Failed to parse milestoneData JSON',
          expect.any(Object)
        )
      })
    })
  })

  describe('Update Timeline', () => {
    it('updates timeline when Update Timeline button is clicked', async () => {
      mockFetch
        .mockResolvedValueOnce({
          ok: true,
          json: async () => mockWorkspaceData,
        })
        .mockResolvedValueOnce({
          ok: true,
          json: async () => ({}),
        })
        .mockResolvedValueOnce({
          ok: true,
          json: async () => mockWorkspaceData,
        })

      render(
        <WorkspaceDashboard
          workspaceId="workspace-123"
          currentUserId="user-1"
          isClient={true}
        />
      )

      await waitFor(() => {
        expect(screen.getByText('Update Timeline')).toBeInTheDocument()
      })

      const updateButton = screen.getByText('Update Timeline')
      fireEvent.click(updateButton)

      await waitFor(() => {
        expect(mockFetch).toHaveBeenCalledWith(
          '/api/workspace/workspace-123/timeline',
          expect.objectContaining({
            method: 'PUT',
            headers: expect.objectContaining({
              'Content-Type': 'application/json',
            }),
            body: JSON.stringify({
              timelineData: { milestones: ['Milestone 1', 'Milestone 2'] },
            }),
          })
        )
      })
    })

    it('displays error when timeline update fails', async () => {
      mockFetch
        .mockResolvedValueOnce({
          ok: true,
          json: async () => mockWorkspaceData,
        })
        .mockResolvedValueOnce({
          ok: false,
          status: 500,
        })

      render(
        <WorkspaceDashboard
          workspaceId="workspace-123"
          currentUserId="user-1"
          isClient={true}
        />
      )

      await waitFor(() => {
        expect(screen.getByText('Update Timeline')).toBeInTheDocument()
      })

      const updateButton = screen.getByText('Update Timeline')
      fireEvent.click(updateButton)

      await waitFor(() => {
        expect(screen.getByText('Failed to update timeline')).toBeInTheDocument()
      })
    })

    it('displays error when timeline update throws exception', async () => {
      mockFetch
        .mockResolvedValueOnce({
          ok: true,
          json: async () => mockWorkspaceData,
        })
        .mockRejectedValueOnce(new Error('Network error'))

      render(
        <WorkspaceDashboard
          workspaceId="workspace-123"
          currentUserId="user-1"
          isClient={true}
        />
      )

      await waitFor(() => {
        expect(screen.getByText('Update Timeline')).toBeInTheDocument()
      })

      const updateButton = screen.getByText('Update Timeline')
      fireEvent.click(updateButton)

      await waitFor(() => {
        expect(screen.getByText('An error occurred while updating timeline')).toBeInTheDocument()
      })
    })
  })

  describe('Integration Status', () => {
    it('displays integration status as initialized', async () => {
      mockFetch.mockResolvedValueOnce({
        ok: true,
        json: async () => mockWorkspaceData,
      })

      render(
        <WorkspaceDashboard
          workspaceId="workspace-123"
          currentUserId="user-1"
          isClient={true}
        />
      )

      await waitFor(() => {
        expect(screen.getByText('Integration Status')).toBeInTheDocument()
      })

      expect(screen.getByText('initialized')).toBeInTheDocument()
    })

    it('displays Unknown when integration status is missing', async () => {
      mockFetch.mockResolvedValueOnce({
        ok: true,
        json: async () => ({
          ...mockWorkspaceData,
          integrationStatus: undefined,
        }),
      })

      render(
        <WorkspaceDashboard
          workspaceId="workspace-123"
          currentUserId="user-1"
          isClient={true}
        />
      )

      await waitFor(() => {
        expect(screen.getByText('Unknown')).toBeInTheDocument()
      })
    })

    it('displays last sync time when available', async () => {
      mockFetch.mockResolvedValueOnce({
        ok: true,
        json: async () => mockWorkspaceData,
      })

      render(
        <WorkspaceDashboard
          workspaceId="workspace-123"
          currentUserId="user-1"
          isClient={true}
        />
      )

      await waitFor(() => {
        expect(screen.getByText(/Last Sync:/)).toBeInTheDocument()
      })
    })

    it('does not display last sync when not available', async () => {
      mockFetch.mockResolvedValueOnce({
        ok: true,
        json: async () => ({
          ...mockWorkspaceData,
          lastSyncedAt: undefined,
        }),
      })

      render(
        <WorkspaceDashboard
          workspaceId="workspace-123"
          currentUserId="user-1"
          isClient={true}
        />
      )

      await waitFor(() => {
        expect(screen.getByText('Integration Status')).toBeInTheDocument()
      })

      expect(screen.queryByText(/Last Sync:/)).not.toBeInTheDocument()
    })
  })

  describe('Quick Actions - Navigation', () => {
    beforeEach(() => {
      mockFetch.mockResolvedValueOnce({
        ok: true,
        json: async () => mockWorkspaceData,
      })
      mockPush.mockClear()
    })

    it('navigates to messages when Messages button is clicked', async () => {
      render(
        <WorkspaceDashboard
          workspaceId="workspace-123"
          currentUserId="user-1"
          isClient={true}
        />
      )

      await waitFor(() => {
        expect(screen.getByText('Messages')).toBeInTheDocument()
      })

      const messagesButton = screen.getByText('Messages')
      fireEvent.click(messagesButton)

      expect(mockPush).toHaveBeenCalledWith('/workspace/workspace-123/messages')
    })

    it('navigates to files when Files button is clicked', async () => {
      render(
        <WorkspaceDashboard
          workspaceId="workspace-123"
          currentUserId="user-1"
          isClient={true}
        />
      )

      await waitFor(() => {
        expect(screen.getByText('Files')).toBeInTheDocument()
      })

      const filesButton = screen.getByText('Files')
      fireEvent.click(filesButton)

      expect(mockPush).toHaveBeenCalledWith('/workspace/workspace-123/files')
    })

    it('navigates to escrow when Escrow button is clicked', async () => {
      render(
        <WorkspaceDashboard
          workspaceId="workspace-123"
          currentUserId="user-1"
          isClient={true}
        />
      )

      await waitFor(() => {
        expect(screen.getByText('Escrow')).toBeInTheDocument()
      })

      const escrowButton = screen.getByText('Escrow')
      fireEvent.click(escrowButton)

      expect(mockPush).toHaveBeenCalledWith('/workspace/workspace-123/escrow')
    })
  })

  describe('Archive Workspace', () => {
    it('shows Archive button only for clients with Active workspace', async () => {
      mockFetch.mockResolvedValueOnce({
        ok: true,
        json: async () => mockWorkspaceData,
      })

      render(
        <WorkspaceDashboard
          workspaceId="workspace-123"
          currentUserId="user-1"
          isClient={true}
        />
      )

      await waitFor(() => {
        expect(screen.getByText('Archive Workspace')).toBeInTheDocument()
      })
    })

    it('does not show Archive button for providers', async () => {
      mockFetch.mockResolvedValueOnce({
        ok: true,
        json: async () => mockWorkspaceData,
      })

      render(
        <WorkspaceDashboard
          workspaceId="workspace-123"
          currentUserId="user-1"
          isClient={false}
        />
      )

      await waitFor(() => {
        expect(screen.getByText('Test Project')).toBeInTheDocument()
      })

      expect(screen.queryByText('Archive Workspace')).not.toBeInTheDocument()
    })

    it('does not show Archive button for Archived workspace', async () => {
      mockFetch.mockResolvedValueOnce({
        ok: true,
        json: async () => ({
          ...mockWorkspaceData,
          status: 'Archived',
        }),
      })

      render(
        <WorkspaceDashboard
          workspaceId="workspace-123"
          currentUserId="user-1"
          isClient={true}
        />
      )

      await waitFor(() => {
        expect(screen.getByText('Test Project')).toBeInTheDocument()
      })

      expect(screen.queryByText('Archive Workspace')).not.toBeInTheDocument()
    })

    it('shows confirmation dialog and archives workspace when confirmed', async () => {
      global.confirm = jest.fn(() => true)

      mockFetch
        .mockResolvedValueOnce({
          ok: true,
          json: async () => mockWorkspaceData,
        })
        .mockResolvedValueOnce({
          ok: true,
          json: async () => ({}),
        })
        .mockResolvedValueOnce({
          ok: true,
          json: async () => ({
            ...mockWorkspaceData,
            status: 'Archived',
          }),
        })

      render(
        <WorkspaceDashboard
          workspaceId="workspace-123"
          currentUserId="user-1"
          isClient={true}
        />
      )

      await waitFor(() => {
        expect(screen.getByText('Archive Workspace')).toBeInTheDocument()
      })

      const archiveButton = screen.getByText('Archive Workspace')
      fireEvent.click(archiveButton)

      expect(global.confirm).toHaveBeenCalledWith(
        'Are you sure you want to archive this workspace?'
      )

      await waitFor(() => {
        expect(mockFetch).toHaveBeenCalledWith(
          '/api/workspace/workspace-123/archive',
          expect.objectContaining({
            method: 'POST',
          })
        )
      })
    })

    it('does not archive when user cancels confirmation', async () => {
      global.confirm = jest.fn(() => false)

      mockFetch.mockResolvedValueOnce({
        ok: true,
        json: async () => mockWorkspaceData,
      })

      render(
        <WorkspaceDashboard
          workspaceId="workspace-123"
          currentUserId="user-1"
          isClient={true}
        />
      )

      await waitFor(() => {
        expect(screen.getByText('Archive Workspace')).toBeInTheDocument()
      })

      const archiveButton = screen.getByText('Archive Workspace')
      fireEvent.click(archiveButton)

      expect(global.confirm).toHaveBeenCalledWith(
        'Are you sure you want to archive this workspace?'
      )

      // Should not make archive API call (only 1 call for initial fetch)
      expect(mockFetch).toHaveBeenCalledTimes(1)
    })

    it('displays error when archive fails', async () => {
      global.confirm = jest.fn(() => true)

      mockFetch
        .mockResolvedValueOnce({
          ok: true,
          json: async () => mockWorkspaceData,
        })
        .mockResolvedValueOnce({
          ok: false,
          status: 500,
        })

      render(
        <WorkspaceDashboard
          workspaceId="workspace-123"
          currentUserId="user-1"
          isClient={true}
        />
      )

      await waitFor(() => {
        expect(screen.getByText('Archive Workspace')).toBeInTheDocument()
      })

      const archiveButton = screen.getByText('Archive Workspace')
      fireEvent.click(archiveButton)

      await waitFor(() => {
        expect(screen.getByText('Failed to archive workspace')).toBeInTheDocument()
      })
    })

    it('displays error when archive throws exception', async () => {
      global.confirm = jest.fn(() => true)

      mockFetch
        .mockResolvedValueOnce({
          ok: true,
          json: async () => mockWorkspaceData,
        })
        .mockRejectedValueOnce(new Error('Network error'))

      render(
        <WorkspaceDashboard
          workspaceId="workspace-123"
          currentUserId="user-1"
          isClient={true}
        />
      )

      await waitFor(() => {
        expect(screen.getByText('Archive Workspace')).toBeInTheDocument()
      })

      const archiveButton = screen.getByText('Archive Workspace')
      fireEvent.click(archiveButton)

      await waitFor(() => {
        expect(screen.getByText('An error occurred while archiving workspace')).toBeInTheDocument()
      })
    })
  })

  describe('API Integration', () => {
    it('fetches workspace data on mount with correct credentials', async () => {
      mockFetch.mockResolvedValueOnce({
        ok: true,
        json: async () => mockWorkspaceData,
      })

      render(
        <WorkspaceDashboard
          workspaceId="workspace-123"
          currentUserId="user-1"
          isClient={true}
        />
      )

      await waitFor(() => {
        expect(mockFetch).toHaveBeenCalledWith(
          '/api/workspace/workspace-123',
          expect.objectContaining({
            credentials: 'include',
            headers: {
              'Content-Type': 'application/json',
            },
          })
        )
      })
    })

    it('refetches data when workspaceId changes', async () => {
      mockFetch.mockResolvedValue({
        ok: true,
        json: async () => mockWorkspaceData,
      })

      const { rerender } = render(
        <WorkspaceDashboard
          workspaceId="workspace-123"
          currentUserId="user-1"
          isClient={true}
        />
      )

      await waitFor(() => {
        expect(mockFetch).toHaveBeenCalledTimes(1)
      })

      // Change workspaceId
      rerender(
        <WorkspaceDashboard
          workspaceId="workspace-456"
          currentUserId="user-1"
          isClient={true}
        />
      )

      await waitFor(() => {
        expect(mockFetch).toHaveBeenCalledTimes(2)
      })

      expect(mockFetch).toHaveBeenLastCalledWith(
        '/api/workspace/workspace-456',
        expect.any(Object)
      )
    })
  })
})
