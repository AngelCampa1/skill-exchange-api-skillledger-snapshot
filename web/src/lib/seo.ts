import { Metadata } from 'next'

// ============================================================================
// SEO Configuration Constants
// ============================================================================

export const SITE_CONFIG = {
  name: 'SkillLedger',
  tagline: 'Professional Collaboration Platform',
  description:
    'Exchange professional services across 19 skill categories in 50 US cities. 30-day free trial — no cash, no commissions. Join SkillLedger today.',
  url: 'https://skillledger.app',
  locale: 'en_US',
  twitterHandle: '@skillledger',
} as const

// Primary keywords for SEO targeting
export const TARGET_KEYWORDS = [
  'professional barter exchange',
  'skills marketplace platform',
  'freelancer collaboration tools',
  'service swap platform',
  'professional credit exchange system',
  'professional collaboration',
  'service exchange',
  'skill trading',
] as const

// ============================================================================
// Default Metadata Templates
// ============================================================================

export const DEFAULT_OG_IMAGE = {
  url: `${SITE_CONFIG.url}/opengraph-image`,
  width: 1200,
  height: 630,
  alt: `${SITE_CONFIG.name} - ${SITE_CONFIG.tagline}`,
} as const

export const DEFAULT_TWITTER_IMAGE = {
  url: `${SITE_CONFIG.url}/opengraph-image`,
  alt: `${SITE_CONFIG.name} - ${SITE_CONFIG.tagline}`,
} as const

// ============================================================================
// Metadata Builder Functions
// ============================================================================

interface BuildMetadataOptions {
  title?: string
  description?: string
  path?: string
  image?: {
    url: string
    width?: number
    height?: number
    alt?: string
  }
  noIndex?: boolean
  keywords?: string[]
}

/**
 * Builds complete metadata object for a page
 * @param options - Configuration options for the metadata
 * @returns Metadata object compatible with Next.js
 */
export function buildMetadata(options: BuildMetadataOptions = {}): Metadata {
  const {
    title,
    description = SITE_CONFIG.description,
    path = '',
    image = DEFAULT_OG_IMAGE,
    noIndex = false,
    keywords = [...TARGET_KEYWORDS],
  } = options

  // Note: Root layout has template `%s | SkillLedger`, so page title doesn't need suffix
  // OG/Twitter titles need full branding since they don't use template
  const pageTitle = title || `${SITE_CONFIG.name} - ${SITE_CONFIG.tagline}`
  const socialTitle = title
    ? `${title} | ${SITE_CONFIG.name}`
    : `${SITE_CONFIG.name} - ${SITE_CONFIG.tagline}`

  const canonicalUrl = `${SITE_CONFIG.url}${path}`

  const metadata: Metadata = {
    title: pageTitle,
    description,
    keywords: keywords.join(', '),
    authors: [{ name: SITE_CONFIG.name }],
    creator: SITE_CONFIG.name,
    publisher: SITE_CONFIG.name,
    metadataBase: new URL(SITE_CONFIG.url),
    alternates: {
      canonical: canonicalUrl,
      languages: {
        'en-US': canonicalUrl,
        'x-default': canonicalUrl,
      },
    },
    openGraph: {
      type: 'website',
      locale: SITE_CONFIG.locale,
      url: canonicalUrl,
      siteName: SITE_CONFIG.name,
      title: socialTitle,
      description,
      images: [
        {
          url: image.url,
          width: image.width || 512,
          height: image.height || 512,
          alt: image.alt || socialTitle,
        },
      ],
    },
    twitter: {
      card: 'summary_large_image',
      site: SITE_CONFIG.twitterHandle,
      creator: SITE_CONFIG.twitterHandle,
      title: socialTitle,
      description,
      images: [image.url],
    },
  }

  if (noIndex) {
    metadata.robots = {
      index: false,
      follow: false,
      googleBot: {
        index: false,
        follow: false,
      },
    }
  }

  return metadata
}

/**
 * Builds metadata for auth pages (login, register, etc.) with noindex
 */
export function buildAuthPageMetadata(
  title: string,
  description: string,
  path: string
): Metadata {
  return buildMetadata({
    title,
    description,
    path,
    noIndex: true,
  })
}

/**
 * Builds metadata for public discovery pages (marketplace, search)
 */
export function buildPublicPageMetadata(
  title: string,
  description: string,
  path: string,
  additionalKeywords: string[] = []
): Metadata {
  return buildMetadata({
    title,
    description,
    path,
    keywords: [...TARGET_KEYWORDS, ...additionalKeywords],
  })
}

// ============================================================================
// JSON-LD Structured Data Generators
// ============================================================================

/**
 * Generates Organization schema for structured data
 */
