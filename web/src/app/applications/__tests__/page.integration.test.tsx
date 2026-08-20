/**
 * applications/page.tsx Integration Tests
 *
 * Tests the My Applications page with authentication, application listing, filtering, and pagination.
 * Focus: User application management, status filtering, pagination, error handling.
 *
 * Coverage Target: 85%+ (313 lines)
 * Test Count: 30 tests
 */

import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import MyApplicationsPage from '../page';
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

describe('MyApplicationsPage - Authentication & Loading', () => {
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

    render(<MyApplicationsPage />);

    expect(screen.getByText(/loading your applications/i)).toBeInTheDocument();
  });

  it('should redirect to login if not authenticated', async () => {
    mockUseAuth.mockReturnValue({
      ...mockAuthenticatedUser,
      isAuthenticated: false,
      isLoading: false,
      user: null,
    });

    fetchMock.respondWith({ success: true }); // logout API response

    render(<MyApplicationsPage />);

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

  it('should fetch applications when authenticated', async () => {
    mockUseAuth.mockReturnValue(mockAuthenticatedUser);

    const mockResponse = {
      applications: [
        {
          id: 'app-1',
          projectId: 'proj-1',
          projectTitle: 'Test Project',
          status: 'Pending',
          proposedBudget: 500,
          estimatedDuration: '2 weeks',
          coverLetter: 'I am interested',
          createdAt: '2025-01-01T00:00:00Z',
          updatedAt: '2025-01-01T00:00:00Z',
        },
      ],
      totalCount: 1,
      pageSize: 10,
      currentPage: 1,
      totalPages: 1,
    };

    fetchMock.respondWith(mockResponse);

    render(<MyApplicationsPage />);

    await waitFor(() => {
      expect(screen.getByText('Test Project')).toBeInTheDocument();
    });

    const calls = fetchMock.getCalls();
    expect(calls).toContainEqual(
      expect.objectContaining({
        url: expect.stringContaining('/api/project-applications/my-applications'),
      })
    );
  });

  it('should include pagination params in API request', async () => {
    mockUseAuth.mockReturnValue(mockAuthenticatedUser);
    fetchMock.respondWith({ applications: [], totalCount: 0, pageSize: 10, currentPage: 1, totalPages: 1 });

    render(<MyApplicationsPage />);

    await waitFor(() => {
      const calls = fetchMock.getCalls();
      const applicationsCall = calls.find(c => c.url.includes('/api/project-applications/my-applications'));
      expect(applicationsCall?.url).toContain('page=1');
      expect(applicationsCall?.url).toContain('pageSize=10');
    });
  });

  it('should show auth error message on 401 response', async () => {
    mockUseAuth.mockReturnValue(mockAuthenticatedUser);
    fetchMock.respondWithError(401, 'Unauthorized');

    render(<MyApplicationsPage />);

    await waitFor(() => {
      expect(screen.getByText(/please log in to view your applications/i)).toBeInTheDocument();
    });
  });
});

