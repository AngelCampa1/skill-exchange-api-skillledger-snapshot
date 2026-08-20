/**
 * Tests for root layout (app/layout.tsx)
 *
 * Comprehensive test suite for application root layout
 * Coverage target: 95%+ (52 lines)
 *
 * Known issues to test:
 * - BUG-HIGH-006: ErrorBoundary prevents complete crashes (fixed - wrapped at root)
 */

import React from 'react'
import { render, screen } from '@testing-library/react'
import '@testing-library/jest-dom'
import RootLayout, { metadata } from '../layout'

// Mock all providers and components
jest.mock('@/contexts/AuthContext', () => ({
  AuthProvider: ({ children }: { children: React.ReactNode }) => (
    <div data-testid="auth-provider">{children}</div>
  ),
  useAuth: () => ({ isAuthenticated: false }),
}))

jest.mock('@/contexts/ThemeContext', () => ({
  ThemeProvider: ({ children }: { children: React.ReactNode }) => (
    <div data-testid="theme-provider">{children}</div>
  ),
}))

jest.mock('@/contexts/CookieConsentContext', () => ({
  CookieConsentProvider: ({ children }: { children: React.ReactNode }) => (
    <div data-testid="cookie-consent-provider">{children}</div>
  ),
}))

jest.mock('@/components/ErrorBoundary', () => {
  return function ErrorBoundary({ children }: { children: React.ReactNode }) {
    return <div data-testid="error-boundary">{children}</div>
  }
})

jest.mock('@/components/feedback/VentoraFeedbackWidget', () => {
  return function VentoraFeedbackWidget() {
    return <div data-testid="ventora-feedback-widget">Ventora Feedback</div>
  }
})

jest.mock('@/components/analytics/AnalyticsScripts', () => {
  return function AnalyticsScripts() {
    return <div data-testid="analytics-scripts">Analytics</div>
  }
})

jest.mock('@/components/analytics/PageViewTracker', () => {
  return function PageViewTracker() {
    return <div data-testid="page-view-tracker">PageView</div>
  }
})

jest.mock('@/components/cookies/CookieConsentBanner', () => {
  return function CookieConsentBanner() {
    return <div data-testid="cookie-consent-banner">Cookie Consent</div>
  }
})

