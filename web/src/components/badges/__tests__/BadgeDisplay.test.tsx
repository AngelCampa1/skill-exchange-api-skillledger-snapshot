import React from 'react'
import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import BadgeDisplay from '../BadgeDisplay'
import { UserBadge, BadgeCategory, VerificationLevel } from '@/types/badge'

// Mock fetch globally
const mockFetch = jest.fn()
global.fetch = mockFetch

// Mock clipboard API
const mockClipboard = {
  writeText: jest.fn().mockResolvedValue(undefined),
}
Object.assign(navigator, { clipboard: mockClipboard })

// Mock logger
jest.mock('@/utils/logger', () => ({
  logger: {
    debug: jest.fn(),
    error: jest.fn(),
    info: jest.fn(),
    warn: jest.fn(),
  },
}))

// Mock Next.js Image component
jest.mock('next/image', () => ({
  __esModule: true,
  default: ({ src, alt, onError, ...props }: any) => {
    // eslint-disable-next-line @next/next/no-img-element
    return <img src={src} alt={alt} {...props} />
  },
}))

// Helper to create mock badge
const createMockBadge = (overrides: Partial<UserBadge> = {}): UserBadge => ({
  id: 'badge-1',
  userId: 'user-1',
  badgeName: 'Top Performer',
  badgeDescription: 'Awarded to users who maintain a 95%+ rating',
  badgeType: 'top-performer',
  category: BadgeCategory.Performance,
  earnedAt: new Date('2024-06-15').toISOString(),
  expiresAt: undefined,
  isActive: true,
  verificationLevel: VerificationLevel.Automatic,
  verifiedAt: new Date('2024-06-15').toISOString(),
  verifiedBy: 'system',
  iconUrl: undefined,
  ...overrides,
})