describe('MyApplicationsPage - Application Fetching & API Integration', () => {
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

  it('should display multiple applications', async () => {
    const mockResponse = {
      applications: [
        {
          id: 'app-1',
          projectId: 'proj-1',
          projectTitle: 'Project One',
          status: 'Pending',
          proposedBudget: 500,
          estimatedDuration: '2 weeks',
          coverLetter: 'Application 1',
          createdAt: '2025-01-01T00:00:00Z',
          updatedAt: '2025-01-01T00:00:00Z',
        },
        {
          id: 'app-2',
          projectId: 'proj-2',
          projectTitle: 'Project Two',
          status: 'Accepted',
          proposedBudget: 1000,
          estimatedDuration: '4 weeks',
          coverLetter: 'Application 2',
          createdAt: '2025-01-02T00:00:00Z',
          updatedAt: '2025-01-02T00:00:00Z',
        },
      ],
      totalCount: 2,
      pageSize: 10,
      currentPage: 1,
      totalPages: 1,
    };

    fetchMock.respondWith(mockResponse);

    render(<MyApplicationsPage />);

    await waitFor(() => {
      expect(screen.getByText('Project One')).toBeInTheDocument();
      expect(screen.getByText('Project Two')).toBeInTheDocument();
    });
  });

  it('should handle API error with generic error message', async () => {
    fetchMock.respondWithError(500, 'Server Error');

    render(<MyApplicationsPage />);

    await waitFor(() => {
      expect(screen.getByText(/failed to load applications/i)).toBeInTheDocument();
    });
  });

  it('should handle network error gracefully', async () => {
    fetchMock.mockFetch.mockRejectedValueOnce(new Error('Network error'));

    render(<MyApplicationsPage />);

    await waitFor(() => {
      expect(screen.getByText(/an error occurred while loading your applications/i)).toBeInTheDocument();
    });
  });

  it('should parse ApplicationSearchResult and update totalPages', async () => {
    const mockResponse = {
      applications: [{ id: 'app-1', projectId: 'proj-1', projectTitle: 'Test', status: 'Pending', proposedBudget: 500, estimatedDuration: '1 week', coverLetter: '', createdAt: '2025-01-01T00:00:00Z', updatedAt: '2025-01-01T00:00:00Z' }],
      totalCount: 25,
      pageSize: 10,
      currentPage: 1,
      totalPages: 3,
    };

    fetchMock.respondWith(mockResponse);

    render(<MyApplicationsPage />);

    await waitFor(() => {
      expect(screen.getByText('Test')).toBeInTheDocument();
    });

    // Should show pagination with 3 pages
    expect(screen.getByText('Page 1 of 3')).toBeInTheDocument();
  });

  it('should handle empty applications array', async () => {
    fetchMock.respondWith({ applications: [], totalCount: 0, pageSize: 10, currentPage: 1, totalPages: 1 });

    render(<MyApplicationsPage />);

    await waitFor(() => {
      expect(screen.getByText(/you haven't applied to any projects yet/i)).toBeInTheDocument();
    });
  });

  it('should show "Try Again" button on error', async () => {
    fetchMock.respondWithError(500, 'Server Error');

    render(<MyApplicationsPage />);

    await waitFor(() => {
      expect(screen.getByText('Try Again')).toBeInTheDocument();
    });
  });
});

describe('MyApplicationsPage - Status Filtering', () => {
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

  it('should filter applications by Pending status', async () => {
    fetchMock.respondWith({ applications: [], totalCount: 0, pageSize: 10, currentPage: 1, totalPages: 1 });
    fetchMock.respondWith({ applications: [], totalCount: 0, pageSize: 10, currentPage: 1, totalPages: 1 });

    render(<MyApplicationsPage />);

    await waitFor(() => {
      expect(screen.getByText(/you haven't applied to any projects yet/i)).toBeInTheDocument();
    });

    const pendingButton = screen.getByRole('button', { name: 'Pending' });
    await user.click(pendingButton);

    await waitFor(() => {
      const calls = fetchMock.getCalls();
      const filteredCall = calls.find(c => c.url.includes('status=Pending'));
      expect(filteredCall).toBeDefined();
    });
  });

  it('should filter applications by Accepted status', async () => {
    fetchMock.respondWith({ applications: [], totalCount: 0, pageSize: 10, currentPage: 1, totalPages: 1 });
    fetchMock.respondWith({ applications: [], totalCount: 0, pageSize: 10, currentPage: 1, totalPages: 1 });

    render(<MyApplicationsPage />);

    await waitFor(() => {
      expect(screen.getByText(/you haven't applied to any projects yet/i)).toBeInTheDocument();
    });

    const acceptedButton = screen.getByRole('button', { name: 'Accepted' });
    await user.click(acceptedButton);

    await waitFor(() => {
      const calls = fetchMock.getCalls();
      const filteredCall = calls.find(c => c.url.includes('status=Accepted'));
      expect(filteredCall).toBeDefined();
    });
  });

  it('should filter applications by Rejected status', async () => {
    fetchMock.respondWith({ applications: [], totalCount: 0, pageSize: 10, currentPage: 1, totalPages: 1 });
    fetchMock.respondWith({ applications: [], totalCount: 0, pageSize: 10, currentPage: 1, totalPages: 1 });

    render(<MyApplicationsPage />);

    await waitFor(() => {
      expect(screen.getByText(/you haven't applied to any projects yet/i)).toBeInTheDocument();
    });

    const rejectedButton = screen.getByRole('button', { name: 'Rejected' });
    await user.click(rejectedButton);

    await waitFor(() => {
      const calls = fetchMock.getCalls();
      const filteredCall = calls.find(c => c.url.includes('status=Rejected'));
      expect(filteredCall).toBeDefined();
    });
  });

  it('should reset to page 1 when filter changes', async () => {
    const mockResponse = {
      applications: Array.from({ length: 30 }, (_, i) => ({
        id: `app-${i}`,
        projectId: `proj-${i}`,
        projectTitle: `Project ${i}`,
        status: 'Pending',
        proposedBudget: 500,
        estimatedDuration: '1 week',
        coverLetter: '',
        createdAt: '2025-01-01T00:00:00Z',
        updatedAt: '2025-01-01T00:00:00Z',
      })),
      totalCount: 30,
      pageSize: 10,
      currentPage: 1,
      totalPages: 3,
    };

    fetchMock.respondWith(mockResponse);
    fetchMock.respondWith(mockResponse); // page 2
    fetchMock.respondWith(mockResponse); // after filter change

    render(<MyApplicationsPage />);

    await waitFor(() => {
      expect(screen.getByText('Project 0')).toBeInTheDocument();
    });

    // Go to page 2
    await user.click(screen.getByText('Next'));

    await waitFor(() => {
      expect(screen.getByText('Page 2 of 3')).toBeInTheDocument();
    });

    // Change filter - should reset to page 1
    await user.click(screen.getByRole('button', { name: 'Accepted' }));

    await waitFor(() => {
      const calls = fetchMock.getCalls();
      const lastCall = calls[calls.length - 1];
      expect(lastCall.url).toContain('page=1');
      expect(lastCall.url).toContain('status=Accepted');
    });
  });
});

describe('MyApplicationsPage - Application Display', () => {
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

  it('should display application details (title, budget, duration, date)', async () => {
    const mockResponse = {
      applications: [
        {
          id: 'app-1',
          projectId: 'proj-1',
          projectTitle: 'Full Stack Developer',
          status: 'Pending',
          proposedBudget: 750,
          estimatedDuration: '3 weeks',
          coverLetter: 'I am very interested in this project',
          createdAt: '2025-01-15T00:00:00Z',
          updatedAt: '2025-01-15T00:00:00Z',
        },
      ],
      totalCount: 1,
      pageSize: 10,
      currentPage: 1,
      totalPages: 1,
    };

    fetchMock.respondWith(mockResponse);

    render(<MyApplicationsPage />);

    await waitFor(() => {
      expect(screen.getByText('Full Stack Developer')).toBeInTheDocument();
      expect(screen.getByText('Proposed: 750 credits')).toBeInTheDocument();
      expect(screen.getByText('Duration: 3 weeks')).toBeInTheDocument();
      expect(screen.getByText(/Applied:/)).toBeInTheDocument(); // Date format varies by locale
    });
  });

  it('should show client name if present', async () => {
    const mockResponse = {
      applications: [
        {
          id: 'app-1',
          projectId: 'proj-1',
          projectTitle: 'Project',
          status: 'Pending',
          proposedBudget: 500,
          estimatedDuration: '1 week',
          coverLetter: '',
          createdAt: '2025-01-01T00:00:00Z',
          updatedAt: '2025-01-01T00:00:00Z',
          clientName: 'John Doe',
        },
      ],
      totalCount: 1,
      pageSize: 10,
      currentPage: 1,
      totalPages: 1,
    };

    fetchMock.respondWith(mockResponse);

    render(<MyApplicationsPage />);

    await waitFor(() => {
      expect(screen.getByText('Client: John Doe')).toBeInTheDocument();
    });
  });

  it('should show cover letter if present', async () => {
    const mockResponse = {
      applications: [
        {
          id: 'app-1',
          projectId: 'proj-1',
          projectTitle: 'Project',
          status: 'Pending',
          proposedBudget: 500,
          estimatedDuration: '1 week',
          coverLetter: 'This is my cover letter explaining why I am a good fit',
          createdAt: '2025-01-01T00:00:00Z',
          updatedAt: '2025-01-01T00:00:00Z',
        },
      ],
      totalCount: 1,
      pageSize: 10,
      currentPage: 1,
      totalPages: 1,
    };

    fetchMock.respondWith(mockResponse);

    render(<MyApplicationsPage />);

    await waitFor(() => {
      expect(screen.getByText(/this is my cover letter explaining why i am a good fit/i)).toBeInTheDocument();
    });
  });

  it('should show correct status badge for each status', async () => {
    const mockResponse = {
      applications: [
        { id: 'app-1', projectId: 'p1', projectTitle: 'Pending App', status: 'Pending', proposedBudget: 500, estimatedDuration: '1w', coverLetter: '', createdAt: '2025-01-01T00:00:00Z', updatedAt: '2025-01-01T00:00:00Z' },
        { id: 'app-2', projectId: 'p2', projectTitle: 'Accepted App', status: 'Accepted', proposedBudget: 500, estimatedDuration: '1w', coverLetter: '', createdAt: '2025-01-01T00:00:00Z', updatedAt: '2025-01-01T00:00:00Z' },
        { id: 'app-3', projectId: 'p3', projectTitle: 'Rejected App', status: 'Rejected', proposedBudget: 500, estimatedDuration: '1w', coverLetter: '', createdAt: '2025-01-01T00:00:00Z', updatedAt: '2025-01-01T00:00:00Z' },
        { id: 'app-4', projectId: 'p4', projectTitle: 'Withdrawn App', status: 'Withdrawn', proposedBudget: 500, estimatedDuration: '1w', coverLetter: '', createdAt: '2025-01-01T00:00:00Z', updatedAt: '2025-01-01T00:00:00Z' },
      ],
      totalCount: 4,
      pageSize: 10,
      currentPage: 1,
      totalPages: 1,
    };

    fetchMock.respondWith(mockResponse);

    render(<MyApplicationsPage />);

    await waitFor(() => {
      const statusBadges = screen.getAllByText(/Pending|Accepted|Rejected|Withdrawn/);
      expect(statusBadges.length).toBeGreaterThanOrEqual(4);
    });
  });

  it('should show View Project link for each application', async () => {
    const mockResponse = {
      applications: [
        {
          id: 'app-1',
          projectId: 'proj-123',
          projectTitle: 'Test Project',
          status: 'Pending',
          proposedBudget: 500,
          estimatedDuration: '1 week',
          coverLetter: '',
          createdAt: '2025-01-01T00:00:00Z',
          updatedAt: '2025-01-01T00:00:00Z',
        },
      ],
      totalCount: 1,
      pageSize: 10,
      currentPage: 1,
      totalPages: 1,
    };

    fetchMock.respondWith(mockResponse);

    render(<MyApplicationsPage />);

    await waitFor(() => {
      const viewProjectLink = screen.getByText('View Project');
      expect(viewProjectLink).toBeInTheDocument();
      expect(viewProjectLink.closest('a')).toHaveAttribute('href', '/projects/proj-123');
    });
  });
});

describe('MyApplicationsPage - Pagination', () => {
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
    fetchMock.respondWith({
      applications: [{ id: 'app-1', projectId: 'p1', projectTitle: 'App 1', status: 'Pending', proposedBudget: 500, estimatedDuration: '1w', coverLetter: '', createdAt: '2025-01-01T00:00:00Z', updatedAt: '2025-01-01T00:00:00Z' }],
      totalCount: 15,
      pageSize: 10,
      currentPage: 1,
      totalPages: 2,
    });

    render(<MyApplicationsPage />);

    await waitFor(() => {
      expect(screen.getByText('App 1')).toBeInTheDocument();
    });

    const previousButton = screen.getByText('Previous');
    expect(previousButton).toBeDisabled();
  });

  it('should disable Next button on last page', async () => {
    const page1Response = {
      applications: [{ id: 'app-1', projectId: 'p1', projectTitle: 'App 1', status: 'Pending', proposedBudget: 500, estimatedDuration: '1w', coverLetter: '', createdAt: '2025-01-01T00:00:00Z', updatedAt: '2025-01-01T00:00:00Z' }],
      totalCount: 20,
      pageSize: 10,
      currentPage: 1,
      totalPages: 2,
    };

    const page2Response = {
      applications: [{ id: 'app-2', projectId: 'p2', projectTitle: 'App 2', status: 'Pending', proposedBudget: 500, estimatedDuration: '1w', coverLetter: '', createdAt: '2025-01-01T00:00:00Z', updatedAt: '2025-01-01T00:00:00Z' }],
      totalCount: 20,
      pageSize: 10,
      currentPage: 2,
      totalPages: 2,
    };

    fetchMock.respondWith(page1Response);

    render(<MyApplicationsPage />);

    await waitFor(() => {
      expect(screen.getByText('App 1')).toBeInTheDocument();
    });

    // Navigate to page 2
    fetchMock.respondWith(page2Response);
    const nextButton = screen.getByText('Next');
    await user.click(nextButton);

    // Wait for page 2 to load
    await waitFor(() => {
      expect(screen.getByText('App 2')).toBeInTheDocument();
    });

    // Now on page 2 of 2, Next button should be disabled
    const nextButtonPage2 = screen.getByText('Next');
    expect(nextButtonPage2).toBeDisabled();
  });

  it('should navigate to next page when Next button clicked', async () => {
    const page1Response = {
      applications: [{ id: 'app-1', projectId: 'p1', projectTitle: 'Page 1 App', status: 'Pending', proposedBudget: 500, estimatedDuration: '1w', coverLetter: '', createdAt: '2025-01-01T00:00:00Z', updatedAt: '2025-01-01T00:00:00Z' }],
      totalCount: 20,
      pageSize: 10,
      currentPage: 1,
      totalPages: 2,
    };

    const page2Response = {
      applications: [{ id: 'app-2', projectId: 'p2', projectTitle: 'Page 2 App', status: 'Pending', proposedBudget: 500, estimatedDuration: '1w', coverLetter: '', createdAt: '2025-01-01T00:00:00Z', updatedAt: '2025-01-01T00:00:00Z' }],
      totalCount: 20,
      pageSize: 10,
      currentPage: 2,
      totalPages: 2,
    };

    fetchMock.respondWith(page1Response);
    fetchMock.respondWith(page2Response);

    render(<MyApplicationsPage />);

    await waitFor(() => {
      expect(screen.getByText('Page 1 App')).toBeInTheDocument();
    });

    await user.click(screen.getByText('Next'));

    await waitFor(() => {
      expect(screen.getByText('Page 2 App')).toBeInTheDocument();
    });

    // Verify API was called with page=2
    const calls = fetchMock.getCalls();
    const page2Call = calls.find(c => c.url.includes('page=2'));
    expect(page2Call).toBeDefined();
  });

  it('should navigate to previous page when Previous button clicked', async () => {
    const page1Response = {
      applications: [{ id: 'app-1', projectId: 'p1', projectTitle: 'Page 1 App', status: 'Pending', proposedBudget: 500, estimatedDuration: '1w', coverLetter: '', createdAt: '2025-01-01T00:00:00Z', updatedAt: '2025-01-01T00:00:00Z' }],
      totalCount: 20,
      pageSize: 10,
      currentPage: 1,
      totalPages: 2,
    };

    const page2Response = {
      applications: [{ id: 'app-2', projectId: 'p2', projectTitle: 'Page 2 App', status: 'Pending', proposedBudget: 500, estimatedDuration: '1w', coverLetter: '', createdAt: '2025-01-01T00:00:00Z', updatedAt: '2025-01-01T00:00:00Z' }],
      totalCount: 20,
      pageSize: 10,
      currentPage: 2,
      totalPages: 2,
    };

    fetchMock.respondWith(page1Response);
    fetchMock.respondWith(page2Response);
    fetchMock.respondWith(page1Response); // for going back

    render(<MyApplicationsPage />);

    // Wait for page 1 to load
    await waitFor(() => {
      expect(screen.getByText('Page 1 App')).toBeInTheDocument();
    });

    // Go to page 2
    await user.click(screen.getByText('Next'));

    await waitFor(() => {
      expect(screen.getByText('Page 2 App')).toBeInTheDocument();
    });

    // Go back to page 1
    await user.click(screen.getByText('Previous'));

    await waitFor(() => {
      const calls = fetchMock.getCalls();
      const backToPage1Calls = calls.filter(c => c.url.includes('page=1'));
      expect(backToPage1Calls.length).toBeGreaterThanOrEqual(2); // Initial + after Previous
    });
  });
});

describe('MyApplicationsPage - Empty & Error States', () => {
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

  it('should show empty state when no applications exist', async () => {
    fetchMock.respondWith({ applications: [], totalCount: 0, pageSize: 10, currentPage: 1, totalPages: 1 });

    render(<MyApplicationsPage />);

    await waitFor(() => {
      expect(screen.getByText(/you haven't applied to any projects yet/i)).toBeInTheDocument();
      expect(screen.getByText('Browse Projects')).toBeInTheDocument();
    });
  });

  it('should show filtered empty state message', async () => {
    fetchMock.respondWith({ applications: [], totalCount: 0, pageSize: 10, currentPage: 1, totalPages: 1 });

    render(<MyApplicationsPage />);

    // Wait for initial render
    await waitFor(() => {
      expect(screen.getByText('Filter by status:')).toBeInTheDocument();
    });

    // Click on "Pending" filter
    const pendingButton = screen.getByRole('button', { name: 'Pending' });
    await user.click(pendingButton);

    // Now check for the filtered empty state message
    await waitFor(() => {
      expect(screen.getByText(/you don't have any pending applications/i)).toBeInTheDocument();
    });
  });

  it('should show error state with retry button', async () => {
    fetchMock.respondWithError(500, 'Server Error');

    render(<MyApplicationsPage />);

    await waitFor(() => {
      expect(screen.getByText(/failed to load applications/i)).toBeInTheDocument();
      expect(screen.getByText('Try Again')).toBeInTheDocument();
    });
  });

  it('should not show pagination when totalPages is 1', async () => {
    fetchMock.respondWith({
      applications: [{ id: 'app-1', projectId: 'p1', projectTitle: 'App 1', status: 'Pending', proposedBudget: 500, estimatedDuration: '1w', coverLetter: '', createdAt: '2025-01-01T00:00:00Z', updatedAt: '2025-01-01T00:00:00Z' }],
      totalCount: 1,
      pageSize: 10,
      currentPage: 1,
      totalPages: 1,
    });

    render(<MyApplicationsPage />);

    await waitFor(() => {
      expect(screen.getByText('App 1')).toBeInTheDocument();
    });

    expect(screen.queryByText('Previous')).not.toBeInTheDocument();
    expect(screen.queryByText('Next')).not.toBeInTheDocument();
  });

  it('should retry fetching applications when Try Again button is clicked', async () => {
    // First request fails
    fetchMock.respondWithError(500, 'Server Error');

    render(<MyApplicationsPage />);

    await waitFor(() => {
      expect(screen.getByText(/failed to load applications/i)).toBeInTheDocument();
    });

    // Second request succeeds
    const mockResponse = {
      applications: [
        {
          id: 'app-1',
          projectId: 'proj-1',
          projectTitle: 'Retry Success Project',
          status: 'Pending',
          proposedBudget: 500,
          estimatedDuration: '1 week',
          coverLetter: '',
          createdAt: '2025-01-01T00:00:00Z',
          updatedAt: '2025-01-01T00:00:00Z',
        },
      ],
      totalCount: 1,
      pageSize: 10,
      currentPage: 1,
      totalPages: 1,
    };

    fetchMock.respondWith(mockResponse);

    // Click Try Again button (covers line 211)
    const tryAgainButton = screen.getByText('Try Again');
    await user.click(tryAgainButton);

    await waitFor(() => {
      expect(screen.getByText('Retry Success Project')).toBeInTheDocument();
    });
  });

  it('should handle unknown status with default styling', async () => {
    const mockResponse = {
      applications: [
        {
          id: 'app-1',
          projectId: 'proj-1',
          projectTitle: 'Unknown Status App',
          status: 'InReview', // Unknown status - covers line 119 (default case)
          proposedBudget: 500,
          estimatedDuration: '1 week',
          coverLetter: '',
          createdAt: '2025-01-01T00:00:00Z',
          updatedAt: '2025-01-01T00:00:00Z',
        },
      ],
      totalCount: 1,
      pageSize: 10,
      currentPage: 1,
      totalPages: 1,
    };

    fetchMock.respondWith(mockResponse);

    render(<MyApplicationsPage />);

    await waitFor(() => {
      expect(screen.getByText('Unknown Status App')).toBeInTheDocument();
      expect(screen.getByText('InReview')).toBeInTheDocument();
    });
  });
});
