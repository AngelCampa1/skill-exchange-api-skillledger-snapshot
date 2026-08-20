/**
 * Tests for the internal link validator.
 *
 * Tests buildKnownRoutes(), isValidHref(), parseRelatedSlugsFromFrontmatter(),
 * and collectMdxSlugs() using the actual project root.
 */

import * as path from 'path'
import {
  buildKnownRoutes,
  isValidHref,
  parseRelatedSlugsFromFrontmatter,
  collectMdxSlugs,
  validateRelatedSlugs,
  SKIP_HREFS,
  DEFAULT_ROOT,
} from '../../scripts/validate-links'

const PROJECT_ROOT = path.resolve(__dirname, '../..')

describe('buildKnownRoutes', () => {
  let known: Set<string>

  beforeAll(() => {
    known = buildKnownRoutes(PROJECT_ROOT)
  })

  it('includes the root route', () => {
    expect(known.has('/')).toBe(true)
  })

  it('includes static app routes like /register and /login', () => {
    expect(known.has('/register')).toBe(true)
    expect(known.has('/login')).toBe(true)
  })

  it('includes glossary slug routes', () => {
    expect(known.has('/glossary/skill-barter')).toBe(true)
    expect(known.has('/glossary/credit-exchange')).toBe(true)
  })

  it('includes category slug routes', () => {
    expect(known.has('/categories/web-development')).toBe(true)
    expect(known.has('/categories/design')).toBe(true)
    expect(known.has('/categories/marketing')).toBe(true)
  })

  it('includes industry slug routes', () => {
    expect(known.has('/industries/legal-professionals')).toBe(true)
    expect(known.has('/industries/saas-startups')).toBe(true)
  })

  it('includes comparison slug routes', () => {
    // At least one comparison route should exist
    const hasComparison = [...known].some((r) => r.startsWith('/compare/'))
    expect(hasComparison).toBe(true)
  })

  it('includes how-to scenario routes', () => {
    expect(known.has('/how-to/web-development-for-design')).toBe(true)
    expect(known.has('/how-to/design-for-marketing')).toBe(true)
  })

  it('includes MDX article routes', () => {
    expect(known.has('/resources/how-barter-exchange-works')).toBe(true)
    expect(known.has('/resources/what-is-skill-trading')).toBe(true)
  })

  it('includes exhaustive trade pair routes', () => {
    expect(known.has('/trade/web-development/for/design')).toBe(true)
    expect(known.has('/trade/design/for/marketing')).toBe(true)
  })

  it('does NOT include self-pair trade routes', () => {
    expect(known.has('/trade/web-development/for/web-development')).toBe(false)
  })

  it('includes skill-exchange city routes', () => {
    expect(known.has('/skill-exchange/new-york')).toBe(true)
    expect(known.has('/skill-exchange/san-francisco')).toBe(true)
  })

  it('includes locations city/category combo routes', () => {
    expect(known.has('/locations/new-york/web-development')).toBe(true)
    expect(known.has('/locations/chicago/marketing')).toBe(true)
  })

  it('has more than 1000 known routes (exhaustive coverage)', () => {
    expect(known.size).toBeGreaterThan(1000)
  })
})

describe('isValidHref', () => {
  const knownRoutes = new Set([
    '/',
    '/register',
    '/login',
    '/categories/web-development',
    '/trade/web-development/for/design',
    '/skill-exchange/new-york',
    '/locations/chicago/marketing',
  ])

  it('accepts exact known routes', () => {
    expect(isValidHref('/register', knownRoutes)).toBe(true)
    expect(isValidHref('/categories/web-development', knownRoutes)).toBe(true)
  })

  it('accepts root route', () => {
    expect(isValidHref('/', knownRoutes)).toBe(true)
  })

  it('accepts routes with trailing slash (stripped)', () => {
    expect(isValidHref('/register/', knownRoutes)).toBe(true)
  })

  it('accepts routes with hash fragment (stripped)', () => {
    expect(isValidHref('/categories/web-development#section', knownRoutes)).toBe(true)
  })

  it('accepts routes with query string (stripped)', () => {
    expect(isValidHref('/login?redirect=/dashboard', knownRoutes)).toBe(true)
  })

  it('accepts SKIP_HREFS entries', () => {
    for (const href of SKIP_HREFS) {
      expect(isValidHref(href, knownRoutes)).toBe(true)
    }
  })

  it('rejects unknown routes', () => {
    expect(isValidHref('/nonexistent-page', knownRoutes)).toBe(false)
    expect(isValidHref('/industries/marketers', knownRoutes)).toBe(false)
    expect(isValidHref('/home', knownRoutes)).toBe(false)
  })

  it('rejects unknown dynamic routes', () => {
    // These would pass under old DYNAMIC_PREFIXES bypass; now they must be in the known set
    expect(isValidHref('/trade/fake-skill/for/another-fake', knownRoutes)).toBe(false)
    expect(isValidHref('/skill-exchange/fake-city', knownRoutes)).toBe(false)
  })

  it('accepts exhaustive dynamic routes in known set', () => {
    expect(isValidHref('/trade/web-development/for/design', knownRoutes)).toBe(true)
    expect(isValidHref('/skill-exchange/new-york', knownRoutes)).toBe(true)
    expect(isValidHref('/locations/chicago/marketing', knownRoutes)).toBe(true)
  })
})

