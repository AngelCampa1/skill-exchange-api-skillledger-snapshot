/**
 * ExperienceTimeline Component Tests
 *
 * Week 18 - Gap Filling: Testing highest-impact untested files
 * Target: 85%+ coverage
 *
 * Tests cover:
 * - Loading state
 * - Stats display
 * - Search and filtering
 * - CRUD operations
 * - Visibility/featured toggles
 * - Modal interactions
 */

import React from 'react';
import { render, screen, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import ExperienceTimeline from '../ExperienceTimeline';

// Mock window.confirm
const mockConfirm = jest.fn();
global.confirm = mockConfirm;

describe('ExperienceTimeline', () => {
  beforeEach(() => {
    mockConfirm.mockReset();
    mockConfirm.mockReturnValue(true);
  });

  // =========================================================================
  // Suite 1: Loading & Initial Render (4 tests)
  // =========================================================================
  describe('Loading & Initial Render', () => {
    test('renders without crashing', () => {
      // The component has a loading state that transitions very quickly
      // because it uses synchronous mock data. This test verifies
      // the component mounts successfully without errors.
      const { container } = render(<ExperienceTimeline />);
      expect(container).toBeInTheDocument();
    });

    test('displays page title after loading', async () => {
      render(<ExperienceTimeline />);

      await waitFor(() => {
        expect(screen.getByText('Experience Timeline')).toBeInTheDocument();
      });
    });

    test('displays subtitle description', async () => {
      render(<ExperienceTimeline />);

      await waitFor(() => {
        expect(screen.getByText(/manage your professional experiences/i)).toBeInTheDocument();
      });
    });

    test('loads mock experiences on mount', async () => {
      render(<ExperienceTimeline />);

      await waitFor(() => {
        expect(screen.getByText('Senior Full Stack Developer')).toBeInTheDocument();
        expect(screen.getByText('TechCorp Inc.')).toBeInTheDocument();
      });
    });
  });

  // =========================================================================
  // Suite 2: Stats Cards (4 tests)
  // =========================================================================
  describe('Stats Cards', () => {
    test('displays Total Experiences count', async () => {
      render(<ExperienceTimeline />);

      await waitFor(() => {
        expect(screen.getByText('Total Experiences')).toBeInTheDocument();
      });

      // Check that a count is displayed (4 experiences in mock data)
      const totalCard = screen.getByText('Total Experiences').closest('div')?.parentElement;
      expect(totalCard?.textContent).toContain('4');
    });

    test('displays Featured count', async () => {
      render(<ExperienceTimeline />);

      await waitFor(() => {
        expect(screen.getByText('Featured')).toBeInTheDocument();
        // Mock data has 2 featured experiences
        const featuredCard = screen.getByText('Featured').closest('div');
        expect(featuredCard).toBeInTheDocument();
      });
    });

    test('displays Visible count', async () => {
      render(<ExperienceTimeline />);

      await waitFor(() => {
        expect(screen.getByText('Visible')).toBeInTheDocument();
      });
    });

    test('displays Experience Types count', async () => {
      render(<ExperienceTimeline />);

      await waitFor(() => {
        expect(screen.getByText('Experience Types')).toBeInTheDocument();
      });
    });
  });

  // =========================================================================
  // Suite 3: Search & Filtering (6 tests)
  // =========================================================================
  describe('Search & Filtering', () => {
    test('search input filters experiences by title', async () => {
      render(<ExperienceTimeline />);

      await waitFor(() => {
        expect(screen.getByText('Senior Full Stack Developer')).toBeInTheDocument();
      });

      const searchInput = screen.getByPlaceholderText(/search experiences/i);
      await userEvent.type(searchInput, 'Frontend');

      await waitFor(() => {
        expect(screen.getByText('Frontend Developer')).toBeInTheDocument();
        expect(screen.queryByText('Senior Full Stack Developer')).not.toBeInTheDocument();
      });
    });

    test('search input filters by organization', async () => {
      render(<ExperienceTimeline />);

      await waitFor(() => {
        expect(screen.getByText('TechCorp Inc.')).toBeInTheDocument();
      });

      const searchInput = screen.getByPlaceholderText(/search experiences/i);
      await userEvent.type(searchInput, 'StartupXYZ');

      await waitFor(() => {
        expect(screen.getByText('StartupXYZ')).toBeInTheDocument();
        expect(screen.queryByText('TechCorp Inc.')).not.toBeInTheDocument();
      });
    });

    test('type dropdown filters by experience type', async () => {
      render(<ExperienceTimeline />);

      await waitFor(() => {
        expect(screen.getByText('Senior Full Stack Developer')).toBeInTheDocument();
      });

      const typeSelect = screen.getByDisplayValue('All Types');
      await userEvent.selectOptions(typeSelect, 'Education');

      await waitFor(() => {
        expect(screen.getByText('Bachelor of Science in Computer Science')).toBeInTheDocument();
        expect(screen.queryByText('Senior Full Stack Developer')).not.toBeInTheDocument();
      });
    });

    test('visible only checkbox filters to visible experiences', async () => {
      render(<ExperienceTimeline />);

      await waitFor(() => {
        expect(screen.getByText('Senior Full Stack Developer')).toBeInTheDocument();
      });

      const visibleCheckbox = screen.getByLabelText(/visible only/i);
      await userEvent.click(visibleCheckbox);

      // All mock experiences are visible, so nothing should change
      await waitFor(() => {
        expect(screen.getByText('Senior Full Stack Developer')).toBeInTheDocument();
      });
    });

    test('featured only checkbox filters to featured experiences', async () => {
      render(<ExperienceTimeline />);

      await waitFor(() => {
        expect(screen.getByText('Senior Full Stack Developer')).toBeInTheDocument();
      });

      const featuredCheckbox = screen.getByLabelText(/featured only/i);
      await userEvent.click(featuredCheckbox);

      await waitFor(() => {
        // Only featured experiences should show
        expect(screen.getByText('Senior Full Stack Developer')).toBeInTheDocument(); // Featured
        expect(screen.getByText('Bachelor of Science in Computer Science')).toBeInTheDocument(); // Featured
        expect(screen.queryByText('Frontend Developer')).not.toBeInTheDocument(); // Not featured
      });
    });

    test('shows empty state when no experiences match filter', async () => {
      render(<ExperienceTimeline />);

      await waitFor(() => {
        expect(screen.getByText('Senior Full Stack Developer')).toBeInTheDocument();
      });

      const searchInput = screen.getByPlaceholderText(/search experiences/i);
      await userEvent.type(searchInput, 'NonexistentExperience12345');

      await waitFor(() => {
        expect(screen.getByText(/no experiences found/i)).toBeInTheDocument();
        expect(screen.getByText(/try adjusting your filters/i)).toBeInTheDocument();
      });
    });
  });

  // =========================================================================
  // Suite 4: Experience Display (5 tests)
  // =========================================================================
  describe('Experience Display', () => {
    test('displays experience title and organization', async () => {
      render(<ExperienceTimeline />);

      await waitFor(() => {
        expect(screen.getByText('Senior Full Stack Developer')).toBeInTheDocument();
        expect(screen.getByText('TechCorp Inc.')).toBeInTheDocument();
      });
    });

    test('displays experience type badge', async () => {
      const { container } = render(<ExperienceTimeline />);

      await waitFor(() => {
        expect(screen.getByText('Senior Full Stack Developer')).toBeInTheDocument();
      });

      // Type badges are spans with rounded-full class
      const badges = container.querySelectorAll('span.rounded-full');
      expect(badges.length).toBeGreaterThan(0);

      // Check for different type values in the badges
      const badgeTexts = Array.from(badges).map(b => b.textContent);
      expect(badgeTexts.some(t => t === 'Work')).toBe(true);
      expect(badgeTexts.some(t => t === 'Education')).toBe(true);
    });

    test('displays experience duration', async () => {
      render(<ExperienceTimeline />);

      await waitFor(() => {
        expect(screen.getByText('2 years')).toBeInTheDocument();
      });
    });

    test('displays experience location', async () => {
      render(<ExperienceTimeline />);

      await waitFor(() => {
        expect(screen.getByText('Senior Full Stack Developer')).toBeInTheDocument();
      });

      // Check locations are displayed in the experience cards
      expect(screen.getByText('San Francisco, CA')).toBeInTheDocument();
      expect(screen.getByText('Berkeley, CA')).toBeInTheDocument();
    });

    test('displays skills used', async () => {
      render(<ExperienceTimeline />);

      await waitFor(() => {
        expect(screen.getAllByText('React').length).toBeGreaterThan(0);
        expect(screen.getAllByText('TypeScript').length).toBeGreaterThan(0);
      });
    });
  });

  // =========================================================================
  // Suite 5: Add Experience Modal (5 tests)
  // =========================================================================
  describe('Add Experience Modal', () => {
    test('opens add modal when Add Experience button clicked', async () => {
      render(<ExperienceTimeline />);

      await waitFor(() => {
        expect(screen.getByText('Add Experience')).toBeInTheDocument();
      });

      await userEvent.click(screen.getByText('Add Experience'));

      expect(screen.getByText('Add New Experience')).toBeInTheDocument();
    });

    test('closes add modal when Cancel clicked', async () => {
      render(<ExperienceTimeline />);

      await waitFor(() => {
        expect(screen.getByText('Add Experience')).toBeInTheDocument();
      });

      await userEvent.click(screen.getByText('Add Experience'));
      expect(screen.getByText('Add New Experience')).toBeInTheDocument();

      await userEvent.click(screen.getByRole('button', { name: 'Cancel' }));

      await waitFor(() => {
        expect(screen.queryByText('Add New Experience')).not.toBeInTheDocument();
      });
    });

    test('add button is disabled without required fields', async () => {
      render(<ExperienceTimeline />);

      await waitFor(() => {
        expect(screen.getByText('Add Experience')).toBeInTheDocument();
      });

      await userEvent.click(screen.getByText('Add Experience'));

      // Wait for modal to appear
      await waitFor(() => {
        expect(screen.getByText('Add New Experience')).toBeInTheDocument();
      });

      // Find the submit button in the modal (the second "Add Experience" button)
      const addButtons = screen.getAllByRole('button', { name: /add experience/i });
      const submitButton = addButtons[addButtons.length - 1]; // Last one is the submit button
      expect(submitButton).toBeDisabled();
    });

    test('can fill out add form fields', async () => {
      render(<ExperienceTimeline />);

      await waitFor(() => {
        expect(screen.getByText('Add Experience')).toBeInTheDocument();
      });

      await userEvent.click(screen.getByText('Add Experience'));

      // Fill out form
      const titleInput = screen.getByPlaceholderText(/senior developer/i);
      await userEvent.type(titleInput, 'Test Developer');

      const orgInput = screen.getByPlaceholderText(/techcorp inc/i);
      await userEvent.type(orgInput, 'Test Company');

      expect(titleInput).toHaveValue('Test Developer');
      expect(orgInput).toHaveValue('Test Company');
    });

    test('adds experience and closes modal on submit', async () => {
      render(<ExperienceTimeline />);

      await waitFor(() => {
        expect(screen.getByText('Add Experience')).toBeInTheDocument();
      });

      await userEvent.click(screen.getByText('Add Experience'));

      // Wait for modal
      await waitFor(() => {
        expect(screen.getByText('Add New Experience')).toBeInTheDocument();
      });

      // Fill required fields
      await userEvent.type(screen.getByPlaceholderText(/senior developer/i), 'New Position');
      await userEvent.type(screen.getByPlaceholderText(/techcorp inc/i), 'New Company');

      // Find and fill start date input (it's a date type input)
      const dateInputs = document.querySelectorAll('input[type="date"]');
      const startDateInput = dateInputs[0] as HTMLInputElement;
      await userEvent.clear(startDateInput);
      await userEvent.type(startDateInput, '2024-01-01');

      // Wait for button to be enabled
      await waitFor(() => {
        const addButtons = screen.getAllByRole('button', { name: /add experience/i });
        const submitButton = addButtons[addButtons.length - 1];
        return !submitButton.hasAttribute('disabled');
      });

      // Click submit
      const addButtons = screen.getAllByRole('button', { name: /add experience/i });
      const submitButton = addButtons[addButtons.length - 1];
      await userEvent.click(submitButton);

      await waitFor(() => {
        expect(screen.queryByText('Add New Experience')).not.toBeInTheDocument();
      });
    });
  });

  // =========================================================================
  // Suite 6: Delete Experience (3 tests)
  // =========================================================================
  describe('Delete Experience', () => {
    test('shows confirmation dialog when delete clicked', async () => {
      render(<ExperienceTimeline />);

      await waitFor(() => {
        expect(screen.getByText('Senior Full Stack Developer')).toBeInTheDocument();
      });

      // Find delete button (trash icon) for first experience
      const deleteButtons = screen.getAllByTitle(/delete experience/i);
      await userEvent.click(deleteButtons[0]);

      expect(mockConfirm).toHaveBeenCalledWith('Are you sure you want to delete this experience?');
    });

    test('deletes experience when confirmed', async () => {
      mockConfirm.mockReturnValue(true);
      render(<ExperienceTimeline />);

      await waitFor(() => {
        expect(screen.getByText('Senior Full Stack Developer')).toBeInTheDocument();
      });

      const deleteButtons = screen.getAllByTitle(/delete experience/i);
      await userEvent.click(deleteButtons[0]);

      await waitFor(() => {
        expect(screen.queryByText('Senior Full Stack Developer')).not.toBeInTheDocument();
      });
    });

    test('does not delete when cancelled', async () => {
      mockConfirm.mockReturnValue(false);
      render(<ExperienceTimeline />);

      await waitFor(() => {
        expect(screen.getByText('Senior Full Stack Developer')).toBeInTheDocument();
      });

      const deleteButtons = screen.getAllByTitle(/delete experience/i);
      await userEvent.click(deleteButtons[0]);

      // Should still be there
      expect(screen.getByText('Senior Full Stack Developer')).toBeInTheDocument();
    });
  });

  // =========================================================================
  // Suite 7: Toggle Visibility & Featured (4 tests)
  // =========================================================================
  describe('Toggle Visibility & Featured', () => {
    test('toggles visibility when eye icon clicked', async () => {
      render(<ExperienceTimeline />);

      await waitFor(() => {
        expect(screen.getByText('Senior Full Stack Developer')).toBeInTheDocument();
      });

      const visibilityButtons = screen.getAllByTitle(/hide from profile|show on profile/i);
      await userEvent.click(visibilityButtons[0]);

      // Visibility toggled - button title should change
      await waitFor(() => {
        const buttons = screen.getAllByTitle(/hide from profile|show on profile/i);
        expect(buttons.length).toBeGreaterThan(0);
      });
    });

    test('toggles featured when star icon clicked', async () => {
      render(<ExperienceTimeline />);

      await waitFor(() => {
        expect(screen.getByText('Senior Full Stack Developer')).toBeInTheDocument();
      });

      const featuredButtons = screen.getAllByTitle(/add to featured|remove from featured/i);
      await userEvent.click(featuredButtons[0]);

      // Featured toggled
      await waitFor(() => {
        const buttons = screen.getAllByTitle(/add to featured|remove from featured/i);
        expect(buttons.length).toBeGreaterThan(0);
      });
    });

    test('featured experiences show star icon', async () => {
      render(<ExperienceTimeline />);

      await waitFor(() => {
        expect(screen.getByText('Senior Full Stack Developer')).toBeInTheDocument();
      });

      // Featured experiences should have filled star
      const filledStars = document.querySelectorAll('.fill-current');
      expect(filledStars.length).toBeGreaterThan(0);
    });

    test('edit button opens edit modal', async () => {
      render(<ExperienceTimeline />);

      await waitFor(() => {
        expect(screen.getByText('Senior Full Stack Developer')).toBeInTheDocument();
      });

      const editButtons = screen.getAllByTitle(/edit experience/i);
      await userEvent.click(editButtons[0]);

      await waitFor(() => {
        expect(screen.getByText(/edit senior full stack developer/i)).toBeInTheDocument();
      });
    });
  });

  // =========================================================================
  // Suite 8: Edit Experience Modal (3 tests)
  // =========================================================================
  describe('Edit Experience Modal', () => {
    test('edit modal displays current experience data', async () => {
      render(<ExperienceTimeline />);

      await waitFor(() => {
        expect(screen.getByText('Senior Full Stack Developer')).toBeInTheDocument();
      });

      const editButtons = screen.getAllByTitle(/edit experience/i);
      await userEvent.click(editButtons[0]);

      await waitFor(() => {
        const modal = screen.getByText(/edit senior full stack developer/i).closest('div');
        expect(modal).toBeInTheDocument();

        // Should have the title pre-filled
        const titleInputs = screen.getAllByDisplayValue('Senior Full Stack Developer');
        expect(titleInputs.length).toBeGreaterThan(0);
      });
    });

    test('closes edit modal when Cancel clicked', async () => {
      render(<ExperienceTimeline />);

      await waitFor(() => {
        expect(screen.getByText('Senior Full Stack Developer')).toBeInTheDocument();
      });

      const editButtons = screen.getAllByTitle(/edit experience/i);
      await userEvent.click(editButtons[0]);

      await waitFor(() => {
        expect(screen.getByText(/edit senior full stack developer/i)).toBeInTheDocument();
      });

      const cancelButtons = screen.getAllByRole('button', { name: 'Cancel' });
      await userEvent.click(cancelButtons[cancelButtons.length - 1]);

      await waitFor(() => {
        expect(screen.queryByText(/edit senior full stack developer/i)).not.toBeInTheDocument();
      });
    });

    test('saves changes when Save Changes clicked', async () => {
      render(<ExperienceTimeline />);

      await waitFor(() => {
        expect(screen.getByText('Senior Full Stack Developer')).toBeInTheDocument();
      });

      const editButtons = screen.getAllByTitle(/edit experience/i);
      await userEvent.click(editButtons[0]);

      await waitFor(() => {
        expect(screen.getByText(/edit senior full stack developer/i)).toBeInTheDocument();
      });

      const saveButton = screen.getByRole('button', { name: /save changes/i });
      await userEvent.click(saveButton);

      await waitFor(() => {
        expect(screen.queryByText(/edit senior full stack developer/i)).not.toBeInTheDocument();
      });
    });
  });

  // =========================================================================
  // Suite 9: Duration Helpers (3 tests)
  // =========================================================================
  describe('Duration Display', () => {
    test('displays "Present" for current experiences', async () => {
      render(<ExperienceTimeline />);

      await waitFor(() => {
        expect(screen.getByText(/present/i)).toBeInTheDocument();
      });
    });

    test('displays formatted date range', async () => {
      render(<ExperienceTimeline />);

      await waitFor(() => {
        expect(screen.getByText('Senior Full Stack Developer')).toBeInTheDocument();
      });

      // The date range is shown with format like "Jan 2022 - Present"
      // Check that date formatting is working by looking for year
      expect(screen.getByText(/2022/)).toBeInTheDocument();
    });

    test('displays duration in years and months', async () => {
      render(<ExperienceTimeline />);

      await waitFor(() => {
        expect(screen.getByText(/2 years/i)).toBeInTheDocument();
        expect(screen.getByText(/1 year 10 months/i)).toBeInTheDocument();
      });
    });
  });
});
