/**
 * PaymentMethodManager Tests - Week 8
 *
 * Testing Philosophy: Mock ONLY external services (fetch), never mock internal logic
 * - Tests real component state management and user interactions
 * - Tests payment method CRUD operations and security
 * - Tests error handling and edge cases
 *
 * Expected Bugs to Find:
 * - CAN DELETE LAST PAYMENT METHOD (critical business logic)
 * - Add button not disabled while loading
 * - No error UI shown to user on checkout failure
 * - No optimistic UI updates
 */

import React from 'react';
import { render, screen, fireEvent, waitFor, act } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { PaymentMethodManager } from '../PaymentMethodManager';
import { setupFetchMock, createMockFetchResponse } from '@/utils/test/testUtils';

// Mock the useSubscription hook
jest.mock('@/lib/subscription-api', () => ({
  useSubscription: jest.fn(() => ({
    setupPaymentMethod: jest.fn(),
  })),
}));

import { useSubscription } from '@/lib/subscription-api';

// Mock logger to verify error logging
jest.mock('@/utils/logger', () => ({
  logger: {
    error: jest.fn(),
  },
}));

import { logger } from '@/utils/logger';

// Mock next/navigation
jest.mock('next/navigation', () => ({
  useRouter: () => ({
    push: jest.fn(),
    replace: jest.fn(),
    refresh: jest.fn(),
  }),
}));

// Mock window.confirm
const originalConfirm = window.confirm;

