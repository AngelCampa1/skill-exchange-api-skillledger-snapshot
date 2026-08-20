/**
 * Tests for SkillLedger dynamic sitemap
 * TDD: Write tests FIRST
 */

import {
  getStaticPages,
  getProjectPages,
  getCategoryPages,
  getArticlePages,
  SitemapEntry,
} from '../sitemap'

// Mock fetch for API calls
const mockFetch = jest.fn()
global.fetch = mockFetch

// Mock content loader — avoids filesystem reads in test environment
jest.mock('@/lib/content', () => ({
  getAllArticles: () => [
    { slug: 'how-barter-exchange-works', frontmatter: { publishedAt: '2026-01-15' } },
    { slug: 'what-is-skill-trading', frontmatter: { publishedAt: '2026-01-20' } },
    { slug: 'freelancer-collaboration-guide', frontmatter: { publishedAt: '2026-02-01' } },
    { slug: 'what-is-credit-exchange', frontmatter: { publishedAt: '2026-02-05' } },
    { slug: 'guide-to-service-swapping', frontmatter: { publishedAt: '2026-02-10' } },
    { slug: 'how-to-trade-skills-online', frontmatter: { publishedAt: '2026-02-15' } },
    { slug: 'how-to-find-freelancers', frontmatter: { publishedAt: '2026-02-20' } },
  ],
}))

