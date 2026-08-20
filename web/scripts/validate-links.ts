/* eslint-disable no-console */
/**
 * Internal link validator for SkillLedger.
 * Scans all MDX content files and TSX/component files for internal href/link patterns,
 * then verifies each target exists as a valid static route or data-driven slug.
 *
 * Run with: yarn validate-links
 *           yarn validate-links --report   (coverage summary without failing)
 *
 * Console output is intentional — this is a CLI tool run from the command line.
 */

import * as fs from 'fs'
import * as path from 'path'

export const DEFAULT_ROOT = path.resolve(__dirname, '..')

// ---------------------------------------------------------------------------
// 1. Enumerate known valid routes
// ---------------------------------------------------------------------------

/** Static app routes derived from the Next.js app directory structure */
export function collectStaticRoutes(appDir: string, base = ''): string[] {
  const routes: string[] = []
  if (!fs.existsSync(appDir)) return routes

  for (const entry of fs.readdirSync(appDir)) {
    const fullPath = path.join(appDir, entry)
    const stat = fs.statSync(fullPath)

    if (stat.isDirectory()) {
      // Skip Next.js internals and dynamic segments (validated separately)
      if (entry.startsWith('_') || entry.startsWith('(')) {
        // Route groups — descend but don't add segment to path
        routes.push(...collectStaticRoutes(fullPath, base))
        continue
      }
      if (entry.startsWith('[')) {
        // Dynamic segment — skip for static list
        continue
      }
      const segment = `${base}/${entry}`
      if (fs.existsSync(path.join(fullPath, 'page.tsx')) || fs.existsSync(path.join(fullPath, 'page.mdx'))) {
        routes.push(segment)
      }
      routes.push(...collectStaticRoutes(fullPath, segment))
    }
  }
  return routes
}

/** Read slug arrays from data files */
export function readSlugsFromFile(filePath: string, pattern: RegExp): string[] {
  if (!fs.existsSync(filePath)) return []
  const content = fs.readFileSync(filePath, 'utf-8')
  const slugs: string[] = []
  let match: RegExpExecArray | null
  // eslint-disable-next-line no-cond-assign
  while ((match = pattern.exec(content)) !== null) {
    slugs.push(match[1])
  }
  return slugs
}

/** Build all /trade/${catA}/for/${catB} pairs (exhaustive, skips self-pairs) */
function buildTradePairRoutes(dataDir: string): string[] {
  const slugPattern = /slug:\s*['"]([^'"]+)['"]/g
  const catSlugs = readSlugsFromFile(path.join(dataDir, 'categories-data.ts'), slugPattern)
  const pairs: string[] = []
  for (const catA of catSlugs) {
    for (const catB of catSlugs) {
      if (catA !== catB) pairs.push(`/trade/${catA}/for/${catB}`)
    }
  }
  return pairs
}

/** Build all /skill-exchange/${city} routes */
function buildSkillExchangeRoutes(dataDir: string): string[] {
  const slugPattern = /slug:\s*['"]([^'"]+)['"]/g
  const citySlugs = readSlugsFromFile(path.join(dataDir, 'cities-data.ts'), slugPattern)
  return citySlugs.map((city) => `/skill-exchange/${city}`)
}

/** Build all /locations/${city}/${category} combos */
function buildLocationRoutes(dataDir: string): string[] {
  const slugPattern = /slug:\s*['"]([^'"]+)['"]/g
  const citySlugs = readSlugsFromFile(path.join(dataDir, 'cities-data.ts'), slugPattern)
  // Re-create pattern since exec is stateful
  const catPattern = /slug:\s*['"]([^'"]+)['"]/g
  const catSlugs = readSlugsFromFile(path.join(dataDir, 'categories-data.ts'), catPattern)
  const routes: string[] = []
  for (const city of citySlugs) {
    for (const cat of catSlugs) {
      routes.push(`/locations/${city}/${cat}`)
    }
  }
  return routes
}

