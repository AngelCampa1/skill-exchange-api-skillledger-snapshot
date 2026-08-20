import { render, screen, fireEvent, act } from '@testing-library/react'
import '@testing-library/jest-dom'
import { ThemeProvider, useTheme } from '../ThemeContext'

function TestComponent() {
  const { theme, setTheme, resolvedTheme } = useTheme()

  return (
    <div>
      <span data-testid="current-theme">{theme}</span>
      <span data-testid="resolved-theme">{resolvedTheme}</span>
      <button onClick={() => setTheme('light')}>Set Light</button>
    </div>
  )
}

describe('ThemeContext', () => {
  it('provides light as the only theme', () => {
    render(
      <ThemeProvider>
        <TestComponent />
      </ThemeProvider>
    )

    expect(screen.getByTestId('current-theme')).toHaveTextContent('light')
    expect(screen.getByTestId('resolved-theme')).toHaveTextContent('light')
  })

  it('keeps light when setTheme is called', () => {
    render(
      <ThemeProvider>
        <TestComponent />
      </ThemeProvider>
    )

    act(() => {
      fireEvent.click(screen.getByText('Set Light'))
    })

    expect(screen.getByTestId('current-theme')).toHaveTextContent('light')
  })

  it('throws error when useTheme is used outside provider', () => {
    const consoleSpy = jest.spyOn(console, 'error').mockImplementation(() => {})

    expect(() => {
      render(<TestComponent />)
    }).toThrow('useTheme must be used within ThemeProvider')

    consoleSpy.mockRestore()
  })
})
