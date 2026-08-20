import React from 'react'
import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { Pagination, PaginationInfo } from '../pagination'

describe('Pagination Component', () => {
  const mockOnPageChange = jest.fn()

  beforeEach(() => {
    mockOnPageChange.mockClear()
  })

  describe('Basic Rendering', () => {
    it('should render pagination with page numbers', () => {
      render(
        <Pagination
          currentPage={1}
          totalPages={5}
          onPageChange={mockOnPageChange}
        />
      )

      expect(screen.getByRole('navigation', { name: 'Pagination' })).toBeInTheDocument()
      expect(screen.getByLabelText('Go to page 1')).toBeInTheDocument()
      expect(screen.getByLabelText('Go to page 2')).toBeInTheDocument()
    })

    it('should apply custom className', () => {
      const { container } = render(
        <Pagination
          currentPage={1}
          totalPages={5}
          onPageChange={mockOnPageChange}
          className="custom-pagination"
        />
      )

      expect(container.querySelector('.custom-pagination')).toBeInTheDocument()
    })

    it('should highlight current page', () => {
      render(
        <Pagination
          currentPage={3}
          totalPages={5}
          onPageChange={mockOnPageChange}
        />
      )

      const currentPageButton = screen.getByLabelText('Go to page 3')
      expect(currentPageButton).toHaveAttribute('aria-current', 'page')
    })

    it('should not set aria-current on non-current pages', () => {
      render(
        <Pagination
          currentPage={3}
          totalPages={5}
          onPageChange={mockOnPageChange}
        />
      )

      const page1Button = screen.getByLabelText('Go to page 1')
      expect(page1Button).not.toHaveAttribute('aria-current')
    })

    it('should render navigation with proper ARIA label', () => {
      render(
        <Pagination
          currentPage={1}
          totalPages={5}
          onPageChange={mockOnPageChange}
        />
      )

      expect(screen.getByRole('navigation')).toHaveAttribute('aria-label', 'Pagination')
    })
  })

  describe('Navigation Buttons', () => {
    describe('First and Last Buttons', () => {
      it('should show first and last buttons by default', () => {
        render(
          <Pagination
            currentPage={5}
            totalPages={10}
            onPageChange={mockOnPageChange}
          />
        )

        expect(screen.getByLabelText('Go to first page')).toBeInTheDocument()
        expect(screen.getByLabelText('Go to last page')).toBeInTheDocument()
      })

      it('should hide first and last buttons when showFirstLast is false', () => {
        render(
          <Pagination
            currentPage={5}
            totalPages={10}
            onPageChange={mockOnPageChange}
            showFirstLast={false}
          />
        )

        expect(screen.queryByLabelText('Go to first page')).not.toBeInTheDocument()
        expect(screen.queryByLabelText('Go to last page')).not.toBeInTheDocument()
      })

      it('should call onPageChange with 1 when first button is clicked', async () => {
        const user = userEvent.setup()
        render(
          <Pagination
            currentPage={5}
            totalPages={10}
            onPageChange={mockOnPageChange}
          />
        )

        await user.click(screen.getByLabelText('Go to first page'))
        expect(mockOnPageChange).toHaveBeenCalledWith(1)
      })

      it('should call onPageChange with totalPages when last button is clicked', async () => {
        const user = userEvent.setup()
        render(
          <Pagination
            currentPage={5}
            totalPages={10}
            onPageChange={mockOnPageChange}
          />
        )

        await user.click(screen.getByLabelText('Go to last page'))
        expect(mockOnPageChange).toHaveBeenCalledWith(10)
      })

      it('should disable first button on first page', () => {
        render(
          <Pagination
            currentPage={1}
            totalPages={10}
            onPageChange={mockOnPageChange}
          />
        )

        expect(screen.getByLabelText('Go to first page')).toBeDisabled()
      })

      it('should disable last button on last page', () => {
        render(
          <Pagination
            currentPage={10}
            totalPages={10}
            onPageChange={mockOnPageChange}
          />
        )

        expect(screen.getByLabelText('Go to last page')).toBeDisabled()
      })
    })

    describe('Previous and Next Buttons', () => {
      it('should show previous and next buttons by default', () => {
        render(
          <Pagination
            currentPage={5}
            totalPages={10}
            onPageChange={mockOnPageChange}
          />
        )

        expect(screen.getByLabelText('Go to previous page')).toBeInTheDocument()
        expect(screen.getByLabelText('Go to next page')).toBeInTheDocument()
      })

      it('should hide previous and next buttons when showPreviousNext is false', () => {
        render(
          <Pagination
            currentPage={5}
            totalPages={10}
            onPageChange={mockOnPageChange}
            showPreviousNext={false}
          />
        )

        expect(screen.queryByLabelText('Go to previous page')).not.toBeInTheDocument()
        expect(screen.queryByLabelText('Go to next page')).not.toBeInTheDocument()
      })

      it('should call onPageChange with currentPage-1 when previous is clicked', async () => {
        const user = userEvent.setup()
        render(
          <Pagination
            currentPage={5}
            totalPages={10}
            onPageChange={mockOnPageChange}
          />
        )

        await user.click(screen.getByLabelText('Go to previous page'))
        expect(mockOnPageChange).toHaveBeenCalledWith(4)
      })

      it('should call onPageChange with currentPage+1 when next is clicked', async () => {
        const user = userEvent.setup()
        render(
          <Pagination
            currentPage={5}
            totalPages={10}
            onPageChange={mockOnPageChange}
          />
        )

        await user.click(screen.getByLabelText('Go to next page'))
        expect(mockOnPageChange).toHaveBeenCalledWith(6)
      })

      it('should disable previous button on first page', () => {
        render(
          <Pagination
            currentPage={1}
            totalPages={10}
            onPageChange={mockOnPageChange}
          />
        )

        expect(screen.getByLabelText('Go to previous page')).toBeDisabled()
      })

      it('should disable next button on last page', () => {
        render(
          <Pagination
            currentPage={10}
            totalPages={10}
            onPageChange={mockOnPageChange}
          />
        )

        expect(screen.getByLabelText('Go to next page')).toBeDisabled()
      })

      it('should not call onPageChange when previous is clicked on page 1', async () => {
        const user = userEvent.setup()
        render(
          <Pagination
            currentPage={1}
            totalPages={10}
            onPageChange={mockOnPageChange}
          />
        )

        // Button is disabled, but test the onClick logic
        await user.click(screen.getByLabelText('Go to previous page'))
        expect(mockOnPageChange).not.toHaveBeenCalled()
      })

      it('should not call onPageChange when next is clicked on last page', async () => {
        const user = userEvent.setup()
        render(
          <Pagination
            currentPage={10}
            totalPages={10}
            onPageChange={mockOnPageChange}
          />
        )

        await user.click(screen.getByLabelText('Go to next page'))
        expect(mockOnPageChange).not.toHaveBeenCalled()
      })
    })
  })

  describe('Page Number Buttons', () => {
    it('should call onPageChange with page number when clicked', async () => {
      const user = userEvent.setup()
      render(
        <Pagination
          currentPage={1}
          totalPages={5}
          onPageChange={mockOnPageChange}
        />
      )

      await user.click(screen.getByLabelText('Go to page 3'))
      expect(mockOnPageChange).toHaveBeenCalledWith(3)
    })

    it('should render all page numbers when total is small', () => {
      render(
        <Pagination
          currentPage={1}
          totalPages={5}
          onPageChange={mockOnPageChange}
        />
      )

      expect(screen.getByLabelText('Go to page 1')).toBeInTheDocument()
      expect(screen.getByLabelText('Go to page 2')).toBeInTheDocument()
      expect(screen.getByLabelText('Go to page 3')).toBeInTheDocument()
      expect(screen.getByLabelText('Go to page 4')).toBeInTheDocument()
      expect(screen.getByLabelText('Go to page 5')).toBeInTheDocument()
    })

    it('should display page numbers correctly', () => {
      render(
        <Pagination
          currentPage={2}
          totalPages={5}
          onPageChange={mockOnPageChange}
        />
      )

      const page2Button = screen.getByLabelText('Go to page 2')
      expect(page2Button).toHaveTextContent('2')
    })
  })

  describe('Pagination Range Logic - usePagination Hook', () => {
    describe('Case 1: Total pages less than page numbers to show', () => {
      it('should show all pages when totalPages <= 7 (siblingCount=1)', () => {
        render(
          <Pagination
            currentPage={1}
            totalPages={5}
            onPageChange={mockOnPageChange}
            siblingCount={1}
          />
        )

        expect(screen.getByLabelText('Go to page 1')).toBeInTheDocument()
        expect(screen.getByLabelText('Go to page 5')).toBeInTheDocument()
        expect(screen.queryByRole('img', { hidden: true })).not.toBeInTheDocument() // No ellipsis
      })

      it('should show all pages for 6 pages', () => {
        render(
          <Pagination
            currentPage={3}
            totalPages={6}
            onPageChange={mockOnPageChange}
          />
        )

        // For 6 pages with siblingCount=1, totalPageNumbers=6, so all pages should show
        for (let i = 1; i <= 6; i++) {
          expect(screen.getByLabelText(`Go to page ${i}`)).toBeInTheDocument()
        }
      })

      it('should show ellipsis for 7 pages with siblingCount=1', () => {
        render(
          <Pagination
            currentPage={4}
            totalPages={7}
            onPageChange={mockOnPageChange}
            siblingCount={1}
          />
        )

        // With totalPages=7, siblingCount=1: totalPageNumbers=6 < 7
        // So it will show: 1, ..., 3, 4, 5, 6, 7 (left dots, no right dots)
        expect(screen.getByLabelText('Go to page 1')).toBeInTheDocument()
        expect(screen.queryByLabelText('Go to page 2')).not.toBeInTheDocument() // Replaced by dots
        expect(screen.getByLabelText('Go to page 3')).toBeInTheDocument()
        expect(screen.getByLabelText('Go to page 7')).toBeInTheDocument()
      })
    })

    describe('Case 2: No left dots, but right dots', () => {
      it('should show right ellipsis when on early pages', () => {
        render(
          <Pagination
            currentPage={3}
            totalPages={20}
            onPageChange={mockOnPageChange}
            siblingCount={1}
          />
        )

        // Should show pages 1-5, ..., 20
        expect(screen.getByLabelText('Go to page 1')).toBeInTheDocument()
        expect(screen.getByLabelText('Go to page 2')).toBeInTheDocument()
        expect(screen.getByLabelText('Go to page 3')).toBeInTheDocument()
        expect(screen.getByLabelText('Go to page 4')).toBeInTheDocument()
        expect(screen.getByLabelText('Go to page 5')).toBeInTheDocument()
        expect(screen.getByLabelText('Go to page 20')).toBeInTheDocument()

        // Middle pages should NOT be rendered (ellipsis instead)
        expect(screen.queryByLabelText('Go to page 10')).not.toBeInTheDocument()
        expect(screen.queryByLabelText('Go to page 15')).not.toBeInTheDocument()
      })

      it('should show correct pages on page 1 of 20', () => {
        render(
          <Pagination
            currentPage={1}
            totalPages={20}
            onPageChange={mockOnPageChange}
          />
        )

        expect(screen.getByLabelText('Go to page 1')).toBeInTheDocument()
        expect(screen.getByLabelText('Go to page 5')).toBeInTheDocument()
        expect(screen.getByLabelText('Go to page 20')).toBeInTheDocument()
      })
    })

    describe('Case 3: Left dots, but no right dots', () => {
      it('should show left ellipsis when on late pages', () => {
        render(
          <Pagination
            currentPage={18}
            totalPages={20}
            onPageChange={mockOnPageChange}
            siblingCount={1}
          />
        )

        // Should show 1, ..., 16-20
        expect(screen.getByLabelText('Go to page 1')).toBeInTheDocument()
        expect(screen.getByLabelText('Go to page 16')).toBeInTheDocument()
        expect(screen.getByLabelText('Go to page 17')).toBeInTheDocument()
        expect(screen.getByLabelText('Go to page 18')).toBeInTheDocument()
        expect(screen.getByLabelText('Go to page 19')).toBeInTheDocument()
        expect(screen.getByLabelText('Go to page 20')).toBeInTheDocument()

        // Early pages should NOT be rendered (ellipsis instead)
        expect(screen.queryByLabelText('Go to page 5')).not.toBeInTheDocument()
        expect(screen.queryByLabelText('Go to page 10')).not.toBeInTheDocument()
      })

      it('should show correct pages on page 20 of 20', () => {
        render(
          <Pagination
            currentPage={20}
            totalPages={20}
            onPageChange={mockOnPageChange}
          />
        )

        expect(screen.getByLabelText('Go to page 1')).toBeInTheDocument()
        expect(screen.getByLabelText('Go to page 16')).toBeInTheDocument()
        expect(screen.getByLabelText('Go to page 20')).toBeInTheDocument()
      })
    })

    describe('Case 4: Both left and right dots', () => {
      it('should show both ellipses when in middle pages', () => {
        render(
          <Pagination
            currentPage={10}
            totalPages={20}
            onPageChange={mockOnPageChange}
            siblingCount={1}
          />
        )

        // Should show 1, ..., 9-11, ..., 20
        expect(screen.getByLabelText('Go to page 1')).toBeInTheDocument()
        expect(screen.getByLabelText('Go to page 9')).toBeInTheDocument()
        expect(screen.getByLabelText('Go to page 10')).toBeInTheDocument()
        expect(screen.getByLabelText('Go to page 11')).toBeInTheDocument()
        expect(screen.getByLabelText('Go to page 20')).toBeInTheDocument()

        // Pages in ellipsis range should NOT be rendered
        expect(screen.queryByLabelText('Go to page 2')).not.toBeInTheDocument()
        expect(screen.queryByLabelText('Go to page 5')).not.toBeInTheDocument()
        expect(screen.queryByLabelText('Go to page 15')).not.toBeInTheDocument()
        expect(screen.queryByLabelText('Go to page 19')).not.toBeInTheDocument()
      })

      it('should show correct middle range with siblingCount=2', () => {
        render(
          <Pagination
            currentPage={10}
            totalPages={20}
            onPageChange={mockOnPageChange}
            siblingCount={2}
          />
        )

        // With siblingCount=2, should show 8-12
        expect(screen.getByLabelText('Go to page 8')).toBeInTheDocument()
        expect(screen.getByLabelText('Go to page 9')).toBeInTheDocument()
        expect(screen.getByLabelText('Go to page 10')).toBeInTheDocument()
        expect(screen.getByLabelText('Go to page 11')).toBeInTheDocument()
        expect(screen.getByLabelText('Go to page 12')).toBeInTheDocument()
      })
    })
  })

  describe('Sibling Count Configuration', () => {
    it('should use siblingCount=1 by default', () => {
      render(
        <Pagination
          currentPage={10}
          totalPages={20}
          onPageChange={mockOnPageChange}
        />
      )

      // With siblingCount=1, should show 9-11
      expect(screen.getByLabelText('Go to page 9')).toBeInTheDocument()
      expect(screen.getByLabelText('Go to page 10')).toBeInTheDocument()
      expect(screen.getByLabelText('Go to page 11')).toBeInTheDocument()
    })

    it('should respect custom siblingCount=0', () => {
      render(
        <Pagination
          currentPage={10}
          totalPages={20}
          onPageChange={mockOnPageChange}
          siblingCount={0}
        />
      )

      // With siblingCount=0, should only show current page
      expect(screen.getByLabelText('Go to page 10')).toBeInTheDocument()
      expect(screen.queryByLabelText('Go to page 9')).not.toBeInTheDocument()
      expect(screen.queryByLabelText('Go to page 11')).not.toBeInTheDocument()
    })

    it('should respect custom siblingCount=3', () => {
      render(
        <Pagination
          currentPage={10}
          totalPages={30}
          onPageChange={mockOnPageChange}
          siblingCount={3}
        />
      )

      // With siblingCount=3, should show 7-13
      expect(screen.getByLabelText('Go to page 7')).toBeInTheDocument()
      expect(screen.getByLabelText('Go to page 13')).toBeInTheDocument()
    })
  })

  describe('Edge Cases', () => {
    it('should return null when currentPage is 0', () => {
      const { container } = render(
        <Pagination
          currentPage={0}
          totalPages={10}
          onPageChange={mockOnPageChange}
        />
      )

      expect(container.firstChild).toBeNull()
    })

    it('should return null when totalPages is 0', () => {
      const { container } = render(
        <Pagination
          currentPage={1}
          totalPages={0}
          onPageChange={mockOnPageChange}
        />
      )

      expect(container.firstChild).toBeNull()
    })

    it('should return null when pagination range is less than 2', () => {
      const { container } = render(
        <Pagination
          currentPage={1}
          totalPages={1}
          onPageChange={mockOnPageChange}
        />
      )

      expect(container.firstChild).toBeNull()
    })

    it('should handle 2 total pages correctly', () => {
      render(
        <Pagination
          currentPage={1}
          totalPages={2}
          onPageChange={mockOnPageChange}
        />
      )

      expect(screen.getByLabelText('Go to page 1')).toBeInTheDocument()
      expect(screen.getByLabelText('Go to page 2')).toBeInTheDocument()
    })

    it('should handle 100 total pages', () => {
      render(
        <Pagination
          currentPage={50}
          totalPages={100}
          onPageChange={mockOnPageChange}
        />
      )

      expect(screen.getByLabelText('Go to page 1')).toBeInTheDocument()
      expect(screen.getByLabelText('Go to page 50')).toBeInTheDocument()
      expect(screen.getByLabelText('Go to page 100')).toBeInTheDocument()
    })

    it('should handle last page correctly', () => {
      render(
        <Pagination
          currentPage={10}
          totalPages={10}
          onPageChange={mockOnPageChange}
        />
      )

      expect(screen.getByLabelText('Go to first page')).not.toBeDisabled()
      expect(screen.getByLabelText('Go to previous page')).not.toBeDisabled()
      expect(screen.getByLabelText('Go to next page')).toBeDisabled()
      expect(screen.getByLabelText('Go to last page')).toBeDisabled()
    })

    it('should handle first page correctly', () => {
      render(
        <Pagination
          currentPage={1}
          totalPages={10}
          onPageChange={mockOnPageChange}
        />
      )

      expect(screen.getByLabelText('Go to first page')).toBeDisabled()
      expect(screen.getByLabelText('Go to previous page')).toBeDisabled()
      expect(screen.getByLabelText('Go to next page')).not.toBeDisabled()
      expect(screen.getByLabelText('Go to last page')).not.toBeDisabled()
    })
  })

  describe('Integration', () => {
    it('should work with all features combined', async () => {
      const user = userEvent.setup()
      render(
        <Pagination
          currentPage={10}
          totalPages={50}
          onPageChange={mockOnPageChange}
          showFirstLast={true}
          showPreviousNext={true}
          siblingCount={2}
          className="test-pagination"
        />
      )

      expect(screen.getByRole('navigation')).toHaveClass('test-pagination')

      await user.click(screen.getByLabelText('Go to next page'))
      expect(mockOnPageChange).toHaveBeenCalledWith(11)

      mockOnPageChange.mockClear()
      await user.click(screen.getByLabelText('Go to first page'))
      expect(mockOnPageChange).toHaveBeenCalledWith(1)
    })

    it('should handle navigation without first/last buttons', async () => {
      const user = userEvent.setup()
      render(
        <Pagination
          currentPage={5}
          totalPages={10}
          onPageChange={mockOnPageChange}
          showFirstLast={false}
        />
      )

      expect(screen.queryByLabelText('Go to first page')).not.toBeInTheDocument()
      expect(screen.queryByLabelText('Go to last page')).not.toBeInTheDocument()

      await user.click(screen.getByLabelText('Go to previous page'))
      expect(mockOnPageChange).toHaveBeenCalledWith(4)
    })

    it('should handle navigation without previous/next buttons', async () => {
      const user = userEvent.setup()
      render(
        <Pagination
          currentPage={5}
          totalPages={10}
          onPageChange={mockOnPageChange}
          showPreviousNext={false}
        />
      )

      expect(screen.queryByLabelText('Go to previous page')).not.toBeInTheDocument()
      expect(screen.queryByLabelText('Go to next page')).not.toBeInTheDocument()

      await user.click(screen.getByLabelText('Go to first page'))
      expect(mockOnPageChange).toHaveBeenCalledWith(1)
    })

    it('should handle minimal configuration', async () => {
      const user = userEvent.setup()
      render(
        <Pagination
          currentPage={3}
          totalPages={10}
          onPageChange={mockOnPageChange}
          showFirstLast={false}
          showPreviousNext={false}
        />
      )

      // Only page buttons should be present
      expect(screen.queryByLabelText('Go to first page')).not.toBeInTheDocument()
      expect(screen.queryByLabelText('Go to last page')).not.toBeInTheDocument()
      expect(screen.queryByLabelText('Go to previous page')).not.toBeInTheDocument()
      expect(screen.queryByLabelText('Go to next page')).not.toBeInTheDocument()

      await user.click(screen.getByLabelText('Go to page 5'))
      expect(mockOnPageChange).toHaveBeenCalledWith(5)
    })
  })
})

