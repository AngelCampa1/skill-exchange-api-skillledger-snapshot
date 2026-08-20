/**
 * Integration tests for Wallet Page
 * Week 16: Part A - Wallet & Marketplace Pages
 *
 * Testing Strategy:
 * - Mock only external dependencies: fetch, useAuth, useRouter
 * - Test real component behavior with actual UI interactions
 * - Verify API calls, state changes, and user feedback
 * - Follow implementation details from actual wallet/page.tsx
 *
 * Coverage Target: 85%+ line coverage
 */

import React from 'react';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import '@testing-library/jest-dom';
import WalletPage from '../page';
import { fetchWithAuth } from '@/utils/apiClient';

// Mock apiClient so fetchWithAuth can be controlled in tests
jest.mock('@/utils/apiClient', () => ({
  fetchWithAuth: jest.fn(),
}));

// Mock dependencies - IMPORTANT: Return stable object references to avoid infinite re-renders
const mockPush = jest.fn();
const stableRouterRef = { push: mockPush };

// Create a stable auth state that can be updated per-test
let mockAuthState = {
  user: { id: 'user-1', email: 'test@example.com', firstName: 'John' } as { id: string; email: string; firstName?: string } | null,
  isAuthenticated: true,
  isLoading: false,
};

jest.mock('next/navigation', () => ({
  useRouter: () => stableRouterRef, // Return stable reference directly
}));

jest.mock('@/contexts/AuthContext', () => ({
  useAuth: () => mockAuthState, // Return stable reference directly
}));

jest.mock('@/components/LogoutButton', () => {
  return function MockLogoutButton() {
    return <button>Logout</button>;
  };
});

jest.mock('@/components/ThemeToggle', () => ({
  ThemeToggle: () => <button>Toggle Theme</button>,
}));

jest.mock('@/utils/logger', () => ({
  logger: {
    error: jest.fn(),
  },
}));

jest.mock('@/utils/analytics', () => ({
  trackEvent: jest.fn(),
}));

