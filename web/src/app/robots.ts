import { MetadataRoute } from 'next'

const BASE_URL = 'https://skillledger.app'

export default function robots(): MetadataRoute.Robots {
  return {
    rules: [
      {
        userAgent: '*',
        allow: '/',
        disallow: [
          '/api/',
          '/dashboard',
          '/profile',
          '/wallet',
          '/messages',
          '/workspace',
          '/my-projects',
          '/applications',
          '/create-project',
          '/subscription',
          '/reputation',
          '/reviews',
        ],
      },
      // Allow AI bots to read public content and LLM context files
      {
        userAgent: ['GPTBot', 'ChatGPT-User', 'ClaudeBot', 'PerplexityBot', 'Googlebot-Extended', 'Bingbot', 'CCBot', 'anthropic-ai'],
        allow: ['/', '/llms.txt', '/llms-full.txt', '/sitemap.xml', '/md/', '/resources/', '/glossary/', '/categories/', '/skill-exchange/', '/how-to/', '/industries/', '/compare/', '/trade/', '/locations/', '/tools/', '/resources/templates', '/features/', '/faq'],
        crawlDelay: 1,
        disallow: [
          '/api/',
          '/dashboard',
          '/profile',
          '/wallet',
          '/messages',
          '/workspace',
          '/my-projects',
          '/applications',
          '/create-project',
          '/subscription',
          '/reputation',
          '/reviews',
        ],
      },
    ],
    sitemap: `${BASE_URL}/sitemap.xml`,
    host: BASE_URL,
  }
}
