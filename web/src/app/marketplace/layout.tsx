import { Metadata } from 'next'
import { buildPublicPageMetadata } from '@/lib/seo'

export const metadata: Metadata = buildPublicPageMetadata(
  'Project Marketplace',
  'Browse and discover exciting collaboration projects that match your skills. Find freelance opportunities, exchange services, and connect with professionals on SkillLedger.',
  '/marketplace',
  ['project marketplace', 'find projects', 'freelance opportunities', 'collaboration projects', 'skill-based projects']
)

export default function MarketplaceLayout({
  children,
}: {
  children: React.ReactNode
}) {
  return <>{children}</>
}
