import React from 'react'
import { render, screen, fireEvent } from '@testing-library/react'
import '@testing-library/jest-dom'
import {
  Accordion,
  AccordionItem,
  AccordionTrigger,
  AccordionContent,
} from '../accordion'

describe('Accordion', () => {
  // ========================================
  // Basic Rendering Tests
  // ========================================
  describe('Basic Rendering', () => {
    it('should render accordion container', () => {
      render(
        <Accordion>
          <div>Test content</div>
        </Accordion>
      )
      expect(screen.getByText('Test content')).toBeInTheDocument()
    })

    it('should apply custom className to accordion', () => {
      const { container } = render(
        <Accordion className="custom-accordion">
          <div>Test</div>
        </Accordion>
      )
      const accordion = container.firstChild as HTMLElement
      expect(accordion).toHaveClass('custom-accordion')
      expect(accordion).toHaveClass('divide-y')
      expect(accordion).toHaveClass('divide-border')
    })

    it('should pass through additional props to accordion', () => {
      const { container } = render(
        <Accordion data-testid="test-accordion" aria-label="Test Accordion">
          <div>Test</div>
        </Accordion>
      )
      const accordion = container.firstChild as HTMLElement
      expect(accordion).toHaveAttribute('data-testid', 'test-accordion')
      expect(accordion).toHaveAttribute('aria-label', 'Test Accordion')
    })

    it('should render multiple accordion items', () => {
      render(
        <Accordion>
          <AccordionItem value="item-1">
            <AccordionTrigger value="item-1">Item 1</AccordionTrigger>
            <AccordionContent value="item-1">Content 1</AccordionContent>
          </AccordionItem>
          <AccordionItem value="item-2">
            <AccordionTrigger value="item-2">Item 2</AccordionTrigger>
            <AccordionContent value="item-2">Content 2</AccordionContent>
          </AccordionItem>
        </Accordion>
      )
      expect(screen.getByText('Item 1')).toBeInTheDocument()
      expect(screen.getByText('Item 2')).toBeInTheDocument()
    })

    it('should default to type="single"', () => {
      render(
        <Accordion>
          <AccordionItem value="item-1">
            <AccordionTrigger value="item-1">Item 1</AccordionTrigger>
            <AccordionContent value="item-1">Content 1</AccordionContent>
          </AccordionItem>
          <AccordionItem value="item-2">
            <AccordionTrigger value="item-2">Item 2</AccordionTrigger>
            <AccordionContent value="item-2">Content 2</AccordionContent>
          </AccordionItem>
        </Accordion>
      )
      const trigger1 = screen.getByText('Item 1')
      const trigger2 = screen.getByText('Item 2')

      fireEvent.click(trigger1)
      expect(trigger1).toHaveAttribute('aria-expanded', 'true')

      fireEvent.click(trigger2)
      expect(trigger1).toHaveAttribute('aria-expanded', 'false')
      expect(trigger2).toHaveAttribute('aria-expanded', 'true')
    })
  })

  // ========================================
  // AccordionContext and Hook Tests
  // ========================================
  describe('AccordionContext and useAccordion Hook', () => {
    it('should throw error when AccordionItem used outside Accordion', () => {
      // Suppress console.error for this test
      const originalError = console.error
      console.error = jest.fn()

      expect(() => {
        render(
          <AccordionItem value="test">
            <div>Content</div>
          </AccordionItem>
        )
      }).toThrow('Accordion components must be used within an Accordion')

      console.error = originalError
    })

    it('should throw error when AccordionTrigger used outside Accordion', () => {
      const originalError = console.error
      console.error = jest.fn()

      expect(() => {
        render(<AccordionTrigger value="test">Trigger</AccordionTrigger>)
      }).toThrow('Accordion components must be used within an Accordion')

      console.error = originalError
    })

    it('should throw error when AccordionContent used outside Accordion', () => {
      const originalError = console.error
      console.error = jest.fn()

      expect(() => {
        render(<AccordionContent value="test">Content</AccordionContent>)
      }).toThrow('Accordion components must be used within an Accordion')

      console.error = originalError
    })

    it('should provide context to all child components', () => {
      render(
        <Accordion>
          <AccordionItem value="item-1">
            <AccordionTrigger value="item-1">Trigger</AccordionTrigger>
            <AccordionContent value="item-1">Content</AccordionContent>
          </AccordionItem>
        </Accordion>
      )

      const trigger = screen.getByText('Trigger')
      expect(trigger).toHaveAttribute('aria-expanded', 'false')

      fireEvent.click(trigger)
      expect(trigger).toHaveAttribute('aria-expanded', 'true')
      expect(screen.getByText('Content')).toBeVisible()
    })
  })

  // ========================================
  // Single Mode Behavior Tests
  // ========================================
  describe('Single Mode (type="single")', () => {
    it('should open item when clicked', () => {
      render(
        <Accordion type="single">
          <AccordionItem value="item-1">
            <AccordionTrigger value="item-1">Item 1</AccordionTrigger>
            <AccordionContent value="item-1">Content 1</AccordionContent>
          </AccordionItem>
        </Accordion>
      )

      const trigger = screen.getByText('Item 1')
      expect(trigger).toHaveAttribute('aria-expanded', 'false')

      fireEvent.click(trigger)
      expect(trigger).toHaveAttribute('aria-expanded', 'true')
      expect(screen.getByText('Content 1')).toBeVisible()
    })

    it('should close item when clicked again', () => {
      render(
        <Accordion type="single">
          <AccordionItem value="item-1">
            <AccordionTrigger value="item-1">Item 1</AccordionTrigger>
            <AccordionContent value="item-1">Content 1</AccordionContent>
          </AccordionItem>
        </Accordion>
      )

      const trigger = screen.getByText('Item 1')

      fireEvent.click(trigger)
      expect(trigger).toHaveAttribute('aria-expanded', 'true')

      fireEvent.click(trigger)
      expect(trigger).toHaveAttribute('aria-expanded', 'false')
    })

    it('should close previously open item when opening new item', () => {
      render(
        <Accordion type="single">
          <AccordionItem value="item-1">
            <AccordionTrigger value="item-1">Item 1</AccordionTrigger>
            <AccordionContent value="item-1">Content 1</AccordionContent>
          </AccordionItem>
          <AccordionItem value="item-2">
            <AccordionTrigger value="item-2">Item 2</AccordionTrigger>
            <AccordionContent value="item-2">Content 2</AccordionContent>
          </AccordionItem>
        </Accordion>
      )

      const trigger1 = screen.getByText('Item 1')
      const trigger2 = screen.getByText('Item 2')

      fireEvent.click(trigger1)
      expect(trigger1).toHaveAttribute('aria-expanded', 'true')
      expect(screen.getByText('Content 1')).toBeVisible()

      fireEvent.click(trigger2)
      expect(trigger1).toHaveAttribute('aria-expanded', 'false')
      expect(trigger2).toHaveAttribute('aria-expanded', 'true')
      expect(screen.getByText('Content 2')).toBeVisible()
    })

    it('should call onValueChange with string in single mode', () => {
      const handleValueChange = jest.fn()
      render(
        <Accordion type="single" onValueChange={handleValueChange}>
          <AccordionItem value="item-1">
            <AccordionTrigger value="item-1">Item 1</AccordionTrigger>
            <AccordionContent value="item-1">Content 1</AccordionContent>
          </AccordionItem>
        </Accordion>
      )

      const trigger = screen.getByText('Item 1')
      fireEvent.click(trigger)

      expect(handleValueChange).toHaveBeenCalledWith('item-1')
    })

    it('should call onValueChange with empty string when closing in single mode', () => {
      const handleValueChange = jest.fn()
      render(
        <Accordion type="single" onValueChange={handleValueChange}>
          <AccordionItem value="item-1">
            <AccordionTrigger value="item-1">Item 1</AccordionTrigger>
            <AccordionContent value="item-1">Content 1</AccordionContent>
          </AccordionItem>
        </Accordion>
      )

      const trigger = screen.getByText('Item 1')
      fireEvent.click(trigger)
      fireEvent.click(trigger)

      expect(handleValueChange).toHaveBeenLastCalledWith('')
    })

    it('should support defaultValue in single mode', () => {
      render(
        <Accordion type="single" defaultValue="item-2">
          <AccordionItem value="item-1">
            <AccordionTrigger value="item-1">Item 1</AccordionTrigger>
            <AccordionContent value="item-1">Content 1</AccordionContent>
          </AccordionItem>
          <AccordionItem value="item-2">
            <AccordionTrigger value="item-2">Item 2</AccordionTrigger>
            <AccordionContent value="item-2">Content 2</AccordionContent>
          </AccordionItem>
        </Accordion>
      )

      expect(screen.getByText('Item 1')).toHaveAttribute('aria-expanded', 'false')
      expect(screen.getByText('Item 2')).toHaveAttribute('aria-expanded', 'true')
      expect(screen.getByText('Content 2')).toBeVisible()
    })
  })

  // ========================================
  // Multiple Mode Behavior Tests
  // ========================================
  describe('Multiple Mode (type="multiple")', () => {
    it('should allow multiple items to be open', () => {
      render(
        <Accordion type="multiple">
          <AccordionItem value="item-1">
            <AccordionTrigger value="item-1">Item 1</AccordionTrigger>
            <AccordionContent value="item-1">Content 1</AccordionContent>
          </AccordionItem>
          <AccordionItem value="item-2">
            <AccordionTrigger value="item-2">Item 2</AccordionTrigger>
            <AccordionContent value="item-2">Content 2</AccordionContent>
          </AccordionItem>
        </Accordion>
      )

      const trigger1 = screen.getByText('Item 1')
      const trigger2 = screen.getByText('Item 2')

      fireEvent.click(trigger1)
      expect(trigger1).toHaveAttribute('aria-expanded', 'true')
      expect(screen.getByText('Content 1')).toBeVisible()

      fireEvent.click(trigger2)
      expect(trigger1).toHaveAttribute('aria-expanded', 'true')
      expect(trigger2).toHaveAttribute('aria-expanded', 'true')
      expect(screen.getByText('Content 1')).toBeVisible()
      expect(screen.getByText('Content 2')).toBeVisible()
    })

    it('should close individual items when clicked in multiple mode', () => {
      render(
        <Accordion type="multiple">
          <AccordionItem value="item-1">
            <AccordionTrigger value="item-1">Item 1</AccordionTrigger>
            <AccordionContent value="item-1">Content 1</AccordionContent>
          </AccordionItem>
          <AccordionItem value="item-2">
            <AccordionTrigger value="item-2">Item 2</AccordionTrigger>
            <AccordionContent value="item-2">Content 2</AccordionContent>
          </AccordionItem>
        </Accordion>
      )

      const trigger1 = screen.getByText('Item 1')
      const trigger2 = screen.getByText('Item 2')

      fireEvent.click(trigger1)
      fireEvent.click(trigger2)

      expect(trigger1).toHaveAttribute('aria-expanded', 'true')
      expect(trigger2).toHaveAttribute('aria-expanded', 'true')

      fireEvent.click(trigger1)
      expect(trigger1).toHaveAttribute('aria-expanded', 'false')
      expect(trigger2).toHaveAttribute('aria-expanded', 'true')
    })

    it('should call onValueChange with array in multiple mode', () => {
      const handleValueChange = jest.fn()
      render(
        <Accordion type="multiple" onValueChange={handleValueChange}>
          <AccordionItem value="item-1">
            <AccordionTrigger value="item-1">Item 1</AccordionTrigger>
            <AccordionContent value="item-1">Content 1</AccordionContent>
          </AccordionItem>
          <AccordionItem value="item-2">
            <AccordionTrigger value="item-2">Item 2</AccordionTrigger>
            <AccordionContent value="item-2">Content 2</AccordionContent>
          </AccordionItem>
        </Accordion>
      )

      const trigger1 = screen.getByText('Item 1')
      const trigger2 = screen.getByText('Item 2')

      fireEvent.click(trigger1)
      expect(handleValueChange).toHaveBeenCalledWith(['item-1'])

      fireEvent.click(trigger2)
      expect(handleValueChange).toHaveBeenCalledWith(['item-1', 'item-2'])
    })

    it('should call onValueChange with updated array when closing item', () => {
      const handleValueChange = jest.fn()
      render(
        <Accordion type="multiple" onValueChange={handleValueChange}>
          <AccordionItem value="item-1">
            <AccordionTrigger value="item-1">Item 1</AccordionTrigger>
            <AccordionContent value="item-1">Content 1</AccordionContent>
          </AccordionItem>
          <AccordionItem value="item-2">
            <AccordionTrigger value="item-2">Item 2</AccordionTrigger>
            <AccordionContent value="item-2">Content 2</AccordionContent>
          </AccordionItem>
        </Accordion>
      )

      const trigger1 = screen.getByText('Item 1')
      const trigger2 = screen.getByText('Item 2')

      fireEvent.click(trigger1)
      fireEvent.click(trigger2)
      fireEvent.click(trigger1)

      expect(handleValueChange).toHaveBeenLastCalledWith(['item-2'])
    })

    it('should support defaultValue array in multiple mode', () => {
      render(
        <Accordion type="multiple" defaultValue={['item-1', 'item-3']}>
          <AccordionItem value="item-1">
            <AccordionTrigger value="item-1">Item 1</AccordionTrigger>
            <AccordionContent value="item-1">Content 1</AccordionContent>
          </AccordionItem>
          <AccordionItem value="item-2">
            <AccordionTrigger value="item-2">Item 2</AccordionTrigger>
            <AccordionContent value="item-2">Content 2</AccordionContent>
          </AccordionItem>
          <AccordionItem value="item-3">
            <AccordionTrigger value="item-3">Item 3</AccordionTrigger>
            <AccordionContent value="item-3">Content 3</AccordionContent>
          </AccordionItem>
        </Accordion>
      )

      expect(screen.getByText('Item 1')).toHaveAttribute('aria-expanded', 'true')
      expect(screen.getByText('Item 2')).toHaveAttribute('aria-expanded', 'false')
      expect(screen.getByText('Item 3')).toHaveAttribute('aria-expanded', 'true')
      expect(screen.getByText('Content 1')).toBeVisible()
      expect(screen.getByText('Content 3')).toBeVisible()
    })

    it('should call onValueChange with empty array when all items closed', () => {
      const handleValueChange = jest.fn()
      render(
        <Accordion type="multiple" onValueChange={handleValueChange}>
          <AccordionItem value="item-1">
            <AccordionTrigger value="item-1">Item 1</AccordionTrigger>
            <AccordionContent value="item-1">Content 1</AccordionContent>
          </AccordionItem>
        </Accordion>
      )

      const trigger = screen.getByText('Item 1')
      fireEvent.click(trigger)
      fireEvent.click(trigger)

      expect(handleValueChange).toHaveBeenLastCalledWith([])
    })
  })

  // ========================================
  // Controlled vs Uncontrolled Tests
  // ========================================
  describe('Controlled vs Uncontrolled', () => {
    it('should work as uncontrolled component with defaultValue', () => {
      render(
        <Accordion defaultValue="item-1">
          <AccordionItem value="item-1">
            <AccordionTrigger value="item-1">Item 1</AccordionTrigger>
            <AccordionContent value="item-1">Content 1</AccordionContent>
          </AccordionItem>
          <AccordionItem value="item-2">
            <AccordionTrigger value="item-2">Item 2</AccordionTrigger>
            <AccordionContent value="item-2">Content 2</AccordionContent>
          </AccordionItem>
        </Accordion>
      )

      expect(screen.getByText('Item 1')).toHaveAttribute('aria-expanded', 'true')

      fireEvent.click(screen.getByText('Item 2'))
      expect(screen.getByText('Item 1')).toHaveAttribute('aria-expanded', 'false')
      expect(screen.getByText('Item 2')).toHaveAttribute('aria-expanded', 'true')
    })

    it('should work as controlled component with value prop', () => {
      const TestComponent = () => {
        const [value, setValue] = React.useState('item-1')
        return (
          <Accordion value={value} onValueChange={setValue as (value: string | string[]) => void}>
            <AccordionItem value="item-1">
              <AccordionTrigger value="item-1">Item 1</AccordionTrigger>
              <AccordionContent value="item-1">Content 1</AccordionContent>
            </AccordionItem>
            <AccordionItem value="item-2">
              <AccordionTrigger value="item-2">Item 2</AccordionTrigger>
              <AccordionContent value="item-2">Content 2</AccordionContent>
            </AccordionItem>
          </Accordion>
        )
      }

      render(<TestComponent />)

      expect(screen.getByText('Item 1')).toHaveAttribute('aria-expanded', 'true')

      fireEvent.click(screen.getByText('Item 2'))
      expect(screen.getByText('Item 2')).toHaveAttribute('aria-expanded', 'true')
      expect(screen.getByText('Item 1')).toHaveAttribute('aria-expanded', 'false')
    })

    it('should work as controlled component with array value in multiple mode', () => {
      const TestComponent = () => {
        const [value, setValue] = React.useState<string[]>(['item-1'])
        return (
          <Accordion type="multiple" value={value} onValueChange={setValue as (value: string | string[]) => void}>
            <AccordionItem value="item-1">
              <AccordionTrigger value="item-1">Item 1</AccordionTrigger>
              <AccordionContent value="item-1">Content 1</AccordionContent>
            </AccordionItem>
            <AccordionItem value="item-2">
              <AccordionTrigger value="item-2">Item 2</AccordionTrigger>
              <AccordionContent value="item-2">Content 2</AccordionContent>
            </AccordionItem>
          </Accordion>
        )
      }

      render(<TestComponent />)

      expect(screen.getByText('Item 1')).toHaveAttribute('aria-expanded', 'true')
      expect(screen.getByText('Item 2')).toHaveAttribute('aria-expanded', 'false')

      fireEvent.click(screen.getByText('Item 2'))
      expect(screen.getByText('Item 1')).toHaveAttribute('aria-expanded', 'true')
      expect(screen.getByText('Item 2')).toHaveAttribute('aria-expanded', 'true')
    })

    it('should not update internal state when controlled', () => {
      const handleValueChange = jest.fn()
      render(
        <Accordion value="item-1" onValueChange={handleValueChange}>
          <AccordionItem value="item-1">
            <AccordionTrigger value="item-1">Item 1</AccordionTrigger>
            <AccordionContent value="item-1">Content 1</AccordionContent>
          </AccordionItem>
          <AccordionItem value="item-2">
            <AccordionTrigger value="item-2">Item 2</AccordionTrigger>
            <AccordionContent value="item-2">Content 2</AccordionContent>
          </AccordionItem>
        </Accordion>
      )

      fireEvent.click(screen.getByText('Item 2'))
      expect(handleValueChange).toHaveBeenCalledWith('item-2')

      // Should still show item-1 as open since value prop hasn't changed
      expect(screen.getByText('Item 1')).toHaveAttribute('aria-expanded', 'true')
      expect(screen.getByText('Item 2')).toHaveAttribute('aria-expanded', 'false')
    })

    it('should handle empty string value in controlled mode', () => {
      render(
        <Accordion value="">
          <AccordionItem value="item-1">
            <AccordionTrigger value="item-1">Item 1</AccordionTrigger>
            <AccordionContent value="item-1">Content 1</AccordionContent>
          </AccordionItem>
        </Accordion>
      )

      expect(screen.getByText('Item 1')).toHaveAttribute('aria-expanded', 'false')
    })
  })

  // ========================================
  // AccordionItem Tests
  // ========================================
  describe('AccordionItem', () => {
    it('should render with data-state="closed" by default', () => {
      const { container } = render(
        <Accordion>
          <AccordionItem value="item-1">
            <AccordionTrigger value="item-1">Item 1</AccordionTrigger>
            <AccordionContent value="item-1">Content 1</AccordionContent>
          </AccordionItem>
        </Accordion>
      )

      const item = container.querySelector('[data-state]')
      expect(item).toHaveAttribute('data-state', 'closed')
    })

    it('should render with data-state="open" when opened', () => {
      const { container } = render(
        <Accordion defaultValue="item-1">
          <AccordionItem value="item-1">
            <AccordionTrigger value="item-1">Item 1</AccordionTrigger>
            <AccordionContent value="item-1">Content 1</AccordionContent>
          </AccordionItem>
        </Accordion>
      )

      const item = container.querySelector('[data-state]')
      expect(item).toHaveAttribute('data-state', 'open')
    })

    it('should apply custom className to AccordionItem', () => {
      const { container } = render(
        <Accordion>
          <AccordionItem value="item-1" className="custom-item">
            <AccordionTrigger value="item-1">Item 1</AccordionTrigger>
            <AccordionContent value="item-1">Content 1</AccordionContent>
          </AccordionItem>
        </Accordion>
      )

      const item = container.querySelector('[data-state]')
      expect(item).toHaveClass('custom-item')
    })

    it('should pass through additional props to AccordionItem', () => {
      const { container } = render(
        <Accordion>
          <AccordionItem value="item-1" data-testid="test-item">
            <AccordionTrigger value="item-1">Item 1</AccordionTrigger>
            <AccordionContent value="item-1">Content 1</AccordionContent>
          </AccordionItem>
        </Accordion>
      )

      const item = container.querySelector('[data-testid="test-item"]')
      expect(item).toBeInTheDocument()
    })
  })

  // ========================================
  // AccordionTrigger Tests
  // ========================================
  describe('AccordionTrigger', () => {
    it('should render as button element', () => {
      render(
        <Accordion>
          <AccordionItem value="item-1">
            <AccordionTrigger value="item-1">Trigger Text</AccordionTrigger>
            <AccordionContent value="item-1">Content</AccordionContent>
          </AccordionItem>
        </Accordion>
      )

      const trigger = screen.getByText('Trigger Text')
      expect(trigger.tagName).toBe('BUTTON')
      expect(trigger).toHaveAttribute('type', 'button')
    })

    it('should have aria-expanded="false" by default', () => {
      render(
        <Accordion>
          <AccordionItem value="item-1">
            <AccordionTrigger value="item-1">Trigger</AccordionTrigger>
            <AccordionContent value="item-1">Content</AccordionContent>
          </AccordionItem>
        </Accordion>
      )

      expect(screen.getByText('Trigger')).toHaveAttribute('aria-expanded', 'false')
    })

    it('should have aria-expanded="true" when open', () => {
      render(
        <Accordion defaultValue="item-1">
          <AccordionItem value="item-1">
            <AccordionTrigger value="item-1">Trigger</AccordionTrigger>
            <AccordionContent value="item-1">Content</AccordionContent>
          </AccordionItem>
        </Accordion>
      )

      expect(screen.getByText('Trigger')).toHaveAttribute('aria-expanded', 'true')
    })

    it('should render chevron icon', () => {
      const { container } = render(
        <Accordion>
          <AccordionItem value="item-1">
            <AccordionTrigger value="item-1">Trigger</AccordionTrigger>
            <AccordionContent value="item-1">Content</AccordionContent>
          </AccordionItem>
        </Accordion>
      )

      const svg = container.querySelector('svg')
      expect(svg).toBeInTheDocument()
      expect(svg).toHaveAttribute('aria-hidden', 'true')
    })

    it('should rotate chevron when open', () => {
      const { container } = render(
        <Accordion defaultValue="item-1">
          <AccordionItem value="item-1">
            <AccordionTrigger value="item-1">Trigger</AccordionTrigger>
            <AccordionContent value="item-1">Content</AccordionContent>
          </AccordionItem>
        </Accordion>
      )

      const svg = container.querySelector('svg')
      expect(svg).toHaveClass('rotate-180')
    })

    it('should not rotate chevron when closed', () => {
      const { container } = render(
        <Accordion>
          <AccordionItem value="item-1">
            <AccordionTrigger value="item-1">Trigger</AccordionTrigger>
            <AccordionContent value="item-1">Content</AccordionContent>
          </AccordionItem>
        </Accordion>
      )

      const svg = container.querySelector('svg')
      expect(svg).not.toHaveClass('rotate-180')
    })

    it('should apply custom className to AccordionTrigger', () => {
      render(
        <Accordion>
          <AccordionItem value="item-1">
            <AccordionTrigger value="item-1" className="custom-trigger">
              Trigger
            </AccordionTrigger>
            <AccordionContent value="item-1">Content</AccordionContent>
          </AccordionItem>
        </Accordion>
      )

      expect(screen.getByText('Trigger')).toHaveClass('custom-trigger')
    })

    it('should forward ref to trigger button', () => {
      const ref = React.createRef<HTMLButtonElement>()
      render(
        <Accordion>
          <AccordionItem value="item-1">
            <AccordionTrigger value="item-1" ref={ref}>
              Trigger
            </AccordionTrigger>
            <AccordionContent value="item-1">Content</AccordionContent>
          </AccordionItem>
        </Accordion>
      )

      expect(ref.current).toBeInstanceOf(HTMLButtonElement)
      expect(ref.current?.textContent).toContain('Trigger')
    })

    it('should pass through additional props to trigger', () => {
      render(
        <Accordion>
          <AccordionItem value="item-1">
            <AccordionTrigger value="item-1" data-testid="test-trigger" disabled>
              Trigger
            </AccordionTrigger>
            <AccordionContent value="item-1">Content</AccordionContent>
          </AccordionItem>
        </Accordion>
      )

      const trigger = screen.getByTestId('test-trigger')
      expect(trigger).toBeDisabled()
    })
  })

  // ========================================
  // AccordionContent Tests
  // ========================================
  describe('AccordionContent', () => {
    it('should be hidden when closed', () => {
      const { container } = render(
        <Accordion>
          <AccordionItem value="item-1">
            <AccordionTrigger value="item-1">Trigger</AccordionTrigger>
            <AccordionContent value="item-1">Content Text</AccordionContent>
          </AccordionItem>
        </Accordion>
      )

      const contentWrapper = container.querySelector('[role="region"]')
      expect(contentWrapper).toHaveClass('hidden')
    })

    it('should be visible when open', () => {
      render(
        <Accordion defaultValue="item-1">
          <AccordionItem value="item-1">
            <AccordionTrigger value="item-1">Trigger</AccordionTrigger>
            <AccordionContent value="item-1">Content Text</AccordionContent>
          </AccordionItem>
        </Accordion>
      )

      expect(screen.getByText('Content Text')).toBeVisible()
    })

    it('should have role="region"', () => {
      const { container } = render(
        <Accordion defaultValue="item-1">
          <AccordionItem value="item-1">
            <AccordionTrigger value="item-1">Trigger</AccordionTrigger>
            <AccordionContent value="item-1">Content</AccordionContent>
          </AccordionItem>
        </Accordion>
      )

      const region = container.querySelector('[role="region"]')
      expect(region).toBeInTheDocument()
    })

    it('should have aria-hidden="true" when closed', () => {
      const { container } = render(
        <Accordion>
          <AccordionItem value="item-1">
            <AccordionTrigger value="item-1">Trigger</AccordionTrigger>
            <AccordionContent value="item-1">Content</AccordionContent>
          </AccordionItem>
        </Accordion>
      )

      const region = container.querySelector('[role="region"]')
      expect(region).toHaveAttribute('aria-hidden', 'true')
    })

    it('should have aria-hidden="false" when open', () => {
      const { container } = render(
        <Accordion defaultValue="item-1">
          <AccordionItem value="item-1">
            <AccordionTrigger value="item-1">Trigger</AccordionTrigger>
            <AccordionContent value="item-1">Content</AccordionContent>
          </AccordionItem>
        </Accordion>
      )

      const region = container.querySelector('[role="region"]')
      expect(region).toHaveAttribute('aria-hidden', 'false')
    })

    it('should apply custom className to AccordionContent wrapper', () => {
      const { container } = render(
        <Accordion defaultValue="item-1">
          <AccordionItem value="item-1">
            <AccordionTrigger value="item-1">Trigger</AccordionTrigger>
            <AccordionContent value="item-1" className="custom-content">
              Content
            </AccordionContent>
          </AccordionItem>
        </Accordion>
      )

      const contentWrapper = container.querySelector('.custom-content')
      expect(contentWrapper).toBeInTheDocument()
    })

    it('should forward ref to content container', () => {
      const ref = React.createRef<HTMLDivElement>()
      render(
        <Accordion defaultValue="item-1">
          <AccordionItem value="item-1">
            <AccordionTrigger value="item-1">Trigger</AccordionTrigger>
            <AccordionContent value="item-1" ref={ref}>
              Content
            </AccordionContent>
          </AccordionItem>
        </Accordion>
      )

      expect(ref.current).toBeInstanceOf(HTMLDivElement)
      expect(ref.current).toHaveAttribute('role', 'region')
    })

    it('should pass through additional props to content', () => {
      const { container } = render(
        <Accordion defaultValue="item-1">
          <AccordionItem value="item-1">
            <AccordionTrigger value="item-1">Trigger</AccordionTrigger>
            <AccordionContent value="item-1" data-testid="test-content">
              Content
            </AccordionContent>
          </AccordionItem>
        </Accordion>
      )

      expect(container.querySelector('[data-testid="test-content"]')).toBeInTheDocument()
    })
  })

  // ========================================
  // Accessibility Tests
  // ========================================
  describe('Accessibility', () => {
    it('should support keyboard interaction', () => {
      render(
        <Accordion>
          <AccordionItem value="item-1">
            <AccordionTrigger value="item-1">Item 1</AccordionTrigger>
            <AccordionContent value="item-1">Content 1</AccordionContent>
          </AccordionItem>
        </Accordion>
      )

      const trigger = screen.getByText('Item 1')
      trigger.focus()
      expect(trigger).toHaveFocus()
    })

    it('should have proper ARIA attributes when closed', () => {
      const { container } = render(
        <Accordion>
          <AccordionItem value="item-1">
            <AccordionTrigger value="item-1">Trigger</AccordionTrigger>
            <AccordionContent value="item-1">Content</AccordionContent>
          </AccordionItem>
        </Accordion>
      )

      const trigger = screen.getByText('Trigger')
      const region = container.querySelector('[role="region"]')

      expect(trigger).toHaveAttribute('aria-expanded', 'false')
      expect(region).toHaveAttribute('aria-hidden', 'true')
    })

    it('should have proper ARIA attributes when open', () => {
      const { container } = render(
        <Accordion defaultValue="item-1">
          <AccordionItem value="item-1">
            <AccordionTrigger value="item-1">Trigger</AccordionTrigger>
            <AccordionContent value="item-1">Content</AccordionContent>
          </AccordionItem>
        </Accordion>
      )

      const trigger = screen.getByText('Trigger')
      const region = container.querySelector('[role="region"]')

      expect(trigger).toHaveAttribute('aria-expanded', 'true')
      expect(region).toHaveAttribute('aria-hidden', 'false')
    })

    it('should hide chevron icon from screen readers', () => {
      const { container } = render(
        <Accordion>
          <AccordionItem value="item-1">
            <AccordionTrigger value="item-1">Trigger</AccordionTrigger>
            <AccordionContent value="item-1">Content</AccordionContent>
          </AccordionItem>
        </Accordion>
      )

      const svg = container.querySelector('svg')
      expect(svg).toHaveAttribute('aria-hidden', 'true')
    })
  })

  // ========================================
  // Edge Cases Tests
  // ========================================
  describe('Edge Cases', () => {
    it('should handle accordion with no items', () => {
      const { container } = render(<Accordion />)
      expect(container.firstChild).toBeInTheDocument()
    })

    it('should handle accordion with single item', () => {
      render(
        <Accordion>
          <AccordionItem value="item-1">
            <AccordionTrigger value="item-1">Single Item</AccordionTrigger>
            <AccordionContent value="item-1">Content</AccordionContent>
          </AccordionItem>
        </Accordion>
      )

      const trigger = screen.getByText('Single Item')
      fireEvent.click(trigger)
      expect(trigger).toHaveAttribute('aria-expanded', 'true')
    })

    it('should handle rapid toggle clicks', () => {
      render(
        <Accordion>
          <AccordionItem value="item-1">
            <AccordionTrigger value="item-1">Item 1</AccordionTrigger>
            <AccordionContent value="item-1">Content 1</AccordionContent>
          </AccordionItem>
        </Accordion>
      )

      const trigger = screen.getByText('Item 1')

      fireEvent.click(trigger)
      fireEvent.click(trigger)
      fireEvent.click(trigger)
      fireEvent.click(trigger)

      expect(trigger).toHaveAttribute('aria-expanded', 'false')
    })

    it('should handle defaultValue with non-existent item', () => {
      render(
        <Accordion defaultValue="non-existent">
          <AccordionItem value="item-1">
            <AccordionTrigger value="item-1">Item 1</AccordionTrigger>
            <AccordionContent value="item-1">Content 1</AccordionContent>
          </AccordionItem>
        </Accordion>
      )

      expect(screen.getByText('Item 1')).toHaveAttribute('aria-expanded', 'false')
    })

    it('should handle empty defaultValue array in multiple mode', () => {
      render(
        <Accordion type="multiple" defaultValue={[]}>
          <AccordionItem value="item-1">
            <AccordionTrigger value="item-1">Item 1</AccordionTrigger>
            <AccordionContent value="item-1">Content 1</AccordionContent>
          </AccordionItem>
        </Accordion>
      )

      expect(screen.getByText('Item 1')).toHaveAttribute('aria-expanded', 'false')
    })
  })

  // ========================================
  // Integration Tests
  // ========================================
  describe('Integration', () => {
    it('should work with complex nested content', () => {
      render(
        <Accordion>
          <AccordionItem value="item-1">
            <AccordionTrigger value="item-1">Item 1</AccordionTrigger>
            <AccordionContent value="item-1">
              <div>
                <h4>Nested Header</h4>
                <p>Paragraph text</p>
                <button>Nested Button</button>
              </div>
            </AccordionContent>
          </AccordionItem>
        </Accordion>
      )

      fireEvent.click(screen.getByText('Item 1'))

      expect(screen.getByText('Nested Header')).toBeVisible()
      expect(screen.getByText('Paragraph text')).toBeVisible()
      expect(screen.getByText('Nested Button')).toBeVisible()
    })

    it('should maintain independent state for multiple accordions', () => {
      render(
        <>
          <Accordion data-testid="accordion-1">
            <AccordionItem value="item-1">
              <AccordionTrigger value="item-1">Accordion 1 Item</AccordionTrigger>
              <AccordionContent value="item-1">Content 1</AccordionContent>
            </AccordionItem>
          </Accordion>
          <Accordion data-testid="accordion-2">
            <AccordionItem value="item-2">
              <AccordionTrigger value="item-2">Accordion 2 Item</AccordionTrigger>
              <AccordionContent value="item-2">Content 2</AccordionContent>
            </AccordionItem>
          </Accordion>
        </>
      )

      fireEvent.click(screen.getByText('Accordion 1 Item'))

      expect(screen.getByText('Accordion 1 Item')).toHaveAttribute('aria-expanded', 'true')
      expect(screen.getByText('Accordion 2 Item')).toHaveAttribute('aria-expanded', 'false')
    })

    it('should work with all props combined', () => {
      const handleValueChange = jest.fn()
      render(
        <Accordion
          type="multiple"
          defaultValue={['item-1']}
          onValueChange={handleValueChange}
          className="custom-accordion"
          data-testid="full-accordion"
        >
          <AccordionItem value="item-1" className="custom-item-1">
            <AccordionTrigger value="item-1" className="custom-trigger-1">
              Item 1
            </AccordionTrigger>
            <AccordionContent value="item-1" className="custom-content-1">
              Content 1
            </AccordionContent>
          </AccordionItem>
          <AccordionItem value="item-2" className="custom-item-2">
            <AccordionTrigger value="item-2" className="custom-trigger-2">
              Item 2
            </AccordionTrigger>
            <AccordionContent value="item-2" className="custom-content-2">
              Content 2
            </AccordionContent>
          </AccordionItem>
        </Accordion>
      )

      expect(screen.getByTestId('full-accordion')).toHaveClass('custom-accordion')
      expect(screen.getByText('Item 1')).toHaveAttribute('aria-expanded', 'true')

      fireEvent.click(screen.getByText('Item 2'))
      expect(handleValueChange).toHaveBeenCalledWith(['item-1', 'item-2'])
    })
  })
})