export function buildKnownRoutes(rootDir = DEFAULT_ROOT): Set<string> {
  const appDir = path.join(rootDir, 'src', 'app')
  const known = new Set<string>()

  // Static app routes
  for (const r of collectStaticRoutes(appDir)) {
    known.add(r)
  }

  // Root route
  known.add('/')

  // Data-driven slugs
  const dataDir = path.join(rootDir, 'src', 'lib', 'data')
  const slugPattern = /slug:\s*['"]([^'"]+)['"]/g

  const glossarySlugs = readSlugsFromFile(path.join(dataDir, 'glossary-data.ts'), new RegExp(slugPattern.source, 'g'))
  for (const s of glossarySlugs) known.add(`/glossary/${s}`)

  const categorySlugs = readSlugsFromFile(path.join(dataDir, 'categories-data.ts'), new RegExp(slugPattern.source, 'g'))
  for (const s of categorySlugs) known.add(`/categories/${s}`)

  const industrySlugs = readSlugsFromFile(path.join(dataDir, 'industries-data.ts'), new RegExp(slugPattern.source, 'g'))
  for (const s of industrySlugs) known.add(`/industries/${s}`)

  const comparisonSlugs = readSlugsFromFile(path.join(dataDir, 'comparisons-data.ts'), new RegExp(slugPattern.source, 'g'))
  for (const s of comparisonSlugs) known.add(`/compare/${s}`)

  const scenarioSlugs = readSlugsFromFile(path.join(dataDir, 'scenarios-data.ts'), new RegExp(slugPattern.source, 'g'))
  for (const s of scenarioSlugs) known.add(`/how-to/${s}`)

  const featureSlugs = readSlugsFromFile(path.join(dataDir, 'features-data.ts'), new RegExp(slugPattern.source, 'g'))
  for (const s of featureSlugs) known.add(`/features/${s}`)

  // MDX article slugs (filename without .mdx)
  const contentDir = path.join(rootDir, 'content', 'articles')
  if (fs.existsSync(contentDir)) {
    collectMdxSlugs(contentDir).forEach((s) => known.add(`/resources/${s}`))
  }

  // Exhaustive dynamic routes — no more prefix bypass
  for (const r of buildTradePairRoutes(dataDir)) known.add(r)
  for (const r of buildSkillExchangeRoutes(dataDir)) known.add(r)
  for (const r of buildLocationRoutes(dataDir)) known.add(r)

  return known
}

export function collectMdxSlugs(dir: string): string[] {
  const slugs: string[] = []
  for (const entry of fs.readdirSync(dir)) {
    const fullPath = path.join(dir, entry)
    if (fs.statSync(fullPath).isDirectory()) {
      slugs.push(...collectMdxSlugs(fullPath))
    } else if (entry.endsWith('.mdx')) {
      slugs.push(entry.replace(/\.mdx$/, ''))
    }
  }
  return slugs
}

// ---------------------------------------------------------------------------
// 2. Extract links from files
// ---------------------------------------------------------------------------

export interface LinkOccurrence {
  file: string
  line: number
  href: string
}

