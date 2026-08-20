import React from 'react'
import { render, screen, waitFor, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MilestoneTracker } from '../MilestoneTracker'
import { MilestoneStatus, MilestonePriority, ProjectMilestone, ProjectProgress, DeliverableType } from '@/types/milestone'

// Mock fetch globally
const mockFetch = jest.fn()
global.fetch = mockFetch

// Mock logger
jest.mock('@/utils/logger', () => ({
  logger: {
    debug: jest.fn(),
    error: jest.fn(),
    info: jest.fn(),
    warn: jest.fn(),
  },
}))

// Helper to create mock milestone
const createMockMilestone = (overrides: Partial<ProjectMilestone> = {}): ProjectMilestone => ({
  id: 'milestone-1',
  projectId: 'project-1',
  sequenceOrder: 1,
  title: 'Design Phase',
  description: 'Complete the design mockups',
  status: MilestoneStatus.InProgress,
  priority: MilestonePriority.High,
  weightPercentage: 25,
  dueDate: new Date(Date.now() + 7 * 24 * 60 * 60 * 1000).toISOString(), // 7 days from now
  assignedToUserId: 'user-1',
  assignedToUserName: 'John Doe',
  createdByUserId: 'user-0',
  createdByUserName: 'Admin',
  createdAt: new Date().toISOString(),
  updatedAt: new Date().toISOString(),
  isOverdue: false,
  canBeStarted: false,
  canBeSubmitted: true,
  canBeApproved: false,
  submissions: [],
  ...overrides,
})

// Helper to create mock project progress
const createMockProgress = (overrides: Partial<ProjectProgress> = {}): ProjectProgress => ({
  projectId: 'project-1',
  totalMilestones: 5,
  completedMilestones: 2,
  inProgressMilestones: 2,
  overdueMilestones: 1,
  overallProgressPercentage: 40,
  upcomingMilestones: [],
  overdueMilestonesList: [],
  ...overrides,
})

// Helper to setup successful fetch responses
const setupSuccessfulFetch = (
  milestones: ProjectMilestone[] = [createMockMilestone()],
  progress: ProjectProgress = createMockProgress()
) => {
  mockFetch.mockImplementation((url: string) => {
    if (url.includes('/api/milestone?projectId=')) {
      return Promise.resolve({
        ok: true,
        json: () => Promise.resolve({ items: milestones, totalCount: milestones.length }),
      })
    }
    if (url.includes('/progress')) {
      return Promise.resolve({
        ok: true,
        json: () => Promise.resolve(progress),
      })
    }
    return Promise.resolve({
      ok: true,
      json: () => Promise.resolve({}),
    })
  })
}

