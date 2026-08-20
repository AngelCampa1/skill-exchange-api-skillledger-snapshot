/**
 * subscription/page.tsx Integration Tests
 *
 * Tests subscription tier selection page with authentication and content rendering.
 * Focus: Auth flow, TierSelectionFlow integration, FAQ content, checkout handlers.
 *
 * Coverage Target: 85%+ (190 lines)
 * Test Count: 12 tests
 */

import React from 'react';
import { render, screen, waitFor } from '@testing-library/react';
import { useAuth } from '@/contexts/AuthContext';
import SubscriptionPage from '../page';

// Mock dependencies
jest.mock('@/contexts/AuthContext', () => ({
  useAuth: jest.fn(),
}));

jest.mock('@/contexts/ThemeContext', () => ({
  useTheme: jest.fn(() => ({
    theme: 'light',
    setTheme: jest.fn(),
  })),
}));

jest.mock('@/components/ThemeToggle', () => ({
  ThemeToggle: () => <div>Theme Toggle</div>,
}));

jest.mock('@/components/TierSelectionFlow', () => ({
  TierSelectionFlow: ({ onCheckoutSuccess, onCheckoutError }: any) => (
    <div data-testid="tier-selection-flow">
      <button onClick={() => onCheckoutSuccess({ success: true })}>Mock Success</button>
      <button onClick={() => onCheckoutError(new Error('Mock error'))}>Mock Error</button>
    </div>
  ),
}));

const mockUseAuth = useAuth as jest.MockedFunction<typeof useAuth>;

describe('SubscriptionPage - Authentication & Loading', () => {
  afterEach(() => {
    jest.clearAllMocks();
  });

  it('should show loading spinner while auth is loading', () => {
    mockUseAuth.mockReturnValue({
      user: null,
      isAuthenticated: false,
      isLoading: true,
      isInitialized: false,
      login: jest.fn(),
      logout: jest.fn(),
      refreshToken: jest.fn(),
      updateUser: jest.fn(),
    });

    render(<SubscriptionPage />);

    expect(screen.getByText('Loading subscription options...')).toBeInTheDocument();
    expect(screen.getByText('Loading subscription options...').previousSibling).toHaveClass('loading-spinner');
  });

  it('should return null if not authenticated', () => {
    mockUseAuth.mockReturnValue({
      user: null,
      isAuthenticated: false,
      isLoading: false,
      isInitialized: true,
      login: jest.fn(),
      logout: jest.fn(),
      refreshToken: jest.fn(),
      updateUser: jest.fn(),
    });

    const { container } = render(<SubscriptionPage />);

    // Should render null (empty container)
    expect(container.firstChild).toBeNull();
  });

  it('should render page when authenticated', async () => {
    mockUseAuth.mockReturnValue({
      user: {
        id: 'user-123',
        email: 'test@example.com',
        userName: 'testuser',
        firstName: 'John',
        lastName: 'Doe',
        emailVerified: true,
        taxCompliant: true,
        status: 'Active',
        roles: ['User'],
        permissions: [],
      },
      isAuthenticated: true,
      isLoading: false,
      isInitialized: true,
      login: jest.fn(),
      logout: jest.fn(),
      refreshToken: jest.fn(),
      updateUser: jest.fn(),
    });

    render(<SubscriptionPage />);

    await waitFor(() => {
      expect(screen.getByText(/Upgrade Your Experience/i)).toBeInTheDocument();
    });
  });
});

describe('SubscriptionPage - Navigation & Header', () => {
  const mockAuthenticatedUser = {
    user: {
      id: 'user-123',
      email: 'test@example.com',
      userName: 'testuser',
      firstName: 'John',
      lastName: 'Doe',
      emailVerified: true,
      taxCompliant: true,
      status: 'Active',
      roles: ['User'],
      permissions: [],
    },
    isAuthenticated: true,
    isLoading: false,
    isInitialized: true,
    login: jest.fn(),
    logout: jest.fn(),
    refreshToken: jest.fn(),
    updateUser: jest.fn(),
  };

  beforeEach(() => {
    mockUseAuth.mockReturnValue(mockAuthenticatedUser);
  });

  afterEach(() => {
    jest.clearAllMocks();
  });

  it('should show "Back to Dashboard" link', () => {
    render(<SubscriptionPage />);

    const backLink = screen.getByText('Back to Dashboard');
    expect(backLink).toBeInTheDocument();
    expect(backLink.closest('a')).toHaveAttribute('href', '/dashboard');
  });

  it('should display user\'s userName in header', () => {
    render(<SubscriptionPage />);

    expect(screen.getByText('Welcome')).toBeInTheDocument();
    expect(screen.getByText('testuser')).toBeInTheDocument();
  });
});

