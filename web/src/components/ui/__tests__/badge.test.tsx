import React from 'react'
import { render, screen } from '@testing-library/react'
import '@testing-library/jest-dom'
import { Badge } from '../badge'

describe('Badge', () => {
  // ========================================
  // Basic Rendering Tests
  // ========================================
  describe('Basic Rendering', () => {
    it('should render as span element', () => {
      const { container } = render(<Badge>Badge</Badge>)
      const badge = screen.getByText('Badge')
      expect(badge.tagName).toBe('SPAN')
    })

    it('should render children correctly', () => {
      render(<Badge>Test Badge</Badge>)
      expect(screen.getByText('Test Badge')).toBeInTheDocument()
    })

    it('should apply base classes', () => {
      render(<Badge>Badge</Badge>)
      const badge = screen.getByText('Badge')
      expect(badge).toHaveClass('inline-flex')
      expect(badge).toHaveClass('items-center')
      expect(badge).toHaveClass('rounded-full')
      expect(badge).toHaveClass('border')
      expect(badge).toHaveClass('font-semibold')
      expect(badge).toHaveClass('transition-colors')
    })

    it('should apply custom className', () => {
      render(<Badge className="custom-badge">Badge</Badge>)
      const badge = screen.getByText('Badge')
      expect(badge).toHaveClass('custom-badge')
      expect(badge).toHaveClass('inline-flex')
    })

    it('should forward refs correctly', () => {
      const ref = React.createRef<HTMLSpanElement>()
      render(<Badge ref={ref}>Badge</Badge>)
      expect(ref.current).toBeInstanceOf(HTMLSpanElement)
    })

    it('should spread additional props', () => {
      render(<Badge data-testid="badge" id="badge-1">Badge</Badge>)
      const badge = screen.getByTestId('badge')
      expect(badge).toHaveAttribute('id', 'badge-1')
    })

    it('should apply focus ring classes', () => {
      render(<Badge>Badge</Badge>)
      const badge = screen.getByText('Badge')
      expect(badge).toHaveClass('focus:outline-none')
      expect(badge).toHaveClass('focus:ring-2')
      expect(badge).toHaveClass('focus:ring-ring')
      expect(badge).toHaveClass('focus:ring-offset-2')
    })
  })

  // ========================================
  // Variant Tests
  // ========================================
  describe('Variants', () => {
    it('should apply default variant classes', () => {
      render(<Badge variant="default">Default</Badge>)
      const badge = screen.getByText('Default')
      expect(badge).toHaveClass('border-transparent')
      expect(badge).toHaveClass('bg-primary')
      expect(badge).toHaveClass('text-primary-foreground')
      expect(badge).toHaveClass('hover:bg-primary/80')
    })

    it('should apply secondary variant classes', () => {
      render(<Badge variant="secondary">Secondary</Badge>)
      const badge = screen.getByText('Secondary')
      expect(badge).toHaveClass('bg-secondary')
      expect(badge).toHaveClass('text-secondary-foreground')
      expect(badge).toHaveClass('hover:bg-secondary/80')
    })

    it('should apply destructive variant classes', () => {
      render(<Badge variant="destructive">Destructive</Badge>)
      const badge = screen.getByText('Destructive')
      expect(badge).toHaveClass('bg-destructive')
      expect(badge).toHaveClass('text-destructive-foreground')
      expect(badge).toHaveClass('hover:bg-destructive/80')
    })

    it('should apply outline variant classes', () => {
      render(<Badge variant="outline">Outline</Badge>)
      const badge = screen.getByText('Outline')
      expect(badge).toHaveClass('text-foreground')
      expect(badge).toHaveClass('border-border')
      expect(badge).toHaveClass('hover:bg-accent')
    })

    it('should apply success variant classes', () => {
      render(<Badge variant="success">Success</Badge>)
      const badge = screen.getByText('Success')
      expect(badge).toHaveClass('bg-success')
      expect(badge).toHaveClass('text-success-foreground')
      expect(badge).toHaveClass('hover:bg-success/80')
    })

    it('should apply warning variant classes', () => {
      render(<Badge variant="warning">Warning</Badge>)
      const badge = screen.getByText('Warning')
      expect(badge).toHaveClass('bg-warning')
      expect(badge).toHaveClass('text-warning-foreground')
      expect(badge).toHaveClass('hover:bg-warning/80')
    })

    it('should apply info variant classes', () => {
      render(<Badge variant="info">Info</Badge>)
      const badge = screen.getByText('Info')
      expect(badge).toHaveClass('bg-info')
      expect(badge).toHaveClass('text-info-foreground')
      expect(badge).toHaveClass('hover:bg-info/80')
    })

    it('should default to default variant when no variant specified', () => {
      render(<Badge>Badge</Badge>)
      const badge = screen.getByText('Badge')
      expect(badge).toHaveClass('bg-primary')
    })
  })

  // ========================================
  // Size Tests
  // ========================================
  describe('Sizes', () => {
    it('should apply sm size classes', () => {
      render(<Badge size="sm">Small</Badge>)
      const badge = screen.getByText('Small')
      expect(badge).toHaveClass('px-2')
      expect(badge).toHaveClass('py-0.5')
      expect(badge).toHaveClass('text-xs')
    })

    it('should apply md size classes', () => {
      render(<Badge size="md">Medium</Badge>)
      const badge = screen.getByText('Medium')
      expect(badge).toHaveClass('px-2.5')
      expect(badge).toHaveClass('py-0.5')
      expect(badge).toHaveClass('text-sm')
    })

    it('should apply lg size classes', () => {
      render(<Badge size="lg">Large</Badge>)
      const badge = screen.getByText('Large')
      expect(badge).toHaveClass('px-3')
      expect(badge).toHaveClass('py-1')
      expect(badge).toHaveClass('text-base')
    })

    it('should default to sm size when no size specified', () => {
      render(<Badge>Badge</Badge>)
      const badge = screen.getByText('Badge')
      expect(badge).toHaveClass('px-2')
      expect(badge).toHaveClass('text-xs')
    })
  })

  // ========================================
  // Edge Cases Tests
  // ========================================
  describe('Edge Cases', () => {
    it('should handle empty children', () => {
      const { container } = render(<Badge />)
      const badge = container.querySelector('span')
      expect(badge).toBeInTheDocument()
    })

    it('should handle numeric children', () => {
      render(<Badge>{42}</Badge>)
      expect(screen.getByText('42')).toBeInTheDocument()
    })

    it('should handle zero as children', () => {
      render(<Badge>{0}</Badge>)
      expect(screen.getByText('0')).toBeInTheDocument()
    })

    it('should handle null children', () => {
      const { container } = render(<Badge>{null}</Badge>)
      const badge = container.querySelector('span')
      expect(badge).toBeInTheDocument()
    })

    it('should handle undefined children', () => {
      const { container } = render(<Badge>{undefined}</Badge>)
      const badge = container.querySelector('span')
      expect(badge).toBeInTheDocument()
    })

    it('should handle complex children', () => {
      render(
        <Badge>
          <span>Icon</span> Text
        </Badge>
      )
      expect(screen.getByText('Icon')).toBeInTheDocument()
      expect(screen.getByText(/Text/)).toBeInTheDocument()
    })

    it('should handle long text content', () => {
      const longText = 'A'.repeat(100)
      render(<Badge>{longText}</Badge>)
      expect(screen.getByText(longText)).toBeInTheDocument()
    })

    it('should handle empty className', () => {
      render(<Badge className="">Badge</Badge>)
      const badge = screen.getByText('Badge')
      expect(badge).toHaveClass('inline-flex')
    })

    it('should handle special characters in children', () => {
      render(<Badge>@#$%^&*()</Badge>)
      expect(screen.getByText('@#$%^&*()')).toBeInTheDocument()
    })
  })

  // ========================================
  // Integration Tests
  // ========================================
  describe('Integration', () => {
    it('should work with all props combined', () => {
      render(
        <Badge
          variant="destructive"
          size="lg"
          className="custom-class"
          data-testid="integrated-badge"
          id="badge-1"
        >
          Delete
        </Badge>
      )

      const badge = screen.getByTestId('integrated-badge')
      expect(badge).toHaveClass('bg-destructive')
      expect(badge).toHaveClass('px-3')
      expect(badge).toHaveClass('text-base')
      expect(badge).toHaveClass('custom-class')
      expect(badge).toHaveAttribute('id', 'badge-1')
      expect(badge).toHaveTextContent('Delete')
    })

    it('should support variant changes via rerender', () => {
      const { rerender } = render(<Badge variant="default">Status</Badge>)
      let badge = screen.getByText('Status')
      expect(badge).toHaveClass('bg-primary')

      rerender(<Badge variant="success">Status</Badge>)
      badge = screen.getByText('Status')
      expect(badge).toHaveClass('bg-success')

      rerender(<Badge variant="destructive">Status</Badge>)
      badge = screen.getByText('Status')
      expect(badge).toHaveClass('bg-destructive')
    })

    it('should support size changes via rerender', () => {
      const { rerender } = render(<Badge size="sm">Badge</Badge>)
      let badge = screen.getByText('Badge')
      expect(badge).toHaveClass('text-xs')

      rerender(<Badge size="md">Badge</Badge>)
      badge = screen.getByText('Badge')
      expect(badge).toHaveClass('text-sm')

      rerender(<Badge size="lg">Badge</Badge>)
      badge = screen.getByText('Badge')
      expect(badge).toHaveClass('text-base')
    })

    it('should maintain ref through rerenders', () => {
      const ref = React.createRef<HTMLSpanElement>()
      const { rerender } = render(
        <Badge ref={ref} variant="default">
          Badge
        </Badge>
      )

      const initialRef = ref.current
      expect(initialRef).toBeInstanceOf(HTMLSpanElement)

      rerender(
        <Badge ref={ref} variant="success">
          Badge
        </Badge>
      )

      expect(ref.current).toBe(initialRef)
    })

    it('should work with icon and text', () => {
      render(
        <Badge variant="info" size="md">
          <svg data-testid="icon" />
          <span>New</span>
        </Badge>
      )

      expect(screen.getByTestId('icon')).toBeInTheDocument()
      expect(screen.getByText('New')).toBeInTheDocument()
    })

    it('should support onClick handler', () => {
      const handleClick = jest.fn()
      render(<Badge onClick={handleClick}>Clickable</Badge>)

      const badge = screen.getByText('Clickable')
      badge.click()

      expect(handleClick).toHaveBeenCalledTimes(1)
    })

    it('should be keyboard focusable with tabIndex', () => {
      render(<Badge tabIndex={0}>Focusable</Badge>)
      const badge = screen.getByText('Focusable')
      badge.focus()
      expect(badge).toHaveFocus()
    })
  })
})
