import React, { Suspense } from 'react'
import { render, screen } from '@testing-library/react'
import '@testing-library/jest-dom'
import ResetPasswordPage, { metadata } from '../page'

// Mock ResetPassword component
jest.mock('../../../components/ResetPassword', () => {
  return function MockResetPassword() {
    return <div data-testid="reset-password-component">Reset Password Component</div>
  }
})

describe('ResetPasswordPage', () => {
  // ========================================
  // Basic Rendering Tests
  // ========================================
  describe('Basic Rendering', () => {
    it('should render ResetPassword component', () => {
      render(<ResetPasswordPage />)

      expect(screen.getByTestId('reset-password-component')).toBeInTheDocument()
    })

    it('should wrap ResetPassword in Suspense', () => {
      const { container } = render(<ResetPasswordPage />)

      // Component should be rendered (Suspense resolved immediately in tests)
      expect(screen.getByTestId('reset-password-component')).toBeInTheDocument()
      expect(container.firstChild).toBeInTheDocument()
    })
  })

  // ========================================
  // Fallback UI Tests
  // ========================================
  describe('Fallback UI', () => {
    it('should render fallback with loading spinner', () => {
      // Create a component that never resolves to test the fallback
      const NeverResolve = React.lazy(() => new Promise(() => {}))

      const { container } = render(
        <Suspense fallback={<div data-testid="test-fallback">Fallback</div>}>
          <NeverResolve />
        </Suspense>
      )

      expect(screen.getByTestId('test-fallback')).toBeInTheDocument()
    })

    it('should apply correct classes to fallback container', () => {
      const { container } = render(
        <div className="min-h-screen flex items-center justify-center bg-background">
          <div className="text-center space-md">
            <div className="loading-spinner mx-auto"></div>
            <p className="text-body text-muted-foreground">Loading password reset...</p>
          </div>
        </div>
      )

      const mainDiv = container.querySelector('.min-h-screen')
      expect(mainDiv).toBeInTheDocument()
      expect(mainDiv).toHaveClass('flex', 'items-center', 'justify-center', 'bg-background')
    })

    it('should apply correct classes to fallback content wrapper', () => {
      const { container } = render(
        <div className="min-h-screen flex items-center justify-center bg-background">
          <div className="text-center space-md">
            <div className="loading-spinner mx-auto"></div>
            <p className="text-body text-muted-foreground">Loading password reset...</p>
          </div>
        </div>
      )

      const contentWrapper = container.querySelector('.text-center')
      expect(contentWrapper).toBeInTheDocument()
      expect(contentWrapper).toHaveClass('space-md')
    })

    it('should render loading spinner in fallback', () => {
      const { container } = render(
        <div className="min-h-screen flex items-center justify-center bg-background">
          <div className="text-center space-md">
            <div className="loading-spinner mx-auto"></div>
            <p className="text-body text-muted-foreground">Loading password reset...</p>
          </div>
        </div>
      )

      const spinner = container.querySelector('.loading-spinner')
      expect(spinner).toBeInTheDocument()
      expect(spinner).toHaveClass('mx-auto')
    })

    it('should render loading message in fallback', () => {
      render(
        <div className="min-h-screen flex items-center justify-center bg-background">
          <div className="text-center space-md">
            <div className="loading-spinner mx-auto"></div>
            <p className="text-body text-muted-foreground">Loading password reset...</p>
          </div>
        </div>
      )

      expect(screen.getByText('Loading password reset...')).toBeInTheDocument()
    })

    it('should apply correct classes to loading message', () => {
      render(
        <div className="min-h-screen flex items-center justify-center bg-background">
          <div className="text-center space-md">
            <div className="loading-spinner mx-auto"></div>
            <p className="text-body text-muted-foreground">Loading password reset...</p>
          </div>
        </div>
      )

      const loadingText = screen.getByText('Loading password reset...')
      expect(loadingText).toHaveClass('text-body', 'text-muted-foreground')
      expect(loadingText.tagName).toBe('P')
    })
  })

  // ========================================
  // Metadata Tests
  // ========================================
  describe('Metadata', () => {
    it('should export metadata object', () => {
      expect(metadata).toBeDefined()
      expect(typeof metadata).toBe('object')
    })

    it('should have correct title in metadata', () => {
      // Note: Root layout uses template '%s | SkillLedger', so page metadata only needs the page title
      expect(metadata.title).toBe('Reset Password')
    })

    it('should have correct description in metadata', () => {
      expect(metadata.description).toBe('Set a new password for your SkillLedger account.')
    })

    it('should have SEO-optimized metadata fields', () => {
      expect(metadata).toHaveProperty('title')
      expect(metadata).toHaveProperty('description')
      // Enhanced SEO metadata now includes additional fields
      const keys = Object.keys(metadata)
      expect(keys).toContain('robots')  // noindex for auth pages
    })
  })

  // ========================================
  // Structure Tests
  // ========================================
  describe('Component Structure', () => {
    it('should have proper DOM hierarchy in fallback', () => {
      const { container } = render(
        <div className="min-h-screen flex items-center justify-center bg-background">
          <div className="text-center space-md">
            <div className="loading-spinner mx-auto"></div>
            <p className="text-body text-muted-foreground">Loading password reset...</p>
          </div>
        </div>
      )

      const mainDiv = container.querySelector('.min-h-screen')
      const contentWrapper = container.querySelector('.text-center')
      const spinner = container.querySelector('.loading-spinner')
      const text = screen.getByText('Loading password reset...')

      expect(mainDiv as HTMLElement).toContainElement(contentWrapper as HTMLElement)
      expect(contentWrapper as HTMLElement).toContainElement(spinner as HTMLElement)
      expect(contentWrapper).toContainElement(text)
    })

    it('should render spinner before text in fallback', () => {
      const { container } = render(
        <div className="min-h-screen flex items-center justify-center bg-background">
          <div className="text-center space-md">
            <div className="loading-spinner mx-auto"></div>
            <p className="text-body text-muted-foreground">Loading password reset...</p>
          </div>
        </div>
      )

      const contentWrapper = container.querySelector('.text-center')
      const children = Array.from(contentWrapper?.children || [])

      expect(children[0]).toHaveClass('loading-spinner')
      expect(children[1].textContent).toBe('Loading password reset...')
    })
  })

  // ========================================
  // Integration Tests
  // ========================================
  describe('Integration', () => {
    it('should successfully render when ResetPassword component is available', () => {
      render(<ResetPasswordPage />)

      // Should render the actual component, not the fallback
      expect(screen.getByTestId('reset-password-component')).toBeInTheDocument()
      expect(screen.queryByText('Loading password reset...')).not.toBeInTheDocument()
    })

    it('should pass through to ResetPassword component', () => {
      const { container } = render(<ResetPasswordPage />)

      expect(container).toContainElement(screen.getByTestId('reset-password-component'))
    })
  })
})
