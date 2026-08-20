/**
 * Project Search Page Tests
 *
 * Week 18 - Gap Filling: Testing highest-impact untested files
 * Target: 85%+ coverage
 *
 * Tests cover:
 * - Loading state
 * - Search results display
 * - Empty results handling
 * - Error handling
 * - Pagination
 * - Sort functionality
 * - Filter changes
 */

import React from 'react';
import { render, screen, waitFor, act } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import ProjectSearchPage from '../page';

// Mock next/navigation - use stable mock that doesn't change between renders
const mockPush = jest.fn();
const mockSearchParams = new URLSearchParams();
jest.mock('next/navigation', () => ({
  useRouter: jest.fn(() => ({
    push: mockPush,
    replace: jest.fn(),
    back: jest.fn(),
  })),
  useSearchParams: jest.fn(() => mockSearchParams),
}));

// Mock window.history.pushState to prevent state changes
const mockPushState = jest.fn();
Object.defineProperty(window, 'history', {
  writable: true,
  value: {
    ...window.history,
    pushState: mockPushState,
  },
});

// Mock ProjectSearchForm component
jest.mock('@/components/ProjectSearchForm', () => {
  return function MockProjectSearchForm({ onFiltersChange, isLoading }: any) {
    return (
      <div data-testid="search-form">
        <button
          onClick={() => onFiltersChange({ query: 'test', page: 1, pageSize: 20 })}
          disabled={isLoading}
        >
          Apply Filters
        </button>
        <input
          type="text"
          placeholder="Search..."
          onChange={(e) => onFiltersChange({ query: e.target.value, page: 1, pageSize: 20 })}
        />
      </div>
    );
  };
});

// Mock ThemeToggle
jest.mock('@/components/ThemeToggle', () => ({
  ThemeToggle: () => <button>Theme Toggle</button>,
}));

// Mock data
const mockProjects = [
  {
    id: 'proj-1',
    title: 'React Development Project',
    description: 'Build a modern web application',
    creditBudget: 500,
    status: 'Published',
    createdAt: '2024-01-15T00:00:00Z',
    endDate: '2024-06-15T00:00:00Z',
    isUrgent: true,
    isFeatured: false,
    location: { city: 'New York', state: 'NY', country: 'USA' },
    skills: [
      { skillId: 's1', skillName: 'React', proficiencyRequired: 3, weight: 1 },
      { skillId: 's2', skillName: 'TypeScript', proficiencyRequired: 2, weight: 1 },
    ],
    client: { id: 'c1', userName: 'johndoe', displayName: 'John Doe' },
  },
  {
    id: 'proj-2',
    title: 'Backend API Development',
    description: 'Create RESTful APIs',
    creditBudget: 800,
    status: 'Published',
    createdAt: '2024-01-10T00:00:00Z',
    endDate: '2024-07-01T00:00:00Z',
    isUrgent: false,
    isFeatured: true,
    location: { city: 'Remote' },
    requiredSkillNames: ['Node.js', 'PostgreSQL', 'Docker'],
    client: { id: 'c2', userName: 'jane@company.com' },
  },
];

const mockSearchResults = {
  projects: mockProjects,
  totalCount: 2,
  currentPage: 1,
  totalPages: 1,
  aggregations: {
    skillCounts: [],
    budgetRanges: [],
    locationCounts: [],
    statusCounts: [],
  },
};

const mockSkills = [
  { id: 's1', name: 'React', description: 'React framework', category: 'Frontend' },
  { id: 's2', name: 'TypeScript', description: 'TypeScript language', category: 'Language' },
];

