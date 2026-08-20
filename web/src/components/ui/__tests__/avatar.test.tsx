import React from 'react'
import { render, screen, fireEvent } from '@testing-library/react'
import '@testing-library/jest-dom'
import { Avatar, AvatarGroup } from '../avatar'

// Mock Next.js Image component
jest.mock('next/image', () => ({
  __esModule: true,
  default: (props: any) => {
    // eslint-disable-next-line @next/next/no-img-element, jsx-a11y/alt-text
    return <img {...props} />
  },
}))

describe('Avatar', () => {
  // ========================================
  // Basic Rendering Tests
  // ========================================
  describe('Basic Rendering', () => {
    it('should render avatar container', () => {
      const { container } = render(<Avatar />)
      const avatar = container.firstChild as HTMLElement
      expect(avatar).toBeInTheDocument()
      expect(avatar.tagName).toBe('DIV')
    })

    it('should apply default size (md)', () => {
      const { container } = render(<Avatar />)
      const avatar = container.firstChild as HTMLElement
      expect(avatar).toHaveClass('h-10')
      expect(avatar).toHaveClass('w-10')
    })

    it('should apply default variant (circular)', () => {
      const { container } = render(<Avatar />)
      const avatar = container.firstChild as HTMLElement
      expect(avatar).toHaveClass('rounded-full')
    })

    it('should apply custom className', () => {
      const { container } = render(<Avatar className="custom-avatar" />)
      const avatar = container.firstChild as HTMLElement
      expect(avatar).toHaveClass('custom-avatar')
    })

    it('should pass through additional props', () => {
      const { container } = render(
        <Avatar data-testid="test-avatar" aria-label="User avatar" />
      )
      const avatar = container.firstChild as HTMLElement
      expect(avatar).toHaveAttribute('data-testid', 'test-avatar')
      expect(avatar).toHaveAttribute('aria-label', 'User avatar')
    })

    it('should forward ref to avatar div', () => {
      const ref = React.createRef<HTMLDivElement>()
      render(<Avatar ref={ref} />)
      expect(ref.current).toBeInstanceOf(HTMLDivElement)
    })
  })

  // ========================================
  // Image Loading Tests
  // ========================================
  describe('Image Loading', () => {
    it('should render image when src is provided', () => {
      render(<Avatar src="/test-avatar.jpg" alt="Test User" />)
      const image = screen.getByAltText('Test User')
      expect(image).toBeInTheDocument()
      expect(image).toHaveAttribute('src', '/test-avatar.jpg')
    })

    it('should use default alt text when not provided', () => {
      render(<Avatar src="/test-avatar.jpg" />)
      const image = screen.getByAltText('Avatar')
      expect(image).toBeInTheDocument()
    })

    it('should apply object-cover class to image', () => {
      render(<Avatar src="/test-avatar.jpg" alt="Test" />)
      const image = screen.getByAltText('Test')
      expect(image).toHaveClass('object-cover')
    })

    it('should show fallback when image fails to load', () => {
      render(<Avatar src="/broken-image.jpg" fallback="John Doe" />)
      const image = screen.getByAltText('Avatar')

      // Simulate image error
      fireEvent.error(image)

      expect(screen.getByText('JD')).toBeInTheDocument()
      expect(screen.queryByAltText('Avatar')).not.toBeInTheDocument()
    })

    it('should show icon when image fails and no fallback provided', () => {
      const { container } = render(<Avatar src="/broken-image.jpg" />)
      const image = screen.getByAltText('Avatar')

      // Simulate image error
      fireEvent.error(image)

      const icon = container.querySelector('svg')
      expect(icon).toBeInTheDocument()
      expect(icon).toHaveAttribute('aria-hidden', 'true')
    })
  })

  // ========================================
  // Fallback Logic Tests
  // ========================================
  describe('Fallback Logic', () => {
    it('should display fallback initials when no src provided', () => {
      render(<Avatar fallback="John Doe" />)
      expect(screen.getByText('JD')).toBeInTheDocument()
    })

    it('should extract initials from two-word name', () => {
      render(<Avatar fallback="Jane Smith" />)
      expect(screen.getByText('JS')).toBeInTheDocument()
    })

    it('should extract initials from three-word name (first two only)', () => {
      render(<Avatar fallback="Mary Jane Watson" />)
      expect(screen.getByText('MJ')).toBeInTheDocument()
    })

    it('should extract first two characters from single word', () => {
      render(<Avatar fallback="Alice" />)
      expect(screen.getByText('AL')).toBeInTheDocument()
    })

    it('should handle name with extra spaces', () => {
      render(<Avatar fallback="  Bob   Johnson  " />)
      expect(screen.getByText('BJ')).toBeInTheDocument()
    })

    it('should handle single character name', () => {
      render(<Avatar fallback="A" />)
      expect(screen.getByText('A')).toBeInTheDocument()
    })

    it('should convert initials to uppercase', () => {
      render(<Avatar fallback="john doe" />)
      expect(screen.getByText('JD')).toBeInTheDocument()
    })

    it('should show default icon when no src and no fallback', () => {
      const { container } = render(<Avatar />)
      const icon = container.querySelector('svg')
      expect(icon).toBeInTheDocument()
      expect(icon).toHaveAttribute('aria-hidden', 'true')
    })

    it('should apply select-none to fallback text', () => {
      const { container } = render(<Avatar fallback="Test User" />)
      const fallbackSpan = container.querySelector('span')
      expect(fallbackSpan).toHaveClass('select-none')
    })
  })

  // ========================================
  // Size Variants Tests
  // ========================================
  describe('Size Variants', () => {
    it('should apply xs size classes', () => {
      const { container } = render(<Avatar size="xs" />)
      const avatar = container.firstChild as HTMLElement
      expect(avatar).toHaveClass('h-6')
      expect(avatar).toHaveClass('w-6')
      expect(avatar).toHaveClass('text-xs')
    })

    it('should apply sm size classes', () => {
      const { container } = render(<Avatar size="sm" />)
      const avatar = container.firstChild as HTMLElement
      expect(avatar).toHaveClass('h-8')
      expect(avatar).toHaveClass('w-8')
      expect(avatar).toHaveClass('text-sm')
    })

    it('should apply md size classes (default)', () => {
      const { container } = render(<Avatar size="md" />)
      const avatar = container.firstChild as HTMLElement
      expect(avatar).toHaveClass('h-10')
      expect(avatar).toHaveClass('w-10')
      expect(avatar).toHaveClass('text-base')
    })

    it('should apply lg size classes', () => {
      const { container } = render(<Avatar size="lg" />)
      const avatar = container.firstChild as HTMLElement
      expect(avatar).toHaveClass('h-12')
      expect(avatar).toHaveClass('w-12')
      expect(avatar).toHaveClass('text-lg')
    })

    it('should apply xl size classes', () => {
      const { container } = render(<Avatar size="xl" />)
      const avatar = container.firstChild as HTMLElement
      expect(avatar).toHaveClass('h-16')
      expect(avatar).toHaveClass('w-16')
      expect(avatar).toHaveClass('text-xl')
    })

    it('should apply 2xl size classes', () => {
      const { container } = render(<Avatar size="2xl" />)
      const avatar = container.firstChild as HTMLElement
      expect(avatar).toHaveClass('h-20')
      expect(avatar).toHaveClass('w-20')
      expect(avatar).toHaveClass('text-2xl')
    })
  })

  // ========================================
  // Variant/Shape Tests
  // ========================================
  describe('Variant/Shape', () => {
    it('should apply circular variant (default)', () => {
      const { container } = render(<Avatar variant="circular" />)
      const avatar = container.firstChild as HTMLElement
      expect(avatar).toHaveClass('rounded-full')
    })

    it('should apply rounded variant', () => {
      const { container } = render(<Avatar variant="rounded" />)
      const avatar = container.firstChild as HTMLElement
      expect(avatar).toHaveClass('rounded-lg')
    })

    it('should apply square variant', () => {
      const { container } = render(<Avatar variant="square" />)
      const avatar = container.firstChild as HTMLElement
      expect(avatar).toHaveClass('rounded-none')
    })
  })

  // ========================================
  // Status Indicator Tests
  // ========================================
  describe('Status Indicator', () => {
    it('should not show status indicator by default', () => {
      const { container } = render(<Avatar status="online" />)
      expect(container.querySelector('[aria-label*="Status"]')).not.toBeInTheDocument()
    })

    it('should show status indicator when showStatus is true', () => {
      render(<Avatar status="online" showStatus />)
      expect(screen.getByLabelText('Status: online')).toBeInTheDocument()
    })

    it('should apply online status color', () => {
      const { container } = render(<Avatar status="online" showStatus />)
      const statusIndicator = container.querySelector('[aria-label*="Status"]')
      expect(statusIndicator).toHaveClass('bg-success')
    })

    it('should apply offline status color', () => {
      const { container } = render(<Avatar status="offline" showStatus />)
      const statusIndicator = container.querySelector('[aria-label*="Status"]')
      expect(statusIndicator).toHaveClass('bg-muted-foreground')
    })

    it('should apply away status color', () => {
      const { container } = render(<Avatar status="away" showStatus />)
      const statusIndicator = container.querySelector('[aria-label*="Status"]')
      expect(statusIndicator).toHaveClass('bg-warning')
    })

    it('should apply busy status color', () => {
      const { container } = render(<Avatar status="busy" showStatus />)
      const statusIndicator = container.querySelector('[aria-label*="Status"]')
      expect(statusIndicator).toHaveClass('bg-destructive')
    })

    it('should apply status size based on avatar size (xs)', () => {
      const { container } = render(<Avatar size="xs" status="online" showStatus />)
      const statusIndicator = container.querySelector('[aria-label*="Status"]')
      expect(statusIndicator).toHaveClass('h-1.5')
      expect(statusIndicator).toHaveClass('w-1.5')
    })

    it('should apply status size based on avatar size (md)', () => {
      const { container } = render(<Avatar size="md" status="online" showStatus />)
      const statusIndicator = container.querySelector('[aria-label*="Status"]')
      expect(statusIndicator).toHaveClass('h-2.5')
      expect(statusIndicator).toHaveClass('w-2.5')
    })

    it('should apply status size based on avatar size (2xl)', () => {
      const { container } = render(<Avatar size="2xl" status="online" showStatus />)
      const statusIndicator = container.querySelector('[aria-label*="Status"]')
      expect(statusIndicator).toHaveClass('h-4')
      expect(statusIndicator).toHaveClass('w-4')
    })

    it('should position status indicator at bottom-right', () => {
      const { container } = render(<Avatar status="online" showStatus />)
      const statusIndicator = container.querySelector('[aria-label*="Status"]')
      expect(statusIndicator).toHaveClass('absolute')
      expect(statusIndicator).toHaveClass('bottom-0')
      expect(statusIndicator).toHaveClass('right-0')
    })

    it('should not show status when showStatus is false even with status prop', () => {
      const { container } = render(<Avatar status="online" showStatus={false} />)
      expect(container.querySelector('[aria-label*="Status"]')).not.toBeInTheDocument()
    })

    it('should not show status when status is undefined but showStatus is true', () => {
      const { container } = render(<Avatar showStatus />)
      expect(container.querySelector('[aria-label*="Status"]')).not.toBeInTheDocument()
    })
  })

  // ========================================
  // AvatarGroup Tests
  // ========================================
  describe('AvatarGroup', () => {
    it('should render avatar group container', () => {
      const { container } = render(
        <AvatarGroup>
          <Avatar fallback="A" />
          <Avatar fallback="B" />
        </AvatarGroup>
      )
      expect(container.firstChild).toBeInTheDocument()
    })

    it('should render all avatars when count is less than max', () => {
      render(
        <AvatarGroup max={5}>
          <Avatar fallback="User 1" />
          <Avatar fallback="User 2" />
          <Avatar fallback="User 3" />
        </AvatarGroup>
      )
      expect(screen.getByText('U1')).toBeInTheDocument()
      expect(screen.getByText('U2')).toBeInTheDocument()
      expect(screen.getByText('U3')).toBeInTheDocument()
    })

    it('should limit avatars to max count', () => {
      render(
        <AvatarGroup max={2}>
          <Avatar fallback="User 1" />
          <Avatar fallback="User 2" />
          <Avatar fallback="User 3" />
          <Avatar fallback="User 4" />
        </AvatarGroup>
      )
      expect(screen.getByText('U1')).toBeInTheDocument()
      expect(screen.getByText('U2')).toBeInTheDocument()
      expect(screen.queryByText('U3')).not.toBeInTheDocument()
      expect(screen.queryByText('U4')).not.toBeInTheDocument()
    })

    it('should show remaining count when avatars exceed max', () => {
      render(
        <AvatarGroup max={2}>
          <Avatar fallback="User 1" />
          <Avatar fallback="User 2" />
          <Avatar fallback="User 3" />
          <Avatar fallback="User 4" />
        </AvatarGroup>
      )
      expect(screen.getByText('+2')).toBeInTheDocument()
    })

    it('should not show remaining count when avatars equal max', () => {
      render(
        <AvatarGroup max={3}>
          <Avatar fallback="User 1" />
          <Avatar fallback="User 2" />
          <Avatar fallback="User 3" />
        </AvatarGroup>
      )
      expect(screen.queryByText('+0')).not.toBeInTheDocument()
    })

    it('should use default max of 5', () => {
      render(
        <AvatarGroup>
          <Avatar fallback="User 1" />
          <Avatar fallback="User 2" />
          <Avatar fallback="User 3" />
          <Avatar fallback="User 4" />
          <Avatar fallback="User 5" />
          <Avatar fallback="User 6" />
          <Avatar fallback="User 7" />
        </AvatarGroup>
      )
      expect(screen.getByText('U1')).toBeInTheDocument()
      expect(screen.getByText('U5')).toBeInTheDocument()
      expect(screen.queryByText('U6')).not.toBeInTheDocument()
      expect(screen.getByText('+2')).toBeInTheDocument()
    })

    it('should apply custom className to group', () => {
      const { container } = render(
        <AvatarGroup className="custom-group">
          <Avatar fallback="A" />
        </AvatarGroup>
      )
      expect(container.firstChild).toHaveClass('custom-group')
    })

    it('should apply negative space between avatars', () => {
      const { container } = render(
        <AvatarGroup>
          <Avatar fallback="A" />
          <Avatar fallback="B" />
        </AvatarGroup>
      )
      expect(container.firstChild).toHaveClass('-space-x-2')
    })

    it('should apply ring to each avatar wrapper', () => {
      const { container } = render(
        <AvatarGroup>
          <Avatar fallback="A" />
          <Avatar fallback="B" />
        </AvatarGroup>
      )
      const wrappers = container.querySelectorAll('.ring-2')
      expect(wrappers.length).toBeGreaterThan(0)
    })

    it('should apply z-index in descending order', () => {
      const { container } = render(
        <AvatarGroup>
          <Avatar fallback="A" />
          <Avatar fallback="B" />
          <Avatar fallback="C" />
        </AvatarGroup>
      )
      const wrappers = container.querySelectorAll('.ring-2')
      const firstWrapper = wrappers[0] as HTMLElement
      const lastWrapper = wrappers[wrappers.length - 2] as HTMLElement // -2 because last is the +count

      const firstZ = parseInt(firstWrapper.style.zIndex || '0')
      const lastZ = parseInt(lastWrapper.style.zIndex || '0')
      expect(firstZ).toBeGreaterThan(lastZ)
    })

    it('should pass size prop to remaining count avatar', () => {
      const { container } = render(
        <AvatarGroup max={1} size="lg">
          <Avatar fallback="User 1" />
          <Avatar fallback="User 2" />
        </AvatarGroup>
      )
      const remainingAvatar = screen.getByText('+1').closest('div')
      expect(remainingAvatar).toHaveClass('h-12')
      expect(remainingAvatar).toHaveClass('w-12')
    })

    it('should handle single avatar in group', () => {
      render(
        <AvatarGroup>
          <Avatar fallback="Solo" />
        </AvatarGroup>
      )
      expect(screen.getByText('SO')).toBeInTheDocument()
      expect(screen.queryByText(/^\+/)).not.toBeInTheDocument()
    })

    it('should handle empty avatar group', () => {
      const { container } = render(<AvatarGroup>{[]}</AvatarGroup>)
      expect(container.firstChild).toBeInTheDocument()
    })
  })

  // ========================================
  // Accessibility Tests
  // ========================================
  describe('Accessibility', () => {
    it('should have alt text on image', () => {
      render(<Avatar src="/test.jpg" alt="User profile" />)
      const image = screen.getByAltText('User profile')
      expect(image).toBeInTheDocument()
    })

    it('should hide default icon from screen readers', () => {
      const { container } = render(<Avatar />)
      const icon = container.querySelector('svg')
      expect(icon).toHaveAttribute('aria-hidden', 'true')
    })

    it('should have aria-label on status indicator', () => {
      render(<Avatar status="online" showStatus />)
      expect(screen.getByLabelText('Status: online')).toBeInTheDocument()
    })

    it('should support custom aria attributes', () => {
      const { container } = render(
        <Avatar aria-label="Team member avatar" role="img" />
      )
      const avatar = container.firstChild as HTMLElement
      expect(avatar).toHaveAttribute('aria-label', 'Team member avatar')
      expect(avatar).toHaveAttribute('role', 'img')
    })
  })

  // ========================================
  // Edge Cases Tests
  // ========================================
  describe('Edge Cases', () => {
    it('should handle empty fallback string', () => {
      const { container } = render(<Avatar fallback="" />)
      // Should show icon since fallback is empty
      const icon = container.querySelector('svg')
      expect(icon).toBeInTheDocument()
    })

    it('should handle fallback with only spaces', () => {
      render(<Avatar fallback="   " />)
      // getInitials with only spaces should return empty string
      expect(screen.queryByText(/[A-Z]{1,2}/)).not.toBeInTheDocument()
    })

    it('should handle numeric fallback', () => {
      render(<Avatar fallback="123" />)
      expect(screen.getByText('12')).toBeInTheDocument()
    })

    it('should handle special characters in fallback', () => {
      render(<Avatar fallback="@User #1" />)
      // "@User #1" splits on whitespace to ["@User", "#1"], taking first char of each
      expect(screen.getByText('@#')).toBeInTheDocument()
    })

    it('should handle very long src URL', () => {
      const longUrl = 'https://example.com/very/long/path/' + 'a'.repeat(200) + '.jpg'
      render(<Avatar src={longUrl} alt="Test" />)
      const image = screen.getByAltText('Test')
      expect(image).toHaveAttribute('src', longUrl)
    })

    it('should handle all props together', () => {
      render(
        <Avatar
          src="/test.jpg"
          alt="Test User"
          fallback="Test User"
          size="lg"
          variant="rounded"
          status="online"
          showStatus
          className="custom-class"
          data-testid="full-avatar"
        />
      )
      const image = screen.getByAltText('Test User')
      expect(image).toBeInTheDocument()

      const avatar = screen.getByTestId('full-avatar')
      expect(avatar).toHaveClass('h-12')
      expect(avatar).toHaveClass('rounded-lg')
      expect(avatar).toHaveClass('custom-class')

      expect(screen.getByLabelText('Status: online')).toBeInTheDocument()
    })

    it('should recover from image error and show fallback', () => {
      const { rerender } = render(
        <Avatar src="/initial.jpg" fallback="Test User" />
      )

      const image = screen.getByAltText('Avatar')
      fireEvent.error(image)

      expect(screen.getByText('TU')).toBeInTheDocument()

      // Changing src should try to load new image
      rerender(<Avatar src="/new.jpg" fallback="Test User" />)

      // Image error state should persist (imageError is true)
      expect(screen.getByText('TU')).toBeInTheDocument()
    })
  })

  // ========================================
  // Integration Tests
  // ========================================
  describe('Integration', () => {
    it('should work in avatar group with mixed states', () => {
      render(
        <AvatarGroup max={4}>
          <Avatar src="/user1.jpg" alt="User 1" />
          <Avatar fallback="User Two" />
          <Avatar />
          <Avatar status="online" showStatus />
        </AvatarGroup>
      )

      expect(screen.getByAltText('User 1')).toBeInTheDocument()
      expect(screen.getByText('UT')).toBeInTheDocument()
    })

    it('should maintain image error state across re-renders', () => {
      const { rerender } = render(<Avatar src="/test.jpg" fallback="Test" />)

      const image = screen.getByAltText('Avatar')
      fireEvent.error(image)

      expect(screen.getByText('TE')).toBeInTheDocument()

      // Re-render with different props
      rerender(<Avatar src="/test.jpg" fallback="Test" size="lg" />)

      // Should still show fallback
      expect(screen.getByText('TE')).toBeInTheDocument()
    })

    it('should support avatar group with size prop', () => {
      render(
        <AvatarGroup size="xl" max={2}>
          <Avatar fallback="A" />
          <Avatar fallback="B" />
          <Avatar fallback="C" />
        </AvatarGroup>
      )

      // AvatarGroup passes size only to the remaining count avatar
      const remainingAvatar = screen.getByText('+1').closest('div')
      expect(remainingAvatar).toHaveClass('h-16')
      expect(remainingAvatar).toHaveClass('w-16')
    })

    it('should handle complex nested scenarios', () => {
      const { container } = render(
        <div>
          <AvatarGroup max={3}>
            <Avatar src="/user1.jpg" alt="User 1" size="md" status="online" showStatus />
            <Avatar fallback="Jane Doe" size="md" variant="rounded" />
            <Avatar size="md" variant="square" />
            <Avatar fallback="Extra User" />
          </AvatarGroup>
        </div>
      )

      expect(screen.getByAltText('User 1')).toBeInTheDocument()
      expect(screen.getByText('JD')).toBeInTheDocument()
      expect(screen.getByText('+1')).toBeInTheDocument()
      expect(container.querySelector('svg')).toBeInTheDocument()
    })
  })
})
