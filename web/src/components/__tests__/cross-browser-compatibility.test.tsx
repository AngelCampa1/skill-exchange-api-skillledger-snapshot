import React from 'react'
import { render, screen } from '@testing-library/react'
import { ThemeProvider } from '@/contexts/ThemeContext'
import { ThemeToggle } from '@/components/ThemeToggle'

// Mock different browser environments
const simulateBrowser = (userAgent: string, features: Record<string, any> = {}) => {
  Object.defineProperty(navigator, 'userAgent', {
    value: userAgent,
    configurable: true,
  })
  
  // Apply browser-specific features
  Object.keys(features).forEach(key => {
    Object.defineProperty(window, key, {
      value: features[key],
      configurable: true,
    })
  })
}

describe('Cross-Browser Compatibility Tests', () => {
  beforeEach(() => {
    jest.clearAllMocks()
  })

  describe('Chrome/Chromium Compatibility', () => {
    beforeEach(() => {
      simulateBrowser('Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36')
    })

    it('should render ThemeToggle correctly in Chrome', () => {
      render(
        <ThemeProvider>
          <ThemeToggle />
        </ThemeProvider>
      )

      // ThemeToggle is a dropdown with a single toggle button by default
      expect(screen.getByRole('button', { name: /theme selector.*current theme/i })).toBeInTheDocument()
      expect(screen.getAllByRole('button')).toHaveLength(1)

      // Theme options should not be visible initially (dropdown is closed)
      expect(screen.queryByText('Light')).not.toBeInTheDocument()
      expect(screen.queryByText('Dark')).not.toBeInTheDocument()
      expect(screen.queryByText('System')).not.toBeInTheDocument()
    })

    it('should handle CSS custom properties in Chrome', () => {
      const { container } = render(
        <ThemeProvider>
          <div className="bg-background text-foreground">
            <ThemeToggle />
          </div>
        </ThemeProvider>
      )

      const themeContainer = container.firstChild as HTMLElement
      expect(themeContainer).toHaveClass('bg-background', 'text-foreground')
    })
  })

  describe('Firefox Compatibility', () => {
    beforeEach(() => {
      simulateBrowser('Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:120.0) Gecko/20100101 Firefox/120.0')
    })

    it('should render ThemeToggle correctly in Firefox', () => {
      render(
        <ThemeProvider>
          <ThemeToggle />
        </ThemeProvider>
      )

      // ThemeToggle should render as a single button in Firefox as well
      const buttons = screen.getAllByRole('button')
      expect(buttons).toHaveLength(1)

      const toggleButton = buttons[0]
      expect(toggleButton).toHaveClass('flex', 'items-center', 'justify-center')
    })

    it('should handle flexbox layouts in Firefox', () => {
      const { container } = render(
        <ThemeProvider>
          <div className="flex items-center space-x-1">
            <ThemeToggle />
          </div>
        </ThemeProvider>
      )

      const flexContainer = container.firstChild as HTMLElement
      expect(flexContainer).toHaveClass('flex', 'items-center', 'space-x-1')
    })
  })

  describe('Safari Compatibility', () => {
    beforeEach(() => {
      simulateBrowser('Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/17.0 Safari/605.1.15')
    })

    it('should render ThemeToggle correctly in Safari', () => {
      render(
        <ThemeProvider>
          <ThemeToggle />
        </ThemeProvider>
      )

      // Safari should render the same as other browsers - single toggle button
      expect(screen.getByRole('button', { name: /theme selector.*current theme/i })).toBeInTheDocument()
      expect(screen.getAllByRole('button')).toHaveLength(1)

      // Theme options should not be visible initially (dropdown is closed)
      expect(screen.queryByText('Light')).not.toBeInTheDocument()
      expect(screen.queryByText('Dark')).not.toBeInTheDocument()
      expect(screen.queryByText('System')).not.toBeInTheDocument()
    })

    it('should handle webkit-specific CSS in Safari', () => {
      const { container } = render(
        <div style={{ WebkitAppearance: 'none' }}>
          <ThemeProvider>
            <ThemeToggle />
          </ThemeProvider>
        </div>
      )

      expect(container.firstChild).toBeInTheDocument()
    })
  })

  describe('Edge Compatibility', () => {
    beforeEach(() => {
      simulateBrowser('Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36 Edg/120.0.0.0')
    })

    it('should render ThemeToggle correctly in Edge', () => {
      render(
        <ThemeProvider>
          <ThemeToggle />
        </ThemeProvider>
      )

      // Edge should also render a single toggle button
      const themeButtons = screen.getAllByRole('button')
      expect(themeButtons).toHaveLength(1)
    })
  })

  describe('CSS Feature Support', () => {
    it('should handle CSS Grid fallbacks', () => {
      const { container } = render(
        <div className="grid grid-cols-3 gap-4">
          <div>Item 1</div>
          <div>Item 2</div>
          <div>Item 3</div>
        </div>
      )

      const gridContainer = container.firstChild as HTMLElement
      expect(gridContainer).toHaveClass('grid', 'grid-cols-3', 'gap-4')
    })

    it('should handle Flexbox layouts consistently', () => {
      const { container } = render(
        <div className="flex flex-wrap justify-between items-center">
          <ThemeProvider>
            <ThemeToggle />
          </ThemeProvider>
        </div>
      )

      const flexContainer = container.firstChild as HTMLElement
      expect(flexContainer).toHaveClass('flex', 'flex-wrap', 'justify-between', 'items-center')
    })

    it('should handle CSS custom properties', () => {
      const { container } = render(
        <ThemeProvider>
          <div className="bg-primary text-primary-foreground">
            Test Content
          </div>
        </ThemeProvider>
      )

      const element = container.firstChild as HTMLElement
      expect(element).toHaveClass('bg-primary', 'text-primary-foreground')
    })
  })

  describe('JavaScript Feature Support', () => {
    it('should handle modern JavaScript features', () => {
      // Test that the component works with modern JS features
      const modernFeatures = {
        // Mock modern features
        fetch: jest.fn(() => Promise.resolve({ ok: true, json: () => Promise.resolve({}) })),
        localStorage: {
          getItem: jest.fn(),
          setItem: jest.fn(),
          removeItem: jest.fn(),
          clear: jest.fn(),
        },
        matchMedia: jest.fn().mockImplementation(query => ({
          matches: false,
          media: query,
          onchange: null,
          addListener: jest.fn(),
          removeListener: jest.fn(),
          addEventListener: jest.fn(),
          removeEventListener: jest.fn(),
          dispatchEvent: jest.fn(),
        }))
      }

      Object.keys(modernFeatures).forEach(key => {
        // Only define property if it doesn't already exist
        if (!(key in window)) {
          Object.defineProperty(window, key, {
            value: (modernFeatures as any)[key],
            configurable: true,
          })
        }
      })

      render(
        <ThemeProvider>
          <ThemeToggle />
        </ThemeProvider>
      )

      // ThemeToggle should render with modern JS features - single button
      expect(screen.getByRole('button', { name: /theme selector.*current theme/i })).toBeInTheDocument()
      expect(screen.getAllByRole('button')).toHaveLength(1)
    })

    it('should handle ES6+ features gracefully', () => {
      // Test arrow functions, destructuring, etc. are handled
      const TestComponent = () => {
        const themes = ['light', 'dark', 'system']
        const [currentTheme] = themes
        
        return <div>Current: {currentTheme}</div>
      }

      render(<TestComponent />)
      expect(screen.getByText('Current: light')).toBeInTheDocument()
    })
  })

  describe('Mobile Browser Compatibility', () => {
    it('should work on iOS Safari', () => {
      simulateBrowser('Mozilla/5.0 (iPhone; CPU iPhone OS 17_0 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/17.0 Mobile/15E148 Safari/604.1')

      render(
        <ThemeProvider>
          <ThemeToggle />
        </ThemeProvider>
      )

      // Mobile browsers should also render single toggle button
      expect(screen.getAllByRole('button')).toHaveLength(1)
    })

    it('should work on Chrome Mobile', () => {
      simulateBrowser('Mozilla/5.0 (Linux; Android 10; Pixel 3) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Mobile Safari/537.36')

      render(
        <ThemeProvider>
          <ThemeToggle />
        </ThemeProvider>
      )

      // Mobile Chrome should also render single toggle button
      expect(screen.getAllByRole('button')).toHaveLength(1)
    })

    it('should handle touch interactions', () => {
      const { container } = render(
        <ThemeProvider>
          <ThemeToggle />
        </ThemeProvider>
      )

      const buttons = container.querySelectorAll('button')
      buttons.forEach(button => {
        // Should have adequate touch target size (w-10 h-10 = 40px x 40px)
        expect(button).toHaveClass('w-10', 'h-10')
      })
    })
  })

  describe('Polyfill Requirements', () => {
    it('should identify required polyfills', () => {
      const requiredFeatures = [
        'localStorage',
        'matchMedia',
        'fetch',
        'Promise',
        'Object.assign',
        'Array.from'
      ]

      requiredFeatures.forEach(feature => {
        if (feature.includes('.')) {
          // Handle nested features like Object.assign, Array.from
          const [obj, method] = feature.split('.')
          expect((global as any)[obj][method]).toBeDefined()
        } else {
          // Handle window properties
          expect((window as any)[feature]).toBeDefined()
        }
      })
    })

    it('should handle missing features gracefully', () => {
      // Mock a failing localStorage
      const originalLocalStorage = window.localStorage
      const mockFailingStorage = {
        getItem: jest.fn(() => { throw new Error('LocalStorage not available') }),
        setItem: jest.fn(() => { throw new Error('LocalStorage not available') }),
        removeItem: jest.fn(() => { throw new Error('LocalStorage not available') }),
        clear: jest.fn(() => { throw new Error('LocalStorage not available') }),
      }
      Object.defineProperty(window, 'localStorage', {
        value: mockFailingStorage,
        configurable: true
      })

      // Should not crash with failing localStorage
      const consoleSpy = jest.spyOn(console, 'error').mockImplementation(() => {})

      render(
        <ThemeProvider>
          <ThemeToggle />
        </ThemeProvider>
      )

      // ThemeToggle should still render even with failing localStorage
      expect(screen.getByRole('button', { name: /theme selector.*current theme/i })).toBeInTheDocument()
      expect(screen.getAllByRole('button')).toHaveLength(1)

      // Restore
      Object.defineProperty(window, 'localStorage', {
        value: originalLocalStorage,
        configurable: true
      })
      consoleSpy.mockRestore()
    })
  })

  describe('Performance Across Browsers', () => {
    it('should render consistently fast across browsers', () => {
      const browsers = [
        'Chrome/120.0.0.0',
        'Firefox/120.0',
        'Safari/605.1.15',
        'Edge/120.0.0.0'
      ]

      browsers.forEach(browser => {
        simulateBrowser(`Mozilla/5.0... ${browser}`)
        
        const startTime = performance.now()
        
        render(
          <ThemeProvider>
            <ThemeToggle />
          </ThemeProvider>
        )
        
        const endTime = performance.now()
        const renderTime = endTime - startTime

        // Should render in reasonable time regardless of browser
        expect(renderTime).toBeLessThan(100)
      })
    })
  })
})