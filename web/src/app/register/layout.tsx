import { Metadata } from 'next'
import { buildAuthPageMetadata } from '@/lib/seo'

export const metadata: Metadata = buildAuthPageMetadata(
  'Create Account',
  'Join SkillLedger and start collaborating with professionals. Start your 30-day free trial to exchange services and build your reputation.',
  '/register'
)

export default function RegisterLayout({
  children,
}: {
  children: React.ReactNode
}) {
  return <>{children}</>
}
