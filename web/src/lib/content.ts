import fs from 'fs'
import path from 'path'
import matter from 'gray-matter'
import readingTime from 'reading-time'
import { z } from 'zod'

// NOTE: This module uses Node.js `fs` APIs. It is ONLY called during `next build`
// (via generateStaticParams / generateMetadata / page components) on a Node.js host.
// It must never be imported in code that runs in the Cloudflare Workers runtime.
// All routes using this module set `dynamicParams = false` to prevent runtime fallback.
// `next build` is always run from the `web/` directory, so process.cwd() reliably
// resolves to `web/`, making `web/content/articles/` the correct content path.
const contentDir = path.join(process.cwd(), 'content', 'articles')

const ArticleFrontmatterSchema = z.object({
  title: z.string(),
  description: z.string().max(160),
  publishedAt: z.string(),
  author: z.string().default('SkillLedger Team'),
  silo: z.enum(['freelancing', 'skill-exchange', 'barter-economy', 'credit-systems', 'collaboration', 'tax-and-legal', 'trust-and-safety', 'industries']),
  tags: z.array(z.string()).default([]),
  draft: z.boolean().default(false),
  buyerStage: z.enum(['awareness', 'consideration', 'decision']).default('awareness'),
  relatedSlugs: z.array(z.string()).default([]),
  faqs: z.array(z.object({ question: z.string(), answer: z.string() })).optional(),
  keyTakeaways: z.array(z.string()).optional(),
  modifiedAt: z.string().optional(),
})

export type ArticleFrontmatter = z.infer<typeof ArticleFrontmatterSchema>

export interface Article {
  slug: string
  frontmatter: ArticleFrontmatter
  content: string
  readingTime: string
}

function getSlugFromFilePath(filePath: string): string {
  return path.basename(filePath, '.mdx')
}

function getAllMdxFiles(dir: string): string[] {
  const files: string[] = []
  if (!fs.existsSync(dir)) return files

  const entries = fs.readdirSync(dir, { withFileTypes: true })
  for (const entry of entries) {
    const fullPath = path.join(dir, entry.name)
    if (entry.isDirectory()) {
      files.push(...getAllMdxFiles(fullPath))
    } else if (entry.name.endsWith('.mdx')) {
      files.push(fullPath)
    }
  }
  return files
}

export function getAllArticles(): Article[] {
  const files = getAllMdxFiles(contentDir)

  return files
    .map((filePath) => {
      const raw = fs.readFileSync(filePath, 'utf-8')
      const { data, content } = matter(raw)

      const parsed = ArticleFrontmatterSchema.safeParse(data)
      if (!parsed.success) return null
      if (parsed.data.draft) return null

      return {
        slug: getSlugFromFilePath(filePath),
        frontmatter: parsed.data,
        content,
        readingTime: readingTime(content).text,
      }
    })
    .filter((a): a is Article => a !== null)
    .sort((a, b) =>
      new Date(b.frontmatter.publishedAt).getTime() -
      new Date(a.frontmatter.publishedAt).getTime()
    )
}

export function getArticleBySlug(slug: string): Article | null {
  const files = getAllMdxFiles(contentDir)
  const filePath = files.find((f) => getSlugFromFilePath(f) === slug)
  if (!filePath) return null

  const raw = fs.readFileSync(filePath, 'utf-8')
  const { data, content } = matter(raw)

  const parsed = ArticleFrontmatterSchema.safeParse(data)
  if (!parsed.success) return null
  if (parsed.data.draft) return null

  return {
    slug,
    frontmatter: parsed.data,
    content,
    readingTime: readingTime(content).text,
  }
}

export function getArticlesBySilo(silo: ArticleFrontmatter['silo']): Article[] {
  return getAllArticles().filter((a) => a.frontmatter.silo === silo)
}
