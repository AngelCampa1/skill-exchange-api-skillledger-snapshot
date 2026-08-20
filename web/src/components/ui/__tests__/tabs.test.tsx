import React from 'react'
import { render, screen, fireEvent } from '@testing-library/react'
import '@testing-library/jest-dom'
import { Tabs, TabsList, TabsTrigger, TabsContent } from '../tabs'

describe('Tabs', () => {
  // ========================================
  // Basic Rendering Tests
  // ========================================
  describe('Basic Rendering', () => {
    it('should render tabs container', () => {
      const { container } = render(
        <Tabs>
          <div>Test content</div>
        </Tabs>
      )
      expect(container.firstChild).toBeInTheDocument()
    })

    it('should apply custom className to tabs', () => {
      const { container } = render(
        <Tabs className="custom-tabs">
          <div>Test</div>
        </Tabs>
      )
      const tabs = container.firstChild as HTMLElement
      expect(tabs).toHaveClass('custom-tabs')
      expect(tabs).toHaveClass('w-full')
    })

    it('should pass through additional props to tabs', () => {
      const { container } = render(
        <Tabs data-testid="test-tabs" aria-label="Test Tabs">
          <div>Test</div>
        </Tabs>
      )
      const tabs = container.firstChild as HTMLElement
      expect(tabs).toHaveAttribute('data-testid', 'test-tabs')
      expect(tabs).toHaveAttribute('aria-label', 'Test Tabs')
    })

    it('should default to horizontal orientation', () => {
      const { container } = render(
        <Tabs>
          <div>Test</div>
        </Tabs>
      )
      const tabs = container.firstChild as HTMLElement
      expect(tabs).toHaveAttribute('data-orientation', 'horizontal')
    })

    it('should support vertical orientation', () => {
      const { container } = render(
        <Tabs orientation="vertical">
          <div>Test</div>
        </Tabs>
      )
      const tabs = container.firstChild as HTMLElement
      expect(tabs).toHaveAttribute('data-orientation', 'vertical')
    })
  })

  // ========================================
  // TabsList Tests
  // ========================================
  describe('TabsList', () => {
    it('should render with role="tablist"', () => {
      render(
        <TabsList>
          <div>Tabs</div>
        </TabsList>
      )
      const tablist = screen.getByRole('tablist')
      expect(tablist).toBeInTheDocument()
    })

    it('should apply default classes', () => {
      render(
        <TabsList>
          <div>Tabs</div>
        </TabsList>
      )
      const tablist = screen.getByRole('tablist')
      expect(tablist).toHaveClass('inline-flex')
      expect(tablist).toHaveClass('h-12')
      expect(tablist).toHaveClass('rounded-xl')
    })

    it('should apply custom className', () => {
      render(
        <TabsList className="custom-list">
          <div>Tabs</div>
        </TabsList>
      )
      const tablist = screen.getByRole('tablist')
      expect(tablist).toHaveClass('custom-list')
    })

    it('should forward ref to tablist', () => {
      const ref = React.createRef<HTMLDivElement>()
      render(
        <TabsList ref={ref}>
          <div>Tabs</div>
        </TabsList>
      )
      expect(ref.current).toBeInstanceOf(HTMLDivElement)
      expect(ref.current).toHaveAttribute('role', 'tablist')
    })

    it('should pass through additional props', () => {
      render(
        <TabsList data-testid="custom-tablist">
          <div>Tabs</div>
        </TabsList>
      )
      expect(screen.getByTestId('custom-tablist')).toBeInTheDocument()
    })
  })

  // ========================================
  // TabsTrigger Tests
  // ========================================
  describe('TabsTrigger', () => {
    it('should render with role="tab"', () => {
      render(
        <Tabs>
          <TabsList>
            <TabsTrigger value="tab1">Tab 1</TabsTrigger>
          </TabsList>
        </Tabs>
      )
      const tab = screen.getByRole('tab')
      expect(tab).toBeInTheDocument()
    })

    it('should render as button element', () => {
      render(
        <Tabs>
          <TabsList>
            <TabsTrigger value="tab1">Tab 1</TabsTrigger>
          </TabsList>
        </Tabs>
      )
      const tab = screen.getByText('Tab 1')
      expect(tab.tagName).toBe('BUTTON')
      expect(tab).toHaveAttribute('type', 'button')
    })

    it('should have aria-selected="false" when not selected', () => {
      render(
        <Tabs defaultValue="tab2">
          <TabsList>
            <TabsTrigger value="tab1">Tab 1</TabsTrigger>
          </TabsList>
        </Tabs>
      )
      const tab = screen.getByText('Tab 1')
      expect(tab).toHaveAttribute('aria-selected', 'false')
    })

    it('should have aria-selected="true" when selected', () => {
      render(
        <Tabs defaultValue="tab1">
          <TabsList>
            <TabsTrigger value="tab1">Tab 1</TabsTrigger>
          </TabsList>
        </Tabs>
      )
      const tab = screen.getByText('Tab 1')
      expect(tab).toHaveAttribute('aria-selected', 'true')
    })

    it('should have data-state="inactive" when not selected', () => {
      render(
        <Tabs defaultValue="tab2">
          <TabsList>
            <TabsTrigger value="tab1">Tab 1</TabsTrigger>
          </TabsList>
        </Tabs>
      )
      const tab = screen.getByText('Tab 1')
      expect(tab).toHaveAttribute('data-state', 'inactive')
    })

    it('should have data-state="active" when selected', () => {
      render(
        <Tabs defaultValue="tab1">
          <TabsList>
            <TabsTrigger value="tab1">Tab 1</TabsTrigger>
          </TabsList>
        </Tabs>
      )
      const tab = screen.getByText('Tab 1')
      expect(tab).toHaveAttribute('data-state', 'active')
    })

    it('should apply active styles when selected', () => {
      render(
        <Tabs defaultValue="tab1">
          <TabsList>
            <TabsTrigger value="tab1">Tab 1</TabsTrigger>
          </TabsList>
        </Tabs>
      )
      const tab = screen.getByText('Tab 1')
      expect(tab).toHaveClass('bg-background')
      expect(tab).toHaveClass('text-foreground')
    })

    it('should apply inactive styles when not selected', () => {
      render(
        <Tabs defaultValue="tab2">
          <TabsList>
            <TabsTrigger value="tab1">Tab 1</TabsTrigger>
          </TabsList>
        </Tabs>
      )
      const tab = screen.getByText('Tab 1')
      expect(tab).toHaveClass('text-muted-foreground')
    })

    it('should apply custom className', () => {
      render(
        <Tabs>
          <TabsList>
            <TabsTrigger value="tab1" className="custom-trigger">
              Tab 1
            </TabsTrigger>
          </TabsList>
        </Tabs>
      )
      const tab = screen.getByText('Tab 1')
      expect(tab).toHaveClass('custom-trigger')
    })

    it('should forward ref to trigger button', () => {
      const ref = React.createRef<HTMLButtonElement>()
      render(
        <Tabs>
          <TabsList>
            <TabsTrigger value="tab1" ref={ref}>
              Tab 1
            </TabsTrigger>
          </TabsList>
        </Tabs>
      )
      expect(ref.current).toBeInstanceOf(HTMLButtonElement)
    })

    it('should pass through additional props', () => {
      render(
        <Tabs>
          <TabsList>
            <TabsTrigger value="tab1" data-testid="custom-trigger" disabled>
              Tab 1
            </TabsTrigger>
          </TabsList>
        </Tabs>
      )
      const tab = screen.getByTestId('custom-trigger')
      expect(tab).toBeDisabled()
    })
  })

  // ========================================
  // TabsContent Tests
  // ========================================
  describe('TabsContent', () => {
    it('should render with role="tabpanel"', () => {
      render(
        <Tabs defaultValue="tab1">
          <TabsContent value="tab1">Content 1</TabsContent>
        </Tabs>
      )
      const panel = screen.getByRole('tabpanel')
      expect(panel).toBeInTheDocument()
    })

    it('should show content when tab is selected', () => {
      render(
        <Tabs defaultValue="tab1">
          <TabsContent value="tab1">Content 1</TabsContent>
        </Tabs>
      )
      expect(screen.getByText('Content 1')).toBeVisible()
    })

    it('should hide content when tab is not selected', () => {
      render(
        <Tabs defaultValue="tab2">
          <TabsContent value="tab1">Content 1</TabsContent>
        </Tabs>
      )
      expect(screen.queryByText('Content 1')).not.toBeInTheDocument()
    })

    it('should have aria-hidden="false" when selected', () => {
      render(
        <Tabs defaultValue="tab1">
          <TabsContent value="tab1">Content 1</TabsContent>
        </Tabs>
      )
      const panel = screen.getByRole('tabpanel')
      expect(panel).toHaveAttribute('aria-hidden', 'false')
    })

    it('should have data-state="active" when selected', () => {
      render(
        <Tabs defaultValue="tab1">
          <TabsContent value="tab1">Content 1</TabsContent>
        </Tabs>
      )
      const panel = screen.getByRole('tabpanel')
      expect(panel).toHaveAttribute('data-state', 'active')
    })

    it('should apply custom className', () => {
      render(
        <Tabs defaultValue="tab1">
          <TabsContent value="tab1" className="custom-content">
            Content 1
          </TabsContent>
        </Tabs>
      )
      const panel = screen.getByRole('tabpanel')
      expect(panel).toHaveClass('custom-content')
    })

    it('should forward ref to content div', () => {
      const ref = React.createRef<HTMLDivElement>()
      render(
        <Tabs defaultValue="tab1">
          <TabsContent value="tab1" ref={ref}>
            Content 1
          </TabsContent>
        </Tabs>
      )
      expect(ref.current).toBeInstanceOf(HTMLDivElement)
      expect(ref.current).toHaveAttribute('role', 'tabpanel')
    })

    it('should pass through additional props', () => {
      render(
        <Tabs defaultValue="tab1">
          <TabsContent value="tab1" data-testid="custom-content">
            Content 1
          </TabsContent>
        </Tabs>
      )
      expect(screen.getByTestId('custom-content')).toBeInTheDocument()
    })
  })

  // ========================================
  // Tab Switching Tests
  // ========================================
  describe('Tab Switching', () => {
    it('should switch tabs on click', () => {
      render(
        <Tabs defaultValue="tab1">
          <TabsList>
            <TabsTrigger value="tab1">Tab 1</TabsTrigger>
            <TabsTrigger value="tab2">Tab 2</TabsTrigger>
          </TabsList>
          <TabsContent value="tab1">Content 1</TabsContent>
          <TabsContent value="tab2">Content 2</TabsContent>
        </Tabs>
      )

      expect(screen.getByText('Content 1')).toBeVisible()
      expect(screen.queryByText('Content 2')).not.toBeInTheDocument()

      fireEvent.click(screen.getByText('Tab 2'))

      expect(screen.queryByText('Content 1')).not.toBeInTheDocument()
      expect(screen.getByText('Content 2')).toBeVisible()
    })

    it('should update aria-selected on tab switch', () => {
      render(
        <Tabs defaultValue="tab1">
          <TabsList>
            <TabsTrigger value="tab1">Tab 1</TabsTrigger>
            <TabsTrigger value="tab2">Tab 2</TabsTrigger>
          </TabsList>
        </Tabs>
      )

      const tab1 = screen.getByText('Tab 1')
      const tab2 = screen.getByText('Tab 2')

      expect(tab1).toHaveAttribute('aria-selected', 'true')
      expect(tab2).toHaveAttribute('aria-selected', 'false')

      fireEvent.click(tab2)

      expect(tab1).toHaveAttribute('aria-selected', 'false')
      expect(tab2).toHaveAttribute('aria-selected', 'true')
    })

    it('should update data-state on tab switch', () => {
      render(
        <Tabs defaultValue="tab1">
          <TabsList>
            <TabsTrigger value="tab1">Tab 1</TabsTrigger>
            <TabsTrigger value="tab2">Tab 2</TabsTrigger>
          </TabsList>
        </Tabs>
      )

      const tab1 = screen.getByText('Tab 1')
      const tab2 = screen.getByText('Tab 2')

      expect(tab1).toHaveAttribute('data-state', 'active')
      expect(tab2).toHaveAttribute('data-state', 'inactive')

      fireEvent.click(tab2)

      expect(tab1).toHaveAttribute('data-state', 'inactive')
      expect(tab2).toHaveAttribute('data-state', 'active')
    })

    it('should call onValueChange when tab is clicked', () => {
      const handleValueChange = jest.fn()
      render(
        <Tabs defaultValue="tab1" onValueChange={handleValueChange}>
          <TabsList>
            <TabsTrigger value="tab1">Tab 1</TabsTrigger>
            <TabsTrigger value="tab2">Tab 2</TabsTrigger>
          </TabsList>
        </Tabs>
      )

      fireEvent.click(screen.getByText('Tab 2'))

      expect(handleValueChange).toHaveBeenCalledWith('tab2')
    })

    it('should handle clicking already selected tab', () => {
      const handleValueChange = jest.fn()
      render(
        <Tabs defaultValue="tab1" onValueChange={handleValueChange}>
          <TabsList>
            <TabsTrigger value="tab1">Tab 1</TabsTrigger>
          </TabsList>
          <TabsContent value="tab1">Content 1</TabsContent>
        </Tabs>
      )

      fireEvent.click(screen.getByText('Tab 1'))

      expect(handleValueChange).toHaveBeenCalledWith('tab1')
      expect(screen.getByText('Content 1')).toBeVisible()
    })
  })

  // ========================================
  // Controlled vs Uncontrolled Tests
  // ========================================
  describe('Controlled vs Uncontrolled', () => {
    it('should work as uncontrolled component with defaultValue', () => {
      render(
        <Tabs defaultValue="tab1">
          <TabsList>
            <TabsTrigger value="tab1">Tab 1</TabsTrigger>
            <TabsTrigger value="tab2">Tab 2</TabsTrigger>
          </TabsList>
          <TabsContent value="tab1">Content 1</TabsContent>
          <TabsContent value="tab2">Content 2</TabsContent>
        </Tabs>
      )

      expect(screen.getByText('Content 1')).toBeVisible()

      fireEvent.click(screen.getByText('Tab 2'))

      expect(screen.getByText('Content 2')).toBeVisible()
    })

    it('should work as controlled component with value prop', () => {
      const TestComponent = () => {
        const [value, setValue] = React.useState('tab1')
        return (
          <Tabs value={value} onValueChange={setValue}>
            <TabsList>
              <TabsTrigger value="tab1">Tab 1</TabsTrigger>
              <TabsTrigger value="tab2">Tab 2</TabsTrigger>
            </TabsList>
            <TabsContent value="tab1">Content 1</TabsContent>
            <TabsContent value="tab2">Content 2</TabsContent>
          </Tabs>
        )
      }

      render(<TestComponent />)

      expect(screen.getByText('Content 1')).toBeVisible()

      fireEvent.click(screen.getByText('Tab 2'))

      expect(screen.getByText('Content 2')).toBeVisible()
    })

    it('should not update internal state when controlled', () => {
      const handleValueChange = jest.fn()
      render(
        <Tabs value="tab1" onValueChange={handleValueChange}>
          <TabsList>
            <TabsTrigger value="tab1">Tab 1</TabsTrigger>
            <TabsTrigger value="tab2">Tab 2</TabsTrigger>
          </TabsList>
          <TabsContent value="tab1">Content 1</TabsContent>
          <TabsContent value="tab2">Content 2</TabsContent>
        </Tabs>
      )

      fireEvent.click(screen.getByText('Tab 2'))

      expect(handleValueChange).toHaveBeenCalledWith('tab2')
      // Should still show Content 1 since value prop hasn't changed
      expect(screen.getByText('Content 1')).toBeVisible()
      expect(screen.queryByText('Content 2')).not.toBeInTheDocument()
    })

    it('should default to empty string when no defaultValue', () => {
      render(
        <Tabs>
          <TabsList>
            <TabsTrigger value="tab1">Tab 1</TabsTrigger>
          </TabsList>
          <TabsContent value="tab1">Content 1</TabsContent>
        </Tabs>
      )

      const tab1 = screen.getByText('Tab 1')
      expect(tab1).toHaveAttribute('aria-selected', 'false')
      expect(screen.queryByText('Content 1')).not.toBeInTheDocument()
    })

    it('should handle controlled component with empty value', () => {
      render(
        <Tabs value="">
          <TabsList>
            <TabsTrigger value="tab1">Tab 1</TabsTrigger>
          </TabsList>
          <TabsContent value="tab1">Content 1</TabsContent>
        </Tabs>
      )

      expect(screen.getByText('Tab 1')).toHaveAttribute('aria-selected', 'false')
      expect(screen.queryByText('Content 1')).not.toBeInTheDocument()
    })
  })

  // ========================================
  // Accessibility Tests
  // ========================================
  describe('Accessibility', () => {
    it('should have proper ARIA roles', () => {
      render(
        <Tabs defaultValue="tab1">
          <TabsList>
            <TabsTrigger value="tab1">Tab 1</TabsTrigger>
          </TabsList>
          <TabsContent value="tab1">Content 1</TabsContent>
        </Tabs>
      )

      expect(screen.getByRole('tablist')).toBeInTheDocument()
      expect(screen.getByRole('tab')).toBeInTheDocument()
      expect(screen.getByRole('tabpanel')).toBeInTheDocument()
    })

    it('should have correct aria-selected states', () => {
      render(
        <Tabs defaultValue="tab1">
          <TabsList>
            <TabsTrigger value="tab1">Tab 1</TabsTrigger>
            <TabsTrigger value="tab2">Tab 2</TabsTrigger>
          </TabsList>
        </Tabs>
      )

      expect(screen.getByText('Tab 1')).toHaveAttribute('aria-selected', 'true')
      expect(screen.getByText('Tab 2')).toHaveAttribute('aria-selected', 'false')
    })

    it('should have correct aria-hidden states', () => {
      render(
        <Tabs defaultValue="tab1">
          <TabsContent value="tab1">Content 1</TabsContent>
        </Tabs>
      )

      expect(screen.getByRole('tabpanel')).toHaveAttribute('aria-hidden', 'false')
    })

    it('should support keyboard focus', () => {
      render(
        <Tabs defaultValue="tab1">
          <TabsList>
            <TabsTrigger value="tab1">Tab 1</TabsTrigger>
          </TabsList>
        </Tabs>
      )

      const tab = screen.getByText('Tab 1')
      tab.focus()
      expect(tab).toHaveFocus()
    })
  })

  // ========================================
  // Edge Cases Tests
  // ========================================
  describe('Edge Cases', () => {
    it('should handle tabs with no content', () => {
      render(
        <Tabs defaultValue="tab1">
          <TabsList>
            <TabsTrigger value="tab1">Tab 1</TabsTrigger>
          </TabsList>
        </Tabs>
      )

      expect(screen.getByText('Tab 1')).toHaveAttribute('aria-selected', 'true')
    })

    it('should handle content with no matching tab', () => {
      render(
        <Tabs defaultValue="tab1">
          <TabsContent value="tab2">Content 2</TabsContent>
        </Tabs>
      )

      expect(screen.queryByText('Content 2')).not.toBeInTheDocument()
    })

    it('should handle multiple content panels', () => {
      render(
        <Tabs defaultValue="tab2">
          <TabsContent value="tab1">Content 1</TabsContent>
          <TabsContent value="tab2">Content 2</TabsContent>
          <TabsContent value="tab3">Content 3</TabsContent>
        </Tabs>
      )

      expect(screen.queryByText('Content 1')).not.toBeInTheDocument()
      expect(screen.getByText('Content 2')).toBeVisible()
      expect(screen.queryByText('Content 3')).not.toBeInTheDocument()
    })

    it('should handle rapid tab switching', () => {
      render(
        <Tabs defaultValue="tab1">
          <TabsList>
            <TabsTrigger value="tab1">Tab 1</TabsTrigger>
            <TabsTrigger value="tab2">Tab 2</TabsTrigger>
            <TabsTrigger value="tab3">Tab 3</TabsTrigger>
          </TabsList>
          <TabsContent value="tab1">Content 1</TabsContent>
          <TabsContent value="tab2">Content 2</TabsContent>
          <TabsContent value="tab3">Content 3</TabsContent>
        </Tabs>
      )

      fireEvent.click(screen.getByText('Tab 2'))
      fireEvent.click(screen.getByText('Tab 3'))
      fireEvent.click(screen.getByText('Tab 1'))

      expect(screen.getByText('Content 1')).toBeVisible()
      expect(screen.queryByText('Content 2')).not.toBeInTheDocument()
      expect(screen.queryByText('Content 3')).not.toBeInTheDocument()
    })

    it('should handle empty children', () => {
      const { container } = render(<Tabs />)
      expect(container.firstChild).toBeInTheDocument()
    })
  })

  // ========================================
  // Integration Tests
  // ========================================
  describe('Integration', () => {
    it('should work with all components together', () => {
      render(
        <Tabs defaultValue="tab1">
          <TabsList>
            <TabsTrigger value="tab1">Tab 1</TabsTrigger>
            <TabsTrigger value="tab2">Tab 2</TabsTrigger>
            <TabsTrigger value="tab3">Tab 3</TabsTrigger>
          </TabsList>
          <TabsContent value="tab1">Content 1</TabsContent>
          <TabsContent value="tab2">Content 2</TabsContent>
          <TabsContent value="tab3">Content 3</TabsContent>
        </Tabs>
      )

      expect(screen.getByText('Content 1')).toBeVisible()

      fireEvent.click(screen.getByText('Tab 2'))
      expect(screen.getByText('Content 2')).toBeVisible()

      fireEvent.click(screen.getByText('Tab 3'))
      expect(screen.getByText('Content 3')).toBeVisible()
    })

    it('should work with complex nested content', () => {
      render(
        <Tabs defaultValue="tab1">
          <TabsList>
            <TabsTrigger value="tab1">Tab 1</TabsTrigger>
          </TabsList>
          <TabsContent value="tab1">
            <div>
              <h2>Nested Header</h2>
              <p>Paragraph text</p>
              <button>Nested Button</button>
            </div>
          </TabsContent>
        </Tabs>
      )

      expect(screen.getByText('Nested Header')).toBeVisible()
      expect(screen.getByText('Paragraph text')).toBeVisible()
      expect(screen.getByText('Nested Button')).toBeVisible()
    })

    it('should work with all props combined', () => {
      const handleValueChange = jest.fn()
      render(
        <Tabs
          defaultValue="tab1"
          onValueChange={handleValueChange}
          orientation="vertical"
          className="custom-tabs"
          data-testid="full-tabs"
        >
          <TabsList className="custom-list">
            <TabsTrigger value="tab1" className="custom-trigger">
              Tab 1
            </TabsTrigger>
            <TabsTrigger value="tab2">Tab 2</TabsTrigger>
          </TabsList>
          <TabsContent value="tab1" className="custom-content">
            Content 1
          </TabsContent>
          <TabsContent value="tab2">Content 2</TabsContent>
        </Tabs>
      )

      expect(screen.getByTestId('full-tabs')).toHaveAttribute('data-orientation', 'vertical')
      expect(screen.getByRole('tablist')).toHaveClass('custom-list')
      expect(screen.getByText('Tab 1')).toHaveClass('custom-trigger')
      expect(screen.getByRole('tabpanel')).toHaveClass('custom-content')

      fireEvent.click(screen.getByText('Tab 2'))
      expect(handleValueChange).toHaveBeenCalledWith('tab2')
    })
  })
})
