/**
 * BadgeProgress.tsx Tests
 *
 * Tests for badge progress tracking component.
 * Coverage Target: 80%+
 */

import React from 'react';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import '@testing-library/jest-dom';
import BadgeProgress from '../BadgeProgress';
import { BadgeCategory, BadgeProgress as BadgeProgressType } from '@/types/badge';

// Mock Next.js Image
jest.mock('next/image', () => ({
  __esModule: true,
  default: ({ src, alt, onError, ...props }: any) => {
    return (
      <img
        src={src}
        alt={alt}
        onError={onError}
        data-testid="badge-image"
        {...props}
      />
    );
  },
}));

// Mock lucide-react icons
jest.mock('lucide-react', () => ({
  ChevronRight: () => <div data-testid="chevron-right-icon">ChevronRight</div>,
  Target: () => <div data-testid="target-icon">Target</div>,
  TrendingUp: () => <div data-testid="trending-up-icon">TrendingUp</div>,
  Clock: () => <div data-testid="clock-icon">Clock</div>,
  CheckCircle: () => <div data-testid="check-circle-icon">CheckCircle</div>,
  Lock: () => <div data-testid="lock-icon">Lock</div>,
}));

// Mock UI components
jest.mock('../../ui/badge', () => ({
  Badge: ({ children, variant, className }: any) => (
    <span data-testid="badge" data-variant={variant} className={className}>
      {children}
    </span>
  ),
}));

jest.mock('../../ui/button', () => ({
  Button: ({ children, onClick, variant, size, disabled, className }: any) => (
    <button
      data-testid="button"
      onClick={onClick}
      data-variant={variant}
      data-size={size}
      disabled={disabled}
      className={className}
    >
      {children}
    </button>
  ),
}));

jest.mock('../../ui/progress', () => ({
  Progress: ({ value, className }: any) => (
    <div data-testid="progress" data-value={value} className={className}>
      Progress: {value}%
    </div>
  ),
}));

// Mock logger
jest.mock('@/utils/logger', () => ({
  logger: {
    error: jest.fn(),
  },
}));

// Mock alert
global.alert = jest.fn();