describe('SubscriptionPage - Page Content', () => {
  const mockAuthenticatedUser = {
    user: {
      id: 'user-123',
      email: 'test@example.com',
      userName: 'testuser',
      firstName: 'John',
      lastName: 'Doe',
      emailVerified: true,
      taxCompliant: true,
      status: 'Active',
      roles: ['User'],
      permissions: [],
    },
    isAuthenticated: true,
    isLoading: false,
    isInitialized: true,
    login: jest.fn(),
    logout: jest.fn(),
    refreshToken: jest.fn(),
    updateUser: jest.fn(),
  };

  beforeEach(() => {
    mockUseAuth.mockReturnValue(mockAuthenticatedUser);
  });

  afterEach(() => {
    jest.clearAllMocks();
  });

  it('should show page title "Upgrade Your Experience"', () => {
    render(<SubscriptionPage />);

    expect(screen.getByText(/Upgrade Your Experience/i)).toBeInTheDocument();
    expect(screen.getByText(/Choose the perfect plan for your needs/i)).toBeInTheDocument();
  });

  it('should show trust indicators', () => {
    render(<SubscriptionPage />);

    expect(screen.getByText('No Setup Fees')).toBeInTheDocument();
    expect(screen.getByText('Start instantly')).toBeInTheDocument();

    expect(screen.getByText('Cancel Anytime')).toBeInTheDocument();
    expect(screen.getByText('No long-term contracts')).toBeInTheDocument();

    expect(screen.getByText('30-Day Guarantee')).toBeInTheDocument();
    expect(screen.getByText('Full refund if not satisfied')).toBeInTheDocument();
  });

  it('should render TierSelectionFlow component', () => {
    render(<SubscriptionPage />);

    expect(screen.getByTestId('tier-selection-flow')).toBeInTheDocument();
  });

  it('should show FAQ section with 4 questions', () => {
    render(<SubscriptionPage />);

    expect(screen.getByText('Frequently Asked Questions')).toBeInTheDocument();

    // Check all 4 FAQ titles
    expect(screen.getByText('Can I change plans anytime?')).toBeInTheDocument();
    expect(screen.getByText('What payment methods do you accept?')).toBeInTheDocument();
    expect(screen.getByText('Is there a free trial?')).toBeInTheDocument();
    expect(screen.getByText('What happens if I exceed my limits?')).toBeInTheDocument();
  });

  it('should show CTA section with "Go to Dashboard" link', () => {
    render(<SubscriptionPage />);

    expect(screen.getByText('Ready to get started?')).toBeInTheDocument();
    expect(screen.getByText(/Pick a plan that fits your needs/i)).toBeInTheDocument();

    const ctaLink = screen.getByText('Go to Dashboard');
    expect(ctaLink).toBeInTheDocument();
    expect(ctaLink.closest('a')).toHaveAttribute('href', '/dashboard');
  });
});

describe('SubscriptionPage - Checkout Handlers', () => {
  const mockAuthenticatedUser = {
    user: {
      id: 'user-123',
      email: 'test@example.com',
      userName: 'testuser',
      firstName: 'John',
      lastName: 'Doe',
      emailVerified: true,
      taxCompliant: true,
      status: 'Active',
      roles: ['User'],
      permissions: [],
    },
    isAuthenticated: true,
    isLoading: false,
    isInitialized: true,
    login: jest.fn(),
    logout: jest.fn(),
    refreshToken: jest.fn(),
    updateUser: jest.fn(),
  };

  beforeEach(() => {
    mockUseAuth.mockReturnValue(mockAuthenticatedUser);
  });

  afterEach(() => {
    jest.clearAllMocks();
  });

  it('should handle checkout success', () => {
    // Mock logger.debug to verify it's called
    const mockDebug = jest.fn();
    jest.spyOn(require('@/utils/logger').logger, 'debug').mockImplementation(mockDebug);

    render(<SubscriptionPage />);

    const successButton = screen.getByText('Mock Success');
    successButton.click();

    expect(mockDebug).toHaveBeenCalledWith('Checkout successful:', { success: true });
  });

  it('should handle checkout error', () => {
    // Mock logger.error to verify it's called
    const mockError = jest.fn();
    jest.spyOn(require('@/utils/logger').logger, 'error').mockImplementation(mockError);

    render(<SubscriptionPage />);

    const errorButton = screen.getByText('Mock Error');
    errorButton.click();

    expect(mockError).toHaveBeenCalledWith('Checkout failed:', expect.any(Error));
  });
});
