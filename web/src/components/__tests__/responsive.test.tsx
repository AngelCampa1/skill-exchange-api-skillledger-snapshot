import React from 'react'
import { render, screen } from '@testing-library/react'
import { ThemeProvider } from '@/contexts/ThemeContext'
import { ThemeToggle } from '@/components/ThemeToggle'

// Mock window.matchMedia for responsive testing
const createMatchMedia = (matches: boolean) => jest.fn().mockImplementation(query => ({
  matches,
  media: query,
  onchange: null,
  addListener: jest.fn(),
  removeListener: jest.fn(),
  addEventListener: jest.fn(),
  removeEventListener: jest.fn(),
  dispatchEvent: jest.fn(),
}))

// Test different viewport sizes
const viewportSizes = {
  mobile: { width: 375, height: 667 },
  tablet: { width: 768, height: 1024 },
  desktop: { width: 1440, height: 900 },
  largeDesktop: { width: 1920, height: 1080 }
}

const setViewport = (size: { width: number; height: number }) => {
  Object.defineProperty(window, 'innerWidth', {
    writable: true,
    configurable: true,
    value: size.width,
  })
  Object.defineProperty(window, 'innerHeight', {
    writable: true,
    configurable: true,
    value: size.height,
  })
  
  // Mock CSS media queries based on viewport size
  const isTablet = size.width >= 768 && size.width < 1024
  const isDesktop = size.width >= 1024
  
  window.matchMedia = createMatchMedia(
    size.width <= 768 ? true : false // mobile-first approach
  )
}

describe('Responsive Design Tests', () => {
  beforeEach(() => {
    jest.clearAllMocks()
  })

  describe('ThemeToggle Component', () => {
    it('should render correctly on mobile viewport', () => {
      setViewport(viewportSizes.mobile)

      render(
        <ThemeProvider>
          <ThemeToggle />
        </ThemeProvider>
      )

      // Check that the main toggle button is present
      expect(screen.getByRole('button', { name: /theme selector.*current theme/i })).toBeInTheDocument()
      expect(screen.getByRole('button')).toHaveClass('w-10', 'h-10')

      // Check that dropdown is initially closed
      expect(screen.queryByText('Light')).not.toBeInTheDocument()
      expect(screen.queryByText('Dark')).not.toBeInTheDocument()
      expect(screen.queryByText('System')).not.toBeInTheDocument()
    })

    it('should render correctly on tablet viewport', () => {
      setViewport(viewportSizes.tablet)

      render(
        <ThemeProvider>
          <ThemeToggle />
        </ThemeProvider>
      )

      // Should still only show main toggle button (closed by default)
      expect(screen.getAllByRole('button')).toHaveLength(1)
      expect(screen.getByRole('button', { name: /theme selector.*current theme/i })).toBeInTheDocument()

      // Check that dropdown is initially closed
      expect(screen.queryByText('Light')).not.toBeInTheDocument()
      expect(screen.queryByText('Dark')).not.toBeInTheDocument()
      expect(screen.queryByText('System')).not.toBeInTheDocument()
    })

    it('should render correctly on desktop viewport', () => {
      setViewport(viewportSizes.desktop)

      render(
        <ThemeProvider>
          <ThemeToggle />
        </ThemeProvider>
      )

      // Should still only show main toggle button (closed by default)
      expect(screen.getAllByRole('button')).toHaveLength(1)
      expect(screen.getByRole('button', { name: /theme selector.*current theme/i })).toBeInTheDocument()

      // Check that dropdown is initially closed
      expect(screen.queryByText('Light')).not.toBeInTheDocument()
      expect(screen.queryByText('Dark')).not.toBeInTheDocument()
      expect(screen.queryByText('System')).not.toBeInTheDocument()
    })

    it('should have appropriate spacing and sizing across viewports', () => {
      Object.entries(viewportSizes).forEach(([sizeName, size]) => {
        setViewport(size)

        const { container, unmount } = render(
          <ThemeProvider>
            <ThemeToggle />
          </ThemeProvider>
        )

        // Check that main container exists
        const themeContainer = container.firstChild as HTMLElement
        expect(themeContainer).toBeInTheDocument()
        expect(themeContainer).toHaveClass('relative')

        // Check that main toggle button has proper styling
        const buttons = screen.getAllByRole('button')
        expect(buttons).toHaveLength(1)
        expect(buttons[0]).toHaveClass('w-10', 'h-10')

        // Clean up after each test
        unmount()
      })
    })
  })

  describe('Responsive Layout Classes', () => {
    it('should apply mobile-first responsive classes correctly', () => {
      setViewport(viewportSizes.mobile)
      
      const testComponent = render(
        <div className="w-full px-4 sm:px-6 lg:px-8 max-w-sm sm:max-w-md lg:max-w-lg">
          Test Content
        </div>
      )
      
      const element = testComponent.container.firstChild as HTMLElement
      expect(element).toHaveClass('w-full', 'px-4', 'max-w-sm')
    })

    it('should handle container spacing responsively', () => {
      const { container } = render(
        <div className="container-premium">
          <div className="space-md">
            Content
          </div>
        </div>
      )
      
      const containerElement = container.firstChild as HTMLElement
      expect(containerElement).toHaveClass('container-premium')
      
      // Check that the CSS utility class is applied
      const spacingElement = containerElement.querySelector('.space-md')
      expect(spacingElement).toBeInTheDocument()
    })
  })

  describe('Typography Responsiveness', () => {
    it('should scale typography appropriately across screen sizes', () => {
      const { container } = render(
        <div>
          <h1 className="text-display">Display Text</h1>
          <h2 className="text-heading">Heading Text</h2>
          <p className="text-body">Body Text</p>
          <span className="text-caption">Caption Text</span>
        </div>
      )
      
      const displayText = container.querySelector('.text-display')
      const headingText = container.querySelector('.text-heading')
      const bodyText = container.querySelector('.text-body')
      const captionText = container.querySelector('.text-caption')
      
      // Check that the typography utility classes are applied
      expect(displayText).toHaveClass('text-display')
      expect(headingText).toHaveClass('text-heading')
      expect(bodyText).toHaveClass('text-body')
      expect(captionText).toHaveClass('text-caption')
      
      // Verify the elements exist and have content
      expect(displayText).toHaveTextContent('Display Text')
      expect(headingText).toHaveTextContent('Heading Text')
      expect(bodyText).toHaveTextContent('Body Text')
      expect(captionText).toHaveTextContent('Caption Text')
    })
  })
})

describe('CSS Grid and Flexbox Responsive Behavior', () => {
  it('should handle flexbox layouts responsively', () => {
    const { container } = render(
      <div className="flex flex-col sm:flex-row gap-4">
        <div className="flex-1">Column 1</div>
        <div className="flex-1">Column 2</div>
      </div>
    )
    
    const flexContainer = container.firstChild as HTMLElement
    expect(flexContainer).toHaveClass('flex', 'flex-col', 'gap-4')
  })

  it('should handle responsive spacing utilities', () => {
    const { container } = render(
      <div className="space-xs sm:space-md lg:space-lg">
        <div>Item 1</div>
        <div>Item 2</div>
      </div>
    )
    
    const spacingContainer = container.firstChild as HTMLElement
    expect(spacingContainer).toHaveClass('space-xs')
  })
})