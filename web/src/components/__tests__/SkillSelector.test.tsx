import React from 'react';
import { render, screen, fireEvent, waitFor, act } from '@testing-library/react';
import '@testing-library/jest-dom';
import SkillSelector from '../SkillSelector';

// Mock fetch
global.fetch = jest.fn();
const mockFetch = fetch as jest.MockedFunction<typeof fetch>;

// Mock localStorage
const mockLocalStorage = {
  getItem: jest.fn(),
  setItem: jest.fn(),
  removeItem: jest.fn(),
  clear: jest.fn(),
};
Object.defineProperty(window, 'localStorage', {
  value: mockLocalStorage,
});

// Mock skills data
const mockSkills = [
  {
    id: 'skill1',
    name: 'React',
    description: 'JavaScript library for building user interfaces',
    category: 'Frontend Development',
    isSystemManaged: true,
    isActive: true,
    createdAt: '2023-01-01T00:00:00Z'
  },
  {
    id: 'skill2',
    name: 'TypeScript',
    description: 'Typed superset of JavaScript',
    category: 'Programming Languages',
    isSystemManaged: true,
    isActive: true,
    createdAt: '2023-01-01T00:00:00Z'
  },
  {
    id: 'skill3',
    name: 'Node.js',
    description: 'JavaScript runtime for server-side development',
    category: 'Backend Development',
    isSystemManaged: true,
    isActive: true,
    createdAt: '2023-01-01T00:00:00Z'
  }
];

const mockCategories = [
  { name: 'Frontend Development', skillCount: 25, userCount: 150 },
  { name: 'Backend Development', skillCount: 30, userCount: 120 },
  { name: 'Programming Languages', skillCount: 20, userCount: 200 }
];

