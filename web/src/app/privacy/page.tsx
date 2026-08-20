import Link from 'next/link'
import { ArrowLeft } from 'lucide-react'
import { ThemeToggle } from '@/components/ThemeToggle'
import { buildPublicPageMetadata } from '@/lib/seo'

export const metadata = buildPublicPageMetadata(
  'Privacy Policy',
  'SkillLedger Privacy Policy — how we collect, use, and protect your personal data.',
  '/privacy'
)

const EFFECTIVE_DATE = 'May 28, 2026'

export default function PrivacyPage() {
  return (
    <div className="min-h-screen bg-background">
      <div className="container mx-auto px-4 py-16 max-w-4xl">
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

        <h1 className="text-3xl font-bold mb-8">Privacy Policy</h1>

        <div className="prose max-w-none space-y-8">
          <p className="text-muted-foreground">Effective date: {EFFECTIVE_DATE}</p>

          <section>
            <h2 className="text-xl font-semibold mb-4">1. Who we are</h2>
            <p className="text-muted-foreground">
              SkillLedger (&quot;SkillLedger,&quot; &quot;we,&quot; &quot;our,&quot; or &quot;us&quot;) is a
              professional collaboration platform and skill-exchange marketplace operated by{' '}
              <span className="font-medium">SkillLedger</span>.
              For the purposes of the EU General Data Protection Regulation (GDPR) and the UK GDPR,
              we are the data controller of the personal data described in this policy.
            </p>
            <p className="text-muted-foreground mt-4">
              Questions about this policy or our handling of your data can be sent to{' '}
              <a href="mailto:angel.campa@skillledger.app" className="text-primary hover:underline">
                angel.campa@skillledger.app
              </a>
              .
            </p>
          </section>

          <section>
            <h2 className="text-xl font-semibold mb-4">2. What we collect</h2>
            <p className="text-muted-foreground">
              We collect only the data needed to operate the platform. Specifically:
            </p>

            <h3 className="text-lg font-medium mt-6 mb-3">Account and registration data</h3>
            <ul className="list-disc pl-6 text-muted-foreground space-y-2 mt-2">
              <li>Email address (used as your login identifier)</li>
              <li>Password (stored only as a one-way hash — see Security)</li>
              <li>First name and last name (optional)</li>
              <li>Account status and email-verification state</li>
            </ul>

            <h3 className="text-lg font-medium mt-6 mb-3">Profile and marketplace data</h3>
            <ul className="list-disc pl-6 text-muted-foreground space-y-2 mt-2">
              <li>Profile details, skills, experience, and endorsements you add</li>
              <li>Projects you post or apply to, and applications and reviews you submit</li>
              <li>Reputation and badge data generated from your platform activity</li>
              <li>Messages and files you exchange with other users in workspaces</li>
            </ul>

            <h3 className="text-lg font-medium mt-6 mb-3">Credit and payment data</h3>
            <ul className="list-disc pl-6 text-muted-foreground space-y-2 mt-2">
              <li>Credit wallet balances and transfer history</li>
              <li>
                A payment-provider customer identifier (we use Stripe to process card payments; card
                numbers are handled by Stripe and are not stored on our servers)
              </li>
            </ul>

            <h3 className="text-lg font-medium mt-6 mb-3">Security and technical data</h3>
            <ul className="list-disc pl-6 text-muted-foreground space-y-2 mt-2">
              <li>
                IP address recorded at account creation and at sensitive actions, used for audit
                logging, rate limiting, and fraud prevention
              </li>
              <li>Security audit logs of account and authentication events</li>
              <li>
                Error and diagnostic data (e.g., stack traces and request metadata) captured when the
                application encounters a problem
              </li>
            </ul>

            <h3 className="text-lg font-medium mt-6 mb-3">
              Analytics data (where enabled and consented)
            </h3>
            <ul className="list-disc pl-6 text-muted-foreground space-y-2 mt-2">
              <li>
                Page views, navigation, clicks, device/browser type, and an anonymized IP /
                approximate location, collected through Google Analytics 4 and Microsoft Clarity where
                those services are enabled and you have given consent. See the Cookies section below.
              </li>
            </ul>
          </section>

          <section>
            <h2 className="text-xl font-semibold mb-4">3. How and why we use your data (legal bases)</h2>
            <p className="text-muted-foreground">
              Under the GDPR / UK GDPR we rely on the following lawful bases (Article 6):
            </p>
            <ul className="list-disc pl-6 text-muted-foreground space-y-2 mt-4">
              <li>
                <strong>Performance of a contract (Art. 6(1)(b)):</strong> creating and managing your
                account, providing the marketplace, processing credit transfers and payments, and
                enabling collaboration and messaging.
              </li>
              <li>
                <strong>Legitimate interests (Art. 6(1)(f)):</strong> securing the platform, preventing
                fraud and abuse, rate limiting, audit logging, and diagnosing errors. We balance these
                interests against your rights and freedoms.
              </li>
              <li>
                <strong>Legal obligation (Art. 6(1)(c)):</strong> retaining certain records and
                responding to lawful requests where required.
              </li>
              <li>
                <strong>Consent (Art. 6(1)(a)):</strong> analytics cookies (where analytics is enabled
                and consent is obtained) and any optional marketing communications. You can withdraw
                consent at any time without affecting prior processing.
              </li>
            </ul>
          </section>

          <section>
            <h2 className="text-xl font-semibold mb-4">4. Sub-processors and sharing</h2>
            <p className="text-muted-foreground">
              We do not sell your personal information. We share data with other users where you choose
              to (e.g., your public profile, project posts, and workspace messages), and with the
              following service providers (sub-processors) that process data on our behalf:
            </p>
            <ul className="list-disc pl-6 text-muted-foreground space-y-2 mt-4">
              <li>
                <strong>Railway</strong> — application hosting infrastructure
              </li>
              <li>
                <strong>Neon</strong> — managed PostgreSQL database storing your account
                and platform data
              </li>
              <li>
                <strong>Stripe</strong> — payment processing for credit purchases and subscriptions
              </li>
              <li>
                <strong>Resend</strong> — transactional email delivery (e.g., verification emails)
              </li>
              <li>
                <strong>Sentry</strong> — error and exception monitoring
              </li>
              <li>
                <strong>Microsoft Azure</strong> — Application Insights (telemetry) and Azure AI Content
                Safety (content moderation), where enabled
              </li>
              <li>
                <strong>Google Analytics 4</strong> and <strong>Microsoft Clarity</strong> — product
                analytics, loaded only with your consent
              </li>
            </ul>
            <p className="text-muted-foreground mt-4">
              We may also disclose data to professional advisors or to law enforcement and regulators
              where required by law or to protect our rights and users.
            </p>
          </section>

          <section>
            <h2 className="text-xl font-semibold mb-4">5. International transfers</h2>
            <p className="text-muted-foreground">
              Some of our sub-processors (including those listed above) may process data outside your
              country, including in the United States. Where personal data is transferred out of the
              EEA or the UK, we rely on appropriate safeguards such as the European Commission&apos;s
              Standard Contractual Clauses (and the UK International Data Transfer Addendum) or an
              adequacy decision. Contact{' '}
              <a href="mailto:angel.campa@skillledger.app" className="text-primary hover:underline">
                angel.campa@skillledger.app
              </a>{' '}
              for details of the safeguards that apply.
            </p>
          </section>

          <section>
            <h2 className="text-xl font-semibold mb-4">6. Data retention</h2>
            <p className="text-muted-foreground">
              We keep personal data for as long as your account is active and for as long as needed for
              the purposes described in this policy, then delete or anonymize it.
              Where consent-based analytics is used, Google Analytics 4 data is retained for up to 14
              months and Microsoft Clarity recordings for up to 60 days. Certain records may be kept
              longer where a legal obligation requires it.
            </p>
          </section>

          <section>
            <h2 className="text-xl font-semibold mb-4">7. Security</h2>
            <p className="text-muted-foreground">
              We apply the following technical measures, which reflect what is implemented in the
              platform:
            </p>
            <ul className="list-disc pl-6 text-muted-foreground space-y-2 mt-4">
              <li>
                <strong>Password hashing:</strong> passwords are never stored in plain text. They are
                hashed using ASP.NET Core Identity&apos;s default password hasher (PBKDF2 with
                HMAC-SHA256 and a per-user salt).
              </li>
              <li>
                <strong>Password strength requirements:</strong> a minimum of 12 characters with
                uppercase, lowercase, number, and special-character requirements.
              </li>
              <li>
                <strong>Field-level encryption:</strong> credit-wallet balance fields are encrypted at
                the application layer using AES-256-GCM authenticated encryption.
              </li>
              <li>
                <strong>Encryption in transit:</strong> connections to the platform use HTTPS/TLS.
              </li>
              <li>
                <strong>Abuse controls:</strong> IP-based rate limiting on sensitive endpoints (for
                example, registration and verification) and security audit logging.
              </li>
              <li>
                <strong>Protective design:</strong> anti-CSRF tokens on state-changing requests,
                parameterized database queries, and email-enumeration protections on authentication
                flows.
              </li>
            </ul>
            <p className="text-muted-foreground mt-4">
              No method of transmission or storage is completely secure, and we cannot guarantee
              absolute security.
            </p>
          </section>

          <section>
            <h2 className="text-xl font-semibold mb-4">8. Your rights</h2>
            <h3 className="text-lg font-medium mt-2 mb-3">EU / UK (GDPR)</h3>
            <p className="text-muted-foreground">
              Subject to applicable law, you have the right to access, rectify, erase, restrict, or
              object to processing of your personal data; the right to data portability; and the right
              to withdraw consent. You also have the right to lodge a complaint with your local
              supervisory authority (in the UK, the Information Commissioner&apos;s Office; in the EEA,
              your national data protection authority).
            </p>

            <h3 className="text-lg font-medium mt-6 mb-3">California (CCPA / CPRA)</h3>
            <p className="text-muted-foreground">
              If you are a California resident, you have the right to know what personal information we
              collect and how we use and share it, the right to correct inaccurate information, the
              right to delete your information, and the right to limit certain uses. We do not sell or
              share your personal information for cross-context behavioral advertising, and we will not
              discriminate against you for exercising your rights.
            </p>
            <p className="text-muted-foreground mt-4">
              To exercise any of these rights, contact{' '}
              <a href="mailto:angel.campa@skillledger.app" className="text-primary hover:underline">
                angel.campa@skillledger.app
              </a>
              . We will verify your request and respond within the timeframes required by law.
            </p>
          </section>

          <section>
            <h2 className="text-xl font-semibold mb-4">9. Children</h2>
            <p className="text-muted-foreground">
              SkillLedger is not intended for anyone under 18 years of age. We do not knowingly collect
              personal data from children. If we learn that we have collected such data, we will delete
              it.
            </p>
          </section>

          <section>
            <h2 className="text-xl font-semibold mb-4">10. Cookies and analytics</h2>
            <p className="text-muted-foreground">
              We use cookies and similar technologies that are strictly necessary for the platform to
              function (for example, to keep you signed in). Where enabled in production, we also use
              Google Analytics 4 (configured with IP anonymization) and Microsoft Clarity for product
              analytics; those services are loaded only with your consent, obtained through whatever
              consent mechanism is implemented at the time. You can withdraw consent at any time. We
              honor the Do Not Track browser signal. This policy will be updated to describe the
              specific consent mechanism before any analytics service goes live.
            </p>
          </section>

          <section>
            <h2 className="text-xl font-semibold mb-4">11. Changes to this policy</h2>
            <p className="text-muted-foreground">
              We may update this Privacy Policy from time to time. We will post the updated version on
              this page and revise the effective date above. Material changes will be communicated
              through the platform or by email where appropriate.
            </p>
          </section>

          <section>
            <h2 className="text-xl font-semibold mb-4">12. Contact</h2>
            <ul className="list-none text-muted-foreground space-y-2 mt-2">
              <li>
                Privacy:{' '}
                <a href="mailto:angel.campa@skillledger.app" className="text-primary hover:underline">
                  angel.campa@skillledger.app
                </a>
              </li>
              <li>
                General support:{' '}
                <a href="mailto:angel.campa@skillledger.app" className="text-primary hover:underline">
                  angel.campa@skillledger.app
                </a>
              </li>
            </ul>
          </section>
        </div>
      </div>
    </div>
  )
}