describe('PaginationInfo Component', () => {
  describe('Basic Rendering', () => {
    it('should render pagination info with correct text', () => {
      render(
        <PaginationInfo
          currentPage={1}
          totalPages={10}
          totalItems={100}
          itemsPerPage={10}
        />
      )

      expect(screen.getByText(/Showing/)).toBeInTheDocument()
      expect(screen.getByText('1')).toBeInTheDocument()
      expect(screen.getByText('10')).toBeInTheDocument()
      expect(screen.getByText('100')).toBeInTheDocument()
    })

    it('should apply custom className', () => {
      const { container } = render(
        <PaginationInfo
          currentPage={1}
          totalPages={10}
          totalItems={100}
          itemsPerPage={10}
          className="custom-info"
        />
      )

      expect(container.querySelector('.custom-info')).toBeInTheDocument()
    })

    it('should calculate correct start and end for first page', () => {
      render(
        <PaginationInfo
          currentPage={1}
          totalPages={10}
          totalItems={100}
          itemsPerPage={10}
        />
      )

      expect(screen.getByText(/Showing/)).toHaveTextContent('Showing 1 to 10 of 100 results')
    })

    it('should calculate correct start and end for middle page', () => {
      render(
        <PaginationInfo
          currentPage={5}
          totalPages={10}
          totalItems={100}
          itemsPerPage={10}
        />
      )

      expect(screen.getByText(/Showing/)).toHaveTextContent('Showing 41 to 50 of 100 results')
    })

    it('should calculate correct start and end for last page', () => {
      render(
        <PaginationInfo
          currentPage={10}
          totalPages={10}
          totalItems={100}
          itemsPerPage={10}
        />
      )

      expect(screen.getByText(/Showing/)).toHaveTextContent('Showing 91 to 100 of 100 results')
    })

    it('should handle partial last page correctly', () => {
      render(
        <PaginationInfo
          currentPage={11}
          totalPages={11}
          totalItems={105}
          itemsPerPage={10}
        />
      )

      expect(screen.getByText(/Showing/)).toHaveTextContent('Showing 101 to 105 of 105 results')
    })
  })

  describe('Empty State', () => {
    it('should show "No results found" when totalItems is 0', () => {
      render(
        <PaginationInfo
          currentPage={1}
          totalPages={0}
          totalItems={0}
          itemsPerPage={10}
        />
      )

      expect(screen.getByText('No results found')).toBeInTheDocument()
      expect(screen.queryByText(/Showing/)).not.toBeInTheDocument()
    })

    it('should apply className to empty state', () => {
      const { container } = render(
        <PaginationInfo
          currentPage={1}
          totalPages={0}
          totalItems={0}
          itemsPerPage={10}
          className="custom-empty"
        />
      )

      expect(container.querySelector('.custom-empty')).toBeInTheDocument()
    })
  })

  describe('Different Configurations', () => {
    it('should handle itemsPerPage=25', () => {
      render(
        <PaginationInfo
          currentPage={2}
          totalPages={4}
          totalItems={100}
          itemsPerPage={25}
        />
      )

      expect(screen.getByText(/Showing/)).toHaveTextContent('Showing 26 to 50 of 100 results')
    })

    it('should handle itemsPerPage=50', () => {
      render(
        <PaginationInfo
          currentPage={1}
          totalPages={2}
          totalItems={100}
          itemsPerPage={50}
        />
      )

      expect(screen.getByText(/Showing/)).toHaveTextContent('Showing 1 to 50 of 100 results')
    })

    it('should handle single item', () => {
      render(
        <PaginationInfo
          currentPage={1}
          totalPages={1}
          totalItems={1}
          itemsPerPage={10}
        />
      )

      expect(screen.getByText(/Showing/)).toHaveTextContent('Showing 1 to 1 of 1 results')
    })

    it('should handle large numbers correctly', () => {
      render(
        <PaginationInfo
          currentPage={100}
          totalPages={1000}
          totalItems={10000}
          itemsPerPage={10}
        />
      )

      expect(screen.getByText(/Showing/)).toHaveTextContent('Showing 991 to 1000 of 10000 results')
    })
  })

  describe('Font Styling', () => {
    it('should apply font-medium to numbers', () => {
      render(
        <PaginationInfo
          currentPage={1}
          totalPages={10}
          totalItems={100}
          itemsPerPage={10}
        />
      )

      const startNumber = screen.getByText('1')
      const endNumber = screen.getByText('10')
      const totalNumber = screen.getByText('100')

      expect(startNumber).toHaveClass('font-medium')
      expect(endNumber).toHaveClass('font-medium')
      expect(totalNumber).toHaveClass('font-medium')
    })
  })
})
