import React from 'react'
import { render, screen, waitFor, act } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import ReputationDashboard from '../ReputationDashboard'

// Mock fetch API
const mockFetch = jest.fn()
global.fetch = mockFetch

// Mock logger to prevent console noise
jest.mock('@/utils/logger', () => ({
  logger: {
    error: jest.fn(),
    warn: jest.fn(),
    info: jest.fn(),
  },
}))

// Helper to create mock reputation data
const createMockReputation = (overrides: Partial<{
  overallScore: number
  reliabilityScore: number
  qualityScore: number
  responseScore: number
  riskLevel: 'Low' | 'Medium' | 'High' | 'Critical'
  trustLevel: 'New' | 'Emerging' | 'Established' | 'Trusted' | 'Elite'
}> = {}) => ({
  userId: 'user-123',
  overallScore: 0.85,
  reliabilityScore: 0.82,
  qualityScore: 0.88,
  responseScore: 0.79,
  riskLevel: 'Low' as const,
  trustLevel: 'Trusted' as const,
  lastUpdated: '2025-12-19T10:00:00Z',
  trends: [
    { period: 'This Week', scoreChange: 0.02, direction: 'up' as const },
    { period: 'Last Month', scoreChange: -0.01, direction: 'down' as const },
    { period: 'Last Quarter', scoreChange: 0.05, direction: 'up' as const },
  ],
  ...overrides,
})

const createMockHistory = () => [
  { date: '2025-12-18', overallScore: 0.84, reliabilityScore: 0.81, qualityScore: 0.87, responseScore: 0.78 },
  { date: '2025-12-17', overallScore: 0.83, reliabilityScore: 0.80, qualityScore: 0.86, responseScore: 0.77 },
]

const createMockAccountStatus = (overrides: Partial<{
  hasActivePenalties: boolean
  penaltyCount: number
  reviewRestricted: boolean
  suspensionEndDate: string | undefined
}> = {}) => ({
  hasActivePenalties: false,
  penaltyCount: 0,
  lastPenaltyDate: undefined,
  appealsCount: 0,
  reviewRestricted: false,
  suspensionEndDate: undefined,
  ...overrides,
})

// Helper to setup successful API responses
const setupSuccessfulFetch = (
  reputation = createMockReputation(),
  history = createMockHistory(),
  status = createMockAccountStatus()
) => {
  mockFetch.mockImplementation((url: string) => {
    if (url.includes('/api/user/reputation/history')) {
      return Promise.resolve({
        ok: true,
        json: () => Promise.resolve(history),
      })
    }
    if (url.includes('/api/user/reputation/status')) {
      return Promise.resolve({
        ok: true,
        json: () => Promise.resolve(status),
      })
    }
    if (url.includes('/api/user/reputation')) {
      return Promise.resolve({
        ok: true,
        json: () => Promise.resolve(reputation),
      })
    }
    return Promise.reject(new Error('Unknown endpoint'))
  })
}

