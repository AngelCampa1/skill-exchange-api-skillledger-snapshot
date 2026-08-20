import React from 'react'
import { render, screen } from '@testing-library/react'
import '@testing-library/jest-dom'
import { Alert, AlertTitle, AlertDescription } from '../alert'

describe('Alert', () => {
  // ========================================
  // Basic Rendering Tests
  // ========================================
  describe('Basic Rendering', () => {
    it('should render with default variant', () => {
      render(<Alert>Test alert</Alert>)
      const alert = screen.getByRole('alert')
      expect(alert).toBeInTheDocument()
      expect(alert).toHaveTextContent('Test alert')
    })

    it('should render children correctly', () => {
      render(
        <Alert>
          <div>Alert content</div>
        </Alert>
      )
      expect(screen.getByText('Alert content')).toBeInTheDocument()
    })

    it('should apply custom className', () => {
      render(<Alert className="custom-alert">Alert</Alert>)
      const alert = screen.getByRole('alert')
      expect(alert).toHaveClass('custom-alert')
    })

    it('should forward refs correctly', () => {
      const ref = React.createRef<HTMLDivElement>()
      render(<Alert ref={ref}>Alert</Alert>)
      expect(ref.current).toBeInstanceOf(HTMLDivElement)
      expect(ref.current).toHaveAttribute('role', 'alert')
    })

    it('should apply base classes', () => {
      render(<Alert>Alert</Alert>)
      const alert = screen.getByRole('alert')
      expect(alert).toHaveClass('relative')
      expect(alert).toHaveClass('w-full')
      expect(alert).toHaveClass('rounded-lg')
      expect(alert).toHaveClass('border')
      expect(alert).toHaveClass('p-4')
      expect(alert).toHaveClass('flex')
      expect(alert).toHaveClass('items-start')
      expect(alert).toHaveClass('space-x-3')
    })

    it('should spread additional props', () => {
      render(<Alert data-testid="custom-alert" id="alert-1">Alert</Alert>)
      const alert = screen.getByTestId('custom-alert')
      expect(alert).toHaveAttribute('id', 'alert-1')
    })
  })

  // ========================================
  // Variant Tests
  // ========================================
  describe('Variants', () => {
    it('should apply default variant classes', () => {
      render(<Alert variant="default">Default</Alert>)
      const alert = screen.getByRole('alert')
      expect(alert).toHaveClass('bg-background')
      expect(alert).toHaveClass('text-foreground')
      expect(alert).toHaveClass('border-border')
    })

    it('should apply destructive variant classes', () => {
      render(<Alert variant="destructive">Destructive</Alert>)
      const alert = screen.getByRole('alert')
      expect(alert).toHaveClass('border-destructive/20')
      expect(alert).toHaveClass('bg-destructive/10')
      expect(alert).toHaveClass('text-destructive')
    })

    it('should apply success variant classes', () => {
      render(<Alert variant="success">Success</Alert>)
      const alert = screen.getByRole('alert')
      expect(alert).toHaveClass('border-success/20')
      expect(alert).toHaveClass('bg-success/10')
      expect(alert).toHaveClass('text-success')
    })

    it('should apply warning variant classes', () => {
      render(<Alert variant="warning">Warning</Alert>)
      const alert = screen.getByRole('alert')
      expect(alert).toHaveClass('border-warning/20')
      expect(alert).toHaveClass('bg-warning/10')
      expect(alert).toHaveClass('text-warning')
    })

    it('should apply info variant classes', () => {
      render(<Alert variant="info">Info</Alert>)
      const alert = screen.getByRole('alert')
      expect(alert).toHaveClass('border-info/20')
      expect(alert).toHaveClass('bg-info/10')
      expect(alert).toHaveClass('text-info')
    })

    it('should default to default variant when no variant specified', () => {
      render(<Alert>Default</Alert>)
      const alert = screen.getByRole('alert')
      expect(alert).toHaveClass('bg-background')
    })
  })

  // ========================================
  // Icon Tests
  // ========================================
  describe('Icon Functionality', () => {
    it('should not show icon for default variant by default', () => {
      const { container } = render(<Alert variant="default">Alert</Alert>)
      const svg = container.querySelector('svg')
      expect(svg).not.toBeInTheDocument()
    })

    it('should show AlertCircle icon for destructive variant', () => {
      const { container } = render(<Alert variant="destructive">Alert</Alert>)
      const svg = container.querySelector('svg')
      expect(svg).toBeInTheDocument()
      expect(svg).toHaveClass('h-5')
      expect(svg).toHaveClass('w-5')
      expect(svg).toHaveAttribute('aria-hidden', 'true')
    })

    it('should show CheckCircle icon for success variant', () => {
      const { container } = render(<Alert variant="success">Alert</Alert>)
      const svg = container.querySelector('svg')
      expect(svg).toBeInTheDocument()
      expect(svg).toHaveClass('flex-shrink-0')
      expect(svg).toHaveClass('mt-0.5')
    })

    it('should show AlertTriangle icon for warning variant', () => {
      const { container } = render(<Alert variant="warning">Alert</Alert>)
      const svg = container.querySelector('svg')
      expect(svg).toBeInTheDocument()
    })

    it('should show Info icon for info variant', () => {
      const { container } = render(<Alert variant="info">Alert</Alert>)
      const svg = container.querySelector('svg')
      expect(svg).toBeInTheDocument()
    })

    it('should use custom icon when provided', () => {
      render(
        <Alert icon={<span data-testid="custom-icon">⚠️</span>}>
          Alert
        </Alert>
      )
      expect(screen.getByTestId('custom-icon')).toBeInTheDocument()
      expect(screen.getByText('⚠️')).toBeInTheDocument()
    })

    it('should hide icon when showIcon is false', () => {
      const { container } = render(
        <Alert variant="destructive" showIcon={false}>
          Alert
        </Alert>
      )
      const svg = container.querySelector('svg')
      expect(svg).not.toBeInTheDocument()
    })

    it('should hide default variant icon when showIcon is false', () => {
      const { container } = render(
        <Alert variant="default" showIcon={false}>
          Alert
        </Alert>
      )
      const svg = container.querySelector('svg')
      expect(svg).not.toBeInTheDocument()
    })

    it('should prioritize custom icon over default icon', () => {
      const { container } = render(
        <Alert variant="destructive" icon={<span data-testid="custom">🔥</span>}>
          Alert
        </Alert>
      )
      expect(screen.getByTestId('custom')).toBeInTheDocument()
      // Should not have the default AlertCircle
      const svg = container.querySelector('svg')
      expect(svg).not.toBeInTheDocument()
    })

    it('should show custom icon even when showIcon is false', () => {
      render(
        <Alert
          variant="destructive"
          showIcon={false}
          icon={<span data-testid="custom">🔥</span>}
        >
          Alert
        </Alert>
      )
      // Custom icon is shown because icon || (showIcon ? defaultIcons : null)
      // When icon is provided, showIcon only controls default icons
      expect(screen.getByTestId('custom')).toBeInTheDocument()
    })
  })

  // ========================================
  // AlertTitle Tests
  // ========================================
  describe('AlertTitle', () => {
    it('should render as h5 element', () => {
      render(<AlertTitle>Title</AlertTitle>)
      const title = screen.getByText('Title')
      expect(title.tagName).toBe('H5')
    })

    it('should apply default classes', () => {
      render(<AlertTitle>Title</AlertTitle>)
      const title = screen.getByText('Title')
      expect(title).toHaveClass('mb-1')
      expect(title).toHaveClass('font-medium')
      expect(title).toHaveClass('leading-none')
      expect(title).toHaveClass('tracking-tight')
    })

    it('should apply custom className', () => {
      render(<AlertTitle className="custom-title">Title</AlertTitle>)
      const title = screen.getByText('Title')
      expect(title).toHaveClass('custom-title')
      expect(title).toHaveClass('mb-1')
    })

    it('should forward refs correctly', () => {
      const ref = React.createRef<HTMLHeadingElement>()
      render(<AlertTitle ref={ref}>Title</AlertTitle>)
      expect(ref.current).toBeInstanceOf(HTMLHeadingElement)
      expect(ref.current?.tagName).toBe('H5')
    })

    it('should render children correctly', () => {
      render(<AlertTitle><span>Complex Title</span></AlertTitle>)
      expect(screen.getByText('Complex Title')).toBeInTheDocument()
    })

    it('should spread additional props', () => {
      render(<AlertTitle id="title-1" data-testid="title">Title</AlertTitle>)
      const title = screen.getByTestId('title')
      expect(title).toHaveAttribute('id', 'title-1')
    })
  })

  // ========================================
  // AlertDescription Tests
  // ========================================
  describe('AlertDescription', () => {
    it('should render as div element', () => {
      const { container } = render(<AlertDescription>Description</AlertDescription>)
      const description = screen.getByText('Description')
      expect(description.tagName).toBe('DIV')
    })

    it('should apply default classes', () => {
      render(<AlertDescription>Description</AlertDescription>)
      const description = screen.getByText('Description')
      expect(description).toHaveClass('text-sm')
      expect(description).toHaveClass('leading-relaxed')
    })

    it('should apply custom className', () => {
      render(<AlertDescription className="custom-desc">Description</AlertDescription>)
      const description = screen.getByText('Description')
      expect(description).toHaveClass('custom-desc')
      expect(description).toHaveClass('text-sm')
    })

    it('should forward refs correctly', () => {
      const ref = React.createRef<HTMLDivElement>()
      render(<AlertDescription ref={ref}>Description</AlertDescription>)
      expect(ref.current).toBeInstanceOf(HTMLDivElement)
    })

    it('should render children correctly', () => {
      render(
        <AlertDescription>
          <p>Paragraph description</p>
        </AlertDescription>
      )
      expect(screen.getByText('Paragraph description')).toBeInTheDocument()
    })

    it('should spread additional props', () => {
      render(<AlertDescription id="desc-1" data-testid="desc">Description</AlertDescription>)
      const description = screen.getByTestId('desc')
      expect(description).toHaveAttribute('id', 'desc-1')
    })
  })

  // ========================================
  // Accessibility Tests
  // ========================================
  describe('Accessibility', () => {
    it('should have role="alert"', () => {
      render(<Alert>Alert</Alert>)
      const alert = screen.getByRole('alert')
      expect(alert).toBeInTheDocument()
    })

    it('should have aria-live="polite"', () => {
      render(<Alert>Alert</Alert>)
      const alert = screen.getByRole('alert')
      expect(alert).toHaveAttribute('aria-live', 'polite')
    })

    it('should have aria-atomic="true"', () => {
      render(<Alert>Alert</Alert>)
      const alert = screen.getByRole('alert')
      expect(alert).toHaveAttribute('aria-atomic', 'true')
    })

    it('should hide icon from screen readers with aria-hidden', () => {
      const { container } = render(<Alert variant="destructive">Alert</Alert>)
      const svg = container.querySelector('svg')
      expect(svg).toHaveAttribute('aria-hidden', 'true')
    })

    it('should be keyboard accessible', () => {
      render(<Alert tabIndex={0}>Alert</Alert>)
      const alert = screen.getByRole('alert')
      alert.focus()
      expect(alert).toHaveFocus()
    })
  })

  // ========================================
  // Edge Cases Tests
  // ========================================
  describe('Edge Cases', () => {
    it('should handle empty children', () => {
      const { container } = render(<Alert />)
      const alert = container.querySelector('[role="alert"]')
      expect(alert).toBeInTheDocument()
    })

    it('should handle numeric children', () => {
      render(<Alert>{0}</Alert>)
      expect(screen.getByText('0')).toBeInTheDocument()
    })

    it('should handle null children', () => {
      const { container } = render(<Alert>{null}</Alert>)
      const alert = container.querySelector('[role="alert"]')
      expect(alert).toBeInTheDocument()
    })

    it('should handle undefined children', () => {
      const { container } = render(<Alert>{undefined}</Alert>)
      const alert = container.querySelector('[role="alert"]')
      expect(alert).toBeInTheDocument()
    })

    it('should handle multiple children elements', () => {
      render(
        <Alert>
          <span>First</span>
          <span>Second</span>
          <span>Third</span>
        </Alert>
      )
      expect(screen.getByText('First')).toBeInTheDocument()
      expect(screen.getByText('Second')).toBeInTheDocument()
      expect(screen.getByText('Third')).toBeInTheDocument()
    })

    it('should handle long content', () => {
      const longText = 'A'.repeat(1000)
      render(<Alert>{longText}</Alert>)
      expect(screen.getByText(longText)).toBeInTheDocument()
    })

    it('should handle empty className', () => {
      render(<Alert className="">Alert</Alert>)
      const alert = screen.getByRole('alert')
      expect(alert).toBeInTheDocument()
    })

    it('should handle AlertTitle with empty content', () => {
      const { container } = render(<AlertTitle />)
      const title = container.querySelector('h5')
      expect(title).toBeInTheDocument()
    })

    it('should handle AlertDescription with empty content', () => {
      const { container } = render(<AlertDescription />)
      const desc = container.querySelector('.text-sm')
      expect(desc).toBeInTheDocument()
    })
  })

  // ========================================
  // Integration Tests
  // ========================================
  describe('Integration', () => {
    it('should work with all components together', () => {
      render(
        <Alert variant="destructive">
          <AlertTitle>Error occurred</AlertTitle>
          <AlertDescription>Something went wrong. Please try again.</AlertDescription>
        </Alert>
      )

      const alert = screen.getByRole('alert')
      expect(alert).toHaveClass('bg-destructive/10')
      expect(screen.getByText('Error occurred')).toBeInTheDocument()
      expect(screen.getByText('Something went wrong. Please try again.')).toBeInTheDocument()
    })

    it('should work with custom icon and all sub-components', () => {
      render(
        <Alert variant="info" icon={<span data-testid="icon">ℹ️</span>}>
          <AlertTitle>Information</AlertTitle>
          <AlertDescription>This is an informational message.</AlertDescription>
        </Alert>
      )

      expect(screen.getByTestId('icon')).toBeInTheDocument()
      expect(screen.getByText('Information')).toBeInTheDocument()
      expect(screen.getByText('This is an informational message.')).toBeInTheDocument()
    })

    it('should work with all variants and components', () => {
      const { rerender } = render(
        <Alert variant="success">
          <AlertTitle>Success</AlertTitle>
          <AlertDescription>Operation completed.</AlertDescription>
        </Alert>
      )

      expect(screen.getByText('Success')).toBeInTheDocument()
      expect(screen.getByRole('alert')).toHaveClass('bg-success/10')

      rerender(
        <Alert variant="warning">
          <AlertTitle>Warning</AlertTitle>
          <AlertDescription>Please be careful.</AlertDescription>
        </Alert>
      )

      expect(screen.getByText('Warning')).toBeInTheDocument()
      expect(screen.getByRole('alert')).toHaveClass('bg-warning/10')
    })

    it('should work with custom classes on all components', () => {
      render(
        <Alert className="custom-alert" variant="info">
          <AlertTitle className="custom-title">Title</AlertTitle>
          <AlertDescription className="custom-desc">Description</AlertDescription>
        </Alert>
      )

      const alert = screen.getByRole('alert')
      expect(alert).toHaveClass('custom-alert')
      expect(screen.getByText('Title')).toHaveClass('custom-title')
      expect(screen.getByText('Description')).toHaveClass('custom-desc')
    })

    it('should maintain structure with refs on all components', () => {
      const alertRef = React.createRef<HTMLDivElement>()
      const titleRef = React.createRef<HTMLHeadingElement>()
      const descRef = React.createRef<HTMLDivElement>()

      render(
        <Alert ref={alertRef} variant="success">
          <AlertTitle ref={titleRef}>Success</AlertTitle>
          <AlertDescription ref={descRef}>Operation completed</AlertDescription>
        </Alert>
      )

      expect(alertRef.current).toBeInstanceOf(HTMLDivElement)
      expect(titleRef.current).toBeInstanceOf(HTMLHeadingElement)
      expect(descRef.current).toBeInstanceOf(HTMLDivElement)
    })
  })
})
