/**
 * Integration tests for Reputation Page
 * TDD RED Phase: Write failing tests first
 *
 * Testing Strategy:
 * - Mock only external dependencies: fetch, useAuth, useRouter
 * - Test real component behavior with actual UI interactions
 * - Verify API calls, state changes, and user feedback
 *
 * Coverage Target: 85%+ line coverage
 */

import React from 'react';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import '@testing-library/jest-dom';
import ReputationPage from '../page';

// Mock dependencies
const mockPush = jest.fn();
const stableRouterRef = { push: mockPush };

let mockAuthState = {
  user: { id: 'user-123', email: 'test@example.com', firstName: 'John' } as { id: string; email: string; firstName?: string } | null,
  isAuthenticated: true,
  isLoading: false,
};

jest.mock('next/navigation', () => ({
  useRouter: () => stableRouterRef,
}));

jest.mock('@/contexts/AuthContext', () => ({
  useAuth: () => mockAuthState,
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
    debug: jest.fn(),
  },
}));

// Mock data
const mockReputationScore = {
  userId: 'user-123',
  overallScore: 4.2,
  projectCompletionRate: 0.95,
  averageResponseTime: '2 hours',
  totalProjectsCompleted: 15,
  performanceStreakBonus: 0.3,
  totalPenalties: 1,
  lastUpdated: '2024-01-15T10:00:00Z',
  activeDisputes: 0,
  averageQualityRating: 4.5,
  averageCommunicationRating: 4.3,
  averageTimelinessRating: 4.0,
  averageProfessionalismRating: 4.2,
};

const mockReputationTrend = {
  trendDirection: 'Improving',
  averageChangePerDay: 0.02,
  totalChange: 0.6,
  startingScore: 3.6,
  currentScore: 4.2,
  projectsInPeriod: 5,
  peakScore: 4.3,
  lowestScore: 3.5,
  summary: 'Your reputation is steadily improving',
  daysActive: 30,
  totalReviews: 12,
  recentReviews: 3,
};

const mockReputationHistory = [
  {
    date: '2024-01-15',
    score: 4.2,
    projectsCompleted: 1,
    eventType: 'ProjectCompleted',
    description: 'Completed website redesign project',
    scoreChange: 0.1,
    changeReason: 'Positive review received',
    projectId: 'proj-1',
    reviewId: 'rev-1',
  },
  {
    date: '2024-01-10',
    score: 4.1,
    projectsCompleted: 1,
    eventType: 'ProjectCompleted',
    description: 'Completed mobile app project',
    scoreChange: 0.2,
    changeReason: 'Excellent rating',
    projectId: 'proj-2',
    reviewId: 'rev-2',
  },
];

