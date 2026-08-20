import React from 'react'
import { render, screen, fireEvent } from '@testing-library/react'
import '@testing-library/jest-dom'
import { FunnelCTA } from '../FunnelCTA'
import { FUNNEL_CTA_PRESETS } from '@/lib/funnel'

jest.mock('next/link', () => {
  const MockLink = ({ children, href, onClick }: { children: React.ReactNode; href: string; onClick?: () => void }) => (
    <a href={href} onClick={onClick}>{children}</a>
  )
  MockLink.displayName = 'MockLink'
  return MockLink
})

jest.mock('@/utils/analytics', () => ({
  trackEvent: jest.fn(),
}))

describe('FunnelCTA', () => {
  describe('TOFU stage', () => {
    it('renders the TOFU heading', () => {
      render(<FunnelCTA stage="tofu" />)
      expect(screen.getByRole('heading')).toHaveTextContent(FUNNEL_CTA_PRESETS.tofu.heading)
    })

    it('renders primary and secondary CTAs', () => {
      render(<FunnelCTA stage="tofu" />)
      expect(screen.getByRole('link', { name: FUNNEL_CTA_PRESETS.tofu.primary.label })).toBeInTheDocument()
      expect(screen.getByRole('link', { name: FUNNEL_CTA_PRESETS.tofu.secondary.label })).toBeInTheDocument()
    })

    it('primary link points to /skill-match', () => {
      render(<FunnelCTA stage="tofu" />)
      expect(screen.getByRole('link', { name: FUNNEL_CTA_PRESETS.tofu.primary.label })).toHaveAttribute('href', '/skill-match')
    })

    it('secondary link points to the calculator', () => {
      render(<FunnelCTA stage="tofu" />)
      expect(screen.getByRole('link', { name: FUNNEL_CTA_PRESETS.tofu.secondary.label })).toHaveAttribute('href', '/tools/barter-valuation-calculator')
    })

    it('personalizes heading with pageContext when stage is tofu', () => {
      render(<FunnelCTA stage="tofu" pageContext="Austin" />)
      expect(screen.getByRole('heading')).toHaveTextContent('Austin')
    })

    it('renders the subheading', () => {
      render(<FunnelCTA stage="tofu" />)
      expect(screen.getByText(FUNNEL_CTA_PRESETS.tofu.subheading)).toBeInTheDocument()
    })
  })

  describe('MOFU stage', () => {
    it('renders the MOFU heading', () => {
      render(<FunnelCTA stage="mofu" />)
      expect(screen.getByRole('heading')).toHaveTextContent(FUNNEL_CTA_PRESETS.mofu.heading)
    })

    it('primary link points to /compare', () => {
      render(<FunnelCTA stage="mofu" />)
      expect(screen.getByRole('link', { name: FUNNEL_CTA_PRESETS.mofu.primary.label })).toHaveAttribute('href', '/compare')
    })

    it('does NOT apply pageContext substitution on mofu stage', () => {
      render(<FunnelCTA stage="mofu" pageContext="Austin" />)
      // mofu heading has no 'Your' token, so heading stays unchanged
      expect(screen.getByRole('heading')).toHaveTextContent(FUNNEL_CTA_PRESETS.mofu.heading)
    })
  })

  describe('BOFU stage', () => {
    it('renders the BOFU heading', () => {
      render(<FunnelCTA stage="bofu" />)
      expect(screen.getByRole('heading')).toHaveTextContent(FUNNEL_CTA_PRESETS.bofu.heading)
    })

    it('primary link points to /register', () => {
      render(<FunnelCTA stage="bofu" />)
      expect(screen.getByRole('link', { name: FUNNEL_CTA_PRESETS.bofu.primary.label })).toHaveAttribute('href', '/register')
    })

    it('does NOT apply pageContext substitution on bofu stage', () => {
      render(<FunnelCTA stage="bofu" pageContext="Austin" />)
      expect(screen.getByRole('heading')).toHaveTextContent(FUNNEL_CTA_PRESETS.bofu.heading)
    })
  })

  describe('click tracking', () => {
    it('calls trackEvent when primary CTA is clicked', () => {
      const { trackEvent } = require('@/utils/analytics')
      render(<FunnelCTA stage="tofu" />)
      fireEvent.click(screen.getByRole('link', { name: FUNNEL_CTA_PRESETS.tofu.primary.label }))
      expect(trackEvent).toHaveBeenCalledWith(
        expect.objectContaining({ name: 'cta_clicked', category: 'conversion' })
      )
    })

    it('calls trackEvent when secondary CTA is clicked', () => {
      const { trackEvent } = require('@/utils/analytics')
      render(<FunnelCTA stage="tofu" />)
      fireEvent.click(screen.getByRole('link', { name: FUNNEL_CTA_PRESETS.tofu.secondary.label }))
      expect(trackEvent).toHaveBeenCalled()
    })
  })
})
