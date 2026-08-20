import { FUNNEL_CTA_PRESETS, buyerStageToFunnel, type FunnelStage } from '@/lib/funnel'

describe('buyerStageToFunnel', () => {
  it('maps awareness to tofu', () => {
    expect(buyerStageToFunnel('awareness')).toBe<FunnelStage>('tofu')
  })

  it('maps consideration to mofu', () => {
    expect(buyerStageToFunnel('consideration')).toBe<FunnelStage>('mofu')
  })

  it('maps decision to bofu', () => {
    expect(buyerStageToFunnel('decision')).toBe<FunnelStage>('bofu')
  })
})

describe('FUNNEL_CTA_PRESETS', () => {
  it('has entries for all three stages', () => {
    const stages: FunnelStage[] = ['tofu', 'mofu', 'bofu']
    for (const stage of stages) {
      expect(FUNNEL_CTA_PRESETS[stage]).toBeDefined()
    }
  })

  it('tofu preset links to skill-match and calculator', () => {
    expect(FUNNEL_CTA_PRESETS.tofu.primary.href).toBe('/skill-match')
    expect(FUNNEL_CTA_PRESETS.tofu.secondary.href).toBe('/tools/barter-valuation-calculator')
  })

  it('mofu preset links to compare and pricing', () => {
    expect(FUNNEL_CTA_PRESETS.mofu.primary.href).toBe('/compare')
    expect(FUNNEL_CTA_PRESETS.mofu.secondary.href).toBe('/pricing')
  })

  it('bofu preset links to register and pricing', () => {
    expect(FUNNEL_CTA_PRESETS.bofu.primary.href).toBe('/register')
    expect(FUNNEL_CTA_PRESETS.bofu.secondary.href).toBe('/pricing')
  })

  it('each preset has heading, subheading, primary, secondary', () => {
    const stages: FunnelStage[] = ['tofu', 'mofu', 'bofu']
    for (const stage of stages) {
      const preset = FUNNEL_CTA_PRESETS[stage]
      expect(preset.heading).toBeTruthy()
      expect(preset.subheading).toBeTruthy()
      expect(preset.primary.label).toBeTruthy()
      expect(preset.secondary.label).toBeTruthy()
    }
  })
})