describe('ReputationDashboard', () => {
  beforeEach(() => {
    jest.clearAllMocks()
  })

  // ============================================
  // Loading States (2 tests)
  // ============================================
  describe('Loading States', () => {
    it('shows loading skeleton initially', async () => {
      // Never resolve the fetch - keep loading
      mockFetch.mockImplementation(() => new Promise(() => {}))

      render(<ReputationDashboard />)

      // Should show loading skeleton with animate-pulse
      const loadingElement = document.querySelector('.animate-pulse')
      expect(loadingElement).toBeInTheDocument()
    })

    it('hides loading skeleton after data loads', async () => {
      setupSuccessfulFetch()

      render(<ReputationDashboard />)

      await waitFor(() => {
        expect(screen.getByText('Reputation Dashboard')).toBeInTheDocument()
      })

      // Skeleton should be gone
      const loadingElement = document.querySelector('.animate-pulse')
      expect(loadingElement).not.toBeInTheDocument()
    })
  })

  // ============================================
  // Score Color Calculations (4 tests)
  // ============================================
  describe('Score Color Calculations', () => {
    it('shows success color for scores >= 0.8', async () => {
      setupSuccessfulFetch(createMockReputation({ overallScore: 0.85 }))

      render(<ReputationDashboard />)

      await waitFor(() => {
        const scoreElement = screen.getByText('85')
        expect(scoreElement).toHaveClass('text-success')
      })
    })

    it('shows warning color for scores between 0.6 and 0.79', async () => {
      setupSuccessfulFetch(createMockReputation({ overallScore: 0.65 }))

      render(<ReputationDashboard />)

      await waitFor(() => {
        const scoreElement = screen.getByText('65')
        expect(scoreElement).toHaveClass('text-warning')
      })
    })

    it('shows warning color for scores between 0.4 and 0.59', async () => {
      setupSuccessfulFetch(createMockReputation({ overallScore: 0.45 }))

      render(<ReputationDashboard />)

      await waitFor(() => {
        const scoreElement = screen.getByText('45')
        expect(scoreElement).toHaveClass('text-warning')
      })
    })

    it('shows destructive color for scores below 0.4', async () => {
      setupSuccessfulFetch(createMockReputation({ overallScore: 0.35 }))

      render(<ReputationDashboard />)

      await waitFor(() => {
        const scoreElement = screen.getByText('35')
        expect(scoreElement).toHaveClass('text-destructive')
      })
    })
  })

  // ============================================
  // Risk Level Colors (4 tests)
  // ============================================
  describe('Risk Level Colors', () => {
    it('shows success styling for Low risk level', async () => {
      setupSuccessfulFetch(createMockReputation({ riskLevel: 'Low' }))

      render(<ReputationDashboard />)

      await waitFor(() => {
        const riskBadge = screen.getByText('Low')
        expect(riskBadge).toHaveClass('text-success')
      })
    })

    it('shows warning styling for Medium risk level', async () => {
      setupSuccessfulFetch(createMockReputation({ riskLevel: 'Medium' }))

      render(<ReputationDashboard />)

      await waitFor(() => {
        const riskBadge = screen.getByText('Medium')
        expect(riskBadge).toHaveClass('text-warning')
      })
    })

    it('shows warning styling for High risk level', async () => {
      setupSuccessfulFetch(createMockReputation({ riskLevel: 'High' }))

      render(<ReputationDashboard />)

      await waitFor(() => {
        const riskBadge = screen.getByText('High')
        expect(riskBadge).toHaveClass('text-warning')
      })
    })

    it('shows destructive styling for Critical risk level', async () => {
      setupSuccessfulFetch(createMockReputation({ riskLevel: 'Critical' }))

      render(<ReputationDashboard />)

      await waitFor(() => {
        const riskBadge = screen.getByText('Critical')
        expect(riskBadge).toHaveClass('text-destructive')
      })
    })
  })

  // ============================================
  // Trust Level Colors (4 tests)
  // ============================================
  describe('Trust Level Colors', () => {
    it('shows primary styling for Elite trust level', async () => {
      setupSuccessfulFetch(createMockReputation({ trustLevel: 'Elite' }))

      render(<ReputationDashboard />)

      await waitFor(() => {
        const trustBadge = screen.getByText('Elite')
        expect(trustBadge).toHaveClass('text-primary')
      })
    })

    it('shows primary styling for Trusted trust level', async () => {
      setupSuccessfulFetch(createMockReputation({ trustLevel: 'Trusted' }))

      render(<ReputationDashboard />)

      await waitFor(() => {
        const trustBadge = screen.getByText('Trusted')
        expect(trustBadge).toHaveClass('text-primary')
      })
    })

    it('shows success styling for Established trust level', async () => {
      setupSuccessfulFetch(createMockReputation({ trustLevel: 'Established' }))

      render(<ReputationDashboard />)

      await waitFor(() => {
        const trustBadge = screen.getByText('Established')
        expect(trustBadge).toHaveClass('text-success')
      })
    })

    it('shows warning styling for Emerging trust level', async () => {
      setupSuccessfulFetch(createMockReputation({ trustLevel: 'Emerging' }))

      render(<ReputationDashboard />)

      await waitFor(() => {
        const trustBadge = screen.getByText('Emerging')
        expect(trustBadge).toHaveClass('text-warning')
      })
    })
  })

  // ============================================
  // Score Change Formatting (3 tests)
  // ============================================
  describe('Score Change Formatting', () => {
    it('formats positive score changes with + sign', async () => {
      const reputation = createMockReputation()
      reputation.trends = [{ period: 'This Week', scoreChange: 0.05, direction: 'up' }]
      setupSuccessfulFetch(reputation)

      render(<ReputationDashboard />)

      await waitFor(() => {
        expect(screen.getByText('+5.0%')).toBeInTheDocument()
      })
    })

    it('formats negative score changes without + sign', async () => {
      const reputation = createMockReputation()
      reputation.trends = [{ period: 'This Week', scoreChange: -0.03, direction: 'down' }]
      setupSuccessfulFetch(reputation)

      render(<ReputationDashboard />)

      await waitFor(() => {
        expect(screen.getByText('-3.0%')).toBeInTheDocument()
      })
    })

    it('displays appropriate trend icons', async () => {
      const reputation = createMockReputation()
      reputation.trends = [
        { period: 'Week 1', scoreChange: 0.02, direction: 'up' as const },
        { period: 'Week 2', scoreChange: -0.01, direction: 'down' as const },
      ]
      setupSuccessfulFetch(reputation)

      render(<ReputationDashboard />)

      await waitFor(() => {
        const upTrends = screen.getAllByText('📈')
        expect(upTrends.length).toBeGreaterThan(0)
        expect(screen.getByText('📉')).toBeInTheDocument()
      })
    })
  })

  // ============================================
  // Account Status Alerts (3 tests)
  // ============================================
  describe('Account Status Alerts', () => {
    it('shows penalty alert when user has active penalties', async () => {
      const status = createMockAccountStatus({
        hasActivePenalties: true,
        penaltyCount: 2,
      })
      setupSuccessfulFetch(createMockReputation(), createMockHistory(), status)

      render(<ReputationDashboard />)

      await waitFor(() => {
        expect(screen.getByText('Account Penalties Active')).toBeInTheDocument()
        expect(screen.getByText(/You have 2 active penalties/)).toBeInTheDocument()
      })
    })

    it('shows review restriction message when restricted', async () => {
      const status = createMockAccountStatus({
        hasActivePenalties: true,
        penaltyCount: 1,
        reviewRestricted: true,
      })
      setupSuccessfulFetch(createMockReputation(), createMockHistory(), status)

      render(<ReputationDashboard />)

      await waitFor(() => {
        expect(screen.getByText(/You are currently restricted from leaving reviews/)).toBeInTheDocument()
      })
    })

    it('shows suspension end date when user is suspended', async () => {
      const status = createMockAccountStatus({
        hasActivePenalties: true,
        penaltyCount: 1,
        suspensionEndDate: '2025-12-31T00:00:00Z',
      })
      setupSuccessfulFetch(createMockReputation(), createMockHistory(), status)

      render(<ReputationDashboard />)

      await waitFor(() => {
        expect(screen.getByText(/Your suspension ends on/)).toBeInTheDocument()
      })
    })
  })

  // ============================================
  // Timeframe Selection (2 tests)
  // ============================================
  describe('Timeframe Selection', () => {
    it('defaults to 30 days timeframe', async () => {
      setupSuccessfulFetch()

      render(<ReputationDashboard />)

      await waitFor(() => {
        const select = screen.getByRole('combobox')
        expect(select).toHaveValue('30d')
      })
    })

    it('refetches data when timeframe changes', async () => {
      const user = userEvent.setup()
      setupSuccessfulFetch()

      render(<ReputationDashboard />)

      await waitFor(() => {
        expect(screen.getByText('Reputation Dashboard')).toBeInTheDocument()
      })

      // Clear previous calls
      mockFetch.mockClear()
      setupSuccessfulFetch()

      // Change timeframe
      const select = screen.getByRole('combobox')
      await user.selectOptions(select, '90d')

      await waitFor(() => {
        // Should have called fetch again with new timeframe
        const historyCall = mockFetch.mock.calls.find((call: string[]) =>
          call[0].includes('/api/user/reputation/history')
        )
        expect(historyCall?.[0]).toContain('timeframe=90d')
      })
    })
  })

  // ============================================
  // Improvement Tips (2 tests)
  // ============================================
  describe('Improvement Tips', () => {
    it('shows reliability improvement tip when score below 0.7', async () => {
      setupSuccessfulFetch(createMockReputation({ reliabilityScore: 0.65 }))

      render(<ReputationDashboard />)

      await waitFor(() => {
        expect(screen.getByText('Improve Reliability')).toBeInTheDocument()
        expect(screen.getByText(/Complete projects on time/)).toBeInTheDocument()
      })
    })

    it('shows congratulations when overall score >= 0.8', async () => {
      setupSuccessfulFetch(createMockReputation({
        overallScore: 0.85,
        reliabilityScore: 0.82,
        qualityScore: 0.88,
        responseScore: 0.85,
      }))

      render(<ReputationDashboard />)

      await waitFor(() => {
        expect(screen.getByText('Great Job!')).toBeInTheDocument()
        expect(screen.getByText(/Your reputation is excellent/)).toBeInTheDocument()
      })
    })
  })

  // ============================================
  // Detailed Score Cards (2 tests)
  // ============================================
  describe('Detailed Score Cards', () => {
    it('displays all three detailed score cards', async () => {
      setupSuccessfulFetch()

      render(<ReputationDashboard />)

      await waitFor(() => {
        expect(screen.getByText('Reliability Score')).toBeInTheDocument()
        expect(screen.getByText('Quality Score')).toBeInTheDocument()
        expect(screen.getByText('Response Score')).toBeInTheDocument()
      })
    })

    it('shows progress bars for each detailed score', async () => {
      setupSuccessfulFetch(createMockReputation({
        reliabilityScore: 0.75,
        qualityScore: 0.80,
        responseScore: 0.70,
      }))

      render(<ReputationDashboard />)

      await waitFor(() => {
        // Each score card should have a progress bar
        const progressBars = document.querySelectorAll('.rounded-full.h-2')
        expect(progressBars.length).toBeGreaterThanOrEqual(3)
      })
    })
  })

  // ============================================
  // Error Handling (2 tests)
  // ============================================
  describe('Error Handling', () => {
    it('handles API failure gracefully', async () => {
      mockFetch.mockRejectedValue(new Error('Network error'))

      render(<ReputationDashboard />)

      // Should still show loading state since data failed to load
      await waitFor(() => {
        const loadingElement = document.querySelector('.animate-pulse')
        expect(loadingElement).toBeInTheDocument()
      })
    })

    it('handles partial API failure (some endpoints succeed)', async () => {
      mockFetch.mockImplementation((url: string) => {
        if (url.includes('/api/user/reputation/history')) {
          return Promise.resolve({ ok: false })
        }
        if (url.includes('/api/user/reputation/status')) {
          return Promise.resolve({ ok: false })
        }
        if (url.includes('/api/user/reputation')) {
          return Promise.resolve({
            ok: true,
            json: () => Promise.resolve(createMockReputation()),
          })
        }
        return Promise.reject(new Error('Unknown endpoint'))
      })

      render(<ReputationDashboard />)

      // Should still render with main reputation data
      await waitFor(() => {
        expect(screen.getByText('Reputation Dashboard')).toBeInTheDocument()
        expect(screen.getByText('85')).toBeInTheDocument()
      })
    })
  })

  // ============================================
  // Date Formatting (1 test)
  // ============================================
  describe('Date Formatting', () => {
    it('displays last updated date correctly', async () => {
      const reputation = createMockReputation()
      reputation.lastUpdated = '2025-12-19T10:00:00Z'
      setupSuccessfulFetch(reputation)

      render(<ReputationDashboard />)

      await waitFor(() => {
        // Should show the date in local format
        const dateElements = screen.getAllByText(/12\/19\/2025|19\/12\/2025|2025/)
        expect(dateElements.length).toBeGreaterThan(0)
      })
    })
  })
})
