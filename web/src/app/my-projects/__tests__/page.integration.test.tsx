/**
 * my-projects/page.tsx Integration Tests
 *
 * Tests the My Projects page with authentication, project listing, filtering, and pagination.
 * Focus: User project management, status filtering, pagination, error handling.
 *
 * Coverage Target: 85%+ (330 lines)
 * Test Count: 31 tests
 */

import { render, screen, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import MyProjectsPage from '../page';
import { useAuth } from '@/contexts/AuthContext';
import { setupFetchMock } from '@/utils/test/testUtils';

// Mock useAuth hook
jest.mock('@/contexts/AuthContext', () => ({
  useAuth: jest.fn(),
}));

// Mock ThemeContext
jest.mock('@/contexts/ThemeContext', () => ({
  useTheme: jest.fn(() => ({
    theme: 'light',
    setTheme: jest.fn(),
  })),
}));

// Mock components
jest.mock('@/components/ThemeToggle', () => ({
  ThemeToggle: () => <div>Theme Toggle</div>,
}));

jest.mock('@/components/LogoutButton', () => {
  return function MockLogoutButton() {
    return <button>Logout</button>;
  };
});

// Mock dependencies
jest.mock('@/utils/logger', () => ({
  logger: {
    error: jest.fn(),
    info: jest.fn(),
    warn: jest.fn(),
  },
}));

jest.mock('next/link', () => {
  return function MockLink({ children, href }: { children: React.ReactNode; href: string }) {
    return <a href={href}>{children}</a>;
  };
});

const mockUseAuth = useAuth as jest.MockedFunction<typeof useAuth>;

describe('MyProjectsPage - Authentication & Loading', () => {
  let fetchMock: ReturnType<typeof setupFetchMock>;

  beforeEach(() => {
    fetchMock = setupFetchMock();
    delete (window as any).location;
    window.location = { href: '' } as any;
  });

  afterEach(() => {
    fetchMock.reset();
    jest.clearAllMocks();
  });

  const mockAuthenticatedUser = {
    user: { id: 'user-123', email: 'test@example.com', userName: 'testuser', firstName: 'John', lastName: 'Doe', emailVerified: true, taxCompliant: true, status: 'Active', roles: ['User'], permissions: [] },
    isAuthenticated: true,
    isLoading: false,
    isInitialized: true,
    login: jest.fn(),
    logout: jest.fn(),
    refreshToken: jest.fn(),
    updateUser: jest.fn(),
  };

  it('should show loading spinner while auth is loading', () => {
    mockUseAuth.mockReturnValue({
      ...mockAuthenticatedUser,
      isLoading: true,
    });

    render(<MyProjectsPage />);

    expect(screen.getByText(/loading your projects/i)).toBeInTheDocument();
  });

  it('should redirect to login if not authenticated', async () => {
    mockUseAuth.mockReturnValue({
      ...mockAuthenticatedUser,
      isAuthenticated: false,
      isLoading: false,
      user: null,
    });

    fetchMock.respondWith({ success: true }); // logout API response

    render(<MyProjectsPage />);

    // Should call /api/auth/logout
    await waitFor(() => {
      const calls = fetchMock.getCalls();
      expect(calls).toContainEqual(
        expect.objectContaining({
          url: '/api/auth/logout',
          options: expect.objectContaining({ method: 'POST' }),
        })
      );
    });

    // Should redirect to login
    await waitFor(() => {
      expect(window.location.href).toBe('/login');
    });
  });

  it('should fetch projects when authenticated', async () => {
    mockUseAuth.mockReturnValue(mockAuthenticatedUser);

    const mockProjects = [
      {
        id: 'proj-1',
        title: 'Test Project',
        description: 'A test project',
        status: 'Active',
        creditBudget: 500,
        createdAt: '2025-01-01T00:00:00Z',
        applicationCount: 3,
      },
    ];

    fetchMock.respondWith(mockProjects);

    render(<MyProjectsPage />);

    await waitFor(() => {
      expect(screen.getByText('Test Project')).toBeInTheDocument();
    });

    const calls = fetchMock.getCalls();
    expect(calls).toContainEqual(
      expect.objectContaining({
        url: expect.stringContaining('/api/project/my-projects'),
      })
    );
  });

  it('should include pagination params in API request', async () => {
    mockUseAuth.mockReturnValue(mockAuthenticatedUser);
    fetchMock.respondWith([]);

    render(<MyProjectsPage />);

    await waitFor(() => {
      const calls = fetchMock.getCalls();
      const projectsCall = calls.find(c => c.url.includes('/api/project/my-projects'));
      expect(projectsCall?.url).toContain('skip=0');
      expect(projectsCall?.url).toContain('take=10');
    });
  });

  it('should show auth error message on 401 response', async () => {
    mockUseAuth.mockReturnValue(mockAuthenticatedUser);
    fetchMock.respondWithError(401, 'Unauthorized');

    render(<MyProjectsPage />);

    await waitFor(() => {
      expect(screen.getByText(/please log in to view your projects/i)).toBeInTheDocument();
    });
  });
});

describe('MyProjectsPage - Project Fetching & API Integration', () => {
  let fetchMock: ReturnType<typeof setupFetchMock>;

  const mockAuthenticatedUser = {
    user: { id: 'user-123', email: 'test@example.com', userName: 'testuser', firstName: 'John', lastName: 'Doe', emailVerified: true, taxCompliant: true, status: 'Active', roles: ['User'], permissions: [] },
    isAuthenticated: true,
    isLoading: false,
    isInitialized: true,
    login: jest.fn(),
    logout: jest.fn(),
    refreshToken: jest.fn(),
    updateUser: jest.fn(),
  };

  beforeEach(() => {
    fetchMock = setupFetchMock();
    mockUseAuth.mockReturnValue(mockAuthenticatedUser);
  });

  afterEach(() => {
    fetchMock.reset();
    jest.clearAllMocks();
  });

  it('should display multiple projects', async () => {
    const mockProjects = [
      {
        id: 'proj-1',
        title: 'Project One',
        description: 'First project',
        status: 'Active',
        creditBudget: 500,
        createdAt: '2025-01-01T00:00:00Z',
      },
      {
        id: 'proj-2',
        title: 'Project Two',
        description: 'Second project',
        status: 'Draft',
        creditBudget: 1000,
        createdAt: '2025-01-02T00:00:00Z',
      },
    ];

    fetchMock.respondWith(mockProjects);

    render(<MyProjectsPage />);

    await waitFor(() => {
      expect(screen.getByText('Project One')).toBeInTheDocument();
      expect(screen.getByText('Project Two')).toBeInTheDocument();
    });
  });

  it('should handle API error with generic error message', async () => {
    fetchMock.respondWithError(500, 'Server Error');

    render(<MyProjectsPage />);

    await waitFor(() => {
      expect(screen.getByText(/failed to load projects/i)).toBeInTheDocument();
    });
  });

  it('should handle network error gracefully', async () => {
    fetchMock.mockFetch.mockRejectedValueOnce(new Error('Network error'));

    render(<MyProjectsPage />);

    await waitFor(() => {
      expect(screen.getByText(/an error occurred while loading your projects/i)).toBeInTheDocument();
    });
  });

  it('should set hasMore to true when full page of results returned', async () => {
    const mockProjects = Array.from({ length: 10 }, (_, i) => ({
      id: `proj-${i}`,
      title: `Project ${i}`,
      description: 'Test project',
      status: 'Active',
      creditBudget: 500,
      createdAt: '2025-01-01T00:00:00Z',
    }));

    fetchMock.respondWith(mockProjects);

    render(<MyProjectsPage />);

    await waitFor(() => {
      const nextButton = screen.getByText('Next');
      expect(nextButton).not.toBeDisabled();
    });
  });

  it('should set hasMore to false when partial page returned', async () => {
    const mockProjects = Array.from({ length: 5 }, (_, i) => ({
      id: `proj-${i}`,
      title: `Project ${i}`,
      description: 'Test project',
      status: 'Active',
      creditBudget: 500,
      createdAt: '2025-01-01T00:00:00Z',
    }));

    fetchMock.respondWith(mockProjects);

    render(<MyProjectsPage />);

    await waitFor(() => {
      const nextButton = screen.getByText('Next');
      expect(nextButton).toBeDisabled();
    });
  });

  it('should handle empty array response', async () => {
    fetchMock.respondWith([]);

    render(<MyProjectsPage />);

    await waitFor(() => {
      expect(screen.getByText(/you haven't created any projects yet/i)).toBeInTheDocument();
    });
  });
});

describe('MyProjectsPage - Status Filtering', () => {
  let fetchMock: ReturnType<typeof setupFetchMock>;
  const user = userEvent.setup();

  const mockAuthenticatedUser = {
    user: { id: 'user-123', email: 'test@example.com', userName: 'testuser', firstName: 'John', lastName: 'Doe', emailVerified: true, taxCompliant: true, status: 'Active', roles: ['User'], permissions: [] },
    isAuthenticated: true,
    isLoading: false,
    isInitialized: true,
    login: jest.fn(),
    logout: jest.fn(),
    refreshToken: jest.fn(),
    updateUser: jest.fn(),
  };

  beforeEach(() => {
    fetchMock = setupFetchMock();
    mockUseAuth.mockReturnValue(mockAuthenticatedUser);
  });

  afterEach(() => {
    fetchMock.reset();
    jest.clearAllMocks();
  });

  it('should filter projects by Active status', async () => {
    const mockProjects = [
      { id: 'proj-1', title: 'Active Project', description: '', status: 'Active', creditBudget: 500, createdAt: '2025-01-01T00:00:00Z' },
      { id: 'proj-2', title: 'Draft Project', description: '', status: 'Draft', creditBudget: 500, createdAt: '2025-01-01T00:00:00Z' },
    ];

    fetchMock.respondWith(mockProjects);

    render(<MyProjectsPage />);

    await waitFor(() => {
      expect(screen.getByText('Active Project')).toBeInTheDocument();
    });

    const activeButton = screen.getByRole('button', { name: 'Active' });
    await user.click(activeButton);

    expect(screen.getByText('Active Project')).toBeInTheDocument();
    expect(screen.queryByText('Draft Project')).not.toBeInTheDocument();
  });

  it('should filter projects by Draft status', async () => {
    const mockProjects = [
      { id: 'proj-1', title: 'Active Project', description: '', status: 'Active', creditBudget: 500, createdAt: '2025-01-01T00:00:00Z' },
      { id: 'proj-2', title: 'Draft Project', description: '', status: 'Draft', creditBudget: 500, createdAt: '2025-01-01T00:00:00Z' },
    ];

    fetchMock.respondWith(mockProjects);

    render(<MyProjectsPage />);

    await waitFor(() => {
      expect(screen.getByText('Draft Project')).toBeInTheDocument();
    });

    const draftButton = screen.getByRole('button', { name: 'Draft' });
    await user.click(draftButton);

    expect(screen.queryByText('Active Project')).not.toBeInTheDocument();
    expect(screen.getByText('Draft Project')).toBeInTheDocument();
  });

  it('should show all projects when "All" filter selected', async () => {
    const mockProjects = [
      { id: 'proj-1', title: 'Active Project', description: '', status: 'Active', creditBudget: 500, createdAt: '2025-01-01T00:00:00Z' },
      { id: 'proj-2', title: 'Draft Project', description: '', status: 'Draft', creditBudget: 500, createdAt: '2025-01-01T00:00:00Z' },
    ];

    fetchMock.respondWith(mockProjects);

    render(<MyProjectsPage />);

    await waitFor(() => {
      expect(screen.getByText('Active Project')).toBeInTheDocument();
    });

    // First filter to Active
    await user.click(screen.getByRole('button', { name: 'Active' }));
    expect(screen.queryByText('Draft Project')).not.toBeInTheDocument();

    // Then switch back to All
    await user.click(screen.getByRole('button', { name: 'All' }));
    expect(screen.getByText('Active Project')).toBeInTheDocument();
    expect(screen.getByText('Draft Project')).toBeInTheDocument();
  });

  it('should show filtered empty state message', async () => {
    const mockProjects = [
      { id: 'proj-1', title: 'Active Project', description: '', status: 'Active', creditBudget: 500, createdAt: '2025-01-01T00:00:00Z' },
    ];

    fetchMock.respondWith(mockProjects);

    render(<MyProjectsPage />);

    await waitFor(() => {
      expect(screen.getByText('Active Project')).toBeInTheDocument();
    });

    const completedButton = screen.getByRole('button', { name: 'Completed' });
    await user.click(completedButton);

    expect(screen.getByText(/you don't have any completed projects/i)).toBeInTheDocument();
  });
});

describe('MyProjectsPage - Project Display & Status Icons', () => {
  let fetchMock: ReturnType<typeof setupFetchMock>;

  const mockAuthenticatedUser = {
    user: { id: 'user-123', email: 'test@example.com', userName: 'testuser', firstName: 'John', lastName: 'Doe', emailVerified: true, taxCompliant: true, status: 'Active', roles: ['User'], permissions: [] },
    isAuthenticated: true,
    isLoading: false,
    isInitialized: true,
    login: jest.fn(),
    logout: jest.fn(),
    refreshToken: jest.fn(),
    updateUser: jest.fn(),
  };

  beforeEach(() => {
    fetchMock = setupFetchMock();
    mockUseAuth.mockReturnValue(mockAuthenticatedUser);
  });

  afterEach(() => {
    fetchMock.reset();
    jest.clearAllMocks();
  });

  it('should display project details (title, description, budget, date)', async () => {
    const mockProjects = [
      {
        id: 'proj-1',
        title: 'Test Project',
        description: 'This is a test project description',
        status: 'Active',
        creditBudget: 750,
        createdAt: '2025-01-15T00:00:00Z',
        applicationCount: 5,
      },
    ];

    fetchMock.respondWith(mockProjects);

    render(<MyProjectsPage />);

    await waitFor(() => {
      expect(screen.getByText('Test Project')).toBeInTheDocument();
      expect(screen.getByText('This is a test project description')).toBeInTheDocument();
      expect(screen.getByText('750 credits')).toBeInTheDocument();
      expect(screen.getByText('5 applications')).toBeInTheDocument();
    });
  });

  it('should show Urgent badge for urgent projects', async () => {
    const mockProjects = [
      {
        id: 'proj-1',
        title: 'Urgent Project',
        description: 'Need help ASAP',
        status: 'Active',
        creditBudget: 500,
        createdAt: '2025-01-01T00:00:00Z',
        isUrgent: true,
      },
    ];

    fetchMock.respondWith(mockProjects);

    render(<MyProjectsPage />);

    await waitFor(() => {
      expect(screen.getByText('Urgent')).toBeInTheDocument();
    });
  });

  it('should show Featured badge for featured projects', async () => {
    const mockProjects = [
      {
        id: 'proj-1',
        title: 'Featured Project',
        description: 'Top project',
        status: 'Active',
        creditBudget: 500,
        createdAt: '2025-01-01T00:00:00Z',
        isFeatured: true,
      },
    ];

    fetchMock.respondWith(mockProjects);

    render(<MyProjectsPage />);

    await waitFor(() => {
      expect(screen.getByText('Featured')).toBeInTheDocument();
    });
  });

  it('should show correct status badge for each status', async () => {
    const mockProjects = [
      { id: 'proj-1', title: 'Active Project', description: '', status: 'Active', creditBudget: 500, createdAt: '2025-01-01T00:00:00Z' },
      { id: 'proj-2', title: 'Draft Project', description: '', status: 'Draft', creditBudget: 500, createdAt: '2025-01-01T00:00:00Z' },
      { id: 'proj-3', title: 'Completed Project', description: '', status: 'Completed', creditBudget: 500, createdAt: '2025-01-01T00:00:00Z' },
      { id: 'proj-4', title: 'Cancelled Project', description: '', status: 'Cancelled', creditBudget: 500, createdAt: '2025-01-01T00:00:00Z' },
    ];

    fetchMock.respondWith(mockProjects);

    render(<MyProjectsPage />);

    await waitFor(() => {
      const statusBadges = screen.getAllByText(/Active|Draft|Completed|Cancelled/);
      expect(statusBadges.length).toBeGreaterThanOrEqual(4);
    });
  });

  it('should not show application count if undefined', async () => {
    const mockProjects = [
      {
        id: 'proj-1',
        title: 'Project No Apps',
        description: 'No applications yet',
        status: 'Draft',
        creditBudget: 500,
        createdAt: '2025-01-01T00:00:00Z',
        // applicationCount undefined
      },
    ];

    fetchMock.respondWith(mockProjects);

    render(<MyProjectsPage />);

    await waitFor(() => {
      expect(screen.getByText('Project No Apps')).toBeInTheDocument();
    });

    // Should not show "X applications" count (where X is a number)
    expect(screen.queryByText(/\d+ application/)).not.toBeInTheDocument();
  });
});

describe('MyProjectsPage - Pagination', () => {
  let fetchMock: ReturnType<typeof setupFetchMock>;
  const user = userEvent.setup();

  const mockAuthenticatedUser = {
    user: { id: 'user-123', email: 'test@example.com', userName: 'testuser', firstName: 'John', lastName: 'Doe', emailVerified: true, taxCompliant: true, status: 'Active', roles: ['User'], permissions: [] },
    isAuthenticated: true,
    isLoading: false,
    isInitialized: true,
    login: jest.fn(),
    logout: jest.fn(),
    refreshToken: jest.fn(),
    updateUser: jest.fn(),
  };

  beforeEach(() => {
    fetchMock = setupFetchMock();
    mockUseAuth.mockReturnValue(mockAuthenticatedUser);
  });

  afterEach(() => {
    fetchMock.reset();
    jest.clearAllMocks();
  });

  it('should disable Previous button on first page', async () => {
    fetchMock.respondWith([
      { id: 'proj-1', title: 'Project 1', description: '', status: 'Active', creditBudget: 500, createdAt: '2025-01-01T00:00:00Z' },
    ]);

    render(<MyProjectsPage />);

    await waitFor(() => {
      expect(screen.getByText('Project 1')).toBeInTheDocument();
    });

    const previousButton = screen.getByText('Previous');
    expect(previousButton).toBeDisabled();
  });

  it('should enable Next button when hasMore is true', async () => {
    const mockProjects = Array.from({ length: 10 }, (_, i) => ({
      id: `proj-${i}`,
      title: `Project ${i}`,
      description: '',
      status: 'Active',
      creditBudget: 500,
      createdAt: '2025-01-01T00:00:00Z',
    }));

    fetchMock.respondWith(mockProjects);

    render(<MyProjectsPage />);

    await waitFor(() => {
      const nextButton = screen.getByText('Next');
      expect(nextButton).not.toBeDisabled();
    });
  });

  it('should fetch next page when Next button clicked', async () => {
    const page1Projects = Array.from({ length: 10 }, (_, i) => ({
      id: `proj-page1-${i}`,
      title: `Page 1 Project ${i}`,
      description: '',
      status: 'Active',
      creditBudget: 500,
      createdAt: '2025-01-01T00:00:00Z',
    }));

    const page2Projects = Array.from({ length: 5 }, (_, i) => ({
      id: `proj-page2-${i}`,
      title: `Page 2 Project ${i}`,
      description: '',
      status: 'Active',
      creditBudget: 500,
      createdAt: '2025-01-01T00:00:00Z',
    }));

    fetchMock.respondWith(page1Projects);
    fetchMock.respondWith(page2Projects);

    render(<MyProjectsPage />);

    await waitFor(() => {
      expect(screen.getByText('Page 1 Project 0')).toBeInTheDocument();
    });

    const nextButton = screen.getByText('Next');
    await user.click(nextButton);

    await waitFor(() => {
      expect(screen.getByText('Page 2 Project 0')).toBeInTheDocument();
    });

    // Verify API was called with skip=10
    const calls = fetchMock.getCalls();
    const page2Call = calls.find(c => c.url.includes('skip=10'));
    expect(page2Call).toBeDefined();
  });

  it('should go back to previous page when Previous button clicked', async () => {
    const page1Projects = Array.from({ length: 10 }, (_, i) => ({
      id: `proj-page1-${i}`,
      title: `Page 1 Project ${i}`,
      description: '',
      status: 'Active',
      creditBudget: 500,
      createdAt: '2025-01-01T00:00:00Z',
    }));

    const page2Projects = Array.from({ length: 5 }, (_, i) => ({
      id: `proj-page2-${i}`,
      title: `Page 2 Project ${i}`,
      description: '',
      status: 'Active',
      creditBudget: 500,
      createdAt: '2025-01-01T00:00:00Z',
    }));

    fetchMock.respondWith(page1Projects);
    fetchMock.respondWith(page2Projects);
    fetchMock.respondWith(page1Projects); // for going back

    render(<MyProjectsPage />);

    // Wait for page 1 to load
    await waitFor(() => {
      expect(screen.getByText('Page 1 Project 0')).toBeInTheDocument();
    });

    // Go to page 2
    await user.click(screen.getByText('Next'));

    await waitFor(() => {
      expect(screen.getByText('Page 2 Project 0')).toBeInTheDocument();
    });

    // Go back to page 1
    await user.click(screen.getByText('Previous'));

    await waitFor(() => {
      const calls = fetchMock.getCalls();
      const backToPage1Call = calls.filter(c => c.url.includes('skip=0'));
      expect(backToPage1Call.length).toBeGreaterThanOrEqual(2); // Initial + after Previous
    });
  });
});

describe('MyProjectsPage - Empty & Error States', () => {
  let fetchMock: ReturnType<typeof setupFetchMock>;

  const mockAuthenticatedUser = {
    user: { id: 'user-123', email: 'test@example.com', userName: 'testuser', firstName: 'John', lastName: 'Doe', emailVerified: true, taxCompliant: true, status: 'Active', roles: ['User'], permissions: [] },
    isAuthenticated: true,
    isLoading: false,
    isInitialized: true,
    login: jest.fn(),
    logout: jest.fn(),
    refreshToken: jest.fn(),
    updateUser: jest.fn(),
  };

  beforeEach(() => {
    fetchMock = setupFetchMock();
    mockUseAuth.mockReturnValue(mockAuthenticatedUser);
  });

  afterEach(() => {
    fetchMock.reset();
    jest.clearAllMocks();
  });

  it('should show empty state when no projects exist', async () => {
    fetchMock.respondWith([]);

    render(<MyProjectsPage />);

    await waitFor(() => {
      expect(screen.getByText(/you haven't created any projects yet/i)).toBeInTheDocument();
      expect(screen.getByText('Create Your First Project')).toBeInTheDocument();
    });
  });

  it('should show error state with error message', async () => {
    fetchMock.respondWithError(500, 'Server Error');

    render(<MyProjectsPage />);

    await waitFor(() => {
      expect(screen.getByText(/failed to load projects/i)).toBeInTheDocument();
    });

    // Error should not show project list
    expect(screen.queryByText('Create Your First Project')).not.toBeInTheDocument();
  });

  it('should not show pagination when no projects', async () => {
    fetchMock.respondWith([]);

    render(<MyProjectsPage />);

    await waitFor(() => {
      expect(screen.getByText(/you haven't created any projects yet/i)).toBeInTheDocument();
    });

    expect(screen.queryByText('Previous')).not.toBeInTheDocument();
    expect(screen.queryByText('Next')).not.toBeInTheDocument();
  });

  it('should show "No projects found" header in empty state', async () => {
    fetchMock.respondWith([]);

    render(<MyProjectsPage />);

    await waitFor(() => {
      expect(screen.getByText('No projects found')).toBeInTheDocument();
    });
  });
});

describe('MyProjectsPage - Navigation & Actions', () => {
  let fetchMock: ReturnType<typeof setupFetchMock>;

  const mockAuthenticatedUser = {
    user: { id: 'user-123', email: 'test@example.com', userName: 'testuser', firstName: 'Jane', lastName: 'Doe', emailVerified: true, taxCompliant: true, status: 'Active', roles: ['User'], permissions: [] },
    isAuthenticated: true,
    isLoading: false,
    isInitialized: true,
    login: jest.fn(),
    logout: jest.fn(),
    refreshToken: jest.fn(),
    updateUser: jest.fn(),
  };

  beforeEach(() => {
    fetchMock = setupFetchMock();
    mockUseAuth.mockReturnValue(mockAuthenticatedUser);
  });

  afterEach(() => {
    fetchMock.reset();
    jest.clearAllMocks();
  });

  it('should display user name in navigation', async () => {
    fetchMock.respondWith([]);

    render(<MyProjectsPage />);

    await waitFor(() => {
      expect(screen.getByText(/welcome back,/i)).toBeInTheDocument();
      expect(screen.getByText(/jane/i)).toBeInTheDocument();
    });
  });

  it('should render Create Project button in header', async () => {
    fetchMock.respondWith([]);

    render(<MyProjectsPage />);

    await waitFor(() => {
      const createButtons = screen.getAllByText(/create project/i);
      expect(createButtons.length).toBeGreaterThan(0);
    });
  });

  it('should render View Applications and View Details links for each project', async () => {
    const mockProjects = [
      {
        id: 'proj-1',
        title: 'Test Project',
        description: 'Test',
        status: 'Active',
        creditBudget: 500,
        createdAt: '2025-01-01T00:00:00Z',
      },
    ];

    fetchMock.respondWith(mockProjects);

    render(<MyProjectsPage />);

    await waitFor(() => {
      expect(screen.getByText('Test Project')).toBeInTheDocument();
    });

    expect(screen.getByText('View Applications')).toBeInTheDocument();
    expect(screen.getByText('View Details')).toBeInTheDocument();
  });
});
