import { Metadata } from 'next'
import ForgotPassword from '../../components/ForgotPassword'
import { buildAuthPageMetadata } from '@/lib/seo'

export const metadata: Metadata = buildAuthPageMetadata(
  'Forgot Password',
  'Reset your SkillLedger account password by entering your email address.',
  '/forgot-password'
)

export default function ForgotPasswordPage() {
  return <ForgotPassword />
}