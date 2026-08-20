import React from 'react'
import { render, screen, fireEvent, waitFor } from '@testing-library/react'
import '@testing-library/jest-dom'
import { ApplicationsView } from '../ApplicationsView'

const mockApplications = [
  {
    id: '1',
    project: {
      id: 'project1',
      title: 'React Development',
      shortDescription: 'Build a React app',
      creditBudget: 2500
    },
    provider: {
      id: 'provider1',
      displayName: 'John Doe',
      email: 'john@example.com',
      title: 'Senior Developer',
      company: 'Tech Corp'
    },
    coverLetter: 'I am excited to work on this project...',
    proposedTimeline: 30,
    skillMatchScore: 0.85,
    status: 'Pending',
    createdAt: '2024-01-15T10:00:00Z',
    updatedAt: '2024-01-15T10:00:00Z',
    isAvailableImmediately: true,
    proposedBudget: 2400,
    availabilityDetails: 'Available 40 hours/week',
    attachments: [
      {
        id: 'att1',
        fileName: 'portfolio.pdf',
        contentType: 'application/pdf',
        fileSize: 1024000,
        url: '/files/portfolio.pdf',
        isSafe: true
      }
    ],
    daysSinceSubmitted: 2,
    canBeWithdrawn: true
  },
  {
    id: '2',
    project: {
      id: 'project2',
      title: 'Node.js Backend',
      shortDescription: 'Build REST API',
      creditBudget: 3000
    },
    provider: {
      id: 'provider2',
      displayName: 'Jane Smith',
      email: 'jane@example.com'
    },
    coverLetter: 'I have extensive backend experience...',
    skillMatchScore: 0.75,
    status: 'Accepted',
    createdAt: '2024-01-10T09:00:00Z',
    updatedAt: '2024-01-12T14:30:00Z',
    reviewedAt: '2024-01-12T14:30:00Z',
    clientFeedback: 'Great proposal, looking forward to working together!',
    isAvailableImmediately: false,
    attachments: [],
    daysSinceSubmitted: 7,
    canBeWithdrawn: false
  }
]

const mockProps = {
  applications: mockApplications,
  viewMode: 'client' as const,
  totalCount: 2,
  hasNextPage: false
}

const mockOnStatusUpdate = jest.fn()
const mockOnWithdraw = jest.fn()
const mockOnRefresh = jest.fn()
const mockOnLoadMore = jest.fn()