describe('WalletPage Integration Tests', () => {
  beforeEach(() => {
    jest.clearAllMocks();
    global.fetch = jest.fn();
    (fetchWithAuth as jest.Mock).mockReset();

    // Reset auth state to default (authenticated user)
    mockAuthState.user = { id: 'user-1', email: 'test@example.com', firstName: 'John' };
    mockAuthState.isAuthenticated = true;
    mockAuthState.isLoading = false;

    // Mock window.location.href
    delete (window as any).location;
    (window as any).location = { href: '' };
  });

  afterEach(() => {
    jest.restoreAllMocks();
  });

  // ============================================================================
  // 1. Balance Display & History (8 tests)
  // ============================================================================

  describe('Balance Display & History', () => {
    test('displays current credit balance prominently', async () => {
      (global.fetch as jest.Mock).mockResolvedValueOnce({
        ok: true,
        json: async () => ({
          wallet: {
            currentBalance: 1250,
            totalEarned: 2000,
            totalSpent: 750,
            pendingBalance: 100,
          },
          recentTransactions: [],
        }),
      });

      render(<WalletPage />);

      await waitFor(() => {
        expect(screen.getByTestId('balance-amount')).toHaveTextContent('1250');
        expect(screen.getByTestId('balance-amount')).toHaveTextContent('credits');
      });
    });

    test('displays available vs. pending credits separately', async () => {
      (global.fetch as jest.Mock).mockResolvedValueOnce({
        ok: true,
        json: async () => ({
          wallet: {
            currentBalance: 1000,
            totalEarned: 1500,
            totalSpent: 500,
            pendingBalance: 250,
          },
          recentTransactions: [],
        }),
      });

      render(<WalletPage />);

      await waitFor(() => {
        expect(screen.getByText('1000')).toBeInTheDocument();
        expect(screen.getByText('250 credits')).toBeInTheDocument(); // Pending
      });
    });

    test('displays transaction history with correct formatting', async () => {
      (global.fetch as jest.Mock).mockResolvedValueOnce({
        ok: true,
        json: async () => ({
          wallet: {
            currentBalance: 1000,
            totalEarned: 1500,
            totalSpent: 500,
            pendingBalance: 0,
          },
          recentTransactions: [
            {
              transactionId: 'tx-1',
              type: 'credit',
              amount: 500,
              description: 'Credit package: Professional',
              createdAt: '2024-01-15T10:00:00Z',
              status: 'Completed',
              wasIncoming: true,
            },
            {
              transactionId: 'tx-2',
              type: 'debit',
              amount: 200,
              description: 'Project payment',
              createdAt: '2024-01-14T15:30:00Z',
              status: 'Completed',
              wasIncoming: false,
            },
          ],
        }),
      });

      render(<WalletPage />);

      await waitFor(() => {
        expect(screen.getByText('Credit package: Professional')).toBeInTheDocument();
        expect(screen.getByText('Project payment')).toBeInTheDocument();
        expect(screen.getByText('+500 credits')).toBeInTheDocument();
        expect(screen.getByText('-200 credits')).toBeInTheDocument();
      });
    });

    test('displays transaction type icons correctly (credit vs debit)', async () => {
      (global.fetch as jest.Mock).mockResolvedValueOnce({
        ok: true,
        json: async () => ({
          wallet: {
            currentBalance: 1000,
            totalEarned: 1500,
            totalSpent: 500,
            pendingBalance: 0,
          },
          recentTransactions: [
            {
              transactionId: 'tx-1',
              type: 'credit',
              amount: 500,
              description: 'Credit added',
              createdAt: '2024-01-15T10:00:00Z',
              status: 'Completed',
              wasIncoming: true,
            },
          ],
        }),
      });

      render(<WalletPage />);

      await waitFor(() => {
        // Transaction should have green success styling for credit
        const creditAmount = screen.getByText('+500 credits');
        expect(creditAmount).toHaveClass('text-success');
      });
    });

    test('displays date formatting for transactions', async () => {
      (global.fetch as jest.Mock).mockResolvedValueOnce({
        ok: true,
        json: async () => ({
          wallet: {
            currentBalance: 1000,
            totalEarned: 1500,
            totalSpent: 500,
            pendingBalance: 0,
          },
          recentTransactions: [
            {
              transactionId: 'tx-1',
              type: 'credit',
              amount: 500,
              description: 'Test transaction',
              createdAt: '2024-01-15T10:00:00Z',
              status: 'Completed',
              wasIncoming: true,
            },
          ],
        }),
      });

      render(<WalletPage />);

      await waitFor(() => {
        // Date should be formatted as "Jan 15, 2024"
        expect(screen.getByText(/Jan 15, 2024/i)).toBeInTheDocument();
      });
    });

    test('displays total earned credits', async () => {
      (global.fetch as jest.Mock).mockResolvedValueOnce({
        ok: true,
        json: async () => ({
          wallet: {
            currentBalance: 1000,
            totalEarned: 3500,
            totalSpent: 2500,
            pendingBalance: 0,
          },
          recentTransactions: [],
        }),
      });

      render(<WalletPage />);

      await waitFor(() => {
        expect(screen.getByText('3,500 credits')).toBeInTheDocument(); // toLocaleString formatting
      });
    });

    test('displays total spent credits', async () => {
      (global.fetch as jest.Mock).mockResolvedValueOnce({
        ok: true,
        json: async () => ({
          wallet: {
            currentBalance: 1000,
            totalEarned: 3500,
            totalSpent: 2500,
            pendingBalance: 0,
          },
          recentTransactions: [],
        }),
      });

      render(<WalletPage />);

      await waitFor(() => {
        expect(screen.getByText('2,500 credits')).toBeInTheDocument();
      });
    });

    test('displays empty state when no transactions exist', async () => {
      (global.fetch as jest.Mock).mockResolvedValueOnce({
        ok: true,
        json: async () => ({
          wallet: {
            currentBalance: 0,
            totalEarned: 0,
            totalSpent: 0,
            pendingBalance: 0,
          },
          recentTransactions: [],
        }),
      });

      render(<WalletPage />);

      await waitFor(() => {
        expect(screen.getByText('No transactions yet')).toBeInTheDocument();
        expect(screen.getByText('Your transaction history will appear here')).toBeInTheDocument();
      });
    });
  });

  // ============================================================================
  // 2. Credit Purchase Flow (8 tests)
  // ============================================================================

  describe('Credit Purchase Flow', () => {
    test('"Add Credits" button opens purchase modal', async () => {
      (global.fetch as jest.Mock).mockResolvedValueOnce({
        ok: true,
        json: async () => ({
          wallet: { currentBalance: 1000, totalEarned: 1000, totalSpent: 0, pendingBalance: 0 },
          recentTransactions: [],
        }),
      });

      render(<WalletPage />);

      await waitFor(() => {
        const addCreditsBtn = screen.getByText('Add Credits');
        fireEvent.click(addCreditsBtn);
      });

      await waitFor(() => {
        expect(screen.getByText('Starter')).toBeInTheDocument();
        expect(screen.getByText('Professional')).toBeInTheDocument();
        expect(screen.getByText('Business')).toBeInTheDocument();
        expect(screen.getByText('Enterprise')).toBeInTheDocument();
      });
    });

    test('credit packages displayed with correct amounts (100, 500, 1000, 5000)', async () => {
      (global.fetch as jest.Mock).mockResolvedValueOnce({
        ok: true,
        json: async () => ({
          wallet: { currentBalance: 1000, totalEarned: 1000, totalSpent: 0, pendingBalance: 0 },
          recentTransactions: [],
        }),
      });

      render(<WalletPage />);

      await waitFor(() => {
        fireEvent.click(screen.getByText('Add Credits'));
      });

      await waitFor(() => {
        expect(screen.getByText('100 credits')).toBeInTheDocument();
        expect(screen.getByText('500 credits')).toBeInTheDocument();
        expect(screen.getByText('1000 credits')).toBeInTheDocument();
        expect(screen.getByText('5000 credits')).toBeInTheDocument();
      });
    });

    test('package selection highlights selected package', async () => {
      (global.fetch as jest.Mock).mockResolvedValueOnce({
        ok: true,
        json: async () => ({
          wallet: { currentBalance: 1000, totalEarned: 1000, totalSpent: 0, pendingBalance: 0 },
          recentTransactions: [],
        }),
      });

      render(<WalletPage />);

      await waitFor(() => {
        fireEvent.click(screen.getByText('Add Credits'));
      });

      await waitFor(() => {
        const professionalPackage = screen.getByText('Professional').closest('button');
        fireEvent.click(professionalPackage!);
      });

      await waitFor(() => {
        const professionalButton = screen.getByText('Professional').closest('button');
        expect(professionalButton).toHaveClass('border-primary');
        expect(professionalButton).toHaveClass('bg-primary/10');
      });
    });

    test('add credits validation: requires package selection', async () => {
      (global.fetch as jest.Mock).mockResolvedValueOnce({
        ok: true,
        json: async () => ({
          wallet: { currentBalance: 1000, totalEarned: 1000, totalSpent: 0, pendingBalance: 0 },
          recentTransactions: [],
        }),
      });

      render(<WalletPage />);

      await waitFor(() => {
        fireEvent.click(screen.getByText('Add Credits'));
      });

      await waitFor(() => {
        const addButtons = screen.getAllByText('Add Credits');
        const modalAddButton = addButtons[addButtons.length - 1]; // Modal button (last one)
        expect(modalAddButton).toBeDisabled(); // Should be disabled without selection
      });
    });

    test('add credits success updates balance immediately (optimistic UI)', async () => {
      // Initial wallet data fetch (GET uses global.fetch directly)
      (global.fetch as jest.Mock).mockResolvedValueOnce({
        ok: true,
        json: async () => ({
          wallet: { currentBalance: 1000, totalEarned: 1000, totalSpent: 0, pendingBalance: 0 },
          recentTransactions: [],
        }),
      });

      // POST to add-credits uses fetchWithAuth (returns parsed JSON directly)
      (fetchWithAuth as jest.Mock).mockResolvedValueOnce({ success: true });

      // Refetch wallet data after success (GET uses global.fetch directly)
      (global.fetch as jest.Mock).mockResolvedValueOnce({
        ok: true,
        json: async () => ({
          wallet: { currentBalance: 1500, totalEarned: 1500, totalSpent: 0, pendingBalance: 0 },
          recentTransactions: [],
        }),
      });

      render(<WalletPage />);

      await waitFor(() => {
        fireEvent.click(screen.getByText('Add Credits'));
      });

      await waitFor(() => {
        const professionalPackage = screen.getByText('Professional').closest('button');
        fireEvent.click(professionalPackage!);
      });

      const addButtons = screen.getAllByText('Add Credits');
      const modalAddButton = addButtons[addButtons.length - 1];
      fireEvent.click(modalAddButton);

      // Wait for fetchWithAuth to be called for the POST
      await waitFor(
        () => {
          expect(fetchWithAuth).toHaveBeenCalledWith(
            '/api/credit-wallet/add-credits',
            expect.objectContaining({ method: 'POST' })
          );
        },
        { timeout: 5000 }
      );

      // Balance should be updated after refetch
      await waitFor(
        () => {
          expect(screen.getByTestId('balance-amount')).toHaveTextContent('1500');
        },
        { timeout: 2000 }
      );
    });

    test('add credits POSTs to /api/credit-wallet/add-credits with correct payload', async () => {
      // Initial wallet data fetch
      (global.fetch as jest.Mock).mockResolvedValueOnce({
        ok: true,
        json: async () => ({
          wallet: { currentBalance: 1000, totalEarned: 1000, totalSpent: 0, pendingBalance: 0 },
          recentTransactions: [],
        }),
      });

      // POST to add-credits via fetchWithAuth
      (fetchWithAuth as jest.Mock).mockResolvedValueOnce({ success: true });

      // Refetch wallet data after success
      (global.fetch as jest.Mock).mockResolvedValueOnce({
        ok: true,
        json: async () => ({
          wallet: { currentBalance: 1500, totalEarned: 1500, totalSpent: 0, pendingBalance: 0 },
          recentTransactions: [],
        }),
      });

      render(<WalletPage />);

      await waitFor(() => {
        fireEvent.click(screen.getByText('Add Credits'));
      });

      await waitFor(() => {
        const professionalPackage = screen.getByText('Professional').closest('button');
        fireEvent.click(professionalPackage!);
      });

      const addButtons = screen.getAllByText('Add Credits');
      const modalAddButton = addButtons[addButtons.length - 1];
      fireEvent.click(modalAddButton);

      await waitFor(
        () => {
          expect(fetchWithAuth).toHaveBeenCalledWith(
            '/api/credit-wallet/add-credits',
            expect.objectContaining({
              method: 'POST',
              body: JSON.stringify({
                amount: 500,
                description: 'Credit package: Professional',
                packageId: 'professional',
              }),
            })
          );
        },
        { timeout: 2000 }
      );
    });

    test('add credits refetches wallet data after success', async () => {
      // Initial wallet data fetch (1st global.fetch call)
      (global.fetch as jest.Mock).mockResolvedValueOnce({
        ok: true,
        json: async () => ({
          wallet: { currentBalance: 1000, totalEarned: 1000, totalSpent: 0, pendingBalance: 0 },
          recentTransactions: [],
        }),
      });

      // POST to add-credits via fetchWithAuth
      (fetchWithAuth as jest.Mock).mockResolvedValueOnce({ success: true });

      // Refetch wallet data after success (2nd global.fetch call)
      (global.fetch as jest.Mock).mockResolvedValueOnce({
        ok: true,
        json: async () => ({
          wallet: { currentBalance: 2000, totalEarned: 2000, totalSpent: 0, pendingBalance: 0 },
          recentTransactions: [],
        }),
      });

      render(<WalletPage />);

      await waitFor(() => {
        fireEvent.click(screen.getByText('Add Credits'));
      });

      await waitFor(() => {
        const businessPackage = screen.getByText('Business').closest('button');
        fireEvent.click(businessPackage!);
      });

      const addButtons = screen.getAllByText('Add Credits');
      const modalAddButton = addButtons[addButtons.length - 1];
      fireEvent.click(modalAddButton);

      await waitFor(
        () => {
          // fetchWithAuth called once for add-credits POST
          expect(fetchWithAuth).toHaveBeenCalledWith(
            '/api/credit-wallet/add-credits',
            expect.objectContaining({ method: 'POST' })
          );
          // global.fetch called twice: initial load + refetch
          expect(global.fetch).toHaveBeenCalledTimes(2);
          expect(global.fetch).toHaveBeenLastCalledWith('/api/credit-wallet', expect.any(Object));
        },
        { timeout: 5000 }
      );
    });

    test('add credits error shows error message', async () => {
      // Initial wallet data fetch
      (global.fetch as jest.Mock).mockResolvedValueOnce({
        ok: true,
        json: async () => ({
          wallet: { currentBalance: 1000, totalEarned: 1000, totalSpent: 0, pendingBalance: 0 },
          recentTransactions: [],
        }),
      });

      // POST to add-credits via fetchWithAuth - throws on error
      (fetchWithAuth as jest.Mock).mockRejectedValueOnce(new Error('Insufficient funds'));

      render(<WalletPage />);

      await waitFor(() => {
        fireEvent.click(screen.getByText('Add Credits'));
      });

      await waitFor(() => {
        const starterPackage = screen.getByText('Starter').closest('button');
        fireEvent.click(starterPackage!);
      });

      const addButtons = screen.getAllByText('Add Credits');
      const modalAddButton = addButtons[addButtons.length - 1];
      fireEvent.click(modalAddButton);

      await waitFor(
        () => {
          expect(fetchWithAuth).toHaveBeenCalledWith(
            '/api/credit-wallet/add-credits',
            expect.objectContaining({ method: 'POST' })
          );
        },
        { timeout: 2000 }
      );

      // Error message should appear
      await waitFor(
        () => {
          const errorText = screen.queryByText('Insufficient funds');
          expect(errorText).toBeInTheDocument();
        },
        { timeout: 3000 }
      );
    });
  });

  // ============================================================================
  // 3. Error Handling (4 tests)
  // ============================================================================

  describe('Error Handling', () => {
    test('API error sets balance to 0 and shows fallback state', async () => {
      (global.fetch as jest.Mock).mockRejectedValueOnce(new Error('Network error'));

      render(<WalletPage />);

      await waitFor(() => {
        expect(screen.getByTestId('balance-amount')).toHaveTextContent('0');
        expect(screen.getByText('No transactions yet')).toBeInTheDocument();
      });
    });

    test('404 response treats as new user with default values', async () => {
      (global.fetch as jest.Mock).mockResolvedValueOnce({
        ok: false,
        status: 404,
      });

      render(<WalletPage />);

      await waitFor(() => {
        expect(screen.getByTestId('balance-amount')).toHaveTextContent('0');
        expect(screen.getByText('No transactions yet')).toBeInTheDocument();
      });
    });

    test('non-404 error response shows fallback state', async () => {
      (global.fetch as jest.Mock).mockResolvedValueOnce({
        ok: false,
        status: 500,
      });

      render(<WalletPage />);

      await waitFor(() => {
        expect(screen.getByTestId('balance-amount')).toHaveTextContent('0');
        expect(screen.getByText('No transactions yet')).toBeInTheDocument();
      });
    });

    test('network error during add credits shows error message', async () => {
      // Initial wallet data fetch
      (global.fetch as jest.Mock).mockResolvedValueOnce({
        ok: true,
        json: async () => ({
          wallet: { currentBalance: 1000, totalEarned: 1000, totalSpent: 0, pendingBalance: 0 },
          recentTransactions: [],
        }),
      });

      // POST to add-credits via fetchWithAuth - throws network error
      (fetchWithAuth as jest.Mock).mockRejectedValueOnce(new Error('Network error. Please try again.'));

      render(<WalletPage />);

      await waitFor(() => {
        fireEvent.click(screen.getByText('Add Credits'));
      });

      await waitFor(() => {
        const starterPackage = screen.getByText('Starter').closest('button');
        fireEvent.click(starterPackage!);
      });

      const addButtons = screen.getAllByText('Add Credits');
      const modalAddButton = addButtons[addButtons.length - 1];
      fireEvent.click(modalAddButton);

      await waitFor(
        () => {
          expect(screen.getByText('Network error. Please try again.')).toBeInTheDocument();
        },
        { timeout: 2000 }
      );
    });
  });

  // ============================================================================
  // 4. Loading & Auth States (3 tests)
  // ============================================================================

  describe('Loading & Auth States', () => {
    test('shows loading spinner while fetching wallet data', () => {
      // Override auth state for this test
      mockAuthState.user = null;
      mockAuthState.isAuthenticated = false;
      mockAuthState.isLoading = true;

      (global.fetch as jest.Mock).mockImplementationOnce(
        () => new Promise(() => {}) // Never resolves
      );

      render(<WalletPage />);

      expect(screen.getByText('Loading your wallet...')).toBeInTheDocument();
    });

    test('redirects to login when not authenticated', async () => {
      // Override auth state for this test
      mockAuthState.user = null;
      mockAuthState.isAuthenticated = false;
      mockAuthState.isLoading = false;

      (global.fetch as jest.Mock).mockResolvedValueOnce({
        ok: true,
      });

      render(<WalletPage />);

      await waitFor(() => {
        expect(global.fetch).toHaveBeenCalledWith(
          '/api/auth/logout',
          expect.objectContaining({ method: 'POST' })
        );
      });

      await waitFor(() => {
        expect(window.location.href).toBe('/login');
      });
    });

    test('displays user firstName in welcome message', async () => {
      // Default mockAuthState already has firstName: 'John'
      (global.fetch as jest.Mock).mockResolvedValueOnce({
        ok: true,
        json: async () => ({
          wallet: { currentBalance: 1000, totalEarned: 1000, totalSpent: 0, pendingBalance: 0 },
          recentTransactions: [],
        }),
      });

      render(<WalletPage />);

      await waitFor(() => {
        expect(screen.getByText(/Welcome back/i)).toBeInTheDocument();
        expect(screen.getByText('John')).toBeInTheDocument();
      });
    });
  });
});