describe('ReputationPage Integration Tests', () => {
  beforeEach(() => {
    jest.clearAllMocks();
    global.fetch = jest.fn();

    // Reset auth state to default (authenticated user)
    mockAuthState.user = { id: 'user-123', email: 'test@example.com', firstName: 'John' };
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
  // 1. Authentication & Loading States (3 tests)
  // ============================================================================

  describe('Authentication & Loading States', () => {
    test('shows loading spinner while auth is checking', () => {
      mockAuthState.user = null;
      mockAuthState.isAuthenticated = false;
      mockAuthState.isLoading = true;

      render(<ReputationPage />);

      expect(screen.getByText(/Loading/i)).toBeInTheDocument();
    });

    test('redirects to login when not authenticated', async () => {
      mockAuthState.user = null;
      mockAuthState.isAuthenticated = false;
      mockAuthState.isLoading = false;

      (global.fetch as jest.Mock).mockResolvedValueOnce({ ok: true });

      render(<ReputationPage />);

      await waitFor(() => {
        expect(window.location.href).toBe('/login');
      });
    });

    test('displays page content when authenticated', async () => {
      (global.fetch as jest.Mock)
        .mockResolvedValueOnce({
          ok: true,
          json: async () => mockReputationScore,
        })
        .mockResolvedValueOnce({
          ok: true,
          json: async () => mockReputationTrend,
        })
        .mockResolvedValueOnce({
          ok: true,
          json: async () => mockReputationHistory,
        });

      render(<ReputationPage />);

      await waitFor(() => {
        expect(screen.getByRole('heading', { level: 1 })).toHaveTextContent('Reputation');
      });
    });
  });

  // ============================================================================
  // 2. Score Display (4 tests)
  // ============================================================================

  describe('Score Display', () => {
    test('displays overall reputation score prominently', async () => {
      (global.fetch as jest.Mock)
        .mockResolvedValueOnce({
          ok: true,
          json: async () => mockReputationScore,
        })
        .mockResolvedValueOnce({
          ok: true,
          json: async () => mockReputationTrend,
        })
        .mockResolvedValueOnce({
          ok: true,
          json: async () => mockReputationHistory,
        });

      render(<ReputationPage />);

      await waitFor(() => {
        expect(screen.getByTestId('overall-score')).toHaveTextContent('4.2');
      });
    });

    test('shows score breakdown by category', async () => {
      (global.fetch as jest.Mock)
        .mockResolvedValueOnce({
          ok: true,
          json: async () => mockReputationScore,
        })
        .mockResolvedValueOnce({
          ok: true,
          json: async () => mockReputationTrend,
        })
        .mockResolvedValueOnce({
          ok: true,
          json: async () => mockReputationHistory,
        });

      render(<ReputationPage />);

      await waitFor(() => {
        expect(screen.getByText('Quality')).toBeInTheDocument();
        expect(screen.getByText('Communication')).toBeInTheDocument();
        expect(screen.getByText('Timeliness')).toBeInTheDocument();
        expect(screen.getByText('Professionalism')).toBeInTheDocument();
      });
    });

    test('displays category scores correctly', async () => {
      (global.fetch as jest.Mock)
        .mockResolvedValueOnce({
          ok: true,
          json: async () => mockReputationScore,
        })
        .mockResolvedValueOnce({
          ok: true,
          json: async () => mockReputationTrend,
        })
        .mockResolvedValueOnce({
          ok: true,
          json: async () => mockReputationHistory,
        });

      render(<ReputationPage />);

      await waitFor(() => {
        expect(screen.getByTestId('quality-score')).toHaveTextContent('4.5');
        expect(screen.getByTestId('communication-score')).toHaveTextContent('4.3');
        expect(screen.getByTestId('timeliness-score')).toHaveTextContent('4.0');
        expect(screen.getByTestId('professionalism-score')).toHaveTextContent('4.2');
      });
    });

    test('displays trend indicator (improving/stable/declining)', async () => {
      (global.fetch as jest.Mock)
        .mockResolvedValueOnce({
          ok: true,
          json: async () => mockReputationScore,
        })
        .mockResolvedValueOnce({
          ok: true,
          json: async () => mockReputationTrend,
        })
        .mockResolvedValueOnce({
          ok: true,
          json: async () => mockReputationHistory,
        });

      render(<ReputationPage />);

      await waitFor(() => {
        expect(screen.getByTestId('trend-indicator')).toHaveTextContent(/Improving/i);
      });
    });
  });

  // ============================================================================
  // 3. Statistics Display (4 tests)
  // ============================================================================

  describe('Statistics Display', () => {
    test('shows total projects completed', async () => {
      (global.fetch as jest.Mock)
        .mockResolvedValueOnce({
          ok: true,
          json: async () => mockReputationScore,
        })
        .mockResolvedValueOnce({
          ok: true,
          json: async () => mockReputationTrend,
        })
        .mockResolvedValueOnce({
          ok: true,
          json: async () => mockReputationHistory,
        });

      render(<ReputationPage />);

      await waitFor(() => {
        expect(screen.getByText('15')).toBeInTheDocument();
        expect(screen.getByText(/Projects Completed/i)).toBeInTheDocument();
      });
    });

    test('shows project completion rate', async () => {
      (global.fetch as jest.Mock)
        .mockResolvedValueOnce({
          ok: true,
          json: async () => mockReputationScore,
        })
        .mockResolvedValueOnce({
          ok: true,
          json: async () => mockReputationTrend,
        })
        .mockResolvedValueOnce({
          ok: true,
          json: async () => mockReputationHistory,
        });

      render(<ReputationPage />);

      await waitFor(() => {
        expect(screen.getByText('95%')).toBeInTheDocument();
        expect(screen.getByText(/Completion Rate/i)).toBeInTheDocument();
      });
    });

    test('displays performance streak bonus', async () => {
      (global.fetch as jest.Mock)
        .mockResolvedValueOnce({
          ok: true,
          json: async () => mockReputationScore,
        })
        .mockResolvedValueOnce({
          ok: true,
          json: async () => mockReputationTrend,
        })
        .mockResolvedValueOnce({
          ok: true,
          json: async () => mockReputationHistory,
        });

      render(<ReputationPage />);

      await waitFor(() => {
        expect(screen.getByText('+0.3')).toBeInTheDocument();
        expect(screen.getByText(/Streak Bonus/i)).toBeInTheDocument();
      });
    });

    test('shows any penalties', async () => {
      (global.fetch as jest.Mock)
        .mockResolvedValueOnce({
          ok: true,
          json: async () => mockReputationScore,
        })
        .mockResolvedValueOnce({
          ok: true,
          json: async () => mockReputationTrend,
        })
        .mockResolvedValueOnce({
          ok: true,
          json: async () => mockReputationHistory,
        });

      render(<ReputationPage />);

      await waitFor(() => {
        expect(screen.getByText('1')).toBeInTheDocument();
        expect(screen.getByText(/Penalties/i)).toBeInTheDocument();
      });
    });
  });

  // ============================================================================
  // 4. History Section (3 tests)
  // ============================================================================

  describe('History Section', () => {
    test('displays reputation history timeline', async () => {
      (global.fetch as jest.Mock)
        .mockResolvedValueOnce({
          ok: true,
          json: async () => mockReputationScore,
        })
        .mockResolvedValueOnce({
          ok: true,
          json: async () => mockReputationTrend,
        })
        .mockResolvedValueOnce({
          ok: true,
          json: async () => mockReputationHistory,
        });

      render(<ReputationPage />);

      await waitFor(() => {
        expect(screen.getByText(/History/i)).toBeInTheDocument();
        expect(screen.getByText(/website redesign project/i)).toBeInTheDocument();
        expect(screen.getByText(/mobile app project/i)).toBeInTheDocument();
      });
    });

    test('shows score changes over time', async () => {
      (global.fetch as jest.Mock)
        .mockResolvedValueOnce({
          ok: true,
          json: async () => mockReputationScore,
        })
        .mockResolvedValueOnce({
          ok: true,
          json: async () => mockReputationTrend,
        })
        .mockResolvedValueOnce({
          ok: true,
          json: async () => mockReputationHistory,
        });

      render(<ReputationPage />);

      await waitFor(() => {
        expect(screen.getByText('+0.1')).toBeInTheDocument();
        expect(screen.getByText('+0.2')).toBeInTheDocument();
      });
    });

    test('filters by time period (7d, 30d, 90d)', async () => {
      (global.fetch as jest.Mock)
        .mockResolvedValueOnce({
          ok: true,
          json: async () => mockReputationScore,
        })
        .mockResolvedValueOnce({
          ok: true,
          json: async () => mockReputationTrend,
        })
        .mockResolvedValueOnce({
          ok: true,
          json: async () => mockReputationHistory,
        });

      render(<ReputationPage />);

      await waitFor(() => {
        expect(screen.getByText('7 Days')).toBeInTheDocument();
        expect(screen.getByText('30 Days')).toBeInTheDocument();
        expect(screen.getByText('90 Days')).toBeInTheDocument();
      });

      // Click 90 days filter
      fireEvent.click(screen.getByText('90 Days'));

      await waitFor(() => {
        expect(global.fetch).toHaveBeenCalledWith(
          expect.stringContaining('days=90'),
          expect.any(Object)
        );
      });
    });
  });

  // ============================================================================
  // 5. Error Handling (2 tests)
  // ============================================================================

  describe('Error Handling', () => {
    test('shows fallback state on API error', async () => {
      (global.fetch as jest.Mock).mockRejectedValueOnce(new Error('Network error'));

      render(<ReputationPage />);

      await waitFor(() => {
        expect(screen.getByText(/Unable to load reputation data/i)).toBeInTheDocument();
      });
    });

    test('handles 404 gracefully for new users', async () => {
      (global.fetch as jest.Mock).mockResolvedValueOnce({
        ok: false,
        status: 404,
      });

      render(<ReputationPage />);

      await waitFor(() => {
        expect(screen.getByText(/No reputation data yet/i)).toBeInTheDocument();
      });
    });
  });

  // ============================================================================
  // 6. Trend Display (2 tests)
  // ============================================================================

  describe('Trend Display', () => {
    test('displays trend summary message', async () => {
      (global.fetch as jest.Mock)
        .mockResolvedValueOnce({
          ok: true,
          json: async () => mockReputationScore,
        })
        .mockResolvedValueOnce({
          ok: true,
          json: async () => mockReputationTrend,
        })
        .mockResolvedValueOnce({
          ok: true,
          json: async () => mockReputationHistory,
        });

      render(<ReputationPage />);

      await waitFor(() => {
        expect(screen.getByText(/steadily improving/i)).toBeInTheDocument();
      });
    });

    test('shows total score change', async () => {
      (global.fetch as jest.Mock)
        .mockResolvedValueOnce({
          ok: true,
          json: async () => mockReputationScore,
        })
        .mockResolvedValueOnce({
          ok: true,
          json: async () => mockReputationTrend,
        })
        .mockResolvedValueOnce({
          ok: true,
          json: async () => mockReputationHistory,
        });

      render(<ReputationPage />);

      await waitFor(() => {
        expect(screen.getByText('+0.6')).toBeInTheDocument();
      });
    });
  });
});
