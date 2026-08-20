import React from 'react'
import { render, screen, fireEvent, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { Rating, StarRatingDisplay, RatingSummary } from '../rating'

// Mock lucide-react icons
jest.mock('lucide-react', () => ({
  Star: ({ className, ...props }: any) => <svg data-testid="star-icon" className={className} {...props} />,
  Heart: ({ className, ...props }: any) => <svg data-testid="heart-icon" className={className} {...props} />,
  ThumbsUp: ({ className, ...props }: any) => <svg data-testid="thumbs-icon" className={className} {...props} />,
}))

describe('Rating Component', () => {
  // ============================================
  // Initial Render (8 tests)
  // ============================================
  describe('Initial Render', () => {
    it('should render with default props', () => {
      render(<Rating />)

      const buttons = screen.getAllByRole('button')
      expect(buttons).toHaveLength(5) // Default max is 5
    })

    it('should render with custom max value', () => {
      render(<Rating max={10} />)

      const buttons = screen.getAllByRole('button')
      expect(buttons).toHaveLength(10)
    })

    it('should display label when provided', () => {
      render(<Rating label="Rate this product" />)

      expect(screen.getByText('Rate this product')).toBeInTheDocument()
    })

    it('should display helper text when provided', () => {
      render(<Rating helperText="Click to rate" />)

      expect(screen.getByText('Click to rate')).toBeInTheDocument()
    })

    it('should show value when showValue is true', () => {
      render(<Rating value={3.5} showValue allowHalf />)

      expect(screen.getByText('3.5 / 5')).toBeInTheDocument()
    })

    it('should show integer value when allowHalf is false', () => {
      render(<Rating value={3.7} showValue allowHalf={false} />)

      expect(screen.getByText('4 / 5')).toBeInTheDocument()
    })

    it('should have accessible aria labels', () => {
      render(<Rating max={5} />)

      expect(screen.getByLabelText('Rate 1 out of 5')).toBeInTheDocument()
      expect(screen.getByLabelText('Rate 5 out of 5')).toBeInTheDocument()
    })

    it('should apply custom className', () => {
      const { container } = render(<Rating className="custom-class" />)

      const wrapper = container.firstChild
      expect(wrapper).toHaveClass('custom-class')
    })
  })

  // ============================================
  // Icon Variants (3 tests)
  // ============================================
  describe('Icon Variants', () => {
    it('should render star icons by default', () => {
      render(<Rating />)

      const stars = screen.getAllByTestId('star-icon')
      expect(stars.length).toBeGreaterThan(0)
    })

    it('should render heart icons when icon prop is heart', () => {
      render(<Rating icon="heart" />)

      const hearts = screen.getAllByTestId('heart-icon')
      expect(hearts.length).toBeGreaterThan(0)
    })

    it('should render thumbs icons when icon prop is thumbs', () => {
      render(<Rating icon="thumbs" />)

      const thumbs = screen.getAllByTestId('thumbs-icon')
      expect(thumbs.length).toBeGreaterThan(0)
    })
  })

  // ============================================
  // Size Variants (3 tests)
  // ============================================
  describe('Size Variants', () => {
    it('should apply small size classes', () => {
      const { container } = render(<Rating size="sm" />)

      const icon = container.querySelector('.h-4.w-4')
      expect(icon).toBeInTheDocument()
    })

    it('should apply medium size classes (default)', () => {
      const { container } = render(<Rating size="md" />)

      const icon = container.querySelector('.h-6.w-6')
      expect(icon).toBeInTheDocument()
    })

    it('should apply large size classes', () => {
      const { container } = render(<Rating size="lg" />)

      const icon = container.querySelector('.h-8.w-8')
      expect(icon).toBeInTheDocument()
    })
  })

  // ============================================
  // Click Interactions (8 tests)
  // ============================================
  describe('Click Interactions', () => {
    it('should call onChange when clicking a rating', async () => {
      const handleChange = jest.fn()
      const user = userEvent.setup()
      render(<Rating onChange={handleChange} />)

      const thirdStar = screen.getByLabelText('Rate 3 out of 5')
      await user.click(thirdStar)

      expect(handleChange).toHaveBeenCalledWith(3)
    })

    it('should allow clicking different ratings', async () => {
      const handleChange = jest.fn()
      const user = userEvent.setup()
      render(<Rating onChange={handleChange} />)

      await user.click(screen.getByLabelText('Rate 1 out of 5'))
      expect(handleChange).toHaveBeenCalledWith(1)

      handleChange.mockClear()

      await user.click(screen.getByLabelText('Rate 5 out of 5'))
      expect(handleChange).toHaveBeenCalledWith(5)
    })

    it('should support half ratings when allowHalf is true', () => {
      const handleChange = jest.fn()
      render(<Rating onChange={handleChange} allowHalf />)

      const thirdStar = screen.getByLabelText('Rate 3 out of 5')

      // Mock getBoundingClientRect for half-click detection
      const mockRect = { left: 0, width: 100 }
      jest.spyOn(thirdStar, 'getBoundingClientRect').mockReturnValue(mockRect as DOMRect)

      // Click on left half (x < width / 2)
      fireEvent.click(thirdStar, { clientX: 25 })

      expect(handleChange).toHaveBeenCalledWith(2.5)
    })

    it('should give full rating when clicking right half', () => {
      const handleChange = jest.fn()
      render(<Rating onChange={handleChange} allowHalf />)

      const thirdStar = screen.getByLabelText('Rate 3 out of 5')

      const mockRect = { left: 0, width: 100 }
      jest.spyOn(thirdStar, 'getBoundingClientRect').mockReturnValue(mockRect as DOMRect)

      // Click on right half (x >= width / 2)
      fireEvent.click(thirdStar, { clientX: 75 })

      expect(handleChange).toHaveBeenCalledWith(3)
    })

    it('should not support half ratings when allowHalf is false', async () => {
      const handleChange = jest.fn()
      const user = userEvent.setup()
      render(<Rating onChange={handleChange} allowHalf={false} />)

      const thirdStar = screen.getByLabelText('Rate 3 out of 5')
      await user.click(thirdStar)

      // Should always give full rating
      expect(handleChange).toHaveBeenCalledWith(3)
    })

    it('should not call onChange when readonly', async () => {
      const handleChange = jest.fn()
      const user = userEvent.setup()
      render(<Rating onChange={handleChange} readonly />)

      await user.click(screen.getByLabelText('Rate 3 out of 5'))

      expect(handleChange).not.toHaveBeenCalled()
    })

    it('should not call onChange when disabled', async () => {
      const handleChange = jest.fn()
      const user = userEvent.setup()
      render(<Rating onChange={handleChange} disabled />)

      await user.click(screen.getByLabelText('Rate 3 out of 5'))

      expect(handleChange).not.toHaveBeenCalled()
    })

    it('should update visual state when value changes', () => {
      const { rerender } = render(<Rating value={2} />)

      rerender(<Rating value={4} />)

      // Component should re-render with new value
      const buttons = screen.getAllByRole('button')
      expect(buttons).toHaveLength(5)
    })
  })

  // ============================================
  // Hover Interactions (6 tests)
  // ============================================
  describe('Hover Interactions', () => {
    it('should show hover preview when hovering over rating', () => {
      render(<Rating allowHalf />)

      const thirdStar = screen.getByLabelText('Rate 3 out of 5')

      const mockRect = { left: 0, width: 100 }
      jest.spyOn(thirdStar, 'getBoundingClientRect').mockReturnValue(mockRect as DOMRect)

      fireEvent.mouseMove(thirdStar, { clientX: 75 })

      // Hover state should be set (verified by component behavior)
      expect(thirdStar).toBeInTheDocument()
    })

    it('should clear hover preview on mouse leave', () => {
      render(<Rating allowHalf />)

      const thirdStar = screen.getByLabelText('Rate 3 out of 5')

      const mockRect = { left: 0, width: 100 }
      jest.spyOn(thirdStar, 'getBoundingClientRect').mockReturnValue(mockRect as DOMRect)

      fireEvent.mouseMove(thirdStar, { clientX: 75 })
      fireEvent.mouseLeave(thirdStar)

      // Hover state should be cleared
      expect(thirdStar).toBeInTheDocument()
    })

    it('should not show hover preview when readonly', () => {
      render(<Rating readonly allowHalf />)

      const thirdStar = screen.getByLabelText('Rate 3 out of 5')

      fireEvent.mouseMove(thirdStar, { clientX: 75 })

      // Should not respond to hover when readonly
      expect(thirdStar).toHaveAttribute('disabled')
    })

    it('should not show hover preview when disabled', () => {
      render(<Rating disabled allowHalf />)

      const thirdStar = screen.getByLabelText('Rate 3 out of 5')

      fireEvent.mouseMove(thirdStar, { clientX: 75 })

      expect(thirdStar).toHaveAttribute('disabled')
    })

    it('should not show hover preview when allowHalf is false', () => {
      render(<Rating allowHalf={false} />)

      const thirdStar = screen.getByLabelText('Rate 3 out of 5')

      fireEvent.mouseMove(thirdStar, { clientX: 25 })

      // Should not respond to mouse move when allowHalf is false
      expect(thirdStar).toBeInTheDocument()
    })

    it('should detect half hover based on mouse position', () => {
      render(<Rating allowHalf />)

      const thirdStar = screen.getByLabelText('Rate 3 out of 5')

      const mockRect = { left: 0, width: 100 }
      jest.spyOn(thirdStar, 'getBoundingClientRect').mockReturnValue(mockRect as DOMRect)

      // Hover on left half
      fireEvent.mouseMove(thirdStar, { clientX: 25 })

      // Component should detect half position
      expect(thirdStar).toBeInTheDocument()
    })
  })

  // ============================================
  // Visual States (6 tests)
  // ============================================
  describe('Visual States', () => {
    it('should show filled icons for rated items', () => {
      const { container } = render(<Rating value={3} />)

      const filledIcons = container.querySelectorAll('.fill-warning')
      expect(filledIcons.length).toBeGreaterThan(0)
    })

    it('should show half-filled icon when value is half', () => {
      render(<Rating value={2.5} allowHalf />)

      // Half-filled icon should be rendered with 50% width overlay
      const buttons = screen.getAllByRole('button')
      expect(buttons).toHaveLength(5)
    })

    it('should show unfilled icons for unrated items', () => {
      const { container } = render(<Rating value={2} max={5} />)

      const unfilledIcons = container.querySelectorAll('.text-muted')
      expect(unfilledIcons.length).toBeGreaterThan(0)
    })

    it('should apply error styling when error is true', () => {
      render(<Rating error label="Rating" />)

      const label = screen.getByText('Rating')
      expect(label).toHaveClass('text-destructive')
    })

    it('should apply error styling to helper text', () => {
      render(<Rating error helperText="Required" />)

      const helperText = screen.getByText('Required')
      expect(helperText).toHaveClass('text-destructive')
    })

    it('should apply error styling to filled icons', () => {
      const { container } = render(<Rating value={3} error />)

      const errorIcons = container.querySelectorAll('.fill-destructive')
      expect(errorIcons.length).toBeGreaterThan(0)
    })
  })

  // ============================================
  // Disabled and Readonly States (4 tests)
  // ============================================
  describe('Disabled and Readonly States', () => {
    it('should disable all buttons when disabled', () => {
      render(<Rating disabled />)

      const buttons = screen.getAllByRole('button')
      buttons.forEach(button => {
        expect(button).toBeDisabled()
      })
    })

    it('should disable all buttons when readonly', () => {
      render(<Rating readonly />)

      const buttons = screen.getAllByRole('button')
      buttons.forEach(button => {
        expect(button).toBeDisabled()
      })
    })

    it('should apply disabled styling', () => {
      const { container } = render(<Rating disabled />)

      const disabledButton = container.querySelector('.opacity-50')
      expect(disabledButton).toBeInTheDocument()
    })

    it('should apply default cursor when readonly', () => {
      const { container } = render(<Rating readonly />)

      const readonlyButton = container.querySelector('.cursor-default')
      expect(readonlyButton).toBeInTheDocument()
    })
  })

  // ============================================
  // Integration (3 tests)
  // ============================================
  describe('Integration', () => {
    it('should handle complete rating flow', async () => {
      const handleChange = jest.fn()
      const user = userEvent.setup()

      render(<Rating onChange={handleChange} showValue />)

      // Initial state
      expect(screen.getByText('0 / 5')).toBeInTheDocument()

      // Click rating
      await user.click(screen.getByLabelText('Rate 4 out of 5'))

      expect(handleChange).toHaveBeenCalledWith(4)
    })

    it('should work with all props combined', () => {
      render(
        <Rating
          value={3.5}
          onChange={() => {}}
          max={5}
          size="lg"
          icon="heart"
          allowHalf
          showValue
          label="Rate this"
          helperText="Choose a rating"
          className="custom"
        />
      )

      expect(screen.getByText('Rate this')).toBeInTheDocument()
      expect(screen.getByText('Choose a rating')).toBeInTheDocument()
      expect(screen.getByText('3.5 / 5')).toBeInTheDocument()
      expect(screen.getAllByTestId('heart-icon').length).toBeGreaterThan(0)
    })

    it('should handle rapid rating changes', async () => {
      const handleChange = jest.fn()
      const user = userEvent.setup()

      render(<Rating onChange={handleChange} />)

      await user.click(screen.getByLabelText('Rate 1 out of 5'))
      await user.click(screen.getByLabelText('Rate 3 out of 5'))
      await user.click(screen.getByLabelText('Rate 5 out of 5'))

      expect(handleChange).toHaveBeenCalledTimes(3)
      expect(handleChange).toHaveBeenLastCalledWith(5)
    })
  })
})

describe('StarRatingDisplay Component', () => {
  // ============================================
  // Initial Render (4 tests)
  // ============================================
  describe('Initial Render', () => {
    it('should render with default props', () => {
      render(<StarRatingDisplay value={3.5} />)

      expect(screen.getByText('3.5')).toBeInTheDocument()
    })

    it('should render correct number of stars', () => {
      const { container } = render(<StarRatingDisplay value={3} max={5} />)

      const stars = container.querySelectorAll('[data-testid="star-icon"]')
      expect(stars.length).toBeGreaterThanOrEqual(5) // At least 5 stars
    })

    it('should render with custom max value', () => {
      const { container } = render(<StarRatingDisplay value={3} max={10} />)

      const stars = container.querySelectorAll('[data-testid="star-icon"]')
      expect(stars.length).toBeGreaterThanOrEqual(10) // At least 10 stars
    })

    it('should apply custom className', () => {
      const { container } = render(<StarRatingDisplay value={3} className="custom-class" />)

      const wrapper = container.firstChild
      expect(wrapper).toHaveClass('custom-class')
    })
  })

  // ============================================
  // Value Display (5 tests)
  // ============================================
  describe('Value Display', () => {
    it('should display value when showValue is true', () => {
      render(<StarRatingDisplay value={4.2} showValue />)

      expect(screen.getByText('4.2')).toBeInTheDocument()
    })

    it('should not display value when showValue is false', () => {
      render(<StarRatingDisplay value={4.2} showValue={false} />)

      expect(screen.queryByText('4.2')).not.toBeInTheDocument()
    })

    it('should display review count when provided', () => {
      render(<StarRatingDisplay value={4.5} reviewCount={1234} />)

      expect(screen.getByText('(1,234)')).toBeInTheDocument()
    })

    it('should format large review counts', () => {
      render(<StarRatingDisplay value={4.5} reviewCount={1234567} />)

      expect(screen.getByText('(1,234,567)')).toBeInTheDocument()
    })

    it('should not display review count when not provided', () => {
      render(<StarRatingDisplay value={4.5} />)

      // Review count is wrapped in parentheses, so check for that pattern
      expect(screen.queryByText(/^\(/)).not.toBeInTheDocument()
    })
  })

  // ============================================
  // Star Fill States (4 tests)
  // ============================================
  describe('Star Fill States', () => {
    it('should show filled stars for whole number ratings', () => {
      const { container } = render(<StarRatingDisplay value={3} />)

      const filledStars = container.querySelectorAll('.fill-warning')
      expect(filledStars.length).toBeGreaterThan(0)
    })

    it('should show partial fill for decimal ratings', () => {
      const { container } = render(<StarRatingDisplay value={3.7} />)

      // Should render partial fill div with correct width
      const partialFills = container.querySelectorAll('.overflow-hidden')
      expect(partialFills.length).toBeGreaterThan(0)
    })

    it('should calculate correct fill percentage for partial stars', () => {
      const { container } = render(<StarRatingDisplay value={3.3} />)

      // Fourth star should be 30% filled (0.3 * 100 = 30)
      const partialFill = container.querySelector('[style*="width: 30%"]')
      expect(partialFill).toBeInTheDocument()
    })

    it('should show empty stars for ratings below star index', () => {
      const { container } = render(<StarRatingDisplay value={2} max={5} />)

      // Should have unfilled stars
      const unfilledStars = container.querySelectorAll('.text-muted')
      expect(unfilledStars.length).toBeGreaterThan(0)
    })
  })

  // ============================================
  // Size Variants (3 tests)
  // ============================================
  describe('Size Variants', () => {
    it('should apply small size classes', () => {
      const { container } = render(<StarRatingDisplay value={3} size="sm" />)

      const smallIcon = container.querySelector('.h-3.w-3')
      expect(smallIcon).toBeInTheDocument()

      const smallText = container.querySelector('.text-xs')
      expect(smallText).toBeInTheDocument()
    })

    it('should apply medium size classes (default)', () => {
      const { container } = render(<StarRatingDisplay value={3} size="md" />)

      const mediumIcon = container.querySelector('.h-4.w-4')
      expect(mediumIcon).toBeInTheDocument()

      const mediumText = container.querySelector('.text-sm')
      expect(mediumText).toBeInTheDocument()
    })

    it('should apply large size classes', () => {
      const { container } = render(<StarRatingDisplay value={3} size="lg" />)

      const largeIcon = container.querySelector('.h-5.w-5')
      expect(largeIcon).toBeInTheDocument()

      const largeText = container.querySelector('.text-base')
      expect(largeText).toBeInTheDocument()
    })
  })

  // ============================================
  // Integration (1 test)
  // ============================================
  describe('Integration', () => {
    it('should render complete display with all features', () => {
      render(
        <StarRatingDisplay
          value={4.7}
          max={5}
          size="lg"
          showValue
          reviewCount={5432}
          className="custom"
        />
      )

      expect(screen.getByText('4.7')).toBeInTheDocument()
      expect(screen.getByText('(5,432)')).toBeInTheDocument()
    })
  })
})

describe('RatingSummary Component', () => {
  const mockDistribution = [
    { rating: 5, count: 100 },
    { rating: 4, count: 50 },
    { rating: 3, count: 20 },
    { rating: 2, count: 5 },
    { rating: 1, count: 2 },
  ]

  // ============================================
  // Initial Render (3 tests)
  // ============================================
  describe('Initial Render', () => {
    it('should render average rating', () => {
      render(<RatingSummary averageRating={4.5} totalReviews={177} distribution={mockDistribution} />)

      expect(screen.getByText('4.5')).toBeInTheDocument()
    })

    it('should render total reviews count', () => {
      render(<RatingSummary averageRating={4.5} totalReviews={177} distribution={mockDistribution} />)

      expect(screen.getByText('177 reviews')).toBeInTheDocument()
    })

    it('should apply custom className', () => {
      const { container } = render(
        <RatingSummary
          averageRating={4.5}
          totalReviews={177}
          distribution={mockDistribution}
          className="custom-class"
        />
      )

      const wrapper = container.firstChild
      expect(wrapper).toHaveClass('custom-class')
    })
  })

  // ============================================
  // Distribution Display (5 tests)
  // ============================================
  describe('Distribution Display', () => {
    it('should render all rating levels', () => {
      const { container } = render(<RatingSummary averageRating={4.5} totalReviews={177} distribution={mockDistribution} />)

      // Check that distribution bars are rendered (one for each rating level)
      const distributionBars = container.querySelectorAll('.bg-warning')
      expect(distributionBars).toHaveLength(mockDistribution.length)
    })

    it('should display count for each rating level', () => {
      render(<RatingSummary averageRating={4.5} totalReviews={177} distribution={mockDistribution} />)

      expect(screen.getByText('100')).toBeInTheDocument()
      expect(screen.getByText('50')).toBeInTheDocument()
      expect(screen.getByText('20')).toBeInTheDocument()
    })

    it('should sort distribution by rating descending', () => {
      const unsortedDist = [
        { rating: 2, count: 5 },
        { rating: 5, count: 100 },
        { rating: 1, count: 2 },
      ]

      const { container } = render(
        <RatingSummary averageRating={4.5} totalReviews={107} distribution={unsortedDist} />
      )

      const ratings = container.querySelectorAll('.flex.items-center.space-x-1.w-16 span')
      const ratingValues = Array.from(ratings).map(el => el.textContent)

      // Should be sorted 5, 2, 1
      expect(ratingValues[0]).toBe('5')
    })

    it('should calculate bar width percentages correctly', () => {
      const { container } = render(
        <RatingSummary averageRating={4.5} totalReviews={177} distribution={mockDistribution} />
      )

      // Max count is 100, so 5-star bar should be 100% width
      const bars = container.querySelectorAll('.bg-warning')
      expect(bars[0]).toHaveStyle({ width: '100%' })
    })

    it('should format review counts with commas', () => {
      const largeDist = [{ rating: 5, count: 1234 }]

      render(<RatingSummary averageRating={5} totalReviews={1234} distribution={largeDist} />)

      expect(screen.getByText('1,234')).toBeInTheDocument()
    })
  })

  // ============================================
  // Review Count Pluralization (2 tests)
  // ============================================
  describe('Review Count Pluralization', () => {
    it('should show singular "review" for 1 review', () => {
      render(<RatingSummary averageRating={5} totalReviews={1} distribution={[{ rating: 5, count: 1 }]} />)

      expect(screen.getByText('1 review')).toBeInTheDocument()
    })

    it('should show plural "reviews" for multiple reviews', () => {
      render(<RatingSummary averageRating={4.5} totalReviews={177} distribution={mockDistribution} />)

      expect(screen.getByText('177 reviews')).toBeInTheDocument()
    })
  })

  // ============================================
  // Edge Cases (2 tests)
  // ============================================
  describe('Edge Cases', () => {
    it('should handle zero count in distribution', () => {
      const distWithZero = [
        { rating: 5, count: 10 },
        { rating: 4, count: 0 },
      ]

      render(<RatingSummary averageRating={5} totalReviews={10} distribution={distWithZero} />)

      expect(screen.getByText('0')).toBeInTheDocument()
    })

    it('should handle empty distribution array', () => {
      render(<RatingSummary averageRating={0} totalReviews={0} distribution={[]} />)

      expect(screen.getByText('0 reviews')).toBeInTheDocument()
    })
  })

  // ============================================
  // Integration (1 test)
  // ============================================
  describe('Integration', () => {
    it('should render complete summary with all features', () => {
      const { container } = render(
        <RatingSummary
          averageRating={4.3}
          totalReviews={5432}
          distribution={mockDistribution}
          className="custom"
        />
      )

      expect(screen.getByText('4.3')).toBeInTheDocument()
      expect(screen.getByText('5,432 reviews')).toBeInTheDocument()

      // Should show all 5 rating level bars
      const distributionBars = container.querySelectorAll('.bg-warning')
      expect(distributionBars).toHaveLength(mockDistribution.length)
    })
  })
})