export function generateOrganizationSchema() {
  return {
    '@context': 'https://schema.org',
    '@type': 'Organization',
    '@id': `${SITE_CONFIG.url}/#organization`,
    name: SITE_CONFIG.name,
    url: SITE_CONFIG.url,
    logo: `${SITE_CONFIG.url}/android-chrome-512x512.png`,
    description: SITE_CONFIG.description,
    sameAs: ['https://x.com/skillledger'],
    foundingDate: '2025',
    knowsAbout: [
      'professional skill exchange',
      'barter economy',
      'freelance collaboration',
      'credit-based trading',
      'escrow services',
      'reputation systems',
      'barter tax compliance',
      'IRS 1099-B reporting',
      'service marketplace',
      'professional networking',
      'skills marketplace',
      'peer-to-peer service trading',
    ],
    contactPoint: {
      '@type': 'ContactPoint',
      contactType: 'customer service',
      url: `${SITE_CONFIG.url}/about`,
    },
  }
}

/**
 * Generates WebSite schema with SearchAction for sitelinks search
 */
export function generateWebSiteSchema() {
  return {
    '@context': 'https://schema.org',
    '@type': 'WebSite',
    name: SITE_CONFIG.name,
    url: SITE_CONFIG.url,
    description: SITE_CONFIG.description,
    potentialAction: {
      '@type': 'SearchAction',
      target: {
        '@type': 'EntryPoint',
        urlTemplate: `${SITE_CONFIG.url}/projects/search?q={search_term_string}`,
      },
      'query-input': 'required name=search_term_string',
    },
  }
}

/**
 * Generates BreadcrumbList schema
 */
export function generateBreadcrumbSchema(
  items: Array<{ name: string; url: string }>
) {
  return {
    '@context': 'https://schema.org',
    '@type': 'BreadcrumbList',
    itemListElement: items.map((item, index) => ({
      '@type': 'ListItem',
      position: index + 1,
      name: item.name,
      item: item.url,
    })),
  }
}

/**
 * Generates Project/Service schema for individual project pages
 */
export function generateProjectSchema(project: {
  id: string
  title: string
  description: string
  category?: string
  createdAt?: string
  creator?: {
    name: string
    url?: string
  }
}) {
  return {
    '@context': 'https://schema.org',
    '@type': 'Service',
    name: project.title,
    description: project.description,
    url: `${SITE_CONFIG.url}/projects/${project.id}`,
    provider: project.creator
      ? {
          '@type': 'Person',
          name: project.creator.name,
          url: project.creator.url,
        }
      : {
          '@type': 'Organization',
          name: SITE_CONFIG.name,
        },
    category: project.category,
    datePublished: project.createdAt,
  }
}

/**
 * Generates SoftwareApplication schema for the platform
 */
export function generateSoftwareApplicationSchema() {
  return {
    '@context': 'https://schema.org',
    '@type': 'SoftwareApplication',
    name: SITE_CONFIG.name,
    description: SITE_CONFIG.description,
    url: SITE_CONFIG.url,
    applicationCategory: 'BusinessApplication',
    operatingSystem: 'Web',
    author: {
      '@type': 'Organization',
      name: SITE_CONFIG.name,
    },
    offers: [
      {
        '@type': 'Offer',
        name: 'Professional',
        price: '19',
        priceCurrency: 'USD',
        description: 'Professional plan with 30-day free trial',
      },
      {
        '@type': 'Offer',
        name: 'Business',
        price: '49',
        priceCurrency: 'USD',
        description: 'Business plan with 30-day free trial',
      },
      {
        '@type': 'Offer',
        name: 'Enterprise',
        price: '99',
        priceCurrency: 'USD',
        description: 'Enterprise plan with 30-day free trial',
      },
    ],
    featureList: [
      'Professional service exchange',
      'Credit-based barter system',
      'Reputation and rating system',
      'Secure messaging',
      'Project collaboration tools',
      'Skills marketplace',
      'Portfolio showcase',
      'Contract management',
    ],
  }
}

/**
 * Generates FAQPage schema for GEO optimization
 */
export function generateFAQSchema(
  faqs: Array<{ question: string; answer: string }>
) {
  return {
    '@context': 'https://schema.org',
    '@type': 'FAQPage',
    mainEntity: faqs.map((faq) => ({
      '@type': 'Question',
      name: faq.question,
      acceptedAnswer: {
        '@type': 'Answer',
        text: faq.answer,
      },
    })),
  }
}

/**
 * Generates ItemList schema for search results and listings
 */
export function generateItemListSchema(
  items: Array<{ name: string; url: string; description?: string }>,
  listName?: string
) {
  return {
    '@context': 'https://schema.org',
    '@type': 'ItemList',
    name: listName,
    numberOfItems: items.length,
    itemListElement: items.map((item, index) => ({
      '@type': 'ListItem',
      position: index + 1,
      name: item.name,
      url: item.url,
      ...(item.description && { description: item.description }),
    })),
  }
}

/**
 * Generates Offer schema for marketplace offerings
 */
export function generateOfferSchema(offer: {
  name: string
  description: string
  price: string
  priceCurrency: string
  url?: string
}) {
  return {
    '@context': 'https://schema.org',
    '@type': 'Offer',
    name: offer.name,
    description: offer.description,
    price: offer.price,
    priceCurrency: offer.priceCurrency,
    availability: 'https://schema.org/InStock',
    url: offer.url || SITE_CONFIG.url,
    seller: {
      '@type': 'Organization',
      name: SITE_CONFIG.name,
    },
  }
}