describe('parseRelatedSlugsFromFrontmatter', () => {
  it('parses double-quoted slugs', () => {
    const content = `---
title: "Test"
relatedSlugs: ["how-barter-exchange-works", "what-is-skill-trading"]
---
Body`
    expect(parseRelatedSlugsFromFrontmatter(content)).toEqual([
      'how-barter-exchange-works',
      'what-is-skill-trading',
    ])
  })

  it('parses single-quoted slugs', () => {
    const content = `---
title: "Test"
relatedSlugs: ['how-barter-exchange-works', 'guide-to-service-swapping']
---
Body`
    expect(parseRelatedSlugsFromFrontmatter(content)).toEqual([
      'how-barter-exchange-works',
      'guide-to-service-swapping',
    ])
  })

  it('returns empty array for empty relatedSlugs', () => {
    const content = `---
title: "Test"
relatedSlugs: []
---
Body`
    expect(parseRelatedSlugsFromFrontmatter(content)).toEqual([])
  })

  it('returns empty array for relatedSlugs with no value', () => {
    const content = `---
title: "Test"
relatedSlugs:
---
Body`
    expect(parseRelatedSlugsFromFrontmatter(content)).toEqual([])
  })

  it('returns empty array when no frontmatter', () => {
    const content = 'Just plain content without frontmatter'
    expect(parseRelatedSlugsFromFrontmatter(content)).toEqual([])
  })

  it('returns empty array when no relatedSlugs field', () => {
    const content = `---
title: "Test"
tags: ["barter"]
---
Body`
    expect(parseRelatedSlugsFromFrontmatter(content)).toEqual([])
  })

  it('handles three slugs', () => {
    const content = `---
title: "Test"
relatedSlugs: ["slug-one", "slug-two", "slug-three"]
---
Body`
    expect(parseRelatedSlugsFromFrontmatter(content)).toEqual([
      'slug-one',
      'slug-two',
      'slug-three',
    ])
  })

  it('parses YAML list format', () => {
    const content = `---
title: "Test"
relatedSlugs:
  - "professional-services-exchange-legal"
  - "skill-exchange-for-saas-startups"
---
Body`
    expect(parseRelatedSlugsFromFrontmatter(content)).toEqual([
      'professional-services-exchange-legal',
      'skill-exchange-for-saas-startups',
    ])
  })
})

describe('collectMdxSlugs', () => {
  it('returns slugs without .mdx extension', () => {
    const contentDir = path.join(PROJECT_ROOT, 'content', 'articles')
    const slugs = collectMdxSlugs(contentDir)
    expect(slugs).toContain('how-barter-exchange-works')
    expect(slugs).toContain('what-is-skill-trading')
    expect(slugs.every((s) => !s.endsWith('.mdx'))).toBe(true)
  })

  it('returns slugs from nested directories', () => {
    const contentDir = path.join(PROJECT_ROOT, 'content', 'articles')
    const slugs = collectMdxSlugs(contentDir)
    // Articles are in subdirectories (barter-economy/, collaboration/, etc.)
    expect(slugs.length).toBeGreaterThan(40)
  })
})

describe('validateRelatedSlugs', () => {
  it('returns no errors when all relatedSlugs are valid article slugs', () => {
    const knownSlugs = new Set(['how-barter-exchange-works', 'what-is-skill-trading', 'guide-to-service-swapping'])
    const content = `---
relatedSlugs: ["how-barter-exchange-works", "what-is-skill-trading"]
---`
    // Inline test with mocked filesystem is impractical; validate against real project
    const articleSlugs = new Set(collectMdxSlugs(path.join(PROJECT_ROOT, 'content', 'articles')))
    const errors = validateRelatedSlugs(PROJECT_ROOT, articleSlugs)
    // All relatedSlugs in the project should reference valid articles
    expect(errors).toEqual([])
  })
})
