/**
 * Cookie Consent Banner Tests
 *
 * TDD approach: These tests are written BEFORE the implementation.
 * They should fail initially (RED), then pass after implementation (GREEN).
 */

import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import CookieConsentBanner from '../CookieConsentBanner'
import { CookieConsentProvider } from '@/contexts/CookieConsentContext'

// Mock next/link
jest.mock('next/link', () => {
  const MockLink = ({ children, href }: { children: React.ReactNode; href: string }) => {
    return <a href={href}>{children}</a>
  }
  MockLink.displayName = 'MockLink'
  return MockLink
})

// Mock localStorage
const localStorageMock = (() => {
  let store: Record<string, string> = {}

  return {
    getItem: (key: string) => store[key] || null,
    setItem: (key: string, value: string) => {
      store[key] = value.toString()
    },
    removeItem: (key: string) => {
      delete store[key]
    },
    clear: () => {
      store = {}
    },
  }
})()

Object.defineProperty(window, 'localStorage', {
  value: localStorageMock,
})

// Mock gtag
const gtagMock = jest.fn()
Object.defineProperty(window, 'gtag', {
  value: gtagMock,
  writable: true,
})

function renderWithProvider(ui: React.ReactElement) {
  return render(<CookieConsentProvider>{ui}</CookieConsentProvider>)
}