describe('SkillSelector', () => {
  const mockProps = {
    selectedSkills: [],
    onSkillsChange: jest.fn(),
    minSkills: 3
  };

  beforeEach(() => {
    jest.clearAllMocks();
    mockLocalStorage.getItem.mockReturnValue('mock-token');

    // Default fetch mock
    mockFetch.mockImplementation((url) => {
      if (typeof url === 'string' && url.includes('/api/skill/categories')) {
        return Promise.resolve({
          ok: true,
          json: async () => mockCategories,
        } as Response);
      }
      if (typeof url === 'string' && url.includes('/api/skill')) {
        return Promise.resolve({
          ok: true,
          json: async () => ({ skills: mockSkills, totalCount: mockSkills.length }),
        } as Response);
      }
      return Promise.reject(new Error('Unknown URL'));
    });
  });

  it('renders skill selector with search input', async () => {
    await act(async () => {
      await act(async () => {
      render(<SkillSelector {...mockProps} />);
    });
    });

    await waitFor(() => {
      expect(screen.getByPlaceholderText(/search skills/i)).toBeInTheDocument();
    });
  });

  it('renders category filter dropdown', async () => {
    await act(async () => {
      await act(async () => {
      render(<SkillSelector {...mockProps} />);
    });
    });

    await waitFor(() => {
      expect(screen.getByRole('combobox', { name: /category/i })).toBeInTheDocument();
    });
  });

  it('loads skills on mount', async () => {
    await act(async () => {
      render(<SkillSelector {...mockProps} />);
    });

    await waitFor(() => {
      expect(mockFetch).toHaveBeenCalledWith(
        expect.stringContaining('/api/skill'),
        expect.any(Object)
      );
    });
  });

  it('loads categories on mount', async () => {
    await act(async () => {
      render(<SkillSelector {...mockProps} />);
    });

    await waitFor(() => {
      expect(mockFetch).toHaveBeenCalledWith(
        expect.stringContaining('/api/skill/categories'),
        expect.any(Object)
      );
    });
  });

  it('displays loading state while fetching skills', async () => {
    mockFetch.mockImplementation(() => new Promise(() => {})); // Never resolves
    await act(async () => {
      render(<SkillSelector {...mockProps} />);
    });

    const searchInput = screen.getByPlaceholderText(/search skills/i);
    fireEvent.focus(searchInput);

    await waitFor(() => {
      expect(screen.getByText(/loading skills/i)).toBeInTheDocument();
    });
  });

  it('filters skills by search term with debouncing', async () => {
    jest.useFakeTimers({ advanceTimers: true });
    await act(async () => {
      render(<SkillSelector {...mockProps} />);
    });

    const searchInput = await screen.findByPlaceholderText(/search skills/i);

    fireEvent.change(searchInput, { target: { value: 'React' } });

    // Should not call fetch immediately
    expect(mockFetch).not.toHaveBeenCalledWith(
      expect.stringContaining('React'),
      expect.any(Object)
    );

    // Fast-forward time
    act(() => {
      jest.advanceTimersByTime(300);
    });

    await waitFor(() => {
      expect(mockFetch).toHaveBeenCalledWith(
        expect.stringContaining('React'),
        expect.any(Object)
      );
    });

    jest.useRealTimers();
  });

  it('filters skills by category', async () => {
    await act(async () => {
      render(<SkillSelector {...mockProps} />);
    });

    // Wait for initial load
    await waitFor(() => {
      expect(mockFetch).toHaveBeenCalled();
    });

    jest.clearAllMocks();

    const categorySelect = await screen.findByRole('combobox', { name: /category/i });

    fireEvent.change(categorySelect, { target: { value: 'Frontend Development' } });

    // Need to wait for debounce and then check the call
    await waitFor(() => {
      const calls = mockFetch.mock.calls;
      const found = calls.some(call =>
        typeof call[0] === 'string' && call[0].includes('category=Frontend')
      );
      expect(found).toBe(true);
    }, { timeout: 1000 });
  });

  it('displays available skills in dropdown', async () => {
    await act(async () => {
      render(<SkillSelector {...mockProps} />);
    });

    const searchInput = await screen.findByPlaceholderText(/search skills/i);
    fireEvent.focus(searchInput);

    await waitFor(() => {
      expect(screen.getByText('React')).toBeInTheDocument();
      expect(screen.getByText('TypeScript')).toBeInTheDocument();
      expect(screen.getByText('Node.js')).toBeInTheDocument();
    });
  });

  it('adds skill when clicked from dropdown', async () => {
    await act(async () => {
      render(<SkillSelector {...mockProps} />);
    });

    const searchInput = await screen.findByPlaceholderText(/search skills/i);
    fireEvent.focus(searchInput);

    const reactSkill = await screen.findByText('React');

    await act(async () => {
      fireEvent.click(reactSkill);
    });

    await waitFor(() => {
      expect(mockProps.onSkillsChange).toHaveBeenCalledWith(
        expect.arrayContaining([
          expect.objectContaining({
            skillId: 'skill1',
            proficiency: 'Beginner'
          })
        ])
      );
    });
  });

  it('displays selected skills as cards', async () => {
    const selectedSkills = [
      {
        skillId: 'skill1',
        skillName: 'React',
        category: 'Frontend Development',
        proficiency: 'Advanced' as const,
        yearsOfExperience: 3
      }
    ];

    await act(async () => {
      render(<SkillSelector {...mockProps} selectedSkills={selectedSkills} />);
    });

    expect(screen.getByText('React')).toBeInTheDocument();
    // Advanced is in the select element's option, not as text
    const proficiencySelect = screen.getByRole('combobox', { name: /proficiency level/i });
    expect(proficiencySelect).toHaveValue('Advanced');
    expect(screen.getByText('Frontend Development')).toBeInTheDocument();
  });

  it('shows proficiency selector for each skill', async () => {
    const selectedSkills = [
      {
        skillId: 'skill1',
        skillName: 'React',
        category: 'Frontend Development',
        proficiency: 'Beginner' as const
      }
    ];

    await act(async () => {
      render(<SkillSelector {...mockProps} selectedSkills={selectedSkills} />);
    });

    const proficiencySelect = screen.getByRole('combobox', { name: /proficiency level/i });
    expect(proficiencySelect).toHaveValue('Beginner');
  });

  it('updates proficiency level', async () => {
    const selectedSkills = [
      {
        skillId: 'skill1',
        skillName: 'React',
        category: 'Frontend Development',
        proficiency: 'Beginner' as const
      }
    ];

    await act(async () => {
      render(<SkillSelector {...mockProps} selectedSkills={selectedSkills} />);
    });

    const proficiencySelect = screen.getByRole('combobox', { name: /proficiency level/i });

    await act(async () => {
      fireEvent.change(proficiencySelect, { target: { value: 'Advanced' } });
    });

    await waitFor(() => {
      expect(mockProps.onSkillsChange).toHaveBeenCalledWith(
        expect.arrayContaining([
          expect.objectContaining({
            skillId: 'skill1',
            proficiency: 'Advanced'
          })
        ])
      );
    });
  });

  it('removes skill when delete button clicked', async () => {
    const selectedSkills = [
      {
        skillId: 'skill1',
        skillName: 'React',
        category: 'Frontend Development',
        proficiency: 'Beginner' as const
      }
    ];

    await act(async () => {
      render(<SkillSelector {...mockProps} selectedSkills={selectedSkills} />);
    });

    const removeButton = screen.getByRole('button', { name: /remove.*react/i });

    await act(async () => {
      fireEvent.click(removeButton);
    });

    await waitFor(() => {
      expect(mockProps.onSkillsChange).toHaveBeenCalledWith([]);
    });
  });

  it('shows validation message when minimum skills not met', async () => {
    await act(async () => {
      render(<SkillSelector {...mockProps} selectedSkills={[]} minSkills={3} />);
    });

    expect(screen.getByText(/select at least 3 skills/i)).toBeInTheDocument();
  });

  it('does not show validation message when minimum skills met', async () => {
    const selectedSkills = [
      { skillId: 'skill1', skillName: 'React', category: 'Frontend', proficiency: 'Beginner' as const },
      { skillId: 'skill2', skillName: 'TypeScript', category: 'Languages', proficiency: 'Intermediate' as const },
      { skillId: 'skill3', skillName: 'Node.js', category: 'Backend', proficiency: 'Advanced' as const }
    ];

    await act(async () => {
      render(<SkillSelector {...mockProps} selectedSkills={selectedSkills} minSkills={3} />);
    });

    expect(screen.queryByText(/select at least 3 skills/i)).not.toBeInTheDocument();
  });

  it('prevents adding duplicate skills', async () => {
    const selectedSkills = [
      {
        skillId: 'skill1',
        skillName: 'React',
        category: 'Frontend Development',
        proficiency: 'Beginner' as const
      }
    ];

    await act(async () => {
      render(<SkillSelector {...mockProps} selectedSkills={selectedSkills} />);
    });

    const searchInput = await screen.findByPlaceholderText(/search skills/i);
    fireEvent.focus(searchInput);

    // React should not appear in dropdown because it's already selected
    await waitFor(() => {
      const reactOptions = screen.queryAllByText('React');
      // Only one React should appear (in the selected skills card, not in dropdown)
      expect(reactOptions.length).toBe(1);
    });
  });

  it('displays proficiency descriptions', async () => {
    const selectedSkills = [
      {
        skillId: 'skill1',
        skillName: 'React',
        category: 'Frontend Development',
        proficiency: 'Expert' as const
      }
    ];

    await act(async () => {
      render(<SkillSelector {...mockProps} selectedSkills={selectedSkills} />);
    });

    const proficiencySelect = screen.getByRole('combobox', { name: /proficiency level/i });
    expect(proficiencySelect).toHaveValue('Expert');
  });

  it('handles API errors gracefully', async () => {
    // Mock the fetch to reject on both calls (categories and skills)
    mockFetch.mockRejectedValue(new Error('API Error'));

    await act(async () => {
      render(<SkillSelector {...mockProps} />);
    });

    // Error will be shown after attempting to load skills
    await waitFor(() => {
      expect(screen.getByText(/failed to load/i)).toBeInTheDocument();
    }, { timeout: 3000 });
  });

  // Note: Auth token tests removed - application now uses cookie-based authentication instead of Bearer tokens

  it('shows skill count indicator', async () => {
    const selectedSkills = [
      { skillId: 'skill1', skillName: 'React', category: 'Frontend', proficiency: 'Beginner' as const },
      { skillId: 'skill2', skillName: 'TypeScript', category: 'Languages', proficiency: 'Intermediate' as const }
    ];

    await act(async () => {
      render(<SkillSelector {...mockProps} selectedSkills={selectedSkills} minSkills={3} />);
    });

    expect(screen.getByText(/2.*3.*skills/i)).toBeInTheDocument();
  });

  it('clears search input after selecting skill', async () => {
    await act(async () => {
      render(<SkillSelector {...mockProps} />);
    });

    const searchInput = await screen.findByPlaceholderText(/search skills/i);

    await act(async () => {
      fireEvent.change(searchInput, { target: { value: 'React' } });
      fireEvent.focus(searchInput);
    });

    const reactSkill = await screen.findByText('React');

    await act(async () => {
      fireEvent.click(reactSkill);
    });

    await waitFor(() => {
      expect(searchInput).toHaveValue('');
    });
  });

  it('allows optional years of experience input', async () => {
    const selectedSkills = [
      {
        skillId: 'skill1',
        skillName: 'React',
        category: 'Frontend Development',
        proficiency: 'Advanced' as const
      }
    ];

    await act(async () => {
      render(<SkillSelector {...mockProps} selectedSkills={selectedSkills} />);
    });

    const yearsInput = screen.getByLabelText(/years of experience/i);

    await act(async () => {
      fireEvent.change(yearsInput, { target: { value: '5' } });
    });

    await waitFor(() => {
      expect(mockProps.onSkillsChange).toHaveBeenCalledWith(
        expect.arrayContaining([
          expect.objectContaining({
            skillId: 'skill1',
            yearsOfExperience: 5
          })
        ])
      );
    });
  });

  it('shows visual feedback for proficiency levels with color coding', async () => {
    const selectedSkills = [
      { skillId: 'skill1', skillName: 'React', category: 'Frontend', proficiency: 'Beginner' as const },
      { skillId: 'skill2', skillName: 'TypeScript', category: 'Languages', proficiency: 'Expert' as const }
    ];

    await act(async () => {
      render(<SkillSelector {...mockProps} selectedSkills={selectedSkills} />);
    });

    // Find the badge span elements by their text and check the span itself, not its parent div
    const beginnerBadge = screen.getByText((content, element) => {
      return content === 'Beginner' && element?.tagName.toLowerCase() === 'span';
    });
    const expertBadge = screen.getByText((content, element) => {
      return content === 'Expert' && element?.tagName.toLowerCase() === 'span';
    });

    expect(beginnerBadge).toHaveClass('bg-muted');
    expect(expertBadge).toHaveClass('bg-accent/10');
  });
});
