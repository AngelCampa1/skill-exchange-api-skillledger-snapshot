'use client'

import React from 'react'
import { useRouter } from 'next/navigation'
import { Mail, Info, Rocket, Briefcase, Star, Handshake, AlertTriangle } from 'lucide-react'

interface RegistrationSuccessProps {
  email: string
  onResendEmail?: () => void
}

export default function RegistrationSuccess({ email, onResendEmail }: RegistrationSuccessProps) {
  const router = useRouter()

  return (
    <div className="text-center">
      <div className="mx-auto flex items-center justify-center h-16 w-16 rounded-full bg-success/10 mb-6">
        <svg className="h-8 w-8 text-success" fill="none" viewBox="0 0 24 24" stroke="currentColor">
          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M3 8l7.89 4.26a2 2 0 002.22 0L21 8M5 19h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v10a2 2 0 002 2z" />
        </svg>
      </div>

      <h2 className="text-heading text-foreground mb-2">
        Account Created Successfully!
      </h2>

      <p className="text-muted-foreground mb-4">
        Welcome to SkillLedger! We've sent a verification email to:
      </p>

      <p className="font-medium text-foreground bg-muted px-4 py-2 rounded-lg mb-6 break-all">
        {email}
      </p>
      
      <div className="card-premium bg-muted p-6 space-md">
        <div className="flex items-center text-body text-foreground mb-4">
          <Info className="w-5 h-5 mr-3" />
          <strong>Next Steps</strong>
        </div>
        <ol className="text-caption text-muted-foreground space-y-2 list-decimal list-inside">
          <li>Check your email inbox (and spam/junk folder)</li>
          <li>Click the verification link in the email</li>
          <li>Complete your email verification</li>
          <li>Start collaborating on SkillLedger!</li>
        </ol>
      </div>
      
      <div className="bg-warning/10 border border-warning/20 rounded-xl p-4 mb-6">
        <div className="flex items-start space-x-3">
          <div className="flex-shrink-0">
            <AlertTriangle className="h-5 w-5 text-warning" />
          </div>
          <div>
            <p className="text-body text-warning">
              <strong>Important:</strong> The verification link expires in 24 hours.
              If you don't receive the email within a few minutes, check your spam folder or request a new one.
            </p>
          </div>
        </div>
      </div>
      
      <div className="space-y-3">
        <button
          onClick={onResendEmail}
          className="btn-primary w-full mb-3"
        >
          <Mail className="w-4 h-4 mr-2" />
          Resend Verification Email
        </button>
        
        <button
          onClick={() => router.push('/login')}
          className="btn-secondary w-full"
        >
          I'll verify later - Go to Login
        </button>
      </div>
      
      <div className="mt-8 pt-6 border-t border-border">
        <h3 className="text-sm font-medium text-foreground mb-3">What can you do on SkillLedger?</h3>
        <div className="grid grid-cols-1 gap-4 text-body text-muted-foreground">
          <div className="flex items-start space-x-3">
            <div className="p-2 bg-muted rounded-lg">
              <Rocket className="w-4 h-4 text-primary" />
            </div>
            <span>Post projects and find skilled professionals</span>
          </div>
          <div className="flex items-start space-x-3">
            <div className="p-2 bg-muted rounded-lg">
              <Briefcase className="w-4 h-4 text-primary" />
            </div>
            <span>Offer your skills and earn SkillCredits</span>
          </div>
          <div className="flex items-start space-x-3">
            <div className="p-2 bg-muted rounded-lg">
              <Star className="w-4 h-4 text-primary" />
            </div>
            <span>Build your reputation through quality work</span>
          </div>
          <div className="flex items-start space-x-3">
            <div className="p-2 bg-muted rounded-lg">
              <Handshake className="w-4 h-4 text-primary" />
            </div>
            <span>Collaborate in secure workspaces</span>
          </div>
        </div>
      </div>
    </div>
  )
}