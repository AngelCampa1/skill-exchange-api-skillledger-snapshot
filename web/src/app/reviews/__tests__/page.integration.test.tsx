/**
 * Integration tests for Reviews Page
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
import ReviewsPage from '../page';
import { fetchWithAuth } from '@/utils/apiClient';

// Mock apiClient so fetchWithAuth can be controlled in tests
jest.mock('@/utils/apiClient', () => ({
  fetchWithAuth: jest.fn(),
}));

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
const mockReviewStatistics = {
  userId: 'user-123',
  userName: 'John Doe',
  totalReviewsReceived: 8,
  averageOverallRating: 8.5,
  averageQualityRating: 8.8,
  averageCommunicationRating: 8.2,
  averageTimelinessRating: 8.0,
  averageProfessionalismRating: 8.6,
  clientReviewsCount: 5,
  providerReviewsCount: 3,
  mostRecentReviewDate: '2024-01-15T10:00:00Z',
};

const mockReviews = {
  success: true,
  data: [
    {
      id: 'rev-1',
      projectId: 'proj-1',
      projectTitle: 'Website Redesign',
      reviewerId: 'reviewer-1',
      reviewerName: 'Jane Smith',
      revieweeId: 'user-123',
      revieweeName: 'John Doe',
      type: 'ClientToProvider',
      overallRating: 9,
      qualityRating: 9,
      communicationRating: 8,
      timelinessRating: 9,
      professionalismRating: 9,
      calculatedAverageRating: 8.75,
      reviewText: 'Excellent work on the website redesign. Very professional and timely delivery.',
      responseText: null,
      status: 'Published',
      createdAt: '2024-01-15T10:00:00Z',
      publishedAt: '2024-01-16T10:00:00Z',
      hasPhotoAttachments: false,
      photoAttachmentCount: 0,
      photoAttachments: [],
    },
    {
      id: 'rev-2',
      projectId: 'proj-2',
      projectTitle: 'Mobile App Development',
      reviewerId: 'reviewer-2',
      reviewerName: 'Bob Johnson',
      revieweeId: 'user-123',
      revieweeName: 'John Doe',
      type: 'ProviderToClient',
      overallRating: 8,
      qualityRating: 8,
      communicationRating: 9,
      timelinessRating: 7,
      professionalismRating: 8,
      calculatedAverageRating: 8.0,
      reviewText: 'Great collaboration. Clear requirements and responsive communication.',
      responseText: 'Thank you for the positive feedback!',
      status: 'Published',
      createdAt: '2024-01-10T10:00:00Z',
      publishedAt: '2024-01-11T10:00:00Z',
      hasPhotoAttachments: false,
      photoAttachmentCount: 0,
      photoAttachments: [],
    },
  ],
  pagination: {
    currentPage: 1,
    pageSize: 10,
    totalCount: 2,
    totalPages: 1,
  },
  statistics: mockReviewStatistics,
};

describe('ReviewsPage Integration Tests', () => {
  beforeEach(() => {
    jest.clearAllMocks();
    global.fetch = jest.fn();
    (fetchWithAuth as jest.Mock).mockReset();

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

      render(<ReviewsPage />);

      expect(screen.getByText(/Loading/i)).toBeInTheDocument();
    });

    test('redirects to login when not authenticated', async () => {
      mockAuthState.user = null;
      mockAuthState.isAuthenticated = false;
      mockAuthState.isLoading = false;

      (global.fetch as jest.Mock).mockResolvedValueOnce({ ok: true });

      render(<ReviewsPage />);

      await waitFor(() => {
        expect(window.location.href).toBe('/login');
      });
    });

    test('displays page content when authenticated', async () => {
      (global.fetch as jest.Mock)
        .mockResolvedValueOnce({
          ok: true,
          json: async () => mockReviewStatistics,
        })
        .mockResolvedValueOnce({
          ok: true,
          json: async () => mockReviews,
        });

      render(<ReviewsPage />);

      await waitFor(() => {
        expect(screen.getByRole('heading', { level: 1 })).toHaveTextContent('Reviews');
      });
    });
  });

  // ============================================================================
  // 2. Reviews Statistics (4 tests)
  // ============================================================================

  describe('Reviews Statistics', () => {
    test('displays total reviews received', async () => {
      (global.fetch as jest.Mock)
        .mockResolvedValueOnce({
          ok: true,
          json: async () => mockReviewStatistics,
        })
        .mockResolvedValueOnce({
          ok: true,
          json: async () => mockReviews,
        });

      render(<ReviewsPage />);

      await waitFor(() => {
        expect(screen.getByTestId('total-reviews')).toHaveTextContent('8');
      });
    });

    test('shows average overall rating', async () => {
      (global.fetch as jest.Mock)
        .mockResolvedValueOnce({
          ok: true,
          json: async () => mockReviewStatistics,
        })
        .mockResolvedValueOnce({
          ok: true,
          json: async () => mockReviews,
        });

      render(<ReviewsPage />);

      await waitFor(() => {
        expect(screen.getByTestId('average-rating')).toHaveTextContent('8.5');
      });
    });

    test('shows client vs provider reviews breakdown', async () => {
      (global.fetch as jest.Mock)
        .mockResolvedValueOnce({
          ok: true,
          json: async () => mockReviewStatistics,
        })
        .mockResolvedValueOnce({
          ok: true,
          json: async () => mockReviews,
        });

      render(<ReviewsPage />);

      await waitFor(() => {
        // providerReviewsCount: 3, clientReviewsCount: 5 from mock data
        expect(screen.getByText(/3 as Provider/i)).toBeInTheDocument();
        expect(screen.getByText(/5 as Client/i)).toBeInTheDocument();
      });
    });

    test('shows ratings breakdown by category', async () => {
      (global.fetch as jest.Mock)
        .mockResolvedValueOnce({
          ok: true,
          json: async () => mockReviewStatistics,
        })
        .mockResolvedValueOnce({
          ok: true,
          json: async () => mockReviews,
        });

      render(<ReviewsPage />);

      await waitFor(() => {
        expect(screen.getByText('Quality')).toBeInTheDocument();
        expect(screen.getByText('Communication')).toBeInTheDocument();
        expect(screen.getByText('Timeliness')).toBeInTheDocument();
        expect(screen.getByText('Professionalism')).toBeInTheDocument();
      });
    });
  });

  // ============================================================================
  // 3. Reviews List (5 tests)
  // ============================================================================

  describe('Reviews List', () => {
    test('displays list of received reviews', async () => {
      (global.fetch as jest.Mock)
        .mockResolvedValueOnce({
          ok: true,
          json: async () => mockReviewStatistics,
        })
        .mockResolvedValueOnce({
          ok: true,
          json: async () => mockReviews,
        });

      render(<ReviewsPage />);

      await waitFor(() => {
        expect(screen.getByText('Website Redesign')).toBeInTheDocument();
        expect(screen.getByText('Mobile App Development')).toBeInTheDocument();
      });
    });

    test('shows reviewer name and project title', async () => {
      (global.fetch as jest.Mock)
        .mockResolvedValueOnce({
          ok: true,
          json: async () => mockReviewStatistics,
        })
        .mockResolvedValueOnce({
          ok: true,
          json: async () => mockReviews,
        });

      render(<ReviewsPage />);

      await waitFor(() => {
        expect(screen.getByText('Jane Smith')).toBeInTheDocument();
        expect(screen.getByText('Bob Johnson')).toBeInTheDocument();
      });
    });

    test('shows review rating and date', async () => {
      (global.fetch as jest.Mock)
        .mockResolvedValueOnce({
          ok: true,
          json: async () => mockReviewStatistics,
        })
        .mockResolvedValueOnce({
          ok: true,
          json: async () => mockReviews,
        });

      render(<ReviewsPage />);

      await waitFor(() => {
        expect(screen.getByText('9/10')).toBeInTheDocument();
        expect(screen.getByText('8/10')).toBeInTheDocument();
      });
    });

    test('shows review text content', async () => {
      (global.fetch as jest.Mock)
        .mockResolvedValueOnce({
          ok: true,
          json: async () => mockReviewStatistics,
        })
        .mockResolvedValueOnce({
          ok: true,
          json: async () => mockReviews,
        });

      render(<ReviewsPage />);

      await waitFor(() => {
        expect(screen.getByText(/Excellent work on the website redesign/i)).toBeInTheDocument();
        expect(screen.getByText(/Great collaboration/i)).toBeInTheDocument();
      });
    });

    test('pagination works correctly', async () => {
      const paginatedReviews = {
        ...mockReviews,
        pagination: {
          currentPage: 1,
          pageSize: 10,
          totalCount: 25,
          totalPages: 3,
        },
      };

      (global.fetch as jest.Mock)
        .mockResolvedValueOnce({
          ok: true,
          json: async () => mockReviewStatistics,
        })
        .mockResolvedValueOnce({
          ok: true,
          json: async () => paginatedReviews,
        });

      render(<ReviewsPage />);

      await waitFor(() => {
        expect(screen.getByText('Page 1 of 3')).toBeInTheDocument();
      });

      // Click next page
      const nextButton = screen.getByRole('button', { name: /next/i });
      fireEvent.click(nextButton);

      await waitFor(() => {
        expect(global.fetch).toHaveBeenCalledWith(
          expect.stringContaining('page=2'),
          expect.any(Object)
        );
      });
    });
  });

  // ============================================================================
  // 4. Filtering (3 tests)
  // ============================================================================

  describe('Filtering', () => {
    test('filter by review type (client/provider)', async () => {
      (global.fetch as jest.Mock)
        .mockResolvedValueOnce({
          ok: true,
          json: async () => mockReviewStatistics,
        })
        .mockResolvedValueOnce({
          ok: true,
          json: async () => mockReviews,
        });

      render(<ReviewsPage />);

      await waitFor(() => {
        expect(screen.getByText('All Reviews')).toBeInTheDocument();
      });

      // Click provider filter
      fireEvent.click(screen.getByText('As Provider'));

      await waitFor(() => {
        expect(global.fetch).toHaveBeenCalledWith(
          expect.stringContaining('type=ClientToProvider'),
          expect.any(Object)
        );
      });
    });

    test('sort by date', async () => {
      (global.fetch as jest.Mock)
        .mockResolvedValueOnce({
          ok: true,
          json: async () => mockReviewStatistics,
        })
        .mockResolvedValueOnce({
          ok: true,
          json: async () => mockReviews,
        });

      render(<ReviewsPage />);

      await waitFor(() => {
        expect(screen.getByText(/Sort by/i)).toBeInTheDocument();
      });

      // Select oldest first
      fireEvent.click(screen.getByText('Oldest First'));

      await waitFor(() => {
        expect(global.fetch).toHaveBeenCalledWith(
          expect.stringContaining('sortDescending=false'),
          expect.any(Object)
        );
      });
    });

    test('sort by rating', async () => {
      (global.fetch as jest.Mock)
        .mockResolvedValueOnce({
          ok: true,
          json: async () => mockReviewStatistics,
        })
        .mockResolvedValueOnce({
          ok: true,
          json: async () => mockReviews,
        });

      render(<ReviewsPage />);

      await waitFor(() => {
        expect(screen.getByText(/Sort by/i)).toBeInTheDocument();
      });

      // Select highest rated
      fireEvent.click(screen.getByText('Highest Rated'));

      await waitFor(() => {
        expect(global.fetch).toHaveBeenCalledWith(
          expect.stringContaining('sortBy=overallRating'),
          expect.any(Object)
        );
      });
    });
  });

  // ============================================================================
  // 5. Review Response (4 tests)
  // ============================================================================

  describe('Review Response', () => {
    test('shows "Respond" button on reviews without response', async () => {
      (global.fetch as jest.Mock)
        .mockResolvedValueOnce({
          ok: true,
          json: async () => mockReviewStatistics,
        })
        .mockResolvedValueOnce({
          ok: true,
          json: async () => mockReviews,
        });

      render(<ReviewsPage />);

      await waitFor(() => {
        // First review has no response, should show button
        const respondButtons = screen.getAllByText('Respond');
        expect(respondButtons.length).toBeGreaterThan(0);
      });
    });

    test('shows existing responses', async () => {
      (global.fetch as jest.Mock)
        .mockResolvedValueOnce({
          ok: true,
          json: async () => mockReviewStatistics,
        })
        .mockResolvedValueOnce({
          ok: true,
          json: async () => mockReviews,
        });

      render(<ReviewsPage />);

      await waitFor(() => {
        // Second review has a response
        expect(screen.getByText('Thank you for the positive feedback!')).toBeInTheDocument();
      });
    });

    test('opens response modal on click', async () => {
      (global.fetch as jest.Mock)
        .mockResolvedValueOnce({
          ok: true,
          json: async () => mockReviewStatistics,
        })
        .mockResolvedValueOnce({
          ok: true,
          json: async () => mockReviews,
        });

      render(<ReviewsPage />);

      await waitFor(() => {
        const respondButton = screen.getAllByText('Respond')[0];
        fireEvent.click(respondButton);
      });

      await waitFor(() => {
        expect(screen.getByText('Respond to Review')).toBeInTheDocument();
        expect(screen.getByPlaceholderText(/Write your response/i)).toBeInTheDocument();
      });
    });

    test('submits response successfully', async () => {
      // Initial page load: statistics + reviews (both use global.fetch for GETs)
      (global.fetch as jest.Mock)
        .mockResolvedValueOnce({
          ok: true,
          json: async () => mockReviewStatistics,
        })
        .mockResolvedValueOnce({
          ok: true,
          json: async () => mockReviews,
        });

      // POST response via fetchWithAuth (returns parsed JSON directly)
      (fetchWithAuth as jest.Mock).mockResolvedValueOnce({ success: true, message: 'Response added' });

      // Refetch after success: statistics + reviews (global.fetch for GETs)
      (global.fetch as jest.Mock)
        .mockResolvedValueOnce({
          ok: true,
          json: async () => mockReviewStatistics,
        })
        .mockResolvedValueOnce({
          ok: true,
          json: async () => mockReviews,
        });

      render(<ReviewsPage />);

      await waitFor(() => {
        const respondButton = screen.getAllByText('Respond')[0];
        fireEvent.click(respondButton);
      });

      await waitFor(() => {
        const textarea = screen.getByPlaceholderText(/Write your response/i);
        fireEvent.change(textarea, { target: { value: 'Thank you for your feedback!' } });
      });

      const submitButton = screen.getByText('Submit Response');
      fireEvent.click(submitButton);

      await waitFor(() => {
        expect(fetchWithAuth).toHaveBeenCalledWith(
          expect.stringContaining('/api/review/rev-1/respond'),
          expect.objectContaining({
            method: 'POST',
            body: JSON.stringify({ response: 'Thank you for your feedback!' }),
          })
        );
      });
    });
  });

  // ============================================================================
  // 6. Empty States (2 tests)
  // ============================================================================

  describe('Empty States', () => {
    test('shows message when no reviews exist', async () => {
      const emptyReviews = {
        success: true,
        data: [],
        pagination: {
          currentPage: 1,
          pageSize: 10,
          totalCount: 0,
          totalPages: 0,
        },
        statistics: {
          ...mockReviewStatistics,
          totalReviewsReceived: 0,
        },
      };

      (global.fetch as jest.Mock)
        .mockResolvedValueOnce({
          ok: true,
          json: async () => ({ ...mockReviewStatistics, totalReviewsReceived: 0 }),
        })
        .mockResolvedValueOnce({
          ok: true,
          json: async () => emptyReviews,
        });

      render(<ReviewsPage />);

      await waitFor(() => {
        expect(screen.getByText(/No reviews yet/i)).toBeInTheDocument();
      });
    });

    test('shows helpful message for new users', async () => {
      (global.fetch as jest.Mock).mockResolvedValueOnce({
        ok: false,
        status: 404,
      });

      render(<ReviewsPage />);

      await waitFor(() => {
        expect(screen.getByText(/Complete your first project to receive reviews/i)).toBeInTheDocument();
      });
    });
  });

  // ============================================================================
  // 7. Error Handling (2 tests)
  // ============================================================================

  describe('Error Handling', () => {
    test('shows error state on API failure', async () => {
      (global.fetch as jest.Mock).mockRejectedValueOnce(new Error('Network error'));

      render(<ReviewsPage />);

      await waitFor(() => {
        expect(screen.getByText(/Unable to load reviews/i)).toBeInTheDocument();
      });
    });

    test('handles response submission error', async () => {
      // Initial page load: statistics + reviews (global.fetch for GETs)
      (global.fetch as jest.Mock)
        .mockResolvedValueOnce({
          ok: true,
          json: async () => mockReviewStatistics,
        })
        .mockResolvedValueOnce({
          ok: true,
          json: async () => mockReviews,
        });

      // POST response via fetchWithAuth - throws on error
      (fetchWithAuth as jest.Mock).mockRejectedValueOnce(new Error('Response too short'));

      render(<ReviewsPage />);

      // Wait for page to load and click respond button
      await waitFor(() => {
        expect(screen.getAllByText('Respond').length).toBeGreaterThan(0);
      });

      const respondButton = screen.getAllByText('Respond')[0];
      fireEvent.click(respondButton);

      // Wait for modal to open and fill in response (min 10 chars)
      await waitFor(() => {
        expect(screen.getByPlaceholderText(/Write your response/i)).toBeInTheDocument();
      });

      const textarea = screen.getByPlaceholderText(/Write your response/i);
      fireEvent.change(textarea, { target: { value: 'Thank you!' } }); // 10 chars minimum

      const submitButton = screen.getByText('Submit Response');
      fireEvent.click(submitButton);

      await waitFor(() => {
        expect(screen.getByText(/Response too short/i)).toBeInTheDocument();
      }, { timeout: 3000 });
    });
  });
});
