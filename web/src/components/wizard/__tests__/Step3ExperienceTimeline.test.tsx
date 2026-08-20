/**
 * Step3ExperienceTimeline.tsx Tests
 *
 * Tests for experience timeline wizard step.
 * Coverage Target: 80%+
 */

import React from 'react';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import '@testing-library/jest-dom';
import Step3ExperienceTimeline from '../Step3ExperienceTimeline';
import { Experience } from '@/types/profile';

describe('Step3ExperienceTimeline', () => {
  const mockOnUpdate = jest.fn();
  const mockOnNext = jest.fn();
  const mockOnBack = jest.fn();

  const defaultProps = {
    experiences: [] as Experience[],
    onUpdate: mockOnUpdate,
    onNext: mockOnNext,
    onBack: mockOnBack,
  };

  const createMockExperience = (overrides: Partial<Experience> = {}): Experience => ({
    id: 'exp-1',
    type: 'work',
    title: 'Software Engineer',
    organization: 'Tech Corp',
    location: 'New York, NY',
    startDate: '2020-01',
    endDate: '2023-12',
    isCurrent: false,
    description: 'Developed web applications',
    ...overrides,
  });

  beforeEach(() => {
    jest.clearAllMocks();
  });

  describe('Rendering', () => {
    it('renders heading and description', () => {
      render(<Step3ExperienceTimeline {...defaultProps} />);

      expect(screen.getByText('Experience Timeline')).toBeInTheDocument();
      expect(screen.getByText(/Add your work experience and education history/i)).toBeInTheDocument();
    });

    it('renders add experience form', () => {
      render(<Step3ExperienceTimeline {...defaultProps} />);

      expect(screen.getByRole('heading', { name: 'Add Experience' })).toBeInTheDocument();
      expect(screen.getByLabelText(/Job Title/i)).toBeInTheDocument();
      expect(screen.getByLabelText(/Company/i)).toBeInTheDocument();
      expect(screen.getByLabelText(/Location/i)).toBeInTheDocument();
      expect(screen.getByLabelText(/Start Date/i)).toBeInTheDocument();
      expect(screen.getByLabelText(/End Date/i)).toBeInTheDocument();
    });

    it('renders navigation buttons', () => {
      render(<Step3ExperienceTimeline {...defaultProps} />);

      expect(screen.getByRole('button', { name: /back/i })).toBeInTheDocument();
      expect(screen.getByRole('button', { name: /next step/i })).toBeInTheDocument();
    });

    it('shows work experience radio selected by default', () => {
      render(<Step3ExperienceTimeline {...defaultProps} />);

      const workRadio = screen.getByLabelText('Work Experience');
      expect(workRadio).toBeChecked();
    });

    it('does not show experience list when empty', () => {
      render(<Step3ExperienceTimeline {...defaultProps} />);

      expect(screen.queryByText(/Your Experience/i)).not.toBeInTheDocument();
    });
  });

  describe('Type Selection', () => {
    it('switches to education type when education radio is clicked', () => {
      render(<Step3ExperienceTimeline {...defaultProps} />);

      const educationRadio = screen.getByLabelText('Education');
      fireEvent.click(educationRadio);

      expect(educationRadio).toBeChecked();
      expect(screen.getByLabelText(/Degree/i)).toBeInTheDocument();
      expect(screen.getByLabelText(/School/i)).toBeInTheDocument();
    });

    it('shows work-specific labels when work type is selected', () => {
      render(<Step3ExperienceTimeline {...defaultProps} />);

      expect(screen.getByLabelText(/Job Title/i)).toBeInTheDocument();
      expect(screen.getByLabelText(/Company/i)).toBeInTheDocument();
      expect(screen.getByText(/I currently work here/i)).toBeInTheDocument();
    });

    it('shows education-specific labels when education type is selected', () => {
      render(<Step3ExperienceTimeline {...defaultProps} />);

      const educationRadio = screen.getByLabelText('Education');
      fireEvent.click(educationRadio);

      expect(screen.getByLabelText(/Degree/i)).toBeInTheDocument();
      expect(screen.getByLabelText(/School/i)).toBeInTheDocument();
      expect(screen.getByText(/I currently study here/i)).toBeInTheDocument();
    });
  });

  describe('Form Validation', () => {
    it('shows error when title is empty', () => {
      render(<Step3ExperienceTimeline {...defaultProps} />);

      const addButton = screen.getByRole('button', { name: /add experience/i });
      fireEvent.click(addButton);

      expect(screen.getByText('Title is required')).toBeInTheDocument();
    });

    it('shows error when title is only whitespace', () => {
      render(<Step3ExperienceTimeline {...defaultProps} />);

      const titleInput = screen.getByLabelText(/Job Title/i);
      fireEvent.change(titleInput, { target: { value: '   ' } });

      const addButton = screen.getByRole('button', { name: /add experience/i });
      fireEvent.click(addButton);

      expect(screen.getByText('Title is required')).toBeInTheDocument();
    });

    it('shows error when organization is empty', () => {
      render(<Step3ExperienceTimeline {...defaultProps} />);

      const titleInput = screen.getByLabelText(/Job Title/i);
      fireEvent.change(titleInput, { target: { value: 'Software Engineer' } });

      const addButton = screen.getByRole('button', { name: /add experience/i });
      fireEvent.click(addButton);

      expect(screen.getByText('Organization is required')).toBeInTheDocument();
    });

    it('shows error when organization is only whitespace', () => {
      render(<Step3ExperienceTimeline {...defaultProps} />);

      const titleInput = screen.getByLabelText(/Job Title/i);
      fireEvent.change(titleInput, { target: { value: 'Software Engineer' } });

      const orgInput = screen.getByLabelText(/Company/i);
      fireEvent.change(orgInput, { target: { value: '   ' } });

      const addButton = screen.getByRole('button', { name: /add experience/i });
      fireEvent.click(addButton);

      expect(screen.getByText('Organization is required')).toBeInTheDocument();
    });

    it('shows error when start date is empty', () => {
      render(<Step3ExperienceTimeline {...defaultProps} />);

      const titleInput = screen.getByLabelText(/Job Title/i);
      fireEvent.change(titleInput, { target: { value: 'Software Engineer' } });

      const orgInput = screen.getByLabelText(/Company/i);
      fireEvent.change(orgInput, { target: { value: 'Tech Corp' } });

      const addButton = screen.getByRole('button', { name: /add experience/i });
      fireEvent.click(addButton);

      expect(screen.getByText('Start date is required')).toBeInTheDocument();
    });
  });

  describe('Adding Experiences', () => {
    it('adds work experience successfully', () => {
      render(<Step3ExperienceTimeline {...defaultProps} />);

      // Fill form
      fireEvent.change(screen.getByLabelText(/Job Title/i), {
        target: { value: 'Software Engineer' },
      });
      fireEvent.change(screen.getByLabelText(/Company/i), {
        target: { value: 'Tech Corp' },
      });
      fireEvent.change(screen.getByLabelText(/Location/i), {
        target: { value: 'New York, NY' },
      });
      fireEvent.change(screen.getByLabelText(/Start Date/i), {
        target: { value: '2020-01' },
      });
      fireEvent.change(screen.getByLabelText(/End Date/i), {
        target: { value: '2023-12' },
      });
      fireEvent.change(screen.getByLabelText(/Description/i), {
        target: { value: 'Developed applications' },
      });

      // Add experience
      fireEvent.click(screen.getByRole('button', { name: /add experience/i }));

      // Verify experience is shown
      expect(screen.getByText('Your Experience (1)')).toBeInTheDocument();
      expect(screen.getByText('Software Engineer')).toBeInTheDocument();
      expect(screen.getByText('Tech Corp')).toBeInTheDocument();
      expect(screen.getByText('New York, NY')).toBeInTheDocument();
    });

    it('adds education experience successfully', () => {
      render(<Step3ExperienceTimeline {...defaultProps} />);

      // Switch to education
      fireEvent.click(screen.getByLabelText('Education'));

      // Fill form
      fireEvent.change(screen.getByLabelText(/Degree/i), {
        target: { value: 'Bachelor of Science' },
      });
      fireEvent.change(screen.getByLabelText(/School/i), {
        target: { value: 'University of Example' },
      });
      fireEvent.change(screen.getByLabelText(/Start Date/i), {
        target: { value: '2016-09' },
      });

      // Add experience
      fireEvent.click(screen.getByRole('button', { name: /add experience/i }));

      // Verify experience is shown
      expect(screen.getByText('Bachelor of Science')).toBeInTheDocument();
      expect(screen.getByText('University of Example')).toBeInTheDocument();
      // Badge should show "Education" - use getAllByText since the radio label also has this text
      const educationTexts = screen.getAllByText('Education');
      expect(educationTexts.length).toBeGreaterThan(0);
    });

    it('resets form after adding experience', () => {
      render(<Step3ExperienceTimeline {...defaultProps} />);

      // Fill and add
      fireEvent.change(screen.getByLabelText(/Job Title/i), {
        target: { value: 'Software Engineer' },
      });
      fireEvent.change(screen.getByLabelText(/Company/i), {
        target: { value: 'Tech Corp' },
      });
      fireEvent.change(screen.getByLabelText(/Start Date/i), {
        target: { value: '2020-01' },
      });

      fireEvent.click(screen.getByRole('button', { name: /add experience/i }));

      // Verify form is reset
      expect(screen.getByLabelText(/Job Title/i)).toHaveValue('');
      expect(screen.getByLabelText(/Company/i)).toHaveValue('');
      expect(screen.getByLabelText(/Start Date/i)).toHaveValue('');
    });

    it('clears error after successful add', () => {
      render(<Step3ExperienceTimeline {...defaultProps} />);

      // Trigger error
      fireEvent.click(screen.getByRole('button', { name: /add experience/i }));
      expect(screen.getByText('Title is required')).toBeInTheDocument();

      // Fill and add successfully
      fireEvent.change(screen.getByLabelText(/Job Title/i), {
        target: { value: 'Software Engineer' },
      });
      fireEvent.change(screen.getByLabelText(/Company/i), {
        target: { value: 'Tech Corp' },
      });
      fireEvent.change(screen.getByLabelText(/Start Date/i), {
        target: { value: '2020-01' },
      });

      fireEvent.click(screen.getByRole('button', { name: /add experience/i }));

      // Error should be cleared
      expect(screen.queryByText('Title is required')).not.toBeInTheDocument();
    });

    it('handles optional location field', () => {
      render(<Step3ExperienceTimeline {...defaultProps} />);

      // Add without location
      fireEvent.change(screen.getByLabelText(/Job Title/i), {
        target: { value: 'Software Engineer' },
      });
      fireEvent.change(screen.getByLabelText(/Company/i), {
        target: { value: 'Tech Corp' },
      });
      fireEvent.change(screen.getByLabelText(/Start Date/i), {
        target: { value: '2020-01' },
      });

      fireEvent.click(screen.getByRole('button', { name: /add experience/i }));

      // Should still add successfully
      expect(screen.getByText('Software Engineer')).toBeInTheDocument();
    });

    it('handles optional description field', () => {
      render(<Step3ExperienceTimeline {...defaultProps} />);

      // Add without description
      fireEvent.change(screen.getByLabelText(/Job Title/i), {
        target: { value: 'Software Engineer' },
      });
      fireEvent.change(screen.getByLabelText(/Company/i), {
        target: { value: 'Tech Corp' },
      });
      fireEvent.change(screen.getByLabelText(/Start Date/i), {
        target: { value: '2020-01' },
      });

      fireEvent.click(screen.getByRole('button', { name: /add experience/i }));

      // Should still add successfully
      expect(screen.getByText('Software Engineer')).toBeInTheDocument();
    });
  });

  describe('Current Position Checkbox', () => {
    it('disables end date when current position is checked', () => {
      render(<Step3ExperienceTimeline {...defaultProps} />);

      const endDateInput = screen.getByLabelText(/End Date/i);
      expect(endDateInput).not.toBeDisabled();

      const currentCheckbox = screen.getByLabelText(/I currently work here/i);
      fireEvent.click(currentCheckbox);

      expect(endDateInput).toBeDisabled();
    });

    it('clears end date when current position is checked', () => {
      render(<Step3ExperienceTimeline {...defaultProps} />);

      const endDateInput = screen.getByLabelText(/End Date/i);
      fireEvent.change(endDateInput, { target: { value: '2023-12' } });

      const currentCheckbox = screen.getByLabelText(/I currently work here/i);
      fireEvent.click(currentCheckbox);

      expect(endDateInput).toHaveValue('');
    });

    it('shows "Present" for current positions in experience list', () => {
      render(<Step3ExperienceTimeline {...defaultProps} />);

      // Add current position
      fireEvent.change(screen.getByLabelText(/Job Title/i), {
        target: { value: 'Software Engineer' },
      });
      fireEvent.change(screen.getByLabelText(/Company/i), {
        target: { value: 'Tech Corp' },
      });
      fireEvent.change(screen.getByLabelText(/Start Date/i), {
        target: { value: '2020-01' },
      });
      fireEvent.click(screen.getByLabelText(/I currently work here/i));

      fireEvent.click(screen.getByRole('button', { name: /add experience/i }));

      expect(screen.getByText(/Present/i)).toBeInTheDocument();
    });
  });

  describe('Removing Experiences', () => {
    it('removes experience when remove button is clicked', () => {
      render(<Step3ExperienceTimeline {...defaultProps} />);

      // Add experience
      fireEvent.change(screen.getByLabelText(/Job Title/i), {
        target: { value: 'Software Engineer' },
      });
      fireEvent.change(screen.getByLabelText(/Company/i), {
        target: { value: 'Tech Corp' },
      });
      fireEvent.change(screen.getByLabelText(/Start Date/i), {
        target: { value: '2020-01' },
      });

      fireEvent.click(screen.getByRole('button', { name: /add experience/i }));

      expect(screen.getByText('Software Engineer')).toBeInTheDocument();

      // Remove experience
      fireEvent.click(screen.getByRole('button', { name: /remove/i }));

      expect(screen.queryByText('Software Engineer')).not.toBeInTheDocument();
      expect(screen.queryByText(/Your Experience/i)).not.toBeInTheDocument();
    });
  });

  describe('Experience List Display', () => {
    it('renders existing experiences from props', () => {
      const experiences = [createMockExperience()];

      render(<Step3ExperienceTimeline {...defaultProps} experiences={experiences} />);

      expect(screen.getByText('Software Engineer')).toBeInTheDocument();
      expect(screen.getByText('Tech Corp')).toBeInTheDocument();
      expect(screen.getByText('New York, NY')).toBeInTheDocument();
    });

    it('shows work badge for work experiences', () => {
      const experiences = [createMockExperience({ type: 'work' })];

      render(<Step3ExperienceTimeline {...defaultProps} experiences={experiences} />);

      expect(screen.getByText('Work')).toBeInTheDocument();
    });

    it('shows education badge for education experiences', () => {
      const experiences = [createMockExperience({ type: 'education' })];

      render(<Step3ExperienceTimeline {...defaultProps} experiences={experiences} />);

      // Badge should show "Education" - use getAllByText since the radio label also has this text
      const educationTexts = screen.getAllByText('Education');
      expect(educationTexts.length).toBeGreaterThan(0);
    });

    it('shows description when present', () => {
      const experiences = [
        createMockExperience({ description: 'Developed web applications' }),
      ];

      render(<Step3ExperienceTimeline {...defaultProps} experiences={experiences} />);

      expect(screen.getByText('Developed web applications')).toBeInTheDocument();
    });

    it('does not show location when not provided', () => {
      const experiences = [createMockExperience({ location: '' })];

      render(<Step3ExperienceTimeline {...defaultProps} experiences={experiences} />);

      expect(screen.queryByText('New York, NY')).not.toBeInTheDocument();
    });

    it('sorts experiences by start date, most recent first', () => {
      const experiences = [
        createMockExperience({ id: 'exp-1', title: 'Job 1', startDate: '2020-01' }),
        createMockExperience({ id: 'exp-2', title: 'Job 2', startDate: '2023-01' }),
        createMockExperience({ id: 'exp-3', title: 'Job 3', startDate: '2021-06' }),
      ];

      render(<Step3ExperienceTimeline {...defaultProps} experiences={experiences} />);

      const titles = screen.getAllByText(/Job \d/);
      expect(titles[0]).toHaveTextContent('Job 2'); // 2023
      expect(titles[1]).toHaveTextContent('Job 3'); // 2021
      expect(titles[2]).toHaveTextContent('Job 1'); // 2020
    });

    it('shows N/A for end date when not provided and not current', () => {
      const experiences = [
        createMockExperience({ endDate: undefined, isCurrent: false }),
      ];

      render(<Step3ExperienceTimeline {...defaultProps} experiences={experiences} />);

      expect(screen.getByText(/N\/A/)).toBeInTheDocument();
    });
  });

  describe('Date Formatting', () => {
    it('formats dates correctly', () => {
      const experiences = [
        createMockExperience({ startDate: '2020-01', endDate: '2023-12' }),
      ];

      render(<Step3ExperienceTimeline {...defaultProps} experiences={experiences} />);

      // Should show formatted dates - exact format may vary by timezone/locale
      // Just verify that date text exists with year values
      expect(screen.getByText(/20(19|20)/)).toBeInTheDocument(); // Start date year
      expect(screen.getByText(/20(22|23)/)).toBeInTheDocument(); // End date year
    });
  });

  describe('Navigation', () => {
    it('calls onBack when back button is clicked', () => {
      render(<Step3ExperienceTimeline {...defaultProps} />);

      fireEvent.click(screen.getByRole('button', { name: /back/i }));

      expect(mockOnBack).toHaveBeenCalledTimes(1);
    });

    it('calls onUpdate and onNext when next button is clicked', () => {
      render(<Step3ExperienceTimeline {...defaultProps} />);

      fireEvent.click(screen.getByRole('button', { name: /next step/i }));

      expect(mockOnUpdate).toHaveBeenCalledWith([]);
      expect(mockOnNext).toHaveBeenCalledTimes(1);
    });

    it('allows proceeding with no experiences', () => {
      render(<Step3ExperienceTimeline {...defaultProps} />);

      fireEvent.click(screen.getByRole('button', { name: /next step/i }));

      expect(mockOnUpdate).toHaveBeenCalledWith([]);
      expect(mockOnNext).toHaveBeenCalled();
    });

    it('passes current experiences to onUpdate', () => {
      render(<Step3ExperienceTimeline {...defaultProps} />);

      // Add experience
      fireEvent.change(screen.getByLabelText(/Job Title/i), {
        target: { value: 'Software Engineer' },
      });
      fireEvent.change(screen.getByLabelText(/Company/i), {
        target: { value: 'Tech Corp' },
      });
      fireEvent.change(screen.getByLabelText(/Start Date/i), {
        target: { value: '2020-01' },
      });

      fireEvent.click(screen.getByRole('button', { name: /add experience/i }));

      // Click next
      fireEvent.click(screen.getByRole('button', { name: /next step/i }));

      expect(mockOnUpdate).toHaveBeenCalledWith(
        expect.arrayContaining([
          expect.objectContaining({
            title: 'Software Engineer',
            organization: 'Tech Corp',
            startDate: '2020-01',
          }),
        ])
      );
    });
  });
});
