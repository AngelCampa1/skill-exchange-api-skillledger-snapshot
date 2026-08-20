/**
 * Tests for SEO utilities and Schema.org structured data
 * TDD: Write tests FIRST
 */

import {
  SITE_CONFIG,
  TARGET_KEYWORDS,
  buildMetadata,
  buildAuthPageMetadata,
  buildPublicPageMetadata,
  generateOrganizationSchema,
  generateWebSiteSchema,
  generateBreadcrumbSchema,
  generateProjectSchema,
  generateSoftwareApplicationSchema,
  generateFAQSchema,
  generateItemListSchema,
  generateOfferSchema,
} from '../seo'

describe('SITE_CONFIG', () => {
  it('has required site configuration', () => {
    expect(SITE_CONFIG.name).toBe('SkillLedger')
    expect(SITE_CONFIG.url).toBeDefined()
    expect(SITE_CONFIG.description).toBeDefined()
    expect(SITE_CONFIG.locale).toBe('en_US')
  })
})

describe('TARGET_KEYWORDS', () => {
  it('contains relevant SEO keywords', () => {
    expect(TARGET_KEYWORDS.length).toBeGreaterThan(0)
    expect(TARGET_KEYWORDS).toContain('professional barter exchange')
    expect(TARGET_KEYWORDS).toContain('skills marketplace platform')
  })
})

describe('buildMetadata', () => {
  it('builds metadata with defaults', () => {
    const metadata = buildMetadata()
    expect(metadata.description).toBe(SITE_CONFIG.description)
    expect(metadata.openGraph?.siteName).toBe(SITE_CONFIG.name)
  })

  it('builds metadata with custom title', () => {
    const metadata = buildMetadata({ title: 'Custom Page' })
    expect(metadata.title).toBe('Custom Page')
  })

  it('sets canonical URL from path', () => {
    const metadata = buildMetadata({ path: '/about' })
    expect(metadata.alternates?.canonical).toBe(`${SITE_CONFIG.url}/about`)
  })

  it('sets noindex when specified', () => {
    const metadata = buildMetadata({ noIndex: true })
    expect(metadata.robots).toBeDefined()
  })
})

describe('buildAuthPageMetadata', () => {
  it('creates noindex metadata for auth pages', () => {
    const metadata = buildAuthPageMetadata('Login', 'Sign in to your account', '/login')
    expect(metadata.robots).toBeDefined()
    expect(metadata.title).toBe('Login')
  })
})

describe('buildPublicPageMetadata', () => {
  it('creates indexable metadata for public pages', () => {
    const metadata = buildPublicPageMetadata(
      'Marketplace',
      'Browse professional services',
      '/marketplace',
      ['service marketplace']
    )
    expect(metadata.title).toBe('Marketplace')
    expect(metadata.robots).toBeUndefined()
  })
})

describe('generateOrganizationSchema', () => {
  it('generates valid Organization schema', () => {
    const schema = generateOrganizationSchema()
    expect(schema['@context']).toBe('https://schema.org')
    expect(schema['@type']).toBe('Organization')
    expect(schema.name).toBe('SkillLedger')
    expect(schema.url).toBeDefined()
    expect(schema.logo).toBeDefined()
  })

  it('includes contact point', () => {
    const schema = generateOrganizationSchema()
    expect(schema.contactPoint).toBeDefined()
    expect(schema.contactPoint['@type']).toBe('ContactPoint')
  })
})

describe('generateWebSiteSchema', () => {
  it('generates valid WebSite schema', () => {
    const schema = generateWebSiteSchema()
    expect(schema['@context']).toBe('https://schema.org')
    expect(schema['@type']).toBe('WebSite')
    expect(schema.name).toBe('SkillLedger')
  })

  it('includes SearchAction for sitelinks', () => {
    const schema = generateWebSiteSchema()
    expect(schema.potentialAction).toBeDefined()
    expect(schema.potentialAction['@type']).toBe('SearchAction')
  })
})

describe('generateBreadcrumbSchema', () => {
  it('generates valid BreadcrumbList schema', () => {
    const items = [
      { name: 'Home', url: 'https://skillledger.app' },
      { name: 'Projects', url: 'https://skillledger.app/projects' },
    ]
    const schema = generateBreadcrumbSchema(items)

    expect(schema['@context']).toBe('https://schema.org')
    expect(schema['@type']).toBe('BreadcrumbList')
    expect(schema.itemListElement).toHaveLength(2)
    expect(schema.itemListElement[0].position).toBe(1)
    expect(schema.itemListElement[1].position).toBe(2)
  })
})

