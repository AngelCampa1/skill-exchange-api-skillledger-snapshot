/**
 * Unit tests for VentoraFeedbackWidget (Ventora CRM feedback-button embed).
 *
 * The component is double-gated:
 *   1. Route gate — renders only on authenticated app routes
 *      (shouldRenderVentoraFeedbackWidget), never on public/marketing/login.
 *   2. Env gate — renders only when NEXT_PUBLIC_CRM_WIDGET_KEY is set.
 *
 * Script injection is handled by next/script (mocked here) — we verify the
 * props passed to it rather than actual DOM injection.
 */

import React from 'react'
import { render } from '@testing-library/react'
import VentoraFeedbackWidget, {
  shouldRenderVentoraFeedbackWidget,
} from '../VentoraFeedbackWidget'

// next/script is a server/client component that doesn't run in jsdom —
// mock it as a plain <script> element so we can assert on its props.
jest.mock('next/script', () => {
  // eslint-disable-next-line @typescript-eslint/no-explicit-any
  return function MockScript(props: any) {
    // Render a <script> tag with all forwarded attributes for easy querying
    const { src, strategy: _strategy, ...rest } = props
    return React.createElement('script', { src, ...rest })
  }
})

// usePathname drives the route gate; mock it so each test can set the path.
let mockPathname = '/dashboard'
jest.mock('next/navigation', () => ({
  usePathname: () => mockPathname,
}))

const ORIGINAL_ENV = process.env

beforeEach(() => {
  jest.resetModules()
  process.env = { ...ORIGINAL_ENV }
  mockPathname = '/dashboard'
})

afterEach(() => {
  process.env = ORIGINAL_ENV
})

describe('shouldRenderVentoraFeedbackWidget (route gate)', () => {
  it('returns false on public/marketing routes', () => {
    expect(shouldRenderVentoraFeedbackWidget('/')).toBe(false)
    expect(shouldRenderVentoraFeedbackWidget('/pricing')).toBe(false)
    expect(shouldRenderVentoraFeedbackWidget('/features')).toBe(false)
    expect(shouldRenderVentoraFeedbackWidget('/about')).toBe(false)
  })

  it('returns false on auth-entry routes (login/register)', () => {
    expect(shouldRenderVentoraFeedbackWidget('/login')).toBe(false)
    expect(shouldRenderVentoraFeedbackWidget('/register')).toBe(false)
  })

  it('returns true on authenticated app routes', () => {
    expect(shouldRenderVentoraFeedbackWidget('/dashboard')).toBe(true)
    expect(shouldRenderVentoraFeedbackWidget('/dashboard/overview')).toBe(true)
    expect(shouldRenderVentoraFeedbackWidget('/profile')).toBe(true)
    expect(shouldRenderVentoraFeedbackWidget('/messages')).toBe(true)
    expect(shouldRenderVentoraFeedbackWidget('/wallet')).toBe(true)
  })

  it('handles null/undefined pathname', () => {
    expect(shouldRenderVentoraFeedbackWidget(null)).toBe(false)
    expect(shouldRenderVentoraFeedbackWidget(undefined)).toBe(false)
  })
})

describe('VentoraFeedbackWidget', () => {
  describe('on an authenticated route with NEXT_PUBLIC_CRM_WIDGET_KEY set', () => {
    beforeEach(() => {
      mockPathname = '/dashboard'
      process.env.NEXT_PUBLIC_CRM_WIDGET_KEY = 'wk_test_key_abc123'
      delete process.env.NEXT_PUBLIC_CRM_LOADER_URL
    })

    it('renders a script tag with data-widget="feedback-button"', () => {
      const { container } = render(React.createElement(VentoraFeedbackWidget))
      const script = container.querySelector('script')
      expect(script).not.toBeNull()
      expect(script?.getAttribute('data-widget')).toBe('feedback-button')
    })

    it('sets data-product to the env key value', () => {
      const { container } = render(React.createElement(VentoraFeedbackWidget))
      const script = container.querySelector('script')
      expect(script?.getAttribute('data-product')).toBe('wk_test_key_abc123')
    })

    it('defaults src to https://crm.example.com/w/v1.js', () => {
      const { container } = render(React.createElement(VentoraFeedbackWidget))
      const script = container.querySelector('script')
      expect(script?.getAttribute('src')).toBe('https://crm.example.com/w/v1.js')
    })

    it('uses NEXT_PUBLIC_CRM_LOADER_URL when provided', () => {
      process.env.NEXT_PUBLIC_CRM_LOADER_URL = 'https://custom.example.com/w/v1.js'
      const { container } = render(React.createElement(VentoraFeedbackWidget))
      const script = container.querySelector('script')
      expect(script?.getAttribute('src')).toBe('https://custom.example.com/w/v1.js')
    })
  })

  describe('route gating', () => {
    beforeEach(() => {
      process.env.NEXT_PUBLIC_CRM_WIDGET_KEY = 'wk_test_key_abc123'
    })

    it('does NOT render on a public route even when the key is set', () => {
      mockPathname = '/pricing'
      const { container } = render(React.createElement(VentoraFeedbackWidget))
      expect(container.firstChild).toBeNull()
    })

    it('does NOT render on the login route', () => {
      mockPathname = '/login'
      const { container } = render(React.createElement(VentoraFeedbackWidget))
      expect(container.firstChild).toBeNull()
    })

    it('DOES render on an authenticated route', () => {
      mockPathname = '/dashboard'
      const { container } = render(React.createElement(VentoraFeedbackWidget))
      expect(container.querySelector('script')).not.toBeNull()
    })
  })

  describe('when NEXT_PUBLIC_CRM_WIDGET_KEY is not set', () => {
    beforeEach(() => {
      mockPathname = '/dashboard'
      delete process.env.NEXT_PUBLIC_CRM_WIDGET_KEY
    })

    it('renders nothing', () => {
      const { container } = render(React.createElement(VentoraFeedbackWidget))
      expect(container.firstChild).toBeNull()
    })
  })
})
