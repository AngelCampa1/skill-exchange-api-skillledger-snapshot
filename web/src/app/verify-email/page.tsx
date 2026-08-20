import { Metadata } from 'next'
import VerifyEmail from '@/components/VerifyEmail'
import { buildAuthPageMetadata } from '@/lib/seo'

export const metadata: Metadata = buildAuthPageMetadata(
  'Verify Email',
  'Verify your email address to access all features of SkillLedger.',
  '/verify-email'
)

export default function VerifyEmailPage() {
  return <VerifyEmail />
}
