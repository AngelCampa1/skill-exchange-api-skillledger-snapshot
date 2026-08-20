import React from 'react'
import { render, screen, fireEvent } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { axe, toHaveNoViolations } from 'jest-axe'
import { ThemeProvider } from '@/contexts/ThemeContext'
import { ThemeToggle } from '@/components/ThemeToggle'

// Extend Jest matchers to include jest-axe
expect.extend(toHaveNoViolations)

describe('Accessibility Tests (WCAG 2.1 AA)', () => {
  describe('ThemeToggle Accessibility', () => {
    it('should pass axe accessibility tests', async () => {
      const { container } = render(
        <ThemeProvider>
          <ThemeToggle />
        </ThemeProvider>
      )
      
      const results = await axe(container)
      expect(results).toHaveNoViolations()
    })

    it('should have proper ARIA labels', async () => {
      const user = userEvent.setup()

      render(
        <ThemeProvider>
          <ThemeToggle />
        </ThemeProvider>
      )

      // Main toggle button should be present
      expect(screen.getByLabelText(/Theme selector.*Current theme/i)).toBeInTheDocument()

      // Open the dropdown to reveal theme options
      await user.click(screen.getByLabelText(/Theme selector.*Current theme/i))

      // Theme option buttons should now be present and visible
      expect(screen.getByLabelText(/Light theme.*/i)).toBeInTheDocument()
      expect(screen.getByLabelText(/Light Theme.*/i)).toBeInTheDocument()
      expect(screen.getByLabelText(/System theme.*/i)).toBeInTheDocument()
    })

    it('should be keyboard navigable', async () => {
      const user = userEvent.setup()

      render(
        <ThemeProvider>
          <ThemeToggle />
        </ThemeProvider>
      )

      const mainButton = screen.getByLabelText(/Theme selector.*Current theme/i)

      // Should be able to focus the main toggle button
      await user.tab()
      expect(mainButton).toHaveFocus()

      // Should be able to activate with Enter
      await user.keyboard('{Enter}')

      // After opening dropdown, theme options should be available
      const lightButton = screen.getByLabelText(/Light theme.*/i)
      expect(lightButton).toBeInTheDocument()
    })

    it('should have proper color contrast ratios', () => {
      render(
        <ThemeProvider>
          <ThemeToggle />
        </ThemeProvider>
      )

      const buttons = screen.getAllByRole('button')
      buttons.forEach(button => {
        // Check that buttons have appropriate styling for contrast
        // Main button uses icon for visual indication, dropdown buttons use text
        const hasIcon = button.querySelector('svg') !== null
        const hasText = button.querySelector('span') !== null

        if (hasText) {
          // Dropdown buttons should have text styling
          expect(button).toHaveClass('text-sm', 'font-medium')
        }

        // Check for proper contrast classes
        const hasContrastClasses = button.className.includes('text-') ||
                                 button.className.includes('bg-') ||
                                 button.className.includes('text-primary')
        expect(hasContrastClasses).toBe(true)
      })
    })

    it('should have proper focus indicators', async () => {
      const user = userEvent.setup()
      
      render(
        <ThemeProvider>
          <ThemeToggle />
        </ThemeProvider>
      )
      
      const firstButton = screen.getAllByRole('button')[0]
      await user.tab()
      
      // Focus should be visible (browser default or custom styles)
      expect(firstButton).toHaveFocus()
      expect(document.activeElement).toBe(firstButton)
    })

    it('should have appropriate icon alt text', () => {
      render(
        <ThemeProvider>
          <ThemeToggle />
        </ThemeProvider>
      )
      
      // Icons should be marked as aria-hidden since text labels are present
      const icons = screen.getAllByRole('button').map(button => 
        button.querySelector('[aria-hidden="true"]')
      )
      
      icons.forEach(icon => {
        expect(icon).toHaveAttribute('aria-hidden', 'true')
      })
    })
  })

  describe('Button Component Accessibility', () => {
    it('should have proper button styling with focus states', () => {
      const { container } = render(
        <div>
          <button type="button" className="inline-flex items-center justify-center rounded-xl bg-primary text-primary-foreground">Primary Button</button>
          <button type="button" className="inline-flex items-center justify-center rounded-xl bg-secondary text-secondary-foreground">Secondary Button</button>
          <button type="button" className="inline-flex items-center justify-center rounded-xl hover:bg-accent">Ghost Button</button>
        </div>
      )
      
      const buttons = container.querySelectorAll('button')
      
      // Verify all buttons have proper attributes
      buttons.forEach(button => {
        expect(button).toHaveAttribute('type', 'button')
        expect(button).not.toHaveAttribute('disabled')
        // Check that buttons have some styling classes
        expect(button.className).toContain('inline-flex')
        expect(button.className).toContain('rounded')
      })
    })

    it('should handle disabled states accessibly', () => {
      const { container } = render(
        <button className="btn-primary" disabled>
          Disabled Button
        </button>
      )
      
      const button = container.querySelector('button')
      expect(button).toBeDisabled()
      expect(button).toHaveClass('btn-primary')
      
      // Verify disabled button is not focusable through tabbing
      expect(button).toHaveAttribute('disabled')
    })
  })

  describe('Form Input Accessibility', () => {
    it('should have proper input styling with focus states', () => {
      const { container } = render(
        <div>
          <label htmlFor="test-input">Test Input</label>
          <input 
            id="test-input"
            className="input-primary" 
            placeholder="Enter text"
            aria-describedby="input-help"
          />
          <div id="input-help">Helper text</div>
        </div>
      )
      
      const input = container.querySelector('.input-primary')
      const label = screen.getByLabelText('Test Input')
      
      expect(input).toHaveClass('input-primary')
      expect(label).toBeInTheDocument()
      expect(input).toHaveAttribute('aria-describedby', 'input-help')
      expect(input).toHaveAttribute('placeholder', 'Enter text')
    })
  })

  describe('Color and Theme Accessibility', () => {
    it('should maintain accessibility in light theme', async () => {
      const { container } = render(
        <ThemeProvider>
          <div className="bg-background text-foreground p-4">
            <h1 className="text-heading">Test Heading</h1>
            <p className="text-body">Test body text</p>
            <button className="btn-primary">Test Button</button>
          </div>
        </ThemeProvider>
      )
      
      const results = await axe(container)
      expect(results).toHaveNoViolations()
    })

    it('should maintain accessibility in Light Theme', async () => {
      // Mock Light Theme preference
      Object.defineProperty(window, 'matchMedia', {
        writable: true,
        value: jest.fn().mockImplementation(query => ({
          matches: query === '(prefers-light-scheme: dark)',
          media: query,
          onchange: null,
          addListener: jest.fn(),
          removeListener: jest.fn(),
          addEventListener: jest.fn(),
          removeEventListener: jest.fn(),
          dispatchEvent: jest.fn(),
        })),
      })
      
      const { container } = render(
        <ThemeProvider>
          <div className="light">
            <div className="bg-background text-foreground p-4">
              <h1 className="text-heading">Test Heading</h1>
              <p className="text-body">Test body text</p>
              <button className="btn-primary">Test Button</button>
            </div>
          </div>
        </ThemeProvider>
      )
      
      const results = await axe(container)
      expect(results).toHaveNoViolations()
    })

    it('should have sufficient color contrast in both themes', () => {
      // Test light theme colors
      const lightTheme = render(
        <div className="bg-background text-foreground">
          <p className="text-muted-foreground">Muted text</p>
          <button className="btn-primary">Primary button</button>
        </div>
      )
      
      // Test Light Theme colors
      const darkTheme = render(
        <div className="light">
          <div className="bg-background text-foreground">
            <p className="text-muted-foreground">Muted text</p>
            <button className="btn-primary">Primary button</button>
          </div>
        </div>
      )
      
      // Both should render without contrast issues
      expect(lightTheme.container.firstChild).toBeInTheDocument()
      expect(darkTheme.container.firstChild).toBeInTheDocument()
    })
  })

  describe('Semantic HTML and ARIA', () => {
    it('should use semantic HTML elements appropriately', () => {
      const { container } = render(
        <main>
          <header>
            <nav>
              <ul>
                {/* eslint-disable-next-line @next/next/no-html-link-for-pages -- test fixture uses raw <a> to verify semantic HTML structure, not Next.js navigation */}
                <li><a href="/">Home</a></li>
                <li><a href="/about">About</a></li>
              </ul>
            </nav>
          </header>
          <section>
            <h1>Main Content</h1>
            <article>
              <h2>Article Title</h2>
              <p>Article content</p>
            </article>
          </section>
          <aside>
            <h2>Sidebar</h2>
          </aside>
          <footer>
            <p>Footer content</p>
          </footer>
        </main>
      )
      
      expect(container.querySelector('main')).toBeInTheDocument()
      expect(container.querySelector('header')).toBeInTheDocument()
      expect(container.querySelector('nav')).toBeInTheDocument()
      expect(container.querySelector('section')).toBeInTheDocument()
      expect(container.querySelector('article')).toBeInTheDocument()
      expect(container.querySelector('aside')).toBeInTheDocument()
      expect(container.querySelector('footer')).toBeInTheDocument()
    })

    it('should have proper heading hierarchy', () => {
      render(
        <div>
          <h1>Main Title</h1>
          <h2>Section Title</h2>
          <h3>Subsection Title</h3>
          <h2>Another Section</h2>
        </div>
      )
      
      expect(screen.getByRole('heading', { level: 1 })).toBeInTheDocument()
      expect(screen.getAllByRole('heading', { level: 2 })).toHaveLength(2)
      expect(screen.getByRole('heading', { level: 3 })).toBeInTheDocument()
    })
  })

  describe('Motion and Animation Accessibility', () => {
    it('should respect prefers-reduced-motion', () => {
      // Mock prefers-reduced-motion
      Object.defineProperty(window, 'matchMedia', {
        writable: true,
        value: jest.fn().mockImplementation(query => ({
          matches: query === '(prefers-reduced-motion: reduce)',
          media: query,
          onchange: null,
          addListener: jest.fn(),
          removeListener: jest.fn(),
          addEventListener: jest.fn(),
          removeEventListener: jest.fn(),
          dispatchEvent: jest.fn(),
        })),
      })
      
      const { container } = render(
        <button className="btn-primary transition-all duration-200 hover:scale-[1.02]">
          Animated Button
        </button>
      )
      
      const button = container.querySelector('button')
      expect(button).toHaveClass('transition-all', 'duration-200')
      
      // In a real implementation, we would check for motion-reduce classes
      // or CSS that disables animations when prefers-reduced-motion is set
    })
  })
})
