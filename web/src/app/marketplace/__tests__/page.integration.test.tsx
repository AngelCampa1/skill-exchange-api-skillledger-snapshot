
/**
 * Integration tests for Marketplace page (Week 16 Part B)
 *
 * Test Coverage:
 * 1. Project Listing & Display (6 tests)
 * 2. Search & Filters (8 tests)
 * 3. Sorting (3 tests)
 * 4. Pagination (3 tests)
 * 5. Error & Empty States (2 tests)
 * 6. Project Navigation (3 tests)
 *
 * Total: 25 tests
 */

import { render, screen, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import MarketplacePage from '../page';

// Mock dependencies - IMPORTANT: Return stable object references to avoid infinite re-renders
const mockPush = jest.fn();
const stableRouterRef = { push: mockPush };
const stableSearchParamsRef = new URLSearchParams();

// Create a stable auth state that can be updated per-test
let mockAuthState = {
  user: { id: 'user-1', email: 'test@example.com', firstName: 'John' } as { id: string; email: string; firstName?: string } | null,
  isAuthenticated: true,
  isLoading: false,
};

jest.mock('next/navigation', () => ({
  useRouter: () => stableRouterRef, // Return stable reference
  useSearchParams: () => stableSearchParamsRef, // Return stable reference
}));

jest.mock('@/contexts/AuthContext', () => ({
  useAuth: () => mockAuthState, // Return stable reference
}));

// Mock ThemeToggle component (named export)
jest.mock('@/components/ThemeToggle', () => ({
  ThemeToggle: function MockThemeToggle() {
    return null;
  },
}));

// Mock fetch
global.fetch = jest.fn();

describe('Marketplace Page - Integration Tests', () => {
  beforeEach(() => {
    jest.clearAllMocks();
    (global.fetch as jest.Mock).mockClear();

    // Reset auth state to default (authenticated user)
    mockAuthState.user = { id: 'user-1', email: 'test@example.com', firstName: 'John' };
    mockAuthState.isAuthenticated = true;
    mockAuthState.isLoading = false;
  });

  afterEach(() => {
    jest.restoreAllMocks();
  });

  // Helper function to create mock project data
  const createMockProject = (id: string, overrides = {}) => ({
    id,
    title: `Project ${id}`,
    description: `Description for project ${id}`,
    creditBudget: 1000, // Correct property name
    status: 'Open',
    requiredSkillNames: ['JavaScript', 'React'],
    deadline: '2024-12-31',
    location: 'Remote',
    clientName: 'Client Name',
    ...overrides,
  });

  // Helper function to mock successful API response
  const mockSuccessfulFetch = (projects: any[], totalPages = 1) => {
    (global.fetch as jest.Mock).mockResolvedValueOnce({
      ok: true,
      json: async () => ({
        projects,
        totalPages,
        currentPage: 1,
        totalCount: projects.length,
      }),
    });
  };

  // Helper function to open filters panel (filters are hidden by default)
  const openFilters = async (user: ReturnType<typeof userEvent.setup>) => {
    const filterButton = screen.getByTestId('toggle-filters-button');
    await user.click(filterButton);
  };

  // ===================================================================
  // 1. Project Listing & Display (6 tests)
  // ===================================================================

  describe('Project Listing & Display', () => {
    test('displays project cards with title, description, and budget', async () => {
      const mockProjects = [
        createMockProject('proj-1', { title: 'Build a Website', description: 'Need a modern website', creditBudget: 2500 }),
        createMockProject('proj-2', { title: 'Mobile App Development', description: 'iOS app needed', creditBudget: 5000 }),
      ];

      mockSuccessfulFetch(mockProjects);

      render(<MarketplacePage />);

      await waitFor(() => {
        expect(screen.getByText('Build a Website')).toBeInTheDocument();
        expect(screen.getByText('Mobile App Development')).toBeInTheDocument();
      });

      expect(screen.getByText(/Need a modern website/i)).toBeInTheDocument();
      expect(screen.getByText(/iOS app needed/i)).toBeInTheDocument();
      expect(screen.getByText(/2500/)).toBeInTheDocument();
      expect(screen.getByText(/5000/)).toBeInTheDocument();
    });

    test('displays project status badges correctly', async () => {
      const mockProjects = [
        createMockProject('proj-1', { status: 'Open' }),
        createMockProject('proj-2', { status: 'In Progress' }),
      ];

      mockSuccessfulFetch(mockProjects);

      render(<MarketplacePage />);

      await waitFor(() => {
        expect(screen.getByText('Open')).toBeInTheDocument();
        expect(screen.getByText('In Progress')).toBeInTheDocument();
      });
    });

    test('displays skill tags for each project', async () => {
      const mockProjects = [
        createMockProject('proj-1', { requiredSkillNames: ['JavaScript', 'React', 'Node.js'] }),
      ];

      mockSuccessfulFetch(mockProjects);

      render(<MarketplacePage />);

      await waitFor(() => {
        expect(screen.getByText('JavaScript')).toBeInTheDocument();
        expect(screen.getByText('React')).toBeInTheDocument();
        expect(screen.getByText('Node.js')).toBeInTheDocument();
      });
    });

    test('displays project deadline', async () => {
      // Use a future date to see relative time display
      const futureDate = new Date();
      futureDate.setDate(futureDate.getDate() + 10); // 10 days from now
      const mockProjects = [
        createMockProject('proj-1', { deadline: futureDate.toISOString().split('T')[0] }),
      ];

      mockSuccessfulFetch(mockProjects);

      render(<MarketplacePage />);

      await waitFor(() => {
        // Component shows relative time: "X days", "Today", "Tomorrow", or "Expired"
        expect(screen.getByText(/days|Today|Tomorrow|Expired/)).toBeInTheDocument();
      });
    });

    test('displays project location', async () => {
      const mockProjects = [
        createMockProject('proj-1', { location: 'Remote' }),
        createMockProject('proj-2', { location: 'Onsite' }),
      ];

      mockSuccessfulFetch(mockProjects);

      render(<MarketplacePage />);

      await waitFor(() => {
        expect(screen.getByText('Remote')).toBeInTheDocument();
        expect(screen.getByText('Onsite')).toBeInTheDocument();
      });
    });

    test('displays client name for each project', async () => {
      const mockProjects = [
        createMockProject('proj-1', { clientName: 'Acme Corp' }),
        createMockProject('proj-2', { clientName: 'Tech Startup' }),
      ];

      mockSuccessfulFetch(mockProjects);

      render(<MarketplacePage />);

      await waitFor(() => {
        expect(screen.getByText('Acme Corp')).toBeInTheDocument();
        expect(screen.getByText('Tech Startup')).toBeInTheDocument();
      });
    });
  });

  // ===================================================================
  // 2. Search & Filters (8 tests)
  // ===================================================================

  describe('Search & Filters', () => {
    test('search input filters projects by keyword', async () => {
      const user = userEvent.setup();
      const mockProjects = [
        createMockProject('proj-1', { title: 'Build a Website' }),
        createMockProject('proj-2', { title: 'Mobile App Development' }),
      ];

      mockSuccessfulFetch(mockProjects);

      render(<MarketplacePage />);

      await waitFor(() => {
        expect(screen.getByText('Build a Website')).toBeInTheDocument();
      });

      mockSuccessfulFetch([mockProjects[0]]);

      const searchInput = screen.getByPlaceholderText(/search/i);
      await user.type(searchInput, 'website');

      await waitFor(() => {
        expect(global.fetch).toHaveBeenCalledWith(
          expect.stringContaining('query=website'),
          expect.any(Object)
        );
      }, { timeout: 3000 });
    });

    test('skill filter checkboxes update results', async () => {
      const user = userEvent.setup();
      const mockProjects = [
        createMockProject('proj-1', { title: 'Build a Website', requiredSkillNames: ['JavaScript', 'React'] }),
      ];

      mockSuccessfulFetch(mockProjects);

      render(<MarketplacePage />);

      await waitFor(() => {
        expect(screen.getByText('Build a Website')).toBeInTheDocument();
      });

      await openFilters(user);

      mockSuccessfulFetch(mockProjects);

      const jsCheckbox = screen.getByTestId('skill-filter-JavaScript');
      await user.click(jsCheckbox);

      await waitFor(() => {
        expect(global.fetch).toHaveBeenCalledWith(
          expect.stringContaining('skillNames=JavaScript'),
          expect.any(Object)
        );
      }, { timeout: 2000 });
    });

    test('budget range filters work correctly', async () => {
      const user = userEvent.setup();
      const mockProjects = [
        createMockProject('proj-1', { title: 'Build a Website', creditBudget: 1500 }),
      ];

      mockSuccessfulFetch(mockProjects);

      render(<MarketplacePage />);

      await waitFor(() => {
        expect(screen.getByText('Build a Website')).toBeInTheDocument();
      });

      await openFilters(user);

      mockSuccessfulFetch(mockProjects);

      const minBudgetInput = screen.getByTestId('budget-min-input');
      await user.clear(minBudgetInput);
      await user.type(minBudgetInput, '1000');

      const maxBudgetInput = screen.getByTestId('budget-max-input');
      await user.clear(maxBudgetInput);
      await user.type(maxBudgetInput, '2000');

      await waitFor(() => {
        expect(global.fetch).toHaveBeenCalledWith(
          expect.stringMatching(/minBudget=1000/),
          expect.any(Object)
        );
      }, { timeout: 3000 });
    });

    test('quick budget range buttons apply filters', async () => {
      const user = userEvent.setup();
      const mockProjects = [
        createMockProject('proj-1', { title: 'Build a Website', creditBudget: 750 }),
      ];

      mockSuccessfulFetch(mockProjects);

      render(<MarketplacePage />);

      await waitFor(() => {
        expect(screen.getByText('Build a Website')).toBeInTheDocument();
      });

      await openFilters(user);

      mockSuccessfulFetch(mockProjects);

      const budgetRangeButton = screen.getByRole('button', { name: /500 - 1000 credits/i });
      await user.click(budgetRangeButton);

      await waitFor(() => {
        expect(global.fetch).toHaveBeenCalledWith(
          expect.stringMatching(/minBudget=500/),
          expect.any(Object)
        );
      }, { timeout: 2000 });
    });

    test('location filter updates results', async () => {
      const user = userEvent.setup();
      const mockProjects = [
        createMockProject('proj-1', { title: 'Build a Website', location: 'Remote' }),
      ];

      mockSuccessfulFetch(mockProjects);

      render(<MarketplacePage />);

      await waitFor(() => {
        expect(screen.getByText('Build a Website')).toBeInTheDocument();
      });

      await openFilters(user);

      mockSuccessfulFetch(mockProjects);

      const locationInput = screen.getByTestId('location-filter-input');
      await user.type(locationInput, 'Remote');

      await waitFor(() => {
        expect(screen.getByTestId('location-filter-input')).toHaveValue('Remote');
      }, { timeout: 3000 });
    });

    test('location quick filter buttons work', async () => {
      const user = userEvent.setup();
      const mockProjects = [
        createMockProject('proj-1', { title: 'Build a Website', location: 'Remote' }),
      ];

      mockSuccessfulFetch(mockProjects);

      render(<MarketplacePage />);

      await waitFor(() => {
        expect(screen.getByText('Build a Website')).toBeInTheDocument();
      });

      await openFilters(user);

      mockSuccessfulFetch(mockProjects);

      const remoteButton = screen.getByRole('button', { name: 'Remote' });
      await user.click(remoteButton);

      await waitFor(() => {
        expect(screen.getByTestId('location-filter-input')).toHaveValue('Remote');
      }, { timeout: 2000 });
    });

    test('combined filters work correctly', async () => {
      const user = userEvent.setup();
      const mockProjects = [
        createMockProject('proj-1', {
          title: 'Website Project',
          requiredSkillNames: ['JavaScript'],
          creditBudget: 1500,
          location: 'Remote'
        }),
      ];

      mockSuccessfulFetch(mockProjects);

      render(<MarketplacePage />);

      await waitFor(() => {
        expect(screen.getByText('Website Project')).toBeInTheDocument();
      });

      await openFilters(user);

      mockSuccessfulFetch(mockProjects);

      const searchInput = screen.getByPlaceholderText(/search/i);
      await user.type(searchInput, 'website');

      const jsCheckbox = screen.getByTestId('skill-filter-JavaScript');
      await user.click(jsCheckbox);

      const minBudgetInput = screen.getByTestId('budget-min-input');
      await user.clear(minBudgetInput);
      await user.type(minBudgetInput, '1000');

      await waitFor(() => {
        expect(global.fetch).toHaveBeenCalledWith(
          expect.stringContaining('query=website'),
          expect.any(Object)
        );
      }, { timeout: 3000 });
    });

    test('clear filters button resets all filters', async () => {
      const user = userEvent.setup();
      const mockProjects = [
        createMockProject('proj-1'),
        createMockProject('proj-2'),
      ];

      mockSuccessfulFetch(mockProjects);

      render(<MarketplacePage />);

      await waitFor(() => {
        expect(screen.getByText('Project proj-1')).toBeInTheDocument();
      });

      await openFilters(user);

      const searchInput = screen.getByPlaceholderText(/search/i);
      await user.type(searchInput, 'test');

      mockSuccessfulFetch(mockProjects);

      const clearButton = screen.getByTestId('clear-filters-button');
      await user.click(clearButton);

      await waitFor(() => {
        expect(searchInput).toHaveValue('');
      });
    });
  });

  // ===================================================================
  // 3. Sorting (3 tests)
  // ===================================================================

  describe('Sorting', () => {
    test('sort by newest first (default)', async () => {
      const mockProjects = [
        createMockProject('proj-1'),
        createMockProject('proj-2'),
      ];

      mockSuccessfulFetch(mockProjects);

      render(<MarketplacePage />);

      await waitFor(() => {
        expect(global.fetch).toHaveBeenCalledWith(
          expect.stringContaining('sortBy=createdat'),
          expect.any(Object)
        );
      });
    });

    test('sort by budget high to low', async () => {
      const user = userEvent.setup();
      const mockProjects = [
        createMockProject('proj-1', { title: 'Build a Website', creditBudget: 5000 }),
        createMockProject('proj-2', { creditBudget: 1000 }),
      ];

      mockSuccessfulFetch(mockProjects);

      render(<MarketplacePage />);

      await waitFor(() => {
        expect(screen.getByText('Build a Website')).toBeInTheDocument();
      });

      mockSuccessfulFetch(mockProjects);

      const sortSelect = screen.getByTestId('sort-select');
      await user.selectOptions(sortSelect, 'budget_high');

      await waitFor(() => {
        expect(global.fetch).toHaveBeenCalledWith(
          expect.stringContaining('sortBy=budget'),
          expect.any(Object)
        );
      }, { timeout: 2000 });
    });

    test('sort by deadline soonest first', async () => {
      const user = userEvent.setup();
      const mockProjects = [
        createMockProject('proj-1', { title: 'Build a Website', deadline: '2024-12-31' }),
        createMockProject('proj-2', { deadline: '2025-06-30' }),
      ];

      mockSuccessfulFetch(mockProjects);

      render(<MarketplacePage />);

      await waitFor(() => {
        expect(screen.getByText('Build a Website')).toBeInTheDocument();
      });

      mockSuccessfulFetch(mockProjects);

      const sortSelect = screen.getByTestId('sort-select');
      await user.selectOptions(sortSelect, 'deadline');

      await waitFor(() => {
        expect(global.fetch).toHaveBeenCalledWith(
          expect.stringContaining('sortBy=enddate'),
          expect.any(Object)
        );
      }, { timeout: 2000 });
    });
  });

  // ===================================================================
  // 4. Pagination (3 tests)
  // ===================================================================

  describe('Pagination', () => {
    test('initial load shows correct page size', async () => {
      const mockProjects = Array.from({ length: 12 }, (_, i) =>
        createMockProject(`proj-${i + 1}`)
      );

      mockSuccessfulFetch(mockProjects, 3);

      render(<MarketplacePage />);

      await waitFor(() => {
        expect(global.fetch).toHaveBeenCalledWith(
          expect.stringMatching(/skip=0.*take=12/),
          expect.any(Object)
        );
      });
    });

    test('next page button loads next set of projects', async () => {
      const user = userEvent.setup();
      const page1Projects = Array.from({ length: 12 }, (_, i) =>
        createMockProject(`proj-${i + 1}`)
      );
      const page2Projects = Array.from({ length: 12 }, (_, i) =>
        createMockProject(`proj-${i + 13}`)
      );

      mockSuccessfulFetch(page1Projects, 2);

      render(<MarketplacePage />);

      await waitFor(() => {
        expect(screen.getByText('Project proj-1')).toBeInTheDocument();
      });

      mockSuccessfulFetch(page2Projects, 2);

      const nextButton = screen.getByTestId('next-page-button');
      await user.click(nextButton);

      await waitFor(() => {
        expect(global.fetch).toHaveBeenCalledWith(
          expect.stringMatching(/skip=12.*take=12/),
          expect.any(Object)
        );
      }, { timeout: 2000 });
    });

    test('previous page button loads previous set of projects', async () => {
      const user = userEvent.setup();
      const page1Projects = Array.from({ length: 12 }, (_, i) =>
        createMockProject(`proj-${i + 1}`)
      );
      const page2Projects = Array.from({ length: 12 }, (_, i) =>
        createMockProject(`proj-${i + 13}`)
      );

      mockSuccessfulFetch(page1Projects, 2);

      render(<MarketplacePage />);

      await waitFor(() => {
        expect(screen.getByText('Project proj-1')).toBeInTheDocument();
      });

      mockSuccessfulFetch(page2Projects, 2);
      const nextButton = screen.getByTestId('next-page-button');
      await user.click(nextButton);

      await waitFor(() => {
        expect(global.fetch).toHaveBeenCalledWith(
          expect.stringMatching(/skip=12/),
          expect.any(Object)
        );
      });

      mockSuccessfulFetch(page1Projects, 2);
      const prevButton = screen.getByTestId('prev-page-button');
      await user.click(prevButton);

      await waitFor(() => {
        expect(global.fetch).toHaveBeenCalledWith(
          expect.stringMatching(/skip=0/),
          expect.any(Object)
        );
      }, { timeout: 2000 });
    });
  });

  // ===================================================================
  // 5. Error & Empty States (2 tests)
  // ===================================================================

  describe('Error & Empty States', () => {
    test('displays error message on API failure', async () => {
      (global.fetch as jest.Mock).mockRejectedValueOnce(new Error('Network error'));

      render(<MarketplacePage />);

      await waitFor(() => {
        expect(screen.getByText(/unable/i)).toBeInTheDocument();
      });
    });

    test('displays empty state when no projects match filters', async () => {
      mockSuccessfulFetch([]);

      render(<MarketplacePage />);

      await waitFor(() => {
        expect(screen.getByText(/no projects found/i)).toBeInTheDocument();
      });

      expect(screen.getByRole('button', { name: /clear filters/i })).toBeInTheDocument();
    });
  });

  // ===================================================================
  // 6. Project Navigation (3 tests)
  // ===================================================================

  describe('Project Navigation', () => {
    test('clicking project card navigates to project details', async () => {
      const user = userEvent.setup();
      const mockProjects = [
        createMockProject('proj-123', { title: 'Test Project' }),
      ];

      mockSuccessfulFetch(mockProjects);

      render(<MarketplacePage />);

      await waitFor(() => {
        expect(screen.getByText('Test Project')).toBeInTheDocument();
      });

      const projectCard = screen.getByText('Test Project');
      await user.click(projectCard);

      await waitFor(() => {
        expect(mockPush).toHaveBeenCalledWith('/projects/proj-123');
      });
    });

    test('view details button navigates to project page', async () => {
      const user = userEvent.setup();
      const mockProjects = [
        createMockProject('proj-456', { title: 'Another Project' }),
      ];

      mockSuccessfulFetch(mockProjects);

      render(<MarketplacePage />);

      await waitFor(() => {
        expect(screen.getByText('Another Project')).toBeInTheDocument();
      });

      const viewButton = screen.getByTestId('view-project-proj-456');
      await user.click(viewButton);

      await waitFor(() => {
        expect(mockPush).toHaveBeenCalledWith('/projects/proj-456');
      });
    });

    test('unauthenticated users can view projects (marketplace is public)', async () => {
      const user = userEvent.setup();

      mockAuthState.user = null;
      mockAuthState.isAuthenticated = false;
      mockAuthState.isLoading = false;

      const mockProjects = [
        createMockProject('proj-789', { title: 'Public Project' }),
      ];

      mockSuccessfulFetch(mockProjects);

      render(<MarketplacePage />);

      await waitFor(() => {
        expect(screen.getByText('Public Project')).toBeInTheDocument();
      });

      const projectCard = screen.getByText('Public Project');
      await user.click(projectCard);

      await waitFor(() => {
        expect(mockPush).toHaveBeenCalledWith('/projects/proj-789');
      });
    });
  });
});
