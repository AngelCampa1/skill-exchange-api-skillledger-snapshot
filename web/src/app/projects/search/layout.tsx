import { Metadata } from 'next'
import { buildPublicPageMetadata } from '@/lib/seo'

export const metadata: Metadata = buildPublicPageMetadata(
  'Search Projects',
  'Search for collaboration projects by skills, budget, location, and more. Find the perfect project that matches your expertise on SkillLedger.',
  '/projects/search',
  ['search projects', 'find work', 'skill matching', 'project discovery', 'collaboration search']
)

export default function ProjectSearchLayout({
  children,
}: {
  children: React.ReactNode
}) {
  return <>{children}</>
}
