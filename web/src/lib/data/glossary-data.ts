export interface GlossaryTerm {
  slug: string
  term: string
  definition: string
  relatedTerms: string[]
}

export const glossaryData: GlossaryTerm[] = [
  {
    slug: 'skill-barter',
    term: 'Skill Barter',
    definition: 'The exchange of professional skills or services between two parties without the use of money. On SkillLedger, skill barter is facilitated through a credit system that enables fair valuation and multi-party exchanges.',
    relatedTerms: ['service-swap', 'credit-exchange', 'barter-economy'],
  },
  {
    slug: 'credit-exchange',
    term: 'Credit Exchange',
    definition: 'A system in which participants earn credits by providing services and spend those credits to receive services from others. SkillLedger uses credits as a unit of account for professional skill exchanges, enabling asynchronous and multi-party trades.',
    relatedTerms: ['skill-barter', 'credit-rate', 'credit-wallet'],
  },
  {
    slug: 'service-swap',
    term: 'Service Swap',
    definition: 'A direct or credit-mediated exchange of professional services between two individuals or businesses. On SkillLedger, service swaps are structured agreements with defined deliverables, timelines, and credit values.',
    relatedTerms: ['skill-barter', 'professional-exchange', 'escrow'],
  },
  {
    slug: 'barter-economy',
    term: 'Barter Economy',
    definition: 'An economic system in which goods and services are exchanged directly for other goods and services without using money as an intermediary. Modern barter economies use digital credit systems to overcome the traditional limitations of direct trade. SkillLedger applies this model to professional services.',
    relatedTerms: ['skill-barter', 'credit-exchange', 'professional-exchange'],
  },
  {
    slug: 'professional-exchange',
    term: 'Professional Exchange',
    definition: 'A structured marketplace where professionals trade skills and services with each other. SkillLedger operates as a professional exchange platform that enables verified professionals to offer and receive services across dozens of skill categories.',
    relatedTerms: ['service-swap', 'skill-barter', 'skills-marketplace'],
  },
  {
    slug: 'escrow',
    term: 'Escrow',
    definition: 'A neutral holding arrangement in which credits or funds are held by a third party until both sides confirm successful delivery of services. On SkillLedger, the escrow system protects both providers and clients during skill exchanges, releasing credits only upon milestone completion.',
    relatedTerms: ['project-escrow', 'credit-exchange', 'milestone'],
  },
  {
    slug: 'project-escrow',
    term: 'Project Escrow',
    definition: 'An escrow arrangement designed for multi-milestone projects. Credits are locked at the start of an engagement and released incrementally as milestones are completed and approved. On SkillLedger, project escrow breaks large exchanges into smaller, lower-risk stages.',
    relatedTerms: ['escrow', 'milestone', 'credit-exchange'],
  },
  {
    slug: 'reputation-score',
    term: 'Reputation Score',
    definition: 'A numerical measure of a professional\'s reliability, quality, and trustworthiness, derived from completed exchanges, peer reviews, badge attainments, and platform activity. On SkillLedger, higher reputation scores unlock additional features and attract more exchange requests.',
    relatedTerms: ['badge', 'peer-review', 'trust-level'],
  },
  {
    slug: 'credit-rate',
    term: 'Credit Rate',
    definition: 'The number of credits charged per hour (or per unit of work) for a specific professional service. Credit rates are set by individual providers and reflect their skill level, specialization, and market demand. On SkillLedger, providers set their own credit rates when creating skill listings.',
    relatedTerms: ['credit-exchange', 'credit-wallet', 'hourly-rate'],
  },
  {
    slug: 'credit-wallet',
    term: 'Credit Wallet',
    definition: 'A digital account that holds a user\'s earned and purchased credits. Credits in the wallet can be used to request services from other professionals, held in escrow during active projects, or converted to cash under certain subscription plans. On SkillLedger, the credit wallet is the central hub for all transactions.',
    relatedTerms: ['credit-exchange', 'credit-rate', 'escrow'],
  },
  {
    slug: 'skills-marketplace',
    term: 'Skills Marketplace',
    definition: 'A platform where professionals list their services and browse services offered by others. SkillLedger\'s skills marketplace enables discovery, matching, and exchange of professional skills across categories including development, design, marketing, and consulting.',
    relatedTerms: ['professional-exchange', 'skill-listing', 'barter-economy'],
  },
  {
    slug: 'skill-listing',
    term: 'Skill Listing',
    definition: 'A profile entry where a professional describes a specific service they offer, including a description, credit rate, deliverables, and timeline. Skill listings are the primary unit of supply in any skills marketplace. On SkillLedger, each listing is searchable and filterable by category.',
    relatedTerms: ['skills-marketplace', 'credit-rate', 'professional-exchange'],
  },
  {
    slug: 'badge',
    term: 'Badge',
    definition: 'A verified credential that recognizes a professional\'s achievement, expertise, or trustworthiness in a specific area. Badges are earned through completing exchanges, peer endorsements, or demonstrated skill verification. On SkillLedger, badges appear on profiles and influence search ranking.',
    relatedTerms: ['reputation-score', 'peer-review', 'trust-level'],
  },
  {
    slug: 'peer-review',
    term: 'Peer Review',
    definition: 'Structured feedback and a rating provided by a professional after completing an exchange. Peer reviews contribute to reputation scores and help future users evaluate potential exchange partners. On SkillLedger, both parties in an exchange can leave reviews.',
    relatedTerms: ['reputation-score', 'badge', 'trust-level'],
  },
  {
    slug: 'trust-level',
    term: 'Trust Level',
    definition: 'A tiered designation reflecting a professional\'s verified history, reputation score, and platform tenure. Higher trust levels unlock increased credit limits, larger escrow allowances, and greater marketplace visibility. On SkillLedger, trust levels range from new member to established professional.',
    relatedTerms: ['reputation-score', 'badge', 'peer-review'],
  },
  {
    slug: 'milestone',
    term: 'Milestone',
    definition: 'A defined deliverable or checkpoint within a project. Milestones structure multi-stage work agreements and trigger escrow releases when approved by the receiving party. On SkillLedger, milestones are set during exchange setup and tracked in the project workspace.',
    relatedTerms: ['project-escrow', 'escrow', 'service-swap'],
  },
  {
    slug: 'hourly-rate',
    term: 'Hourly Rate',
    definition: 'The credit value assigned to one hour of professional service. Hourly rates are set by individual providers and vary by skill category, experience level, and demand. They form the basis for calculating total project values. On SkillLedger, hourly rates map directly to credit rates.',
    relatedTerms: ['credit-rate', 'credit-wallet', 'skill-listing'],
  },
  {
    slug: 'barter-income',
    term: 'Barter Income',
    definition: 'The fair market value of services received in exchange for services provided. In the United States and most jurisdictions, barter income is taxable and must be reported. SkillLedger provides transaction records to help users maintain accurate tax documentation.',
    relatedTerms: ['skill-barter', 'barter-economy', 'fair-market-value'],
  },
  {
    slug: 'fair-market-value',
    term: 'Fair Market Value',
    definition: 'The price at which services would be exchanged between a willing buyer and seller in an open market. On SkillLedger, fair market value is approximated by the credit rate set by providers and accepted by clients.',
    relatedTerms: ['barter-income', 'credit-rate', 'skill-barter'],
  },
  {
    slug: 'exchange-agreement',
    term: 'Exchange Agreement',
    definition: 'A formal agreement between two professionals that specifies the services to be exchanged, credit values, milestones, timelines, and acceptance criteria. Exchange agreements are stored on the platform and form the basis for escrow and dispute resolution. On SkillLedger, every exchange begins with an agreement signed by both parties.',
    relatedTerms: ['service-swap', 'escrow', 'milestone'],
  },
  {
    slug: 'skill-verification',
    term: 'Skill Verification',
    definition: 'The process of confirming a professional\'s claimed skills through portfolio review, peer endorsement, or third-party credential validation. Verified skills increase marketplace visibility and buyer confidence. On SkillLedger, verified skills display a trust badge on the provider\'s profile.',
    relatedTerms: ['badge', 'reputation-score', 'skill-listing'],
  },
  {
    slug: 'multi-party-exchange',
    term: 'Multi-Party Exchange',
    definition: 'An exchange involving three or more professionals, enabled by a credit system. Unlike direct barter, which requires a double coincidence of wants, multi-party exchanges allow A to provide services to B, B to provide to C, and C to provide to A, all mediated by credits. SkillLedger\'s credit wallet makes multi-party exchanges automatic.',
    relatedTerms: ['credit-exchange', 'barter-economy', 'skill-barter'],
  },
  {
    slug: 'double-coincidence-of-wants',
    term: 'Double Coincidence of Wants',
    definition: 'The traditional limitation of barter where trade requires two parties to each want exactly what the other has to offer at the same time. Credit-based systems like SkillLedger eliminate this constraint by allowing professionals to earn credits from anyone and spend them with anyone.',
    relatedTerms: ['barter-economy', 'multi-party-exchange', 'credit-exchange'],
  },
  {
    slug: 'professional-collaboration',
    term: 'Professional Collaboration',
    definition: 'A structured working relationship between two or more professionals to complete a project or deliver a combined service offering. On SkillLedger, professional collaboration is supported by shared workspaces, messaging, and milestone tracking.',
    relatedTerms: ['exchange-agreement', 'skills-marketplace', 'professional-exchange'],
  },
  {
    slug: 'credit-transfer',
    term: 'Credit Transfer',
    definition: 'The movement of credits from one user\'s wallet to another, typically upon completion of a service or approval of a milestone. On SkillLedger, credit transfers are recorded in the platform\'s ledger and available for tax documentation.',
    relatedTerms: ['credit-wallet', 'escrow', 'milestone'],
  },
  {
    slug: 'dispute-resolution',
    term: 'Dispute Resolution',
    definition: 'The process of mediating conflicts between exchange partners when deliverables are contested or agreements are not honored. On SkillLedger, the dispute resolution system reviews exchange agreements, communications, and deliverable evidence before determining escrow outcomes.',
    relatedTerms: ['escrow', 'exchange-agreement', 'trust-level'],
  },
  {
    slug: 'service-provider',
    term: 'Service Provider',
    definition: 'A professional who offers skills or services in exchange for credits. Service providers create skill listings, set their credit rates, and deliver agreed-upon work in exchange for credit compensation. On SkillLedger, any member can act as a service provider.',
    relatedTerms: ['skill-listing', 'credit-rate', 'skills-marketplace'],
  },
  {
    slug: 'service-client',
    term: 'Service Client',
    definition: 'A professional who requests and receives services from providers, paying credits from their wallet. In most exchanges, participants alternate between client and provider roles. On SkillLedger, any member can act as a service client.',
    relatedTerms: ['credit-wallet', 'exchange-agreement', 'escrow'],
  },
  {
    slug: 'subscription-tier',
    term: 'Subscription Tier',
    definition: 'A membership level that determines platform feature access, credit limits, escrow allowances, and marketplace visibility. Subscription tiers range from free access with basic features to paid plans with expanded capabilities. On SkillLedger, tiers include Free and Premium.',
    relatedTerms: ['credit-wallet', 'trust-level', 'skills-marketplace'],
  },
  {
    slug: 'endorsement',
    term: 'Endorsement',
    definition: 'A formal acknowledgment from one professional to another confirming their expertise in a specific skill. Endorsements contribute to reputation scores and can support badge attainment. On SkillLedger, endorsements appear on the recipient\'s profile.',
    relatedTerms: ['peer-review', 'badge', 'reputation-score'],
  },
  {
    slug: 'portfolio',
    term: 'Portfolio',
    definition: 'A collection of work samples, completed projects, and case studies displayed on a professional\'s profile. Portfolios provide evidence of skill quality and help attract exchange partners. On SkillLedger, portfolio items can be linked to verified exchanges.',
    relatedTerms: ['skill-listing', 'skill-verification', 'reputation-score'],
  },
  {
    slug: 'time-banking',
    term: 'Time Banking',
    definition: 'An exchange system where time spent providing services is the unit of currency, regardless of the service type. Unlike traditional time banking, SkillLedger uses variable credit rates that reflect skill scarcity and market demand rather than treating all hours as equal.',
    relatedTerms: ['credit-exchange', 'skill-barter', 'barter-economy'],
  },
  {
    slug: 'barter-agreement',
    term: 'Barter Agreement',
    definition: 'A legally enforceable contract governing the exchange of professional services between two parties without cash payment. Under US law, a valid barter agreement must satisfy five elements: offer, acceptance, consideration, capacity, and legality. Courts do not require monetary consideration or equal value exchange. Each party\'s promise to perform services constitutes valid consideration.',
    relatedTerms: ['barter-valuation', 'scope-creep', 'barter-invoice'],
  },
  {
    slug: 'form-1099-b',
    term: 'Form 1099-B',
    definition: 'The IRS information return that barter exchanges must file for each member\'s transactions under IRC 6045. Box 13 reports the fair market value of goods or services received through the exchange. Unlike general business reporting under IRC 6041, barter exchanges must file Form 1099-B even for corporate members and have no minimum aggregate dollar threshold, only a de minimis exemption for transactions under $1.00.',
    relatedTerms: ['barter-income', 'fair-market-value', 'schedule-c-barter-income'],
  },
  {
    slug: 'barter-valuation',
    term: 'Barter Valuation',
    definition: 'The process of determining the fair market value of services exchanged in a barter transaction. The IRS requires each party to report the FMV of what they receive, not what they give. Under Treasury Regulation 1.61-2(d)(1), if both parties agree on a stipulated price, the IRS accepts that figure unless contradicted by evidence. Services must be valued at the provider\'s normal retail rate.',
    relatedTerms: ['fair-market-value', 'service-exchange-rate', 'credit-rate'],
  },
  {
    slug: 'scope-creep',
    term: 'Scope Creep',
    definition: 'The gradual expansion of deliverables beyond the original agreement in a barter exchange. When one party overdelivers without renegotiating terms, they may seek recovery through unjust enrichment or quantum meruit doctrines. Barter agreements should include explicit scope definitions, change-order procedures, and a percentage threshold (typically 5-10%) above which formal renegotiation is required.',
    relatedTerms: ['barter-agreement', 'exchange-agreement', 'dispute-resolution'],
  },
  {
    slug: 'service-exchange-rate',
    term: 'Service Exchange Rate',
    definition: 'The ratio at which one professional\'s services are exchanged for another\'s, calculated by comparing each party\'s normal market rate. For example, if a lawyer charges $400/hour and a designer charges $75/hour, the designer provides approximately 5.3 hours of work per hour of legal services received. This dollar-for-dollar approach is the IRS-mandated standard for barter valuation.',
    relatedTerms: ['barter-valuation', 'fair-market-value', 'credit-rate'],
  },
  {
    slug: 'schedule-c-barter-income',
    term: 'Schedule C Barter Income',
    definition: 'The IRS form where sole proprietors and freelancers report barter income as part of their business gross receipts. Under IRC 61 and Revenue Ruling 79-24, the fair market value of services received through barter must be included in gross income in the year received. Barter income is also subject to self-employment tax, calculated on Schedule SE.',
    relatedTerms: ['form-1099-b', 'barter-income', 'fair-market-value'],
  },
  {
    slug: 'trade-exchange-network',
    term: 'Trade Exchange Network',
    definition: 'An organized network of businesses and professionals who use a shared credit system to facilitate multilateral barter transactions. Major networks like ITEX, BizX, and IMS Barter maintain a 1:1 peg between trade dollars and US dollars, process millions in transactions annually, and are regulated as barter exchanges under IRC 6045(c). The International Reciprocal Trade Association (IRTA) estimates the global commercial barter industry at $12-14 billion annually.',
    relatedTerms: ['multi-party-exchange', 'credit-exchange', 'barter-economy'],
  },
  {
    slug: 'lets-local-exchange-trading',
    term: 'LETS (Local Exchange Trading System)',
    definition: 'A community-based exchange system originated by Michael Linton in 1983 in British Columbia where members negotiate their own rates in local credit units. Unlike strict time banks, most LETS allow market-based pricing. Research shows only 13% of UK LETS practiced strict hour-for-hour equivalence. LETS credits cannot be converted to national currency, and balances are publicly visible to promote trust.',
    relatedTerms: ['time-banking', 'trade-exchange-network', 'credit-exchange'],
  },
  {
    slug: 'mutual-credit-clearing',
    term: 'Mutual Credit Clearing',
    definition: 'A multilateral settlement system where debits and credits between exchange members are netted against each other rather than requiring direct bilateral settlement. IRTA\'s Universal Currency (UC) uses mutual credit clearing to connect over 100 barter exchanges globally, recording $14.5 million in inter-exchange barter transactions in its peak year (2017). SkillLedger\'s credit system operates on similar mutual credit principles.',
    relatedTerms: ['multi-party-exchange', 'trade-exchange-network', 'credit-liquidity'],
  },
  {
    slug: 'fractional-skill-swap',
    term: 'Fractional Skill Swap',
    definition: 'A partial exchange where professionals trade a fraction of their service hours rather than committing to a full project. Fractional swaps lower the barrier to entry for skill exchange by allowing professionals to test partnerships, balance risk, and maintain cash-paying client work at the same time. SkillLedger credits enable fractional exchanges by decoupling the timing and magnitude of each party\'s contribution.',
    relatedTerms: ['service-swap', 'credit-exchange', 'skill-barter'],
  },
  {
    slug: 'barter-invoice',
    term: 'Barter Invoice',
    definition: 'A formal document recording the fair market value of services exchanged in a barter transaction. Barter invoices must NOT show $0. They must display the full FMV of services provided, with a notation that payment was received through reciprocal services or barter credits. Under GAAP (ASC 845), each party records a debit to barter receivable and a credit to service revenue at fair market value.',
    relatedTerms: ['barter-agreement', 'fair-market-value', 'schedule-c-barter-income'],
  },
  {
    slug: 'reciprocal-arrangement',
    term: 'Reciprocal Arrangement',
    definition: 'A mutual agreement where two or more parties exchange services of equivalent value over a defined period. The IRS treats reciprocal arrangements identically to cash transactions under IRC 61. Both parties must report the fair market value of services received as gross income. Under Revenue Ruling 80-52, income is recognized when credits are allocated, not when services are eventually rendered.',
    relatedTerms: ['barter-agreement', 'service-swap', 'form-1099-b'],
  },
  {
    slug: 'skill-deficit',
    term: 'Skill Deficit',
    definition: 'A negative credit balance on a barter exchange, indicating that a member has received more services than they have provided. IRTA guidelines recommend exchanges cap deficits at no more than 2.5 to 3.0 times the monthly annual averaged trade volume to prevent trade dollar inflation. On SkillLedger, credit wallets and escrow mechanisms prevent uncontrolled deficit accumulation.',
    relatedTerms: ['credit-wallet', 'credit-liquidity', 'trade-exchange-network'],
  },
  {
    slug: 'credit-liquidity',
    term: 'Credit Liquidity',
    definition: 'The ease with which barter credits can be spent on desired services within an exchange network. Low liquidity, where members accumulate credits but cannot find desirable services to spend them on, is the primary failure mode of barter exchanges. IRTA applies the Quantity Theory of Money (MV = PQ) to manage credit supply and recommends exchanges maintain enough service variety to ensure members can reliably spend their earnings.',
    relatedTerms: ['skill-deficit', 'trade-exchange-network', 'credit-wallet'],
  },
  {
    slug: 'bootstrapping-with-barter',
    term: 'Bootstrapping with Barter',
    definition: 'A startup strategy where founders exchange professional services instead of spending cash to build their initial product, brand, or go-to-market capabilities. Documented examples include founders trading web development for design, marketing for legal services, and offering equity-equivalent barter credits to early contributors. Under GAAP (ASC 845), bartered services received must be recorded at fair market value on financial statements.',
    relatedTerms: ['skill-barter', 'barter-economy', 'fractional-skill-swap'],
  },
]

export function getTermBySlug(slug: string): GlossaryTerm | undefined {
  return glossaryData.find((t) => t.slug === slug)
}

export function getTermsByFirstLetter(): Record<string, GlossaryTerm[]> {
  const grouped: Record<string, GlossaryTerm[]> = {}
  const sorted = [...glossaryData].sort((a, b) => a.term.localeCompare(b.term))
  for (const term of sorted) {
    const letter = term.term[0].toUpperCase()
    if (!grouped[letter]) grouped[letter] = []
    grouped[letter].push(term)
  }
  return grouped
}