describe('CookieConsentBanner', () => {
  beforeEach(() => {
    localStorage.clear()
    jest.clearAllMocks()
    gtagMock.mockClear()
    // Reset Do Not Track
    Object.defineProperty(navigator, 'doNotTrack', {
      value: '0',
      writable: true,
      configurable: true,
    })
  })

  describe('Visibility', () => {
    it('should show banner when consent is null (not asked)', () => {
      renderWithProvider(<CookieConsentBanner />)

      expect(screen.getByText(/we use cookies/i)).toBeInTheDocument()
      expect(screen.getByRole('button', { name: /accept/i })).toBeInTheDocument()
      expect(screen.getByRole('button', { name: /decline/i })).toBeInTheDocument()
    })

    it('should hide banner when consent is true (granted)', () => {
      localStorage.setItem('cookie-consent', 'granted')

      renderWithProvider(<CookieConsentBanner />)

      expect(screen.queryByText(/we use cookies/i)).not.toBeInTheDocument()
    })

    it('should hide banner when consent is false (denied)', () => {
      localStorage.setItem('cookie-consent', 'denied')

      renderWithProvider(<CookieConsentBanner />)

      expect(screen.queryByText(/we use cookies/i)).not.toBeInTheDocument()
    })
  })

  describe('Accept Button', () => {
    it('should call giveConsent when Accept button is clicked', async () => {
      const user = userEvent.setup()
      renderWithProvider(<CookieConsentBanner />)

      const acceptButton = screen.getByRole('button', { name: /accept/i })
      await user.click(acceptButton)

      // Verify consent was saved
      expect(localStorage.getItem('cookie-consent')).toBe('granted')

      // Verify gtag was called
      await waitFor(() => {
        expect(gtagMock).toHaveBeenCalledWith('consent', 'update', {
          analytics_storage: 'granted',
        })
      })
    })

    it('should hide banner after accepting', async () => {
      const user = userEvent.setup()
      renderWithProvider(<CookieConsentBanner />)

      const acceptButton = screen.getByRole('button', { name: /accept/i })
      await user.click(acceptButton)

      await waitFor(() => {
        expect(screen.queryByText(/we use cookies/i)).not.toBeInTheDocument()
      })
    })
  })

  describe('Decline Button', () => {
    it('should call revokeConsent when Decline button is clicked', async () => {
      const user = userEvent.setup()
      renderWithProvider(<CookieConsentBanner />)

      const declineButton = screen.getByRole('button', { name: /decline/i })
      await user.click(declineButton)

      // Verify consent was denied
      expect(localStorage.getItem('cookie-consent')).toBe('denied')

      // Verify gtag was called
      await waitFor(() => {
        expect(gtagMock).toHaveBeenCalledWith('consent', 'update', {
          analytics_storage: 'denied',
        })
      })
    })

    it('should hide banner after declining', async () => {
      const user = userEvent.setup()
      renderWithProvider(<CookieConsentBanner />)

      const declineButton = screen.getByRole('button', { name: /decline/i })
      await user.click(declineButton)

      await waitFor(() => {
        expect(screen.queryByText(/we use cookies/i)).not.toBeInTheDocument()
      })
    })
  })

  describe('Privacy Policy Link', () => {
    it('should include a link to the privacy policy', () => {
      renderWithProvider(<CookieConsentBanner />)

      const privacyLink = screen.getByRole('link', { name: /privacy policy/i })
      expect(privacyLink).toBeInTheDocument()
      expect(privacyLink).toHaveAttribute('href', '/privacy')
    })
  })

  describe('Keyboard Accessibility', () => {
    it('should close banner when Escape key is pressed', async () => {
      const user = userEvent.setup()
      renderWithProvider(<CookieConsentBanner />)

      expect(screen.getByText(/we use cookies/i)).toBeInTheDocument()

      // Press Escape
      await user.keyboard('{Escape}')

      await waitFor(() => {
        expect(screen.queryByText(/we use cookies/i)).not.toBeInTheDocument()
      })

      // Consent should still be null (user didn't choose)
      expect(localStorage.getItem('cookie-consent')).toBeNull()
    })

    it('should accept when Enter is pressed on Accept button', async () => {
      const user = userEvent.setup()
      renderWithProvider(<CookieConsentBanner />)

      const acceptButton = screen.getByRole('button', { name: /accept/i })
      acceptButton.focus()

      // Press Enter
      await user.keyboard('{Enter}')

      await waitFor(() => {
        expect(localStorage.getItem('cookie-consent')).toBe('granted')
      })
    })

    it('should decline when Enter is pressed on Decline button', async () => {
      const user = userEvent.setup()
      renderWithProvider(<CookieConsentBanner />)

      const declineButton = screen.getByRole('button', { name: /decline/i })
      declineButton.focus()

      // Press Enter
      await user.keyboard('{Enter}')

      await waitFor(() => {
        expect(localStorage.getItem('cookie-consent')).toBe('denied')
      })
    })

    it('should allow tab navigation between buttons', async () => {
      const user = userEvent.setup()
      renderWithProvider(<CookieConsentBanner />)

      const privacyLink = screen.getByRole('link', { name: /privacy policy/i })
      const declineButton = screen.getByRole('button', { name: /decline/i })
      const acceptButton = screen.getByRole('button', { name: /accept/i })

      // Tab to privacy link (first tabbable element)
      await user.tab()
      expect(privacyLink).toHaveFocus()

      // Tab to decline button
      await user.tab()
      expect(declineButton).toHaveFocus()

      // Tab to accept button
      await user.tab()
      expect(acceptButton).toHaveFocus()
    })
  })

  describe('Content', () => {
    it('should display informative consent message', () => {
      renderWithProvider(<CookieConsentBanner />)

      // Check for analytics-related text
      expect(screen.getByText(/we use cookies/i)).toBeInTheDocument()
      // Use getAllByText since "analytics" appears multiple times in the text
      const analyticsText = screen.getAllByText(/analytics/i)
      expect(analyticsText.length).toBeGreaterThan(0)
    })

    it('should mention user experience improvement', () => {
      renderWithProvider(<CookieConsentBanner />)

      expect(screen.getByText(/improve.*experience|experience.*improve/i)).toBeInTheDocument()
    })
  })

  describe('Styling', () => {
    it('should be positioned at the bottom of the screen', () => {
      const { container } = renderWithProvider(<CookieConsentBanner />)

      const banner = container.querySelector('[role="banner"]') || container.querySelector('[class*="cookie"]')
      expect(banner).toHaveClass(/bottom|fixed/i)
    })
  })
})