/** Extract internal hrefs from MDX and TSX source files */
export function extractLinks(filePath: string): LinkOccurrence[] {
  const content = fs.readFileSync(filePath, 'utf-8')
  const lines = content.split('\n')
  const results: LinkOccurrence[] = []

  // Match Markdown links: [text](/path)
  const mdPattern = /\]\((\/?[a-zA-Z0-9/\-_#?=&.]+)\)/g
  // Match JSX href: href="/path" or href={`/path`}
  const jsxPattern = /href=["'`](\/[a-zA-Z0-9/\-_#?=&.]+)["'`]/g

  lines.forEach((line, i) => {
    let m: RegExpExecArray | null

    const mdp = new RegExp(mdPattern.source, 'g')
    // eslint-disable-next-line no-cond-assign
    while ((m = mdp.exec(line)) !== null) {
      const href = m[1]
      if (href.startsWith('/')) results.push({ file: filePath, line: i + 1, href })
    }

    const jsxp = new RegExp(jsxPattern.source, 'g')
    // eslint-disable-next-line no-cond-assign
    while ((m = jsxp.exec(line)) !== null) {
      results.push({ file: filePath, line: i + 1, href: m[1] })
    }
  })

  return results
}

export function walkDir(dir: string, extensions: string[]): string[] {
  if (!fs.existsSync(dir)) return []
  const files: string[] = []
  for (const entry of fs.readdirSync(dir)) {
    const fullPath = path.join(dir, entry)
    const stat = fs.statSync(fullPath)
    if (stat.isDirectory() && !entry.startsWith('.') && entry !== 'node_modules') {
      files.push(...walkDir(fullPath, extensions))
    } else if (extensions.some((ext) => entry.endsWith(ext))) {
      files.push(fullPath)
    }
  }
  return files
}

// ---------------------------------------------------------------------------
// 3. Validate relatedSlugs in MDX frontmatter
// ---------------------------------------------------------------------------

export interface RelatedSlugError {
  file: string
  slug: string
}

/** Parse relatedSlugs array from MDX YAML frontmatter.
 * Handles both inline array format: relatedSlugs: ["a", "b"]
 * and YAML list format:
 *   relatedSlugs:
 *     - "a"
 *     - "b"
 * Works with both LF and CRLF line endings.
 */
export function parseRelatedSlugsFromFrontmatter(content: string): string[] {
  // Normalize line endings so regex works on both LF and CRLF files
  const normalized = content.replace(/\r\n/g, '\n')
  const fmMatch = normalized.match(/^---\n([\s\S]*?)\n---/)
  if (!fmMatch) return []
  const yaml = fmMatch[1]

  // Try inline array format: relatedSlugs: ["a", "b"]
  const arrayMatch = yaml.match(/relatedSlugs:\s*\[([^\]]*)\]/)
  if (arrayMatch) {
    const items = arrayMatch[1]
    if (!items.trim()) return []
    return items
      .split(',')
      .map((s) => s.trim().replace(/^["']|["']$/g, ''))
      .filter(Boolean)
  }

  // Try YAML list format:
  //   relatedSlugs:
  //     - "slug-one"
  const listMatch = yaml.match(/relatedSlugs:\s*\n((?:\s+-\s+["']?[^"'\n]+["']?\n?)+)/)
  if (listMatch) {
    return listMatch[1]
      .split('\n')
      .map((line) => line.trim().replace(/^-\s+/, '').replace(/^["']|["']$/g, ''))
      .filter(Boolean)
  }

  return []
}

/** Validate all relatedSlugs in MDX files against known article slugs */
export function validateRelatedSlugs(rootDir = DEFAULT_ROOT, knownArticleSlugs: Set<string>): RelatedSlugError[] {
  const errors: RelatedSlugError[] = []
  const contentDir = path.join(rootDir, 'content', 'articles')
  if (!fs.existsSync(contentDir)) return errors
  const mdxFiles = walkDir(contentDir, ['.mdx'])
  for (const file of mdxFiles) {
    const content = fs.readFileSync(file, 'utf-8')
    const slugs = parseRelatedSlugsFromFrontmatter(content)
    for (const slug of slugs) {
      if (!knownArticleSlugs.has(slug)) {
        errors.push({ file, slug })
      }
    }
  }
  return errors
}

// ---------------------------------------------------------------------------
// 4. Validate links
// ---------------------------------------------------------------------------

export const SKIP_HREFS = new Set([
  '/api',
  '/sitemap.xml',
  '/llms.txt',
  '/llms-full.txt',
  '/robots.txt',
])

export function isValidHref(href: string, known: Set<string>): boolean {
  // Strip hash and query
  const clean = href.split('#')[0].split('?')[0].replace(/\/$/, '') || '/'

  if (SKIP_HREFS.has(clean)) return true

  // Exact match (includes exhaustive dynamic routes)
  if (known.has(clean)) return true

  // Root
  if (clean === '') return true

  return false
}

// ---------------------------------------------------------------------------
// 5. Coverage / orphan report
// ---------------------------------------------------------------------------

export interface CoverageReport {
  totalRoutes: number
  orphanRoutes: string[]
  totalLinks: number
}

export function buildCoverageReport(known: Set<string>, allLinks: LinkOccurrence[]): CoverageReport {
  // Build inbound link map: route -> inbound count
  const inboundCount = new Map<string, number>()
  for (const link of allLinks) {
    const clean = link.href.split('#')[0].split('?')[0].replace(/\/$/, '') || '/'
    inboundCount.set(clean, (inboundCount.get(clean) ?? 0) + 1)
  }

  // Find orphan routes (no inbound links) — check non-dynamic static pages only
  const orphanRoutes: string[] = []
  for (const route of known) {
    // Skip root and pSEO pages (too many to track individually)
    if (route === '/') continue
    if (route.includes('/trade/') || route.includes('/skill-exchange/') || route.includes('/locations/')) continue
    if (!inboundCount.has(route) || inboundCount.get(route)! === 0) {
      orphanRoutes.push(route)
    }
  }

  return {
    totalRoutes: known.size,
    orphanRoutes: orphanRoutes.sort(),
    totalLinks: allLinks.length,
  }
}

// ---------------------------------------------------------------------------
// 6. Main
// ---------------------------------------------------------------------------

function main() {
  const args = process.argv.slice(2)
  const reportOnly = args.includes('--report')

  console.log('Building known routes...')
  const known = buildKnownRoutes()
  console.log(`  Found ${known.size} known routes/slugs\n`)

  const sourceFiles = [
    ...walkDir(path.join(DEFAULT_ROOT, 'src'), ['.tsx', '.ts']),
    ...walkDir(path.join(DEFAULT_ROOT, 'content'), ['.mdx', '.md']),
    ...walkDir(path.join(DEFAULT_ROOT, 'public'), ['.txt']),
  ]

  console.log(`Scanning ${sourceFiles.length} source files...\n`)

  const allLinks: LinkOccurrence[] = []
  const broken: LinkOccurrence[] = []

  for (const file of sourceFiles) {
    const links = extractLinks(file)
    allLinks.push(...links)
    for (const link of links) {
      if (!isValidHref(link.href, known)) {
        broken.push(link)
      }
    }
  }

  // Validate relatedSlugs in MDX frontmatter
  const articleSlugs = new Set(collectMdxSlugs(path.join(DEFAULT_ROOT, 'content', 'articles')))
  const relatedSlugErrors = validateRelatedSlugs(DEFAULT_ROOT, articleSlugs)

  if (reportOnly) {
    const report = buildCoverageReport(known, allLinks)
    console.log('=== Coverage Report ===\n')
    console.log(`Total known routes: ${report.totalRoutes}`)
    console.log(`Total internal links found: ${report.totalLinks}`)
    console.log(`Orphan routes (0 inbound links): ${report.orphanRoutes.length}`)
    if (report.orphanRoutes.length > 0) {
      for (const r of report.orphanRoutes) console.log(`  ${r}`)
    }
    if (broken.length > 0) {
      console.log(`\nBroken links (FYI): ${broken.length}`)
    }
    if (relatedSlugErrors.length > 0) {
      console.log(`\nInvalid relatedSlugs: ${relatedSlugErrors.length}`)
    }
    console.log('\n✓ Report complete.')
    process.exit(0)
  }

  const hasErrors = broken.length > 0 || relatedSlugErrors.length > 0

  if (!hasErrors) {
    console.log('✓ No broken internal links found.\n')
    process.exit(0)
  }

  const relRoot = path.join(DEFAULT_ROOT, '..')

  if (broken.length > 0) {
    console.error(`✗ Found ${broken.length} broken internal link(s):\n`)
    for (const b of broken) {
      const rel = path.relative(relRoot, b.file).replace(/\\/g, '/')
      console.error(`  ${rel}:${b.line}  →  ${b.href}`)
    }
    console.error()
  }

  if (relatedSlugErrors.length > 0) {
    console.error(`✗ Found ${relatedSlugErrors.length} invalid relatedSlug(s) in MDX frontmatter:\n`)
    for (const e of relatedSlugErrors) {
      const rel = path.relative(relRoot, e.file).replace(/\\/g, '/')
      console.error(`  ${rel}  →  "${e.slug}"`)
    }
    console.error()
  }

  process.exit(1)
}

// Guard: only run main() when executed directly (not when imported in tests)
if (require.main === module) {
  main()
}