describe('generateProjectSchema', () => {
  it('generates valid Service schema for project', () => {
    const project = {
      id: '123',
      title: 'Web Development Service',
      description: 'Full-stack development services',
      category: 'Development',
      creator: { name: 'John Doe', url: 'https://skillledger.app/profile/john' },
    }
    const schema = generateProjectSchema(project)

    expect(schema['@context']).toBe('https://schema.org')
    expect(schema['@type']).toBe('Service')
    expect(schema.name).toBe('Web Development Service')
    expect(schema.provider['@type']).toBe('Person')
  })

  it('uses Organization as provider when no creator', () => {
    const project = {
      id: '123',
      title: 'Service',
      description: 'Description',
    }
    const schema = generateProjectSchema(project)
    expect(schema.provider['@type']).toBe('Organization')
  })
})

describe('generateSoftwareApplicationSchema', () => {
  it('generates valid SoftwareApplication schema', () => {
    const schema = generateSoftwareApplicationSchema()

    expect(schema['@context']).toBe('https://schema.org')
    expect(schema['@type']).toBe('SoftwareApplication')
    expect(schema.name).toBe('SkillLedger')
    expect(schema.applicationCategory).toBe('BusinessApplication')
    expect(schema.operatingSystem).toBe('Web')
  })

  it('includes offers with pricing', () => {
    const schema = generateSoftwareApplicationSchema()
    expect(schema.offers).toBeDefined()
    expect(Array.isArray(schema.offers)).toBe(true)
    expect(schema.offers.length).toBeGreaterThan(0)
  })

  it('includes feature list', () => {
    const schema = generateSoftwareApplicationSchema()
    expect(schema.featureList).toBeDefined()
    expect(Array.isArray(schema.featureList)).toBe(true)
    expect(schema.featureList.length).toBeGreaterThan(0)
  })
})

describe('generateFAQSchema', () => {
  it('generates valid FAQPage schema', () => {
    const faqs = [
      { question: 'What is SkillLedger?', answer: 'A professional collaboration platform.' },
      { question: 'How does it work?', answer: 'Exchange services using credits.' },
    ]
    const schema = generateFAQSchema(faqs)

    expect(schema['@context']).toBe('https://schema.org')
    expect(schema['@type']).toBe('FAQPage')
    expect(schema.mainEntity).toHaveLength(2)
  })

  it('formats questions correctly', () => {
    const faqs = [{ question: 'Test Q?', answer: 'Test A' }]
    const schema = generateFAQSchema(faqs)

    expect(schema.mainEntity[0]['@type']).toBe('Question')
    expect(schema.mainEntity[0].name).toBe('Test Q?')
    expect(schema.mainEntity[0].acceptedAnswer['@type']).toBe('Answer')
    expect(schema.mainEntity[0].acceptedAnswer.text).toBe('Test A')
  })
})

describe('generateItemListSchema', () => {
  it('generates valid ItemList schema for search results', () => {
    const items = [
      { name: 'Web Design', url: 'https://skillledger.app/projects/1' },
      { name: 'Logo Design', url: 'https://skillledger.app/projects/2' },
    ]
    const schema = generateItemListSchema(items, 'Design Services')

    expect(schema['@context']).toBe('https://schema.org')
    expect(schema['@type']).toBe('ItemList')
    expect(schema.name).toBe('Design Services')
    expect(schema.itemListElement).toHaveLength(2)
  })

  it('sets positions correctly', () => {
    const items = [
      { name: 'Item 1', url: 'https://example.com/1' },
      { name: 'Item 2', url: 'https://example.com/2' },
    ]
    const schema = generateItemListSchema(items)

    expect(schema.itemListElement[0].position).toBe(1)
    expect(schema.itemListElement[1].position).toBe(2)
  })
})

describe('generateOfferSchema', () => {
  it('generates valid Offer schema', () => {
    const offer = {
      name: 'Premium Subscription',
      description: 'Full access to all features',
      price: '29',
      priceCurrency: 'USD',
    }
    const schema = generateOfferSchema(offer)

    expect(schema['@context']).toBe('https://schema.org')
    expect(schema['@type']).toBe('Offer')
    expect(schema.name).toBe('Premium Subscription')
    expect(schema.price).toBe('29')
    expect(schema.priceCurrency).toBe('USD')
  })

  it('includes availability', () => {
    const schema = generateOfferSchema({
      name: 'Basic',
      description: 'Basic access',
      price: '0',
      priceCurrency: 'USD',
    })
    expect(schema.availability).toBeDefined()
  })
})
