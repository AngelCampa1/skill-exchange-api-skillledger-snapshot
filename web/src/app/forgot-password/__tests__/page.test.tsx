import React from 'react'
import { render, screen } from '@testing-library/react'
import '@testing-library/jest-dom'
import ForgotPasswordPage, { metadata } from '../page'

// Mock ForgotPassword component
jest.mock('../../../components/ForgotPassword', () => {
  return function MockForgotPassword() {
    return <div data-testid="forgot-password-component">Forgot Password Component</div>
  }
})

describe('ForgotPasswordPage', () => {
  // ========================================
  // Basic Rendering Tests
  // ========================================
  describe('Basic Rendering', () => {
    it('should render ForgotPassword component', () => {
      render(<ForgotPasswordPage />)

      expect(screen.getByTestId('forgot-password-component')).toBeInTheDocument()
    })

    it('should render as a div element', () => {
      const { container } = render(<ForgotPasswordPage />)

      expect(container.firstChild).toBeInstanceOf(HTMLDivElement)
    })

    it('should only render ForgotPassword component without wrapper', () => {
      const { container } = render(<ForgotPasswordPage />)

      // Should only have the mocked component as direct child
      expect(container.children).toHaveLength(1)
      expect(screen.getByTestId('forgot-password-component')).toBeInTheDocument()
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
      expect(metadata.title).toBe('Forgot Password')
    })

    it('should have correct description in metadata', () => {
      expect(metadata.description).toBe(
        'Reset your SkillLedger account password by entering your email address.'
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
    it('should successfully render when ForgotPassword component is available', () => {
      render(<ForgotPasswordPage />)

      expect(screen.getByTestId('forgot-password-component')).toBeInTheDocument()
    })

    it('should pass through to ForgotPassword component', () => {
      const { container } = render(<ForgotPasswordPage />)

      expect(container).toContainElement(screen.getByTestId('forgot-password-component'))
    })
  })
})
