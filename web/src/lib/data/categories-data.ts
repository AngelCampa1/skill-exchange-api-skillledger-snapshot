export interface CategoryData {
  slug: string
  name: string
  description: string
  longDescription: string
  sampleSkills: string[]
  averageCreditRate: number // credits per hour
  demandLevel: 'high' | 'medium' | 'low'
  faqs: Array<{ question: string; answer: string }>
}

export const categoriesData: CategoryData[] = [
  {
    slug: 'web-development',
    name: 'Web Development',
    description: 'Exchange frontend, backend, and full-stack development skills for services you actually need.',
    longDescription: 'Web development tops the demand charts on SkillLedger. Developers working in React, Next.js, Node.js, and .NET trade hours of coding for design, marketing, and business services that would otherwise eat into their project budgets.',
    sampleSkills: ['React', 'Next.js', 'Node.js', 'TypeScript', 'PostgreSQL', 'REST APIs'],
    averageCreditRate: 85,
    demandLevel: 'high',
    faqs: [
      { question: 'What web development skills are most in demand on SkillLedger?', answer: 'React, Next.js, and full-stack TypeScript pull the most collaboration requests right now. Backend skills like Node.js, PostgreSQL, and REST API work also get steady interest.' },
      { question: 'How are web development credits calculated?', answer: 'You set your own hourly rate. Most web developers on SkillLedger price between 60 and 120 credits per hour, depending on their specialization and track record.' },
      { question: 'Can I exchange web development for design work?', answer: 'Yes, and it happens constantly. A developer building a portfolio site trades with a graphic designer who needs brand identity work. Both sides walk away with something they needed.' },
    ],
  },
  {
    slug: 'design',
    name: 'Design',
    description: 'Trade graphic design, UI/UX, branding, and visual identity services with other professionals.',
    longDescription: 'Every business eventually needs a logo, a UI overhaul, or marketing visuals. Designers on SkillLedger trade with developers, writers, and marketers to assemble full client solutions without subcontracting for cash.',
    sampleSkills: ['Logo Design', 'Brand Identity', 'UI/UX', 'Figma', 'Illustration', 'Motion Graphics'],
    averageCreditRate: 75,
    demandLevel: 'high',
    faqs: [
      { question: 'What design services can I offer on SkillLedger?', answer: 'Anything professional: logo and brand identity, UI/UX design, web mockups, print collateral, social media graphics, illustration, and motion graphics.' },
      { question: 'How do designers value their work in credits?', answer: 'Most designers charge 70 to 100 credits per hour, or set project-based prices. A logo package typically runs 300 to 500 credits total.' },
      { question: 'Can I trade design work for marketing services?', answer: 'Yes. Designer-marketer exchanges happen all the time. You create the marketing assets; they run social campaigns for your business.' },
    ],
  },
  {
    slug: 'marketing',
    name: 'Marketing',
    description: 'Exchange SEO, content marketing, social media, and paid advertising expertise.',
    longDescription: 'Technical professionals often put off marketing because the cash cost feels steep. On SkillLedger, developers and designers trade their technical skills for SEO, content strategy, social media management, and paid advertising that actually grows their client base.',
    sampleSkills: ['SEO', 'Content Strategy', 'Social Media', 'Google Ads', 'Email Marketing', 'Analytics'],
    averageCreditRate: 70,
    demandLevel: 'high',
    faqs: [
      { question: 'What marketing skills are most traded on SkillLedger?', answer: 'SEO and content marketing lead the pack, followed by social media management and email marketing. Paid advertising specialists (Google Ads, Meta) also draw strong exchange requests.' },
      { question: 'How long does a marketing engagement typically last?', answer: 'Most marketing exchanges run 1 to 3 months. SEO work, in particular, takes sustained effort before results show. SkillLedger supports ongoing exchange agreements for exactly this reason.' },
    ],
  },
  {
    slug: 'writing',
    name: 'Writing & Content',
    description: 'Trade copywriting, content writing, technical writing, and editorial services.',
    longDescription: 'Website copy, blog posts, technical docs, email sequences, social captions: every business runs on written content. Writers on SkillLedger trade content creation for development, design, and marketing services they would otherwise have to pay cash for.',
    sampleSkills: ['Copywriting', 'Blog Writing', 'Technical Writing', 'SEO Content', 'Email Sequences', 'UX Writing'],
    averageCreditRate: 60,
    demandLevel: 'high',
    faqs: [
      { question: 'What writing services trade best on SkillLedger?', answer: 'Website and landing page copy, SEO blog content, and email marketing sequences pull the most requests consistently.' },
      { question: 'How do writers price their services in credits?', answer: 'Writers typically charge by word (1 to 3 credits per word) or by project (500 to 2,000 credits for a full website). Blog posts usually run 200 to 500 credits depending on length and research depth.' },
    ],
  },
  {
    slug: 'consulting',
    name: 'Business Consulting',
    description: 'Exchange business strategy, operations, finance, and leadership consulting.',
    longDescription: 'Hiring a business consultant at cash rates can run $200 to $500 per hour. SkillLedger lets founders and operators access strategic advice by trading their own professional skills instead.',
    sampleSkills: ['Business Strategy', 'Operations', 'Financial Planning', 'Product Strategy', 'Growth Consulting', 'HR & Culture'],
    averageCreditRate: 120,
    demandLevel: 'medium',
    faqs: [
      { question: 'What consulting services are available on SkillLedger?', answer: 'Business strategy, operations, financial planning, product management, growth strategy, and HR consulting all have active listings.' },
      { question: 'How does consulting exchange work?', answer: 'Most consultants offer hourly advisory sessions at 120 to 200 credits per hour, or structured engagements with defined deliverables and milestones.' },
    ],
  },
  {
    slug: 'video-production',
    name: 'Video Production',
    description: 'Trade video production, editing, animation, and motion graphics expertise.',
    longDescription: 'Video drives engagement on every platform, but production costs add up fast. Video producers and editors on SkillLedger trade production work for web development, marketing, and design services.',
    sampleSkills: ['Video Editing', 'Motion Graphics', 'YouTube Production', 'Commercial Video', 'Social Media Video', 'Animation'],
    averageCreditRate: 80,
    demandLevel: 'medium',
    faqs: [
      { question: 'What video services trade on SkillLedger?', answer: 'Video editing, motion graphics, YouTube production packages, short-form social content, and promotional videos are the most common listings.' },
    ],
  },
  {
    slug: 'photography',
    name: 'Photography',
    description: 'Exchange professional photography for headshots, products, events, and commercial use.',
    longDescription: 'Photographers typically need websites and branding but would rather trade a shoot than pay cash. On SkillLedger, a photographer trades a headshot session or product shoot for a professionally built portfolio site.',
    sampleSkills: ['Portrait Photography', 'Product Photography', 'Event Coverage', 'Real Estate Photography', 'Brand Photography'],
    averageCreditRate: 70,
    demandLevel: 'medium',
    faqs: [
      { question: 'Can I trade photography for website design?', answer: 'Yes, and it is one of the most natural exchanges on the platform. Photographers trade a shoot for a portfolio site. Both sides get exactly what they need.' },
    ],
  },
  {
    slug: 'music-audio',
    name: 'Music & Audio',
    description: 'Trade music production, sound design, podcast editing, and voice-over services.',
    longDescription: 'Podcasts, YouTube channels, apps, and brand content all need audio work. Music and audio professionals trade production skills for the marketing and development help that builds their client pipeline.',
    sampleSkills: ['Music Production', 'Podcast Editing', 'Sound Design', 'Voice Over', 'Jingles', 'Audio Mastering'],
    averageCreditRate: 65,
    demandLevel: 'low',
    faqs: [
      { question: 'What audio services are most in demand?', answer: 'Podcast editing and production, voice-over recording, and background music for video content get the most exchange requests.' },
    ],
  },
  {
    slug: 'data-science',
    name: 'Data Science & Analytics',
    description: 'Exchange data analysis, machine learning, and business intelligence expertise.',
    longDescription: 'Data science commands some of the highest credit rates on SkillLedger. Analysts and data scientists trade their expertise for marketing, content, and development services that help them build a broader professional presence.',
    sampleSkills: ['Data Analysis', 'Python', 'Machine Learning', 'Business Intelligence', 'SQL', 'Visualization'],
    averageCreditRate: 100,
    demandLevel: 'medium',
    faqs: [
      { question: 'What data skills are most valuable on SkillLedger?', answer: 'Data analysis, Python scripting, and business intelligence work (dashboards, KPI reporting) see the most exchange activity.' },
    ],
  },
  {
    slug: 'ai-ml',
    name: 'AI & Machine Learning',
    description: 'Trade AI/ML development, LLM integration, and automation expertise.',
    longDescription: 'AI expertise is the fastest-growing category on SkillLedger. AI and ML engineers trade automation and integration work for design, content, and business development services that round out their offerings.',
    sampleSkills: ['LLM Integration', 'AI Automation', 'Prompt Engineering', 'Computer Vision', 'NLP', 'RAG Systems'],
    averageCreditRate: 120,
    demandLevel: 'high',
    faqs: [
      { question: 'What AI skills are most in demand on SkillLedger?', answer: 'LLM integration, AI workflow automation, and RAG (retrieval-augmented generation) system development are growing fastest right now.' },
    ],
  },
  {
    slug: 'mobile-development',
    name: 'Mobile Development',
    description: 'Exchange iOS, Android, and cross-platform mobile app development.',
    longDescription: 'Mobile developers trade app work for marketing, design, and business services that help them grow their freelance practices or launch their own products.',
    sampleSkills: ['React Native', 'iOS (Swift)', 'Android (Kotlin)', 'Flutter', 'App Store Optimization'],
    averageCreditRate: 90,
    demandLevel: 'medium',
    faqs: [
      { question: 'What mobile platforms are covered?', answer: 'iOS (Swift/SwiftUI), Android (Kotlin), React Native, and Flutter development all have active listings.' },
    ],
  },
  {
    slug: 'legal',
    name: 'Legal Services',
    description: 'Trade contract review, IP protection, business formation, and legal consulting.',
    longDescription: 'Legal help is expensive at cash rates. Lawyers and legal professionals on SkillLedger trade reviews and consulting for tech, design, and marketing services, making legal guidance accessible to freelancers who need it.',
    sampleSkills: ['Contract Review', 'IP Protection', 'Business Formation', 'Terms of Service', 'Privacy Policy', 'NDA Drafting'],
    averageCreditRate: 150,
    demandLevel: 'high',
    faqs: [
      { question: 'Is legal advice on SkillLedger attorney-client privileged?', answer: 'That depends on the specific attorney and jurisdiction. Always clarify the nature of the relationship with any legal professional before sharing confidential information.' },
    ],
  },
  {
    slug: 'finance',
    name: 'Finance & Accounting',
    description: 'Exchange bookkeeping, tax preparation, financial planning, and CFO services.',
    longDescription: 'Financial professionals trade accounting and advisory services for technology and marketing help that grows their own practices. Freelancers get bookkeeping and tax prep without the cash outlay.',
    sampleSkills: ['Bookkeeping', 'Tax Preparation', 'Financial Modeling', 'CFO Advisory', 'Payroll', 'Fundraising Support'],
    averageCreditRate: 100,
    demandLevel: 'medium',
    faqs: [
      { question: 'Can accountants exchange tax services on SkillLedger?', answer: 'Yes. Tax preparation and bookkeeping are exchangeable like any other professional service. Note that the IRS treats barter income as taxable at fair market value.' },
    ],
  },
  {
    slug: 'engineering',
    name: 'Engineering',
    description: 'Trade civil, mechanical, electrical, and systems engineering expertise.',
    longDescription: 'Engineering professionals trade specialized technical knowledge for business, marketing, and digital services they need but rarely have in-house.',
    sampleSkills: ['Systems Engineering', 'CAD Design', 'Technical Documentation', 'Process Engineering', 'Quality Assurance'],
    averageCreditRate: 110,
    demandLevel: 'low',
    faqs: [
      { question: 'What engineering services trade on SkillLedger?', answer: 'Technical consulting, CAD design, systems documentation, and process engineering are the most active engineering categories.' },
    ],
  },
  {
    slug: 'business',
    name: 'Business Development',
    description: 'Exchange sales, partnerships, lead generation, and business development expertise.',
    longDescription: 'Business development professionals trade pipeline-building skills for the technology and marketing services that support their outreach and close rates.',
    sampleSkills: ['Sales Strategy', 'Lead Generation', 'Partnership Development', 'CRM Management', 'Account Management'],
    averageCreditRate: 85,
    demandLevel: 'medium',
    faqs: [
      { question: 'What business development services are most traded?', answer: 'Sales strategy consulting, LinkedIn outreach, lead generation, and partnership development are the most common exchanges.' },
    ],
  },
  {
    slug: 'healthcare-wellness',
    name: 'Healthcare & Wellness',
    description: 'Exchange wellness coaching, therapy consultation, massage therapy, and holistic health services.',
    longDescription: 'Healthcare and wellness professionals barter more than almost any other group. 75% of massage therapists report bartering services, averaging nine transactions per year. On SkillLedger, wellness professionals trade coaching, bodywork, and holistic health services for development, design, and marketing expertise while staying compliant with HIPAA, state licensing boards, and professional ethics codes.',
    sampleSkills: ['Wellness Coaching', 'Nutrition Consulting', 'Yoga Instruction', 'Massage Therapy', 'Mental Health Consulting', 'Fitness Training'],
    averageCreditRate: 90,
    demandLevel: 'medium',
    faqs: [
      { question: 'Is it legal for therapists to barter services?', answer: 'It depends on your profession and state. APA Ethics Code Section 6.05 permits psychologists to barter if it is not clinically contraindicated and not exploitative. However, Texas LPCs face an outright prohibition, while social workers (NASW 1.13(b)) can barter only in very limited circumstances. Always check your specific licensing board\'s rules.' },
      { question: 'Do I need a HIPAA agreement for healthcare barter?', answer: 'Only if your barter partner accesses Protected Health Information. A purely personal exchange (therapy for a personal website) creates no HIPAA obligation. If the partner works on your practice website and accesses scheduling systems or patient data, they likely qualify as a business associate requiring a BAA under 45 CFR 164.502(e) and 164.504(e).' },
      { question: 'How are bartered healthcare services taxed?', answer: 'Identically to cash payments. The IRS makes no distinction between healthcare barter and any other barter. All exchanged services must be reported at fair market value under IRC 61. The malpractice standard of care does not change based on payment method either.' },
    ],
  },
  {
    slug: 'real-estate',
    name: 'Real Estate',
    description: 'Exchange real estate photography, marketing, staging, and transaction coordination services.',
    longDescription: 'Real estate professionals need marketing, photography, videography, and web development but face income cycles that make cash spending unpredictable. SkillLedger lets agents, brokers, and property managers trade real estate expertise for the creative and technical services that drive listings and closings.',
    sampleSkills: ['Real Estate Photography', 'Property Marketing', 'Virtual Staging', 'Transaction Coordination', 'Property Management', 'Real Estate Copywriting'],
    averageCreditRate: 85,
    demandLevel: 'medium',
    faqs: [
      { question: 'Can real estate agents barter their commission services?', answer: 'Agents must comply with state licensing laws and brokerage agreements when bartering. Most states require that any compensation, including barter, flow through the licensed brokerage. Check your state real estate commission regulations and broker-agent agreement before listing.' },
      { question: 'What real estate services trade best on SkillLedger?', answer: 'Property photography, virtual staging, listing copywriting, social media marketing for agents, and website development for real estate teams are the most active categories.' },
      { question: 'Are bartered real estate services taxable?', answer: 'Yes. The fair market value of all bartered services must be reported as income under IRS rules. If a photographer trades a listing shoot valued at $500 for $500 of marketing services, both parties report $500 in barter income.' },
    ],
  },
  {
    slug: 'non-profit',
    name: 'Non-Profit',
    description: 'Exchange grant writing, fundraising strategy, volunteer management, and non-profit marketing services.',
    longDescription: 'Non-profit organizations run on tight budgets but still need professional web development, design, marketing, and strategic consulting. SkillLedger lets non-profits trade their grant writing, community organizing, and social impact expertise for the technical and creative services their missions depend on.',
    sampleSkills: ['Grant Writing', 'Fundraising Strategy', 'Volunteer Management', 'Non-Profit Marketing', 'Impact Measurement', 'Community Organizing'],
    averageCreditRate: 65,
    demandLevel: 'low',
    faqs: [
      { question: 'Can non-profit organizations participate in barter exchanges?', answer: 'Yes. Non-profits can barter services like any other organization. However, the fair market value of services received may need to be reported as unrelated business income (UBI) if the barter activity is regularly carried on and not substantially related to the exempt purpose.' },
      { question: 'How do non-profits value volunteer hours versus bartered services?', answer: 'They are legally distinct. Bartered services involve a reciprocal exchange of value and are taxable. Volunteer contributions are one-directional and not taxable income. Non-profits should document the distinction clearly.' },
      { question: 'What services do non-profits trade most on SkillLedger?', answer: 'Grant writing, event planning, and community engagement expertise are the most commonly traded for web development, graphic design, and digital marketing services.' },
    ],
  },
  {
    slug: 'content-creators',
    name: 'Content Creators',
    description: 'Exchange video production, podcasting, social media content, and influencer marketing services.',
    longDescription: 'Content creators need video editing, web development, graphic design, and SEO, but many operate solo without budgets for professional help. SkillLedger lets creators trade their audience reach and production skills for the technical services that raise their output quality.',
    sampleSkills: ['YouTube Production', 'Podcast Production', 'Social Media Content', 'Influencer Marketing', 'Livestream Production', 'Content Strategy'],
    averageCreditRate: 75,
    demandLevel: 'high',
    faqs: [
      { question: 'How do content creators value their services for barter?', answer: 'Creators typically price based on their standard rates for sponsored content, production work, or consulting. A creator who charges $500 for a sponsored Instagram post would set their exchange rate to reflect that same market value.' },
      { question: 'Can creators trade audience exposure for professional services?', answer: 'Yes, but structure it carefully. The value of audience exposure (mentions, features, collaborations) should be estimated from the creator\'s engagement rates and the market value of equivalent advertising reach.' },
      { question: 'What services do content creators need most?', answer: 'Video editing, thumbnail design, SEO for YouTube and blog content, website development, and social media growth strategy are the most requested services from creators on SkillLedger.' },
    ],
  },
]

export function getCategoryBySlug(slug: string): CategoryData | undefined {
  return categoriesData.find((c) => c.slug === slug)
}
