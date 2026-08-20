/**
 * Integration tests for AntiGamingDashboard component (Week 17 Part A)
 *
 * Test Coverage:
 * 1. Suspicious Activity Detection (6 tests)
 * 2. Summary Cards & Statistics (4 tests)
 * 3. Moderation Actions (4 tests)
 * 4. Loading & Error States (2 tests)
 * 5. Priority & Risk Score Display (2 tests)
 *
 * Total: 18 tests
 */

import { render, screen, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import AntiGamingDashboard from '../AntiGamingDashboard';

// Mock fetch
global.fetch = jest.fn();

describe('AntiGamingDashboard - Integration Tests', () => {
  beforeEach(() => {
    jest.clearAllMocks();
    (global.fetch as jest.Mock).mockClear();
  });

  afterEach(() => {
    jest.restoreAllMocks();
  });

  // Helper function to create mock alert data
  const createMockAlert = (id: string, overrides = {}) => ({
    id,
    userId: `user-${id}`,
    userName: `User ${id}`,
    userEmail: `user${id}@example.com`,
    alertType: 'BehaviorAnomaly' as const,
    riskScore: 0.5,
    isResolved: false,
    createdAt: new Date().toISOString(),
    details: `Suspicious activity detected for ${id}`,
    evidenceItems: ['Evidence 1', 'Evidence 2'],
    ...overrides,
  });

  // Helper function to create mock sanction data
  const createMockSanction = (id: string, overrides = {}) => ({
    id,
    userId: `user-${id}`,
    userName: `User ${id}`,
    sanctionType: 'Warning' as const,
    reason: 'Gaming behavior detected',
    isActive: true,
    createdAt: new Date().toISOString(),
    ...overrides,
  });

  // Helper function to create mock pending review data
  const createMockReview = (alertId: string, overrides = {}) => ({
    alertId,
    userName: `User ${alertId}`,
    riskScore: 0.7,
    alertType: 'ReviewGaming',
    createdAt: new Date().toISOString(),
    priority: 'Medium' as const,
    ...overrides,
  });

  // Helper function to mock successful dashboard data fetch
  const mockSuccessfulFetch = (alerts: any[] = [], sanctions: any[] = [], reviews: any[] = []) => {
    (global.fetch as jest.Mock)
      .mockResolvedValueOnce({
        ok: true,
        json: async () => alerts,
      })
      .mockResolvedValueOnce({
        ok: true,
        json: async () => sanctions,
      })
      .mockResolvedValueOnce({
        ok: true,
        json: async () => reviews,
      });
  };

  // ===================================================================
  // 1. Suspicious Activity Detection (6 tests)
  // ===================================================================

  describe('Suspicious Activity Detection', () => {
    test('displays flagged users with risk scores', async () => {
      const mockAlerts = [
        createMockAlert('1', { userName: 'Suspicious User', riskScore: 0.85 }),
        createMockAlert('2', { userName: 'Normal User', riskScore: 0.3 }),
      ];

      mockSuccessfulFetch(mockAlerts, [], []);

      render(<AntiGamingDashboard />);

      await waitFor(() => {
        expect(screen.getByText('Suspicious User')).toBeInTheDocument();
        expect(screen.getByText('Normal User')).toBeInTheDocument();
      });

      // Verify risk scores are displayed
      expect(screen.getByText('85%')).toBeInTheDocument();
      expect(screen.getByText('30%')).toBeInTheDocument();
    });

    test('risk score color coding (High, Medium, Low)', async () => {
      const mockAlerts = [
        createMockAlert('1', { riskScore: 0.9 }), // High: >= 0.8 red
        createMockAlert('2', { riskScore: 0.7 }), // Medium: >= 0.6 warning
        createMockAlert('3', { riskScore: 0.3 }), // Low: < 0.4 success
      ];

      mockSuccessfulFetch(mockAlerts, [], []);

      render(<AntiGamingDashboard />);

      await waitFor(() => {
        const riskBadges = screen.getAllByText(/\d+%/);
        expect(riskBadges.length).toBeGreaterThanOrEqual(3);
      });

      // Verify high risk has destructive styling
      const highRiskBadge = screen.getByText('90%');
      expect(highRiskBadge).toHaveClass('text-destructive');
    });

    test('activity timeline for flagged user (createdAt display)', async () => {
      const yesterday = new Date();
      yesterday.setDate(yesterday.getDate() - 1);

      const mockAlerts = [
        createMockAlert('1', { createdAt: yesterday.toISOString() }),
      ];

      mockSuccessfulFetch(mockAlerts, [], []);

      render(<AntiGamingDashboard />);

      await waitFor(() => {
        // Verify date is displayed (format may vary)
        expect(screen.getByText(new Date(yesterday).toLocaleDateString())).toBeInTheDocument();
      });
    });

    test('detection patterns displayed (alertType)', async () => {
      const mockAlerts = [
        createMockAlert('1', { alertType: 'BehaviorAnomaly' }),
        createMockAlert('2', { alertType: 'ContentSimilarity' }),
        createMockAlert('3', { alertType: 'NetworkSuspicion' }),
      ];

      mockSuccessfulFetch(mockAlerts, [], []);

      render(<AntiGamingDashboard />);

      await waitFor(() => {
        expect(screen.getByText('BehaviorAnomaly')).toBeInTheDocument();
        expect(screen.getByText('ContentSimilarity')).toBeInTheDocument();
        expect(screen.getByText('NetworkSuspicion')).toBeInTheDocument();
      });
    });

    test('alert resolution status shown (Resolved vs Pending)', async () => {
      const mockAlerts = [
        createMockAlert('1', { isResolved: true }),
        createMockAlert('2', { isResolved: false }),
      ];

      mockSuccessfulFetch(mockAlerts, [], []);

      render(<AntiGamingDashboard />);

      await waitFor(() => {
        expect(screen.getAllByText('Resolved').length).toBeGreaterThan(0);
        expect(screen.getAllByText('Pending').length).toBeGreaterThan(0);
      });
    });

    test('evidence items displayed in alert details', async () => {
      const user = userEvent.setup();
      const mockAlerts = [
        createMockAlert('evidence-1', {
          userName: 'Evidence User',
          evidenceItems: ['Duplicate content detected', 'Rating manipulation pattern'],
        }),
      ];

      mockSuccessfulFetch(mockAlerts, [], [{ alertId: 'evidence-1', userName: 'Evidence User', riskScore: 0.8, alertType: 'ReviewGaming', createdAt: new Date().toISOString(), priority: 'High' }]);

      render(<AntiGamingDashboard />);

      // Wait for pending reviews to load - user appears in both sections
      await waitFor(() => {
        const userElements = screen.getAllByText('Evidence User');
        expect(userElements.length).toBeGreaterThan(0);
      });

      // Click Review button in pending reviews section
      const reviewButtons = screen.getAllByText('Review');
      await user.click(reviewButtons[0]);

      // Verify evidence is shown in modal
      await waitFor(() => {
        expect(screen.getByText(/Duplicate content detected/)).toBeInTheDocument();
        expect(screen.getByText(/Rating manipulation pattern/)).toBeInTheDocument();
      });
    });
  });

  // ===================================================================
  // 2. Summary Cards & Statistics (4 tests)
  // ===================================================================

  // Helper to find summary card containing specific title text
  // Uses getAllByText because titles may appear in both summary cards and section headers
  const findCardWithTitle = (title: string): HTMLElement => {
    const titleElements = screen.getAllByText(title);
    // Find the title element that's in a summary card (has text-sm class for summary card titles)
    for (const titleElement of titleElements) {
      const isSummaryCardTitle = titleElement.className?.includes('text-sm') ||
        titleElement.closest('.grid.grid-cols-1.md\\:grid-cols-4');
      if (isSummaryCardTitle) {
        // Navigate up to find the card (the element with rounded-2xl class)
        let current = titleElement.parentElement;
        while (current && !current.className?.includes('rounded-2xl')) {
          current = current.parentElement;
        }
        if (current) return current as HTMLElement;
      }
    }
    // Fallback: use first element if no summary card found
    const titleElement = titleElements[0];
    let current = titleElement.parentElement;
    while (current && !current.className?.includes('rounded-2xl')) {
      current = current.parentElement;
    }
    return current as HTMLElement;
  };

  describe('Summary Cards & Statistics', () => {
    test('displays pending reviews count', async () => {
      const mockReviews = [
        createMockReview('1'),
        createMockReview('2'),
        createMockReview('3'),
      ];

      mockSuccessfulFetch([], [], mockReviews);

      render(<AntiGamingDashboard />);

      await waitFor(() => {
        const pendingCard = findCardWithTitle('Pending Reviews');
        expect(within(pendingCard).getByText('3')).toBeInTheDocument();
      });
    });

    test('displays high risk alerts count (>= 0.8)', async () => {
      const mockAlerts = [
        createMockAlert('1', { riskScore: 0.9, isResolved: false }), // High risk
        createMockAlert('2', { riskScore: 0.85, isResolved: false }), // High risk
        createMockAlert('3', { riskScore: 0.7, isResolved: false }), // Not high risk
        createMockAlert('4', { riskScore: 0.9, isResolved: true }), // Resolved, shouldn't count
      ];

      mockSuccessfulFetch(mockAlerts, [], []);

      render(<AntiGamingDashboard />);

      await waitFor(() => {
        const highRiskCard = findCardWithTitle('High Risk Alerts');
        expect(within(highRiskCard).getByText('2')).toBeInTheDocument();
      });
    });

    test('displays active sanctions count', async () => {
      const mockSanctions = [
        createMockSanction('1', { isActive: true }),
        createMockSanction('2', { isActive: true }),
        createMockSanction('3', { isActive: false }), // Inactive, shouldn't count
      ];

      mockSuccessfulFetch([], mockSanctions, []);

      render(<AntiGamingDashboard />);

      await waitFor(() => {
        const sanctionsCard = findCardWithTitle('Active Sanctions');
        expect(within(sanctionsCard).getByText('2')).toBeInTheDocument();
      });
    });

    test('displays today\'s alerts count', async () => {
      const today = new Date();
      const yesterday = new Date();
      yesterday.setDate(yesterday.getDate() - 1);

      const mockAlerts = [
        createMockAlert('1', { createdAt: today.toISOString() }),
        createMockAlert('2', { createdAt: today.toISOString() }),
        createMockAlert('3', { createdAt: yesterday.toISOString() }), // Yesterday, shouldn't count
      ];

      mockSuccessfulFetch(mockAlerts, [], []);

      render(<AntiGamingDashboard />);

      await waitFor(() => {
        const todayCard = findCardWithTitle("Today's Alerts");
        expect(within(todayCard).getByText('2')).toBeInTheDocument();
      });
    });
  });

  // ===================================================================
  // 3. Moderation Actions (4 tests)
  // ===================================================================

  describe('Moderation Actions', () => {
    test('dismiss alert button triggers API call', async () => {
      const user = userEvent.setup();
      const mockAlerts = [createMockAlert('alert-123', { userName: 'Dismissable User' })];

      mockSuccessfulFetch(mockAlerts, [], [{ alertId: 'alert-123', userName: 'Dismissable User', riskScore: 0.8, alertType: 'ReviewGaming', createdAt: new Date().toISOString(), priority: 'High' }]);

      render(<AntiGamingDashboard />);

      // Wait for data to load - user appears in both sections
      await waitFor(() => {
        const userElements = screen.getAllByText('Dismissable User');
        expect(userElements.length).toBeGreaterThan(0);
      });

      const reviewButton = screen.getAllByText('Review')[0];
      await user.click(reviewButton);

      // Wait for modal and click Dismiss button
      await waitFor(() => {
        expect(screen.getByText('Alert Review')).toBeInTheDocument();
      });

      // Mock dismiss API call and subsequent data reload
      (global.fetch as jest.Mock).mockResolvedValueOnce({
        ok: true,
        json: async () => ({}),
      });
      mockSuccessfulFetch(mockAlerts, [], []); // Reload data after dismiss

      const dismissButton = screen.getByText('Dismiss');
      await user.click(dismissButton);

      await waitFor(() => {
        expect(global.fetch).toHaveBeenCalledWith(
          '/api/admin/anti-gaming/alerts/alert-123/resolve',
          expect.objectContaining({
            method: 'POST',
            body: JSON.stringify({ action: 'dismiss' }),
          })
        );
      });
    });

    test('issue warning button triggers API call', async () => {
      const user = userEvent.setup();
      const mockAlerts = [createMockAlert('alert-456', { userName: 'Warnable User' })];

      mockSuccessfulFetch(mockAlerts, [], [{ alertId: 'alert-456', userName: 'Warnable User', riskScore: 0.8, alertType: 'ReviewGaming', createdAt: new Date().toISOString(), priority: 'High' }]);

      render(<AntiGamingDashboard />);

      await waitFor(() => {
        const userElements = screen.getAllByText('Warnable User');
        expect(userElements.length).toBeGreaterThan(0);
      });

      const reviewButton = screen.getAllByText('Review')[0];
      await user.click(reviewButton);

      await waitFor(() => {
        expect(screen.getByText('Alert Review')).toBeInTheDocument();
      });

      // Mock warning API call and reload
      (global.fetch as jest.Mock).mockResolvedValueOnce({
        ok: true,
        json: async () => ({}),
      });
      mockSuccessfulFetch(mockAlerts, [], []);

      const warnButton = screen.getByText('Issue Warning');
      await user.click(warnButton);

      await waitFor(() => {
        expect(global.fetch).toHaveBeenCalledWith(
          '/api/admin/anti-gaming/alerts/alert-456/resolve',
          expect.objectContaining({
            method: 'POST',
            body: JSON.stringify({ action: 'warn' }),
          })
        );
      });
    });

    test('suspend user button triggers API call', async () => {
      const user = userEvent.setup();
      const mockAlerts = [createMockAlert('alert-789', { userName: 'Suspendable User' })];

      mockSuccessfulFetch(mockAlerts, [], [{ alertId: 'alert-789', userName: 'Suspendable User', riskScore: 0.95, alertType: 'ReviewGaming', createdAt: new Date().toISOString(), priority: 'Critical' }]);

      render(<AntiGamingDashboard />);

      await waitFor(() => {
        const userElements = screen.getAllByText('Suspendable User');
        expect(userElements.length).toBeGreaterThan(0);
      });

      const reviewButton = screen.getAllByText('Review')[0];
      await user.click(reviewButton);

      await waitFor(() => {
        expect(screen.getByText('Alert Review')).toBeInTheDocument();
      });

      // Mock suspend API call and reload
      (global.fetch as jest.Mock).mockResolvedValueOnce({
        ok: true,
        json: async () => ({}),
      });
      mockSuccessfulFetch(mockAlerts, [], []);

      // Find Suspend User button in modal (not in the list)
      const suspendButton = screen.getByRole('button', { name: 'Suspend User' });
      await user.click(suspendButton);

      await waitFor(() => {
        expect(global.fetch).toHaveBeenCalledWith(
          '/api/admin/anti-gaming/alerts/alert-789/resolve',
          expect.objectContaining({
            method: 'POST',
            body: JSON.stringify({ action: 'suspend' }),
          })
        );
      });
    });

    test('modal closes after successful action', async () => {
      const user = userEvent.setup();
      const mockAlerts = [createMockAlert('alert-close', { userName: 'Closeable User' })];

      mockSuccessfulFetch(mockAlerts, [], [{ alertId: 'alert-close', userName: 'Closeable User', riskScore: 0.8, alertType: 'ReviewGaming', createdAt: new Date().toISOString(), priority: 'High' }]);

      render(<AntiGamingDashboard />);

      await waitFor(() => {
        const userElements = screen.getAllByText('Closeable User');
        expect(userElements.length).toBeGreaterThan(0);
      });

      const reviewButton = screen.getAllByText('Review')[0];
      await user.click(reviewButton);

      await waitFor(() => {
        expect(screen.getByText('Alert Review')).toBeInTheDocument();
      });

      // Mock dismiss API call and reload
      (global.fetch as jest.Mock).mockResolvedValueOnce({
        ok: true,
        json: async () => ({}),
      });
      mockSuccessfulFetch(mockAlerts, [], []);

      const dismissButton = screen.getByText('Dismiss');
      await user.click(dismissButton);

      // Verify modal closes
      await waitFor(() => {
        expect(screen.queryByText('Alert Review')).not.toBeInTheDocument();
      });
    });
  });

  // ===================================================================
  // 4. Loading & Error States (2 tests)
  // ===================================================================

  describe('Loading & Error States', () => {
    test('displays loading skeleton during data fetch', () => {
      // Don't resolve the promises immediately
      (global.fetch as jest.Mock).mockReturnValue(new Promise(() => {}));

      render(<AntiGamingDashboard />);

      // Verify loading skeleton is shown
      expect(screen.getByText((content, element) => {
        return element?.className?.includes('animate-pulse') ?? false;
      })).toBeInTheDocument();
    });

    test('displays empty states when no data', async () => {
      mockSuccessfulFetch([], [], []);

      render(<AntiGamingDashboard />);

      await waitFor(() => {
        expect(screen.getByText('No pending reviews')).toBeInTheDocument();
        expect(screen.getByText('No alerts to display')).toBeInTheDocument();
      });
    });
  });

  // ===================================================================
  // 5. Priority & Risk Score Display (2 tests)
  // ===================================================================

  describe('Priority & Risk Score Display', () => {
    test('priority color coding (Critical, High, Medium, Low)', async () => {
      const mockReviews = [
        createMockReview('1', { priority: 'Critical' }),
        createMockReview('2', { priority: 'High' }),
        createMockReview('3', { priority: 'Medium' }),
        createMockReview('4', { priority: 'Low' }),
      ];

      mockSuccessfulFetch([], [], mockReviews);

      render(<AntiGamingDashboard />);

      await waitFor(() => {
        expect(screen.getByText('Critical')).toBeInTheDocument();
        expect(screen.getByText('High')).toBeInTheDocument();
        expect(screen.getByText('Medium')).toBeInTheDocument();
        expect(screen.getByText('Low')).toBeInTheDocument();
      });

      // Verify critical has destructive styling
      const criticalBadge = screen.getByText('Critical');
      expect(criticalBadge).toHaveClass('text-destructive');

      // Verify medium has warning styling
      const mediumBadge = screen.getByText('Medium');
      expect(mediumBadge).toHaveClass('text-warning');
    });

    test('risk score percentage displayed correctly', async () => {
      const mockAlerts = [
        createMockAlert('1', { riskScore: 0.856 }), // Should display as 85% or 85.6%
        createMockAlert('2', { riskScore: 0.123 }), // Should display as 12% or 12.3%
      ];

      mockSuccessfulFetch(mockAlerts, [], []);

      render(<AntiGamingDashboard />);

      await waitFor(() => {
        // Risk scores are displayed as percentages (allowing for 85% or 86% due to rounding)
        expect(screen.getByText(/8[56]%/)).toBeInTheDocument();
        expect(screen.getByText(/12%/)).toBeInTheDocument();
      });
    });
  });
});
