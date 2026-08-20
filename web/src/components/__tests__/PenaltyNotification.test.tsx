import React from 'react'
import { render, screen, waitFor, fireEvent } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import PenaltyNotification from '../PenaltyNotification'

// Mock fetch globally
global.fetch = jest.fn()
const mockFetch = global.fetch as jest.Mock

describe('PenaltyNotification', () => {
  beforeEach(() => {
    jest.clearAllMocks()
  })

  // ============================================
  // Loading State (2 tests)
  // ============================================
  describe('Loading State', () => {
    it('should display loading skeleton while fetching data', () => {
      mockFetch.mockImplementation(() =>
        new Promise(() => {}) // Never resolves
      )

      render(<PenaltyNotification />)

      const skeleton = document.querySelector('.animate-pulse')
      expect(skeleton).toBeInTheDocument()
    })

    it('should have correct loading skeleton structure', () => {
      mockFetch.mockImplementation(() =>
        new Promise(() => {})
      )

      const { container } = render(<PenaltyNotification />)

      const skeletonElements = container.querySelectorAll('.bg-muted')
      expect(skeletonElements.length).toBeGreaterThan(0)
    })
  })

  // ============================================
  // Data Fetching (4 tests)
  // ============================================
  describe('Data Fetching', () => {
    it('should fetch sanctions and alerts on mount', async () => {
      mockFetch.mockResolvedValue({
        ok: true,
        json: async () => [],
      } as Response)

      render(<PenaltyNotification />)

      await waitFor(() => {
        expect(mockFetch).toHaveBeenCalledWith('/api/user/penalties/sanctions')
        expect(mockFetch).toHaveBeenCalledWith('/api/user/penalties/alerts')
      })
    })

    it('should handle successful API responses', async () => {
      const mockSanctions = [
        {
          id: 'sanction-1',
          sanctionType: 'Warning' as const,
          reason: 'Policy violation',
          isActive: true,
          createdAt: '2024-01-01T00:00:00Z',
          canAppeal: true,
          hasAppealed: false,
        },
      ]

      const mockAlerts = [
        {
          id: 'alert-1',
          message: 'Your account has been flagged',
          severity: 'Warning' as const,
          isRead: false,
          createdAt: '2024-01-01T00:00:00Z',
        },
      ]

      mockFetch.mockImplementation((url) => {
        if (url.includes('sanctions')) {
          return Promise.resolve({
            ok: true,
            json: async () => mockSanctions,
          } as Response)
        }
        if (url.includes('alerts')) {
          return Promise.resolve({
            ok: true,
            json: async () => mockAlerts,
          } as Response)
        }
        return Promise.resolve({
          ok: false,
          json: async () => ({}),
        } as Response)
      })

      render(<PenaltyNotification />)

      await waitFor(() => {
        expect(screen.getByText('Your account has been flagged')).toBeInTheDocument()
        expect(screen.getByText('Warning')).toBeInTheDocument()
      })
    })

    it('should handle API errors gracefully', async () => {
      mockFetch.mockRejectedValue(new Error('Network error'))

      render(<PenaltyNotification />)

      await waitFor(() => {
        expect(mockFetch).toHaveBeenCalled()
      })

      // Should not crash, just render empty (null)
      const fixedContainer = document.querySelector('.fixed.top-4')
      expect(fixedContainer).not.toBeInTheDocument()
    })

    it('should set loading to false after fetch completes', async () => {
      mockFetch.mockResolvedValue({
        ok: true,
        json: async () => [],
      } as Response)

      const { container } = render(<PenaltyNotification />)

      // Initially loading
      expect(container.querySelector('.animate-pulse')).toBeInTheDocument()

      // After fetch
      await waitFor(() => {
        expect(container.querySelector('.animate-pulse')).not.toBeInTheDocument()
      })
    })
  })

  // ============================================
  // Empty State (2 tests)
  // ============================================
  describe('Empty State', () => {
    it('should return null when no unread alerts', async () => {
      mockFetch.mockImplementation((url) => {
        if (url.includes('sanctions')) {
          return Promise.resolve({
            ok: true,
            json: async () => [],
          } as Response)
        }
        if (url.includes('alerts')) {
          return Promise.resolve({
            ok: true,
            json: async () => [
              {
                id: 'alert-1',
                message: 'Test',
                severity: 'Info',
                isRead: true, // Read alert
                createdAt: '2024-01-01T00:00:00Z',
              },
            ],
          } as Response)
        }
        return Promise.resolve({ ok: false } as Response)
      })

      const { container } = render(<PenaltyNotification />)

      await waitFor(() => {
        expect(container.firstChild).toBeNull()
      })
    })

    it('should return null when no active sanctions', async () => {
      mockFetch.mockImplementation((url) => {
        if (url.includes('sanctions')) {
          return Promise.resolve({
            ok: true,
            json: async () => [
              {
                id: 'sanction-1',
                sanctionType: 'Warning',
                reason: 'Test',
                isActive: false, // Inactive sanction
                createdAt: '2024-01-01T00:00:00Z',
                canAppeal: false,
                hasAppealed: false,
              },
            ],
          } as Response)
        }
        if (url.includes('alerts')) {
          return Promise.resolve({
            ok: true,
            json: async () => [],
          } as Response)
        }
        return Promise.resolve({ ok: false } as Response)
      })

      const { container } = render(<PenaltyNotification />)

      await waitFor(() => {
        expect(container.firstChild).toBeNull()
      })
    })
  })

  // ============================================
  // Alert Display (6 tests)
  // ============================================
  describe('Alert Display', () => {
    it('should display unread alerts', async () => {
      mockFetch.mockImplementation((url) => {
        if (url.includes('alerts')) {
          return Promise.resolve({
            ok: true,
            json: async () => [
              {
                id: 'alert-1',
                message: 'Account Notice Message',
                severity: 'Info',
                isRead: false,
                createdAt: '2024-01-01T10:00:00Z',
              },
            ],
          } as Response)
        }
        return Promise.resolve({
          ok: true,
          json: async () => [],
        } as Response)
      })

      render(<PenaltyNotification />)

      await waitFor(() => {
        expect(screen.getByText('Account Notice Message')).toBeInTheDocument()
      })
    })

    it('should show correct severity icon for Error alerts', async () => {
      mockFetch.mockImplementation((url) => {
        if (url.includes('alerts')) {
          return Promise.resolve({
            ok: true,
            json: async () => [
              {
                id: 'alert-1',
                message: 'Error message',
                severity: 'Error',
                isRead: false,
                createdAt: '2024-01-01T00:00:00Z',
              },
            ],
          } as Response)
        }
        return Promise.resolve({
          ok: true,
          json: async () => [],
        } as Response)
      })

      render(<PenaltyNotification />)

      await waitFor(() => {
        expect(screen.getByText(/⚠️/)).toBeInTheDocument()
      })
    })

    it('should show correct severity icon for Warning alerts', async () => {
      mockFetch.mockImplementation((url) => {
        if (url.includes('alerts')) {
          return Promise.resolve({
            ok: true,
            json: async () => [
              {
                id: 'alert-1',
                message: 'Warning message',
                severity: 'Warning',
                isRead: false,
                createdAt: '2024-01-01T00:00:00Z',
              },
            ],
          } as Response)
        }
        return Promise.resolve({
          ok: true,
          json: async () => [],
        } as Response)
      })

      render(<PenaltyNotification />)

      await waitFor(() => {
        expect(screen.getByText(/⚠️/)).toBeInTheDocument()
      })
    })

    it('should show correct severity icon for Info alerts', async () => {
      mockFetch.mockImplementation((url) => {
        if (url.includes('alerts')) {
          return Promise.resolve({
            ok: true,
            json: async () => [
              {
                id: 'alert-1',
                message: 'Info message',
                severity: 'Info',
                isRead: false,
                createdAt: '2024-01-01T00:00:00Z',
              },
            ],
          } as Response)
        }
        return Promise.resolve({
          ok: true,
          json: async () => [],
        } as Response)
      })

      render(<PenaltyNotification />)

      await waitFor(() => {
        expect(screen.getByText(/ℹ️/)).toBeInTheDocument()
      })
    })

    it('should display alert timestamp', async () => {
      const testDate = '2024-01-15T14:30:00Z'
      mockFetch.mockImplementation((url) => {
        if (url.includes('alerts')) {
          return Promise.resolve({
            ok: true,
            json: async () => [
              {
                id: 'alert-1',
                message: 'Test message',
                severity: 'Info',
                isRead: false,
                createdAt: testDate,
              },
            ],
          } as Response)
        }
        return Promise.resolve({
          ok: true,
          json: async () => [],
        } as Response)
      })

      render(<PenaltyNotification />)

      await waitFor(() => {
        // Check that date is rendered (format will vary by locale)
        const dateText = new Date(testDate).toLocaleString()
        expect(screen.getByText(dateText)).toBeInTheDocument()
      })
    })

    it('should filter out read alerts from display', async () => {
      mockFetch.mockImplementation((url) => {
        if (url.includes('alerts')) {
          return Promise.resolve({
            ok: true,
            json: async () => [
              {
                id: 'alert-1',
                message: 'Unread message',
                severity: 'Info',
                isRead: false,
                createdAt: '2024-01-01T00:00:00Z',
              },
              {
                id: 'alert-2',
                message: 'Read message',
                severity: 'Info',
                isRead: true,
                createdAt: '2024-01-01T00:00:00Z',
              },
            ],
          } as Response)
        }
        return Promise.resolve({
          ok: true,
          json: async () => [],
        } as Response)
      })

      render(<PenaltyNotification />)

      await waitFor(() => {
        expect(screen.getByText('Unread message')).toBeInTheDocument()
        expect(screen.queryByText('Read message')).not.toBeInTheDocument()
      })
    })
  })

  // ============================================
  // Alert Mark as Read (3 tests)
  // ============================================
  describe('Alert Mark as Read', () => {
    it('should call API when close button clicked', async () => {
      const user = userEvent.setup()
      mockFetch.mockImplementation((url) => {
        if (url.includes('alerts') && !url.includes('read')) {
          return Promise.resolve({
            ok: true,
            json: async () => [
              {
                id: 'alert-1',
                message: 'Test message',
                severity: 'Info',
                isRead: false,
                createdAt: '2024-01-01T00:00:00Z',
              },
            ],
          } as Response)
        }
        return Promise.resolve({
          ok: true,
          json: async () => [],
        } as Response)
      })

      render(<PenaltyNotification />)

      await waitFor(() => {
        expect(screen.getByText('Test message')).toBeInTheDocument()
      })

      const closeButton = screen.getByText('×')
      await user.click(closeButton)

      await waitFor(() => {
        expect(mockFetch).toHaveBeenCalledWith(
          '/api/user/penalties/alerts/alert-1/read',
          expect.objectContaining({ method: 'POST' })
        )
      })
    })

    it('should update alert state to read after clicking close', async () => {
      const user = userEvent.setup()
      mockFetch.mockImplementation((url) => {
        if (url.includes('alerts') && !url.includes('read')) {
          return Promise.resolve({
            ok: true,
            json: async () => [
              {
                id: 'alert-1',
                message: 'Test message',
                severity: 'Info',
                isRead: false,
                createdAt: '2024-01-01T00:00:00Z',
              },
            ],
          } as Response)
        }
        return Promise.resolve({
          ok: true,
          json: async () => [],
        } as Response)
      })

      const { container } = render(<PenaltyNotification />)

      await waitFor(() => {
        expect(screen.getByText('Test message')).toBeInTheDocument()
      })

      const closeButton = screen.getByText('×')
      await user.click(closeButton)

      await waitFor(() => {
        // Alert should disappear after being marked as read
        expect(container.firstChild).toBeNull()
      })
    })

    it('should handle mark as read API error gracefully', async () => {
      const user = userEvent.setup()
      mockFetch.mockImplementation((url) => {
        if (url.includes('alerts') && !url.includes('read')) {
          return Promise.resolve({
            ok: true,
            json: async () => [
              {
                id: 'alert-1',
                message: 'Test message',
                severity: 'Info',
                isRead: false,
                createdAt: '2024-01-01T00:00:00Z',
              },
            ],
          } as Response)
        }
        if (url.includes('read')) {
          return Promise.reject(new Error('Network error'))
        }
        return Promise.resolve({
          ok: true,
          json: async () => [],
        } as Response)
      })

      render(<PenaltyNotification />)

      await waitFor(() => {
        expect(screen.getByText('Test message')).toBeInTheDocument()
      })

      const closeButton = screen.getByText('×')
      await user.click(closeButton)

      // Should not crash
      await waitFor(() => {
        expect(mockFetch).toHaveBeenCalledWith(
          '/api/user/penalties/alerts/alert-1/read',
          expect.any(Object)
        )
      })
    })
  })

  // ============================================
  // Sanction Display (7 tests)
  // ============================================
  describe('Sanction Display', () => {
    it('should display active sanctions', async () => {
      mockFetch.mockImplementation((url) => {
        if (url.includes('sanctions')) {
          return Promise.resolve({
            ok: true,
            json: async () => [
              {
                id: 'sanction-1',
                sanctionType: 'Warning',
                reason: 'Spam posting',
                isActive: true,
                createdAt: '2024-01-01T00:00:00Z',
                canAppeal: true,
                hasAppealed: false,
              },
            ],
          } as Response)
        }
        return Promise.resolve({
          ok: true,
          json: async () => [],
        } as Response)
      })

      render(<PenaltyNotification />)

      await waitFor(() => {
        expect(screen.getByText(/Account Penalty/)).toBeInTheDocument()
        expect(screen.getByText('Warning')).toBeInTheDocument()
        expect(screen.getByText('Spam posting')).toBeInTheDocument()
      })
    })

    it('should show correct description for Warning sanction', async () => {
      mockFetch.mockImplementation((url) => {
        if (url.includes('sanctions')) {
          return Promise.resolve({
            ok: true,
            json: async () => [
              {
                id: 'sanction-1',
                sanctionType: 'Warning',
                reason: 'Test',
                isActive: true,
                createdAt: '2024-01-01T00:00:00Z',
                canAppeal: false,
                hasAppealed: false,
              },
            ],
          } as Response)
        }
        return Promise.resolve({
          ok: true,
          json: async () => [],
        } as Response)
      })

      render(<PenaltyNotification />)

      await waitFor(() => {
        expect(screen.getByText('You have received a warning for policy violations.')).toBeInTheDocument()
      })
    })

    it('should show correct description for TempSuspension sanction', async () => {
      mockFetch.mockImplementation((url) => {
        if (url.includes('sanctions')) {
          return Promise.resolve({
            ok: true,
            json: async () => [
              {
                id: 'sanction-1',
                sanctionType: 'TempSuspension',
                reason: 'Test',
                isActive: true,
                createdAt: '2024-01-01T00:00:00Z',
                canAppeal: false,
                hasAppealed: false,
              },
            ],
          } as Response)
        }
        return Promise.resolve({
          ok: true,
          json: async () => [],
        } as Response)
      })

      render(<PenaltyNotification />)

      await waitFor(() => {
        expect(screen.getByText('Your account has been temporarily suspended.')).toBeInTheDocument()
      })
    })

    it('should show correct description for PermBan sanction', async () => {
      mockFetch.mockImplementation((url) => {
        if (url.includes('sanctions')) {
          return Promise.resolve({
            ok: true,
            json: async () => [
              {
                id: 'sanction-1',
                sanctionType: 'PermBan',
                reason: 'Test',
                isActive: true,
                createdAt: '2024-01-01T00:00:00Z',
                canAppeal: false,
                hasAppealed: false,
              },
            ],
          } as Response)
        }
        return Promise.resolve({
          ok: true,
          json: async () => [],
        } as Response)
      })

      render(<PenaltyNotification />)

      await waitFor(() => {
        expect(screen.getByText('Your account has been permanently banned.')).toBeInTheDocument()
      })
    })

    it('should show correct description for ReviewRestriction sanction', async () => {
      mockFetch.mockImplementation((url) => {
        if (url.includes('sanctions')) {
          return Promise.resolve({
            ok: true,
            json: async () => [
              {
                id: 'sanction-1',
                sanctionType: 'ReviewRestriction',
                reason: 'Test',
                isActive: true,
                createdAt: '2024-01-01T00:00:00Z',
                canAppeal: false,
                hasAppealed: false,
              },
            ],
          } as Response)
        }
        return Promise.resolve({
          ok: true,
          json: async () => [],
        } as Response)
      })

      render(<PenaltyNotification />)

      await waitFor(() => {
        expect(screen.getByText('You are temporarily restricted from leaving reviews.')).toBeInTheDocument()
      })
    })

    it('should display expiration date when provided', async () => {
      const futureDate = new Date()
      futureDate.setDate(futureDate.getDate() + 3)

      mockFetch.mockImplementation((url) => {
        if (url.includes('sanctions')) {
          return Promise.resolve({
            ok: true,
            json: async () => [
              {
                id: 'sanction-1',
                sanctionType: 'TempSuspension',
                reason: 'Test',
                isActive: true,
                expiresAt: futureDate.toISOString(),
                createdAt: '2024-01-01T00:00:00Z',
                canAppeal: false,
                hasAppealed: false,
              },
            ],
          } as Response)
        }
        return Promise.resolve({
          ok: true,
          json: async () => [],
        } as Response)
      })

      render(<PenaltyNotification />)

      await waitFor(() => {
        expect(screen.getByText(/Expires in 3 days/)).toBeInTheDocument()
      })
    })

    it('should filter out inactive sanctions', async () => {
      mockFetch.mockImplementation((url) => {
        if (url.includes('sanctions')) {
          return Promise.resolve({
            ok: true,
            json: async () => [
              {
                id: 'sanction-1',
                sanctionType: 'Warning',
                reason: 'Active sanction',
                isActive: true,
                createdAt: '2024-01-01T00:00:00Z',
                canAppeal: false,
                hasAppealed: false,
              },
              {
                id: 'sanction-2',
                sanctionType: 'Warning',
                reason: 'Inactive sanction',
                isActive: false,
                createdAt: '2024-01-01T00:00:00Z',
                canAppeal: false,
                hasAppealed: false,
              },
            ],
          } as Response)
        }
        return Promise.resolve({
          ok: true,
          json: async () => [],
        } as Response)
      })

      render(<PenaltyNotification />)

      await waitFor(() => {
        expect(screen.getByText('Active sanction')).toBeInTheDocument()
        expect(screen.queryByText('Inactive sanction')).not.toBeInTheDocument()
      })
    })
  })

  // ============================================
  // Appeal System (8 tests)
  // ============================================
  describe('Appeal System', () => {
    it('should show Appeal This Penalty button when canAppeal and not appealed', async () => {
      mockFetch.mockImplementation((url) => {
        if (url.includes('sanctions')) {
          return Promise.resolve({
            ok: true,
            json: async () => [
              {
                id: 'sanction-1',
                sanctionType: 'Warning',
                reason: 'Test',
                isActive: true,
                createdAt: '2024-01-01T00:00:00Z',
                canAppeal: true,
                hasAppealed: false,
              },
            ],
          } as Response)
        }
        return Promise.resolve({
          ok: true,
          json: async () => [],
        } as Response)
      })

      render(<PenaltyNotification />)

      await waitFor(() => {
        expect(screen.getByText('Appeal This Penalty')).toBeInTheDocument()
      })
    })

    it('should not show appeal button when canAppeal is false', async () => {
      mockFetch.mockImplementation((url) => {
        if (url.includes('sanctions')) {
          return Promise.resolve({
            ok: true,
            json: async () => [
              {
                id: 'sanction-1',
                sanctionType: 'Warning',
                reason: 'Test',
                isActive: true,
                createdAt: '2024-01-01T00:00:00Z',
                canAppeal: false,
                hasAppealed: false,
              },
            ],
          } as Response)
        }
        return Promise.resolve({
          ok: true,
          json: async () => [],
        } as Response)
      })

      render(<PenaltyNotification />)

      await waitFor(() => {
        expect(screen.getByText('Warning')).toBeInTheDocument()
      })

      expect(screen.queryByText('Appeal This Penalty')).not.toBeInTheDocument()
    })

    it('should open appeal form when button clicked', async () => {
      const user = userEvent.setup()
      mockFetch.mockImplementation((url) => {
        if (url.includes('sanctions')) {
          return Promise.resolve({
            ok: true,
            json: async () => [
              {
                id: 'sanction-1',
                sanctionType: 'Warning',
                reason: 'Test',
                isActive: true,
                createdAt: '2024-01-01T00:00:00Z',
                canAppeal: true,
                hasAppealed: false,
              },
            ],
          } as Response)
        }
        return Promise.resolve({
          ok: true,
          json: async () => [],
        } as Response)
      })

      render(<PenaltyNotification />)

      await waitFor(() => {
        expect(screen.getByText('Appeal This Penalty')).toBeInTheDocument()
      })

      const appealButton = screen.getByText('Appeal This Penalty')
      await user.click(appealButton)

      await waitFor(() => {
        expect(screen.getByPlaceholderText(/Explain why you believe this penalty should be reversed/)).toBeInTheDocument()
        expect(screen.getByText('Submit Appeal')).toBeInTheDocument()
        expect(screen.getByText('Cancel')).toBeInTheDocument()
      })
    })

    it('should disable submit button when appeal text is empty', async () => {
      const user = userEvent.setup()
      mockFetch.mockImplementation((url) => {
        if (url.includes('sanctions')) {
          return Promise.resolve({
            ok: true,
            json: async () => [
              {
                id: 'sanction-1',
                sanctionType: 'Warning',
                reason: 'Test',
                isActive: true,
                createdAt: '2024-01-01T00:00:00Z',
                canAppeal: true,
                hasAppealed: false,
              },
            ],
          } as Response)
        }
        return Promise.resolve({
          ok: true,
          json: async () => [],
        } as Response)
      })

      render(<PenaltyNotification />)

      await waitFor(() => {
        expect(screen.getByText('Appeal This Penalty')).toBeInTheDocument()
      })

      const appealButton = screen.getByText('Appeal This Penalty')
      await user.click(appealButton)

      await waitFor(() => {
        const submitButton = screen.getByText('Submit Appeal')
        expect(submitButton).toBeDisabled()
      })
    })

    it('should call API with correct data when submitting appeal', async () => {
      const user = userEvent.setup()
      mockFetch.mockImplementation((url) => {
        if (url.includes('sanctions') && !url.includes('appeal')) {
          return Promise.resolve({
            ok: true,
            json: async () => [
              {
                id: 'sanction-1',
                sanctionType: 'Warning',
                reason: 'Test',
                isActive: true,
                createdAt: '2024-01-01T00:00:00Z',
                canAppeal: true,
                hasAppealed: false,
              },
            ],
          } as Response)
        }
        return Promise.resolve({
          ok: true,
          json: async () => [],
        } as Response)
      })

      render(<PenaltyNotification />)

      await waitFor(() => {
        expect(screen.getByText('Appeal This Penalty')).toBeInTheDocument()
      })

      const appealButton = screen.getByText('Appeal This Penalty')
      await user.click(appealButton)

      const textarea = screen.getByPlaceholderText(/Explain why you believe this penalty should be reversed/)
      fireEvent.change(textarea, { target: { value: 'I believe this was a mistake' } })

      const submitButton = screen.getByText('Submit Appeal')
      await user.click(submitButton)

      await waitFor(() => {
        expect(mockFetch).toHaveBeenCalledWith(
          '/api/user/penalties/sanctions/sanction-1/appeal',
          expect.objectContaining({
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ appealText: 'I believe this was a mistake' }),
          })
        )
      })
    })

    it('should update sanction state to hasAppealed after successful submit', async () => {
      const user = userEvent.setup()
      mockFetch.mockImplementation((url) => {
        if (url.includes('sanctions') && !url.includes('appeal')) {
          return Promise.resolve({
            ok: true,
            json: async () => [
              {
                id: 'sanction-1',
                sanctionType: 'Warning',
                reason: 'Test',
                isActive: true,
                createdAt: '2024-01-01T00:00:00Z',
                canAppeal: true,
                hasAppealed: false,
              },
            ],
          } as Response)
        }
        return Promise.resolve({
          ok: true,
          json: async () => [],
        } as Response)
      })

      render(<PenaltyNotification />)

      await waitFor(() => {
        expect(screen.getByText('Appeal This Penalty')).toBeInTheDocument()
      })

      const appealButton = screen.getByText('Appeal This Penalty')
      await user.click(appealButton)

      const textarea = screen.getByPlaceholderText(/Explain why you believe this penalty should be reversed/)
      fireEvent.change(textarea, { target: { value: 'Test appeal' } })

      const submitButton = screen.getByText('Submit Appeal')
      await user.click(submitButton)

      await waitFor(() => {
        expect(screen.getByText(/Appeal submitted - you will be notified of the outcome/)).toBeInTheDocument()
      })
    })

    it('should show Appeal submitted message when hasAppealed is true', async () => {
      mockFetch.mockImplementation((url) => {
        if (url.includes('sanctions')) {
          return Promise.resolve({
            ok: true,
            json: async () => [
              {
                id: 'sanction-1',
                sanctionType: 'Warning',
                reason: 'Test',
                isActive: true,
                createdAt: '2024-01-01T00:00:00Z',
                canAppeal: true,
                hasAppealed: true,
              },
            ],
          } as Response)
        }
        return Promise.resolve({
          ok: true,
          json: async () => [],
        } as Response)
      })

      render(<PenaltyNotification />)

      await waitFor(() => {
        expect(screen.getByText(/Appeal submitted - you will be notified of the outcome/)).toBeInTheDocument()
      })

      // Should not show appeal button
      expect(screen.queryByText('Appeal This Penalty')).not.toBeInTheDocument()
    })

    it('should close appeal form when cancel button clicked', async () => {
      const user = userEvent.setup()
      mockFetch.mockImplementation((url) => {
        if (url.includes('sanctions')) {
          return Promise.resolve({
            ok: true,
            json: async () => [
              {
                id: 'sanction-1',
                sanctionType: 'Warning',
                reason: 'Test',
                isActive: true,
                createdAt: '2024-01-01T00:00:00Z',
                canAppeal: true,
                hasAppealed: false,
              },
            ],
          } as Response)
        }
        return Promise.resolve({
          ok: true,
          json: async () => [],
        } as Response)
      })

      render(<PenaltyNotification />)

      await waitFor(() => {
        expect(screen.getByText('Appeal This Penalty')).toBeInTheDocument()
      })

      const appealButton = screen.getByText('Appeal This Penalty')
      await user.click(appealButton)

      await waitFor(() => {
        expect(screen.getByText('Cancel')).toBeInTheDocument()
      })

      const cancelButton = screen.getByText('Cancel')
      await user.click(cancelButton)

      await waitFor(() => {
        expect(screen.queryByPlaceholderText(/Explain why you believe this penalty should be reversed/)).not.toBeInTheDocument()
        expect(screen.getByText('Appeal This Penalty')).toBeInTheDocument()
      })
    })
  })

  // ============================================
  // Integration (2 tests)
  // ============================================
  describe('Integration', () => {
    it('should display both alerts and sanctions together', async () => {
      mockFetch.mockImplementation((url) => {
        if (url.includes('sanctions')) {
          return Promise.resolve({
            ok: true,
            json: async () => [
              {
                id: 'sanction-1',
                sanctionType: 'Warning',
                reason: 'Policy violation',
                isActive: true,
                createdAt: '2024-01-01T00:00:00Z',
                canAppeal: true,
                hasAppealed: false,
              },
            ],
          } as Response)
        }
        if (url.includes('alerts')) {
          return Promise.resolve({
            ok: true,
            json: async () => [
              {
                id: 'alert-1',
                message: 'Account flagged',
                severity: 'Warning',
                isRead: false,
                createdAt: '2024-01-01T00:00:00Z',
              },
            ],
          } as Response)
        }
        return Promise.resolve({ ok: false } as Response)
      })

      render(<PenaltyNotification />)

      await waitFor(() => {
        expect(screen.getByText('Account flagged')).toBeInTheDocument()
        expect(screen.getByText('Policy violation')).toBeInTheDocument()
      })
    })

    it('should handle complete appeal flow', async () => {
      const user = userEvent.setup()
      let appealSubmitted = false

      mockFetch.mockImplementation((url) => {
        if (url.includes('sanctions') && !url.includes('appeal')) {
          return Promise.resolve({
            ok: true,
            json: async () => [
              {
                id: 'sanction-1',
                sanctionType: 'TempSuspension',
                reason: 'Multiple violations',
                isActive: true,
                expiresAt: new Date(Date.now() + 7 * 24 * 60 * 60 * 1000).toISOString(),
                createdAt: '2024-01-01T00:00:00Z',
                canAppeal: true,
                hasAppealed: appealSubmitted,
              },
            ],
          } as Response)
        }
        if (url.includes('appeal')) {
          appealSubmitted = true
          return Promise.resolve({
            ok: true,
            json: async () => ({ success: true }),
          } as Response)
        }
        return Promise.resolve({
          ok: true,
          json: async () => [],
        } as Response)
      })

      render(<PenaltyNotification />)

      // Wait for data to load
      await waitFor(() => {
        expect(screen.getByText('Multiple violations')).toBeInTheDocument()
        expect(screen.getByText('Appeal This Penalty')).toBeInTheDocument()
      })

      // Click appeal button
      const appealButton = screen.getByText('Appeal This Penalty')
      await user.click(appealButton)

      // Fill in appeal text
      await waitFor(() => {
        const textarea = screen.getByPlaceholderText(/Explain why you believe this penalty should be reversed/)
        fireEvent.change(textarea, { target: { value: 'This suspension is unfair and I can provide evidence to support my case.' } })
      })

      // Submit appeal
      const submitButton = screen.getByText('Submit Appeal')
      await user.click(submitButton)

      // Verify appeal was submitted
      await waitFor(() => {
        expect(mockFetch).toHaveBeenCalledWith(
          '/api/user/penalties/sanctions/sanction-1/appeal',
          expect.objectContaining({
            method: 'POST',
          })
        )
      })

      // Check that success message appears
      await waitFor(() => {
        expect(screen.getByText(/Appeal submitted - you will be notified of the outcome/)).toBeInTheDocument()
      })
    })
  })
})
