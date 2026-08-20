export interface FeatureData {
  slug: string
  name: string
  tagline: string
  description: string
  longDescription: string
  icon: string
  benefits: string[]
  targetKeywords: string[]
  relatedCategories: string[]
  faqs: Array<{ question: string; answer: string }>
}

export const featuresData: FeatureData[] = [
  {
    slug: 'credit-wallet-exchange',
    name: 'Credit Wallet & Exchange',
    tagline: 'Trade skills without spending cash. Earn and spend credits across the platform.',
    description: 'SkillLedger credit wallet lets professionals earn credits for services rendered and spend them on services needed, no cash required.',
    longDescription: `SkillLedger replaces cash payments with a credit-based exchange system built for professional services. When you complete work for another member, you earn credits equal to the fair market value of your contribution. Those credits can then be spent on any service offered by any other member on the platform.

Unlike time-banking systems that treat every hour as equal, SkillLedger credits reflect the actual market value of specialized skills. A senior software architect and a junior copywriter both receive credits proportional to what their work would cost on the open market. This means you can exchange a few hours of high-value consulting for dozens of hours of administrative support, or vice versa, without anyone feeling shortchanged.

Every credit transaction is logged with a full audit trail. The platform tracks fair market values automatically and generates tax-ready documentation at year end, so you never have to guess what your exchanges were worth when filing your 1099-B.`,
    icon: 'Wallet',
    benefits: [
      'Earn credits for every completed project based on fair market value',
      'Spend credits on any service from any member, no direct swaps needed',
      'Automatic FMV tracking eliminates manual valuation guesswork',
      '1099-B-ready reports generated automatically at year end',
      'Full transaction history with audit trail for every credit movement',
      'Credits reflect skill market rates, not flat hourly equivalence',
    ],
    targetKeywords: [
      'freelancer credit exchange',
      'skill barter credit system',
      'professional service credits',
      'credit-based barter platform',
    ],
    relatedCategories: ['web-development', 'marketing', 'finance'],
    faqs: [
      {
        question: 'How are credit values determined?',
        answer: 'Credits are pegged to fair market value (FMV). When you list a service, you set a credit rate based on what that service would cost in cash. The platform provides market rate benchmarks from comparable services to help you price accurately.',
      },
      {
        question: 'Can I convert credits to cash?',
        answer: 'Credits are designed for service exchange within the platform, not cash withdrawal. You can purchase additional credits with cash if you need services but have not yet earned enough through your own work.',
      },
      {
        question: 'What happens to unused credits?',
        answer: 'Credits do not expire. Your balance carries forward indefinitely. There are no maintenance fees or balance decay mechanisms. Credits you earn today are available whenever you need them.',
      },
      {
        question: 'How does SkillLedger handle tax reporting for credits?',
        answer: 'The IRS treats barter exchanges as taxable income at fair market value. SkillLedger tracks every transaction automatically and generates 1099-B-compatible reports at year end, making tax filing straightforward.',
      },
    ],
  },
  {
    slug: 'project-escrow-protection',
    name: 'Project Escrow Protection',
    tagline: 'Credits held in escrow until both parties approve. No more trust gambles.',
    description: 'SkillLedger escrow locks credits until deliverables are approved, protecting both parties in every professional skill exchange.',
    longDescription: `Trust is the biggest barrier to professional barter. Without a neutral third party holding the value, one side always risks doing work and never getting paid, or paying upfront and never receiving the deliverable. SkillLedger's escrow system removes that risk entirely.

When a project kicks off, the client's credits are moved into a secure escrow account that neither party can access. The service provider sees the funded escrow as proof that the client is committed and has the credits to pay. Once the provider delivers and the client approves, the escrowed credits are released to the provider's wallet instantly.

If there is a dispute, SkillLedger's mediation team reviews the project scope, communications, and deliverables before deciding how to allocate the escrowed credits. Neither party can simply walk away with the other's value. The escrow holds until resolution.`,
    icon: 'ShieldCheck',
    benefits: [
      'Credits locked in escrow before work begins, providing proof of commitment',
      'Automatic release on client approval, no manual invoicing',
      'Neutral mediation for disputes with documented resolution process',
      'Milestone-based escrow for large projects, releasing credits per deliverable',
      'Full transparency: both parties see escrow status in real time',
    ],
    targetKeywords: [
      'freelancer escrow service',
      'barter escrow protection',
      'skill exchange escrow',
      'professional service escrow',
    ],
    relatedCategories: ['web-development', 'design', 'consulting'],
    faqs: [
      {
        question: 'How does escrow work for multi-milestone projects?',
        answer: 'Large projects can be split into milestones. Credits for each milestone are escrowed separately and released as each deliverable is approved. This protects both parties throughout the project rather than concentrating all risk at the end.',
      },
      {
        question: 'What happens if the client never approves the deliverable?',
        answer: 'If a client does not respond within the configured review period (default 14 days), the provider can request mediation. SkillLedger reviewers examine the deliverables against the agreed scope and release credits accordingly.',
      },
      {
        question: 'Is there a fee for using escrow?',
        answer: 'Escrow is included on Premium plans at no additional per-transaction fee. Free-tier users can upgrade to access escrow protection on any project.',
      },
      {
        question: 'Can I cancel a project after escrow is funded?',
        answer: 'Yes. If work has not started, the client can cancel and receive a full credit refund from escrow. If work is in progress, cancellation triggers the dispute resolution process to fairly compensate the provider for completed work.',
      },
    ],
  },
  {
    slug: 'reputation-badge-system',
    name: 'Reputation & Badge System',
    tagline: 'Verified credentials and earned badges that prove your professional track record.',
    description: 'SkillLedger reputation system combines verified credentials, peer reviews, and earned badges to build trust between professionals.',
    longDescription: `On platforms without reputation systems, every new connection is a guess. SkillLedger's multi-layered reputation system gives you concrete evidence of a professional's track record before you commit to an exchange.

The foundation is credential verification. Members can submit professional certifications, portfolio links, and work samples that SkillLedger's team reviews and marks as verified. Verified credentials appear on your profile with a trust badge, signaling to potential collaborators that your claimed expertise has been confirmed.

On top of verification, the badge system rewards consistent performance. Badges are earned through completed exchanges, positive reviews, on-time delivery streaks, and community contributions. A member with a "Top Rated" badge and 50 completed exchanges represents a very different risk profile than a brand-new account, and the reputation system makes that difference visible at a glance.`,
    icon: 'Award',
    benefits: [
      'Credential verification by SkillLedger review team',
      'Peer review system with structured feedback after every exchange',
      'Achievement badges earned through consistent performance',
      'Reputation score visible on profile and in search results',
      'On-time delivery tracking builds trust over time',
      'Portfolio showcase with verified work samples',
    ],
    targetKeywords: [
      'freelancer reputation system',
      'professional verification badges',
      'skill exchange trust system',
      'verified professional credentials',
    ],
    relatedCategories: ['consulting', 'design', 'marketing'],
    faqs: [
      {
        question: 'How is the reputation score calculated?',
        answer: 'The score combines peer review ratings (weighted by recency), completed exchange count, on-time delivery percentage, dispute history, and credential verification status. The algorithm prioritizes recent activity so your score reflects current performance.',
      },
      {
        question: 'Can negative reviews be removed?',
        answer: 'Reviews that violate community guidelines (spam, harassment, factual inaccuracies) can be flagged for removal. Legitimate negative feedback stays. It is part of the system\'s integrity. You can respond publicly to any review to share your perspective.',
      },
      {
        question: 'What credentials can be verified?',
        answer: 'Professional certifications (PMP, CPA, AWS, etc.), educational degrees, portfolio pieces with provenance, and professional association memberships. Verification typically takes 2-3 business days.',
      },
      {
        question: 'Do badges expire?',
        answer: 'Most badges are permanent once earned. Activity-based badges like "Active Contributor" require ongoing engagement to maintain. If your activity drops below the threshold for 90 days, activity badges become inactive but can be re-earned.',
      },
    ],
  },
  {
    slug: 'real-time-collaboration',
    name: 'Real-Time Collaboration',
    tagline: 'Built-in workspace with messaging, file sharing, and project tracking. No external tools needed.',
    description: 'SkillLedger collaboration workspace includes real-time messaging, file sharing, and milestone tracking so professional exchanges happen in one place.',
    longDescription: `Most barter platforms stop at matching. Once you find a partner, you are on your own for communication and project management. SkillLedger includes a full collaboration workspace so the entire exchange happens in one place.

Every project gets a dedicated workspace with real-time messaging powered by SignalR. No more juggling between a matching platform, a chat app, an email thread, and a file sharing service. Messages, files, milestones, and escrow status all live in the same interface, creating a single source of truth for the exchange.

File sharing supports documents, images, code, and design assets up to 100MB per file. Version history is tracked automatically so you can always reference earlier iterations. Milestone checklists let both parties agree on deliverables upfront and track progress visually, reducing scope creep and miscommunication.`,
    icon: 'MessageSquare',
    benefits: [
      'Real-time messaging with read receipts and typing indicators',
      'File sharing with automatic version history tracking',
      'Milestone checklists for structured deliverable tracking',
      'Integrated escrow status visible within the workspace',
      'Project timeline view for deadline management',
    ],
    targetKeywords: [
      'freelancer collaboration workspace',
      'professional real-time messaging',
      'skill exchange project management',
      'barter collaboration tools',
    ],
    relatedCategories: ['web-development', 'design', 'writing'],
    faqs: [
      {
        question: 'Is messaging included in all plans?',
        answer: 'Yes. Real-time messaging and basic file sharing are available on all paid plans. Business and Enterprise plans add larger file upload limits, advanced project management features, and priority message delivery.',
      },
      {
        question: 'Can I use external tools instead of the built-in workspace?',
        answer: 'You can communicate however you prefer, but using the built-in workspace is recommended because all messages and files are automatically included as evidence in any dispute resolution process. External communications cannot be verified by the mediation team.',
      },
      {
        question: 'What file types are supported?',
        answer: 'All common document formats (PDF, DOCX, XLSX), image formats (PNG, JPG, SVG, PSD), code files, design assets (Figma links, Sketch files), and compressed archives (ZIP, RAR). Maximum file size is 100MB per upload.',
      },
      {
        question: 'Are messages encrypted?',
        answer: 'Messages are encrypted in transit (TLS 1.3) and at rest (AES-256). Only the two workspace participants and authorized mediators (in case of disputes) can access message content.',
      },
    ],
  },
  {
    slug: 'skill-marketplace',
    name: 'Skill Marketplace',
    tagline: 'Browse, search, and match with professionals across 19 skill categories and 50+ cities.',
    description: 'SkillLedger skill marketplace connects professionals across 19 categories and 50+ US cities for credit-based service exchanges.',
    longDescription: `Finding the right professional for a barter exchange takes more than a search bar. SkillLedger's marketplace is built specifically for skill matching, connecting people who have what you need with people who need what you have.

The marketplace spans 19 skill categories from software development and design to legal consulting and healthcare services. Each category includes detailed subcategories so you can find not just "a designer" but specifically "a UI/UX designer experienced in SaaS dashboard design." Location filtering covers 50+ US cities for professionals who prefer local, in-person collaboration.

Smart matching goes beyond keyword search. The platform analyzes your listed skills, your credit balance, and your exchange history to surface opportunities where both parties benefit. When you post a project, the matching algorithm notifies members whose skills align with your needs and who are actively looking for the type of skills you offer, creating natural two-way exchanges.`,
    icon: 'Search',
    benefits: [
      '19 skill categories with detailed subcategory filtering',
      '50+ US cities for local professional matching',
      'Smart matching algorithm surfaces two-way exchange opportunities',
      'Project posting with automatic skill-based notifications',
      'Verified professional profiles with reputation scores',
      'Advanced filters for budget, availability, and experience level',
    ],
    targetKeywords: [
      'skill exchange marketplace',
      'professional service marketplace',
      'freelancer skill matching',
      'barter marketplace professionals',
    ],
    relatedCategories: ['web-development', 'design', 'marketing', 'consulting'],
    faqs: [
      {
        question: 'How many professionals are on the marketplace?',
        answer: 'SkillLedger is growing its professional community across 19 skill categories and 50+ US cities. The marketplace is designed so that even a smaller network creates value through smart matching and credit flexibility. You do not need a direct swap partner.',
      },
      {
        question: 'Can I post a project and wait for proposals?',
        answer: 'Yes. Post a project describing what you need, set a credit budget, and the platform notifies matching professionals. You can also browse available professionals and invite them directly to your project.',
      },
      {
        question: 'Is the marketplace limited to US-based professionals?',
        answer: 'City pages currently cover 50+ US metros, but the platform is open to professionals worldwide. Remote exchanges are fully supported through the built-in collaboration workspace.',
      },
      {
        question: 'How does smart matching work?',
        answer: 'The algorithm considers your listed skills, the skills you have requested in past projects, your location preferences, credit balance, and availability. It then surfaces profiles and projects where both parties are likely to find value in an exchange.',
      },
    ],
  },
]

export function getFeatureBySlug(slug: string): FeatureData | undefined {
  return featuresData.find((f) => f.slug === slug)
}