describe('BadgeDisplay', () => {
  beforeEach(() => {
    jest.clearAllMocks()
    mockFetch.mockResolvedValue({
      ok: true,
      json: () => Promise.resolve({ verificationCode: 'ABC-123-XYZ' }),
    })
  })

  // ============================================
  // Basic Rendering (3 tests)
  // ============================================
  describe('Basic Rendering', () => {
    it('displays badge name', () => {
      render(<BadgeDisplay badge={createMockBadge()} />)

      expect(screen.getByText('Top Performer')).toBeInTheDocument()
    })

    it('displays badge category', () => {
      render(<BadgeDisplay badge={createMockBadge()} />)

      expect(screen.getByText('Performance')).toBeInTheDocument()
    })

    it('displays badge description when showDetails is true', () => {
      render(<BadgeDisplay badge={createMockBadge()} showDetails={true} />)

      expect(screen.getByText('Awarded to users who maintain a 95%+ rating')).toBeInTheDocument()
    })
  })

  // ============================================
  // Size Variations (3 tests)
  // ============================================
  describe('Size Variations', () => {
    it('applies small size class', () => {
      const { container } = render(<BadgeDisplay badge={createMockBadge()} size="small" />)

      expect(container.querySelector('.w-12.h-12')).toBeInTheDocument()
    })

    it('applies medium size class by default', () => {
      const { container } = render(<BadgeDisplay badge={createMockBadge()} />)

      expect(container.querySelector('.w-16.h-16')).toBeInTheDocument()
    })

    it('applies large size class', () => {
      const { container } = render(<BadgeDisplay badge={createMockBadge()} size="large" />)

      expect(container.querySelector('.w-24.h-24')).toBeInTheDocument()
    })
  })

  // ============================================
  // Expiration States (3 tests)
  // ============================================
  describe('Expiration States', () => {
    it('applies expired styling for expired badges', () => {
      const expiredBadge = createMockBadge({
        expiresAt: new Date('2024-01-01').toISOString(), // Past date
      })
      const { container } = render(<BadgeDisplay badge={expiredBadge} />)

      expect(container.querySelector('.opacity-60')).toBeInTheDocument()
      expect(container.querySelector('.grayscale')).toBeInTheDocument()
    })

    it('shows expiring soon warning for badges expiring within 30 days', () => {
      const expiringSoonBadge = createMockBadge({
        expiresAt: new Date(Date.now() + 15 * 24 * 60 * 60 * 1000).toISOString(), // 15 days from now
      })
      const { container } = render(<BadgeDisplay badge={expiringSoonBadge} showDetails={true} />)

      // Should show expiration date with warning styling
      expect(container.querySelector('.text-warning')).toBeInTheDocument()
    })

    it('shows "Expired" text for expired badges with showDetails', () => {
      const expiredBadge = createMockBadge({
        expiresAt: new Date('2024-01-01').toISOString(),
      })
      render(<BadgeDisplay badge={expiredBadge} showDetails={true} />)

      expect(screen.getByText(/Expired/)).toBeInTheDocument()
    })
  })

  // ============================================
  // Category Colors (3 tests)
  // ============================================
  describe('Category Colors', () => {
    it('applies Performance category color', () => {
      render(<BadgeDisplay badge={createMockBadge({ category: BadgeCategory.Performance })} />)

      const categoryBadge = screen.getByText('Performance')
      expect(categoryBadge.className).toContain('text-primary')
    })

    it('applies Trust category color', () => {
      render(<BadgeDisplay badge={createMockBadge({ category: BadgeCategory.Trust })} />)

      const categoryBadge = screen.getByText('Trust')
      expect(categoryBadge.className).toContain('text-warning')
    })

    it('applies Expertise category color', () => {
      render(<BadgeDisplay badge={createMockBadge({ category: BadgeCategory.Expertise })} />)

      const categoryBadge = screen.getByText('Expertise')
      expect(categoryBadge.className).toContain('text-info')
    })
  })

  // ============================================
  // Verification Levels (3 tests)
  // ============================================
  describe('Verification Levels', () => {
    it('shows automatic verification icon', () => {
      const { container } = render(
        <BadgeDisplay badge={createMockBadge({ verificationLevel: VerificationLevel.Automatic })} />
      )

      // CheckCircle icon for automatic verification
      expect(container.querySelector('.text-success')).toBeInTheDocument()
    })

    it('shows manual verification icon', () => {
      const { container } = render(
        <BadgeDisplay badge={createMockBadge({ verificationLevel: VerificationLevel.Manual })} />
      )

      // Shield icon for manual verification
      expect(container.querySelector('.text-primary')).toBeInTheDocument()
    })

    it('shows external verification icon', () => {
      const { container } = render(
        <BadgeDisplay badge={createMockBadge({ verificationLevel: VerificationLevel.External })} />
      )

      // ExternalLink icon for external verification
      expect(container.querySelector('.text-info')).toBeInTheDocument()
    })
  })

  // ============================================
  // Verification Code (4 tests)
  // ============================================
  describe('Verification Code', () => {
    it('shows Generate Verification Code button when showVerificationCode is true', () => {
      render(
        <BadgeDisplay
          badge={createMockBadge()}
          showDetails={true}
          showVerificationCode={true}
        />
      )

      expect(screen.getByRole('button', { name: /Generate Verification Code/i })).toBeInTheDocument()
    })

    it('hides verification button for expired badges', () => {
      const expiredBadge = createMockBadge({
        expiresAt: new Date('2024-01-01').toISOString(),
      })
      render(
        <BadgeDisplay
          badge={expiredBadge}
          showDetails={true}
          showVerificationCode={true}
        />
      )

      expect(screen.queryByRole('button', { name: /Generate Verification Code/i })).not.toBeInTheDocument()
    })

    it('opens verification modal on button click', async () => {
      const user = userEvent.setup()
      render(
        <BadgeDisplay
          badge={createMockBadge()}
          showDetails={true}
          showVerificationCode={true}
        />
      )

      await user.click(screen.getByRole('button', { name: /Generate Verification Code/i }))

      await waitFor(() => {
        expect(screen.getByText('Badge Verification')).toBeInTheDocument()
      })
    })

    it('fetches and displays verification code in modal', async () => {
      const user = userEvent.setup()
      mockFetch.mockResolvedValue({
        ok: true,
        json: () => Promise.resolve({ verificationCode: 'TEST-CODE-123' }),
      })

      render(
        <BadgeDisplay
          badge={createMockBadge()}
          showDetails={true}
          showVerificationCode={true}
        />
      )

      await user.click(screen.getByRole('button', { name: /Generate Verification Code/i }))

      await waitFor(() => {
        expect(screen.getByText('TEST-CODE-123')).toBeInTheDocument()
      })
    })
  })

  // ============================================
  // Copy to Clipboard (2 tests)
  // ============================================
  describe('Copy to Clipboard', () => {
    it('displays Copy Code button in modal', async () => {
      const user = userEvent.setup()
      mockFetch.mockResolvedValue({
        ok: true,
        json: () => Promise.resolve({ verificationCode: 'COPY-ME-123' }),
      })

      render(
        <BadgeDisplay
          badge={createMockBadge()}
          showDetails={true}
          showVerificationCode={true}
        />
      )

      await user.click(screen.getByRole('button', { name: /Generate Verification Code/i }))

      await waitFor(() => {
        expect(screen.getByText('COPY-ME-123')).toBeInTheDocument()
      })

      // Find the Copy Code button in the modal
      const copyButtons = screen.getAllByRole('button')
      const copyCodeButton = copyButtons.find(btn => btn.textContent?.includes('Copy Code'))
      expect(copyCodeButton).toBeInTheDocument()
    })

    it('shows "Copied!" text after copying', async () => {
      const user = userEvent.setup()
      mockFetch.mockResolvedValue({
        ok: true,
        json: () => Promise.resolve({ verificationCode: 'COPY-ME-456' }),
      })

      render(
        <BadgeDisplay
          badge={createMockBadge()}
          showDetails={true}
          showVerificationCode={true}
        />
      )

      await user.click(screen.getByRole('button', { name: /Generate Verification Code/i }))

      await waitFor(() => {
        expect(screen.getByText('COPY-ME-456')).toBeInTheDocument()
      })

      await user.click(screen.getByRole('button', { name: /Copy Code/i }))

      await waitFor(() => {
        expect(screen.getByText('Copied!')).toBeInTheDocument()
      })
    })
  })

  // ============================================
  // Click Handler (2 tests)
  // ============================================
  describe('Click Handler', () => {
    it('calls onClick when badge is clicked', async () => {
      const user = userEvent.setup()
      const handleClick = jest.fn()

      render(<BadgeDisplay badge={createMockBadge()} onClick={handleClick} />)

      await user.click(screen.getByText('Top Performer'))

      expect(handleClick).toHaveBeenCalled()
    })

    it('applies hover cursor when onClick is provided', () => {
      const { container } = render(
        <BadgeDisplay badge={createMockBadge()} onClick={jest.fn()} />
      )

      expect(container.querySelector('.cursor-pointer')).toBeInTheDocument()
    })
  })

  // ============================================
  // Inactive Badge (2 tests)
  // ============================================
  describe('Inactive Badge', () => {
    it('applies grayscale filter to inactive badges', () => {
      const inactiveBadge = createMockBadge({ isActive: false })
      const { container } = render(<BadgeDisplay badge={inactiveBadge} />)

      expect(container.querySelector('.grayscale')).toBeInTheDocument()
    })

    it('hides verification button for inactive badges', () => {
      const inactiveBadge = createMockBadge({ isActive: false })
      render(
        <BadgeDisplay
          badge={inactiveBadge}
          showDetails={true}
          showVerificationCode={true}
        />
      )

      expect(screen.queryByRole('button', { name: /Generate Verification Code/i })).not.toBeInTheDocument()
    })
  })

  // ============================================
  // Earned Date Display (1 test)
  // ============================================
  describe('Earned Date Display', () => {
    it('shows relative earned date when showDetails is true', () => {
      const badge = createMockBadge({
        earnedAt: new Date(Date.now() - 30 * 24 * 60 * 60 * 1000).toISOString(), // 30 days ago
      })
      render(<BadgeDisplay badge={badge} showDetails={true} />)

      // Should show something like "Earned about 1 month ago"
      expect(screen.getByText(/Earned/)).toBeInTheDocument()
    })
  })

  // ============================================
  // Category Icons (1 test)
  // ============================================
  describe('Category Icons', () => {
    it('displays category emoji icon', () => {
      render(<BadgeDisplay badge={createMockBadge({ category: BadgeCategory.Achievement })} />)

      // Achievement category should show medal emoji
      expect(screen.getByText('🏅')).toBeInTheDocument()
    })
  })
})
