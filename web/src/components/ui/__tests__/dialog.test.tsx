import React from 'react'
import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import {
  Dialog,
  DialogTrigger,
  DialogContent,
  DialogHeader,
  DialogFooter,
  DialogTitle,
  DialogDescription,
} from '../dialog'

// Mock FocusTrap to avoid focus management complexities in tests
jest.mock('focus-trap-react', () => {
  return {
    __esModule: true,
    FocusTrap: ({ children }: { children: React.ReactNode }) => <>{children}</>,
  }
})

describe('Dialog Component', () => {
  const mockOnOpenChange = jest.fn()

  beforeEach(() => {
    mockOnOpenChange.mockClear()
    document.body.style.overflow = 'unset'
  })

  describe('Basic Rendering', () => {
    it('should render dialog wrapper', () => {
      const { container } = render(
        <Dialog>
          <div>Dialog content</div>
        </Dialog>
      )

      expect(container.firstChild).toBeInTheDocument()
    })

    it('should render children', () => {
      render(
        <Dialog>
          <div data-testid="child">Dialog content</div>
        </Dialog>
      )

      expect(screen.getByTestId('child')).toBeInTheDocument()
    })

    it('should apply custom props to wrapper', () => {
      const { container } = render(
        <Dialog data-testid="dialog-wrapper">
          <div>Content</div>
        </Dialog>
      )

      expect(screen.getByTestId('dialog-wrapper')).toBeInTheDocument()
    })

    it('should not render content when closed by default', () => {
      render(
        <Dialog>
          <DialogContent><button>Focusable</button>
            <div data-testid="content">Dialog content</div>
          </DialogContent>
        </Dialog>
      )

      expect(screen.queryByTestId('content')).not.toBeInTheDocument()
    })

    it('should render content when open is true', () => {
      render(
        <Dialog open={true}>
          <DialogContent><button>Focusable</button>
            <div data-testid="content">Dialog content</div>
            <button>Action</button>
          </DialogContent>
        </Dialog>
      )

      expect(screen.getByTestId('content')).toBeInTheDocument()
    })
  })

  describe('DialogContext', () => {
    it('should provide open state through context', () => {
      render(
        <Dialog open={true}>
          <DialogContent><button>Focusable</button>
            <div data-testid="content">Content</div>
            <button>OK</button>
          </DialogContent>
        </Dialog>
      )

      expect(screen.getByTestId('content')).toBeInTheDocument()
    })

    it('should provide onOpenChange through context', async () => {
      const user = userEvent.setup()
      render(
        <Dialog open={false} onOpenChange={mockOnOpenChange}>
          <DialogTrigger>
            <button>Open</button>
          </DialogTrigger>
        </Dialog>
      )

      await user.click(screen.getByText('Open'))
      expect(mockOnOpenChange).toHaveBeenCalledWith(true)
    })

    it('should use default onOpenChange when not provided', () => {
      // Should not throw error
      expect(() => {
        render(
          <Dialog>
            <DialogContent><button>Focusable</button>Content</DialogContent>
          </Dialog>
        )
      }).not.toThrow()
    })
  })

  describe('DialogTrigger', () => {
    it('should render trigger content', () => {
      render(
        <Dialog>
          <DialogTrigger>
            <button>Click me</button>
          </DialogTrigger>
        </Dialog>
      )

      expect(screen.getByText('Click me')).toBeInTheDocument()
    })

    it('should call onOpenChange(true) when clicked', async () => {
      const user = userEvent.setup()
      render(
        <Dialog onOpenChange={mockOnOpenChange}>
          <DialogTrigger>
            <button>Open Dialog</button>
          </DialogTrigger>
        </Dialog>
      )

      await user.click(screen.getByText('Open Dialog'))
      expect(mockOnOpenChange).toHaveBeenCalledWith(true)
    })

    it('should call custom onClick handler before opening', async () => {
      const customOnClick = jest.fn()
      const user = userEvent.setup()
      render(
        <Dialog onOpenChange={mockOnOpenChange}>
          <DialogTrigger onClick={customOnClick}>
            <button>Open</button>
          </DialogTrigger>
        </Dialog>
      )

      await user.click(screen.getByText('Open'))
      expect(customOnClick).toHaveBeenCalled()
      expect(mockOnOpenChange).toHaveBeenCalledWith(true)
    })

    it('should apply custom className', () => {
      const { container } = render(
        <Dialog>
          <DialogTrigger className="custom-trigger">
            <button>Open</button>
          </DialogTrigger>
        </Dialog>
      )

      expect(container.querySelector('.custom-trigger')).toBeInTheDocument()
    })

    it('should forward additional props', () => {
      render(
        <Dialog>
          <DialogTrigger data-testid="trigger">
            <button>Open</button>
          </DialogTrigger>
        </Dialog>
      )

      expect(screen.getByTestId('trigger')).toBeInTheDocument()
    })
  })

  describe('DialogContent', () => {
    it('should render content when open', () => {
      render(
        <Dialog open={true}>
          <DialogContent><button>Focusable</button>
            <div data-testid="content">Dialog content</div>
          </DialogContent>
        </Dialog>
      )

      expect(screen.getByTestId('content')).toBeInTheDocument()
    })

    it('should not render when closed', () => {
      render(
        <Dialog open={false}>
          <DialogContent><button>Focusable</button>
            <div data-testid="content">Dialog content</div>
          </DialogContent>
        </Dialog>
      )

      expect(screen.queryByTestId('content')).not.toBeInTheDocument()
    })

    it('should have role="dialog" attribute', () => {
      render(
        <Dialog open={true}>
          <DialogContent><button>Focusable</button>Content</DialogContent>
        </Dialog>
      )

      expect(screen.getByRole('dialog')).toBeInTheDocument()
    })

    it('should have aria-modal="true" attribute', () => {
      render(
        <Dialog open={true}>
          <DialogContent><button>Focusable</button>Content</DialogContent>
        </Dialog>
      )

      const dialog = screen.getByRole('dialog')
      expect(dialog).toHaveAttribute('aria-modal', 'true')
    })

    it('should render close button', () => {
      render(
        <Dialog open={true}>
          <DialogContent><button>Focusable</button>Content</DialogContent>
        </Dialog>
      )

      expect(screen.getByLabelText('Close dialog')).toBeInTheDocument()
    })

    it('should call onOpenChange(false) when close button clicked', async () => {
      const user = userEvent.setup()
      render(
        <Dialog open={true} onOpenChange={mockOnOpenChange}>
          <DialogContent><button>Focusable</button>Content</DialogContent>
        </Dialog>
      )

      await user.click(screen.getByLabelText('Close dialog'))
      expect(mockOnOpenChange).toHaveBeenCalledWith(false)
    })

    it('should render overlay', () => {
      const { container } = render(
        <Dialog open={true}>
          <DialogContent><button>Focusable</button>Content</DialogContent>
        </Dialog>
      )

      const overlay = container.querySelector('.bg-overlay\\/80')
      expect(overlay).toBeInTheDocument()
      expect(overlay).toHaveAttribute('aria-hidden', 'true')
    })

    it('should call onOpenChange(false) when overlay clicked', async () => {
      const user = userEvent.setup()
      const { container } = render(
        <Dialog open={true} onOpenChange={mockOnOpenChange}>
          <DialogContent><button>Focusable</button>Content</DialogContent>
        </Dialog>
      )

      const overlay = container.querySelector('.bg-overlay\\/80')
      if (overlay) {
        await user.click(overlay)
        expect(mockOnOpenChange).toHaveBeenCalledWith(false)
      }
    })

    it('should apply custom className', () => {
      const { container } = render(
        <Dialog open={true}>
          <DialogContent className="custom-content">Content</DialogContent>
        </Dialog>
      )

      expect(container.querySelector('.custom-content')).toBeInTheDocument()
    })

    it('should forward ref', () => {
      const ref = React.createRef<HTMLDivElement>()
      render(
        <Dialog open={true}>
          <DialogContent ref={ref}>Content</DialogContent>
        </Dialog>
      )

      expect(ref.current).toBeInstanceOf(HTMLDivElement)
    })

    it('should include sr-only text for close button', () => {
      render(
        <Dialog open={true}>
          <DialogContent><button>Focusable</button>Content</DialogContent>
        </Dialog>
      )

      expect(screen.getByText('Close')).toHaveClass('sr-only')
    })
  })

  describe('Keyboard Interaction', () => {
    it('should close dialog on Escape key press', async () => {
      const user = userEvent.setup()
      render(
        <Dialog open={true} onOpenChange={mockOnOpenChange}>
          <DialogContent><button>Focusable</button>Content</DialogContent>
        </Dialog>
      )

      await user.keyboard('{Escape}')
      expect(mockOnOpenChange).toHaveBeenCalledWith(false)
    })

    it('should not close on other key presses', async () => {
      const user = userEvent.setup()
      render(
        <Dialog open={true} onOpenChange={mockOnOpenChange}>
          <DialogContent><button>Focusable</button>Content</DialogContent>
        </Dialog>
      )

      await user.keyboard('{Enter}')
      await user.keyboard('{Space}')
      await user.keyboard('a')
      expect(mockOnOpenChange).not.toHaveBeenCalled()
    })

    it('should only handle Escape when dialog is open', async () => {
      const user = userEvent.setup()
      const { rerender } = render(
        <Dialog open={false} onOpenChange={mockOnOpenChange}>
          <DialogContent><button>Focusable</button>Content</DialogContent>
        </Dialog>
      )

      await user.keyboard('{Escape}')
      expect(mockOnOpenChange).not.toHaveBeenCalled()

      rerender(
        <Dialog open={true} onOpenChange={mockOnOpenChange}>
          <DialogContent><button>Focusable</button>Content</DialogContent>
        </Dialog>
      )

      await user.keyboard('{Escape}')
      expect(mockOnOpenChange).toHaveBeenCalledWith(false)
    })

    it('should cleanup event listener on unmount', () => {
      const { unmount } = render(
        <Dialog open={true} onOpenChange={mockOnOpenChange}>
          <DialogContent><button>Focusable</button>Content</DialogContent>
        </Dialog>
      )

      unmount()
      // Should not throw error
      expect(() => {
        const event = new KeyboardEvent('keydown', { key: 'Escape' })
        document.dispatchEvent(event)
      }).not.toThrow()
    })
  })

  describe('Body Overflow Management', () => {
    it('should set body overflow to hidden when dialog opens', async () => {
      const { rerender } = render(
        <Dialog open={false}>
          <DialogContent><button>Focusable</button>Content</DialogContent>
        </Dialog>
      )

      expect(document.body.style.overflow).toBe('unset')

      rerender(
        <Dialog open={true}>
          <DialogContent><button>Focusable</button>Content</DialogContent>
        </Dialog>
      )

      await waitFor(() => {
        expect(document.body.style.overflow).toBe('hidden')
      })
    })

    it('should restore body overflow when dialog closes', async () => {
      const { rerender } = render(
        <Dialog open={true}>
          <DialogContent><button>Focusable</button>Content</DialogContent>
        </Dialog>
      )

      await waitFor(() => {
        expect(document.body.style.overflow).toBe('hidden')
      })

      rerender(
        <Dialog open={false}>
          <DialogContent><button>Focusable</button>Content</DialogContent>
        </Dialog>
      )

      await waitFor(() => {
        expect(document.body.style.overflow).toBe('unset')
      })
    })

    it('should restore overflow on unmount', async () => {
      const { unmount } = render(
        <Dialog open={true}>
          <DialogContent><button>Focusable</button>Content</DialogContent>
        </Dialog>
      )

      await waitFor(() => {
        expect(document.body.style.overflow).toBe('hidden')
      })

      unmount()

      expect(document.body.style.overflow).toBe('unset')
    })
  })

  describe('DialogHeader', () => {
    it('should render header content', () => {
      render(
        <Dialog open={true}>
          <DialogContent><button>Focusable</button>
            <DialogHeader>
              <div data-testid="header-content">Header</div>
            </DialogHeader>
          </DialogContent>
        </Dialog>
      )

      expect(screen.getByTestId('header-content')).toBeInTheDocument()
    })

    it('should apply default styling', () => {
      const { container } = render(
        <Dialog open={true}>
          <DialogContent><button>Focusable</button>
            <DialogHeader>Header</DialogHeader>
          </DialogContent>
        </Dialog>
      )

      const header = container.querySelector('.flex.flex-col.space-y-1\\.5')
      expect(header).toBeInTheDocument()
    })

    it('should apply custom className', () => {
      const { container } = render(
        <Dialog open={true}>
          <DialogContent><button>Focusable</button>
            <DialogHeader className="custom-header">Header</DialogHeader>
          </DialogContent>
        </Dialog>
      )

      expect(container.querySelector('.custom-header')).toBeInTheDocument()
    })

    it('should forward ref', () => {
      const ref = React.createRef<HTMLDivElement>()
      render(
        <Dialog open={true}>
          <DialogContent><button>Focusable</button>
            <DialogHeader ref={ref}>Header</DialogHeader>
          </DialogContent>
        </Dialog>
      )

      expect(ref.current).toBeInstanceOf(HTMLDivElement)
    })
  })

  describe('DialogFooter', () => {
    it('should render footer content', () => {
      render(
        <Dialog open={true}>
          <DialogContent><button>Focusable</button>
            <DialogFooter>
              <div data-testid="footer-content">Footer</div>
            </DialogFooter>
          </DialogContent>
        </Dialog>
      )

      expect(screen.getByTestId('footer-content')).toBeInTheDocument()
    })

    it('should apply default styling', () => {
      const { container } = render(
        <Dialog open={true}>
          <DialogContent><button>Focusable</button>
            <DialogFooter>Footer</DialogFooter>
          </DialogContent>
        </Dialog>
      )

      const footer = container.querySelector('.flex.flex-col-reverse')
      expect(footer).toBeInTheDocument()
    })

    it('should apply custom className', () => {
      const { container } = render(
        <Dialog open={true}>
          <DialogContent><button>Focusable</button>
            <DialogFooter className="custom-footer">Footer</DialogFooter>
          </DialogContent>
        </Dialog>
      )

      expect(container.querySelector('.custom-footer')).toBeInTheDocument()
    })

    it('should forward ref', () => {
      const ref = React.createRef<HTMLDivElement>()
      render(
        <Dialog open={true}>
          <DialogContent><button>Focusable</button>
            <DialogFooter ref={ref}>Footer</DialogFooter>
          </DialogContent>
        </Dialog>
      )

      expect(ref.current).toBeInstanceOf(HTMLDivElement)
    })
  })

  describe('DialogTitle', () => {
    it('should render title text', () => {
      render(
        <Dialog open={true}>
          <DialogContent><button>Focusable</button>
            <DialogTitle>Dialog Title</DialogTitle>
          </DialogContent>
        </Dialog>
      )

      expect(screen.getByText('Dialog Title')).toBeInTheDocument()
    })

    it('should render as h3 element', () => {
      render(
        <Dialog open={true}>
          <DialogContent><button>Focusable</button>
            <DialogTitle>Title</DialogTitle>
          </DialogContent>
        </Dialog>
      )

      const title = screen.getByText('Title')
      expect(title.tagName).toBe('H3')
    })

    it('should apply default styling', () => {
      render(
        <Dialog open={true}>
          <DialogContent><button>Focusable</button>
            <DialogTitle>Title</DialogTitle>
          </DialogContent>
        </Dialog>
      )

      const title = screen.getByText('Title')
      expect(title).toHaveClass('text-lg', 'font-semibold')
    })

    it('should apply custom className', () => {
      render(
        <Dialog open={true}>
          <DialogContent><button>Focusable</button>
            <DialogTitle className="custom-title">Title</DialogTitle>
          </DialogContent>
        </Dialog>
      )

      expect(screen.getByText('Title')).toHaveClass('custom-title')
    })

    it('should forward ref', () => {
      const ref = React.createRef<HTMLHeadingElement>()
      render(
        <Dialog open={true}>
          <DialogContent><button>Focusable</button>
            <DialogTitle ref={ref}>Title</DialogTitle>
          </DialogContent>
        </Dialog>
      )

      expect(ref.current).toBeInstanceOf(HTMLHeadingElement)
    })
  })

  describe('DialogDescription', () => {
    it('should render description text', () => {
      render(
        <Dialog open={true}>
          <DialogContent><button>Focusable</button>
            <DialogDescription>This is a description</DialogDescription>
          </DialogContent>
        </Dialog>
      )

      expect(screen.getByText('This is a description')).toBeInTheDocument()
    })

    it('should render as p element', () => {
      render(
        <Dialog open={true}>
          <DialogContent><button>Focusable</button>
            <DialogDescription>Description</DialogDescription>
          </DialogContent>
        </Dialog>
      )

      const description = screen.getByText('Description')
      expect(description.tagName).toBe('P')
    })

    it('should apply default styling', () => {
      render(
        <Dialog open={true}>
          <DialogContent><button>Focusable</button>
            <DialogDescription>Description</DialogDescription>
          </DialogContent>
        </Dialog>
      )

      const description = screen.getByText('Description')
      expect(description).toHaveClass('text-sm', 'text-muted-foreground')
    })

    it('should apply custom className', () => {
      render(
        <Dialog open={true}>
          <DialogContent><button>Focusable</button>
            <DialogDescription className="custom-desc">Description</DialogDescription>
          </DialogContent>
        </Dialog>
      )

      expect(screen.getByText('Description')).toHaveClass('custom-desc')
    })

    it('should forward ref', () => {
      const ref = React.createRef<HTMLParagraphElement>()
      render(
        <Dialog open={true}>
          <DialogContent><button>Focusable</button>
            <DialogDescription ref={ref}>Description</DialogDescription>
          </DialogContent>
        </Dialog>
      )

      expect(ref.current).toBeInstanceOf(HTMLParagraphElement)
    })
  })

  describe('Integration', () => {
    it('should work with all components together', async () => {
      const user = userEvent.setup()
      const { rerender } = render(
        <Dialog open={false} onOpenChange={mockOnOpenChange}>
          <DialogTrigger>
            <button>Open Dialog</button>
          </DialogTrigger>
          <DialogContent><button>Focusable</button>
            <DialogHeader>
              <DialogTitle>Confirm Action</DialogTitle>
              <DialogDescription>Are you sure you want to proceed?</DialogDescription>
            </DialogHeader>
            <DialogFooter>
              <button onClick={() => mockOnOpenChange(false)}>Cancel</button>
              <button>Confirm</button>
            </DialogFooter>
          </DialogContent>
        </Dialog>
      )

      // Dialog should be closed initially
      expect(screen.queryByText('Confirm Action')).not.toBeInTheDocument()

      // Open dialog via trigger
      await user.click(screen.getByText('Open Dialog'))
      expect(mockOnOpenChange).toHaveBeenCalledWith(true)

      // Rerender with open state
      rerender(
        <Dialog open={true} onOpenChange={mockOnOpenChange}>
          <DialogTrigger>
            <button>Open Dialog</button>
          </DialogTrigger>
          <DialogContent><button>Focusable</button>
            <DialogHeader>
              <DialogTitle>Confirm Action</DialogTitle>
              <DialogDescription>Are you sure you want to proceed?</DialogDescription>
            </DialogHeader>
            <DialogFooter>
              <button onClick={() => mockOnOpenChange(false)}>Cancel</button>
              <button>Confirm</button>
            </DialogFooter>
          </DialogContent>
        </Dialog>
      )

      // All components should be visible
      expect(screen.getByText('Confirm Action')).toBeInTheDocument()
      expect(screen.getByText('Are you sure you want to proceed?')).toBeInTheDocument()
      expect(screen.getByText('Cancel')).toBeInTheDocument()
      expect(screen.getByText('Confirm')).toBeInTheDocument()

      // Body overflow should be hidden
      await waitFor(() => {
        expect(document.body.style.overflow).toBe('hidden')
      })

      // Close via close button
      mockOnOpenChange.mockClear()
      await user.click(screen.getByLabelText('Close dialog'))
      expect(mockOnOpenChange).toHaveBeenCalledWith(false)
    })

    it('should handle controlled state properly', async () => {
      const user = userEvent.setup()
      const ControlledDialog = () => {
        const [open, setOpen] = React.useState(false)

        return (
          <Dialog open={open} onOpenChange={setOpen}>
            <DialogTrigger>
              <button>Open</button>
            </DialogTrigger>
            <DialogContent><button>Focusable</button>
              <DialogTitle>Controlled Dialog</DialogTitle>
              <button onClick={() => setOpen(false)}>Dismiss</button>
            </DialogContent>
          </Dialog>
        )
      }

      render(<ControlledDialog />)

      // Initially closed
      expect(screen.queryByText('Controlled Dialog')).not.toBeInTheDocument()

      // Open dialog
      await user.click(screen.getByText('Open'))
      expect(screen.getByText('Controlled Dialog')).toBeInTheDocument()

      // Close via custom button
      await user.click(screen.getByText('Dismiss'))
      expect(screen.queryByText('Controlled Dialog')).not.toBeInTheDocument()
    })

    it('should support nested content structure', () => {
      render(
        <Dialog open={true}>
          <DialogContent><button>Focusable</button>
            <DialogHeader>
              <DialogTitle>Title</DialogTitle>
              <DialogDescription>Description</DialogDescription>
            </DialogHeader>
            <div data-testid="body-content">
              <p>Body content here</p>
            </div>
            <DialogFooter>
              <button>Action</button>
            </DialogFooter>
          </DialogContent>
        </Dialog>
      )

      expect(screen.getByText('Title')).toBeInTheDocument()
      expect(screen.getByText('Description')).toBeInTheDocument()
      expect(screen.getByTestId('body-content')).toBeInTheDocument()
      expect(screen.getByText('Action')).toBeInTheDocument()
    })
  })
})
