import {
  findCategoriesForArticle,
  findTradePairsForArticle,
  findComparisonsForCategory,
  findComparisonsForIndustry,
  findFeaturesForCategory,
  findHowToGuidesForArticle,
  findComparisonsForArticle,
} from '../cross-links'

describe('findCategoriesForArticle', () => {
  it('returns matching categories for known tags', () => {
    const result = findCategoriesForArticle(['web development', 'design'])
    expect(result.length).toBeGreaterThan(0)
    expect(result[0]).toHaveProperty('slug')
    expect(result[0]).toHaveProperty('name')
  })

  it('returns empty array for unknown tags', () => {
    const result = findCategoriesForArticle(['nonexistent-skill-xyz'])
    expect(result).toEqual([])
  })

  it('deduplicates categories', () => {
    const result = findCategoriesForArticle(['web development', 'web-development', 'Web Development'])
    const slugs = result.map((c) => c.slug)
    expect(new Set(slugs).size).toBe(slugs.length)
  })

  it('limits results to 4', () => {
    const manyTags = [
      'web development', 'design', 'marketing', 'writing',
      'consulting', 'legal', 'finance', 'photography',
    ]
    const result = findCategoriesForArticle(manyTags)
    expect(result.length).toBeLessThanOrEqual(4)
  })

  it('returns empty array for empty tags', () => {
    expect(findCategoriesForArticle([])).toEqual([])
  })
})

describe('findTradePairsForArticle', () => {
  it('returns trade pairs for tags mapping to multiple categories', () => {
    const result = findTradePairsForArticle(['web development', 'design', 'marketing'])
    expect(result.length).toBeGreaterThan(0)
    expect(result[0]).toHaveProperty('skillA')
    expect(result[0]).toHaveProperty('skillB')
    expect(result[0]).toHaveProperty('nameA')
    expect(result[0]).toHaveProperty('nameB')
  })

  it('returns empty for single-category tags', () => {
    const result = findTradePairsForArticle(['web development'])
    expect(result).toEqual([])
  })

  it('limits results to 3 pairs', () => {
    const manyTags = [
      'web development', 'design', 'marketing', 'writing',
      'consulting', 'legal', 'finance',
    ]
    const result = findTradePairsForArticle(manyTags)
    expect(result.length).toBeLessThanOrEqual(3)
  })

  it('returns empty for empty tags', () => {
    expect(findTradePairsForArticle([])).toEqual([])
  })

  it('returns empty for unknown tags', () => {
    expect(findTradePairsForArticle(['nonexistent-xyz'])).toEqual([])
  })
})

describe('findComparisonsForCategory', () => {
  it('returns an array of ComparisonData', () => {
    const result = findComparisonsForCategory('web-development')
    expect(Array.isArray(result)).toBe(true)
    expect(result.length).toBeGreaterThan(0)
    expect(result[0]).toHaveProperty('slug')
    expect(result[0]).toHaveProperty('title')
  })

  it('returns at most 3 results', () => {
    const result = findComparisonsForCategory('design')
    expect(result.length).toBeLessThanOrEqual(3)
  })

  it('falls back to top 3 comparisons for unknown categories', () => {
    const result = findComparisonsForCategory('nonexistent-category-xyz')
    expect(result.length).toBeLessThanOrEqual(3)
  })
})

describe('findComparisonsForIndustry', () => {
  it('returns comparisons for a known industry slug', () => {
    const result = findComparisonsForIndustry('technology')
    expect(Array.isArray(result)).toBe(true)
    expect(result.length).toBeGreaterThan(0)
  })

  it('returns fallback comparisons for unknown industry', () => {
    const result = findComparisonsForIndustry('nonexistent-industry-xyz')
    expect(Array.isArray(result)).toBe(true)
    expect(result.length).toBeLessThanOrEqual(3)
  })

  it('returns at most 3 results', () => {
    const result = findComparisonsForIndustry('creative')
    expect(result.length).toBeLessThanOrEqual(3)
  })
})

describe('findFeaturesForCategory', () => {
  it('returns features whose relatedCategories include the given slug', () => {
    const result = findFeaturesForCategory('web-development')
    expect(Array.isArray(result)).toBe(true)
    result.forEach((f) => {
      expect(f.relatedCategories).toContain('web-development')
    })
  })

  it('returns empty array for category with no feature associations', () => {
    const result = findFeaturesForCategory('nonexistent-slug-xyz')
    expect(result).toEqual([])
  })
})

describe('findHowToGuidesForArticle', () => {
  it('returns scenarios for known skill tags', () => {
    const result = findHowToGuidesForArticle(['web development', 'design'])
    expect(Array.isArray(result)).toBe(true)
    if (result.length > 0) {
      expect(result[0]).toHaveProperty('slug')
      expect(result[0]).toHaveProperty('title')
    }
  })

  it('returns at most 3 results', () => {
    const result = findHowToGuidesForArticle(['web development', 'design', 'marketing', 'writing'])
    expect(result.length).toBeLessThanOrEqual(3)
  })

  it('returns empty array for unknown tags', () => {
    const result = findHowToGuidesForArticle(['nonexistent-tag-xyz'])
    expect(result).toEqual([])
  })

  it('returns empty array for empty tags', () => {
    expect(findHowToGuidesForArticle([])).toEqual([])
  })

  it('deduplicates scenarios across multiple categories', () => {
    const result = findHowToGuidesForArticle(['web development', 'web-development', 'software development'])
    const slugs = result.map((s) => s.slug)
    expect(new Set(slugs).size).toBe(slugs.length)
  })
})

describe('findComparisonsForArticle', () => {
  it('returns comparisons for known skill tags', () => {
    const result = findComparisonsForArticle(['web development'])
    expect(Array.isArray(result)).toBe(true)
    expect(result.length).toBeGreaterThan(0)
  })

  it('returns at most 3 results', () => {
    const result = findComparisonsForArticle(['web development', 'design', 'marketing'])
    expect(result.length).toBeLessThanOrEqual(3)
  })

  it('returns fallback for unknown tags', () => {
    const result = findComparisonsForArticle(['nonexistent-xyz'])
    expect(Array.isArray(result)).toBe(true)
    expect(result.length).toBeLessThanOrEqual(3)
  })

  it('deduplicates comparisons across tags', () => {
    const result = findComparisonsForArticle(['web development', 'software development'])
    const slugs = result.map((c) => c.slug)
    expect(new Set(slugs).size).toBe(slugs.length)
  })
})
