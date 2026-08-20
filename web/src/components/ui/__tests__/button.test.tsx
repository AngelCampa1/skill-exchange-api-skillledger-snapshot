import React from 'react'
import { render, screen, fireEvent } from '@testing-library/react'
import '@testing-library/jest-dom'
import { Button } from '../button'

describe('Button', () => {
  // ========================================
  // Basic Rendering Tests
  // ========================================
  describe('Basic Rendering', () => {
    it('should render button element', () => {
      render(<Button>Click me</Button>)
      const button = screen.getByRole('button', { name: 'Click me' })
      expect(button).toBeInTheDocument()
      expect(button.tagName).toBe('BUTTON')
    })

    it('should render children content', () => {
      render(<Button>Button Text</Button>)
      expect(screen.getByText('Button Text')).toBeInTheDocument()
    })

    it('should default to type="button"', () => {
      render(<Button>Click me</Button>)
      const button = screen.getByRole('button')
      expect(button).toHaveAttribute('type', 'button')
    })

    it('should support custom type attribute', () => {
      render(<Button type="submit">Submit</Button>)
      const button = screen.getByRole('button')
      expect(button).toHaveAttribute('type', 'submit')
    })

    it('should apply custom className', () => {
      render(<Button className="custom-class">Click me</Button>)
      const button = screen.getByRole('button')
      expect(button).toHaveClass('custom-class')
    })

    it('should forward ref to button element', () => {
      const ref = React.createRef<HTMLButtonElement>()
      render(<Button ref={ref}>Click me</Button>)
      expect(ref.current).toBeInstanceOf(HTMLButtonElement)
      expect(ref.current?.textContent).toBe('Click me')
    })

    it('should pass through additional props', () => {
      render(
        <Button data-testid="custom-button" aria-label="Custom Button">
          Click me
        </Button>
      )
      const button = screen.getByTestId('custom-button')
      expect(button).toHaveAttribute('aria-label', 'Custom Button')
    })
  })

  // ========================================
  // Variant Tests
  // ========================================
  describe('Variants', () => {
    it('should apply default variant classes', () => {
      render(<Button variant="default">Default</Button>)
      const button = screen.getByRole('button')
      expect(button).toHaveClass('bg-primary')
      expect(button).toHaveClass('text-primary-foreground')
    })

    it('should apply destructive variant classes', () => {
      render(<Button variant="destructive">Delete</Button>)
      const button = screen.getByRole('button')
      expect(button).toHaveClass('bg-destructive')
      expect(button).toHaveClass('text-destructive-foreground')
    })

    it('should apply outline variant classes', () => {
      render(<Button variant="outline">Outline</Button>)
      const button = screen.getByRole('button')
      expect(button).toHaveClass('border')
      expect(button).toHaveClass('border-border')
      expect(button).toHaveClass('bg-background')
    })

    it('should apply secondary variant classes', () => {
      render(<Button variant="secondary">Secondary</Button>)
      const button = screen.getByRole('button')
      expect(button).toHaveClass('bg-secondary')
      expect(button).toHaveClass('text-secondary-foreground')
    })

    it('should apply ghost variant classes', () => {
      render(<Button variant="ghost">Ghost</Button>)
      const button = screen.getByRole('button')
      expect(button).toHaveClass('bg-transparent')
      expect(button).toHaveClass('text-muted-foreground')
    })

    it('should apply link variant classes', () => {
      render(<Button variant="link">Link</Button>)
      const button = screen.getByRole('button')
      expect(button).toHaveClass('text-primary')
      expect(button).toHaveClass('underline-offset-4')
    })

    it('should default to default variant when not specified', () => {
      render(<Button>Default Variant</Button>)
      const button = screen.getByRole('button')
      expect(button).toHaveClass('bg-primary')
    })
  })

  // ========================================
  // Size Tests
  // ========================================
  describe('Sizes', () => {
    it('should apply default size classes', () => {
      render(<Button size="default">Default Size</Button>)
      const button = screen.getByRole('button')
      expect(button).toHaveClass('h-11')
      expect(button).toHaveClass('px-5')
    })

    it('should apply sm size classes', () => {
      render(<Button size="sm">Small</Button>)
      const button = screen.getByRole('button')
      expect(button).toHaveClass('h-9')
      expect(button).toHaveClass('px-3')
      expect(button).toHaveClass('text-xs')
    })

    it('should apply lg size classes', () => {
      render(<Button size="lg">Large</Button>)
      const button = screen.getByRole('button')
      expect(button).toHaveClass('h-12')
      expect(button).toHaveClass('px-6')
      expect(button).toHaveClass('text-base')
    })

    it('should apply xl size classes', () => {
      render(<Button size="xl">Extra Large</Button>)
      const button = screen.getByRole('button')
      expect(button).toHaveClass('h-14')
      expect(button).toHaveClass('px-8')
      expect(button).toHaveClass('text-lg')
    })

    it('should apply icon size classes', () => {
      render(<Button size="icon">🔍</Button>)
      const button = screen.getByRole('button')
      expect(button).toHaveClass('h-11')
      expect(button).toHaveClass('w-11')
    })

    it('should default to default size when not specified', () => {
      render(<Button>Default Size</Button>)
      const button = screen.getByRole('button')
      expect(button).toHaveClass('h-11')
    })
  })

  // ========================================
  // Loading State Tests
  // ========================================
  describe('Loading State', () => {
    it('should show loading spinner when loading is true', () => {
      const { container } = render(<Button loading>Loading Button</Button>)
      const spinner = container.querySelector('.animate-spin')
      expect(spinner).toBeInTheDocument()
    })

    it('should show default loading text when loading without loadingText', () => {
      render(<Button loading>Click me</Button>)
      expect(screen.getByText('Loading...')).toBeInTheDocument()
      expect(screen.queryByText('Click me')).not.toBeInTheDocument()
    })

    it('should show custom loadingText when provided', () => {
      render(
        <Button loading loadingText="Processing...">
          Click me
        </Button>
      )
      expect(screen.getByText('Processing...')).toBeInTheDocument()
      expect(screen.queryByText('Click me')).not.toBeInTheDocument()
    })

    it('should disable button when loading', () => {
      render(<Button loading>Click me</Button>)
      const button = screen.getByRole('button')
      expect(button).toBeDisabled()
    })

    it('should set aria-busy to true when loading', () => {
      render(<Button loading>Click me</Button>)
      const button = screen.getByRole('button')
      expect(button).toHaveAttribute('aria-busy', 'true')
    })

    it('should set aria-busy to false when not loading', () => {
      render(<Button loading={false}>Click me</Button>)
      const button = screen.getByRole('button')
      expect(button).toHaveAttribute('aria-busy', 'false')
    })

    it('should hide spinner aria from screen readers', () => {
      const { container } = render(<Button loading>Loading</Button>)
      const spinner = container.querySelector('.animate-spin')
      expect(spinner).toHaveAttribute('aria-hidden', 'true')
    })

    it('should disable hover effects when loading', () => {
      render(<Button loading>Loading</Button>)
      const button = screen.getByRole('button')
      expect(button).toHaveClass('hover:scale-100')
      expect(button).toHaveClass('hover:shadow-sm')
    })

    it('should hide children when loading', () => {
      render(<Button loading>Original Text</Button>)
      expect(screen.queryByText('Original Text')).not.toBeInTheDocument()
    })
  })

  // ========================================
  // Icon Tests
  // ========================================
  describe('Icons', () => {
    it('should render startIcon before children', () => {
      const { container } = render(
        <Button startIcon={<span data-testid="start-icon">🔍</span>}>
          Search
        </Button>
      )
      const button = container.querySelector('button')
      const startIcon = screen.getByTestId('start-icon')
      const text = screen.getByText('Search')

      expect(startIcon).toBeInTheDocument()
      expect(button?.textContent).toBe('🔍Search')
    })

    it('should render endIcon after children', () => {
      const { container } = render(
        <Button endIcon={<span data-testid="end-icon">→</span>}>
          Next
        </Button>
      )
      const button = container.querySelector('button')
      const endIcon = screen.getByTestId('end-icon')
      const text = screen.getByText('Next')

      expect(endIcon).toBeInTheDocument()
      expect(button?.textContent).toBe('Next→')
    })

    it('should render both startIcon and endIcon', () => {
      const { container } = render(
        <Button
          startIcon={<span data-testid="start-icon">←</span>}
          endIcon={<span data-testid="end-icon">→</span>}
        >
          Middle
        </Button>
      )
      const button = container.querySelector('button')

      expect(screen.getByTestId('start-icon')).toBeInTheDocument()
      expect(screen.getByTestId('end-icon')).toBeInTheDocument()
      expect(button?.textContent).toBe('←Middle→')
    })

    it('should hide icons when loading', () => {
      render(
        <Button
          loading
          startIcon={<span data-testid="start-icon">🔍</span>}
          endIcon={<span data-testid="end-icon">→</span>}
        >
          Search
        </Button>
      )

      expect(screen.queryByTestId('start-icon')).not.toBeInTheDocument()
      expect(screen.queryByTestId('end-icon')).not.toBeInTheDocument()
      expect(screen.queryByText('Search')).not.toBeInTheDocument()
    })

    it('should apply margin classes to startIcon wrapper', () => {
      const { container } = render(
        <Button startIcon={<span>Icon</span>}>Text</Button>
      )
      const iconWrapper = container.querySelector('.mr-2')
      expect(iconWrapper).toBeInTheDocument()
    })

    it('should apply margin classes to endIcon wrapper', () => {
      const { container } = render(
        <Button endIcon={<span>Icon</span>}>Text</Button>
      )
      const iconWrapper = container.querySelector('.ml-2')
      expect(iconWrapper).toBeInTheDocument()
    })
  })

  // ========================================
  // Disabled State Tests
  // ========================================
  describe('Disabled State', () => {
    it('should disable button when disabled prop is true', () => {
      render(<Button disabled>Disabled Button</Button>)
      const button = screen.getByRole('button')
      expect(button).toBeDisabled()
    })

    it('should apply disabled classes', () => {
      render(<Button disabled>Disabled</Button>)
      const button = screen.getByRole('button')
      expect(button).toHaveClass('disabled:pointer-events-none')
      expect(button).toHaveClass('disabled:opacity-50')
    })

    it('should not trigger onClick when disabled', () => {
      const handleClick = jest.fn()
      render(
        <Button disabled onClick={handleClick}>
          Disabled
        </Button>
      )
      const button = screen.getByRole('button')
      fireEvent.click(button)
      expect(handleClick).not.toHaveBeenCalled()
    })

    it('should disable button when both loading and disabled', () => {
      render(<Button loading disabled>Button</Button>)
      const button = screen.getByRole('button')
      expect(button).toBeDisabled()
    })
  })

  // ========================================
  // Interaction Tests
  // ========================================
  describe('Interactions', () => {
    it('should call onClick handler when clicked', () => {
      const handleClick = jest.fn()
      render(<Button onClick={handleClick}>Click me</Button>)
      const button = screen.getByRole('button')
      fireEvent.click(button)
      expect(handleClick).toHaveBeenCalledTimes(1)
    })

    it('should not call onClick when loading', () => {
      const handleClick = jest.fn()
      render(
        <Button loading onClick={handleClick}>
          Loading
        </Button>
      )
      const button = screen.getByRole('button')
      fireEvent.click(button)
      expect(handleClick).not.toHaveBeenCalled()
    })

    it('should support keyboard interaction', () => {
      const handleClick = jest.fn()
      render(<Button onClick={handleClick}>Press me</Button>)
      const button = screen.getByRole('button')

      button.focus()
      expect(button).toHaveFocus()

      fireEvent.keyDown(button, { key: 'Enter' })
      // Note: fireEvent.keyDown doesn't trigger click, but the button should be focusable
    })

    it('should support onMouseEnter handler', () => {
      const handleMouseEnter = jest.fn()
      render(<Button onMouseEnter={handleMouseEnter}>Hover me</Button>)
      const button = screen.getByRole('button')
      fireEvent.mouseEnter(button)
      expect(handleMouseEnter).toHaveBeenCalledTimes(1)
    })

    it('should support onMouseLeave handler', () => {
      const handleMouseLeave = jest.fn()
      render(<Button onMouseLeave={handleMouseLeave}>Hover me</Button>)
      const button = screen.getByRole('button')
      fireEvent.mouseLeave(button)
      expect(handleMouseLeave).toHaveBeenCalledTimes(1)
    })
  })

  // ========================================
  // Accessibility Tests
  // ========================================
  describe('Accessibility', () => {
    it('should have button role', () => {
      render(<Button>Accessible Button</Button>)
      expect(screen.getByRole('button')).toBeInTheDocument()
    })

    it('should support aria-label', () => {
      render(<Button aria-label="Custom label">Icon only</Button>)
      const button = screen.getByLabelText('Custom label')
      expect(button).toBeInTheDocument()
    })

    it('should support aria-describedby', () => {
      render(<Button aria-describedby="description">Button</Button>)
      const button = screen.getByRole('button')
      expect(button).toHaveAttribute('aria-describedby', 'description')
    })

    it('should indicate loading state with aria-busy', () => {
      render(<Button loading>Loading</Button>)
      const button = screen.getByRole('button')
      expect(button).toHaveAttribute('aria-busy', 'true')
    })

    it('should have focus-visible ring', () => {
      render(<Button>Focus me</Button>)
      const button = screen.getByRole('button')
      expect(button).toHaveClass('focus-visible:ring-2')
    })
  })

  // ========================================
  // Edge Cases Tests
  // ========================================
  describe('Edge Cases', () => {
    it('should handle empty children', () => {
      render(<Button />)
      const button = screen.getByRole('button')
      expect(button).toBeInTheDocument()
      expect(button.textContent).toBe('')
    })

    it('should handle complex nested children', () => {
      render(
        <Button>
          <span>Text 1</span>
          <span>Text 2</span>
        </Button>
      )
      expect(screen.getByText('Text 1')).toBeInTheDocument()
      expect(screen.getByText('Text 2')).toBeInTheDocument()
    })

    it('should handle numeric children', () => {
      render(<Button>{42}</Button>)
      expect(screen.getByText('42')).toBeInTheDocument()
    })

    it('should handle zero as children', () => {
      render(<Button>{0}</Button>)
      expect(screen.getByText('0')).toBeInTheDocument()
    })

    it('should handle loadingText with empty string', () => {
      render(<Button loading loadingText="">Loading</Button>)
      const button = screen.getByRole('button')
      // Empty string falls back to "Loading..." (loadingText || "Loading...")
      expect(button.textContent).toBe('Loading...')
    })

    it('should override hover classes when loading', () => {
      render(<Button loading variant="default">Loading</Button>)
      const button = screen.getByRole('button')
      // Loading adds override classes (both original and override classes are present)
      expect(button).toHaveClass('hover:scale-100')
      expect(button).toHaveClass('hover:shadow-sm')
      // Original variant classes are also present
      expect(button).toHaveClass('hover:scale-[1.02]')
      expect(button).toHaveClass('hover:shadow-md')
    })
  })

  // ========================================
  // Pill Canon Tests — design canon: buttons must be pills (rounded-full)
  // ========================================
  describe('Pill Canon', () => {
    it('default variant has rounded-full class', () => {
      render(<Button>Default</Button>)
      const button = screen.getByRole('button')
      expect(button.className).toContain('rounded-full')
    })

    it('secondary variant has rounded-full class', () => {
      render(<Button variant="secondary">Secondary</Button>)
      const button = screen.getByRole('button')
      expect(button.className).toContain('rounded-full')
    })

    it('icon size has rounded-full class', () => {
      render(<Button size="icon" aria-label="icon button">X</Button>)
      const button = screen.getByRole('button')
      expect(button.className).toContain('rounded-full')
    })

    it('does NOT have rounded-xl class', () => {
      render(<Button>No sharp corners</Button>)
      const button = screen.getByRole('button')
      expect(button.className).not.toContain('rounded-xl')
    })
  })

  // ========================================
  // Integration Tests
  // ========================================
  describe('Integration', () => {
    it('should work with all props combined', () => {
      const handleClick = jest.fn()
      render(
        <Button
          variant="destructive"
          size="lg"
          onClick={handleClick}
          startIcon={<span data-testid="start">🗑️</span>}
          className="custom-class"
          type="submit"
          aria-label="Delete item"
        >
          Delete
        </Button>
      )

      const button = screen.getByRole('button', { name: 'Delete item' })
      expect(button).toHaveClass('bg-destructive')
      expect(button).toHaveClass('h-12')
      expect(button).toHaveClass('custom-class')
      expect(button).toHaveAttribute('type', 'submit')
      expect(screen.getByTestId('start')).toBeInTheDocument()

      fireEvent.click(button)
      expect(handleClick).toHaveBeenCalledTimes(1)
    })

    it('should transition from loading to normal state', () => {
      const { rerender } = render(<Button loading>Submit</Button>)

      expect(screen.getByText('Loading...')).toBeInTheDocument()
      expect(screen.queryByText('Submit')).not.toBeInTheDocument()

      rerender(<Button loading={false}>Submit</Button>)

      expect(screen.queryByText('Loading...')).not.toBeInTheDocument()
      expect(screen.getByText('Submit')).toBeInTheDocument()
    })

    it('should work in form submission context', () => {
      const handleSubmit = jest.fn((e) => e.preventDefault())
      render(
        <form onSubmit={handleSubmit}>
          <Button type="submit">Submit Form</Button>
        </form>
      )

      const button = screen.getByRole('button', { name: 'Submit Form' })
      fireEvent.click(button)
      expect(handleSubmit).toHaveBeenCalledTimes(1)
    })

    it('should maintain ref across re-renders', () => {
      const ref = React.createRef<HTMLButtonElement>()
      const { rerender } = render(<Button ref={ref}>First</Button>)

      const firstElement = ref.current
      expect(firstElement).toBeInstanceOf(HTMLButtonElement)

      rerender(<Button ref={ref}>Second</Button>)

      expect(ref.current).toBe(firstElement)
    })
  })
})
