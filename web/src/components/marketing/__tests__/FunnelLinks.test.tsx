import React from 'react'
import { render, screen } from '@testing-library/react'
import '@testing-library/jest-dom'
import { FunnelLinks } from '../FunnelLinks'

jest.mock('next/link', () => {
  const MockLink = ({ children, href }: { children: React.ReactNode; href: string }) => (
    <a href={href}>{children}</a>
  )
  MockLink.displayName = 'MockLink'
  return MockLink
})

const sampleComparisons = [
  { slug: 'skillledger-vs-fiverr', title: 'SkillLedger vs. Fiverr', description: 'Compare platforms' },
  { slug: 'skillledger-vs-upwork', title: 'SkillLedger vs. Upwork', description: 'Compare upwork' },
]

const sampleHowTo = [
  { slug: 'web-dev-for-design', title: 'Web Dev for Design', skillOffered: 'Web Development', skillNeeded: 'Graphic Design' },
  { slug: 'design-for-writing', title: 'Design for Writing', skillOffered: 'Design', skillNeeded: 'Writing' },
]

const sampleFeatures = [
  { slug: 'credit-wallet-exchange', name: 'Credit Wallet', tagline: 'Trade without cash' },
  { slug: 'project-escrow-protection', name: 'Escrow Protection', tagline: 'Safe exchanges' },
]

const sampleArticles = [
  { slug: 'barter-tax-guide', title: 'Barter Tax Guide', description: 'Understand IRS barter rules' },
  { slug: 'how-to-value-services', title: 'How to Value Services', description: 'Frameworks for pricing' },
]

describe('FunnelLinks', () => {
  describe('bofu stage', () => {
    it('renders nothing for bofu stage', () => {
      const { container } = render(
        <FunnelLinks stage="bofu" comparisons={sampleComparisons} howToGuides={sampleHowTo} features={sampleFeatures} />
      )
      expect(container.firstChild).toBeNull()
    })
  })

  describe('tofu stage', () => {
    it('renders comparison links when provided', () => {
      render(<FunnelLinks stage="tofu" comparisons={sampleComparisons} />)
      expect(screen.getByText('SkillLedger vs. Fiverr')).toBeInTheDocument()
      expect(screen.getByText('SkillLedger vs. Upwork')).toBeInTheDocument()
    })

    it('renders how-to guide links when provided', () => {
      render(<FunnelLinks stage="tofu" howToGuides={sampleHowTo} />)
      expect(screen.getByText('Web Dev for Design')).toBeInTheDocument()
      expect(screen.getByText('Design for Writing')).toBeInTheDocument()
    })

    it('comparison links point to /compare/[slug]', () => {
      render(<FunnelLinks stage="tofu" comparisons={sampleComparisons} />)
      expect(screen.getByRole('link', { name: /skillledger vs. fiverr/i })).toHaveAttribute('href', '/compare/skillledger-vs-fiverr')
    })

    it('how-to links point to /how-to/[slug]', () => {
      render(<FunnelLinks stage="tofu" howToGuides={sampleHowTo} />)
      expect(screen.getByRole('link', { name: /web dev for design/i })).toHaveAttribute('href', '/how-to/web-dev-for-design')
    })

    it('renders "Dig Deeper" label', () => {
      render(<FunnelLinks stage="tofu" comparisons={sampleComparisons} />)
      expect(screen.getByText(/dig deeper/i)).toBeInTheDocument()
    })

    it('renders nothing when both comparisons and howToGuides are empty', () => {
      const { container } = render(<FunnelLinks stage="tofu" />)
      expect(container.firstChild).toBeNull()
    })

    it('shows skill exchange arrows in how-to links', () => {
      render(<FunnelLinks stage="tofu" howToGuides={sampleHowTo} />)
      expect(screen.getByText('Web Development')).toBeInTheDocument()
      expect(screen.getByText('Graphic Design')).toBeInTheDocument()
    })

    it('includes "All comparisons" link', () => {
      render(<FunnelLinks stage="tofu" comparisons={sampleComparisons} />)
      expect(screen.getByRole('link', { name: /all comparisons/i })).toHaveAttribute('href', '/compare')
    })

    it('includes "All how-to guides" link', () => {
      render(<FunnelLinks stage="tofu" howToGuides={sampleHowTo} />)
      expect(screen.getByRole('link', { name: /all how-to guides/i })).toHaveAttribute('href', '/how-to')
    })
  })

  describe('tofu stage with articles', () => {
    it('renders articles column when articles are provided', () => {
      render(<FunnelLinks stage="tofu" comparisons={sampleComparisons} articles={sampleArticles} />)
      expect(screen.getByText('Barter Tax Guide')).toBeInTheDocument()
      expect(screen.getByText('How to Value Services')).toBeInTheDocument()
    })

    it('article links point to /resources/[slug]', () => {
      render(<FunnelLinks stage="tofu" articles={sampleArticles} />)
      expect(screen.getByRole('link', { name: /barter tax guide/i })).toHaveAttribute('href', '/resources/barter-tax-guide')
    })

    it('renders nothing when only articles are empty on tofu', () => {
      const { container } = render(<FunnelLinks stage="tofu" articles={[]} />)
      expect(container.firstChild).toBeNull()
    })

    it('shows articles even without comparisons or howToGuides', () => {
      render(<FunnelLinks stage="tofu" articles={sampleArticles} />)
      expect(screen.getByText('Barter Tax Guide')).toBeInTheDocument()
    })
  })

  describe('mofu stage', () => {
    it('renders feature links when provided', () => {
      render(<FunnelLinks stage="mofu" features={sampleFeatures} />)
      expect(screen.getByText('Credit Wallet')).toBeInTheDocument()
      expect(screen.getByText('Escrow Protection')).toBeInTheDocument()
    })

    it('feature links point to /features/[slug]', () => {
      render(<FunnelLinks stage="mofu" features={sampleFeatures} />)
      expect(screen.getByRole('link', { name: /credit wallet/i })).toHaveAttribute('href', '/features/credit-wallet-exchange')
    })

    it('renders "Ready to Decide?" label', () => {
      render(<FunnelLinks stage="mofu" features={sampleFeatures} />)
      expect(screen.getByText(/ready to decide/i)).toBeInTheDocument()
    })

    it('renders nothing when features are empty', () => {
      const { container } = render(<FunnelLinks stage="mofu" />)
      expect(container.firstChild).toBeNull()
    })

    it('includes links to /features and /pricing', () => {
      render(<FunnelLinks stage="mofu" features={sampleFeatures} />)
      expect(screen.getByRole('link', { name: /all features/i })).toHaveAttribute('href', '/features')
      expect(screen.getByRole('link', { name: /view pricing/i })).toHaveAttribute('href', '/pricing')
    })

    it('renders supplementary articles below features on mofu', () => {
      render(<FunnelLinks stage="mofu" features={sampleFeatures} articles={sampleArticles} />)
      expect(screen.getByText('Barter Tax Guide')).toBeInTheDocument()
    })

    it('article links on mofu point to /resources/[slug]', () => {
      render(<FunnelLinks stage="mofu" features={sampleFeatures} articles={sampleArticles} />)
      expect(screen.getByRole('link', { name: /barter tax guide/i })).toHaveAttribute('href', '/resources/barter-tax-guide')
    })
  })
})