/**
 * Generates WebPage schema for generic/hub pages
 */
export function generateWebPageSchema(page: { name: string; description: string; url: string }) {
  return {
    '@context': 'https://schema.org',
    '@type': 'WebPage',
    name: page.name,
    description: page.description,
    url: page.url,
    isPartOf: { '@type': 'WebSite', url: SITE_CONFIG.url },
  }
}

/**
 * Generates Pros/Cons Review schema for comparison pages
 */
export function generateProsConsSchema(comparison: {
  name: string
  url: string
  positiveNotes: string[]
  negativeNotes: string[]
}) {
  return {
    '@context': 'https://schema.org',
    '@type': 'Review',
    name: `${comparison.name} Review`,
    url: comparison.url,
    author: { '@type': 'Organization', name: SITE_CONFIG.name },
    positiveNotes: {
      '@type': 'ItemList',
      itemListElement: comparison.positiveNotes.map((note, i) => ({
        '@type': 'ListItem',
        position: i + 1,
        name: note,
      })),
    },
    negativeNotes: {
      '@type': 'ItemList',
      itemListElement: comparison.negativeNotes.map((note, i) => ({
        '@type': 'ListItem',
        position: i + 1,
        name: note,
      })),
    },
  }
}

/**
 * Generates Article schema for MDX blog posts
 */
export function generateArticleSchema(article: {
  title: string
  description: string
  url: string
  publishedAt: string
  modifiedAt?: string
  author?: string
  image?: string
  wordCount?: number
  articleSection?: string
}) {
  return {
    '@context': 'https://schema.org',
    '@type': 'Article',
    headline: article.title,
    description: article.description,
    url: article.url,
    mainEntityOfPage: { '@type': 'WebPage', '@id': article.url },
    datePublished: article.publishedAt,
    dateModified: article.modifiedAt || article.publishedAt,
    author: {
      '@type': 'Person',
      name: article.author || 'SkillLedger Team',
    },
    publisher: { '@id': `${SITE_CONFIG.url}/#organization` },
    ...(article.image && { image: article.image }),
    ...(article.wordCount && { wordCount: article.wordCount }),
    ...(article.articleSection && { articleSection: article.articleSection }),
    speakable: {
      '@type': 'SpeakableSpecification',
      cssSelector: ['article h1', 'article > header > p'],
    },
  }
}

/**
 * Generates HowTo schema for scenario pages
 */
export function generateHowToSchema(howTo: {
  name: string
  description: string
  steps: Array<{ name: string; text: string }>
}) {
  return {
    '@context': 'https://schema.org',
    '@type': 'HowTo',
    name: howTo.name,
    description: howTo.description,
    step: howTo.steps.map((step) => ({
      '@type': 'HowToStep',
      name: step.name,
      text: step.text,
    })),
  }
}

/**
 * Generates DefinedTerm schema for glossary pages
 */
export function generateDefinedTermSchema(term: {
  name: string
  description: string
}) {
  return {
    '@context': 'https://schema.org',
    '@type': 'DefinedTerm',
    name: term.name,
    description: term.description,
    inDefinedTermSet: {
      '@type': 'DefinedTermSet',
      name: 'SkillLedger Glossary',
      url: `${SITE_CONFIG.url}/glossary`,
    },
  }
}

/**
 * Generates Service schema for category/trade pages
 */
export function generateServiceSchema(service: {
  name: string
  serviceType: string
  areaServed?: string
  url?: string
}) {
  return {
    '@context': 'https://schema.org',
    '@type': 'Service',
    name: service.name,
    url: service.url || SITE_CONFIG.url,
    provider: { '@id': `${SITE_CONFIG.url}/#organization` },
    serviceType: service.serviceType,
    areaServed: service.areaServed || 'Worldwide',
  }
}

/**
 * Generates LocalBusiness schema for city+category pages
 */
export function generateLocalBusinessSchema(
  city: { city: string; state: string; slug: string },
  category: { name: string; slug: string }
) {
  return {
    '@context': 'https://schema.org',
    '@type': 'LocalBusiness',
    name: `SkillLedger — ${category.name} in ${city.city}`,
    url: `${SITE_CONFIG.url}/locations/${city.slug}/${category.slug}`,
    address: {
      '@type': 'PostalAddress',
      addressLocality: city.city,
      addressRegion: city.state,
      addressCountry: 'US',
    },
    areaServed: { '@type': 'AdministrativeArea', name: city.city, addressRegion: city.state },
    priceRange: '$19 - $99/mo',
    parentOrganization: { '@id': `${SITE_CONFIG.url}/#organization` },
  }
}

/**
 * Builds metadata for article pages with OG type article
 */
export function buildArticleMetadata(options: {
  title: string
  description: string
  path: string
  publishedAt: string
  tags?: string[]
}): Metadata {
  const base = buildPublicPageMetadata(options.title, options.description, options.path, options.tags || [])
  return {
    ...base,
    openGraph: {
      ...base.openGraph,
      type: 'article',
      publishedTime: options.publishedAt,
    },
  }
}
