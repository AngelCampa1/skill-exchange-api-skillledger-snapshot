/**
 * FeedbackButton.tsx Tests
 *
 * Tests for floating feedback button and modal dialog.
 * Coverage Target: 80%+
 */

import React from 'react';
import { render, screen, fireEvent, waitFor, act } from '@testing-library/react';
import '@testing-library/jest-dom';
import FeedbackButton from '../FeedbackButton';

// Mock FeedbackForm
let mockOnSuccess: (() => void) | null = null;
let mockOnError: ((error: string) => void) | null = null;

jest.mock('../FeedbackForm', () => ({
  __esModule: true,
  default: ({ onSuccess, onError }: { onSuccess: () => void; onError: (error: string) => void }) => {
    mockOnSuccess = onSuccess;
    mockOnError = onError;
    return (
      <div data-testid="feedback-form">
        <button data-testid="trigger-success" onClick={onSuccess}>
          Trigger Success
        </button>
        <button data-testid="trigger-error" onClick={() => onError('Test error message')}>
          Trigger Error
        </button>
      </div>
    );
  },
}));

describe('FeedbackButton', () => {
  beforeEach(() => {
    jest.clearAllMocks();
    jest.useFakeTimers();
    mockOnSuccess = null;
    mockOnError = null;
  });

  afterEach(() => {
    jest.useRealTimers();
    document.body.style.overflow = '';
  });

  describe('Floating Button', () => {
    it('renders floating feedback button', () => {
      render(<FeedbackButton />);

      const button = screen.getByRole('button', { name: /send feedback/i });
      expect(button).toBeInTheDocument();
    });

    it('has correct aria-label for accessibility', () => {
      render(<FeedbackButton />);

      const button = screen.getByRole('button', { name: /send feedback/i });
      expect(button).toHaveAttribute('aria-label', 'Send feedback');
    });

    it('shows text label on larger screens', () => {
      render(<FeedbackButton />);

      expect(screen.getByText('Feedback')).toBeInTheDocument();
    });
  });

  describe('Modal Open/Close', () => {
    it('opens modal when button is clicked', () => {
      render(<FeedbackButton />);

      // Modal should not be visible initially
      expect(screen.queryByRole('dialog')).not.toBeInTheDocument();

      // Click the feedback button
      fireEvent.click(screen.getByRole('button', { name: /send feedback/i }));

      // Modal should be visible
      expect(screen.getByRole('dialog')).toBeInTheDocument();
      expect(screen.getByText('Send Feedback')).toBeInTheDocument();
    });

    it('closes modal when close button is clicked', () => {
      render(<FeedbackButton />);

      // Open modal
      fireEvent.click(screen.getByRole('button', { name: /send feedback/i }));
      expect(screen.getByRole('dialog')).toBeInTheDocument();

      // Click close button
      fireEvent.click(screen.getByRole('button', { name: /close feedback form/i }));

      // Modal should be closed
      expect(screen.queryByRole('dialog')).not.toBeInTheDocument();
    });

    it('closes modal when backdrop is clicked', () => {
      render(<FeedbackButton />);

      // Open modal
      fireEvent.click(screen.getByRole('button', { name: /send feedback/i }));
      expect(screen.getByRole('dialog')).toBeInTheDocument();

      // Click backdrop (the element with aria-hidden="true")
      const backdrop = screen.getByRole('dialog').querySelector('[aria-hidden="true"]');
      fireEvent.click(backdrop!);

      // Modal should be closed
      expect(screen.queryByRole('dialog')).not.toBeInTheDocument();
    });

    it('closes modal when Escape key is pressed', () => {
      render(<FeedbackButton />);

      // Open modal
      fireEvent.click(screen.getByRole('button', { name: /send feedback/i }));
      expect(screen.getByRole('dialog')).toBeInTheDocument();

      // Press Escape key
      fireEvent.keyDown(document, { key: 'Escape' });

      // Modal should be closed
      expect(screen.queryByRole('dialog')).not.toBeInTheDocument();
    });

    it('does not close modal on non-Escape key press', () => {
      render(<FeedbackButton />);

      // Open modal
      fireEvent.click(screen.getByRole('button', { name: /send feedback/i }));
      expect(screen.getByRole('dialog')).toBeInTheDocument();

      // Press other key
      fireEvent.keyDown(document, { key: 'Enter' });

      // Modal should still be open
      expect(screen.getByRole('dialog')).toBeInTheDocument();
    });
  });

  describe('Body Scroll Lock', () => {
    it('prevents body scroll when modal is open', () => {
      render(<FeedbackButton />);

      expect(document.body.style.overflow).not.toBe('hidden');

      // Open modal
      fireEvent.click(screen.getByRole('button', { name: /send feedback/i }));

      expect(document.body.style.overflow).toBe('hidden');
    });

    it('restores body scroll when modal is closed', () => {
      render(<FeedbackButton />);

      // Open modal
      fireEvent.click(screen.getByRole('button', { name: /send feedback/i }));
      expect(document.body.style.overflow).toBe('hidden');

      // Close modal
      fireEvent.click(screen.getByRole('button', { name: /close feedback form/i }));

      expect(document.body.style.overflow).toBe('unset');
    });

    it('restores body scroll on unmount', () => {
      const { unmount } = render(<FeedbackButton />);

      // Open modal
      fireEvent.click(screen.getByRole('button', { name: /send feedback/i }));
      expect(document.body.style.overflow).toBe('hidden');

      // Unmount component
      unmount();

      expect(document.body.style.overflow).toBe('unset');
    });
  });

  describe('Modal Content', () => {
    it('renders FeedbackForm when modal is open', () => {
      render(<FeedbackButton />);

      // Open modal
      fireEvent.click(screen.getByRole('button', { name: /send feedback/i }));

      expect(screen.getByTestId('feedback-form')).toBeInTheDocument();
    });

    it('shows introductory text', () => {
      render(<FeedbackButton />);

      // Open modal
      fireEvent.click(screen.getByRole('button', { name: /send feedback/i }));

      expect(screen.getByText(/we appreciate your feedback/i)).toBeInTheDocument();
    });

    it('has proper accessibility attributes', () => {
      render(<FeedbackButton />);

      // Open modal
      fireEvent.click(screen.getByRole('button', { name: /send feedback/i }));

      const dialog = screen.getByRole('dialog');
      expect(dialog).toHaveAttribute('aria-modal', 'true');
      expect(dialog).toHaveAttribute('aria-labelledby', 'feedback-modal-title');
    });
  });

  describe('Success State', () => {
    it('shows success message when feedback is submitted successfully', async () => {
      render(<FeedbackButton />);

      // Open modal
      fireEvent.click(screen.getByRole('button', { name: /send feedback/i }));

      // Trigger success
      fireEvent.click(screen.getByTestId('trigger-success'));

      await waitFor(() => {
        expect(screen.getByText('Thank you!')).toBeInTheDocument();
        expect(screen.getByText(/your feedback has been submitted successfully/i)).toBeInTheDocument();
      });
    });

    it('hides form and shows success message', async () => {
      render(<FeedbackButton />);

      // Open modal
      fireEvent.click(screen.getByRole('button', { name: /send feedback/i }));

      // Trigger success
      fireEvent.click(screen.getByTestId('trigger-success'));

      await waitFor(() => {
        expect(screen.queryByTestId('feedback-form')).not.toBeInTheDocument();
        expect(screen.getByText('Thank you!')).toBeInTheDocument();
      });
    });

    it('auto-closes modal after showing success message', async () => {
      render(<FeedbackButton />);

      // Open modal
      fireEvent.click(screen.getByRole('button', { name: /send feedback/i }));

      // Trigger success
      fireEvent.click(screen.getByTestId('trigger-success'));

      await waitFor(() => {
        expect(screen.getByText('Thank you!')).toBeInTheDocument();
      });

      // Fast-forward time by 2 seconds
      act(() => {
        jest.advanceTimersByTime(2000);
      });

      // Modal should be closed
      expect(screen.queryByRole('dialog')).not.toBeInTheDocument();
    });

    it('clears error state when success occurs', async () => {
      render(<FeedbackButton />);

      // Open modal
      fireEvent.click(screen.getByRole('button', { name: /send feedback/i }));

      // Trigger error first
      fireEvent.click(screen.getByTestId('trigger-error'));

      await waitFor(() => {
        expect(screen.getByText('Test error message')).toBeInTheDocument();
      });

      // Now trigger success
      fireEvent.click(screen.getByTestId('trigger-success'));

      await waitFor(() => {
        expect(screen.queryByText('Test error message')).not.toBeInTheDocument();
        expect(screen.getByText('Thank you!')).toBeInTheDocument();
      });
    });
  });

  describe('Error State', () => {
    it('shows error message when feedback submission fails', async () => {
      render(<FeedbackButton />);

      // Open modal
      fireEvent.click(screen.getByRole('button', { name: /send feedback/i }));

      // Trigger error
      fireEvent.click(screen.getByTestId('trigger-error'));

      await waitFor(() => {
        expect(screen.getByText('Test error message')).toBeInTheDocument();
      });
    });

    it('keeps form visible when error occurs', async () => {
      render(<FeedbackButton />);

      // Open modal
      fireEvent.click(screen.getByRole('button', { name: /send feedback/i }));

      // Trigger error
      fireEvent.click(screen.getByTestId('trigger-error'));

      await waitFor(() => {
        expect(screen.getByText('Test error message')).toBeInTheDocument();
        expect(screen.getByTestId('feedback-form')).toBeInTheDocument();
      });
    });

    it('clears success state when error occurs', async () => {
      render(<FeedbackButton />);

      // Open modal
      fireEvent.click(screen.getByRole('button', { name: /send feedback/i }));

      // Trigger success first
      fireEvent.click(screen.getByTestId('trigger-success'));

      await waitFor(() => {
        expect(screen.getByText('Thank you!')).toBeInTheDocument();
      });

      // Re-open modal (it should auto-close but let's close and reopen)
      act(() => {
        jest.advanceTimersByTime(2000);
      });

      // Reopen
      fireEvent.click(screen.getByRole('button', { name: /send feedback/i }));

      // Now trigger error
      fireEvent.click(screen.getByTestId('trigger-error'));

      await waitFor(() => {
        expect(screen.getByText('Test error message')).toBeInTheDocument();
        expect(screen.queryByText('Thank you!')).not.toBeInTheDocument();
      });
    });
  });

  describe('Reset on Close', () => {
    it('resets success and error state when modal is closed', async () => {
      render(<FeedbackButton />);

      // Open modal and trigger error
      fireEvent.click(screen.getByRole('button', { name: /send feedback/i }));
      fireEvent.click(screen.getByTestId('trigger-error'));

      await waitFor(() => {
        expect(screen.getByText('Test error message')).toBeInTheDocument();
      });

      // Close modal
      fireEvent.click(screen.getByRole('button', { name: /close feedback form/i }));

      // Reopen modal - error should not be visible
      fireEvent.click(screen.getByRole('button', { name: /send feedback/i }));

      expect(screen.queryByText('Test error message')).not.toBeInTheDocument();
      expect(screen.getByTestId('feedback-form')).toBeInTheDocument();
    });
  });
});
