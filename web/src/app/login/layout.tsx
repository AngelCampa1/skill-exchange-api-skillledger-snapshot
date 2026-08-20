import { Metadata } from 'next'
import { buildAuthPageMetadata } from '@/lib/seo'

export const metadata: Metadata = buildAuthPageMetadata(
  'Sign In',
  'Sign in to your SkillLedger account to access your dashboard, projects, and professional collaboration tools.',
  '/login'
)

export default function LoginLayout({
  children,
}: {
  children: React.ReactNode
}) {
  return <>{children}</>
}
