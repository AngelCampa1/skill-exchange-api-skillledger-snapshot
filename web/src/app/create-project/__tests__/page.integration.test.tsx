/**
 * create-project/page.tsx Integration Tests
 *
 * Tests project creation page with skills loading, draft mode, and form submission.
 * Focus: API integration, draft save, URL parameters, error handling, navigation.
 *
 * Coverage Target: 85%+ (543 lines)
 * Test Count: 35 tests
 */

import React from 'react';
import { render, screen, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { useRouter, useSearchParams } from 'next/navigation';
import CreateProjectPage from '../page';
import { setupFetchMock } from '@/utils/test/testUtils';

// Mock dependencies
jest.mock('next/navigation', () => ({
  useRouter: jest.fn(),
  useSearchParams: jest.fn(),
}));

jest.mock('@/components/ProjectCreationForm', () => ({
  __esModule: true,
  default: ({ onSubmit, onSaveDraft, isLoading, isDraftMode }: any) => (
    <div data-testid="project-creation-form">
      <button onClick={() => onSubmit({
        title: 'Test Project',
        description: 'Test Description',
        creditBudget: 1000,
        deliverables: [],
        requiredSkills: []
      })}>
        Submit Project
      </button>
      <button onClick={() => onSaveDraft({
        title: 'Draft Project',
        description: 'Draft Description'
      })}>
        Save Draft
      </button>
      <div data-testid="form-loading">{isLoading ? 'Loading' : 'Ready'}</div>
      <div data-testid="form-draft-mode">{isDraftMode ? 'Draft' : 'Standard'}</div>
    </div>
  ),
}));

jest.mock('@/components/SubscriptionGuard', () => ({
  ProjectCreationGuard: ({ children }: any) => <div>{children}</div>,
}));

jest.mock('@/utils/logger', () => ({
  logger: {
    debug: jest.fn(),
    error: jest.fn(),
    warn: jest.fn(),
  },
}));

jest.mock('@/utils/analytics', () => ({
  trackEvent: jest.fn(),
}));

const mockUseRouter = useRouter as jest.MockedFunction<typeof useRouter>;
const mockUseSearchParams = useSearchParams as jest.MockedFunction<typeof useSearchParams>;

describe('CreateProjectPage - Skills Loading', () => {
  let fetchMock: ReturnType<typeof setupFetchMock>;
  const mockPush = jest.fn();
  const mockBack = jest.fn();

  beforeEach(() => {
    fetchMock = setupFetchMock();

    mockUseRouter.mockReturnValue({
      push: mockPush,
      back: mockBack,
    } as any);

    mockUseSearchParams.mockReturnValue({
      get: jest.fn(() => null),
    } as any);

    jest.clearAllMocks();
  });

  afterEach(() => {
    fetchMock.reset();
  });

  it('should show loading state while fetching skills', () => {
    fetchMock.respondWith({ Skills: [] });

    render(<CreateProjectPage />);

    expect(screen.getByText('Loading skills...')).toBeInTheDocument();
    expect(screen.getByText('Loading skills...').previousSibling).toHaveClass('loading-spinner');
  });

  it('should load skills successfully and show form', async () => {
    const mockSkills = [
      { id: '1', name: 'JavaScript', description: 'JS programming', category: 'Programming' },
      { id: '2', name: 'React', description: 'React framework', category: 'Framework' },
    ];

    fetchMock.respondWith({ Skills: mockSkills });

    render(<CreateProjectPage />);

    await waitFor(() => {
      expect(screen.getByTestId('project-creation-form')).toBeInTheDocument();
    });

    expect(fetchMock.getCalls()[0].url).toBe('/api/skill?take=100');
    expect(screen.queryByText('Loading skills...')).not.toBeInTheDocument();
  });

  it('should handle different skills response structures (data.skills)', async () => {
    const mockSkills = [{ id: '1', name: 'Python', description: 'Python lang', category: 'Programming' }];

    fetchMock.respondWith({ skills: mockSkills });

    render(<CreateProjectPage />);

    await waitFor(() => {
      expect(screen.getByTestId('project-creation-form')).toBeInTheDocument();
    });
  });

  it('should handle array response structure', async () => {
    const mockSkills = [{ id: '1', name: 'TypeScript', description: 'TS lang', category: 'Programming' }];

    fetchMock.respondWith(mockSkills);

    render(<CreateProjectPage />);

    await waitFor(() => {
      expect(screen.getByTestId('project-creation-form')).toBeInTheDocument();
    });
  });

  it('should show error when skills API fails', async () => {
    fetchMock.respondWithError(500, 'Server error');

    render(<CreateProjectPage />);

    await waitFor(() => {
      expect(screen.getByText('Failed to load available skills.')).toBeInTheDocument();
    });
  });

  it('should show error when no skills are returned', async () => {
    fetchMock.respondWith({ Skills: [] });

    render(<CreateProjectPage />);

    await waitFor(() => {
      expect(screen.getByText('No skills available. Please contact support.')).toBeInTheDocument();
    });
  });

  it('should allow retry after skills loading error', async () => {
    const user = userEvent.setup();

    fetchMock.respondWithError(500, 'Server error');

    render(<CreateProjectPage />);

    await waitFor(() => {
      expect(screen.getByText('Failed to load available skills.')).toBeInTheDocument();
    });

    const mockSkills = [{ id: '1', name: 'Java', description: 'Java lang', category: 'Programming' }];
    fetchMock.respondWith({ Skills: mockSkills });

    const retryButton = screen.getByText('Try Again');
    await user.click(retryButton);

    await waitFor(() => {
      expect(screen.getByTestId('project-creation-form')).toBeInTheDocument();
    });

    expect(fetchMock.getCalls().length).toBe(2); // Initial + retry
  });
});

describe('CreateProjectPage - URL Parameters & Draft Mode', () => {
  let fetchMock: ReturnType<typeof setupFetchMock>;
  const mockPush = jest.fn();
  const mockBack = jest.fn();

  beforeEach(() => {
    fetchMock = setupFetchMock();

    mockUseRouter.mockReturnValue({
      push: mockPush,
      back: mockBack,
    } as any);

    jest.clearAllMocks();
  });

  afterEach(() => {
    fetchMock.reset();
  });

  it('should enable draft mode when URL has draft=true parameter', async () => {
    mockUseSearchParams.mockReturnValue({
      get: (key: string) => key === 'draft' ? 'true' : null,
    } as any);

    fetchMock.respondWith({ Skills: [{ id: '1', name: 'Skill1', description: 'Desc', category: 'Cat' }] });

    render(<CreateProjectPage />);

    await waitFor(() => {
      expect(screen.getByTestId('project-creation-form')).toBeInTheDocument();
    });

    expect(screen.getByTestId('form-draft-mode')).toHaveTextContent('Draft');
    expect(screen.getByText('📝 Draft Mode')).toBeInTheDocument();
    expect(screen.getByText(/Working in draft mode/)).toBeInTheDocument();
  });

  it('should load existing project when URL has id parameter', async () => {
    mockUseSearchParams.mockReturnValue({
      get: (key: string) => key === 'id' ? 'project-123' : null,
    } as any);

    const mockSkills = [{ id: '1', name: 'Skill1', description: 'Desc', category: 'Cat' }];
    fetchMock.respondWith({ Skills: mockSkills });

    const mockProject = {
      id: 'project-123',
      title: 'Existing Project',
      description: 'Existing Description',
      creditBudget: 2000,
      startDate: '2025-01-15T00:00:00Z',
      endDate: '2025-02-15T00:00:00Z',
      deliverables: [{ description: 'Deliverable 1', isRequired: true }],
      requiredSkills: [{ skillId: '1', proficiencyRequired: 4, weight: 2 }],
    };
    fetchMock.respondWith(mockProject);

    render(<CreateProjectPage />);

    await waitFor(() => {
      expect(screen.getByTestId('project-creation-form')).toBeInTheDocument();
    });

    const calls = fetchMock.getCalls();
    expect(calls[1].url).toBe('/api/project/project-123');
    expect(screen.getByText('Edit Project')).toBeInTheDocument();
  });

  it('should show error when loading non-existent project (404)', async () => {
    mockUseSearchParams.mockReturnValue({
      get: (key: string) => key === 'id' ? 'nonexistent' : null,
    } as any);

    fetchMock.respondWith({ Skills: [{ id: '1', name: 'Skill1', description: 'Desc', category: 'Cat' }] });
    fetchMock.respondWithError(404, 'Not found');

    render(<CreateProjectPage />);

    await waitFor(() => {
      expect(screen.getByText('Project not found or you do not have permission to edit it.')).toBeInTheDocument();
    });
  });

  it('should toggle draft mode when button clicked', async () => {
    const user = userEvent.setup();

    mockUseSearchParams.mockReturnValue({
      get: jest.fn(() => null),
    } as any);

    fetchMock.respondWith({ Skills: [{ id: '1', name: 'Skill1', description: 'Desc', category: 'Cat' }] });

    render(<CreateProjectPage />);

    await waitFor(() => {
      expect(screen.getByTestId('project-creation-form')).toBeInTheDocument();
    });

    expect(screen.getByTestId('form-draft-mode')).toHaveTextContent('Standard');

    const toggleButton = screen.getByText('📄 Standard Mode');
    await user.click(toggleButton);

    expect(screen.getByTestId('form-draft-mode')).toHaveTextContent('Draft');
    expect(screen.getByText('📝 Draft Mode')).toBeInTheDocument();
  });
});

describe('CreateProjectPage - Form Submission', () => {
  let fetchMock: ReturnType<typeof setupFetchMock>;
  const mockPush = jest.fn();
  const mockBack = jest.fn();

  beforeEach(() => {
    fetchMock = setupFetchMock();

    mockUseRouter.mockReturnValue({
      push: mockPush,
      back: mockBack,
    } as any);

    mockUseSearchParams.mockReturnValue({
      get: jest.fn(() => null),
    } as any);

    jest.clearAllMocks();
    jest.useFakeTimers({ advanceTimers: true });
  });

  afterEach(() => {
    fetchMock.reset();
    jest.runOnlyPendingTimers();
    jest.useRealTimers();
  });

  it('should submit project creation successfully', async () => {
    const user = userEvent.setup({ delay: null });

    // Set up all mock responses BEFORE rendering
    const mockResponses = [
      // 1. Initial skills load
      { Skills: [{ id: '1', name: 'Skill1', description: 'Desc', category: 'Cat' }] },
      // 2. CSRF token request
      { token: 'csrf-token-123' },
      // 3. Project creation
      {
        success: true,
        message: 'Project created successfully!',
        project: { id: 'new-project-123', title: 'Test Project', status: 'Open' },
      },
    ];

    mockResponses.forEach(response => fetchMock.respondWith(response));

    render(<CreateProjectPage />);

    await waitFor(() => {
      expect(screen.getByTestId('project-creation-form')).toBeInTheDocument();
    });

    const submitButton = screen.getByText('Submit Project');
    await user.click(submitButton);

    await waitFor(() => {
      expect(screen.getByText('Project created successfully!')).toBeInTheDocument();
      expect(screen.getByText('Redirecting to your project...')).toBeInTheDocument();
    });

    // Should redirect after 2 seconds
    jest.advanceTimersByTime(2000);
    await waitFor(() => {
      expect(mockPush).toHaveBeenCalledWith('/project/new-project-123');
    });
  });

  it('should update existing project when editing', async () => {
    const user = userEvent.setup({ delay: null });

    mockUseSearchParams.mockReturnValue({
      get: (key: string) => key === 'id' ? 'existing-123' : null,
    } as any);

    // Set up all mock responses BEFORE rendering
    const mockResponses = [
      // 1. Skills load
      { Skills: [{ id: '1', name: 'Skill1', description: 'Desc', category: 'Cat' }] },
      // 2. Project data load
      {
        id: 'existing-123',
        title: 'Existing Project',
        description: 'Desc',
        creditBudget: 1000,
        deliverables: [],
        requiredSkills: [],
      },
      // 3. CSRF token
      { token: 'csrf-token-123' },
      // 4. Project update
      {
        success: true,
        message: 'Project updated successfully!',
        project: { id: 'existing-123', title: 'Updated Project', status: 'Open' },
      },
    ];

    mockResponses.forEach(response => fetchMock.respondWith(response));

    render(<CreateProjectPage />);

    // Wait for both Edit Project title AND form to be ready
    await waitFor(() => {
      expect(screen.getByText('Edit Project')).toBeInTheDocument();
      expect(screen.getByTestId('project-creation-form')).toBeInTheDocument();
    });

    const submitButton = screen.getByText('Submit Project');
    await user.click(submitButton);

    await waitFor(() => {
      expect(screen.getByText('Project updated successfully!')).toBeInTheDocument();
      expect(screen.getByText('Redirecting to your project...')).toBeInTheDocument();
    });

    // Should redirect after 2 seconds
    jest.advanceTimersByTime(2000);
    await waitFor(() => {
      expect(mockPush).toHaveBeenCalledWith('/project/existing-123');
    });
  });

  it('should handle 401 unauthorized error', async () => {
    const user = userEvent.setup({ delay: null });

    fetchMock.respondWith({ Skills: [{ id: '1', name: 'Skill1', description: 'Desc', category: 'Cat' }] });

    render(<CreateProjectPage />);

    await waitFor(() => {
      expect(screen.getByTestId('project-creation-form')).toBeInTheDocument();
    });

    fetchMock.respondWith({ token: 'csrf-token-123' });
    fetchMock.respondWithError(401, 'Unauthorized');

    const submitButton = screen.getByText('Submit Project');
    await user.click(submitButton);

    await waitFor(() => {
      expect(screen.getByText('You must be logged in to create projects.')).toBeInTheDocument();
    });
  });

  it('should handle 403 forbidden error', async () => {
    const user = userEvent.setup({ delay: null });

    fetchMock.respondWith({ Skills: [{ id: '1', name: 'Skill1', description: 'Desc', category: 'Cat' }] });

    render(<CreateProjectPage />);

    await waitFor(() => {
      expect(screen.getByTestId('project-creation-form')).toBeInTheDocument();
    });

    fetchMock.respondWith({ token: 'csrf-token-123' });
    fetchMock.respondWithError(403, 'Forbidden');

    const submitButton = screen.getByText('Submit Project');
    await user.click(submitButton);

    await waitFor(() => {
      expect(screen.getByText('You do not have permission to perform this action.')).toBeInTheDocument();
    });
  });

  it('should handle 429 rate limit error', async () => {
    const user = userEvent.setup({ delay: null });

    fetchMock.respondWith({ Skills: [{ id: '1', name: 'Skill1', description: 'Desc', category: 'Cat' }] });

    render(<CreateProjectPage />);

    await waitFor(() => {
      expect(screen.getByTestId('project-creation-form')).toBeInTheDocument();
    });

    fetchMock.respondWith({ token: 'csrf-token-123' });
    fetchMock.respondWithError(429, 'Too many requests');

    const submitButton = screen.getByText('Submit Project');
    await user.click(submitButton);

    await waitFor(() => {
      expect(screen.getByText('Too many requests. Please wait a moment and try again.')).toBeInTheDocument();
    });
  });

  it('should handle CSRF token fetch failure', async () => {
    const user = userEvent.setup({ delay: null });

    fetchMock.respondWith({ Skills: [{ id: '1', name: 'Skill1', description: 'Desc', category: 'Cat' }] });

    render(<CreateProjectPage />);

    await waitFor(() => {
      expect(screen.getByTestId('project-creation-form')).toBeInTheDocument();
    });

    fetchMock.respondWithError(500, 'Server error');

    const submitButton = screen.getByText('Submit Project');
    await user.click(submitButton);

    await waitFor(() => {
      expect(screen.getByText('Network error. Please check your connection and try again.')).toBeInTheDocument();
    });
  });

  it('should handle network error during submission', async () => {
    const user = userEvent.setup({ delay: null });

    fetchMock.respondWith({ Skills: [{ id: '1', name: 'Skill1', description: 'Desc', category: 'Cat' }] });

    render(<CreateProjectPage />);

    await waitFor(() => {
      expect(screen.getByTestId('project-creation-form')).toBeInTheDocument();
    });

    fetchMock.respondWith({ token: 'csrf-token-123' });
    fetchMock.respondWithError(500, 'Network error');

    const submitButton = screen.getByText('Submit Project');
    await user.click(submitButton);

    await waitFor(() => {
      expect(screen.getByText(/Network error/)).toBeInTheDocument();
    });
  });

  it('should show loading state during submission', async () => {
    const user = userEvent.setup({ delay: null });

    fetchMock.respondWith({ Skills: [{ id: '1', name: 'Skill1', description: 'Desc', category: 'Cat' }] });

    render(<CreateProjectPage />);

    await waitFor(() => {
      expect(screen.getByTestId('project-creation-form')).toBeInTheDocument();
    });

    expect(screen.getByTestId('form-loading')).toHaveTextContent('Ready');

    fetchMock.respondWith({ token: 'csrf-token-123' });
    fetchMock.respondWith({
      success: true,
      message: 'Success',
      project: { id: '1', title: 'Test', status: 'Open' },
    });

    const submitButton = screen.getByText('Submit Project');
    await user.click(submitButton);

    // Should show loading during submission
    expect(screen.getByTestId('form-loading')).toHaveTextContent('Loading');

    await waitFor(() => {
      expect(screen.getByTestId('form-loading')).toHaveTextContent('Ready');
    });
  });
});

describe('CreateProjectPage - Draft Save', () => {
  let fetchMock: ReturnType<typeof setupFetchMock>;
  const mockPush = jest.fn();
  const mockBack = jest.fn();

  beforeEach(() => {
    fetchMock = setupFetchMock();

    mockUseRouter.mockReturnValue({
      push: mockPush,
      back: mockBack,
    } as any);

    mockUseSearchParams.mockReturnValue({
      get: jest.fn(() => null),
    } as any);

    jest.clearAllMocks();
    jest.useFakeTimers({ advanceTimers: true });
  });

  afterEach(() => {
    fetchMock.reset();
    jest.runOnlyPendingTimers();
    jest.useRealTimers();
  });

  it('should save draft successfully', async () => {
    const user = userEvent.setup({ delay: null });

    fetchMock.respondWith({ Skills: [{ id: '1', name: 'Skill1', description: 'Desc', category: 'Cat' }] });

    render(<CreateProjectPage />);

    await waitFor(() => {
      expect(screen.getByTestId('project-creation-form')).toBeInTheDocument();
    });

    fetchMock.respondWith({ token: 'csrf-token-123' });
    fetchMock.respondWith({
      success: true,
      message: 'Draft saved',
      project: { id: 'draft-123', title: 'Draft Project', status: 'Draft' },
    });

    const saveDraftButton = screen.getByText('Save Draft');
    await user.click(saveDraftButton);

    await waitFor(() => {
      expect(screen.getByText('✓ Draft saved')).toBeInTheDocument();
    });

    // Should auto-dismiss after 3 seconds
    jest.advanceTimersByTime(3000);
    await waitFor(() => {
      expect(screen.queryByText('✓ Draft saved')).not.toBeInTheDocument();
    });
  });

  it('should show saving status indicator', async () => {
    const user = userEvent.setup({ delay: null });

    fetchMock.respondWith({ Skills: [{ id: '1', name: 'Skill1', description: 'Desc', category: 'Cat' }] });

    render(<CreateProjectPage />);

    await waitFor(() => {
      expect(screen.getByTestId('project-creation-form')).toBeInTheDocument();
    });

    fetchMock.respondWith({ token: 'csrf-token-123' });
    fetchMock.respondWith({
      success: true,
      message: 'Draft saved',
      project: { id: 'draft-123', title: 'Draft', status: 'Draft' },
    });

    const saveDraftButton = screen.getByText('Save Draft');
    await user.click(saveDraftButton);

    // Should show saving indicator
    expect(screen.getByText('Saving draft...')).toBeInTheDocument();

    await waitFor(() => {
      expect(screen.getByText('✓ Draft saved')).toBeInTheDocument();
    });
  });

  it('should handle draft save error', async () => {
    const user = userEvent.setup({ delay: null });

    fetchMock.respondWith({ Skills: [{ id: '1', name: 'Skill1', description: 'Desc', category: 'Cat' }] });

    render(<CreateProjectPage />);

    await waitFor(() => {
      expect(screen.getByTestId('project-creation-form')).toBeInTheDocument();
    });

    fetchMock.respondWith({ token: 'csrf-token-123' });
    fetchMock.respondWithError(500, 'Server error');

    const saveDraftButton = screen.getByText('Save Draft');
    await user.click(saveDraftButton);

    await waitFor(() => {
      expect(screen.getByText('✗ Failed to save draft')).toBeInTheDocument();
    });

    // Should auto-dismiss error after 5 seconds
    jest.advanceTimersByTime(5000);
    await waitFor(() => {
      expect(screen.queryByText('✗ Failed to save draft')).not.toBeInTheDocument();
    });
  });

  it('should handle CSRF token failure for draft save', async () => {
    const user = userEvent.setup({ delay: null });

    fetchMock.respondWith({ Skills: [{ id: '1', name: 'Skill1', description: 'Desc', category: 'Cat' }] });

    render(<CreateProjectPage />);

    await waitFor(() => {
      expect(screen.getByTestId('project-creation-form')).toBeInTheDocument();
    });

    fetchMock.respondWithError(500, 'Server error');

    const saveDraftButton = screen.getByText('Save Draft');
    await user.click(saveDraftButton);

    await waitFor(() => {
      expect(screen.getByText('✗ Failed to save draft')).toBeInTheDocument();
    });
  });
});

describe('CreateProjectPage - Navigation & UI', () => {
  let fetchMock: ReturnType<typeof setupFetchMock>;
  const mockPush = jest.fn();
  const mockBack = jest.fn();

  beforeEach(() => {
    fetchMock = setupFetchMock();

    mockUseRouter.mockReturnValue({
      push: mockPush,
      back: mockBack,
    } as any);

    mockUseSearchParams.mockReturnValue({
      get: jest.fn(() => null),
    } as any);

    jest.clearAllMocks();
  });

  afterEach(() => {
    fetchMock.reset();
  });

  it('should navigate back when Cancel button clicked', async () => {
    const user = userEvent.setup();

    fetchMock.respondWith({ Skills: [{ id: '1', name: 'Skill1', description: 'Desc', category: 'Cat' }] });

    render(<CreateProjectPage />);

    await waitFor(() => {
      expect(screen.getByTestId('project-creation-form')).toBeInTheDocument();
    });

    const cancelButton = screen.getByText('Cancel');
    await user.click(cancelButton);

    expect(mockBack).toHaveBeenCalledTimes(1);
  });

  it('should show help section with tips', async () => {
    fetchMock.respondWith({ Skills: [{ id: '1', name: 'Skill1', description: 'Desc', category: 'Cat' }] });

    render(<CreateProjectPage />);

    await waitFor(() => {
      expect(screen.getByTestId('project-creation-form')).toBeInTheDocument();
    });

    expect(screen.getByText('Tips for Creating a Great Project')).toBeInTheDocument();
    expect(screen.getByText('Writing Project Descriptions')).toBeInTheDocument();
    expect(screen.getByText('Setting Requirements')).toBeInTheDocument();
    expect(screen.getByText('Be specific about your goals and expectations')).toBeInTheDocument();
  });

  it('should show correct title for new project', async () => {
    fetchMock.respondWith({ Skills: [{ id: '1', name: 'Skill1', description: 'Desc', category: 'Cat' }] });

    render(<CreateProjectPage />);

    await waitFor(() => {
      expect(screen.getByText('Create New Project')).toBeInTheDocument();
    });
  });

  it('should show Suspense fallback while loading', () => {
    const { container } = render(<CreateProjectPage />);

    expect(container.querySelector('.loading-spinner')).toBeInTheDocument();
  });

  it('should toggle draft mode on and then off', async () => {
    const user = userEvent.setup();

    fetchMock.respondWith({ Skills: [{ id: '1', name: 'Skill1', description: 'Desc', category: 'Cat' }] });

    render(<CreateProjectPage />);

    await waitFor(() => {
      expect(screen.getByTestId('project-creation-form')).toBeInTheDocument();
    });

    // Initially in standard mode
    expect(screen.getByTestId('form-draft-mode')).toHaveTextContent('Standard');

    // Toggle ON - draft mode
    const toggleButton = screen.getByText('📄 Standard Mode');
    await user.click(toggleButton);

    expect(screen.getByTestId('form-draft-mode')).toHaveTextContent('Draft');
    expect(screen.getByText('📝 Draft Mode')).toBeInTheDocument();

    // Toggle OFF - back to standard mode (covers line 380)
    const draftButton = screen.getByText('📝 Draft Mode');
    await user.click(draftButton);

    expect(screen.getByTestId('form-draft-mode')).toHaveTextContent('Standard');
  });
});

describe('CreateProjectPage - Error Handling Edge Cases', () => {
  let fetchMock: ReturnType<typeof setupFetchMock>;
  const mockPush = jest.fn();
  const mockBack = jest.fn();

  beforeEach(() => {
    fetchMock = setupFetchMock();

    mockUseRouter.mockReturnValue({
      push: mockPush,
      back: mockBack,
    } as any);

    mockUseSearchParams.mockReturnValue({
      get: jest.fn(() => null),
    } as any);

    jest.clearAllMocks();
    jest.useFakeTimers();
  });

  afterEach(() => {
    fetchMock.reset();
    jest.runOnlyPendingTimers();
    jest.useRealTimers();
  });

  it('should handle skills loading network error (catch block)', async () => {
    // Import logger mock
    const { logger } = require('@/utils/logger');

    // Override fetch to reject (network error, not HTTP error)
    global.fetch = jest.fn(() => Promise.reject(new Error('Network failure'))) as any;

    render(<CreateProjectPage />);

    await waitFor(() => {
      expect(screen.getByText('Failed to load available skills.')).toBeInTheDocument();
    });

    // Verify logger.error was called with correct params (covers lines 121-122)
    expect(logger.error).toHaveBeenCalledWith(
      'Error loading skills',
      expect.any(Error),
      { component: 'CreateProject' }
    );
  });

  it('should handle project loading non-404 HTTP error', async () => {
    mockUseSearchParams.mockReturnValue({
      get: (key: string) => key === 'id' ? 'project-456' : null,
    } as any);

    fetchMock.respondWith({ Skills: [{ id: '1', name: 'Skill1', description: 'Desc', category: 'Cat' }] });
    // Return 500 error for project fetch (covers lines 169-170)
    fetchMock.respondWithError(500, 'Internal Server Error');

    render(<CreateProjectPage />);

    await waitFor(() => {
      expect(screen.getByText('Failed to load project data.')).toBeInTheDocument();
    });
  });

  it('should handle project loading network error (catch block)', async () => {
    const { logger } = require('@/utils/logger');

    mockUseSearchParams.mockReturnValue({
      get: (key: string) => key === 'id' ? 'project-789' : null,
    } as any);

    // First fetch succeeds (skills), second fetch fails (project)
    let callCount = 0;
    global.fetch = jest.fn(() => {
      callCount++;
      if (callCount === 1) {
        // Skills load succeeds
        return Promise.resolve({
          ok: true,
          status: 200,
          json: async () => ({ Skills: [{ id: '1', name: 'Skill1', description: 'Desc', category: 'Cat' }] }),
        } as Response);
      }
      // Project load fails with network error (covers lines 172-173)
      return Promise.reject(new Error('Network timeout'));
    }) as any;

    render(<CreateProjectPage />);

    await waitFor(() => {
      expect(screen.getByText('Failed to load project data.')).toBeInTheDocument();
    });

    expect(logger.error).toHaveBeenCalledWith(
      'Error loading project',
      expect.any(Error),
      { component: 'CreateProject' }
    );
  });

  it('should handle CSRF token fetch failure', async () => {
    const user = userEvent.setup({ delay: null });
    const { logger } = require('@/utils/logger');

    fetchMock.respondWith({ Skills: [{ id: '1', name: 'Skill1', description: 'Desc', category: 'Cat' }] });

    render(<CreateProjectPage />);

    await waitFor(() => {
      expect(screen.getByTestId('project-creation-form')).toBeInTheDocument();
    });

    // Make CSRF token fetch fail (covers line 191)
    global.fetch = jest.fn(() => Promise.reject(new Error('CSRF fetch failed'))) as any;

    const submitButton = screen.getByText('Submit Project');
    await user.click(submitButton);

    await waitFor(() => {
      expect(logger.error).toHaveBeenCalledWith(
        'Failed to get CSRF token',
        expect.any(Error),
        { component: 'CreateProject' }
      );
    });
  });

  it('should navigate to /my-projects when project creation response has no id', async () => {
    const user = userEvent.setup({ delay: null });

    fetchMock.respondWith({ Skills: [{ id: '1', name: 'Skill1', description: 'Desc', category: 'Cat' }] });

    render(<CreateProjectPage />);

    await waitFor(() => {
      expect(screen.getByTestId('project-creation-form')).toBeInTheDocument();
    });

    fetchMock.respondWith({ token: 'csrf-token-123' });
    // Response without project.id (covers line 265)
    fetchMock.respondWith({
      success: true,
      message: 'Project created',
      project: { title: 'Test Project' }, // No id field
    });

    const submitButton = screen.getByText('Submit Project');
    await user.click(submitButton);

    await waitFor(() => {
      expect(screen.getByText('Project created')).toBeInTheDocument();
    });

    // Wait for setTimeout to execute
    jest.advanceTimersByTime(2000);

    await waitFor(() => {
      expect(mockPush).toHaveBeenCalledWith('/my-projects');
    });
  });

  it('should handle draft save network error with auto-dismiss', async () => {
    const user = userEvent.setup({ delay: null });
    const { logger } = require('@/utils/logger');

    fetchMock.respondWith({ Skills: [{ id: '1', name: 'Skill1', description: 'Desc', category: 'Cat' }] });

    render(<CreateProjectPage />);

    await waitFor(() => {
      expect(screen.getByTestId('project-creation-form')).toBeInTheDocument();
    });

    // First fetch succeeds (CSRF token), then network error on draft save (covers lines 362-367)
    let callCount = 0;
    global.fetch = jest.fn(() => {
      callCount++;
      if (callCount === 1) {
        // CSRF token succeeds
        return Promise.resolve({
          ok: true,
          status: 200,
          json: async () => ({ token: 'csrf-token-123' }),
        } as Response);
      }
      // Draft save fails with network error
      return Promise.reject(new Error('Network error'));
    }) as any;

    const saveDraftButton = screen.getByText('Save Draft');
    await user.click(saveDraftButton);

    await waitFor(() => {
      expect(logger.error).toHaveBeenCalledWith(
        'Error saving draft',
        expect.any(Error),
        { component: 'CreateProject' }
      );
      expect(screen.getByText('✗ Failed to save draft')).toBeInTheDocument();
    });

    // Should auto-dismiss error after 5 seconds
    jest.advanceTimersByTime(5000);

    await waitFor(() => {
      expect(screen.queryByText('✗ Failed to save draft')).not.toBeInTheDocument();
    });
  });
});