describe('ProjectSearchPage', () => {
  let mockFetch: jest.Mock;

  beforeEach(() => {
    mockFetch = jest.fn();
    global.fetch = mockFetch;
    mockPush.mockClear();

    // Default mock responses
    mockFetch.mockImplementation((url: string, options?: RequestInit) => {
      if (url.includes('/api/skills')) {
        return Promise.resolve({
          ok: true,
          json: () => Promise.resolve(mockSkills),
        });
      }
      if (url.includes('/api/project-search/advanced')) {
        return Promise.resolve({
          ok: true,
          json: () => Promise.resolve(mockSearchResults),
        });
      }
      return Promise.resolve({ ok: false });
    });
  });

  afterEach(() => {
    jest.clearAllMocks();
  });

  // =========================================================================
  // Suite 1: Initial Render & Loading (4 tests)
  // =========================================================================
  describe('Initial Render & Loading', () => {
    test('renders page title', async () => {
      render(<ProjectSearchPage />);

      await waitFor(() => {
        expect(screen.getByText('Find Projects')).toBeInTheDocument();
      });
    });

    test('displays subtitle description', async () => {
      render(<ProjectSearchPage />);

      await waitFor(() => {
        expect(screen.getByText(/discover opportunities/i)).toBeInTheDocument();
      });
    });

    test('displays Back to Home link', async () => {
      render(<ProjectSearchPage />);

      await waitFor(() => {
        expect(screen.getByText('Back to Home')).toBeInTheDocument();
      });
    });

    test('shows loading spinner while searching', async () => {
      // Make fetch never resolve
      mockFetch.mockImplementation(() => new Promise(() => {}));

      const { container } = render(<ProjectSearchPage />);

      await waitFor(() => {
        const spinner = container.querySelector('.animate-spin');
        expect(spinner).toBeInTheDocument();
      });
    });
  });

  // =========================================================================
  // Suite 2: Search Results Display (6 tests)
  // =========================================================================
  describe('Search Results Display', () => {
    test('displays project titles', async () => {
      render(<ProjectSearchPage />);

      await waitFor(() => {
        expect(screen.getByText('React Development Project')).toBeInTheDocument();
        expect(screen.getByText('Backend API Development')).toBeInTheDocument();
      });
    });

    test('displays project descriptions', async () => {
      render(<ProjectSearchPage />);

      await waitFor(() => {
        expect(screen.getByText('Build a modern web application')).toBeInTheDocument();
        expect(screen.getByText('Create RESTful APIs')).toBeInTheDocument();
      });
    });

    test('displays project budget in credits', async () => {
      render(<ProjectSearchPage />);

      await waitFor(() => {
        expect(screen.getByText('500 credits')).toBeInTheDocument();
        expect(screen.getByText('800 credits')).toBeInTheDocument();
      });
    });

    test('displays urgent badge for urgent projects', async () => {
      render(<ProjectSearchPage />);

      await waitFor(() => {
        expect(screen.getByText('Urgent')).toBeInTheDocument();
      });
    });

    test('displays featured badge for featured projects', async () => {
      render(<ProjectSearchPage />);

      await waitFor(() => {
        expect(screen.getByText('Featured')).toBeInTheDocument();
      });
    });

    test('displays View Project buttons', async () => {
      render(<ProjectSearchPage />);

      await waitFor(() => {
        const viewButtons = screen.getAllByRole('button', { name: /view project/i });
        expect(viewButtons.length).toBe(2);
      });
    });
  });

  // =========================================================================
  // Suite 3: Skills Display (3 tests)
  // =========================================================================
  describe('Skills Display', () => {
    test('displays skills from skills array', async () => {
      render(<ProjectSearchPage />);

      await waitFor(() => {
        expect(screen.getByText('React')).toBeInTheDocument();
        expect(screen.getByText('TypeScript')).toBeInTheDocument();
      });
    });

    test('displays skills from requiredSkillNames array', async () => {
      render(<ProjectSearchPage />);

      await waitFor(() => {
        expect(screen.getByText('Node.js')).toBeInTheDocument();
        expect(screen.getByText('PostgreSQL')).toBeInTheDocument();
      });
    });

    test('displays proficiency stars for skills', async () => {
      render(<ProjectSearchPage />);

      await waitFor(() => {
        // React has proficiencyRequired: 3, should show 3 stars
        expect(screen.getByText('React')).toBeInTheDocument();
      });

      // Find the React skill badge and check for stars
      const reactBadge = screen.getByText('React').closest('span');
      expect(reactBadge?.textContent).toContain('★');
    });
  });

  // =========================================================================
  // Suite 4: Empty & Error States (4 tests)
  // =========================================================================
  describe('Empty & Error States', () => {
    test('displays empty state when no results', async () => {
      mockFetch.mockImplementation((url: string) => {
        if (url.includes('/api/skills')) {
          return Promise.resolve({
            ok: true,
            json: () => Promise.resolve(mockSkills),
          });
        }
        if (url.includes('/api/project-search/advanced')) {
          return Promise.resolve({
            ok: true,
            json: () => Promise.resolve({
              projects: [],
              totalCount: 0,
              currentPage: 1,
              totalPages: 0,
              aggregations: {},
            }),
          });
        }
        return Promise.resolve({ ok: false });
      });

      render(<ProjectSearchPage />);

      await waitFor(() => {
        // Multiple elements show "No projects found" - one in header, one in empty state
        const noResultsElements = screen.getAllByText('No projects found');
        expect(noResultsElements.length).toBeGreaterThan(0);
      });
    });

    test('displays adjustment hint when no results', async () => {
      mockFetch.mockImplementation((url: string) => {
        if (url.includes('/api/skills')) {
          return Promise.resolve({
            ok: true,
            json: () => Promise.resolve(mockSkills),
          });
        }
        if (url.includes('/api/project-search/advanced')) {
          return Promise.resolve({
            ok: true,
            json: () => Promise.resolve({
              projects: [],
              totalCount: 0,
              currentPage: 1,
              totalPages: 0,
              aggregations: {},
            }),
          });
        }
        return Promise.resolve({ ok: false });
      });

      render(<ProjectSearchPage />);

      await waitFor(() => {
        expect(screen.getByText(/try adjusting your search filters/i)).toBeInTheDocument();
      });
    });

    test('displays error message when search fails', async () => {
      mockFetch.mockImplementation((url: string) => {
        if (url.includes('/api/skills')) {
          return Promise.resolve({
            ok: true,
            json: () => Promise.resolve(mockSkills),
          });
        }
        if (url.includes('/api/project-search/advanced')) {
          return Promise.resolve({
            ok: false,
            statusText: 'Internal Server Error',
            json: () => Promise.resolve({ message: 'Search service unavailable' }),
          });
        }
        return Promise.resolve({ ok: false });
      });

      render(<ProjectSearchPage />);

      await waitFor(() => {
        expect(screen.getByText('Search Error')).toBeInTheDocument();
        expect(screen.getByText('Search service unavailable')).toBeInTheDocument();
      });
    });

    test('shows results count', async () => {
      render(<ProjectSearchPage />);

      await waitFor(() => {
        expect(screen.getByText(/showing 1 - 2 of 2 projects/i)).toBeInTheDocument();
      });
    });
  });

  // =========================================================================
  // Suite 5: Sorting (3 tests)
  // =========================================================================
  describe('Sorting', () => {
    test('displays sort dropdown', async () => {
      render(<ProjectSearchPage />);

      await waitFor(() => {
        expect(screen.getByText('Sort by:')).toBeInTheDocument();
      });
    });

    test('shows all sort options', async () => {
      render(<ProjectSearchPage />);

      await waitFor(() => {
        expect(screen.getByText('React Development Project')).toBeInTheDocument();
      });

      const sortSelect = screen.getByDisplayValue('Relevance');
      expect(sortSelect).toBeInTheDocument();

      // Check all options exist
      expect(screen.getByRole('option', { name: 'Relevance' })).toBeInTheDocument();
      expect(screen.getByRole('option', { name: 'Newest' })).toBeInTheDocument();
      expect(screen.getByRole('option', { name: 'Budget (High to Low)' })).toBeInTheDocument();
      expect(screen.getByRole('option', { name: 'Deadline (Soon)' })).toBeInTheDocument();
    });

    test('changes sort option', async () => {
      render(<ProjectSearchPage />);

      await waitFor(() => {
        expect(screen.getByText('React Development Project')).toBeInTheDocument();
      });

      const sortSelect = screen.getByDisplayValue('Relevance');
      await userEvent.selectOptions(sortSelect, 'Newest');

      // Verify the sort was changed
      await waitFor(() => {
        expect(screen.getByDisplayValue('Newest')).toBeInTheDocument();
      });
    });
  });

  // =========================================================================
  // Suite 6: Pagination (4 tests)
  // =========================================================================
  describe('Pagination', () => {
    const manyProjects = Array.from({ length: 25 }, (_, i) => ({
      id: `proj-${i}`,
      title: `Project ${i}`,
      description: `Description ${i}`,
      creditBudget: 100 * (i + 1),
      status: 'Published',
      createdAt: '2024-01-01T00:00:00Z',
      client: { id: `c${i}`, userName: `user${i}` },
    }));

    const paginatedResults = {
      projects: manyProjects.slice(0, 20),
      totalCount: 25,
      currentPage: 1,
      totalPages: 2,
      aggregations: {},
    };

    beforeEach(() => {
      mockFetch.mockImplementation((url: string) => {
        if (url.includes('/api/skills')) {
          return Promise.resolve({
            ok: true,
            json: () => Promise.resolve(mockSkills),
          });
        }
        if (url.includes('/api/project-search/advanced')) {
          return Promise.resolve({
            ok: true,
            json: () => Promise.resolve(paginatedResults),
          });
        }
        return Promise.resolve({ ok: false });
      });
    });

    test('displays pagination when multiple pages', async () => {
      render(<ProjectSearchPage />);

      await waitFor(() => {
        expect(screen.getByText(/page 1 of 2/i)).toBeInTheDocument();
      });
    });

    test('shows Previous and Next buttons', async () => {
      render(<ProjectSearchPage />);

      await waitFor(() => {
        expect(screen.getByRole('button', { name: /previous/i })).toBeInTheDocument();
        expect(screen.getByRole('button', { name: /next/i })).toBeInTheDocument();
      });
    });

    test('disables Previous button on first page', async () => {
      render(<ProjectSearchPage />);

      await waitFor(() => {
        const prevButton = screen.getByRole('button', { name: /previous/i });
        expect(prevButton).toBeDisabled();
      });
    });

    test('displays page numbers', async () => {
      render(<ProjectSearchPage />);

      await waitFor(() => {
        expect(screen.getByRole('button', { name: '1' })).toBeInTheDocument();
        expect(screen.getByRole('button', { name: '2' })).toBeInTheDocument();
      });
    });
  });

  // =========================================================================
  // Suite 7: Navigation (3 tests)
  // =========================================================================
  describe('Navigation', () => {
    test('clicking project title navigates to project detail', async () => {
      render(<ProjectSearchPage />);

      await waitFor(() => {
        expect(screen.getByText('React Development Project')).toBeInTheDocument();
      });

      await userEvent.click(screen.getByText('React Development Project'));

      expect(mockPush).toHaveBeenCalledWith('/projects/proj-1');
    });

    test('clicking View Project button navigates to project detail', async () => {
      render(<ProjectSearchPage />);

      await waitFor(() => {
        expect(screen.getByText('React Development Project')).toBeInTheDocument();
      });

      const viewButtons = screen.getAllByRole('button', { name: /view project/i });
      await userEvent.click(viewButtons[0]);

      expect(mockPush).toHaveBeenCalledWith('/projects/proj-1');
    });

    test('displays project location', async () => {
      render(<ProjectSearchPage />);

      await waitFor(() => {
        expect(screen.getByText('New York, NY, USA')).toBeInTheDocument();
      });
    });
  });

  // =========================================================================
  // Suite 8: Client Info Display (2 tests)
  // =========================================================================
  describe('Client Info Display', () => {
    test('displays client display name', async () => {
      render(<ProjectSearchPage />);

      await waitFor(() => {
        expect(screen.getByText(/by john doe/i)).toBeInTheDocument();
      });
    });

    test('extracts username from email address', async () => {
      render(<ProjectSearchPage />);

      await waitFor(() => {
        // jane@company.com should show as "jane"
        expect(screen.getByText(/by jane/i)).toBeInTheDocument();
      });
    });
  });
});
