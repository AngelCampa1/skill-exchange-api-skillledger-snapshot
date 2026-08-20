import React from 'react'
import { render, screen } from '@testing-library/react'
import '@testing-library/jest-dom'
import { RelatedHubs } from '../RelatedHubs'

jest.mock('next/link', () => {
  const MockLink = ({ children, href }: { children: React.ReactNode; href: string }) => (
    <a href={href}>{children}</a>
  )
  MockLink.displayName = 'MockLink'
  return MockLink
})

describe('RelatedHubs', () => {
  it('renders a section heading', () => {
    render(<RelatedHubs currentPath="/categories" />)
    expect(screen.getByText(/explore/i)).toBeInTheDocument()
  })

  it('excludes current path from results', () => {
    render(<RelatedHubs currentPath="/categories" />)
    const links = screen.getAllByRole('link')
    const hrefs = links.map((l) => l.getAttribute('href'))
    expect(hrefs).not.toContain('/categories')
  })

  it('shows exactly 5 related hub links', () => {
    render(<RelatedHubs currentPath="/categories" />)
    const links = screen.getAllByRole('link')
    expect(links.length).toBe(5)
  })

  it('for a TOFU page includes some MOFU hubs', () => {
    render(<RelatedHubs currentPath="/glossary" />)
    const links = screen.getAllByRole('link')
    const hrefs = links.map((l) => l.getAttribute('href'))
    const mofuHubs = ['/compare', '/how-to', '/features', '/resources', '/tools']
    const hasMofu = hrefs.some((h) => mofuHubs.includes(h ?? ''))
    expect(hasMofu).toBe(true)
  })

  it('for a MOFU page includes /pricing link as BOFU target', () => {
    render(<RelatedHubs currentPath="/compare" />)
    const links = screen.getAllByRole('link')
    const hrefs = links.map((l) => l.getAttribute('href'))
    expect(hrefs).toContain('/pricing')
  })

  it('renders hub labels as link text', () => {
    render(<RelatedHubs currentPath="/categories" />)
    expect(screen.getByText('Platform Comparisons')).toBeInTheDocument()
  })

  it('renders hub descriptions', () => {
    render(<RelatedHubs currentPath="/categories" />)
    expect(screen.getByText(/step-by-step/i)).toBeInTheDocument()
  })

  it('hub links navigate to correct paths', () => {
    render(<RelatedHubs currentPath="/features" />)
    const link = screen.getByRole('link', { name: /platform comparisons/i })
    expect(link).toHaveAttribute('href', '/compare')
  })

  it('does not crash when currentPath is not a hub path', () => {
    render(<RelatedHubs currentPath="/some-random-page" />)
    const links = screen.getAllByRole('link')
    expect(links.length).toBe(5)
  })
})
