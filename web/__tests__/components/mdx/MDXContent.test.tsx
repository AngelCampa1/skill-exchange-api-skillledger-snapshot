/**
 * Tests for MDXContent component.
 * Verifies that SafeLink renders internal links as Next.js <Link> components
 * and external links as <a> tags with proper security attributes.
 *
 * SafeLink is exported from MDXContent.tsx for direct unit testing.
 * MDXContent itself uses next-mdx-remote/rsc (a React Server Component)
 * which cannot be rendered in jsdom, so we test the sub-component directly.
 */

import React from 'react'
import { render, screen } from '@testing-library/react'
import '@testing-library/jest-dom'
import { SafeLink } from '@/components/mdx/MDXContent'

// next-mdx-remote/rsc is a React Server Component — mock it so jsdom can import the module
jest.mock('next-mdx-remote/rsc', () => ({
  MDXRemote: () => null,
}))

// remark-gfm is pure ESM and cannot be required by Jest CommonJS runner
jest.mock('remark-gfm', () => () => null)

// Mock next/link so we can assert it was used for internal navigation
jest.mock('next/link', () => {
  const MockLink = ({ href, children, className }: { href: string; children: React.ReactNode; className?: string }) => (
    <a data-testid="next-link" href={href} className={className}>
      {children}
    </a>
  )
  MockLink.displayName = 'MockLink'
  return MockLink
})

describe('SafeLink (MDXContent internal link component)', () => {
  describe('internal links', () => {
    it('renders internal links with next-link data attribute', () => {
      render(<SafeLink href="/categories/web-development">Web Dev</SafeLink>)
      const link = screen.getByTestId('next-link')
      expect(link).toBeInTheDocument()
      expect(link).toHaveAttribute('href', '/categories/web-development')
    })

    it('renders internal links with correct text', () => {
      render(<SafeLink href="/register">Get Started</SafeLink>)
      expect(screen.getByText('Get Started')).toBeInTheDocument()
    })

    it('applies the link class to internal links', () => {
      render(<SafeLink href="/glossary/skill-barter">Skill Barter</SafeLink>)
      const link = screen.getByTestId('next-link')
      expect(link).toHaveClass('text-primary', 'underline')
    })

    it('does NOT add target or rel to internal links', () => {
      render(<SafeLink href="/how-to/web-development-for-design">Guide</SafeLink>)
      const link = screen.getByTestId('next-link')
      expect(link).not.toHaveAttribute('target')
      expect(link).not.toHaveAttribute('rel')
    })
  })

  describe('external links', () => {
    it('renders external links as plain anchor tags', () => {
      render(<SafeLink href="https://example.com">External</SafeLink>)
      const link = screen.getByRole('link', { name: 'External' })
      expect(link.tagName).toBe('A')
    })

    it('adds target="_blank" to external links', () => {
      render(<SafeLink href="https://example.com">External</SafeLink>)
      expect(screen.getByRole('link')).toHaveAttribute('target', '_blank')
    })

    it('adds rel="noopener noreferrer nofollow" to external links', () => {
      render(<SafeLink href="https://example.com">External</SafeLink>)
      expect(screen.getByRole('link')).toHaveAttribute('rel', 'noopener noreferrer nofollow')
    })

    it('applies the link class to external links', () => {
      render(<SafeLink href="https://example.com">External</SafeLink>)
      expect(screen.getByRole('link')).toHaveClass('text-primary', 'underline')
    })

    it('does NOT use next-link for external links', () => {
      render(<SafeLink href="https://example.com">External</SafeLink>)
      expect(screen.queryByTestId('next-link')).not.toBeInTheDocument()
    })
  })

  describe('XSS protection', () => {
    it('renders javascript: hrefs as span without link', () => {
      render(<SafeLink href="javascript:alert('xss')">Click</SafeLink>)
      expect(screen.queryByRole('link')).not.toBeInTheDocument()
      expect(screen.getByText('Click').tagName).toBe('SPAN')
    })

    it('renders data: hrefs as span without link', () => {
      render(<SafeLink href="data:text/html,<script>alert(1)</script>">Click</SafeLink>)
      expect(screen.queryByRole('link')).not.toBeInTheDocument()
      expect(screen.getByText('Click').tagName).toBe('SPAN')
    })

    it('allows undefined href (no link)', () => {
      render(<SafeLink>No href</SafeLink>)
      // Should render without error — either as span or anchor
      expect(screen.getByText('No href')).toBeInTheDocument()
    })
  })
})
