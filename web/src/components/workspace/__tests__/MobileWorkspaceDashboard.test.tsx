/**
 * Tests for MobileWorkspaceDashboard
 *
 * Comprehensive test suite for the mobile workspace dashboard component
 * Coverage target: 95%+ (193 lines)
 */

import React from 'react'
import { render, screen, fireEvent } from '@testing-library/react'
import '@testing-library/jest-dom'
import { MobileWorkspaceDashboard } from '../MobileWorkspaceDashboard'

// Mock logger
jest.mock('@/utils/logger', () => ({
  logger: {
    error: jest.fn(),
    debug: jest.fn(),
  },
}))

const mockWorkspaceData = {
  workspaceId: 'workspace-123',
  projectTitle: 'Mobile Test Project',
  projectDescription: 'A test project description for mobile view',
  clientName: 'John Doe',
  providerName: 'Jane Smith',
  status: 'Active' as const,
  createdAt: '2024-01-01T00:00:00Z',
  timelineData: JSON.stringify({ milestones: ['Phase 1', 'Phase 2'] }),
  milestoneData: JSON.stringify({ completed: 2, total: 5 }),
  integrationStatus: 'initialized',
  lastSyncedAt: '2024-01-10T12:00:00Z',
}

describe('MobileWorkspaceDashboard', () => {
  let mockOnArchiveWorkspace: jest.Mock
  let mockOnUpdateTimeline: jest.Mock

  beforeEach(() => {
    mockOnArchiveWorkspace = jest.fn()
    mockOnUpdateTimeline = jest.fn()
    jest.clearAllMocks()
  })

  describe('Header Rendering', () => {
    it('renders project title correctly', () => {
      render(
        <MobileWorkspaceDashboard
          workspaceData={mockWorkspaceData}
          isClient={true}
          onArchiveWorkspace={mockOnArchiveWorkspace}
          onUpdateTimeline={mockOnUpdateTimeline}
        />
      )

      expect(screen.getByText('Mobile Test Project')).toBeInTheDocument()
    })

    it('renders project description correctly', () => {
      render(
        <MobileWorkspaceDashboard
          workspaceData={mockWorkspaceData}
          isClient={true}
          onArchiveWorkspace={mockOnArchiveWorkspace}
          onUpdateTimeline={mockOnUpdateTimeline}
        />
      )

      expect(screen.getByText('A test project description for mobile view')).toBeInTheDocument()
    })

    it('renders client and provider names', () => {
      render(
        <MobileWorkspaceDashboard
          workspaceData={mockWorkspaceData}
          isClient={true}
          onArchiveWorkspace={mockOnArchiveWorkspace}
          onUpdateTimeline={mockOnUpdateTimeline}
        />
      )

      expect(screen.getByText('John Doe')).toBeInTheDocument()
      expect(screen.getByText('Jane Smith')).toBeInTheDocument()
    })

    it('renders formatted creation date', () => {
      render(
        <MobileWorkspaceDashboard
          workspaceData={mockWorkspaceData}
          isClient={true}
          onArchiveWorkspace={mockOnArchiveWorkspace}
          onUpdateTimeline={mockOnUpdateTimeline}
        />
      )

      expect(screen.getByText(/Created:/)).toBeInTheDocument()
    })
  })

  describe('Status Badges', () => {
    it('displays Active status badge', () => {
      render(
        <MobileWorkspaceDashboard
          workspaceData={mockWorkspaceData}
          isClient={true}
          onArchiveWorkspace={mockOnArchiveWorkspace}
          onUpdateTimeline={mockOnUpdateTimeline}
        />
      )

      expect(screen.getByText('Active')).toBeInTheDocument()
    })

    it('displays Archived status badge', () => {
      render(
        <MobileWorkspaceDashboard
          workspaceData={{ ...mockWorkspaceData, status: 'Archived' }}
          isClient={true}
          onArchiveWorkspace={mockOnArchiveWorkspace}
          onUpdateTimeline={mockOnUpdateTimeline}
        />
      )

      expect(screen.getByText('Archived')).toBeInTheDocument()
    })

    it('displays Deleted status badge', () => {
      render(
        <MobileWorkspaceDashboard
          workspaceData={{ ...mockWorkspaceData, status: 'Deleted' }}
          isClient={true}
          onArchiveWorkspace={mockOnArchiveWorkspace}
          onUpdateTimeline={mockOnUpdateTimeline}
        />
      )

      expect(screen.getByText('Deleted')).toBeInTheDocument()
    })

    it('displays default badge for unknown status', () => {
      render(
        <MobileWorkspaceDashboard
          workspaceData={{ ...mockWorkspaceData, status: 'Pending' as any }}
          isClient={true}
          onArchiveWorkspace={mockOnArchiveWorkspace}
          onUpdateTimeline={mockOnUpdateTimeline}
        />
      )

      expect(screen.getByText('Pending')).toBeInTheDocument()
    })
  })

  describe('Quick Actions', () => {
    const { logger } = require('@/utils/logger')

    it('renders Messages button and logs when clicked', () => {
      render(
        <MobileWorkspaceDashboard
          workspaceData={mockWorkspaceData}
          isClient={true}
          onArchiveWorkspace={mockOnArchiveWorkspace}
          onUpdateTimeline={mockOnUpdateTimeline}
        />
      )

      const messagesButton = screen.getByText('💬 Messages')
      fireEvent.click(messagesButton)

      expect(logger.debug).toHaveBeenCalledWith(
        'Navigate to messages',
        { component: 'MobileWorkspaceDashboard' }
      )
    })

    it('renders Files button and logs when clicked', () => {
      render(
        <MobileWorkspaceDashboard
          workspaceData={mockWorkspaceData}
          isClient={true}
          onArchiveWorkspace={mockOnArchiveWorkspace}
          onUpdateTimeline={mockOnUpdateTimeline}
        />
      )

      const filesButton = screen.getByText('📁 Files')
      fireEvent.click(filesButton)

      expect(logger.debug).toHaveBeenCalledWith(
        'Navigate to files',
        { component: 'MobileWorkspaceDashboard' }
      )
    })

    it('renders Escrow button and logs when clicked', () => {
      render(
        <MobileWorkspaceDashboard
          workspaceData={mockWorkspaceData}
          isClient={true}
          onArchiveWorkspace={mockOnArchiveWorkspace}
          onUpdateTimeline={mockOnUpdateTimeline}
        />
      )

      const escrowButton = screen.getByText('💰 Escrow')
      fireEvent.click(escrowButton)

      expect(logger.debug).toHaveBeenCalledWith(
        'Navigate to escrow',
        { component: 'MobileWorkspaceDashboard' }
      )
    })
  })

  describe('Integration Status', () => {
    it('displays integration status as initialized', () => {
      render(
        <MobileWorkspaceDashboard
          workspaceData={mockWorkspaceData}
          isClient={true}
          onArchiveWorkspace={mockOnArchiveWorkspace}
          onUpdateTimeline={mockOnUpdateTimeline}
        />
      )

      expect(screen.getByText('Integration Status')).toBeInTheDocument()
      expect(screen.getByText('initialized')).toBeInTheDocument()
    })

    it('displays Unknown when integration status is missing', () => {
      render(
        <MobileWorkspaceDashboard
          workspaceData={{ ...mockWorkspaceData, integrationStatus: undefined }}
          isClient={true}
          onArchiveWorkspace={mockOnArchiveWorkspace}
          onUpdateTimeline={mockOnUpdateTimeline}
        />
      )

      expect(screen.getByText('Unknown')).toBeInTheDocument()
    })

    it('displays last sync time when available', () => {
      render(
        <MobileWorkspaceDashboard
          workspaceData={mockWorkspaceData}
          isClient={true}
          onArchiveWorkspace={mockOnArchiveWorkspace}
          onUpdateTimeline={mockOnUpdateTimeline}
        />
      )

      expect(screen.getByText(/Last Sync:/)).toBeInTheDocument()
    })

    it('does not display last sync when not available', () => {
      render(
        <MobileWorkspaceDashboard
          workspaceData={{ ...mockWorkspaceData, lastSyncedAt: undefined }}
          isClient={true}
          onArchiveWorkspace={mockOnArchiveWorkspace}
          onUpdateTimeline={mockOnUpdateTimeline}
        />
      )

      expect(screen.queryByText(/Last Sync:/)).not.toBeInTheDocument()
    })
  })

  describe('Timeline Data', () => {
    it('displays timeline data when available', () => {
      render(
        <MobileWorkspaceDashboard
          workspaceData={mockWorkspaceData}
          isClient={true}
          onArchiveWorkspace={mockOnArchiveWorkspace}
          onUpdateTimeline={mockOnUpdateTimeline}
        />
      )

      expect(screen.getByText('Timeline')).toBeInTheDocument()
      expect(screen.getByText(/"milestones"/)).toBeInTheDocument()
    })

    it('displays "No timeline data available" when timeline is missing', () => {
      render(
        <MobileWorkspaceDashboard
          workspaceData={{ ...mockWorkspaceData, timelineData: undefined }}
          isClient={true}
          onArchiveWorkspace={mockOnArchiveWorkspace}
          onUpdateTimeline={mockOnUpdateTimeline}
        />
      )

      expect(screen.getByText(/No timeline data available/i)).toBeInTheDocument()
    })

    it('calls onUpdateTimeline when Update Timeline button is clicked', () => {
      render(
        <MobileWorkspaceDashboard
          workspaceData={mockWorkspaceData}
          isClient={true}
          onArchiveWorkspace={mockOnArchiveWorkspace}
          onUpdateTimeline={mockOnUpdateTimeline}
        />
      )

      const updateButton = screen.getByText('Update Timeline')
      fireEvent.click(updateButton)

      expect(mockOnUpdateTimeline).toHaveBeenCalledWith({
        milestones: ['Mobile Milestone 1', 'Mobile Milestone 2'],
      })
    })
  })

  describe('Milestone Data', () => {
    it('displays milestone data when available', () => {
      render(
        <MobileWorkspaceDashboard
          workspaceData={mockWorkspaceData}
          isClient={true}
          onArchiveWorkspace={mockOnArchiveWorkspace}
          onUpdateTimeline={mockOnUpdateTimeline}
        />
      )

      expect(screen.getByText('Milestones')).toBeInTheDocument()
      expect(screen.getByText(/"completed"/)).toBeInTheDocument()
    })

    it('displays "No milestone data available" when milestones are missing', () => {
      render(
        <MobileWorkspaceDashboard
          workspaceData={{ ...mockWorkspaceData, milestoneData: undefined }}
          isClient={true}
          onArchiveWorkspace={mockOnArchiveWorkspace}
          onUpdateTimeline={mockOnUpdateTimeline}
        />
      )

      expect(screen.getByText(/No milestone data available/i)).toBeInTheDocument()
    })
  })

  describe('Safe JSON Parsing', () => {
    const { logger } = require('@/utils/logger')

    it('handles malformed JSON gracefully in timeline data', () => {
      render(
        <MobileWorkspaceDashboard
          workspaceData={{ ...mockWorkspaceData, timelineData: 'invalid json {{{' }}
          isClient={true}
          onArchiveWorkspace={mockOnArchiveWorkspace}
          onUpdateTimeline={mockOnUpdateTimeline}
        />
      )

      expect(screen.getByText('Timeline')).toBeInTheDocument()
      expect(logger.error).toHaveBeenCalledWith(
        'Failed to parse timelineData JSON',
        expect.any(Object)
      )
    })

    it('handles malformed JSON gracefully in milestone data', () => {
      render(
        <MobileWorkspaceDashboard
          workspaceData={{ ...mockWorkspaceData, milestoneData: 'not valid json' }}
          isClient={true}
          onArchiveWorkspace={mockOnArchiveWorkspace}
          onUpdateTimeline={mockOnUpdateTimeline}
        />
      )

      expect(screen.getByText('Milestones')).toBeInTheDocument()
      expect(logger.error).toHaveBeenCalledWith(
        'Failed to parse milestoneData JSON',
        expect.any(Object)
      )
    })
  })

  describe('Archive Workspace', () => {
    it('shows Archive Workspace button for clients with Active workspace', () => {
      render(
        <MobileWorkspaceDashboard
          workspaceData={mockWorkspaceData}
          isClient={true}
          onArchiveWorkspace={mockOnArchiveWorkspace}
          onUpdateTimeline={mockOnUpdateTimeline}
        />
      )

      expect(screen.getByText('Archive Workspace')).toBeInTheDocument()
      expect(screen.getByText('Danger Zone')).toBeInTheDocument()
      expect(screen.getByText(/This action cannot be undone/)).toBeInTheDocument()
    })

    it('does not show Archive Workspace button for providers', () => {
      render(
        <MobileWorkspaceDashboard
          workspaceData={mockWorkspaceData}
          isClient={false}
          onArchiveWorkspace={mockOnArchiveWorkspace}
          onUpdateTimeline={mockOnUpdateTimeline}
        />
      )

      expect(screen.queryByText('Archive Workspace')).not.toBeInTheDocument()
      expect(screen.queryByText('Danger Zone')).not.toBeInTheDocument()
    })

    it('does not show Archive Workspace button for Archived workspace', () => {
      render(
        <MobileWorkspaceDashboard
          workspaceData={{ ...mockWorkspaceData, status: 'Archived' }}
          isClient={true}
          onArchiveWorkspace={mockOnArchiveWorkspace}
          onUpdateTimeline={mockOnUpdateTimeline}
        />
      )

      expect(screen.queryByText('Archive Workspace')).not.toBeInTheDocument()
      expect(screen.queryByText('Danger Zone')).not.toBeInTheDocument()
    })

    it('does not show Archive Workspace button for Deleted workspace', () => {
      render(
        <MobileWorkspaceDashboard
          workspaceData={{ ...mockWorkspaceData, status: 'Deleted' }}
          isClient={true}
          onArchiveWorkspace={mockOnArchiveWorkspace}
          onUpdateTimeline={mockOnUpdateTimeline}
        />
      )

      expect(screen.queryByText('Archive Workspace')).not.toBeInTheDocument()
    })

    it('calls onArchiveWorkspace when Archive button is clicked', () => {
      render(
        <MobileWorkspaceDashboard
          workspaceData={mockWorkspaceData}
          isClient={true}
          onArchiveWorkspace={mockOnArchiveWorkspace}
          onUpdateTimeline={mockOnUpdateTimeline}
        />
      )

      const archiveButton = screen.getByText('Archive Workspace')
      fireEvent.click(archiveButton)

      expect(mockOnArchiveWorkspace).toHaveBeenCalledTimes(1)
    })
  })

  describe('Component Structure', () => {
    it('renders all major sections', () => {
      render(
        <MobileWorkspaceDashboard
          workspaceData={mockWorkspaceData}
          isClient={true}
          onArchiveWorkspace={mockOnArchiveWorkspace}
          onUpdateTimeline={mockOnUpdateTimeline}
        />
      )

      expect(screen.getByText('Quick Actions')).toBeInTheDocument()
      expect(screen.getByText('Integration Status')).toBeInTheDocument()
      expect(screen.getByText('Timeline')).toBeInTheDocument()
      expect(screen.getByText('Milestones')).toBeInTheDocument()
    })

    it('renders with minimal data', () => {
      const minimalData = {
        workspaceId: 'workspace-minimal',
        projectTitle: 'Minimal Project',
        projectDescription: 'Minimal description',
        clientName: 'Client',
        providerName: 'Provider',
        status: 'Active' as const,
        createdAt: '2024-01-01T00:00:00Z',
      }

      render(
        <MobileWorkspaceDashboard
          workspaceData={minimalData}
          isClient={false}
          onArchiveWorkspace={mockOnArchiveWorkspace}
          onUpdateTimeline={mockOnUpdateTimeline}
        />
      )

      expect(screen.getByText('Minimal Project')).toBeInTheDocument()
      expect(screen.getByText(/No timeline data available/i)).toBeInTheDocument()
      expect(screen.getByText(/No milestone data available/i)).toBeInTheDocument()
    })
  })
})
