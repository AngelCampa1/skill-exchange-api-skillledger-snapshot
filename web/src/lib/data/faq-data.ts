// ============================================================================
// FAQ Data -- Platform-level FAQs for /faq page
// ============================================================================

export interface FAQ {
  question: string
  answer: string
}

export interface FAQSection {
  id: string
  title: string
  faqs: FAQ[]
}

export const faqSections: FAQSection[] = [
  {
    id: 'getting-started',
    title: 'Getting Started',
    faqs: [
      {
        question: 'What is SkillLedger?',
        answer:
          'SkillLedger is a professional collaboration platform where freelancers, agencies, and businesses exchange services using a credit-based system instead of cash. You earn credits by delivering work and spend them to hire other professionals on the platform.',
      },
      {
        question: 'How do I create an account?',
        answer:
          'Visit the registration page, enter your email address, and set a password. After verifying your email, you can build your professional profile, set your credit rate, and start browsing the marketplace.',
      },
      {
        question: 'Do I need to pay to join?',
        answer:
          'Every new account starts with a 30-day free trial that includes full access to all features in your chosen plan. A credit card is required to activate the trial, but you will not be charged until the trial ends. Cancel anytime before then and you owe nothing.',
      },
      {
        question: 'What kinds of services can I exchange?',
        answer:
          'SkillLedger supports 19 professional skill categories including web development, graphic design, marketing, legal services, accounting, writing, photography, video production, consulting, and more. Any professional service that can be delivered remotely or locally is eligible.',
      },
      {
        question: 'How do I set up my profile?',
        answer:
          'After registration, add your professional skills, upload portfolio samples, set your hourly credit rate, and write a brief description of the services you offer. A complete profile with verified work samples increases your visibility in the marketplace.',
      },
    ],
  },
  {
    id: 'credits-payments',
    title: 'Credits & Payments',
    faqs: [
      {
        question: 'How do credits work?',
        answer:
          'Credits are the unit of value on SkillLedger. Each professional sets their own credit rate based on their market value. When you complete work for another member, you earn credits. You spend credits to hire other professionals. Credits are held in a secure wallet and protected by escrow during active exchanges.',
      },
      {
        question: 'How do I earn credits?',
        answer:
          'Complete projects for other platform members. When you deliver work and the client approves it, credits are released from escrow into your wallet. Your credit rate determines how many credits you earn per hour of work.',
      },
      {
        question: 'Can I buy credits with cash?',
        answer:
          'SkillLedger is designed around earned credits from professional work. The credit system ensures that value circulates through real service delivery rather than cash purchases, keeping the marketplace balanced and fair.',
      },
      {
        question: 'How do I set my credit rate?',
        answer:
          'Your credit rate should reflect your professional market value. Research comparable services on the platform, factor in your experience level and portfolio strength, and adjust over time as your reputation score grows. Verified professionals with strong track records can set higher rates.',
      },
      {
        question: 'What happens to my credits if I cancel my account?',
        answer:
          'Earned credits remain in your wallet. If you choose to deactivate your account, any active exchanges must be completed or resolved first. Credits in escrow follow the standard release or dispute process before account closure.',
      },
      {
        question: 'Is there a transaction fee?',
        answer:
          'SkillLedger charges no per-transaction fees on any plan. All plans include escrow protection. Subscription tiers (Professional $19/mo, Business $49/mo, Enterprise $99/mo) unlock higher limits and advanced features. A 30-day free trial is included with every paid plan — credit card required to start.',
      },
    ],
  },
  {
    id: 'exchanges-projects',
    title: 'Exchanges & Projects',
    faqs: [
      {
        question: 'How do I find professionals to work with?',
        answer:
          'Browse the marketplace by skill category, search for specific services, or use the matching algorithm to find professionals whose skills complement yours. Filter results by reputation score, credit rate, availability, and location.',
      },
      {
        question: 'How does the exchange process work?',
        answer:
          'Both parties agree to a trade with defined milestones, deliverables, and credit values. Credits are moved into escrow before work begins. The delivering party completes each milestone and submits it for review. Once the receiving party approves, credits are released. If a dispute arises, the platform provides structured mediation.',
      },
      {
        question: 'What are milestones?',
        answer:
          'Milestones are distinct phases of a project, each with specific deliverables, a credit value, and a deadline. Breaking projects into milestones protects both parties: the delivering party gets paid incrementally, and the receiving party can review work before committing to the next phase.',
      },
      {
        question: 'Can I work with multiple people at once?',
        answer:
          'Yes. Active exchange limits depend on your plan tier. All paid plans include a 30-day free trial so you can evaluate the platform before committing. See the pricing page for per-tier limits.',
      },
      {
        question: 'What if I need to cancel an exchange mid-project?',
        answer:
          'If both parties agree to cancel, credits in escrow for incomplete milestones are returned to the receiving party. Completed and approved milestones are not reversed. If parties disagree on cancellation terms, the dispute resolution process applies.',
      },
    ],
  },
  {
    id: 'trust-safety',
    title: 'Trust & Safety',
    faqs: [
      {
        question: 'How does escrow protect me?',
        answer:
          'When an exchange begins, the receiving party\'s credits are moved into escrow, a neutral holding area controlled by the platform. Credits cannot be spent by either party during the exchange. After the delivering party submits work and the receiving party approves, credits release automatically. If the receiving party does not respond, credits auto-release after the review window.',
      },
      {
        question: 'What happens if my exchange partner does not deliver?',
        answer:
          'If the delivering party fails to submit work by the milestone deadline, you can file a dispute. The platform reviews the agreement, communication history, and any partial deliverables to determine a fair outcome. Escrowed credits are returned to you if non-delivery is confirmed.',
      },
      {
        question: 'How does the reputation system work?',
        answer:
          'Every completed exchange generates a review that becomes part of your permanent reputation profile. Timely deliveries, clean approvals, and positive feedback increase your reputation score. Disputes, late submissions, and negative feedback are also recorded. Your reputation score is visible to all potential exchange partners.',
      },
      {
        question: 'How do I verify a potential partner before trading?',
        answer:
          'Check their reputation score, read reviews from previous exchange partners, review their portfolio samples, and verify their profile completeness. Professionals with higher trust levels have completed more exchanges successfully. The platform displays badges for verified skills and consistent performance.',
      },
      {
        question: 'What is the dispute resolution process?',
        answer:
          'Both parties submit their perspective along with supporting evidence (deliverables, messages, agreement terms). The platform reviews the case and issues a binding decision. Reputation consequences apply to the party found at fault. This process replaces the need for external arbitration in most cases.',
      },
    ],
  },
  {
    id: 'tax-legal',
    title: 'Tax & Legal',
    faqs: [
      {
        question: 'Are barter exchanges taxable?',
        answer:
          'Yes. In the United States, the IRS considers barter income taxable at fair market value under IRC Section 1.6045-1. You must report the fair market value of services received through barter on your tax return. SkillLedger provides transaction records to help with reporting.',
      },
      {
        question: 'What is Form 1099-B?',
        answer:
          'Form 1099-B is an IRS form that barter exchanges use to report the fair market value of exchanges conducted through their platform. If your barter income exceeds $600 in a calendar year, you may receive a 1099-B. Even if you do not receive one, you are still required to report barter income.',
      },
      {
        question: 'How do I calculate fair market value for my services?',
        answer:
          'Fair market value (FMV) is what a willing buyer would pay a willing seller for the same service in an arm\'s-length transaction. Use your standard hourly rate, compare with market rates for similar services, and document your valuation method. SkillLedger credit rates help establish FMV by creating a standardized pricing framework.',
      },
      {
        question: 'Do I need a contract for barter exchanges?',
        answer:
          'SkillLedger automatically generates a structured agreement for every exchange that records deliverables, timelines, credit values, and dispute resolution terms. This built-in documentation satisfies the legal requirement for written agreements and provides evidence for tax reporting purposes.',
      },
    ],
  },
  {
    id: 'pricing-subscription',
    title: 'Pricing & Subscriptions',
    faqs: [
      {
        question: 'What is included in the trial?',
        answer:
          'Every new account starts with a 30-day free trial with full access to your chosen plan. A credit card is required to start the trial. You will not be charged until the trial ends. Cancel before then and you owe nothing.',
      },
      {
        question: 'What plans are available?',
        answer:
          'SkillLedger offers three paid plans: Professional at $19/month, Business at $49/month, and Enterprise at $99/month. All plans include a 30-day free trial. Annual billing saves 20%.',
      },
      {
        question: 'Can I cancel Premium anytime?',
        answer:
          'Yes. Cancel from your account settings at any time. Your Premium features remain active through the end of your current billing period. There are no cancellation fees or long-term contracts.',
      },
    ],
  },
]

// Flat list of all FAQs for schema generation
export function getAllFAQs(): FAQ[] {
  return faqSections.flatMap((section) => section.faqs)
}
