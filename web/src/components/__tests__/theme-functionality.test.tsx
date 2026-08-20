import React from 'react'
import { render, screen, fireEvent, waitFor, act } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { ThemeProvider, useTheme } from '@/contexts/ThemeContext'
import { ThemeToggle } from '@/components/ThemeToggle'

// Mock localStorage
const localStorageMock = {
  getItem: jest.fn(),
  setItem: jest.fn(),
  removeItem: jest.fn(),
  clear: jest.fn(),
}
Object.defineProperty(window, 'localStorage', {
  value: localStorageMock
})

// Helper component to test theme values
const ThemeDisplay: React.FC = () => {
  const { theme, resolvedTheme } = useTheme()
  return (
    <div>
      <div data-testid="current-theme">{theme}</div>
      <div data-testid="resolved-theme">{resolvedTheme}</div>
    </div>
  )
}

describe('Theme Functionality Tests', () => {
  beforeEach(() => {
    jest.clearAllMocks()
    localStorageMock.getItem.mockReturnValue(null)
    
    // Reset DOM classes
    document.documentElement.classList.remove('light')
    
    // Reset matchMedia mock
    Object.defineProperty(window, 'matchMedia', {
      writable: true,
      value: jest.fn().mockImplementation(query => ({
        matches: false,
        media: query,
        onchange: null,
        addListener: jest.fn(),
        removeListener: jest.fn(),
        addEventListener: jest.fn(),
        removeEventListener: jest.fn(),
        dispatchEvent: jest.fn(),
      })),
    })
  })

  describe('ThemeProvider', () => {
    it('should initialize with system theme by default', () => {
      render(
        <ThemeProvider>
          <ThemeDisplay />
        </ThemeProvider>
      )

      expect(screen.getByTestId('current-theme')).toHaveTextContent('system')
      expect(screen.getByTestId('resolved-theme')).toHaveTextContent('light')
    })

    it('should load saved theme from localStorage', () => {
      localStorageMock.getItem.mockReturnValue('dark')

      render(
        <ThemeProvider>
          <ThemeDisplay />
        </ThemeProvider>
      )

      expect(screen.getByTestId('current-theme')).toHaveTextContent('dark')
      expect(screen.getByTestId('resolved-theme')).toHaveTextContent('dark')
    })

    it('should resolve system theme based on media query', () => {
      // Mock dark system preference
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

      render(
        <ThemeProvider>
          <ThemeDisplay />
        </ThemeProvider>
      )

      expect(screen.getByTestId('current-theme')).toHaveTextContent('system')
      expect(screen.getByTestId('resolved-theme')).toHaveTextContent('dark')
    })

    it('should apply theme class to document element', () => {
      render(
        <ThemeProvider>
          <ThemeDisplay />
        </ThemeProvider>
      )

      expect(document.documentElement.classList.contains('light')).toBe(true)
    })

    it('should save theme changes to localStorage', async () => {
      const ThemeChanger: React.FC = () => {
        const { setTheme } = useTheme()
        return (
          <button onClick={() => setTheme('light')}>
            Switch to Dark
          </button>
        )
      }

      render(
        <ThemeProvider>
          <ThemeChanger />
          <ThemeDisplay />
        </ThemeProvider>
      )

      const button = screen.getByText('Switch to Dark')
      fireEvent.click(button)

      await waitFor(() => {
        expect(localStorageMock.setItem).toHaveBeenCalledWith('skillledger-theme', 'dark')
        expect(screen.getByTestId('current-theme')).toHaveTextContent('dark')
      })
    })

    it('should listen for system theme changes', () => {
      let mediaQueryListener: () => void = () => {}
      
      const mockAddEventListener = jest.fn((event, listener) => {
        if (event === 'change') {
          mediaQueryListener = listener
        }
      })

      Object.defineProperty(window, 'matchMedia', {
        writable: true,
        value: jest.fn().mockImplementation(query => ({
          matches: false,
          media: query,
          onchange: null,
          addListener: jest.fn(),
          removeListener: jest.fn(),
          addEventListener: mockAddEventListener,
          removeEventListener: jest.fn(),
          dispatchEvent: jest.fn(),
        })),
      })

      render(
        <ThemeProvider>
          <ThemeDisplay />
        </ThemeProvider>
      )

      expect(mockAddEventListener).toHaveBeenCalledWith('change', expect.any(Function))
    })
  })

  describe('ThemeToggle Component', () => {
    it('should render all theme options', async () => {
      const user = userEvent.setup()
      render(
        <ThemeProvider>
          <ThemeToggle />
        </ThemeProvider>
      )

      // First open the theme dropdown
      const toggleButton = screen.getByLabelText(/Theme selector.*Current theme/i)
      await user.click(toggleButton)

      expect(screen.getByLabelText(/Light theme.*/i)).toBeInTheDocument()
      expect(screen.getByLabelText(/Light Theme.*/i)).toBeInTheDocument()
      expect(screen.getByLabelText(/System theme.*/i)).toBeInTheDocument()
    })

    it('should highlight current theme', async () => {
      const user = userEvent.setup()
      localStorageMock.getItem.mockReturnValue('dark')

      render(
        <ThemeProvider>
          <ThemeToggle />
        </ThemeProvider>
      )

      // Open the theme dropdown
      const toggleButton = screen.getByLabelText(/Theme selector.*Current theme/i)
      await user.click(toggleButton)

      const darkButton = screen.getByLabelText(/Light Theme.*/i)
      expect(darkButton).toHaveClass('bg-primary', 'text-primary-foreground')
    })

    it('should switch themes when clicked', async () => {
      const user = userEvent.setup()

      render(
        <ThemeProvider>
          <ThemeToggle />
          <ThemeDisplay />
        </ThemeProvider>
      )

      // Open the theme dropdown first
      const toggleButton = screen.getByLabelText(/Theme selector.*Current theme/i)
      await user.click(toggleButton)

      const darkButton = screen.getByLabelText(/Light Theme.*/i)
      await user.click(darkButton)

      await waitFor(() => {
        expect(screen.getByTestId('current-theme')).toHaveTextContent('dark')
      })
    })

    it('should handle keyboard navigation', async () => {
      const user = userEvent.setup()

      render(
        <ThemeProvider>
          <ThemeToggle />
        </ThemeProvider>
      )

      // Open the theme dropdown first
      const toggleButton = screen.getByLabelText(/Theme selector.*Current theme/i)
      await user.click(toggleButton)

      // Verify dropdown is open and all theme options are visible and accessible
      expect(screen.getByLabelText(/Light theme.*/i)).toBeInTheDocument()
      expect(screen.getByLabelText(/Light Theme.*/i)).toBeInTheDocument()
      expect(screen.getByLabelText(/System theme.*/i)).toBeInTheDocument()

      // Current selected item (system) should be focused initially
      const systemButton = screen.getByLabelText(/System theme.*/i)
      expect(systemButton).toHaveFocus()

      // Verify each button has correct tabIndex for keyboard navigation
      // Selected button should have tabIndex 0, others -1
      expect(systemButton).toHaveAttribute('tabindex', '0')
      expect(screen.getByLabelText(/Light theme.*/i)).toHaveAttribute('tabindex', '-1')
      expect(screen.getByLabelText(/Light Theme.*/i)).toHaveAttribute('tabindex', '-1')

      // Verify the menu has correct ARIA attributes for keyboard navigation
      const menu = screen.getByRole('menu')
      expect(menu).toHaveAttribute('aria-label', 'Theme selection menu')
    })

    it('should activate theme with space key', async () => {
      const user = userEvent.setup()

      render(
        <ThemeProvider>
          <ThemeToggle />
          <ThemeDisplay />
        </ThemeProvider>
      )

      // Open the theme dropdown first
      const toggleButton = screen.getByLabelText(/Theme selector.*Current theme/i)
      await user.click(toggleButton)

      // Click the Light Theme button directly to test activation
      const darkButton = screen.getByLabelText(/Light Theme.*/i)
      await user.click(darkButton)

      await waitFor(() => {
        expect(screen.getByTestId('current-theme')).toHaveTextContent('dark')
      })
    })

    it('should activate theme with enter key', async () => {
      const user = userEvent.setup()

      render(
        <ThemeProvider>
          <ThemeToggle />
          <ThemeDisplay />
        </ThemeProvider>
      )

      // Open the theme dropdown first
      const toggleButton = screen.getByLabelText(/Theme selector.*Current theme/i)
      await user.click(toggleButton)

      // Click the light theme button directly to test activation
      const lightButton = screen.getByLabelText(/Light theme.*/i)
      await user.click(lightButton)

      await waitFor(() => {
        expect(screen.getByTestId('current-theme')).toHaveTextContent('light')
      })
    })
  })

  describe('Theme CSS Classes', () => {
    it('should apply light theme classes correctly', () => {
      render(
        <ThemeProvider>
          <div className="bg-background text-foreground">
            <div className="bg-card text-card-foreground">
              <button className="btn-primary">Primary Button</button>
            </div>
          </div>
        </ThemeProvider>
      )

      expect(document.documentElement.classList.contains('light')).toBe(true)
    })

    it('should apply Light Theme classes correctly', () => {
      localStorageMock.getItem.mockReturnValue('dark')

      render(
        <ThemeProvider>
          <div className="bg-background text-foreground">
            <div className="bg-card text-card-foreground">
              <button className="btn-primary">Primary Button</button>
            </div>
          </div>
        </ThemeProvider>
      )

      expect(document.documentElement.classList.contains('light')).toBe(true)
    })

    it('should switch CSS classes when theme changes', async () => {
      const user = userEvent.setup()

      render(
        <ThemeProvider>
          <ThemeToggle />
        </ThemeProvider>
      )

      // Initially light
      expect(document.documentElement.classList.contains('light')).toBe(true)

      // Open the theme dropdown first
      const toggleButton = screen.getByLabelText(/Theme selector.*Current theme/i)
      await user.click(toggleButton)

      // Switch to dark
      const darkButton = screen.getByLabelText(/Light Theme.*/i)
      await user.click(darkButton)

      await waitFor(() => {
        expect(document.documentElement.classList.contains('light')).toBe(true)
        expect(document.documentElement.classList.contains('light')).toBe(false)
      })
    })
  })

  describe('Theme Persistence', () => {
    it('should persist theme across page reloads', () => {
      localStorageMock.getItem.mockReturnValue('dark')

      const { unmount } = render(
        <ThemeProvider>
          <ThemeDisplay />
        </ThemeProvider>
      )

      expect(screen.getByTestId('current-theme')).toHaveTextContent('dark')

      unmount()

      // Simulate page reload
      render(
        <ThemeProvider>
          <ThemeDisplay />
        </ThemeProvider>
      )

      expect(screen.getByTestId('current-theme')).toHaveTextContent('dark')
    })

    it('should handle invalid theme values in localStorage', () => {
      localStorageMock.getItem.mockReturnValue('invalid-theme')

      render(
        <ThemeProvider>
          <ThemeDisplay />
        </ThemeProvider>
      )

      // Should fallback to system theme
      expect(screen.getByTestId('current-theme')).toHaveTextContent('system')
    })
  })

  describe('System Theme Detection', () => {
    it('should detect light system preference', () => {
      Object.defineProperty(window, 'matchMedia', {
        writable: true,
        value: jest.fn().mockImplementation(query => ({
          matches: query === '(prefers-light-scheme: light)' ||
                  (query === '(prefers-light-scheme: dark)' ? false : false),
          media: query,
          onchange: null,
          addListener: jest.fn(),
          removeListener: jest.fn(),
          addEventListener: jest.fn(),
          removeEventListener: jest.fn(),
          dispatchEvent: jest.fn(),
        })),
      })

      render(
        <ThemeProvider>
          <ThemeDisplay />
        </ThemeProvider>
      )

      expect(screen.getByTestId('resolved-theme')).toHaveTextContent('light')
    })

    it('should detect dark system preference', () => {
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

      render(
        <ThemeProvider>
          <ThemeDisplay />
        </ThemeProvider>
      )

      expect(screen.getByTestId('resolved-theme')).toHaveTextContent('dark')
    })

    it('should update resolved theme when system preference changes', async () => {
      let mediaQueryCallback: () => void = () => {}
      
      const mockAddEventListener = jest.fn((event, callback) => {
        if (event === 'change') {
          mediaQueryCallback = callback
        }
      })

      const mockMatchMedia = jest.fn().mockImplementation(query => ({
        matches: false, // Initially light
        media: query,
        onchange: null,
        addListener: jest.fn(),
        removeListener: jest.fn(),
        addEventListener: mockAddEventListener,
        removeEventListener: jest.fn(),
        dispatchEvent: jest.fn(),
      }))

      Object.defineProperty(window, 'matchMedia', {
        writable: true,
        value: mockMatchMedia,
      })

      render(
        <ThemeProvider>
          <ThemeDisplay />
        </ThemeProvider>
      )

      expect(screen.getByTestId('resolved-theme')).toHaveTextContent('light')

      // Simulate system theme change to dark
      mockMatchMedia.mockImplementation(query => ({
        matches: query === '(prefers-light-scheme: dark)',
        media: query,
        onchange: null,
        addListener: jest.fn(),
        removeListener: jest.fn(),
        addEventListener: mockAddEventListener,
        removeEventListener: jest.fn(),
        dispatchEvent: jest.fn(),
      }))

      // Trigger the callback
      await act(async () => {
        mediaQueryCallback()
      })

      await waitFor(() => {
        expect(screen.getByTestId('resolved-theme')).toHaveTextContent('dark')
      })
    })
  })

  describe('Edge Cases', () => {
    it('should handle missing localStorage gracefully', () => {
      // Mock localStorage to throw
      Object.defineProperty(window, 'localStorage', {
        value: {
          getItem: () => { throw new Error('Storage not available') },
          setItem: () => { throw new Error('Storage not available') },
          removeItem: jest.fn(),
          clear: jest.fn(),
        }
      })

      // Should not crash and fallback to system theme
      render(
        <ThemeProvider>
          <ThemeDisplay />
        </ThemeProvider>
      )

      expect(screen.getByTestId('current-theme')).toHaveTextContent('system')
    })

    it('should throw error when useTheme is used outside provider', () => {
      const consoleError = jest.spyOn(console, 'error').mockImplementation(() => {})
      
      const TestComponent = () => {
        useTheme()
        return <div>Test</div>
      }

      expect(() => {
        render(<TestComponent />)
      }).toThrow('useTheme must be used within a ThemeProvider')

      consoleError.mockRestore()
    })
  })
})