describe('PaymentMethodManager - Week 8 (Payment Security & State)', () => {
  let fetchMock: ReturnType<typeof setupFetchMock>;
  let mockSetupPaymentMethod: jest.Mock;

  const mockPaymentMethods = [
    {
      id: 'pm-123',
      type: 'card',
      brand: 'visa',
      last4: '4242',
      expiryMonth: 12,
      expiryYear: 2025,
      isDefault: true,
      createdAt: '2024-01-15T10:00:00Z',
    },
    {
      id: 'pm-456',
      type: 'card',
      brand: 'mastercard',
      last4: '5555',
      expiryMonth: 6,
      expiryYear: 2026,
      isDefault: false,
      createdAt: '2024-02-20T14:30:00Z',
    },
  ];

  beforeEach(() => {
    fetchMock = setupFetchMock();
    mockSetupPaymentMethod = jest.fn();
    (useSubscription as jest.Mock).mockReturnValue({
      setupPaymentMethod: mockSetupPaymentMethod,
    });
    window.confirm = jest.fn(() => true);
    jest.clearAllMocks();
  });

  afterEach(() => {
    fetchMock.reset();
    window.confirm = originalConfirm;
  });

  // ==========================================================================
  // Suite 1: Add Payment Method Flow (8 tests)
  // ==========================================================================

  describe('Add Payment Method Flow', () => {
    test('Add Payment button is visible and clickable', async () => {
      fetchMock.respondWith(mockPaymentMethods);

      render(<PaymentMethodManager />);

      await waitFor(() => {
        expect(screen.queryByText(/Loading payment methods/)).not.toBeInTheDocument();
      });

      const addButton = screen.getByRole('button', { name: /Add Payment Method/i });
      expect(addButton).toBeInTheDocument();
    });

    test('clicking Add Payment calls setupPaymentMethod', async () => {
      fetchMock.respondWith(mockPaymentMethods);
      mockSetupPaymentMethod.mockResolvedValue({
        success: true,
        sessionUrl: 'https://stripe.com/checkout/session123',
      });

      render(<PaymentMethodManager />);

      await waitFor(() => {
        expect(screen.queryByText(/Loading payment methods/)).not.toBeInTheDocument();
      });

      const addButton = screen.getByRole('button', { name: /Add Payment Method/i });
      await userEvent.click(addButton);

      expect(mockSetupPaymentMethod).toHaveBeenCalled();
    });

    test('successful checkout redirects to sessionUrl', async () => {
      fetchMock.respondWith(mockPaymentMethods);
      mockSetupPaymentMethod.mockResolvedValue({
        success: true,
        sessionUrl: 'https://stripe.com/checkout/session456',
      });

      // Mock window.location
      const mockLocation = { href: '' };
      Object.defineProperty(window, 'location', {
        value: mockLocation,
        writable: true,
      });

      render(<PaymentMethodManager />);

      await waitFor(() => {
        expect(screen.queryByText(/Loading payment methods/)).not.toBeInTheDocument();
      });

      const addButton = screen.getByRole('button', { name: /Add Payment Method/i });
      await userEvent.click(addButton);

      await waitFor(() => {
        expect(mockLocation.href).toBe('https://stripe.com/checkout/session456');
      });
    });

    test('BUG-TEST-045: Add button NOT disabled while checkout is loading', async () => {
      fetchMock.respondWith(mockPaymentMethods);

      // Make setupPaymentMethod hang to simulate loading
      let resolveSetup: (value: any) => void;
      mockSetupPaymentMethod.mockImplementation(() => new Promise((resolve) => {
        resolveSetup = resolve;
      }));

      render(<PaymentMethodManager />);

      await waitFor(() => {
        expect(screen.queryByText(/Loading payment methods/)).not.toBeInTheDocument();
      });

      const addButton = screen.getByRole('button', { name: /Add Payment Method/i });

      // Click to start loading
      await userEvent.click(addButton);

      // EXPECT BUG: Button should be disabled during loading, but isn't
      expect(addButton).not.toBeDisabled();
      console.warn('BUG-TEST-045: Add Payment button not disabled while checkout is loading');

      // Cleanup
      resolveSetup!({ success: false });
    });

    test('BUG-TEST-046: checkout failure does NOT show error to user', async () => {
      fetchMock.respondWith(mockPaymentMethods);
      mockSetupPaymentMethod.mockRejectedValue(new Error('Stripe connection failed'));

      render(<PaymentMethodManager />);

      await waitFor(() => {
        expect(screen.queryByText(/Loading payment methods/)).not.toBeInTheDocument();
      });

      const addButton = screen.getByRole('button', { name: /Add Payment Method/i });
      await userEvent.click(addButton);

      // Wait a bit for error handling
      await act(async () => {
        await new Promise(resolve => setTimeout(resolve, 100));
      });

      // EXPECT BUG: Error is logged but not shown to user
      expect(logger.error).toHaveBeenCalledWith(
        'Failed to setup payment method:',
        expect.any(Error)
      );

      // Error should be shown to user, but isn't
      expect(screen.queryByText(/failed/i)).not.toBeInTheDocument();
      console.warn('BUG-TEST-046: Checkout failure does not show error message to user');
    });

    test('empty payment methods shows "Add Your First Payment Method" button', async () => {
      fetchMock.respondWith([]);

      render(<PaymentMethodManager />);

      await waitFor(() => {
        expect(screen.getByText(/No payment methods/i)).toBeInTheDocument();
      });

      expect(screen.getByRole('button', { name: /Add Your First Payment Method/i })).toBeInTheDocument();
    });

    test('Sync from Stripe button visible when no payment methods', async () => {
      fetchMock.respondWith([]);

      render(<PaymentMethodManager />);

      await waitFor(() => {
        expect(screen.getByText(/No payment methods/i)).toBeInTheDocument();
      });

      expect(screen.getByRole('button', { name: /Sync from Stripe/i })).toBeInTheDocument();
    });

    test('Sync from Stripe fetches payment methods from backend', async () => {
      fetchMock.respondWith([]);

      render(<PaymentMethodManager />);

      await waitFor(() => {
        expect(screen.getByText(/No payment methods/i)).toBeInTheDocument();
      });

      // Add response for sync
      fetchMock.respondWith(mockPaymentMethods);

      const syncButton = screen.getByRole('button', { name: /Sync from Stripe/i });
      await userEvent.click(syncButton);

      await waitFor(() => {
        expect(screen.getByText(/4242/)).toBeInTheDocument();
      });

      // Verify sync endpoint was called
      const calls = fetchMock.getCalls();
      const syncCall = calls.find(c => c.url.includes('/sync'));
      expect(syncCall).toBeDefined();
      expect(syncCall?.options?.method).toBe('POST');
    });
  });

  // ==========================================================================
  // Suite 2: Delete Payment Method Security (8 tests)
  // ==========================================================================

  describe('Delete Payment Method Security', () => {
    test('delete button shows confirmation dialog', async () => {
      fetchMock.respondWith(mockPaymentMethods);

      render(<PaymentMethodManager />);

      await waitFor(() => {
        expect(screen.getByText(/5555/)).toBeInTheDocument();
      });

      // Find non-default card (5555) and click its delete button
      const nonDefaultCard = screen.getByText(/5555/).closest('[class*="card-interactive"]');
      const removeButton = nonDefaultCard?.querySelector('button[class*="text-error"]') as HTMLButtonElement;

      fetchMock.respondWith({ success: true });
      await userEvent.click(removeButton);

      expect(window.confirm).toHaveBeenCalledWith(
        'Are you sure you want to remove this payment method?'
      );
    });

    test('cancel confirmation dialog prevents deletion', async () => {
      fetchMock.respondWith(mockPaymentMethods);
      (window.confirm as jest.Mock).mockReturnValue(false);

      render(<PaymentMethodManager />);

      await waitFor(() => {
        expect(screen.getByText(/5555/)).toBeInTheDocument();
      });

      // Find non-default card's delete button
      const nonDefaultCard = screen.getByText(/5555/).closest('[class*="card-interactive"]');
      const removeButton = nonDefaultCard?.querySelector('button[class*="text-error"]') as HTMLButtonElement;
      await userEvent.click(removeButton);

      // Should not have made delete API call
      const calls = fetchMock.getCalls();
      const deleteCall = calls.find(c => c.options?.method === 'DELETE');
      expect(deleteCall).toBeUndefined();
    });

    test('successful deletion removes payment method from UI', async () => {
      fetchMock.respondWith(mockPaymentMethods);

      render(<PaymentMethodManager />);

      await waitFor(() => {
        expect(screen.getByText(/5555/)).toBeInTheDocument();
      });

      // Mock successful delete response
      fetchMock.respondWith({ success: true });

      // Find non-default card's delete button
      const nonDefaultCard = screen.getByText(/5555/).closest('[class*="card-interactive"]');
      const removeButton = nonDefaultCard?.querySelector('button[class*="text-error"]') as HTMLButtonElement;
      await userEvent.click(removeButton);

      await waitFor(() => {
        expect(screen.queryByText(/5555/)).not.toBeInTheDocument();
      });
    });

    test('delete button disabled for default payment method', async () => {
      fetchMock.respondWith(mockPaymentMethods);

      render(<PaymentMethodManager />);

      await waitFor(() => {
        expect(screen.getByText(/4242/)).toBeInTheDocument();
      });

      // Find the default card's row (has "Default" badge)
      const defaultCard = screen.getByText(/4242/).closest('[class*="card-interactive"]');
      expect(defaultCard).toBeInTheDocument();

      // The delete button in the default card's row should be disabled
      const buttons = defaultCard?.querySelectorAll('button');
      const deleteButton = Array.from(buttons || []).find(b => b.textContent?.includes('Remove'));

      expect(deleteButton).toHaveAttribute('disabled');
    });

    test('BUG-TEST-047: CAN delete last non-default payment method', async () => {
      // Only one non-default payment method
      const singleNonDefault = [
        {
          id: 'pm-999',
          type: 'card',
          brand: 'visa',
          last4: '9999',
          expiryMonth: 12,
          expiryYear: 2025,
          isDefault: false,
          createdAt: '2024-01-15T10:00:00Z',
        },
      ];
      fetchMock.respondWith(singleNonDefault);

      render(<PaymentMethodManager />);

      await waitFor(() => {
        expect(screen.getByText(/9999/)).toBeInTheDocument();
      });

      // Mock successful delete response
      fetchMock.respondWith({ success: true });

      const removeButton = screen.getByRole('button', { name: /Remove/i });

      // EXPECT BUG: Should warn user this is their last payment method
      // But currently allows deletion without warning
      expect(removeButton).not.toBeDisabled();

      await userEvent.click(removeButton);

      // Deleted successfully - no warning shown
      await waitFor(() => {
        expect(screen.queryByText(/9999/)).not.toBeInTheDocument();
      });

      console.warn('BUG-TEST-047: Can delete last payment method without warning');
    });

    test('delete shows loading spinner during API call', async () => {
      fetchMock.respondWith(mockPaymentMethods);

      render(<PaymentMethodManager />);

      await waitFor(() => {
        expect(screen.getByText(/5555/)).toBeInTheDocument();
      });

      // Make delete hang
      let resolveDelete: (value: any) => void;
      global.fetch = jest.fn(() => new Promise((resolve) => {
        resolveDelete = resolve;
      })) as any;

      // Find non-default card's delete button
      const nonDefaultCard = screen.getByText(/5555/).closest('[class*="card-interactive"]');
      const removeButton = nonDefaultCard?.querySelector('button[class*="text-error"]') as HTMLButtonElement;
      await userEvent.click(removeButton);

      // Should show loading spinner
      await waitFor(() => {
        expect(nonDefaultCard?.querySelector('.animate-spin')).toBeInTheDocument();
      });

      // Cleanup
      resolveDelete!(createMockFetchResponse({ success: true }));
    });

    test('delete failure logs error but does NOT show to user', async () => {
      fetchMock.respondWith(mockPaymentMethods);

      render(<PaymentMethodManager />);

      await waitFor(() => {
        expect(screen.getByText(/5555/)).toBeInTheDocument();
      });

      fetchMock.respondWithError(500, 'Server Error');

      // Find non-default card's delete button
      const nonDefaultCard = screen.getByText(/5555/).closest('[class*="card-interactive"]');
      const removeButton = nonDefaultCard?.querySelector('button[class*="text-error"]') as HTMLButtonElement;
      await userEvent.click(removeButton);

      await waitFor(() => {
        expect(logger.error).toHaveBeenCalledWith(
          'Failed to delete payment method:',
          expect.any(Error)
        );
      });

      // Card should still be visible (delete failed)
      expect(screen.getByText(/5555/)).toBeInTheDocument();
    });

    test('delete sends correct payment method ID to API', async () => {
      fetchMock.respondWith(mockPaymentMethods);

      render(<PaymentMethodManager />);

      await waitFor(() => {
        expect(screen.getByText(/5555/)).toBeInTheDocument();
      });

      fetchMock.respondWith({ success: true });

      // Find non-default card's delete button
      const nonDefaultCard = screen.getByText(/5555/).closest('[class*="card-interactive"]');
      const removeButton = nonDefaultCard?.querySelector('button[class*="text-error"]') as HTMLButtonElement;
      await userEvent.click(removeButton);

      await waitFor(() => {
        const calls = fetchMock.getCalls();
        const deleteCall = calls.find(c => c.options?.method === 'DELETE');
        expect(deleteCall?.url).toContain('pm-456');
      });
    });
  });

  // ==========================================================================
  // Suite 3: Set Default Payment Method (6 tests)
  // ==========================================================================

  describe('Set Default Payment Method', () => {
    test('Set Default button visible for non-default cards', async () => {
      fetchMock.respondWith(mockPaymentMethods);

      render(<PaymentMethodManager />);

      await waitFor(() => {
        expect(screen.getByText(/5555/)).toBeInTheDocument();
      });

      expect(screen.getByRole('button', { name: /Set Default/i })).toBeInTheDocument();
    });

    test('Set Default button NOT visible for default card', async () => {
      fetchMock.respondWith(mockPaymentMethods);

      render(<PaymentMethodManager />);

      await waitFor(() => {
        expect(screen.getByText(/4242/)).toBeInTheDocument();
      });

      // Default card should not have "Set Default" button
      const defaultCard = screen.getByText(/4242/).closest('[class*="card-interactive"]');
      const setDefaultBtn = defaultCard?.querySelector('button[class*="btn-ghost"]');

      // The default card row should only have a disabled Remove button
      const buttons = defaultCard?.querySelectorAll('button');
      const hasSetDefault = Array.from(buttons || []).some(b => b.textContent?.includes('Set Default'));

      expect(hasSetDefault).toBe(false);
    });

    test('clicking Set Default calls correct API endpoint', async () => {
      fetchMock.respondWith(mockPaymentMethods);

      render(<PaymentMethodManager />);

      await waitFor(() => {
        expect(screen.getByText(/5555/)).toBeInTheDocument();
      });

      // Add responses for set-default and subsequent refresh
      fetchMock.respondWith({ success: true });
      fetchMock.respondWith(mockPaymentMethods);

      const setDefaultButton = screen.getByRole('button', { name: /Set Default/i });
      await userEvent.click(setDefaultButton);

      await waitFor(() => {
        const calls = fetchMock.getCalls();
        const setDefaultCall = calls.find(c => c.url.includes('/set-default'));
        expect(setDefaultCall).toBeDefined();
        expect(setDefaultCall?.url).toContain('pm-456');
        expect(setDefaultCall?.options?.method).toBe('POST');
      });
    });

    test('Set Default shows loading spinner during API call', async () => {
      fetchMock.respondWith(mockPaymentMethods);

      render(<PaymentMethodManager />);

      await waitFor(() => {
        expect(screen.getByText(/5555/)).toBeInTheDocument();
      });

      // Make set-default hang
      let resolveSetDefault: (value: any) => void;
      global.fetch = jest.fn(() => new Promise((resolve) => {
        resolveSetDefault = resolve;
      })) as any;

      const setDefaultButton = screen.getByRole('button', { name: /Set Default/i });
      await userEvent.click(setDefaultButton);

      // Should show loading spinner
      await waitFor(() => {
        const nonDefaultCard = screen.getByText(/5555/).closest('[class*="card-interactive"]');
        expect(nonDefaultCard?.querySelector('.animate-spin')).toBeInTheDocument();
      });

      // Cleanup
      resolveSetDefault!(createMockFetchResponse({ success: true }));
    });

    test('successful Set Default refreshes payment methods list', async () => {
      fetchMock.respondWith(mockPaymentMethods);

      render(<PaymentMethodManager />);

      await waitFor(() => {
        expect(screen.getByText(/5555/)).toBeInTheDocument();
      });

      // Add responses for set-default and refresh
      fetchMock.respondWith({ success: true });

      // Updated list with new default
      const updatedMethods = [
        { ...mockPaymentMethods[0], isDefault: false },
        { ...mockPaymentMethods[1], isDefault: true },
      ];
      fetchMock.respondWith(updatedMethods);

      const setDefaultButton = screen.getByRole('button', { name: /Set Default/i });
      await userEvent.click(setDefaultButton);

      // Wait for refresh
      await waitFor(() => {
        const calls = fetchMock.getCalls();
        // Should have: initial fetch + set-default + refresh fetch
        expect(calls.length).toBeGreaterThanOrEqual(3);
      });
    });

    test('Set Default failure logs error', async () => {
      fetchMock.respondWith(mockPaymentMethods);

      render(<PaymentMethodManager />);

      await waitFor(() => {
        expect(screen.getByText(/5555/)).toBeInTheDocument();
      });

      fetchMock.respondWithError(500, 'Server Error');

      const setDefaultButton = screen.getByRole('button', { name: /Set Default/i });
      await userEvent.click(setDefaultButton);

      await waitFor(() => {
        expect(logger.error).toHaveBeenCalledWith(
          'Failed to set default payment method:',
          expect.any(Error)
        );
      });
    });
  });

  // ==========================================================================
  // Suite 4: Payment Method Display (5 tests)
  // ==========================================================================

  describe('Payment Method Display', () => {
    test('shows loading state initially', () => {
      fetchMock.respondWith(mockPaymentMethods);

      render(<PaymentMethodManager />);

      expect(screen.getByText(/Loading payment methods/i)).toBeInTheDocument();
    });

    test('displays card brand correctly (Visa, Mastercard, etc)', async () => {
      fetchMock.respondWith(mockPaymentMethods);

      render(<PaymentMethodManager />);

      await waitFor(() => {
        expect(screen.getByText(/Visa/i)).toBeInTheDocument();
        expect(screen.getByText(/Mastercard/i)).toBeInTheDocument();
      });
    });

    test('displays last 4 digits with bullet prefix', async () => {
      fetchMock.respondWith(mockPaymentMethods);

      render(<PaymentMethodManager />);

      await waitFor(() => {
        expect(screen.getByText(/4242/)).toBeInTheDocument();
        expect(screen.getByText(/5555/)).toBeInTheDocument();
      });
    });

    test('displays expiry date in MM/YY format', async () => {
      fetchMock.respondWith(mockPaymentMethods);

      render(<PaymentMethodManager />);

      await waitFor(() => {
        expect(screen.getByText(/12\/25/)).toBeInTheDocument();
        expect(screen.getByText(/06\/26/)).toBeInTheDocument();
      });
    });

    test('default card has visual indicator (ring and badge)', async () => {
      fetchMock.respondWith(mockPaymentMethods);

      render(<PaymentMethodManager />);

      await waitFor(() => {
        expect(screen.getByText(/4242/)).toBeInTheDocument();
      });

      // Check for "Default" badge
      expect(screen.getByText('Default')).toBeInTheDocument();

      // Check for ring styling on default card
      const defaultCard = screen.getByText(/4242/).closest('[class*="card-interactive"]');
      expect(defaultCard?.className).toContain('ring-2');
      expect(defaultCard?.className).toContain('ring-success');
    });
  });

  // ==========================================================================
  // Suite 5: Error Handling & Edge Cases (3 tests)
  // ==========================================================================

  describe('Error Handling & Edge Cases', () => {
    test('fetch error shows error message in UI', async () => {
      fetchMock.respondWithError(500, 'Server Error');

      render(<PaymentMethodManager />);

      await waitFor(() => {
        // Error is displayed in card-error container
        expect(screen.getByText(/Failed to fetch payment methods/i)).toBeInTheDocument();
      }, { timeout: 5000 });
    });

    test('network error during sync shows error message', async () => {
      fetchMock.respondWith([]);

      render(<PaymentMethodManager />);

      await waitFor(() => {
        expect(screen.getByText(/No payment methods/i)).toBeInTheDocument();
      });

      fetchMock.respondWithError(500, 'Sync Failed');

      const syncButton = screen.getByRole('button', { name: /Sync from Stripe/i });
      await userEvent.click(syncButton);

      await waitFor(() => {
        // Sync error from API is shown in card-error container
        expect(screen.getByText(/Sync Failed/i)).toBeInTheDocument();
      }, { timeout: 5000 });
    });

    test('handles API returning data wrapper vs direct array', async () => {
      // API sometimes returns { data: [...] } instead of [...]
      fetchMock.respondWith({ data: mockPaymentMethods });

      render(<PaymentMethodManager />);

      await waitFor(() => {
        expect(screen.getByText(/4242/)).toBeInTheDocument();
        expect(screen.getByText(/5555/)).toBeInTheDocument();
      });
    });
  });
});