describe('BadgeProgress', () => {
  const createMockProgress = (overrides: Partial<BadgeProgressType> = {}): BadgeProgressType => ({
    badgeType: 'test-badge',
    badgeName: 'Test Badge',
    description: 'A test badge for testing',
    category: BadgeCategory.Performance,
    currentProgress: 5,
    maxProgress: 10,
    progressPercentage: 50,
    isEligible: true,
    requirements: ['Complete 10 projects', 'Earn 100 credits'],
    iconUrl: '/badges/test-badge.svg',
    nextMilestone: 'Complete 5 more projects',
    ...overrides,
  });

  beforeEach(() => {
    jest.clearAllMocks();
    global.fetch = jest.fn();
  });

  describe('Empty State', () => {
    it('shows empty state when no progress data', () => {
      render(<BadgeProgress progress={[]} userId="user-1" />);

      expect(screen.getByText('🎯')).toBeInTheDocument();
      expect(screen.getByText('No badge progress found')).toBeInTheDocument();
      expect(screen.getByText(/Start completing projects/)).toBeInTheDocument();
    });

    it('still shows filters when no data', () => {
      render(<BadgeProgress progress={[]} userId="user-1" />);

      // Filters are always shown
      expect(screen.getByText('All Categories')).toBeInTheDocument();
      expect(screen.getByText('Sort by Progress')).toBeInTheDocument();
    });
  });

  describe('Header Stats', () => {
    it('shows correct stats for ready to claim badges', () => {
      const progress = [
        createMockProgress({ badgeType: '1', currentProgress: 10, maxProgress: 10, isEligible: true }),
        createMockProgress({ badgeType: '2', currentProgress: 5, maxProgress: 10 }),
      ];

      render(<BadgeProgress progress={progress} userId="user-1" />);

      expect(screen.getByText('1 ready to claim')).toBeInTheDocument();
      expect(screen.getByText('1 in progress')).toBeInTheDocument();
      expect(screen.getByText('2 total badges')).toBeInTheDocument();
    });

    it('shows correct stats for in progress badges', () => {
      const progress = [
        createMockProgress({ badgeType: '1', currentProgress: 3, maxProgress: 10 }),
        createMockProgress({ badgeType: '2', currentProgress: 7, maxProgress: 10 }),
      ];

      render(<BadgeProgress progress={progress} userId="user-1" />);

      expect(screen.getByText('0 ready to claim')).toBeInTheDocument();
      expect(screen.getByText('2 in progress')).toBeInTheDocument();
    });

    it('does not count zero progress as in progress', () => {
      const progress = [
        createMockProgress({ badgeType: '1', currentProgress: 0, maxProgress: 10 }),
        createMockProgress({ badgeType: '2', currentProgress: 5, maxProgress: 10 }),
      ];

      render(<BadgeProgress progress={progress} userId="user-1" />);

      expect(screen.getByText('1 in progress')).toBeInTheDocument();
    });
  });

  describe('Badge Progress Card', () => {
    it('renders badge with correct name and description', () => {
      const progress = [createMockProgress()];

      render(<BadgeProgress progress={progress} userId="user-1" />);

      expect(screen.getByText('Test Badge')).toBeInTheDocument();
      expect(screen.getByText('A test badge for testing')).toBeInTheDocument();
    });

    it('renders badge category', () => {
      const progress = [createMockProgress({ category: BadgeCategory.Trust })];

      render(<BadgeProgress progress={progress} userId="user-1" />);

      // "Trust" appears in both dropdown and badge - use getAllByText
      const trustElements = screen.getAllByText('Trust');
      expect(trustElements.length).toBeGreaterThan(0);
    });

    it('calculates progress percentage correctly', () => {
      const progress = [createMockProgress({ currentProgress: 7, maxProgress: 10 })];

      render(<BadgeProgress progress={progress} userId="user-1" />);

      expect(screen.getByText('7/10')).toBeInTheDocument();
      expect(screen.getByText('70% complete')).toBeInTheDocument();
    });

    it('caps progress percentage at 100%', () => {
      const progress = [createMockProgress({ currentProgress: 15, maxProgress: 10 })];

      render(<BadgeProgress progress={progress} userId="user-1" />);

      expect(screen.getByText('100% complete')).toBeInTheDocument();
    });

    it('shows checkmark icon when complete', () => {
      const progress = [createMockProgress({ currentProgress: 10, maxProgress: 10 })];

      render(<BadgeProgress progress={progress} userId="user-1" />);

      const checkIcons = screen.getAllByTestId('check-circle-icon');
      expect(checkIcons.length).toBeGreaterThan(0);
    });

    it('shows lock icon when not eligible and low progress', () => {
      const progress = [createMockProgress({ currentProgress: 0, maxProgress: 10, isEligible: false })];

      render(<BadgeProgress progress={progress} userId="user-1" />);

      expect(screen.getAllByTestId('lock-icon').length).toBeGreaterThan(0);
      expect(screen.getByText('Complete prerequisites to unlock')).toBeInTheDocument();
    });

    it('does not show lock icon when progress is >= 10%', () => {
      const progress = [createMockProgress({ currentProgress: 1, maxProgress: 10, isEligible: false })];

      render(<BadgeProgress progress={progress} userId="user-1" />);

      expect(screen.queryByText('Complete prerequisites to unlock')).not.toBeInTheDocument();
    });

    it('shows "Ready to claim!" message when complete', () => {
      const progress = [createMockProgress({ currentProgress: 10, maxProgress: 10 })];

      render(<BadgeProgress progress={progress} userId="user-1" />);

      expect(screen.getByText('Ready to claim!')).toBeInTheDocument();
    });
  });

  describe('Next Milestone', () => {
    it('shows next milestone when not complete', () => {
      const progress = [createMockProgress({ nextMilestone: 'Complete 3 more projects' })];

      render(<BadgeProgress progress={progress} userId="user-1" />);

      expect(screen.getByText('Next milestone:')).toBeInTheDocument();
      expect(screen.getByText('Complete 3 more projects')).toBeInTheDocument();
      expect(screen.getByTestId('target-icon')).toBeInTheDocument();
    });

    it('does not show next milestone when complete', () => {
      const progress = [
        createMockProgress({
          currentProgress: 10,
          maxProgress: 10,
          nextMilestone: 'Should not show',
        }),
      ];

      render(<BadgeProgress progress={progress} userId="user-1" />);

      expect(screen.queryByText('Next milestone:')).not.toBeInTheDocument();
      expect(screen.queryByText('Should not show')).not.toBeInTheDocument();
    });

    it('does not show next milestone section when no milestone provided', () => {
      const progress = [createMockProgress({ nextMilestone: undefined })];

      render(<BadgeProgress progress={progress} userId="user-1" />);

      expect(screen.queryByText('Next milestone:')).not.toBeInTheDocument();
    });
  });

  describe('Requirements', () => {
    it('shows all requirements when 3 or fewer', () => {
      const progress = [
        createMockProgress({
          requirements: ['Requirement 1', 'Requirement 2', 'Requirement 3'],
        }),
      ];

      render(<BadgeProgress progress={progress} userId="user-1" />);

      expect(screen.getByText('Requirement 1')).toBeInTheDocument();
      expect(screen.getByText('Requirement 2')).toBeInTheDocument();
      expect(screen.getByText('Requirement 3')).toBeInTheDocument();
      expect(screen.queryByText(/more requirements/)).not.toBeInTheDocument();
    });

    it('shows first 3 requirements and count when more than 3', () => {
      const progress = [
        createMockProgress({
          requirements: ['Req 1', 'Req 2', 'Req 3', 'Req 4', 'Req 5'],
        }),
      ];

      render(<BadgeProgress progress={progress} userId="user-1" />);

      expect(screen.getByText('Req 1')).toBeInTheDocument();
      expect(screen.getByText('Req 2')).toBeInTheDocument();
      expect(screen.getByText('Req 3')).toBeInTheDocument();
      expect(screen.getByText('+2 more requirements')).toBeInTheDocument();
      expect(screen.queryByText('Req 4')).not.toBeInTheDocument();
    });

    it('does not show requirements section when empty', () => {
      const progress = [createMockProgress({ requirements: [] })];

      render(<BadgeProgress progress={progress} userId="user-1" />);

      expect(screen.queryByText('Requirements')).not.toBeInTheDocument();
    });
  });

  describe('Verification Button', () => {
    it('shows verification button when eligible and complete', () => {
      const progress = [
        createMockProgress({ currentProgress: 10, maxProgress: 10, isEligible: true }),
      ];

      render(<BadgeProgress progress={progress} userId="user-1" />);

      expect(screen.getByText('Request Verification')).toBeInTheDocument();
    });

    it('does not show verification button when not complete', () => {
      const progress = [
        createMockProgress({ currentProgress: 5, maxProgress: 10, isEligible: true }),
      ];

      render(<BadgeProgress progress={progress} userId="user-1" />);

      expect(screen.queryByText('Request Verification')).not.toBeInTheDocument();
    });

    it('does not show verification button when not eligible', () => {
      const progress = [
        createMockProgress({ currentProgress: 10, maxProgress: 10, isEligible: false }),
      ];

      render(<BadgeProgress progress={progress} userId="user-1" />);

      expect(screen.queryByText('Request Verification')).not.toBeInTheDocument();
    });

    it('calls verification API when button clicked', async () => {
      const mockFetch = global.fetch as jest.Mock;
      mockFetch.mockResolvedValueOnce({ ok: true });

      const progress = [
        createMockProgress({ badgeType: 'test-badge', currentProgress: 10, maxProgress: 10 }),
      ];

      render(<BadgeProgress progress={progress} userId="user-1" />);

      const button = screen.getByText('Request Verification');
      fireEvent.click(button);

      await waitFor(() => {
        expect(mockFetch).toHaveBeenCalledWith(
          '/api/badge/verification/request',
          expect.objectContaining({
            method: 'POST',
            headers: expect.objectContaining({
              'Content-Type': 'application/json',
            }),
            body: JSON.stringify({
              badgeType: 'test-badge',
              evidence: {},
            }),
          })
        );
      });
    });

    it('shows success alert when verification request succeeds', async () => {
      const mockFetch = global.fetch as jest.Mock;
      mockFetch
        .mockResolvedValueOnce({ ok: true }) // verification request
        .mockResolvedValueOnce({ ok: true, json: async () => [] }); // refresh progress

      const progress = [createMockProgress({ currentProgress: 10, maxProgress: 10 })];

      render(<BadgeProgress progress={progress} userId="user-1" />);

      fireEvent.click(screen.getByText('Request Verification'));

      await waitFor(() => {
        expect(global.alert).toHaveBeenCalledWith('Verification request submitted successfully!');
      });
    });

    it('shows error alert when verification request fails', async () => {
      const mockFetch = global.fetch as jest.Mock;
      mockFetch.mockResolvedValueOnce({ ok: false, text: async () => 'Error message' });

      const progress = [createMockProgress({ currentProgress: 10, maxProgress: 10 })];

      render(<BadgeProgress progress={progress} userId="user-1" />);

      fireEvent.click(screen.getByText('Request Verification'));

      await waitFor(() => {
        expect(global.alert).toHaveBeenCalledWith('Failed to submit verification request: Error message');
      });
    });

    it('shows error alert when verification throws exception', async () => {
      const mockFetch = global.fetch as jest.Mock;
      mockFetch.mockRejectedValueOnce(new Error('Network error'));

      const progress = [createMockProgress({ currentProgress: 10, maxProgress: 10 })];

      render(<BadgeProgress progress={progress} userId="user-1" />);

      fireEvent.click(screen.getByText('Request Verification'));

      await waitFor(() => {
        expect(global.alert).toHaveBeenCalledWith('An error occurred while submitting your verification request.');
      });
    });
  });

  describe('Refresh Progress', () => {
    it('calls refresh API when refresh button clicked', async () => {
      const mockFetch = global.fetch as jest.Mock;
      mockFetch.mockResolvedValueOnce({
        ok: true,
        json: async () => [createMockProgress()],
      });

      const progress = [createMockProgress()];

      render(<BadgeProgress progress={progress} userId="user-1" />);

      fireEvent.click(screen.getByText('Refresh Progress'));

      await waitFor(() => {
        expect(mockFetch).toHaveBeenCalledWith(
          '/api/badge/user/user-1/progress',
          expect.objectContaining({
            credentials: 'include',
          })
        );
      });
    });

    it('shows loading state when refreshing', async () => {
      const mockFetch = global.fetch as jest.Mock;
      mockFetch.mockImplementation(() => new Promise(() => {})); // Never resolves

      const progress = [createMockProgress()];

      render(<BadgeProgress progress={progress} userId="user-1" />);

      fireEvent.click(screen.getByText('Refresh Progress'));

      await waitFor(() => {
        expect(screen.getByText('Refreshing...')).toBeInTheDocument();
        expect(screen.getByRole('button', { name: /Refreshing/ })).toBeDisabled();
      });
    });

    it('updates progress data after successful refresh', async () => {
      const mockFetch = global.fetch as jest.Mock;
      const newProgress = createMockProgress({ badgeName: 'Updated Badge' });
      mockFetch.mockResolvedValueOnce({
        ok: true,
        json: async () => [newProgress],
      });

      const progress = [createMockProgress({ badgeName: 'Original Badge' })];

      render(<BadgeProgress progress={progress} userId="user-1" />);

      expect(screen.getByText('Original Badge')).toBeInTheDocument();

      fireEvent.click(screen.getByText('Refresh Progress'));

      await waitFor(() => {
        expect(screen.getByText('Updated Badge')).toBeInTheDocument();
        expect(screen.queryByText('Original Badge')).not.toBeInTheDocument();
      });
    });

    it('handles refresh API error gracefully', async () => {
      const mockFetch = global.fetch as jest.Mock;
      mockFetch.mockRejectedValueOnce(new Error('Network error'));

      const { logger } = require('@/utils/logger');
      const progress = [createMockProgress()];

      render(<BadgeProgress progress={progress} userId="user-1" />);

      fireEvent.click(screen.getByText('Refresh Progress'));

      await waitFor(() => {
        expect(logger.error).toHaveBeenCalledWith('Failed to refresh badge progress:', expect.any(Error));
      });
    });

    it('does not update data when refresh fails', async () => {
      const mockFetch = global.fetch as jest.Mock;
      mockFetch.mockResolvedValueOnce({ ok: false });

      const progress = [createMockProgress({ badgeName: 'Original Badge' })];

      render(<BadgeProgress progress={progress} userId="user-1" />);

      fireEvent.click(screen.getByText('Refresh Progress'));

      await waitFor(() => {
        expect(screen.getByText('Original Badge')).toBeInTheDocument();
      });
    });
  });

  describe('Category Filtering', () => {
    it('shows all categories in dropdown', () => {
      const progress = [createMockProgress({ category: BadgeCategory.Volume })]; // Use different category

      const { container } = render(<BadgeProgress progress={progress} userId="user-1" />);

      // Query within the category select element
      const categorySelect = container.querySelectorAll('select')[0];

      expect(categorySelect.textContent).toContain('All Categories');
      expect(categorySelect.textContent).toContain('Performance');
      expect(categorySelect.textContent).toContain('Volume');
      expect(categorySelect.textContent).toContain('Expertise');
      expect(categorySelect.textContent).toContain('Trust');
      expect(categorySelect.textContent).toContain('Community');
      expect(categorySelect.textContent).toContain('Achievement');
    });

    it('filters badges by selected category', () => {
      const progress = [
        createMockProgress({ badgeType: '1', badgeName: 'Performance Badge', category: BadgeCategory.Performance }),
        createMockProgress({ badgeType: '2', badgeName: 'Trust Badge', category: BadgeCategory.Trust }),
      ];

      render(<BadgeProgress progress={progress} userId="user-1" />);

      expect(screen.getByText('Performance Badge')).toBeInTheDocument();
      expect(screen.getByText('Trust Badge')).toBeInTheDocument();

      // Filter to Performance
      const categorySelect = screen.getAllByRole('combobox')[0];
      fireEvent.change(categorySelect, { target: { value: BadgeCategory.Performance } });

      expect(screen.getByText('Performance Badge')).toBeInTheDocument();
      expect(screen.queryByText('Trust Badge')).not.toBeInTheDocument();
    });
  });

  describe('Sorting', () => {
    it('sorts by progress (descending) by default', () => {
      const progress = [
        createMockProgress({ badgeType: '1', badgeName: 'Badge A', progressPercentage: 30 }),
        createMockProgress({ badgeType: '2', badgeName: 'Badge B', progressPercentage: 70 }),
        createMockProgress({ badgeType: '3', badgeName: 'Badge C', progressPercentage: 50 }),
      ];

      const { container } = render(<BadgeProgress progress={progress} userId="user-1" />);

      // Get all badge cards in order
      const badgeCards = container.querySelectorAll('.bg-card.rounded-lg');
      const firstCardText = badgeCards[0].textContent;

      expect(firstCardText).toContain('Badge B'); // 70% should be first
    });

    it('sorts by category when selected', () => {
      const progress = [
        createMockProgress({ badgeType: '1', badgeName: 'Badge Z', category: BadgeCategory.Trust }),
        createMockProgress({ badgeType: '2', badgeName: 'Badge A', category: BadgeCategory.Performance }),
      ];

      const { container } = render(<BadgeProgress progress={progress} userId="user-1" />);

      const sortSelect = screen.getAllByRole('combobox')[1];
      fireEvent.change(sortSelect, { target: { value: 'category' } });

      const badgeCards = container.querySelectorAll('.bg-card.rounded-lg');
      const firstCardText = badgeCards[0].textContent;

      expect(firstCardText).toContain('Badge A'); // Performance comes before Trust
    });

    it('sorts by name when selected', () => {
      const progress = [
        createMockProgress({ badgeType: '1', badgeName: 'Zebra Badge' }),
        createMockProgress({ badgeType: '2', badgeName: 'Apple Badge' }),
      ];

      const { container } = render(<BadgeProgress progress={progress} userId="user-1" />);

      const sortSelect = screen.getAllByRole('combobox')[1];
      fireEvent.change(sortSelect, { target: { value: 'name' } });

      const badgeCards = container.querySelectorAll('.bg-card.rounded-lg');
      const firstCardText = badgeCards[0].textContent;

      expect(firstCardText).toContain('Apple Badge');
    });
  });

  describe('Image Handling', () => {
    it('uses iconUrl when provided', () => {
      const progress = [createMockProgress({ iconUrl: '/custom-icon.svg' })];

      render(<BadgeProgress progress={progress} userId="user-1" />);

      const image = screen.getByTestId('badge-image');
      expect(image).toHaveAttribute('src', '/custom-icon.svg');
    });

    it('uses fallback icon path when iconUrl not provided', () => {
      const progress = [createMockProgress({ badgeType: 'my-badge', iconUrl: undefined })];

      render(<BadgeProgress progress={progress} userId="user-1" />);

      const image = screen.getByTestId('badge-image');
      expect(image).toHaveAttribute('src', '/badges/my-badge.svg');
    });

    it('handles image error gracefully', () => {
      const progress = [createMockProgress()];

      render(<BadgeProgress progress={progress} userId="user-1" />);

      const image = screen.getByTestId('badge-image');
      fireEvent.error(image);

      expect(image.style.display).toBe('none');
    });
  });
});
