export interface ComparisonData {
  slug: string
  title: string
  description: string
  sideA: { name: string; strengths: string[]; weaknesses: string[]; pricing: string }
  sideB: { name: string; strengths: string[]; weaknesses: string[]; pricing: string }
  verdict: string
  keyStatistic?: string
  faqs: Array<{ question: string; answer: string }>
}

export const comparisonsData: ComparisonData[] = [
  {
    slug: 'skillledger-vs-simbi',
    title: 'SkillLedger vs. Simbi: B2B Professional Platform vs. Community Exchange',
    description: 'Compare SkillLedger and Simbi side by side. SkillLedger offers escrow, dispute resolution, and 1099-B compliance for professionals, while Simbi is a free community exchange with volunteer maintenance.',
    sideA: {
      name: 'SkillLedger',
      strengths: [
        'Built-in escrow protects both parties until deliverables are approved',
        'Structured dispute resolution with neutral mediators',
        'Automatic FMV tracking for every exchange',
        '1099-B compliance reporting simplifies year-end taxes',
        'Professional verification ensures credential authenticity',
      ],
      weaknesses: [
        'Newer platform with a smaller initial network',
        'Premium features require a paid subscription',
      ],
      pricing: 'From $19/mo (30-day trial)',
    },
    sideB: {
      name: 'Simbi',
      strengths: [
        'Completely free to use with no paid tiers',
        '501(c)(3) nonprofit with a mission-driven focus',
        'YC-backed origins (W16 batch) lent early credibility',
      ],
      weaknesses: [
        'Density problem acknowledged in their own 2022 community letter. Not enough active users in most areas',
        'No dispute resolution mechanism for failed exchanges',
        'No escrow or payment protection of any kind',
        'Volunteer-maintained app with reported upload and notification bugs',
        '"Newbi" onboarding tier restricts your available pool until you complete initial trades',
      ],
      pricing: 'Free',
    },
    verdict: 'Simbi works for casual community trades where both parties know each other or the stakes are low. SkillLedger is built for professionals who need escrow, tax compliance, and dispute resolution to protect high-value exchanges.',
    keyStatistic: 'Simbi\'s own 2022 community letter acknowledged a critical density problem, with too few active users in most geographic areas to sustain reliable matching.',
    faqs: [
      {
        question: 'Is Simbi still active in 2026?',
        answer: 'Simbi continues to operate as a 501(c)(3) nonprofit, but its 2022 community letter acknowledged a density problem: too few active users in most geographic areas to sustain reliable matching. The platform is volunteer-maintained.',
      },
      {
        question: 'Does SkillLedger charge a commission on exchanges?',
        answer: 'No. SkillLedger does not take a percentage of exchange value. Plans start at $19/month with a 30-day free trial. All plans include escrow protection, dispute resolution, and compliance reporting.',
      },
      {
        question: 'Which platform handles taxes better?',
        answer: 'SkillLedger automatically tracks fair market value and generates 1099-B-ready reports. Simbi does not provide tax documentation, so users must manually calculate and report the FMV of services received.',
      },
    ],
  },
  {
    slug: 'skillledger-vs-fiverr',
    title: 'SkillLedger vs. Fiverr: Why Credits Beat Cash Fees',
    description: 'Compare SkillLedger credit-based skill exchange with Fiverr cash marketplace. Fiverr takes a 27.6% effective take rate while SkillLedger lets you keep the full value of your work.',
    sideA: {
      name: 'SkillLedger',
      strengths: [
        'No 20% commission. Keep the full value of your work',
        'Credit-based exchange eliminates cash outlay for services you need',
        'Escrow and dispute resolution protect both parties',
        'FMV tracking and 1099-B compliance built in',
        'Direct professional relationships instead of anonymous gig transactions',
      ],
      weaknesses: [
        'Requires finding a counterparty who needs your skills',
        'Credits are platform-specific, not convertible to cash',
      ],
      pricing: 'From $19/mo (30-day trial)',
    },
    sideB: {
      name: 'Fiverr',
      strengths: [
        'Massive marketplace with 700+ service categories',
        'NYSE-listed (FVRR) with established brand recognition',
        'Buyer protection and review system',
        'Instant access to global talent pool',
      ],
      weaknesses: [
        '27.6% effective take rate (20% seller fee + processing fees) erodes earnings',
        'Active buyers declined to 3.3M, down from peak levels',
        'Account suspensions reported without warning or clear explanation',
        'Race-to-the-bottom pricing pressure on sellers',
      ],
      pricing: '20% seller commission + buyer service fee',
    },
    verdict: 'Fiverr excels when you need to hire quickly from a massive talent pool and are willing to pay cash plus fees. SkillLedger is the better choice when you want to exchange skills directly, keep the full value of your work, and avoid the 20%+ commission that eats into freelancer earnings.',
    keyStatistic: 'Fiverr\'s effective take rate is 27.1%, combining a 20% seller fee with a 5.5% buyer fee on every transaction.',
    faqs: [
      {
        question: 'How much does Fiverr actually take from sellers?',
        answer: 'Fiverr charges sellers a flat 20% commission on every order. Combined with buyer service fees and payment processing, the effective take rate reaches approximately 27.6% of the total transaction value. On a $100 gig, the seller receives about $80 before withdrawal fees.',
      },
      {
        question: 'Can I use SkillLedger and Fiverr together?',
        answer: 'Yes. Many professionals use Fiverr for cash income and SkillLedger to obtain services they need without spending that cash. For example, a designer might sell logos on Fiverr for revenue while exchanging design work for accounting services on SkillLedger.',
      },
      {
        question: 'What happens to my Fiverr reviews if I switch?',
        answer: 'Fiverr reviews stay on Fiverr. SkillLedger has its own professional verification and reputation system. You can link to your Fiverr profile or portfolio during onboarding to establish credibility on SkillLedger quickly.',
      },
    ],
  },
  {
    slug: 'skillledger-vs-upwork',
    title: 'SkillLedger vs. Upwork: Skill Exchange vs. Cash Freelancing',
    description: 'Compare SkillLedger skill exchange with Upwork cash freelancing. Upwork charges up to 10% freelancer commission plus a 5% buyer fee, while SkillLedger enables direct skill-for-skill trades.',
    sideA: {
      name: 'SkillLedger',
      strengths: [
        'Zero commission on exchanges. No percentage taken from either party',
        'Skill-for-skill trades preserve cash for other business needs',
        'Built-in escrow and dispute resolution',
        'Professional verification and reputation badges',
        'Automatic tax compliance documentation',
      ],
      weaknesses: [
        'Not suited for one-directional cash-for-service transactions',
        'Smaller marketplace compared to Upwork\'s established base',
      ],
      pricing: 'From $19/mo (30-day trial)',
    },
    sideB: {
      name: 'Upwork',
      strengths: [
        'Large global talent marketplace with millions of freelancers',
        'Hourly and fixed-price contract flexibility',
        'Enterprise-grade project management tools',
        'Payment protection for both clients and freelancers',
      ],
      weaknesses: [
        '10% freelancer service fee on all earnings',
        '5% client marketplace fee added to every contract',
        'Connects fee charged for proposals limits visibility',
        'Algorithm-driven visibility can bury newer freelancers',
      ],
      pricing: '10% freelancer fee + 5% client fee',
    },
    verdict: 'Upwork is the go-to platform when you need to hire or be hired for cash freelance work at scale. SkillLedger makes more sense when two professionals can directly exchange expertise, saving both parties the 10-15% in platform fees and preserving cash flow.',
    keyStatistic: 'Upwork charges freelancers 10% on the first $500 with each client, with additional payment processing fees.',
    faqs: [
      {
        question: 'How does Upwork\'s fee structure work?',
        answer: 'Upwork charges freelancers a flat 10% service fee on all earnings. Clients pay an additional 5% marketplace fee. Combined, the platform takes approximately 15% of the total contract value before payment processing costs.',
      },
      {
        question: 'Can I find the same quality of professionals on SkillLedger?',
        answer: 'SkillLedger uses professional verification, credential checks, and reputation badges to ensure quality. While the network is smaller than Upwork, the verification process means you are more likely to connect with vetted professionals in your industry.',
      },
      {
        question: 'Is skill exchange faster than hiring on Upwork?',
        answer: 'It depends on the match. Upwork gives you immediate access to proposals within hours. SkillLedger matching may take longer initially, but once connected, exchanges tend to be more collaborative since both parties are invested in delivering quality work.',
      },
    ],
  },
  {
    slug: 'skill-barter-vs-cash-freelancing',
    title: 'Skill Barter vs. Cash Freelancing: When Each Model Wins',
    description: 'A detailed comparison of skill barter and cash freelancing models. Learn when bartering saves money, when cash is better, and how IRS FMV requirements apply to both.',
    sideA: {
      name: 'Skill Barter',
      strengths: [
        'Preserves cash for rent, payroll, and other fixed costs',
        'No platform fees or commissions on the exchange itself',
        'Converts spare capacity into tangible business value',
        'Builds deeper professional relationships through mutual investment',
        'IRTA reports that businesses using 10-15% barter see measurable overhead reduction',
      ],
      weaknesses: [
        'Requires finding a complementary-skill counterparty',
        'IRS requires reporting barter income at fair market value (FMV)',
        'Less liquid than cash. Cannot easily redirect value elsewhere',
      ],
      pricing: 'No platform fees (SkillLedger: from $19/mo with 30-day free trial)',
    },
    sideB: {
      name: 'Cash Freelancing',
      strengths: [
        'Immediate liquidity. Cash can be spent anywhere',
        'Simpler tax reporting with standard 1099-NEC forms',
        'Scalable: hire multiple freelancers simultaneously',
        'Universally understood pricing eliminates valuation disputes',
      ],
      weaknesses: [
        'Platform fees range from 10-27% on major marketplaces',
        'Cash outlay required upfront or upon delivery',
        'Race-to-the-bottom pricing pressure on commodity skills',
      ],
      pricing: '10-27% platform fees on major marketplaces',
    },
    verdict: 'Cash freelancing wins when you need immediate liquidity, scale, or commodity-level skills. Skill barter wins when you have spare capacity, need high-value professional services, and want to preserve cash. The IRTA recommends businesses keep 10-15% of their procurement in barter for optimal cost savings.',
    keyStatistic: 'The IRTA recommends businesses allocate 10-15% of procurement spend to barter exchanges for measurable overhead reduction.',
    faqs: [
      {
        question: 'Do I have to pay taxes on bartered services?',
        answer: 'Yes. The IRS treats barter income as taxable at fair market value under Revenue Ruling 79-24. Both parties must report the FMV of services received. SkillLedger automates FMV tracking and generates 1099-B-ready documentation to simplify compliance.',
      },
      {
        question: 'What is the IRTA 10-15% recommendation?',
        answer: 'The International Reciprocal Trade Association (IRTA) suggests that businesses allocate 10-15% of their procurement spend to barter exchanges. This threshold is high enough to produce measurable savings while low enough to avoid liquidity constraints.',
      },
      {
        question: 'Can I combine barter and cash freelancing?',
        answer: 'Yes. Many professionals use cash platforms like Upwork or Fiverr for revenue generation while using SkillLedger to obtain services they need without cash outlay. This hybrid approach maximizes both income and cost savings.',
      },
    ],
  },
  {
    slug: 'time-banking-vs-skill-exchange',
    title: 'Time Banking vs. Skill Exchange: Key Differences Explained',
    description: 'Understand the differences between time banking and skill exchange. Time banking values all hours equally, while skill exchange uses market rates and FMV tracking for professional services.',
    sideA: {
      name: 'Time Banking',
      strengths: [
        'Radical equality: 1 hour of any service = 1 hour of any other service (Edgar Cahn model)',
        'IRS generally treats time bank exchanges as non-taxable when community-focused',
        'hOurworld network has grown to 29,016 registered members worldwide',
        'Strong community-building ethos that values all contributions equally',
      ],
      weaknesses: [
        'Undervalues specialized professional skills (a lawyer\'s hour equals a dog walker\'s hour)',
        'Shih et al. 2015 CHI study identified tension between equal-time ideology and market reality',
        'Williams 1996 research found only 13% of participants maintained strict hour-for-hour equivalence',
        'Limited adoption among professionals due to perceived value mismatch',
      ],
      pricing: 'Free (community-run, donation-supported)',
    },
    sideB: {
      name: 'Skill Exchange',
      strengths: [
        'Market-rate valuation reflects the true worth of specialized skills',
        'FMV tracking ensures both parties understand the value exchanged',
        'Commercial focus attracts professionals and businesses',
        'Credit systems allow non-simultaneous exchanges',
      ],
      weaknesses: [
        'Exchanges are taxable at fair market value under IRS rules',
        'Requires FMV documentation and compliance reporting',
        'Less ideologically appealing to community-first participants',
      ],
      pricing: 'Varies by platform (SkillLedger: from $19/mo with 30-day free trial)',
    },
    verdict: 'Time banking is ideal for community service exchanges where equality matters more than market value. Think neighborhood help networks. Skill exchange is the professional choice when specialized expertise has different market values and both parties expect fair-market-value compensation for their time.',
    keyStatistic: 'Williams 1996 research found only 13% of time bank participants maintained strict hour-for-hour equivalence in practice.',
    faqs: [
      {
        question: 'Are time bank hours taxable?',
        answer: 'Generally no. The IRS has indicated that time bank exchanges within community service contexts are typically non-taxable, as they resemble volunteer service more than commercial barter. However, if exchanges involve professional services at commercial scale, tax treatment may differ. Consult a tax professional for your situation.',
      },
      {
        question: 'Why did Edgar Cahn create time banking?',
        answer: 'Civil rights lawyer Edgar Cahn created time banking in 1980 to value the "core economy," the unpaid work of community, family, and neighborhood that the market economy ignores. His model intentionally equates one hour of any service to one hour of any other to promote social equity.',
      },
      {
        question: 'Can a professional use both time banking and skill exchange?',
        answer: 'Yes. Some professionals volunteer through time banks for community service while using platforms like SkillLedger for commercial skill exchanges. The key difference is intent and scale: community service vs. professional business transactions.',
      },
    ],
  },
  {
    slug: 'barter-vs-cash',
    title: "Barter vs. Cash: A Professional's Guide",
    description: 'A guide comparing barter and cash transactions for professionals. Learn how barter converts spare capacity, IRS reporting requirements, and when each payment model makes sense.',
    sideA: {
      name: 'Barter',
      strengths: [
        'Converts idle capacity into real business value. Ron Whitney (former IRTA president) called barter "the original peer-to-peer economy"',
        'No cash outlay required for high-value professional services',
        'IRTA estimates the commercial barter industry at $12-14 billion annually in the U.S.',
        'Builds strategic partnerships beyond transactional relationships',
      ],
      weaknesses: [
        'IRS requires reporting barter income at fair market value on 1099-B forms',
        'Finding a matching counterparty can take time (double coincidence of wants)',
        'Valuation disputes possible without a neutral FMV tracking system',
      ],
      pricing: 'No inherent cost (platform fees vary)',
    },
    sideB: {
      name: 'Cash',
      strengths: [
        'Universally accepted medium of exchange',
        'Simple, familiar transaction process',
        'Immediate settlement with no counterparty matching required',
        'Straightforward tax reporting via standard income documentation',
      ],
      weaknesses: [
        'Requires available liquidity, which strains cash-tight businesses',
        'Platform fees on freelance marketplaces range from 10-27%',
        'Does not put spare professional capacity to work',
      ],
      pricing: 'Direct cost + marketplace fees if applicable',
    },
    verdict: 'Cash remains the default for most transactions due to its simplicity and universal acceptance. Barter shines when professionals have spare capacity, need services they would otherwise pay cash for, and want to preserve working capital. The smartest approach combines both: use cash for commoditized purchases and barter for high-value professional exchanges.',
    keyStatistic: 'The IRTA estimates the U.S. commercial barter industry at $12-14 billion annually, with 450,000+ businesses participating globally.',
    faqs: [
      {
        question: 'How does the IRS treat barter transactions?',
        answer: 'The IRS treats barter exchanges as taxable events under Revenue Ruling 79-24. Both parties must report the fair market value (FMV) of services received as income. Barter exchanges facilitated through a third-party exchange are reported on Form 1099-B.',
      },
      {
        question: 'How big is the barter industry?',
        answer: 'The International Reciprocal Trade Association (IRTA) estimates the U.S. commercial barter industry at $12-14 billion annually. Globally, an estimated 450,000+ businesses participate in organized barter exchanges, with the practice growing steadily among SMBs seeking to preserve cash.',
      },
      {
        question: 'What is the "double coincidence of wants" problem?',
        answer: 'The double coincidence of wants is the classic barter limitation: both parties must want what the other offers at the same time. Modern platforms like SkillLedger solve this with credit systems. You earn credits by providing services to anyone and spend them with anyone else on the platform.',
      },
    ],
  },
  {
    slug: 'alternatives-to-thumbtack',
    title: 'Best Thumbtack Alternatives for Freelancers 2026',
    description: 'Compare the best Thumbtack alternatives for freelancers in 2026. From Bark to Fiverr to skill exchange platforms, find the option that eliminates pay-per-lead fees and maximizes your earnings.',
    sideA: {
      name: 'Thumbtack',
      strengths: [
        '1,100+ service categories covering nearly every local service',
        'Approximately 160,000 service requests per week from active consumers',
        'Strong brand recognition for local home and professional services',
        'Instant lead delivery with consumer intent signals',
      ],
      weaknesses: [
        'Pay-per-lead pricing ranges from $10 to $170+ depending on service category',
        'Lead-to-client conversion rates typically fall between 10-30%, meaning most paid leads never convert',
        'Trustpilot rating of 2.6 out of 5 stars reflects widespread pro dissatisfaction',
        'Same lead sent to up to 10+ competing professionals simultaneously',
      ],
      pricing: '$10-$170+ per lead (no commission, pay regardless of conversion)',
    },
    sideB: {
      name: 'Alternatives',
      strengths: [
        'Bark: fixed-price lead credits ($14-$65 per lead) with less competition per lead',
        'Fiverr: global marketplace but 27.6% effective take rate on seller earnings',
        'Skill exchange platforms like SkillLedger: zero lead fees, trade services directly',
        'Google Business Profile: free listing with organic local search visibility',
      ],
      weaknesses: [
        'Bark still uses pay-per-lead model, just at different price points',
        'Fiverr\'s global competition drives down pricing for commodity services',
        'Skill exchange requires finding counterparties who need your services',
        'Building organic visibility takes months of consistent effort',
      ],
      pricing: 'Varies: $0 (skill exchange) to $14-$65/lead (Bark) to 27.6% take rate (Fiverr)',
    },
    verdict: 'If you are tired of paying $10-$170 per lead on Thumbtack with no guarantee of conversion, consider the alternatives. Bark offers a similar model at sometimes lower prices. Fiverr provides global reach but takes 20%+ of your earnings. For professionals with complementary skills, SkillLedger eliminates lead fees entirely by enabling direct skill-for-skill exchanges.',
    keyStatistic: 'Thumbtack lead-to-client conversion rates typically fall between 10-30%, meaning most paid leads ($10-$170+ each) never convert.',
    faqs: [
      {
        question: 'Why are freelancers leaving Thumbtack?',
        answer: 'Cost and competition are the primary complaints. Leads cost $10-$170+ each, conversion rates hover around 10-30%, and each lead is sent to up to 10 competing professionals. Many freelancers report spending hundreds on leads with minimal return, reflected in Thumbtack\'s 2.6/5 Trustpilot rating.',
      },
      {
        question: 'Is Bark better than Thumbtack?',
        answer: 'Bark uses a similar pay-per-lead model but typically sends leads to fewer competing professionals. Lead credits range from $14-$65 depending on the service category. Whether it is "better" depends on your category. Some freelancers report higher conversion rates on Bark due to less competition per lead.',
      },
      {
        question: 'How does skill exchange eliminate lead fees?',
        answer: 'On a platform like SkillLedger, you do not pay for leads. Instead, you offer your professional skills and receive services from other professionals in return. There is no per-lead cost, no commission on exchanges, and no conversion gamble. Both parties agree to the exchange before any work begins.',
      },
    ],
  },
  {
    slug: 'skillledger-vs-taskrabbit',
    title: 'SkillLedger vs. TaskRabbit: Skill Exchange vs. Task Marketplace',
    description: 'Compare SkillLedger professional skill exchange with TaskRabbit task marketplace. TaskRabbit charges a 15% service fee on every job, while SkillLedger enables commission-free skill-for-skill trades.',
    sideA: {
      name: 'SkillLedger',
      strengths: [
        'Zero commission on exchanges. Keep the full value of your work',
        'Credit-based system eliminates cash outlay for services you need',
        'Professional-grade escrow and dispute resolution included',
        'Covers 19 professional skill categories including software, design, and consulting',
        'Tax compliance built in with FMV tracking and 1099-B reporting',
      ],
      weaknesses: [
        'Focused on professional services, not designed for physical tasks like moving or handyman work',
        'Newer platform building its professional network',
      ],
      pricing: 'From $19/mo (30-day trial)',
    },
    sideB: {
      name: 'TaskRabbit',
      strengths: [
        'Strong marketplace for physical tasks: furniture assembly, moving, cleaning, handyman work',
        'IKEA partnership provides steady furniture assembly demand',
        'Same-day task availability in major metros',
        'Established brand owned by IKEA (Ingka Group) since 2017',
      ],
      weaknesses: [
        '15% service fee charged to clients on every booking',
        'Taskers also pay a registration fee and background check cost',
        'Primarily physical and household tasks, with limited professional service categories',
        'Taskers report inconsistent demand and algorithmic ranking frustrations',
        'No escrow or structured dispute resolution for quality disagreements',
      ],
      pricing: '15% service fee on every booking + Tasker registration fees',
    },
    verdict: 'TaskRabbit dominates physical tasks like furniture assembly and moving thanks to its IKEA partnership and same-day availability. SkillLedger is built for a different market: professional knowledge workers who want to exchange software development, design, consulting, and other high-value services without paying commissions or spending cash.',
    keyStatistic: 'TaskRabbit charges a 15% service fee on every task, plus Taskers pay their own expenses.',
    faqs: [
      {
        question: 'Can I use TaskRabbit for professional services like web design?',
        answer: 'TaskRabbit is primarily designed for physical tasks and handyman services. While some Taskers offer basic tech help, the platform lacks features like escrow, structured project management, and professional verification that knowledge workers need for high-value service exchanges.',
      },
      {
        question: 'Does TaskRabbit take a percentage of Tasker earnings?',
        answer: 'TaskRabbit charges a 15% service fee to clients on every booking. Taskers keep their hourly rate but pay an initial registration fee and background check cost. The effective cost to the client includes the service fee on top of the Tasker rate.',
      },
      {
        question: 'Why would a professional choose SkillLedger over TaskRabbit?',
        answer: 'Professionals with marketable skills (developers, designers, consultants, marketers) can obtain services they need without spending cash by exchanging their expertise on SkillLedger. TaskRabbit requires cash payment for every job, and its service categories focus on physical rather than knowledge work.',
      },
    ],
  },
  {
    slug: 'skillledger-vs-barteronly',
    title: 'SkillLedger vs. BarterOnly: Professional Platform vs. General Barter',
    description: 'Compare SkillLedger professional skill exchange with BarterOnly general barter marketplace. SkillLedger offers escrow, tax compliance, and professional verification that BarterOnly lacks.',
    sideA: {
      name: 'SkillLedger',
      strengths: [
        'Built-in escrow protects both parties on every exchange',
        'Professional verification ensures credential authenticity',
        'Automatic FMV tracking and 1099-B compliance reporting',
        'Structured dispute resolution with neutral mediators',
        'Credit system eliminates the need for direct swap matching',
      ],
      weaknesses: [
        'Focused on professional services, not general goods or household items',
        'Premium features require a paid subscription',
      ],
      pricing: 'From $19/mo (30-day trial)',
    },
    sideB: {
      name: 'BarterOnly',
      strengths: [
        'Broad barter categories including goods, vehicles, real estate, and services',
        'Free to list items and services for barter',
        'Simple posting interface similar to classified ads',
        'Long-running platform with an established community',
      ],
      weaknesses: [
        'No escrow or payment protection for transactions',
        'No professional verification or credential checking',
        'No tax compliance tools. Users must track FMV manually',
        'Classified-ad format means no structured project management',
        'No dispute resolution mechanism beyond user-to-user negotiation',
      ],
      pricing: 'Free basic listing',
    },
    verdict: 'BarterOnly works well for casual trades, like swapping a bicycle for a guitar or exchanging general goods. SkillLedger is purpose-built for professionals who need escrow protection, tax compliance, and credential verification when exchanging high-value services like software development, design, or consulting.',
    keyStatistic: 'BarterOnly operates as a classified-ad marketplace with no escrow, no verification, and no tax compliance tools for professional exchanges.',
    faqs: [
      {
        question: 'Is BarterOnly free to use?',
        answer: 'BarterOnly offers free basic listings for barter trades. The platform operates as a classified-ad style marketplace where users post what they have and what they want, then negotiate directly. There are no escrow, verification, or project management features included.',
      },
      {
        question: 'Can I barter professional services on BarterOnly?',
        answer: 'You can list professional services on BarterOnly, but the platform provides no escrow protection, dispute resolution, or professional verification. For high-value service exchanges where trust and accountability matter, SkillLedger provides the infrastructure that general barter sites lack.',
      },
      {
        question: 'Which platform handles taxes better?',
        answer: 'SkillLedger automatically tracks fair market value for every exchange and generates 1099-B-compatible reports. BarterOnly provides no tax documentation tools, so users must manually calculate and report the FMV of services and goods exchanged.',
      },
    ],
  },
  {
    slug: 'skillledger-vs-timebanking',
    title: 'SkillLedger vs. Time Banking Apps: Credits vs. Hours',
    description: 'Compare SkillLedger credit-based skill exchange with time banking apps. Time banking treats all hours as equal, while SkillLedger credits reflect actual market value of specialized skills.',
    sideA: {
      name: 'SkillLedger',
      strengths: [
        'Credits reflect fair market value, so specialist work is valued accordingly',
        'Professional-grade features: escrow, dispute resolution, project management',
        'Tax compliance with automatic FMV tracking and 1099-B reporting',
        'No requirement for direct swaps. Spend credits with any member',
        'Professional verification and reputation system',
      ],
      weaknesses: [
        'Premium features require a subscription',
        'Not designed for community volunteering or non-professional exchanges',
      ],
      pricing: 'From $19/mo (30-day trial)',
    },
    sideB: {
      name: 'Time Banking Apps',
      strengths: [
        'Egalitarian philosophy: every hour of work is valued equally regardless of skill',
        'Strong community focus, often organized by local nonprofits',
        'Completely free to participate in most time banks',
        'Low barrier to entry. Anyone can contribute',
      ],
      weaknesses: [
        'One hour of brain surgery equals one hour of lawn mowing, which discourages specialists',
        'Most time banks have very small, geographically limited membership',
        'No escrow or payment protection for work quality',
        'No professional verification or credentialing',
        'Limited to direct hour-for-hour exchanges with no credit flexibility',
        'Tax compliance is unclear and rarely addressed by platforms',
      ],
      pricing: 'Free (community-run)',
    },
    verdict: 'Time banking is a strong model for community building and volunteerism where egalitarian values matter more than market efficiency. SkillLedger is the better choice when the work has significant market value differences. A senior developer and a junior assistant should not trade hour-for-hour, and SkillLedger credits ensure fair value for specialized expertise.',
    keyStatistic: 'Time banking treats 1 hour of brain surgery as equal to 1 hour of lawn mowing, a model that discourages specialist participation.',
    faqs: [
      {
        question: 'What is the main difference between time banking and credit exchange?',
        answer: 'Time banking values all hours equally regardless of skill. One hour of legal advice equals one hour of gardening. SkillLedger credits reflect fair market value, so an hour of specialized consulting might be worth several hours of general administrative work. This accurately represents the market reality of professional services.',
      },
      {
        question: 'Are time banking exchanges taxable?',
        answer: 'The IRS considers barter exchanges taxable at fair market value, which creates a compliance challenge for time banks since they do not track FMV. SkillLedger automatically tracks fair market value for every exchange and provides 1099-B-ready documentation.',
      },
      {
        question: 'Can I use both time banking and SkillLedger?',
        answer: 'Yes. Many professionals use time banking for community service and neighborly exchanges while using SkillLedger for higher-value professional service trades where fair market valuation and escrow protection matter.',
      },
    ],
  },
  {
    slug: 'skillledger-vs-contra',
    title: 'SkillLedger vs. Contra: Credit Exchange vs. Commission-Free Cash',
    description: 'Compare SkillLedger skill exchange with Contra commission-free freelancing. Contra charges clients a fee while SkillLedger enables direct skill-for-skill trades with no cash needed.',
    sideA: {
      name: 'SkillLedger',
      strengths: [
        'Exchange skills directly without spending cash',
        'Credit system means you never need a direct swap partner',
        'Built-in escrow and dispute resolution protect every exchange',
        'Professional verification and reputation badges',
        'Tax compliance with FMV tracking and 1099-B reporting',
      ],
      weaknesses: [
        'Credits cannot be withdrawn as cash',
        'Best suited for professionals who both offer and need services',
      ],
      pricing: 'From $19/mo (30-day trial)',
    },
    sideB: {
      name: 'Contra',
      strengths: [
        'Commission-free for freelancers. 0% seller fee',
        'Clean portfolio builder with public profile pages',
        'Focused on independent professionals and agencies',
        'Cash-based payments with no platform commission to freelancers',
      ],
      weaknesses: [
        'Clients pay a service fee (up to 5%) on top of project costs',
        'Still requires cash outlay. No barter or credit exchange option',
        'Smaller marketplace than Upwork or Fiverr',
        'Limited dispute resolution compared to platforms with escrow',
        'No tax compliance tools for barter. Not applicable since cash-only',
      ],
      pricing: '0% freelancer fee; up to 5% client service fee',
    },
    verdict: 'Contra is excellent for freelancers who want commission-free cash income and a polished portfolio presence. SkillLedger fills a different need: professionals who want to obtain services without spending cash by exchanging their own expertise. Use Contra when you need revenue, and SkillLedger when you need services.',
    keyStatistic: 'Contra charges 0% commission to freelancers but passes up to 5% service fee to clients on every project.',
    faqs: [
      {
        question: 'Does Contra really charge 0% commission to freelancers?',
        answer: 'Yes. Contra does not take a percentage from freelancer earnings. Instead, clients pay a service fee (up to 5%) on top of the project cost. This is a meaningful difference from Fiverr (20%) and Upwork (10%), though freelancers still need clients who are willing to pay cash.',
      },
      {
        question: 'Can I use Contra and SkillLedger together?',
        answer: 'Yes. Many professionals use Contra for cash income and SkillLedger to obtain services they need without spending that cash. For example, a developer might take paid projects on Contra and exchange development work for marketing services on SkillLedger.',
      },
      {
        question: 'Which platform is better for building a portfolio?',
        answer: 'Contra offers polished public portfolio pages that function as a professional website. SkillLedger focuses on exchange mechanics rather than portfolio display. For portfolio visibility, Contra is stronger. For obtaining services without cash, SkillLedger is the clear choice.',
      },
    ],
  },
  {
    slug: 'skillledger-vs-freelancer-com',
    title: 'SkillLedger vs. Freelancer.com: Skill Barter vs. Contest Bidding',
    description: 'Compare SkillLedger credit-based skill exchange with Freelancer.com contest and bidding platform. Freelancer.com charges up to 10% freelancer fees, while SkillLedger enables zero-commission exchanges.',
    sideA: {
      name: 'SkillLedger',
      strengths: [
        'Zero commission on skill exchanges',
        'Credit-based system eliminates cash competition and race-to-the-bottom pricing',
        'Escrow with structured dispute resolution and neutral mediators',
        'Professional verification with credential checking',
        'FMV tracking and 1099-B tax compliance',
      ],
      weaknesses: [
        'Credits stay on-platform. Not convertible to cash',
        'Smaller marketplace than Freelancer.com\'s 70M+ registered users',
      ],
      pricing: 'From $19/mo (30-day trial)',
    },
    sideB: {
      name: 'Freelancer.com',
      strengths: [
        'Over 70 million registered users worldwide',
        'Contest format lets clients receive multiple submissions before choosing',
        'Wide range of project categories from development to data entry',
        'Both hourly and fixed-price project structures',
      ],
      weaknesses: [
        'Freelancers pay 10% commission (or $5 minimum) on every project',
        'Contest model means freelancers do speculative work with no guarantee of payment',
        'Race-to-the-bottom bidding drives down rates for quality professionals',
        'Enterprise fees can reach 3% on top of freelancer commission',
        'Dispute resolution process criticized as slow and opaque',
      ],
      pricing: '10% freelancer fee (or $5 min) + optional contest fees',
    },
    verdict: 'Freelancer.com provides access to a massive global talent pool but charges 10% commission and encourages price competition that undervalues professional work. SkillLedger takes a fundamentally different approach: instead of bidding for cash work, you exchange professional skills directly, keeping the full value of your expertise.',
    keyStatistic: 'Freelancer.com charges freelancers 10% commission (or $5 minimum) on every project, plus contest participants do speculative work with no payment guarantee.',
    faqs: [
      {
        question: 'How does Freelancer.com\'s contest model work?',
        answer: 'Clients post a brief and budget. Multiple freelancers submit completed work (logos, designs, code samples). The client picks a winner who gets paid. Everyone else worked for free. This speculative work model is controversial because it devalues professional labor.',
      },
      {
        question: 'Is Freelancer.com cheaper than SkillLedger?',
        answer: 'Freelancer.com involves real cash: clients pay for projects and freelancers lose 10% to commission. SkillLedger uses credits, so you can obtain services without cash outlay by exchanging your own skills. The "cost" on SkillLedger is your time and expertise rather than money.',
      },
      {
        question: 'Which platform is better for high-value professional work?',
        answer: 'For premium professional services, SkillLedger offers better protection through escrow, credential verification, and dispute resolution. Freelancer.com\'s bidding model tends to attract price-sensitive buyers, which can undervalue specialized expertise.',
      },
    ],
  },
  {
    slug: 'skillledger-vs-99designs',
    title: 'SkillLedger vs. 99designs: Collaborative Exchange vs. Design Contests',
    description: 'Compare SkillLedger skill exchange with 99designs contest platform for designers. 99designs takes a significant cut of designer earnings, while SkillLedger enables zero-commission skill trades.',
    sideA: {
      name: 'SkillLedger',
      strengths: [
        'Zero commission. Designers keep the full value of their work',
        'Exchange design skills for development, marketing, legal, or any other professional service',
        'Escrow and dispute resolution protect both parties',
        'One-to-one professional relationships instead of anonymous contests',
        'Professional verification and portfolio showcase',
      ],
      weaknesses: [
        'Credits stay on-platform. Not convertible to cash income',
        'Requires finding professionals who need design services',
      ],
      pricing: 'From $19/mo (30-day trial)',
    },
    sideB: {
      name: '99designs',
      strengths: [
        'Dedicated design marketplace with strong brand recognition',
        'Contest format delivers multiple design concepts to choose from',
        'Vetted designer community with quality standards',
        'Owned by Vistaprint (now Vista) with stable corporate backing',
      ],
      weaknesses: [
        'Platform takes a significant share. Designer payouts are roughly 60-75% of client spend',
        'Contest model means designers create speculative work with no payment guarantee',
        'Top designers avoid contests, leaving less experienced talent in the pool',
        'One-on-one projects available but at premium pricing',
        'Limited to design. No cross-disciplinary service exchange',
      ],
      pricing: 'Designer receives ~60-75% of contest price; 1-on-1 projects at premium rates',
    },
    verdict: '99designs works when clients want multiple design options quickly and are willing to pay for a contest. SkillLedger is the better choice for designers who want to obtain non-design services (development, accounting, legal) by exchanging their design expertise. No cash required, no contest speculation, and no platform commission.',
    keyStatistic: '99designs pays designers roughly 60-75% of what clients spend. Non-winning contest participants receive nothing for their submitted work.',
    faqs: [
      {
        question: 'How much does 99designs take from designer earnings?',
        answer: 'The exact split varies by contest level, but designers typically receive 60-75% of what the client pays. The platform and infrastructure take the remainder. On a $500 logo contest, the winning designer might receive $300-$375. Non-winning designers receive nothing for their submitted work.',
      },
      {
        question: 'Can designers use SkillLedger to get non-design services?',
        answer: 'Yes, that is the core value proposition. A designer can exchange logo work for a developer to build their portfolio site, or trade branding services for accounting help with their freelance taxes. The credit system means you do not need a direct swap partner.',
      },
      {
        question: 'Are design contests ethical?',
        answer: 'This is debated in the design community. Critics argue that contests devalue design labor by asking multiple designers to work without pay. Proponents say contests give emerging designers exposure. SkillLedger avoids this debate entirely. Every exchange is one-to-one with agreed terms and escrow protection.',
      },
    ],
  },
]

export function getComparisonBySlug(slug: string): ComparisonData | undefined {
  return comparisonsData.find((c) => c.slug === slug)
}

export function getAllComparisonSlugs(): string[] {
  return comparisonsData.map((c) => c.slug)
}
