import { Metadata } from 'next'
import { Suspense } from 'react'
import ResetPassword from '../../components/ResetPassword'
import { buildAuthPageMetadata } from '@/lib/seo'

export const metadata: Metadata = buildAuthPageMetadata(
  'Reset Password',
  'Set a new password for your SkillLedger account.',
  '/reset-password'
)

function ResetPasswordFallback() {
  return (
    <div className="min-h-screen flex items-center justify-center bg-background">
      <div className="text-center space-md">
        <div className="loading-spinner mx-auto"></div>
        <p className="text-body text-muted-foreground">Loading password reset...</p>
      </div>
    </div>
  )
}

export default function ResetPasswordPage() {
  return (
    <Suspense fallback={<ResetPasswordFallback />}>
      <ResetPassword />
    </Suspense>
  )
}