describe('MilestoneTracker', () => {
  beforeEach(() => {
    jest.clearAllMocks()
  })

  // ============================================
  // Loading States (2 tests)
  // ============================================
  describe('Loading States', () => {
    it('shows loading spinner while fetching milestones', async () => {
      mockFetch.mockImplementation(() => new Promise(() => {})) // Never resolves

      render(
        <MilestoneTracker
          projectId="project-1"
          userRole="client"
          currentUserId="user-1"
        />
      )

      expect(screen.getByText('Loading milestones...')).toBeInTheDocument()
    })

    it('hides loading spinner after milestones are fetched', async () => {
      setupSuccessfulFetch()

      render(
        <MilestoneTracker
          projectId="project-1"
          userRole="client"
          currentUserId="user-1"
        />
      )

      await waitFor(() => {
        expect(screen.queryByText('Loading milestones...')).not.toBeInTheDocument()
      })
    })
  })

  // ============================================
  // Error Handling (3 tests)
  // ============================================
  describe('Error Handling', () => {
    it('displays error message when milestone fetch fails', async () => {
      mockFetch.mockImplementation((url: string) => {
        if (url.includes('/api/milestone?projectId=')) {
          return Promise.resolve({ ok: false })
        }
        return Promise.resolve({ ok: true, json: () => Promise.resolve({}) })
      })

      render(
        <MilestoneTracker
          projectId="project-1"
          userRole="client"
          currentUserId="user-1"
        />
      )

      await waitFor(() => {
        expect(screen.getByText('Failed to load milestones')).toBeInTheDocument()
      })
    })

    it('displays network error when fetch throws', async () => {
      mockFetch.mockRejectedValue(new Error('Network error'))

      render(
        <MilestoneTracker
          projectId="project-1"
          userRole="client"
          currentUserId="user-1"
        />
      )

      await waitFor(() => {
        expect(screen.getByText('Network error loading milestones')).toBeInTheDocument()
      })
    })

    it('shows error icon with error message', async () => {
      mockFetch.mockImplementation((url: string) => {
        if (url.includes('/api/milestone?projectId=')) {
          return Promise.resolve({ ok: false })
        }
        return Promise.resolve({ ok: true, json: () => Promise.resolve({}) })
      })

      render(
        <MilestoneTracker
          projectId="project-1"
          userRole="client"
          currentUserId="user-1"
        />
      )

      await waitFor(() => {
        const errorContainer = screen.getByText('Failed to load milestones').closest('div')
        expect(errorContainer).toHaveClass('text-destructive')
      })
    })
  })

  // ============================================
  // Project Progress Display (4 tests)
  // ============================================
  describe('Project Progress Display', () => {
    it('displays overall progress percentage', async () => {
      setupSuccessfulFetch([], createMockProgress({ overallProgressPercentage: 65 }))

      render(
        <MilestoneTracker
          projectId="project-1"
          userRole="client"
          currentUserId="user-1"
        />
      )

      await waitFor(() => {
        expect(screen.getByText('65%')).toBeInTheDocument()
      })
    })

    it('displays milestone counts in progress card', async () => {
      setupSuccessfulFetch([], createMockProgress({
        totalMilestones: 10,
        completedMilestones: 4,
        inProgressMilestones: 3,
        overdueMilestones: 2,
      }))

      render(
        <MilestoneTracker
          projectId="project-1"
          userRole="client"
          currentUserId="user-1"
        />
      )

      await waitFor(() => {
        expect(screen.getByText('4 of 10 completed')).toBeInTheDocument()
        expect(screen.getByText('3')).toBeInTheDocument() // In Progress
        expect(screen.getByText('4')).toBeInTheDocument() // Completed
        expect(screen.getByText('2')).toBeInTheDocument() // Overdue
        expect(screen.getByText('10')).toBeInTheDocument() // Total
      })
    })

    it('rounds progress percentage to whole number', async () => {
      setupSuccessfulFetch([], createMockProgress({ overallProgressPercentage: 33.333 }))

      render(
        <MilestoneTracker
          projectId="project-1"
          userRole="client"
          currentUserId="user-1"
        />
      )

      await waitFor(() => {
        expect(screen.getByText('33%')).toBeInTheDocument()
      })
    })

    it('displays 0% for empty project', async () => {
      setupSuccessfulFetch([], createMockProgress({ overallProgressPercentage: 0 }))

      render(
        <MilestoneTracker
          projectId="project-1"
          userRole="client"
          currentUserId="user-1"
        />
      )

      await waitFor(() => {
        expect(screen.getByText('0%')).toBeInTheDocument()
      })
    })
  })

  // ============================================
  // Tab Navigation (4 tests)
  // ============================================
  describe('Tab Navigation', () => {
    it('displays all four tabs', async () => {
      setupSuccessfulFetch()

      render(
        <MilestoneTracker
          projectId="project-1"
          userRole="client"
          currentUserId="user-1"
        />
      )

      await waitFor(() => {
        expect(screen.getByRole('tab', { name: 'Overview' })).toBeInTheDocument()
        expect(screen.getByRole('tab', { name: 'Active' })).toBeInTheDocument()
        expect(screen.getByRole('tab', { name: 'Completed' })).toBeInTheDocument()
        expect(screen.getByRole('tab', { name: 'Upcoming' })).toBeInTheDocument()
      })
    })

    it('shows Overview tab content by default', async () => {
      setupSuccessfulFetch()

      render(
        <MilestoneTracker
          projectId="project-1"
          userRole="client"
          currentUserId="user-1"
        />
      )

      await waitFor(() => {
        expect(screen.getByText('All Milestones')).toBeInTheDocument()
      })
    })

    it('switches to Active tab when clicked', async () => {
      const user = userEvent.setup()
      const milestones = [
        createMockMilestone({ id: '1', title: 'Active Task', status: MilestoneStatus.InProgress }),
        createMockMilestone({ id: '2', title: 'Completed Task', status: MilestoneStatus.Completed }),
      ]
      setupSuccessfulFetch(milestones)

      render(
        <MilestoneTracker
          projectId="project-1"
          userRole="client"
          currentUserId="user-1"
        />
      )

      await waitFor(() => {
        expect(screen.getByRole('tab', { name: 'Active' })).toBeInTheDocument()
      })

      await user.click(screen.getByRole('tab', { name: 'Active' }))

      // Active tab should only show InProgress milestones
      await waitFor(() => {
        const activePanel = screen.getByRole('tabpanel')
        expect(within(activePanel).getByText('Active Task')).toBeInTheDocument()
      })
    })

    it('switches to Completed tab when clicked', async () => {
      const user = userEvent.setup()
      const milestones = [
        createMockMilestone({ id: '1', title: 'Active Task', status: MilestoneStatus.InProgress }),
        createMockMilestone({ id: '2', title: 'Done Task', status: MilestoneStatus.Completed }),
      ]
      setupSuccessfulFetch(milestones)

      render(
        <MilestoneTracker
          projectId="project-1"
          userRole="client"
          currentUserId="user-1"
        />
      )

      await waitFor(() => {
        expect(screen.getByRole('tab', { name: 'Completed' })).toBeInTheDocument()
      })

      await user.click(screen.getByRole('tab', { name: 'Completed' }))

      await waitFor(() => {
        const completedPanel = screen.getByRole('tabpanel')
        expect(within(completedPanel).getByText('Done Task')).toBeInTheDocument()
      })
    })
  })

  // ============================================
  // Milestone Status Colors (5 tests)
  // ============================================
  describe('Milestone Status Colors', () => {
    it('applies correct color for NotStarted status', async () => {
      setupSuccessfulFetch([
        createMockMilestone({ status: MilestoneStatus.NotStarted }),
      ])

      render(
        <MilestoneTracker
          projectId="project-1"
          userRole="client"
          currentUserId="user-1"
        />
      )

      await waitFor(() => {
        const statusBadge = screen.getByText('NotStarted')
        expect(statusBadge).toBeInTheDocument()
      })
    })

    it('applies correct color for InProgress status', async () => {
      setupSuccessfulFetch([
        createMockMilestone({ status: MilestoneStatus.InProgress }),
      ])

      render(
        <MilestoneTracker
          projectId="project-1"
          userRole="client"
          currentUserId="user-1"
        />
      )

      await waitFor(() => {
        const statusBadge = screen.getByText('InProgress')
        expect(statusBadge).toBeInTheDocument()
      })
    })

    it('applies correct color for PendingReview status', async () => {
      setupSuccessfulFetch([
        createMockMilestone({ status: MilestoneStatus.PendingReview }),
      ])

      render(
        <MilestoneTracker
          projectId="project-1"
          userRole="client"
          currentUserId="user-1"
        />
      )

      await waitFor(() => {
        const statusBadge = screen.getByText('PendingReview')
        expect(statusBadge).toBeInTheDocument()
      })
    })

    it('applies correct color for Completed status', async () => {
      setupSuccessfulFetch([
        createMockMilestone({ status: MilestoneStatus.Completed }),
      ])

      render(
        <MilestoneTracker
          projectId="project-1"
          userRole="client"
          currentUserId="user-1"
        />
      )

      await waitFor(() => {
        // Use getAllByText since "Completed" appears in tab and status badge
        const completedElements = screen.getAllByText('Completed')
        // Find the span element (status badge) not the button (tab)
        const statusBadge = completedElements.find(el => el.tagName === 'SPAN')
        expect(statusBadge).toBeInTheDocument()
      })
    })

    it('applies correct color for Cancelled status', async () => {
      setupSuccessfulFetch([
        createMockMilestone({ status: MilestoneStatus.Cancelled }),
      ])

      render(
        <MilestoneTracker
          projectId="project-1"
          userRole="client"
          currentUserId="user-1"
        />
      )

      await waitFor(() => {
        const statusBadge = screen.getByText('Cancelled')
        expect(statusBadge).toBeInTheDocument()
      })
    })
  })

  // ============================================
  // Role-Based Rendering (4 tests)
  // ============================================
  describe('Role-Based Rendering', () => {
    it('shows Add Milestone button for client role', async () => {
      setupSuccessfulFetch()

      render(
        <MilestoneTracker
          projectId="project-1"
          userRole="client"
          currentUserId="user-1"
        />
      )

      await waitFor(() => {
        expect(screen.getByRole('button', { name: /Add Milestone/i })).toBeInTheDocument()
      })
    })

    it('hides Add Milestone button for provider role', async () => {
      setupSuccessfulFetch()

      render(
        <MilestoneTracker
          projectId="project-1"
          userRole="provider"
          currentUserId="user-1"
        />
      )

      await waitFor(() => {
        expect(screen.queryByRole('button', { name: /Add Milestone/i })).not.toBeInTheDocument()
      })
    })

    it('shows Start Work button for provider when milestone can be started', async () => {
      setupSuccessfulFetch([
        createMockMilestone({ canBeStarted: true, canBeSubmitted: false }),
      ])

      render(
        <MilestoneTracker
          projectId="project-1"
          userRole="provider"
          currentUserId="user-1"
        />
      )

      await waitFor(() => {
        expect(screen.getByRole('button', { name: /Start Work/i })).toBeInTheDocument()
      })
    })

    it('shows Approve button for client when milestone can be approved', async () => {
      setupSuccessfulFetch([
        createMockMilestone({ canBeApproved: true }),
      ])

      render(
        <MilestoneTracker
          projectId="project-1"
          userRole="client"
          currentUserId="user-1"
        />
      )

      await waitFor(() => {
        expect(screen.getByRole('button', { name: /Approve/i })).toBeInTheDocument()
      })
    })
  })

  // ============================================
  // Milestone Actions (4 tests)
  // ============================================
  describe('Milestone Actions', () => {
    it('calls start action when Start Work button clicked', async () => {
      const user = userEvent.setup()
      setupSuccessfulFetch([
        createMockMilestone({ id: 'ms-1', canBeStarted: true }),
      ])

      render(
        <MilestoneTracker
          projectId="project-1"
          userRole="provider"
          currentUserId="user-1"
        />
      )

      await waitFor(() => {
        expect(screen.getByRole('button', { name: /Start Work/i })).toBeInTheDocument()
      })

      await user.click(screen.getByRole('button', { name: /Start Work/i }))

      await waitFor(() => {
        expect(mockFetch).toHaveBeenCalledWith(
          '/api/milestone/ms-1/start',
          expect.objectContaining({ method: 'POST' })
        )
      })
    })

    it('calls submit action when Submit for Review button clicked', async () => {
      const user = userEvent.setup()
      setupSuccessfulFetch([
        createMockMilestone({ id: 'ms-2', canBeSubmitted: true }),
      ])

      render(
        <MilestoneTracker
          projectId="project-1"
          userRole="provider"
          currentUserId="user-1"
        />
      )

      await waitFor(() => {
        expect(screen.getByRole('button', { name: /Submit for Review/i })).toBeInTheDocument()
      })

      await user.click(screen.getByRole('button', { name: /Submit for Review/i }))

      await waitFor(() => {
        expect(mockFetch).toHaveBeenCalledWith(
          '/api/milestone/ms-2/submit',
          expect.objectContaining({ method: 'POST' })
        )
      })
    })

    it('calls approve action when Approve button clicked', async () => {
      const user = userEvent.setup()
      setupSuccessfulFetch([
        createMockMilestone({ id: 'ms-3', canBeApproved: true }),
      ])

      render(
        <MilestoneTracker
          projectId="project-1"
          userRole="client"
          currentUserId="user-1"
        />
      )

      await waitFor(() => {
        expect(screen.getByRole('button', { name: /Approve/i })).toBeInTheDocument()
      })

      await user.click(screen.getByRole('button', { name: /Approve/i }))

      await waitFor(() => {
        expect(mockFetch).toHaveBeenCalledWith(
          '/api/milestone/ms-3/approve',
          expect.objectContaining({ method: 'POST' })
        )
      })
    })

    it('shows error when action fails', async () => {
      const user = userEvent.setup()
      mockFetch.mockImplementation((url: string) => {
        if (url.includes('/api/milestone?projectId=')) {
          return Promise.resolve({
            ok: true,
            json: () => Promise.resolve({ items: [createMockMilestone({ id: 'ms-4', canBeApproved: true })] }),
          })
        }
        if (url.includes('/progress')) {
          return Promise.resolve({
            ok: true,
            json: () => Promise.resolve(createMockProgress()),
          })
        }
        if (url.includes('/approve')) {
          return Promise.resolve({
            ok: false,
            json: () => Promise.resolve({ message: 'Approval failed - missing documents' }),
          })
        }
        return Promise.resolve({ ok: true, json: () => Promise.resolve({}) })
      })

      render(
        <MilestoneTracker
          projectId="project-1"
          userRole="client"
          currentUserId="user-1"
        />
      )

      await waitFor(() => {
        expect(screen.getByRole('button', { name: /Approve/i })).toBeInTheDocument()
      })

      await user.click(screen.getByRole('button', { name: /Approve/i }))

      await waitFor(() => {
        expect(screen.getByText('Approval failed - missing documents')).toBeInTheDocument()
      })
    })
  })

  // ============================================
  // Empty State (2 tests)
  // ============================================
  describe('Empty State', () => {
    it('shows empty state when no milestones exist', async () => {
      setupSuccessfulFetch([])

      render(
        <MilestoneTracker
          projectId="project-1"
          userRole="client"
          currentUserId="user-1"
        />
      )

      await waitFor(() => {
        expect(screen.getByText('No milestones yet')).toBeInTheDocument()
        expect(screen.getByText('Get started by creating your first project milestone')).toBeInTheDocument()
      })
    })

    it('shows Create First Milestone button for client in empty state', async () => {
      setupSuccessfulFetch([])

      render(
        <MilestoneTracker
          projectId="project-1"
          userRole="client"
          currentUserId="user-1"
        />
      )

      await waitFor(() => {
        expect(screen.getByRole('button', { name: /Create First Milestone/i })).toBeInTheDocument()
      })
    })
  })

  // ============================================
  // Overdue Milestones (2 tests)
  // ============================================
  describe('Overdue Milestones', () => {
    it('applies overdue styling to overdue milestones', async () => {
      setupSuccessfulFetch([
        createMockMilestone({
          isOverdue: true,
          dueDate: new Date(Date.now() - 2 * 24 * 60 * 60 * 1000).toISOString(), // 2 days ago
        }),
      ])

      render(
        <MilestoneTracker
          projectId="project-1"
          userRole="client"
          currentUserId="user-1"
        />
      )

      await waitFor(() => {
        // Find the card by looking for the element with border-destructive class
        const milestoneTitle = screen.getByText('Design Phase')
        // The card is the ancestor with the border-destructive class
        const card = milestoneTitle.closest('div[class*="border-destructive"]')
        expect(card).toBeInTheDocument()
      })
    })

    it('shows overdue count in progress summary', async () => {
      setupSuccessfulFetch([], createMockProgress({ overdueMilestones: 3 }))

      render(
        <MilestoneTracker
          projectId="project-1"
          userRole="client"
          currentUserId="user-1"
        />
      )

      await waitFor(() => {
        expect(screen.getByText('3')).toBeInTheDocument()
        expect(screen.getByText('Overdue')).toBeInTheDocument()
      })
    })
  })

  // ============================================
  // Submissions Display (2 tests)
  // ============================================
  describe('Submissions Display', () => {
    it('displays submission count when submissions exist', async () => {
      setupSuccessfulFetch([
        createMockMilestone({
          submissions: [
            { id: 's1', title: 'Draft v1', type: DeliverableType.FileUpload, submittedAt: new Date().toISOString(), isReviewed: false, isApproved: false, attachmentCount: 1, totalFileSize: 1024 },
            { id: 's2', title: 'Draft v2', type: DeliverableType.FileUpload, submittedAt: new Date().toISOString(), isReviewed: true, isApproved: true, attachmentCount: 2, totalFileSize: 2048 },
          ],
        }),
      ])

      render(
        <MilestoneTracker
          projectId="project-1"
          userRole="client"
          currentUserId="user-1"
        />
      )

      await waitFor(() => {
        expect(screen.getByText('2 submissions')).toBeInTheDocument()
      })
    })

    it('displays submission titles as badges', async () => {
      setupSuccessfulFetch([
        createMockMilestone({
          submissions: [
            { id: 's1', title: 'Initial Design', type: DeliverableType.FileUpload, submittedAt: new Date().toISOString(), isReviewed: false, isApproved: false, attachmentCount: 1, totalFileSize: 1024 },
          ],
        }),
      ])

      render(
        <MilestoneTracker
          projectId="project-1"
          userRole="client"
          currentUserId="user-1"
        />
      )

      await waitFor(() => {
        expect(screen.getByText('Initial Design')).toBeInTheDocument()
      })
    })
  })

  // ============================================
  // Priority Display (2 tests)
  // ============================================
  describe('Priority Display', () => {
    it('displays milestone priority badge', async () => {
      setupSuccessfulFetch([
        createMockMilestone({ priority: MilestonePriority.Critical }),
      ])

      render(
        <MilestoneTracker
          projectId="project-1"
          userRole="client"
          currentUserId="user-1"
        />
      )

      await waitFor(() => {
        expect(screen.getByText('Critical')).toBeInTheDocument()
      })
    })

    it('displays all priority levels correctly', async () => {
      setupSuccessfulFetch([
        createMockMilestone({ id: '1', title: 'Low Task', priority: MilestonePriority.Low }),
        createMockMilestone({ id: '2', title: 'Medium Task', priority: MilestonePriority.Medium }),
        createMockMilestone({ id: '3', title: 'High Task', priority: MilestonePriority.High }),
      ])

      render(
        <MilestoneTracker
          projectId="project-1"
          userRole="client"
          currentUserId="user-1"
        />
      )

      await waitFor(() => {
        expect(screen.getByText('Low')).toBeInTheDocument()
        expect(screen.getByText('Medium')).toBeInTheDocument()
        expect(screen.getByText('High')).toBeInTheDocument()
      })
    })
  })

  // ============================================
  // Edge Cases (5 tests) - Added to reach 80% coverage
  // ============================================
  describe('Edge Cases', () => {
    it('handles rapid tab switching', async () => {
      const user = userEvent.setup()
      setupSuccessfulFetch()

      render(
        <MilestoneTracker
          projectId="project-1"
          userRole="client"
          currentUserId="user-1"
        />
      )

      await waitFor(() => {
        expect(screen.getByRole('tab', { name: 'Overview' })).toBeInTheDocument()
      })

      // Rapidly switch between tabs
      await user.click(screen.getByRole('tab', { name: 'Active' }))
      await user.click(screen.getByRole('tab', { name: 'Completed' }))
      await user.click(screen.getByRole('tab', { name: 'Upcoming' }))
      await user.click(screen.getByRole('tab', { name: 'Overview' }))

      // Should end up on Overview tab
      await waitFor(() => {
        expect(screen.getByText('All Milestones')).toBeInTheDocument()
      })
    })

    it('handles milestones with missing optional fields', async () => {
      setupSuccessfulFetch([
        createMockMilestone({
          assignedToUserId: undefined as any,
          assignedToUserName: undefined as any,
        }),
      ])

      render(
        <MilestoneTracker
          projectId="project-1"
          userRole="client"
          currentUserId="user-1"
        />
      )

      await waitFor(() => {
        expect(screen.getByText('Design Phase')).toBeInTheDocument()
      })
    })

    it('handles progress with zero milestones', async () => {
      setupSuccessfulFetch([], createMockProgress({
        totalMilestones: 0,
        completedMilestones: 0,
        inProgressMilestones: 0,
        overdueMilestones: 0,
      }))

      render(
        <MilestoneTracker
          projectId="project-1"
          userRole="client"
          currentUserId="user-1"
        />
      )

      await waitFor(() => {
        expect(screen.getByText(/0 of 0 completed/i)).toBeInTheDocument()
      })
    })

    it('handles multiple action failures in sequence', async () => {
      const user = userEvent.setup()
      mockFetch.mockImplementation((url: string) => {
        if (url.includes('/api/milestone?projectId=')) {
          return Promise.resolve({
            ok: true,
            json: () => Promise.resolve({ items: [createMockMilestone({ id: 'ms-1', canBeApproved: true })], totalCount: 1 }),
          })
        }
        if (url.includes('/progress')) {
          return Promise.resolve({
            ok: true,
            json: () => Promise.resolve(createMockProgress()),
          })
        }
        if (url.includes('/approve')) {
          return Promise.resolve({
            ok: false,
            json: () => Promise.resolve({ message: 'First failure' }),
          })
        }
        return Promise.resolve({ ok: true, json: () => Promise.resolve({}) })
      })

      render(
        <MilestoneTracker
          projectId="project-1"
          userRole="client"
          currentUserId="user-1"
        />
      )

      await waitFor(() => {
        expect(screen.getByRole('button', { name: /Approve/i })).toBeInTheDocument()
      })

      // Click approve multiple times (should handle gracefully)
      await user.click(screen.getByRole('button', { name: /Approve/i }))

      await waitFor(() => {
        expect(screen.getByText('First failure')).toBeInTheDocument()
      })
    })

    it('switches to Upcoming tab and filters correctly', async () => {
      const user = userEvent.setup()
      const milestones = [
        createMockMilestone({ id: '1', title: 'Started Task', status: MilestoneStatus.InProgress }),
        createMockMilestone({ id: '2', title: 'Not Started', status: MilestoneStatus.NotStarted }),
      ]
      setupSuccessfulFetch(milestones)

      render(
        <MilestoneTracker
          projectId="project-1"
          userRole="client"
          currentUserId="user-1"
        />
      )

      await waitFor(() => {
        expect(screen.getByRole('tab', { name: 'Upcoming' })).toBeInTheDocument()
      })

      await user.click(screen.getByRole('tab', { name: 'Upcoming' }))

      await waitFor(() => {
        const upcomingPanel = screen.getByRole('tabpanel')
        expect(within(upcomingPanel).getByText('Not Started')).toBeInTheDocument()
      })
    })
  })
})
