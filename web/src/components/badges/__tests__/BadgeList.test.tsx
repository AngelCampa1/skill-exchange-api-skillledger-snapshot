/**
 * BadgeList.tsx Tests
 *
 * Tests for badge list component with filtering, sorting, and grouping.
 * Coverage Target: 80%+
 */

import React from 'react';
import { render, screen, fireEvent, within } from '@testing-library/react';
import '@testing-library/jest-dom';
import BadgeList from '../BadgeList';
import { BadgeCategory, VerificationLevel, UserBadge } from '@/types/badge';

// Mock dependencies
jest.mock('lucide-react', () => ({
  Filter: () => <div data-testid="filter-icon">Filter Icon</div>,
  ChevronDown: () => <div data-testid="chevron-down-icon">ChevronDown Icon</div>,
  ChevronUp: () => <div data-testid="chevron-up-icon">ChevronUp Icon</div>,
}));

jest.mock('../../ui/badge', () => ({
  Badge: ({ children, variant, className }: any) => (
    <span data-testid="badge" data-variant={variant} className={className}>
      {children}
    </span>
  ),
}));

jest.mock('../../ui/button', () => ({
  Button: ({ children, onClick, variant, size, className }: any) => (
    <button
      data-testid="button"
      onClick={onClick}
      data-variant={variant}
      data-size={size}
      className={className}
    >
      {children}
    </button>
  ),
}));

jest.mock('../BadgeDisplay', () => ({
  __esModule: true,
  default: ({ badge, size, showDetails, onClick }: any) => (
    <div
      data-testid={`badge-display-${badge.id}`}
      data-badge-name={badge.badgeName}
      data-category={badge.category}
      onClick={onClick}
    >
      {badge.badgeName}
    </div>
  ),
}));

jest.mock('@/utils/logger', () => ({
  logger: {
    debug: jest.fn(),
  },
}));

// Helper to create mock badges
const createMockBadge = (overrides: Partial<UserBadge> = {}): UserBadge => ({
  id: `badge-${Math.random()}`,
  userId: 'user-1',
  badgeType: 'test-badge',
  badgeName: 'Test Badge',
  badgeDescription: 'A test badge',
  category: BadgeCategory.Performance,
  earnedAt: '2024-01-01T00:00:00Z',
  isActive: true,
  verificationLevel: VerificationLevel.Automatic,
  ...overrides,
});

