import Link from'next/link'
import { ArrowLeft } from'lucide-react'
import { ThemeToggle } from'@/components/ThemeToggle'
import { buildPublicPageMetadata } from'@/lib/seo'

export const metadata = buildPublicPageMetadata('Terms of Service','SkillLedger Terms of Service and User Agreement — the rules governing use of the platform.','/terms'
)

export default function TermsPage() {
  return (
    <div className="min-h-screen bg-background">
      <div className="container mx-auto px-4 py-16 max-w-4xl">
        {/* BUG-004 FIX: Add navigation header with theme toggle */}
        <div className="flex items-center justify-between mb-8">
          <Link
            href="/"
            className="inline-flex items-center text-sm text-muted-foreground hover:text-foreground transition-colors"
          >
            <ArrowLeft className="w-4 h-4 mr-2" />
            Back to Home
          </Link>
          <ThemeToggle />
        </div>

        <h1 className="text-3xl font-bold mb-8">Terms of Service</h1>

        <div className="prose  max-w-none space-y-8">
          <p className="text-muted-foreground">
            Last updated: {new Date().toLocaleDateString('en-US', { month:'long', day:'numeric', year:'numeric' })}
          </p>

          <section>
            <h2 className="text-xl font-semibold mb-4">1. Acceptance of Terms</h2>
            <p className="text-muted-foreground">
              By accessing or using SkillLedger (&quot;the Platform&quot;), you agree to be bound by these Terms of Service
              (&quot;Terms&quot;). If you do not agree to these Terms, you may not access or use the Platform.
            </p>
          </section>

          <section>
            <h2 className="text-xl font-semibold mb-4">2. Description of Service</h2>
            <p className="text-muted-foreground">
              SkillLedger is a professional collaboration platform and barter exchange that enables users to:
            </p>
            <ul className="list-disc pl-6 text-muted-foreground space-y-2 mt-4">
              <li>Create professional profiles showcasing skills and experience</li>
              <li>Post and discover projects requiring specific skills</li>
              <li>Exchange services through a credit-based system</li>
              <li>Build professional networks and collaborate on projects</li>
            </ul>
          </section>

          <section>
            <h2 className="text-xl font-semibold mb-4">3. User Accounts</h2>
            <p className="text-muted-foreground">
              You are responsible for maintaining the confidentiality of your account credentials and for all
              activities that occur under your account. You must:
            </p>
            <ul className="list-disc pl-6 text-muted-foreground space-y-2 mt-4">
              <li>Provide accurate and complete registration information</li>
              <li>Keep your password secure and confidential</li>
              <li>Notify us immediately of any unauthorized access</li>
              <li>Be at least 18 years old to create an account</li>
            </ul>
          </section>

          <section>
            <h2 className="text-xl font-semibold mb-4">4. User Conduct</h2>
            <p className="text-muted-foreground">
              When using SkillLedger, you agree not to:
            </p>
            <ul className="list-disc pl-6 text-muted-foreground space-y-2 mt-4">
              <li>Violate any applicable laws or regulations</li>
              <li>Infringe on intellectual property rights</li>
              <li>Post false, misleading, or fraudulent content</li>
              <li>Harass, abuse, or harm other users</li>
              <li>Attempt to gain unauthorized access to the Platform</li>
              <li>Use the Platform for illegal or unauthorized purposes</li>
            </ul>
          </section>

          <section>
            <h2 className="text-xl font-semibold mb-4">5. Credits and Transactions</h2>
            <p className="text-muted-foreground">
              SkillLedger uses a credit-based system for service exchanges. By using this system, you acknowledge that:
            </p>
            <ul className="list-disc pl-6 text-muted-foreground space-y-2 mt-4">
              <li>Credits have no cash value and cannot be exchanged for currency</li>
              <li>All transactions are final unless both parties agree to a reversal</li>
              <li>You are responsible for reporting any tax obligations</li>
              <li>SkillLedger is not responsible for disputes between users</li>
            </ul>
          </section>

          <section>
            <h2 className="text-xl font-semibold mb-4">6. Intellectual Property</h2>
            <p className="text-muted-foreground">
              You retain ownership of content you create and share on SkillLedger. By posting content, you grant
              SkillLedger a non-exclusive, worldwide license to display and distribute your content on the Platform.
            </p>
          </section>

          <section>
            <h2 className="text-xl font-semibold mb-4">7. Limitation of Liability</h2>
            <p className="text-muted-foreground">
              SkillLedger is provided &quot;as is&quot; without warranties of any kind. We are not liable for any indirect,
              incidental, special, consequential, or punitive damages arising from your use of the Platform.
            </p>
          </section>

          <section>
            <h2 className="text-xl font-semibold mb-4">8. Termination</h2>
            <p className="text-muted-foreground">
              We reserve the right to suspend or terminate your account at any time for violations of these Terms
              or for any other reason at our discretion. You may also delete your account at any time.
            </p>
          </section>

          <section>
            <h2 className="text-xl font-semibold mb-4">9. Changes to Terms</h2>
            <p className="text-muted-foreground">
              We may modify these Terms at any time. Continued use of the Platform after changes constitutes
              acceptance of the modified Terms.
            </p>
          </section>

          <section>
            <h2 className="text-xl font-semibold mb-4">10. Contact Us</h2>
            <p className="text-muted-foreground">
              If you have questions about these Terms, please contact us at{''}
              <a href="mailto:angel.campa@skillledger.app" className="text-primary hover:underline">
                angel.campa@skillledger.app
              </a>
            </p>
          </section>
        </div>
      </div>
    </div>
  )
}