describe('SkillLedger Sitemap', () => {
  beforeEach(() => {
    jest.clearAllMocks()
  })

  describe('getStaticPages', () => {
    it('returns array of static page entries', () => {
      const pages = getStaticPages()

      expect(Array.isArray(pages)).toBe(true)
      expect(pages.length).toBeGreaterThan(0)
    })

    it('includes homepage with highest priority', () => {
      const pages = getStaticPages()
      const homepage = pages.find((p) => p.url === 'https://skillledger.app')

      expect(homepage).toBeDefined()
      expect(homepage?.priority).toBe(1)
    })

    it('includes marketplace page with high priority', () => {
      const pages = getStaticPages()
      const marketplace = pages.find((p) => p.url.includes('/marketplace'))

      expect(marketplace).toBeDefined()
      expect(marketplace?.priority).toBeGreaterThanOrEqual(0.8)
    })

    it('includes search page', () => {
      const pages = getStaticPages()
      const search = pages.find((p) => p.url.includes('/projects/search'))

      expect(search).toBeDefined()
    })

    it('includes legal pages with low priority', () => {
      const pages = getStaticPages()
      const privacy = pages.find((p) => p.url.includes('/privacy'))
      const terms = pages.find((p) => p.url.includes('/terms'))

      expect(privacy).toBeDefined()
      expect(terms).toBeDefined()
      expect(privacy?.priority).toBeLessThanOrEqual(0.4)
      expect(terms?.priority).toBeLessThanOrEqual(0.4)
    })

    it('all entries have required fields', () => {
      const pages = getStaticPages()

      pages.forEach((page) => {
        expect(page.url).toBeDefined()
        expect(page.url).toMatch(/^https:\/\//)
        expect(page.changeFrequency).toBeDefined()
        expect(page.priority).toBeGreaterThanOrEqual(0)
        expect(page.priority).toBeLessThanOrEqual(1)
      })
    })
  })

  describe('getProjectPages', () => {
    it('returns empty array when API fails', async () => {
      mockFetch.mockRejectedValueOnce(new Error('API Error'))

      const pages = await getProjectPages()

      expect(pages).toEqual([])
    })

    it('returns project pages from API response', async () => {
      const mockProjects = [
        { id: 'proj-1', title: 'Web Development', updatedAt: '2024-01-15T00:00:00Z' },
        { id: 'proj-2', title: 'Logo Design', updatedAt: '2024-01-10T00:00:00Z' },
      ]

      mockFetch.mockResolvedValueOnce({
        ok: true,
        json: async () => ({ projects: mockProjects }),
      })

      const pages = await getProjectPages()

      expect(pages.length).toBe(2)
      expect(pages[0].url).toBe('https://skillledger.app/projects/proj-1')
      expect(pages[1].url).toBe('https://skillledger.app/projects/proj-2')
    })

    it('project pages have correct metadata', async () => {
      const mockProjects = [
        { id: 'proj-1', title: 'Web Development', updatedAt: '2024-01-15T00:00:00Z' },
      ]

      mockFetch.mockResolvedValueOnce({
        ok: true,
        json: async () => ({ projects: mockProjects }),
      })

      const pages = await getProjectPages()

      expect(pages[0].changeFrequency).toBe('weekly')
      expect(pages[0].priority).toBeGreaterThanOrEqual(0.6)
      expect(pages[0].priority).toBeLessThanOrEqual(0.8)
    })

    it('handles empty projects list', async () => {
      mockFetch.mockResolvedValueOnce({
        ok: true,
        json: async () => ({ projects: [] }),
      })

      const pages = await getProjectPages()

      expect(pages).toEqual([])
    })

    it('handles non-ok response', async () => {
      mockFetch.mockResolvedValueOnce({
        ok: false,
        status: 500,
      })

      const pages = await getProjectPages()

      expect(pages).toEqual([])
    })
  })

  describe('getCategoryPages', () => {
    it('returns array of category pages', () => {
      const pages = getCategoryPages()

      expect(Array.isArray(pages)).toBe(true)
      expect(pages.length).toBeGreaterThan(0)
    })

    it('includes common service categories', () => {
      const pages = getCategoryPages()
      const categoryUrls = pages.map((p) => p.url)

      // Expect common categories for a skills marketplace
      expect(categoryUrls.some((u) => u.includes('/categories/'))).toBe(true)
    })

    it('category pages have medium priority', () => {
      const pages = getCategoryPages()

      pages.forEach((page) => {
        expect(page.priority).toBeGreaterThanOrEqual(0.5)
        expect(page.priority).toBeLessThanOrEqual(0.8)
      })
    })
  })

  describe('getArticlePages', () => {
    it('returns array of resource/GEO pages', () => {
      const pages = getArticlePages()

      expect(Array.isArray(pages)).toBe(true)
    })

    it('includes GEO-optimized content pages', () => {
      const pages = getArticlePages()
      const resourceUrls = pages.map((p: SitemapEntry) => p.url)

      // GEO-optimized pages for barter/skills marketplace
      expect(resourceUrls.some((u: string) => u.includes('/how-') || u.includes('/what-'))).toBe(true)
    })

    it('resource pages have appropriate priority', () => {
      const pages = getArticlePages()

      pages.forEach((page: SitemapEntry) => {
        expect(page.priority).toBeGreaterThanOrEqual(0.5)
        expect(page.priority).toBeLessThanOrEqual(0.8)
      })
    })
  })

  describe('sitemap integration', () => {
    it('combines all page types into a single sitemap', async () => {
      const sitemapModule = await import('../sitemap')
      const sitemap = sitemapModule.default

      mockFetch.mockResolvedValueOnce({
        ok: true,
        json: async () => ({ projects: [] }),
      })

      const result = await sitemap()

      expect(Array.isArray(result)).toBe(true)
      // Should have static + articles + categories + industries + scenarios + comparisons + features + glossary + trade pairings + cities + city-skill pages
      expect(result.length).toBeGreaterThan(100)
    })

    it('all sitemap entries have valid URLs', async () => {
      const sitemapModule = await import('../sitemap')
      const sitemap = sitemapModule.default

      mockFetch.mockResolvedValueOnce({
        ok: true,
        json: async () => ({ projects: [] }),
      })

      const result = await sitemap()

      result.forEach((entry: SitemapEntry) => {
        expect(entry.url).toMatch(/^https:\/\/skillledger\.app/)
      })
    })

    it('includes all page types in the combined sitemap', async () => {
      const sitemapModule = await import('../sitemap')
      const sitemap = sitemapModule.default

      mockFetch.mockResolvedValueOnce({
        ok: true,
        json: async () => ({ projects: [{ id: 'proj-1', title: 'Test', updatedAt: '2026-01-01T00:00:00Z' }] }),
      })

      const result = await sitemap()

      // Static pages
      expect(result.some((e: SitemapEntry) => e.url === 'https://skillledger.app')).toBe(true)
      // Articles
      expect(result.some((e: SitemapEntry) => e.url.includes('/resources/'))).toBe(true)
      // Categories
      expect(result.some((e: SitemapEntry) => e.url.includes('/categories/'))).toBe(true)
      // Industries
      expect(result.some((e: SitemapEntry) => e.url.includes('/industries/'))).toBe(true)
      // How-to
      expect(result.some((e: SitemapEntry) => e.url.includes('/how-to/'))).toBe(true)
      // Comparisons
      expect(result.some((e: SitemapEntry) => e.url.includes('/compare/'))).toBe(true)
      // Features
      expect(result.some((e: SitemapEntry) => e.url.includes('/features/'))).toBe(true)
      // Glossary
      expect(result.some((e: SitemapEntry) => e.url.includes('/glossary/'))).toBe(true)
      // Trade pairings
      expect(result.some((e: SitemapEntry) => e.url.includes('/trade/'))).toBe(true)
      // City pages
      expect(result.some((e: SitemapEntry) => e.url.includes('/skill-exchange/'))).toBe(true)
      // City + skill pages
      expect(result.some((e: SitemapEntry) => e.url.includes('/locations/'))).toBe(true)
      // Projects
      expect(result.some((e: SitemapEntry) => e.url.includes('/projects/proj-1'))).toBe(true)
    })

    it('handles API failure gracefully — still returns all static pages', async () => {
      const sitemapModule = await import('../sitemap')
      const sitemap = sitemapModule.default

      mockFetch.mockRejectedValueOnce(new Error('API Error'))

      const result = await sitemap()

      expect(Array.isArray(result)).toBe(true)
      // Should still have all non-API pages
      expect(result.length).toBeGreaterThan(100)
      expect(result.some((e: SitemapEntry) => e.url === 'https://skillledger.app')).toBe(true)
    })
  })
})