describe('ApplicationsView', () => {
  beforeEach(() => {
    mockOnStatusUpdate.mockClear()
    mockOnWithdraw.mockClear()
    mockOnRefresh.mockClear()
    mockOnLoadMore.mockClear()
  })

  describe('Rendering', () => {
    it('renders applications list correctly', () => {
      render(<ApplicationsView {...mockProps} />)

      expect(screen.getByText('Project Applications')).toBeInTheDocument()
      expect(screen.getByText('2 applications total')).toBeInTheDocument()
      expect(screen.getByText('John Doe')).toBeInTheDocument()
      expect(screen.getByText('Jane Smith')).toBeInTheDocument()
    })

    it('renders provider view correctly', () => {
      render(<ApplicationsView {...mockProps} viewMode="provider" />)

      expect(screen.getByText('My Applications')).toBeInTheDocument()
      expect(screen.getByText('React Development')).toBeInTheDocument()
      expect(screen.getByText('Node.js Backend')).toBeInTheDocument()
    })

    it('shows refresh button when onRefresh is provided', () => {
      render(<ApplicationsView {...mockProps} onRefresh={mockOnRefresh} />)

      expect(screen.getByText('Refresh')).toBeInTheDocument()
    })

    it('shows loading state', () => {
      render(<ApplicationsView {...mockProps} isLoading={true} onRefresh={mockOnRefresh} />)

      expect(screen.getByText('Loading...')).toBeInTheDocument()
    })
  })

  describe('Application Status', () => {
    it('displays status badges correctly', () => {
      render(<ApplicationsView {...mockProps} />)

      // Use more specific queries to find the status badges in the cards
      const statusBadges = screen.getAllByText('Pending')
      expect(statusBadges.length).toBeGreaterThanOrEqual(1)
      
      const acceptedBadges = screen.getAllByText('Accepted')
      expect(acceptedBadges.length).toBeGreaterThanOrEqual(1)
      
      // Verify they're in the application cards, not just filter options
      const pendingApplication = statusBadges.find(el =>
        el.closest('.bg-card') !== null
      )
      expect(pendingApplication).toBeTruthy()
    })

    it('shows skill match scores', () => {
      render(<ApplicationsView {...mockProps} />)

      expect(screen.getByText('Skill Match: 85%')).toBeInTheDocument()
      expect(screen.getByText('Skill Match: 75%')).toBeInTheDocument()
    })

    it('displays client feedback when available', () => {
      render(<ApplicationsView {...mockProps} />)

      expect(screen.getByText('Client Feedback:')).toBeInTheDocument()
      expect(screen.getByText('Great proposal, looking forward to working together!')).toBeInTheDocument()
    })
  })

  describe('Filtering and Sorting', () => {
    it('renders filter controls', () => {
      render(<ApplicationsView {...mockProps} />)

      expect(screen.getByPlaceholderText('Search applicants...')).toBeInTheDocument()
      expect(screen.getByDisplayValue('All Status')).toBeInTheDocument()
      expect(screen.getByDisplayValue('Date Submitted')).toBeInTheDocument()
    })

    it('filters applications by search query', () => {
      render(<ApplicationsView {...mockProps} />)

      const searchInput = screen.getByPlaceholderText('Search applicants...')
      fireEvent.change(searchInput, { target: { value: 'John' } })

      expect(screen.getByText('John Doe')).toBeInTheDocument()
      expect(screen.queryByText('Jane Smith')).not.toBeInTheDocument()
    })

    it('filters applications by status', () => {
      render(<ApplicationsView {...mockProps} />)

      const statusFilter = screen.getByDisplayValue('All Status')
      fireEvent.change(statusFilter, { target: { value: 'accepted' } })

      expect(screen.queryByText('John Doe')).not.toBeInTheDocument()
      expect(screen.getByText('Jane Smith')).toBeInTheDocument()
    })

    it('sorts applications by skill match', () => {
      render(<ApplicationsView {...mockProps} />)

      const sortBy = screen.getByDisplayValue('Date Submitted')
      fireEvent.change(sortBy, { target: { value: 'skillMatch' } })

      // Applications should be sorted by skill match score (descending)
      const applications = screen.getAllByRole('button', { name: /view/i })
      expect(applications).toHaveLength(2)
    })
  })

  describe('Application Actions', () => {
    it('shows Review button for pending applications in client view', () => {
      render(<ApplicationsView {...mockProps} onStatusUpdate={mockOnStatusUpdate} />)

      const reviewButtons = screen.getAllByText('Review')
      expect(reviewButtons).toHaveLength(1) // Only for pending application
    })

    it('shows Withdraw button for provider applications', () => {
      render(
        <ApplicationsView 
          {...mockProps} 
          viewMode="provider" 
          onWithdraw={mockOnWithdraw}
        />
      )

      const withdrawButtons = screen.getAllByText('Withdraw')
      expect(withdrawButtons).toHaveLength(1) // Only for withdrawable application
    })

    it('opens application details when View is clicked', () => {
      render(<ApplicationsView {...mockProps} />)

      const viewButton = screen.getAllByText('View')[0]
      fireEvent.click(viewButton)

      expect(screen.getByText('Application Details')).toBeInTheDocument()
      expect(screen.getByText('I am excited to work on this project...')).toBeInTheDocument()
    })
  })

  describe('Status Update Dialog', () => {
    it('opens status update dialog when Review is clicked', async () => {
      render(<ApplicationsView {...mockProps} onStatusUpdate={mockOnStatusUpdate} />)

      const reviewButton = screen.getByText('Review')
      fireEvent.click(reviewButton)

      await waitFor(() => {
        expect(screen.getByText('Update Application Status')).toBeInTheDocument()
        expect(screen.getByLabelText('New Status')).toBeInTheDocument()
        expect(screen.getByLabelText('Feedback (optional)')).toBeInTheDocument()
      })
    })

    it('calls onStatusUpdate when status is updated', async () => {
      render(<ApplicationsView {...mockProps} onStatusUpdate={mockOnStatusUpdate} />)

      const reviewButton = screen.getByText('Review')
      fireEvent.click(reviewButton)

      await waitFor(() => {
        const statusSelect = screen.getByLabelText('New Status')
        fireEvent.change(statusSelect, { target: { value: 'Accepted' } })
      })

      const feedbackTextarea = screen.getByLabelText('Feedback (optional)')
      fireEvent.change(feedbackTextarea, { target: { value: 'Great application!' } })

      const updateButton = screen.getByText('Update Status')
      fireEvent.click(updateButton)

      await waitFor(() => {
        expect(mockOnStatusUpdate).toHaveBeenCalledWith('1', 'Accepted', 'Great application!')
      })
    })
  })

  describe('Withdraw Dialog', () => {
    it('opens withdraw dialog when Withdraw is clicked', async () => {
      render(
        <ApplicationsView 
          {...mockProps} 
          viewMode="provider" 
          onWithdraw={mockOnWithdraw}
        />
      )

      const withdrawButton = screen.getByText('Withdraw')
      fireEvent.click(withdrawButton)

      await waitFor(() => {
        // Check for unique dialog content instead of title that might appear multiple times
        expect(screen.getByText('Are you sure you want to withdraw this application? This action cannot be undone.')).toBeInTheDocument()
        expect(screen.getByLabelText('Reason (optional)')).toBeInTheDocument()
      }, { timeout: 2000 })
    })

    it('calls onWithdraw when application is withdrawn', async () => {
      render(
        <ApplicationsView 
          {...mockProps} 
          viewMode="provider" 
          onWithdraw={mockOnWithdraw}
        />
      )

      const withdrawButton = screen.getByText('Withdraw')
      fireEvent.click(withdrawButton)

      await waitFor(() => {
        const reasonTextarea = screen.getByLabelText('Reason (optional)')
        fireEvent.change(reasonTextarea, { target: { value: 'Found another opportunity' } })
      })

      const confirmWithdrawButton = screen.getAllByText('Withdraw Application')[1] // Second one is in dialog
      fireEvent.click(confirmWithdrawButton)

      await waitFor(() => {
        expect(mockOnWithdraw).toHaveBeenCalledWith('1', 'Found another opportunity')
      })
    })
  })

  describe('Empty States', () => {
    it('shows empty state when no applications', () => {
      render(<ApplicationsView {...mockProps} applications={[]} totalCount={0} />)

      expect(screen.getByText('No applications have been submitted for your projects yet.')).toBeInTheDocument()
    })

    it('shows provider empty state', () => {
      render(
        <ApplicationsView 
          {...mockProps} 
          applications={[]} 
          totalCount={0}
          viewMode="provider"
        />
      )

      expect(screen.getByText("You haven't submitted any applications yet.")).toBeInTheDocument()
    })

    it('shows filtered empty state', () => {
      render(<ApplicationsView {...mockProps} />)

      const searchInput = screen.getByPlaceholderText('Search applicants...')
      fireEvent.change(searchInput, { target: { value: 'NonExistentName' } })

      expect(screen.getByText('No applications match your current filters.')).toBeInTheDocument()
    })
  })

  describe('Load More', () => {
    it('shows Load More button when hasNextPage is true', () => {
      render(<ApplicationsView {...mockProps} hasNextPage={true} onLoadMore={mockOnLoadMore} />)

      expect(screen.getByText('Load More Applications')).toBeInTheDocument()
    })

    it('calls onLoadMore when Load More is clicked', () => {
      render(<ApplicationsView {...mockProps} hasNextPage={true} onLoadMore={mockOnLoadMore} />)

      const loadMoreButton = screen.getByText('Load More Applications')
      fireEvent.click(loadMoreButton)

      expect(mockOnLoadMore).toHaveBeenCalled()
    })
  })

  describe('Attachment Display', () => {
    it('shows attachment count', () => {
      render(<ApplicationsView {...mockProps} />)

      expect(screen.getByText('1 attachment')).toBeInTheDocument()
    })

    it('shows attachment details in detail view', () => {
      render(<ApplicationsView {...mockProps} />)

      const viewButton = screen.getAllByText('View')[0]
      fireEvent.click(viewButton)

      expect(screen.getByText('Portfolio Attachments')).toBeInTheDocument()
      expect(screen.getByText('portfolio.pdf')).toBeInTheDocument()
      expect(screen.getByText('Download')).toBeInTheDocument()
    })
  })
})