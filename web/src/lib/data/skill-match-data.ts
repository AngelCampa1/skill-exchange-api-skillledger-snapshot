import { categoriesData, CategoryData } from './categories-data'
import { scenariosData, ScenarioData } from './scenarios-data'

export interface SkillMatchQuestion {
  id: string
  question: string
  options: Array<{ label: string; value: string }>
}

export const skillMatchQuestions: SkillMatchQuestion[] = [
  {
    id: 'profession',
    question: 'What best describes your profession?',
    options: [
      { label: 'Designer (UX, Graphic, Brand)', value: 'design' },
      { label: 'Developer (Web, Mobile, Backend)', value: 'development' },
      { label: 'Writer (Content, Copy, Technical)', value: 'writing' },
      { label: 'Marketer (SEO, Social, Ads)', value: 'marketing' },
      { label: 'Consultant (Strategy, Finance, Legal)', value: 'consulting' },
      { label: 'Other Professional', value: 'other' },
    ],
  },
  {
    id: 'need',
    question: 'What skill do you need most right now?',
    options: [
      { label: 'Website or App Development', value: 'development' },
      { label: 'Design & Branding', value: 'design' },
      { label: 'Content & Copywriting', value: 'writing' },
      { label: 'Marketing & Growth', value: 'marketing' },
      { label: 'Business Strategy & Consulting', value: 'consulting' },
      { label: 'Something Else', value: 'other' },
    ],
  },
  {
    id: 'experience',
    question: 'How experienced are you with skill exchanges?',
    options: [
      { label: 'Brand new -- never done one', value: 'beginner' },
      { label: 'Tried it informally', value: 'intermediate' },
      { label: 'Experienced barter trader', value: 'advanced' },
    ],
  },
]

/**
 * Maps a quiz value to relevant category slugs
 */
const valueToCategorySlugs: Record<string, string[]> = {
  design: ['design'],
  development: ['web-development', 'mobile-development'],
  writing: ['writing'],
  marketing: ['marketing'],
  consulting: ['consulting', 'legal', 'finance'],
  other: ['video-production', 'photography', 'data-science', 'ai-ml'],
}

/**
 * Maps a quiz value to keywords for scenario matching
 */
const valueToScenarioKeywords: Record<string, string[]> = {
  design: ['design', 'graphic design', 'brand'],
  development: ['web development', 'mobile development', 'development'],
  writing: ['writing', 'content', 'copy'],
  marketing: ['marketing', 'seo', 'social'],
  consulting: ['consulting', 'legal', 'finance', 'strategy'],
  other: ['video', 'photography', 'data', 'ai'],
}

export interface SkillMatchResults {
  categories: CategoryData[]
  scenarios: ScenarioData[]
}

/**
 * Returns matching categories and scenarios based on quiz answers.
 */
export function getSkillMatchResults(
  profession: string,
  need: string
): SkillMatchResults {
  // Gather relevant category slugs from both profession and need
  const professionSlugs = valueToCategorySlugs[profession] || []
  const needSlugs = valueToCategorySlugs[need] || []
  const allSlugs = [...new Set([...professionSlugs, ...needSlugs])]

  // Find matching categories (up to 4)
  const matchedCategories = categoriesData.filter((c) =>
    allSlugs.includes(c.slug)
  )
  // If we have fewer than 3, pad with high-demand categories not already included
  const categories = matchedCategories.slice(0, 4)
  if (categories.length < 3) {
    const existingSlugs = new Set(categories.map((c) => c.slug))
    const extras = categoriesData
      .filter((c) => c.demandLevel === 'high' && !existingSlugs.has(c.slug))
      .slice(0, 3 - categories.length)
    categories.push(...extras)
  }

  // Find matching scenarios based on keywords from both profession and need
  const professionKeywords = valueToScenarioKeywords[profession] || []
  const needKeywords = valueToScenarioKeywords[need] || []

  const matchedScenarios = scenariosData.filter((s) => {
    const offered = s.skillOffered.toLowerCase()
    const needed = s.skillNeeded.toLowerCase()
    // Match scenarios where the user's profession matches the offered skill
    // AND the user's need matches the needed skill, or vice versa
    const professionMatchesOffered = professionKeywords.some((kw) =>
      offered.includes(kw)
    )
    const needMatchesNeeded = needKeywords.some((kw) => needed.includes(kw))
    const professionMatchesNeeded = professionKeywords.some((kw) =>
      needed.includes(kw)
    )
    const needMatchesOffered = needKeywords.some((kw) => offered.includes(kw))

    return (
      (professionMatchesOffered && needMatchesNeeded) ||
      (needMatchesOffered && professionMatchesNeeded) ||
      professionMatchesOffered ||
      needMatchesNeeded
    )
  })

  // Take 2-3 scenarios
  const scenarios = matchedScenarios.slice(0, 3)
  // If we have fewer than 2, pad with first available scenarios
  if (scenarios.length < 2) {
    const existingSlugs = new Set(scenarios.map((s) => s.slug))
    const extras = scenariosData
      .filter((s) => !existingSlugs.has(s.slug))
      .slice(0, 2 - scenarios.length)
    scenarios.push(...extras)
  }

  return { categories, scenarios }
}
