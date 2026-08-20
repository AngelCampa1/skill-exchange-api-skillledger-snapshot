import React from 'react'
import { render, screen } from '@testing-library/react'
import '@testing-library/jest-dom'
import VerifyEmailPage, { metadata } from '../page'

// Mock VerifyEmail component
jest.mock('@/components/VerifyEmail', () => {
  return function MockVerifyEmail() {
    return <div data-testid="verify-email-component">Verify Email Component</div>
  }
})

describe('VerifyEmailPage', () => {
  // ========================================
  // Basic Rendering Tests
  // ========================================
  describe('Basic Rendering', () => {
    it('should render VerifyEmail component', () => {
      render(<VerifyEmailPage />)

      expect(screen.getByTestId('verify-email-component')).toBeInTheDocument()
    })

    it('should render as a div element', () => {
      const { container } = render(<VerifyEmailPage />)

      expect(container.firstChild).toBeInstanceOf(HTMLDivElement)
    })

    it('should only render VerifyEmail component without wrapper', () => {
      const { container } = render(<VerifyEmailPage />)

      // Should only have the mocked component as direct child
      expect(container.children).toHaveLength(1)
      expect(screen.getByTestId('verify-email-component')).toBeInTheDocument()
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
      expect(metadata.title).toBe('Verify Email')
    })

    it('should have correct description in metadata', () => {
      expect(metadata.description).toBe(
        'Verify your email address to access all features of SkillLedger.'
      )
    })

    it('should have both title and description fields', () => {
      expect(metadata).toHaveProperty('title')
      expect(metadata).toHaveProperty('description')
    })

    it('should have SEO-optimized metadata fields', () => {
      const keys = Object.keys(metadata)
      expect(keys).toContain('title')
      expect(keys).toContain('description')
      // Enhanced SEO metadata now includes additional fields
      expect(keys).toContain('robots')  // noindex for auth pages
    })
  })

  // ========================================
  // Integration Tests
  // ========================================
  describe('Integration', () => {
    it('should successfully render when VerifyEmail component is available', () => {
      render(<VerifyEmailPage />)

      expect(screen.getByTestId('verify-email-component')).toBeInTheDocument()
    })

    it('should pass through to VerifyEmail component', () => {
      const { container } = render(<VerifyEmailPage />)

      expect(container).toContainElement(screen.getByTestId('verify-email-component'))
    })
  })
})