describe('RootLayout', () => {
  describe('Metadata', () => {
    it('exports correct metadata title with template', () => {
      // Root layout uses title template for SEO
      expect(metadata.title).toEqual({
        default: 'SkillLedger - Professional Collaboration Platform',
        template: '%s | SkillLedger',
      })
    })

    it('exports correct metadata description', () => {
      expect(metadata.description).toBe(
        'Exchange professional services across 19 skill categories in 50 US cities. 30-day free trial — no cash, no commissions. Join SkillLedger today.'
      )
    })

    it('exports correct manifest path', () => {
      expect(metadata.manifest).toBe('/site.webmanifest')
    })

    it('exports metadata with required SEO fields', () => {
      expect(metadata).toHaveProperty('title')
      expect(metadata).toHaveProperty('description')
      expect(metadata).toHaveProperty('keywords')
      expect(metadata).toHaveProperty('openGraph')
      expect(metadata).toHaveProperty('twitter')
      expect(metadata).toHaveProperty('icons')
      expect(metadata).toHaveProperty('manifest')
    })
  })

  describe('Layout Structure', () => {
    it('renders without crashing', () => {
      render(
        <RootLayout>
          <div>Test Child</div>
        </RootLayout>
      )

      expect(screen.getByText('Test Child')).toBeInTheDocument()
    })

    it('renders ErrorBoundary at root level (BUG-HIGH-006 fix)', () => {
      render(
        <RootLayout>
          <div>Test Child</div>
        </RootLayout>
      )

      const errorBoundary = screen.getByTestId('error-boundary')
      expect(errorBoundary).toBeInTheDocument()
    })

    it('renders ThemeProvider', () => {
      render(
        <RootLayout>
          <div>Test Child</div>
        </RootLayout>
      )

      const themeProvider = screen.getByTestId('theme-provider')
      expect(themeProvider).toBeInTheDocument()
    })

    it('renders CookieConsentProvider', () => {
      render(
        <RootLayout>
          <div>Test Child</div>
        </RootLayout>
      )

      const cookieConsentProvider = screen.getByTestId('cookie-consent-provider')
      expect(cookieConsentProvider).toBeInTheDocument()
    })

    it('renders AuthProvider', () => {
      render(
        <RootLayout>
          <div>Test Child</div>
        </RootLayout>
      )

      const authProvider = screen.getByTestId('auth-provider')
      expect(authProvider).toBeInTheDocument()
    })

    it('renders children content', () => {
      render(
        <RootLayout>
          <div data-testid="test-content">Main Content</div>
        </RootLayout>
      )

      expect(screen.getByTestId('test-content')).toBeInTheDocument()
      expect(screen.getByText('Main Content')).toBeInTheDocument()
    })
  })

  describe('Component Hierarchy', () => {
    it('renders providers in correct nesting order', () => {
      const { container } = render(
        <RootLayout>
          <div>Test Child</div>
        </RootLayout>
      )

      const errorBoundary = screen.getByTestId('error-boundary')
      const themeProvider = screen.getByTestId('theme-provider')
      const cookieConsentProvider = screen.getByTestId('cookie-consent-provider')
      const authProvider = screen.getByTestId('auth-provider')

      // ErrorBoundary should contain ThemeProvider
      expect(errorBoundary).toContainElement(themeProvider)
      // ThemeProvider should contain CookieConsentProvider
      expect(themeProvider).toContainElement(cookieConsentProvider)
      // CookieConsentProvider should contain AuthProvider
      expect(cookieConsentProvider).toContainElement(authProvider)
    })
  })

  describe('Additional Components', () => {
    it('renders VentoraFeedbackWidget', () => {
      render(
        <RootLayout>
          <div>Test Child</div>
        </RootLayout>
      )

      expect(screen.getByTestId('ventora-feedback-widget')).toBeInTheDocument()
    })

    it('does not render FeedbackButton on the root marketing layout', () => {
      render(
        <RootLayout>
          <div>Test Child</div>
        </RootLayout>
      )

      expect(screen.queryByTestId('feedback-button')).not.toBeInTheDocument()
    })

    it('renders PageViewTracker', () => {
      render(
        <RootLayout>
          <div>Test Child</div>
        </RootLayout>
      )

      expect(screen.getByTestId('page-view-tracker')).toBeInTheDocument()
    })

    it('renders CookieConsentBanner', () => {
      render(
        <RootLayout>
          <div>Test Child</div>
        </RootLayout>
      )

      expect(screen.getByTestId('cookie-consent-banner')).toBeInTheDocument()
    })

    it('renders AnalyticsScripts', () => {
      render(
        <RootLayout>
          <div>Test Child</div>
        </RootLayout>
      )

      expect(screen.getByTestId('analytics-scripts')).toBeInTheDocument()
    })
  })

  describe('HTML Structure', () => {
    // Note: React testing library renders into a <div>, so <html> and <body>
    // tags from RootLayout are not queryable via container.querySelector.
    // We verify the layout renders without errors and contains children.

    it('renders layout wrapper without crashing', () => {
      const { container } = render(
        <RootLayout>
          <div data-testid="layout-child">Test Child</div>
        </RootLayout>
      )

      expect(container.firstChild).toBeTruthy()
      expect(screen.getByTestId('layout-child')).toBeInTheDocument()
    })

    it('renders all provider wrappers', () => {
      render(
        <RootLayout>
          <div>Test Child</div>
        </RootLayout>
      )

      expect(screen.getByTestId('theme-provider')).toBeInTheDocument()
      expect(screen.getByTestId('auth-provider')).toBeInTheDocument()
      expect(screen.getByTestId('cookie-consent-provider')).toBeInTheDocument()
    })

    it('renders child content within layout', () => {
      render(
        <RootLayout>
          <div>Unique Child Content</div>
        </RootLayout>
      )

      expect(screen.getByText('Unique Child Content')).toBeInTheDocument()
    })
  })

  describe('Multiple Children', () => {
    it('renders multiple children correctly', () => {
      render(
        <RootLayout>
          <div>
            <h1>Title</h1>
            <p>Paragraph</p>
            <span>Span</span>
          </div>
        </RootLayout>
      )

      expect(screen.getByText('Title')).toBeInTheDocument()
      expect(screen.getByText('Paragraph')).toBeInTheDocument()
      expect(screen.getByText('Span')).toBeInTheDocument()
    })
  })
})