describe('BadgeList', () => {
  describe('Empty State', () => {
    it('shows empty state when no badges provided', () => {
      render(<BadgeList badges={[]} />);

      expect(screen.getByText('No badges yet')).toBeInTheDocument();
      expect(screen.getByText(/Complete projects and build your reputation/i)).toBeInTheDocument();
      expect(screen.getByText('🏆')).toBeInTheDocument();
    });

    it('does not show filters when no badges', () => {
      render(<BadgeList badges={[]} />);

      expect(screen.queryByText('All Categories')).not.toBeInTheDocument();
      expect(screen.queryByText('Recently Earned')).not.toBeInTheDocument();
    });
  });

  describe('Rendering with Badges', () => {
    it('renders badge count in header', () => {
      const badges = [
        createMockBadge({ id: '1' }),
        createMockBadge({ id: '2' }),
        createMockBadge({ id: '3' }),
      ];

      render(<BadgeList badges={badges} />);

      expect(screen.getByText('Badges (3)')).toBeInTheDocument();
    });

    it('shows active and expired badge counts', () => {
      const badges = [
        createMockBadge({ id: '1', isActive: true }),
        createMockBadge({ id: '2', isActive: true }),
        createMockBadge({ id: '3', isActive: false }),
      ];

      render(<BadgeList badges={badges} showExpired={true} />);

      expect(screen.getByText('2 Active')).toBeInTheDocument();
      expect(screen.getByText('1 Expired')).toBeInTheDocument();
    });

    it('does not show expired count when all badges are active', () => {
      const badges = [
        createMockBadge({ id: '1', isActive: true }),
        createMockBadge({ id: '2', isActive: true }),
      ];

      render(<BadgeList badges={badges} />);

      expect(screen.getByText('2 Active')).toBeInTheDocument();
      // Should not show an "X Expired" badge count (but "Show Expired" button is okay)
      expect(screen.queryByText(/^\d+ Expired$/)).not.toBeInTheDocument();
    });

    it('renders badges in grid when groupByCategory is false', () => {
      const badges = [
        createMockBadge({ id: '1', badgeName: 'Badge 1' }),
        createMockBadge({ id: '2', badgeName: 'Badge 2' }),
      ];

      render(<BadgeList badges={badges} groupByCategory={false} />);

      expect(screen.getByTestId('badge-display-1')).toBeInTheDocument();
      expect(screen.getByTestId('badge-display-2')).toBeInTheDocument();
    });
  });

  describe('Category Filtering', () => {
    it('shows all categories in dropdown', () => {
      const badges = [createMockBadge()];

      render(<BadgeList badges={badges} />);

      const select = screen.getByDisplayValue('All Categories');
      const options = within(select as HTMLElement).getAllByRole('option');

      expect(options[0]).toHaveTextContent('All Categories');
      expect(options.length).toBe(7); // 1 "All" + 6 categories
    });

    it('filters badges by selected category', () => {
      const badges = [
        createMockBadge({ id: '1', category: BadgeCategory.Performance, badgeName: 'Performance Badge' }),
        createMockBadge({ id: '2', category: BadgeCategory.Trust, badgeName: 'Trust Badge' }),
        createMockBadge({ id: '3', category: BadgeCategory.Community, badgeName: 'Community Badge' }),
      ];

      render(<BadgeList badges={badges} groupByCategory={false} />);

      // All badges shown initially
      expect(screen.getByText('Badges (3)')).toBeInTheDocument();

      // Select Performance category
      const categorySelect = screen.getByDisplayValue('All Categories');
      fireEvent.change(categorySelect, { target: { value: BadgeCategory.Performance } });

      // Only Performance badge shown
      expect(screen.getByText('Badges (1)')).toBeInTheDocument();
      expect(screen.getByTestId('badge-display-1')).toBeInTheDocument();
      expect(screen.queryByTestId('badge-display-2')).not.toBeInTheDocument();
    });

    it('uses initial category prop', () => {
      const badges = [
        createMockBadge({ id: '1', category: BadgeCategory.Performance }),
        createMockBadge({ id: '2', category: BadgeCategory.Trust }),
      ];

      render(<BadgeList badges={badges} category={BadgeCategory.Trust} groupByCategory={false} />);

      expect(screen.getByText('Badges (1)')).toBeInTheDocument();
      expect(screen.queryByTestId('badge-display-1')).not.toBeInTheDocument();
      expect(screen.getByTestId('badge-display-2')).toBeInTheDocument();
    });
  });

  describe('Sorting', () => {
    it('sorts by recently earned (descending) by default', () => {
      const badges = [
        createMockBadge({ id: '1', earnedAt: '2024-01-01T00:00:00Z', badgeName: 'Old Badge' }),
        createMockBadge({ id: '2', earnedAt: '2024-03-01T00:00:00Z', badgeName: 'New Badge' }),
        createMockBadge({ id: '3', earnedAt: '2024-02-01T00:00:00Z', badgeName: 'Mid Badge' }),
      ];

      render(<BadgeList badges={badges} groupByCategory={false} />);

      const badgeDisplays = screen.getAllByTestId(/badge-display-/);
      expect(badgeDisplays[0]).toHaveAttribute('data-testid', 'badge-display-2');
      expect(badgeDisplays[1]).toHaveAttribute('data-testid', 'badge-display-3');
      expect(badgeDisplays[2]).toHaveAttribute('data-testid', 'badge-display-1');
    });

    it('sorts by oldest first when selected', () => {
      const badges = [
        createMockBadge({ id: '1', earnedAt: '2024-01-01T00:00:00Z' }),
        createMockBadge({ id: '2', earnedAt: '2024-03-01T00:00:00Z' }),
      ];

      render(<BadgeList badges={badges} groupByCategory={false} />);

      const sortSelect = screen.getByDisplayValue('Recently Earned');
      fireEvent.change(sortSelect, { target: { value: 'earned-asc' } });

      const badgeDisplays = screen.getAllByTestId(/badge-display-/);
      expect(badgeDisplays[0]).toHaveAttribute('data-testid', 'badge-display-1');
      expect(badgeDisplays[1]).toHaveAttribute('data-testid', 'badge-display-2');
    });

    it('sorts by name A-Z', () => {
      const badges = [
        createMockBadge({ id: '1', badgeName: 'Zebra Badge' }),
        createMockBadge({ id: '2', badgeName: 'Apple Badge' }),
        createMockBadge({ id: '3', badgeName: 'Banana Badge' }),
      ];

      render(<BadgeList badges={badges} groupByCategory={false} />);

      const sortSelect = screen.getByDisplayValue('Recently Earned');
      fireEvent.change(sortSelect, { target: { value: 'name-asc' } });

      const badgeDisplays = screen.getAllByTestId(/badge-display-/);
      expect(badgeDisplays[0]).toHaveAttribute('data-badge-name', 'Apple Badge');
      expect(badgeDisplays[1]).toHaveAttribute('data-badge-name', 'Banana Badge');
      expect(badgeDisplays[2]).toHaveAttribute('data-badge-name', 'Zebra Badge');
    });

    it('sorts by name Z-A', () => {
      const badges = [
        createMockBadge({ id: '1', badgeName: 'Apple Badge' }),
        createMockBadge({ id: '2', badgeName: 'Zebra Badge' }),
      ];

      render(<BadgeList badges={badges} groupByCategory={false} />);

      const sortSelect = screen.getByDisplayValue('Recently Earned');
      fireEvent.change(sortSelect, { target: { value: 'name-desc' } });

      const badgeDisplays = screen.getAllByTestId(/badge-display-/);
      expect(badgeDisplays[0]).toHaveAttribute('data-badge-name', 'Zebra Badge');
      expect(badgeDisplays[1]).toHaveAttribute('data-badge-name', 'Apple Badge');
    });

    it('sorts by category A-Z', () => {
      const badges = [
        createMockBadge({ id: '1', category: BadgeCategory.Trust }),
        createMockBadge({ id: '2', category: BadgeCategory.Achievement }),
        createMockBadge({ id: '3', category: BadgeCategory.Performance }),
      ];

      render(<BadgeList badges={badges} groupByCategory={false} />);

      const sortSelect = screen.getByDisplayValue('Recently Earned');
      fireEvent.change(sortSelect, { target: { value: 'category-asc' } });

      const badgeDisplays = screen.getAllByTestId(/badge-display-/);
      expect(badgeDisplays[0]).toHaveAttribute('data-category', 'Achievement');
      expect(badgeDisplays[1]).toHaveAttribute('data-category', 'Performance');
      expect(badgeDisplays[2]).toHaveAttribute('data-category', 'Trust');
    });
  });

  describe('Expired Badge Filtering', () => {
    it('hides expired badges by default', () => {
      const badges = [
        createMockBadge({ id: '1', isActive: true, badgeName: 'Active' }),
        createMockBadge({ id: '2', isActive: false, badgeName: 'Inactive' }),
        createMockBadge({
          id: '3',
          isActive: true,
          expiresAt: '2020-01-01T00:00:00Z', // Expired
          badgeName: 'Expired',
        }),
      ];

      render(<BadgeList badges={badges} showExpired={false} groupByCategory={false} />);

      expect(screen.getByText('Badges (1)')).toBeInTheDocument();
      expect(screen.getByTestId('badge-display-1')).toBeInTheDocument();
      expect(screen.queryByTestId('badge-display-2')).not.toBeInTheDocument();
      expect(screen.queryByTestId('badge-display-3')).not.toBeInTheDocument();
    });

    it('shows all badges when showExpired is true', () => {
      const badges = [
        createMockBadge({ id: '1', isActive: true }),
        createMockBadge({ id: '2', isActive: false }),
      ];

      render(<BadgeList badges={badges} showExpired={true} groupByCategory={false} />);

      expect(screen.getByText('Badges (2)')).toBeInTheDocument();
      expect(screen.getByTestId('badge-display-1')).toBeInTheDocument();
      expect(screen.getByTestId('badge-display-2')).toBeInTheDocument();
    });

    it('considers expiresAt date for expiration', () => {
      const futureDate = new Date();
      futureDate.setFullYear(futureDate.getFullYear() + 1);

      const badges = [
        createMockBadge({
          id: '1',
          isActive: true,
          expiresAt: futureDate.toISOString(), // Future date - not expired
        }),
      ];

      render(<BadgeList badges={badges} showExpired={false} groupByCategory={false} />);

      expect(screen.getByText('Badges (1)')).toBeInTheDocument();
      expect(screen.getByTestId('badge-display-1')).toBeInTheDocument();
    });
  });

  describe('Grouping by Category', () => {
    it('groups badges by category when groupByCategory is true', () => {
      const badges = [
        createMockBadge({ id: '1', category: BadgeCategory.Performance }),
        createMockBadge({ id: '2', category: BadgeCategory.Trust }),
        createMockBadge({ id: '3', category: BadgeCategory.Performance }),
      ];

      render(<BadgeList badges={badges} groupByCategory={true} />);

      expect(screen.getByText('Performance (2)')).toBeInTheDocument();
      expect(screen.getByText('Trust (1)')).toBeInTheDocument();
    });

    it('shows category icons', () => {
      const badges = [
        createMockBadge({ id: '1', category: BadgeCategory.Performance }),
        createMockBadge({ id: '2', category: BadgeCategory.Volume }),
        createMockBadge({ id: '3', category: BadgeCategory.Expertise }),
        createMockBadge({ id: '4', category: BadgeCategory.Trust }),
        createMockBadge({ id: '5', category: BadgeCategory.Community }),
        createMockBadge({ id: '6', category: BadgeCategory.Achievement }),
      ];

      render(<BadgeList badges={badges} groupByCategory={true} />);

      expect(screen.getByText('⭐')).toBeInTheDocument(); // Performance
      expect(screen.getByText('🎯')).toBeInTheDocument(); // Volume
      expect(screen.getByText('🏆')).toBeInTheDocument(); // Expertise
      expect(screen.getByText('🛡️')).toBeInTheDocument(); // Trust
      expect(screen.getByText('👥')).toBeInTheDocument(); // Community
      expect(screen.getByText('🏅')).toBeInTheDocument(); // Achievement
    });

    it('expands all categories by default', () => {
      const badges = [
        createMockBadge({ id: '1', category: BadgeCategory.Performance }),
        createMockBadge({ id: '2', category: BadgeCategory.Trust }),
      ];

      render(<BadgeList badges={badges} groupByCategory={true} />);

      expect(screen.getByTestId('badge-display-1')).toBeInTheDocument();
      expect(screen.getByTestId('badge-display-2')).toBeInTheDocument();
    });

    it('collapses category when collapse button clicked', () => {
      const badges = [
        createMockBadge({ id: '1', category: BadgeCategory.Performance }),
        createMockBadge({ id: '2', category: BadgeCategory.Performance }),
      ];

      render(<BadgeList badges={badges} groupByCategory={true} />);

      expect(screen.getByTestId('badge-display-1')).toBeInTheDocument();

      // Find and click the collapse button for Performance category
      const collapseButtons = screen.getAllByText('Collapse');
      fireEvent.click(collapseButtons[0]);

      expect(screen.queryByTestId('badge-display-1')).not.toBeInTheDocument();
      expect(screen.getByText('Expand')).toBeInTheDocument();
    });

    it('expands collapsed category when expand button clicked', () => {
      const badges = [
        createMockBadge({ id: '1', category: BadgeCategory.Performance }),
      ];

      render(<BadgeList badges={badges} groupByCategory={true} />);

      // Collapse first
      const collapseButton = screen.getByText('Collapse');
      fireEvent.click(collapseButton);

      expect(screen.queryByTestId('badge-display-1')).not.toBeInTheDocument();

      // Expand
      const expandButton = screen.getByText('Expand');
      fireEvent.click(expandButton);

      expect(screen.getByTestId('badge-display-1')).toBeInTheDocument();
    });
  });

  describe('No Results State', () => {
    it('shows no results message when filters return no badges', () => {
      const badges = [
        createMockBadge({ id: '1', category: BadgeCategory.Performance }),
      ];

      render(<BadgeList badges={badges} category={BadgeCategory.Trust} groupByCategory={false} />);

      expect(screen.getByText('No badges found')).toBeInTheDocument();
      expect(screen.getByText(/Try adjusting your filters/i)).toBeInTheDocument();
      expect(screen.getByText('🔍')).toBeInTheDocument();
    });

    it('does not show no results when all badges match filters', () => {
      const badges = [
        createMockBadge({ id: '1', category: BadgeCategory.Performance }),
      ];

      render(<BadgeList badges={badges} category={BadgeCategory.Performance} groupByCategory={false} />);

      expect(screen.queryByText('No badges found')).not.toBeInTheDocument();
      expect(screen.getByTestId('badge-display-1')).toBeInTheDocument();
    });
  });

  describe('Badge Click Handling', () => {
    it('logs badge click to debug', () => {
      const { logger } = require('@/utils/logger');
      const badge = createMockBadge({ id: '1' });

      render(<BadgeList badges={[badge]} groupByCategory={false} />);

      const badgeDisplay = screen.getByTestId('badge-display-1');
      fireEvent.click(badgeDisplay);

      expect(logger.debug).toHaveBeenCalledWith('Badge clicked:', expect.objectContaining({ id: '1' }));
    });
  });
});
