export interface IndustryData {
  slug: string
  name: string
  description: string
  longDescription: string
  keyBenefits: string[]
  commonPairings: Array<{ skillOffered: string; skillNeeded: string; description: string }>
  regulatoryNotes: string
  keyStatistic?: string
  faqs: Array<{ question: string; answer: string }>
}

export const industriesData: IndustryData[] = [
  {
    slug: 'legal-professionals',
    name: 'Legal Professionals',
    description: 'Exchange professional services as a lawyer, paralegal, or legal consultant. Reduce overhead and access design, marketing, and technology services without cash outlay.',
    longDescription: `Lawyers and legal professionals operate in a high-value service economy where hourly rates typically range from $200 to $600 per hour. This pricing gap creates a real opportunity for skill-based barter: a single hour of legal counsel can be exchanged for multiple hours of web development, graphic design, or marketing services. SkillLedger provides the infrastructure for these exchanges while maintaining the documentation trail that legal professionals require.

Under ABA Model Rule 1.8(a), attorneys entering business transactions with clients must ensure the terms are fair, reasonable, and fully disclosed in writing. Revenue Ruling 79-24 established that the IRS treats bartered services as taxable income at fair market value (FMV), meaning both parties must report the value received. SkillLedger tracks all exchange values automatically, simplifying year-end 1099-B reporting for legal professionals who participate in skill barter.

For solo practitioners and small firms, skill exchange is a practical path to accessing expensive services like website redesign, brand identity development, SEO, and social media management. Rather than paying $15,000 or more in cash for a full rebrand, a business attorney might offer contract review, entity formation guidance, or compliance consulting in return. This approach preserves working capital while building cross-referral relationships that generate future billable work.`,
    keyBenefits: [
      'Reduce firm overhead by exchanging legal expertise for design, development, and marketing services',
      'Access high-quality branding and web presence without depleting operating capital',
      'Expand your practice area reach through collaborative relationships with other professionals',
      'Build referral networks with the designers, developers, and consultants who serve your ideal clients',
      'Maintain full compliance with IRS reporting requirements through automatic FMV tracking',
    ],
    commonPairings: [
      { skillOffered: 'Legal Review & Contract Drafting', skillNeeded: 'Web Development', description: 'Attorneys draft service agreements, terms of service, and privacy policies in exchange for a modern, responsive law firm website built on platforms like Next.js or WordPress.' },
      { skillOffered: 'Contract Drafting & Negotiation', skillNeeded: 'Branding & Identity Design', description: 'Business lawyers provide contract templates, NDA drafting, or partnership agreement review in exchange for logo design, visual identity systems, and brand guidelines.' },
      { skillOffered: 'Intellectual Property Counsel', skillNeeded: 'Software Development', description: 'IP attorneys offer trademark searches, patent strategy consultation, or licensing agreement review in exchange for custom software tools, practice management integrations, or mobile app development.' },
      { skillOffered: 'Compliance Consulting', skillNeeded: 'Digital Marketing & SEO', description: 'Regulatory compliance specialists trade audit preparation, policy drafting, or risk assessments for search engine optimization, Google Ads management, and content marketing strategies.' },
    ],
    regulatoryNotes: 'Legal professionals must comply with ABA Model Rule 1.8(a) governing business transactions with clients, which requires fair terms, written disclosure, and the opportunity for the client to seek independent counsel. State bar ethics opinions may impose additional requirements on barter arrangements. Under IRS Revenue Ruling 79-24, all bartered services must be reported at fair market value as taxable income. Attorneys should also evaluate conflict of interest implications under Model Rules 1.7 and 1.9 before entering barter arrangements with current or former clients.',
    keyStatistic: 'Under ABA Model Rule 1.8(a), attorneys may barter services provided terms are fair, in writing, and the client has opportunity to seek independent counsel.',
    faqs: [
      { question: 'Is it ethical for lawyers to barter legal services?', answer: 'Yes, provided the attorney complies with ABA Model Rule 1.8(a). The rule requires that business transactions with clients be fair and reasonable, fully disclosed in writing, and that the client has the opportunity to consult independent counsel. Many state bar associations have issued ethics opinions confirming that barter is permissible under these conditions.' },
      { question: 'How do lawyers report bartered services to the IRS?', answer: 'The fair market value of services received through barter must be reported as gross income under Revenue Ruling 79-24. SkillLedger tracks all exchange values and provides year-end summaries that simplify 1099-B reporting. Attorneys should consult a tax advisor for specific guidance on their situation.' },
      { question: 'What types of legal services work best for skill exchange?', answer: 'Transactional legal work tends to be the best fit: contract drafting, business entity formation, trademark applications, terms of service, privacy policies, and general business counsel. Litigation and court appearances are harder to value on an hourly exchange basis due to unpredictable time commitments.' },
    ],
  },
  {
    slug: 'healthcare-wellness',
    name: 'Healthcare & Wellness',
    description: 'Skill exchange for healthcare providers, wellness coaches, therapists, and nutritionists. Navigate HIPAA and licensing requirements while accessing business services.',
    longDescription: `Healthcare and wellness professionals bring specialized, high-demand skills to the barter economy. From licensed therapists and registered dietitians to certified wellness coaches and fitness trainers, these practitioners often need business services like web design, content marketing, and brand development but face tight margins that make cash payments difficult. Skill exchange through SkillLedger offers a structured alternative.

The healthcare sector carries significant regulatory requirements that affect how barter arrangements are structured. HIPAA (the Health Insurance Portability and Accountability Act) governs the handling of protected health information, meaning any exchange involving patient data or clinical workflows must maintain full compliance. The Stark Law and the federal Anti-Kickback Statute prohibit certain referral arrangements and kickback schemes in healthcare, so professionals must ensure that barter exchanges do not create prohibited financial relationships with referral sources.

State licensing boards impose additional constraints. Licensed practitioners must ensure that bartered services fall within their scope of practice and that any exchange does not compromise the standard of care owed to patients or clients. SkillLedger provides documentation and audit trails that help healthcare professionals demonstrate compliance with these requirements while accessing the business, technology, and creative services they need to grow their practices.`,
    keyBenefits: [
      'Access professional web design and marketing without straining practice cash flow',
      'Build a strong online presence with SEO-optimized content and patient education resources',
      'Exchange wellness expertise for technology services like booking systems and telehealth setup',
      'Maintain compliance documentation through structured exchange tracking and audit trails',
      'Connect with creative professionals who understand healthcare branding and messaging',
    ],
    commonPairings: [
      { skillOffered: 'Wellness Coaching & Program Design', skillNeeded: 'Web Design & Development', description: 'Certified wellness coaches offer personalized coaching programs, habit formation plans, or corporate wellness strategy in exchange for a professional website with booking integration and content management.' },
      { skillOffered: 'Nutritional Counseling & Meal Planning', skillNeeded: 'Content Creation & Copywriting', description: 'Registered dietitians and nutritionists provide meal plans, dietary assessments, or supplement guidance in exchange for blog posts, email newsletters, and social media content that attracts new clients.' },
      { skillOffered: 'Physical Therapy & Movement Assessment', skillNeeded: 'Video Production & Editing', description: 'Physical therapists and movement specialists trade ergonomic assessments, exercise programming, or injury prevention plans for professional video production of exercise demonstrations and patient education content.' },
      { skillOffered: 'Mental Health & Stress Management', skillNeeded: 'Graphic Design & Branding', description: 'Licensed counselors and therapists exchange stress management workshops, mindfulness training, or EAP consulting for brand identity design, therapy office signage, and marketing collateral.' },
    ],
    regulatoryNotes: 'Healthcare professionals must maintain HIPAA compliance in all exchanges involving protected health information. The Stark Law (42 U.S.C. 1395nn) and the Anti-Kickback Statute (42 U.S.C. 1320a-7b) prohibit certain financial relationships that could influence patient referrals. State licensing boards may restrict the types of services practitioners can offer outside clinical settings. All bartered services must be reported at fair market value per IRS requirements. Practitioners should consult both legal counsel and their licensing board before entering barter arrangements.',
    keyStatistic: 'The IRS requires fair market value reporting for all bartered medical services under Revenue Ruling 79-24.',
    faqs: [
      { question: 'Can healthcare providers legally barter their professional services?', answer: 'Yes, in most cases. Providers must ensure the exchange does not violate HIPAA, the Stark Law, or the Anti-Kickback Statute. Services offered through barter should fall within the provider\'s scope of practice, and the arrangement should not create a prohibited referral relationship. Consulting with a healthcare attorney before starting is strongly recommended.' },
      { question: 'How does HIPAA affect skill exchanges for healthcare professionals?', answer: 'HIPAA applies whenever protected health information (PHI) is involved. If you are exchanging wellness coaching or general health education that does not involve PHI, HIPAA concerns are minimal. If clinical services or patient data are part of the exchange, full HIPAA compliance including Business Associate Agreements may be required.' },
      { question: 'What wellness services are most commonly exchanged on SkillLedger?', answer: 'Non-clinical wellness services are the most popular: wellness coaching, nutritional counseling (non-medical), fitness programming, stress management workshops, ergonomic assessments, and corporate wellness consulting. These services carry fewer regulatory constraints than clinical healthcare and are easier to value on an hourly basis.' },
    ],
  },
  {
    slug: 'non-profit-organizations',
    name: 'Non-Profit Organizations',
    description: 'Skill exchange tailored for 501(c)(3) organizations, social enterprises, and mission-driven teams. Stretch limited budgets by trading skills instead of spending cash.',
    longDescription: `Non-profit organizations face a persistent challenge: they need professional-grade services like web development, graphic design, video production, and strategic marketing but often lack the budget to hire agencies or full-time staff. SkillLedger offers a structured skill exchange platform where non-profits can trade their unique expertise for the technical and creative services they need to advance their missions.

The distinction between in-kind donations and barter matters for non-profit accounting and IRS compliance. An in-kind donation is a one-way gift where the donor receives no services in return (and may claim a tax deduction). Barter, by contrast, is a two-way exchange where both parties give and receive services of roughly equal value. Under IRS rules, bartered services are reported as income by both parties, not as charitable contributions. Non-profits must report barter income on Form 990, and the value of services received through barter cannot be reported as donated services on the financial statements.

For non-profits with strong in-house capabilities in areas like grant writing, fundraising strategy, community organizing, program evaluation, or volunteer management, skill exchange unlocks access to services that would otherwise require $10,000 or more in cash outlays. A community health organization might trade grant writing expertise for a redesigned website. An arts non-profit could exchange event planning services for a donor management system. SkillLedger tracks all exchange values to support accurate Form 990 reporting and financial transparency.`,
    keyBenefits: [
      'Stretch limited budgets by exchanging organizational expertise for design, development, and marketing services',
      'Access professional-quality branding and web presence without diverting funds from program delivery',
      'Build capacity through partnerships with skilled professionals who understand mission-driven work',
      'Maintain IRS compliance with automatic tracking of barter values for Form 990 reporting',
      'Strengthen community connections by exchanging skills with local businesses and consultants',
    ],
    commonPairings: [
      { skillOffered: 'Grant Writing & Fundraising Strategy', skillNeeded: 'Graphic Design & Print Collateral', description: 'Non-profit development staff trade grant writing expertise, donor cultivation strategies, or capital campaign planning for annual report design, fundraising brochures, and event marketing materials.' },
      { skillOffered: 'Program Evaluation & Impact Measurement', skillNeeded: 'Web Development & CMS Setup', description: 'Organizations with strong evaluation teams offer logic model development, outcome measurement frameworks, or data analysis in exchange for a modern website with donation integration, event calendars, and volunteer signup forms.' },
      { skillOffered: 'Community Organizing & Outreach', skillNeeded: 'Video Production & Storytelling', description: 'Non-profits skilled in community engagement trade workshop facilitation, coalition building, or advocacy campaign strategy for documentary-style videos, donor testimonials, and social media video content.' },
      { skillOffered: 'Volunteer Management & Training', skillNeeded: 'Database & CRM Development', description: 'Organizations with proven volunteer programs exchange recruitment strategies, training curriculum design, or retention best practices for custom donor databases, CRM configuration, or Salesforce integration.' },
    ],
    regulatoryNotes: 'Non-profit organizations must distinguish between in-kind donations (one-way gifts eligible for donor tax deductions) and barter exchanges (two-way trades reported as income by both parties). Bartered services received by a 501(c)(3) cannot be recorded as donated services on financial statements. Barter income must be reported on IRS Form 990. Organizations should ensure that barter arrangements do not jeopardize their tax-exempt status by creating unrelated business taxable income (UBTI). Consult with a non-profit accountant or tax advisor before entering significant barter arrangements.',
    keyStatistic: 'Under IRS guidelines, donated services cannot be recorded as in-kind contributions on Form 990, but bartered services exchanged at fair market value may have different treatment.',
    faqs: [
      { question: 'How is barter different from an in-kind donation for non-profits?', answer: 'An in-kind donation is a one-way gift where the donor receives nothing in return and may claim a tax deduction. Barter is a two-way exchange where both parties give and receive services of roughly equal value. The IRS treats barter as taxable income for both parties. Non-profits must report barter transactions on Form 990 and cannot classify received barter services as donated services.' },
      { question: 'Can barter arrangements affect a non-profit\'s tax-exempt status?', answer: 'Potentially, if the barter activity generates unrelated business taxable income (UBTI). If the services exchanged are substantially related to the organization\'s exempt purpose, there is generally no UBTI concern. Organizations should consult a non-profit tax advisor to evaluate specific arrangements.' },
      { question: 'What non-profit skills are most valuable for barter exchanges?', answer: 'Grant writing, fundraising strategy, program evaluation, community organizing, volunteer management, and event planning are among the most sought-after non-profit skills. These capabilities are hard to find on the open market and command high rates when purchased from consultants, making them strong barter assets.' },
    ],
  },
  {
    slug: 'saas-startups',
    name: 'SaaS Startups',
    description: 'Skill exchange for bootstrapped SaaS founders, early-stage startups, and pre-funding teams. Trade development, design, and marketing skills to launch faster.',
    longDescription: `The SaaS startup ecosystem has long relied on informal skill exchanges to get products to market. Before raising seed rounds or generating revenue, founders routinely trade development hours for design work, swap marketing expertise for technical implementation, and barter advisory services for equity-adjacent arrangements. SkillLedger formalizes this untracked economy that founders quietly rely on, providing structure, documentation, and fair value tracking.

Y Combinator and other accelerators have observed that the most resourceful founders find ways to build without cash. Skill bartering is a core part of that resourcefulness. A technical co-founder might trade backend development for a designer who builds the landing page and brand identity. A marketing-focused founder could exchange growth strategy for the MVP development they cannot do themselves. These exchanges happen constantly in startup communities, coworking spaces, and online forums, but without documentation or fair value assessment.

The main risk for startups is the equity-for-services trap. Early-stage founders sometimes offer equity instead of cash for services, creating complex cap table issues, tax liabilities (under IRC Section 83), and misaligned incentives. Credit-based skill exchange through SkillLedger offers a cleaner alternative: both parties contribute defined services at agreed-upon values, with no equity dilution, no vesting schedules, and no cap table complications. For bootstrapped founders targeting $10K MRR before raising, this approach preserves ownership while accessing the multi-disciplinary talent needed to launch.`,
    keyBenefits: [
      'Launch your SaaS product without depleting runway on design, content, and marketing expenses',
      'Avoid equity-for-services arrangements that create cap table complexity and tax issues',
      'Access experienced designers, copywriters, and growth marketers through skill exchange',
      'Build cross-functional relationships with professionals who understand startup constraints',
      'Document all exchanges for clean financial records that investors and accountants can review',
    ],
    commonPairings: [
      { skillOffered: 'Full-Stack Development & API Integration', skillNeeded: 'Product Marketing & Positioning', description: 'Technical founders trade development hours building features, fixing bugs, or integrating third-party APIs in exchange for product positioning, messaging frameworks, go-to-market strategy, and launch marketing campaigns.' },
      { skillOffered: 'UX Design & Prototyping', skillNeeded: 'Technical Copywriting & Documentation', description: 'Product designers exchange wireframes, user flows, Figma prototypes, or design system components for product documentation, help center articles, onboarding copy, and email sequence writing.' },
      { skillOffered: 'DevOps & Infrastructure', skillNeeded: 'SEO & Content Strategy', description: 'Infrastructure engineers trade CI/CD pipeline setup, cloud architecture, monitoring configuration, or database optimization for keyword research, content calendars, blog post writing, and technical SEO audits.' },
      { skillOffered: 'Data Analytics & Business Intelligence', skillNeeded: 'Brand Identity & Visual Design', description: 'Data-focused founders offer analytics dashboard setup, cohort analysis, churn modeling, or investor metrics reporting in exchange for logo design, brand guidelines, pitch deck design, and social media visual assets.' },
    ],
    regulatoryNotes: 'SaaS founders should be aware that bartered services are taxable income under IRS Revenue Ruling 79-24 and must be reported at fair market value. Equity-for-services arrangements carry additional tax implications under IRC Section 83 and should be structured carefully with legal counsel. If your startup is incorporated as a C-corp or LLC, barter transactions may need to be reflected in corporate financial records. Consult with a startup-focused CPA or tax advisor to ensure proper reporting.',
    keyStatistic: 'Under IRC Section 83, equity-for-services arrangements in startups carry tax implications at the time of vesting, making barter a simpler alternative for early-stage service exchanges.',
    faqs: [
      { question: 'Is skill exchange better than offering equity for services?', answer: 'In most cases, yes. Equity-for-services arrangements create cap table complexity, potential tax liabilities under IRC Section 83, and misaligned incentives if the service provider does not remain engaged long-term. Credit-based skill exchange provides clear, documented value for both parties without diluting ownership or creating vesting complications.' },
      { question: 'How do SaaS founders typically use SkillLedger?', answer: 'The most common pattern is technical founders exchanging development work for design, marketing, and content creation services. A founder building a B2B SaaS product might trade 20 hours of backend development for a complete brand identity package, or swap DevOps consulting for a content marketing strategy and initial blog posts.' },
      { question: 'Can I use SkillLedger exchanges as a business expense?', answer: 'Bartered services received are reported as income, but the services you provide in exchange may be deductible as a business expense if they are ordinary and necessary for your business. The tax treatment depends on your business structure and the nature of the exchange. Consult with a CPA who understands startup accounting for specific guidance.' },
    ],
  },
  {
    slug: 'creative-agencies',
    name: 'Creative Agencies',
    description: 'Skill exchange for design studios, marketing agencies, and creative shops. Balance capacity, access specialized talent, and reduce subcontractor costs.',
    longDescription: `Creative agencies frequently face a capacity paradox: too much work in one discipline and not enough in another. A branding agency might land a project requiring custom web development they do not staff in-house. A digital marketing firm may win a client who needs video production capabilities beyond their team. Traditionally, agencies solve this through subcontracting, which means cash outlays that erode already-thin margins.

SkillLedger enables a different model: agency-to-agency and agency-to-freelancer skill exchange. A design studio with excess illustration capacity can trade those hours for the frontend development they need on a client project. A content agency can exchange copywriting for the motion graphics that round out a campaign deliverable. This white-label exchange model lets agencies expand their service offerings without hiring, subcontracting, or turning down work.

The economics are straightforward. Agency principals report that subcontractor costs consume 20% to 40% of project budgets on work delivered outside their core competency. By exchanging skills with complementary agencies and freelancers, firms can reduce these costs while building a reliable network of collaborators. SkillLedger tracks hours, manages credits, and provides the documentation needed for client billing and financial reporting.`,
    keyBenefits: [
      'Balance workload capacity by exchanging surplus capabilities with complementary agencies',
      'Expand service offerings without hiring full-time specialists or expensive subcontractors',
      'Reduce subcontractor costs that typically consume 20% to 40% of project budgets',
      'Build a trusted network of agency partners for white-label collaboration on client work',
      'Maintain financial documentation and audit trails for client billing and internal accounting',
    ],
    commonPairings: [
      { skillOffered: 'Brand Identity & Visual Design', skillNeeded: 'Frontend Web Development', description: 'Design studios trade logo packages, brand guidelines, packaging design, or illustration for responsive website builds, interactive prototypes, or custom WordPress and Webflow development.' },
      { skillOffered: 'Photography & Art Direction', skillNeeded: 'Copywriting & Content Strategy', description: 'Photography studios and art directors exchange product shoots, lifestyle photography, or creative direction for website copy, brand voice development, taglines, and content marketing plans.' },
      { skillOffered: 'Social Media Management', skillNeeded: 'Video Production & Animation', description: 'Social media agencies trade content calendars, community management, influencer outreach, or paid social campaigns for promotional videos, explainer animations, and motion graphics.' },
      { skillOffered: 'Print Design & Production', skillNeeded: 'SEO & Digital Marketing', description: 'Print-focused agencies exchange brochure design, packaging, trade show materials, or direct mail campaigns for search engine optimization, Google Ads management, and digital lead generation.' },
    ],
    regulatoryNotes: 'Agencies must ensure that skill exchanges involving client work comply with confidentiality agreements and non-disclosure obligations. White-label arrangements should be documented to clarify intellectual property ownership and usage rights. Bartered services are taxable income and must be reported at fair market value. Agencies operating as LLCs, S-corps, or C-corps should reflect barter transactions in their financial records and consult with their accountant on proper classification.',
    keyStatistic: 'Creative agencies typically spend 30-40% of revenue on subcontractors. Barter exchanges can convert idle capacity into those same services without cash outlay.',
    faqs: [
      { question: 'How do agencies handle client confidentiality in skill exchanges?', answer: 'SkillLedger supports NDA documentation and confidentiality agreements as part of the exchange setup process. Agencies should establish clear boundaries about what information can be shared with exchange partners and ensure all collaborators sign appropriate confidentiality agreements before accessing client materials.' },
      { question: 'Can agencies use skill exchange for white-label work?', answer: 'Yes. White-label collaboration is one of the most common agency use cases on SkillLedger. A design agency might deliver development work to their client under their own brand, with the actual development performed by an exchange partner. Clear agreements about branding, attribution, and deliverable ownership should be established upfront.' },
      { question: 'How do agencies value different types of creative work for exchange?', answer: 'Most agencies use their standard hourly rates as the basis for exchange valuation. Senior designer hours might be valued at 90-150 credits per hour, while junior production work might be 40-60 credits. SkillLedger allows both parties to agree on credit rates before the exchange begins, ensuring fair value alignment.' },
    ],
  },
  {
    slug: 'content-creators',
    name: 'Content Creators',
    description: 'Skill exchange for YouTubers, podcasters, bloggers, and social media creators. Trade editing, design, and production skills to grow your audience faster.',
    longDescription: `Content creators operate in an attention economy where production quality directly impacts audience growth and monetization potential. A YouTuber with strong on-camera presence may lack video editing skills. A podcaster with great interview technique might need help with audio engineering, show notes, and promotional graphics. Freelance rates on platforms like Fiverr and Upwork for these services range from $25 to $150 per hour depending on specialization, creating real budget pressure for creators who are not yet fully monetized.

SkillLedger provides a skill exchange alternative where creators trade what they do best for the production and marketing support they need. A graphic designer who runs a design-focused YouTube channel can trade thumbnail creation for a video editor who needs custom channel branding. A copywriter with a popular newsletter can exchange SEO-optimized articles for a web developer who builds a custom portfolio site. These exchanges happen peer-to-peer, with transparent credit tracking and no marketplace fees eating into already-slim creator margins.

The creator economy has grown to include over 50 million people worldwide who consider themselves content creators. For the vast majority who earn under $50,000 annually from their content, skill exchange is a practical strategy to improve production quality, expand distribution, and accelerate growth without spending cash they may not have.`,
    keyBenefits: [
      'Improve production quality by exchanging skills with editors, designers, and audio engineers',
      'Grow your audience faster by trading content for SEO, social media, and distribution expertise',
      'Avoid marketplace fees charged by platforms like Fiverr and Upwork for freelance services',
      'Build collaborative relationships with other creators for cross-promotion and joint projects',
      'Access professional branding and web design to establish credibility with sponsors and brands',
    ],
    commonPairings: [
      { skillOffered: 'Video Editing & Post-Production', skillNeeded: 'Thumbnail Design & Channel Branding', description: 'Video editors trade color grading, motion graphics, transitions, and long-form editing for custom YouTube thumbnails, channel art, intro animations, and consistent visual branding across platforms.' },
      { skillOffered: 'Scriptwriting & Content Strategy', skillNeeded: 'SEO & Keyword Research', description: 'Writers and content strategists exchange video scripts, podcast outlines, blog post drafts, or content calendars for keyword research, on-page SEO optimization, and search-driven content planning.' },
      { skillOffered: 'Audio Engineering & Podcast Production', skillNeeded: 'Social Media Marketing & Promotion', description: 'Audio engineers and podcast producers trade mixing, mastering, sound design, and RSS feed management for social media content creation, audiogram clips, community engagement, and paid promotion strategy.' },
      { skillOffered: 'Photography & Visual Content', skillNeeded: 'Web Development & Portfolio Sites', description: 'Photographers and visual artists exchange product photography, headshots, lifestyle imagery, or stock photo packages for custom portfolio websites, blog platforms, or e-commerce storefronts.' },
    ],
    regulatoryNotes: 'Content creators must report bartered services as income at fair market value per IRS requirements. If you operate as a sole proprietor, barter income is reported on Schedule C. Creators with LLCs or S-corps should reflect exchanges in business financial records. FTC guidelines require disclosure of material connections, so if an exchange partner creates sponsored-style content, appropriate disclosures may be needed. Copyright and usage rights for exchanged creative work should be documented in writing before the exchange begins.',
    keyStatistic: 'FTC endorsement guidelines require disclosure of material connections in bartered content. Creators must disclose when services are exchanged rather than paid for.',
    faqs: [
      { question: 'How do content creators value their skills for exchange?', answer: 'Most creators benchmark against freelance marketplace rates. Video editing might be valued at 40-80 credits per hour, graphic design at 50-100 credits per hour, and specialized services like audio mastering at 60-120 credits per hour. SkillLedger lets both parties agree on rates before starting, so values are transparent and fair.' },
      { question: 'Can I exchange content creation for business services like accounting?', answer: 'Yes. Cross-industry exchanges are common on SkillLedger. A content creator might trade video production or social media management for bookkeeping, tax preparation, legal review, or business coaching. Both parties need to agree on the fair value of services exchanged.' },
      { question: 'Who owns the content created through a skill exchange?', answer: 'Ownership should be defined before the exchange begins. By default, the person who commissions and receives the work typically owns it, but this should be documented in the exchange agreement. SkillLedger supports attaching terms and agreements to each exchange to prevent disputes.' },
    ],
  },
  {
    slug: 'local-small-businesses',
    name: 'Local Small Businesses',
    description: 'Skill exchange for Main Street businesses, local service providers, and small business owners. Access professional services without the cash outlay.',
    longDescription: `Local small businesses have participated in barter exchanges for decades. Organizations like the International Reciprocal Trade Association (IRTA), along with commercial barter exchanges such as BizX, ITEX, and IMS Barter, have built infrastructure for business-to-business skill and service trading. SkillLedger extends this model to the digital economy, enabling local businesses to exchange professional services with the same structure and documentation that established barter networks provide.

For a local bakery, accounting firm, auto repair shop, or dental practice, the economics of skill exchange are straightforward. These businesses need websites, social media management, photography, bookkeeping, and marketing but often operate on margins too thin to hire agencies. A local accountant might trade bookkeeping and tax preparation services for a complete website redesign. A restaurant owner could exchange catering services for a social media marketing campaign. A landscaper might trade weekly maintenance for professional photography of completed projects.

The U.S. small business barter economy is estimated at $12 billion to $14 billion annually according to IRTA. Much of this activity happens informally, without proper documentation or tax reporting. SkillLedger brings structure to these exchanges: automatic credit tracking, fair market value documentation, and year-end reporting that satisfies IRS requirements. For businesses already comfortable with barter, SkillLedger makes it more efficient and compliant.`,
    keyBenefits: [
      'Access professional web design, marketing, and accounting services without cash expenditure',
      'Join a structured exchange platform with automatic credit tracking and documentation',
      'Build relationships with local professionals who understand your market and customer base',
      'Maintain IRS compliance with fair market value tracking and year-end reporting summaries',
      'Preserve working capital for inventory, equipment, and other cash-only expenses',
    ],
    commonPairings: [
      { skillOffered: 'Accounting & Bookkeeping', skillNeeded: 'Digital Marketing & Social Media', description: 'Local accountants and bookkeepers exchange monthly financial management, tax preparation, or payroll processing for social media management, Google Business Profile optimization, and local SEO campaigns.' },
      { skillOffered: 'Professional Photography', skillNeeded: 'Web Design & E-commerce Setup', description: 'Local photographers trade product shoots, headshots, real estate photography, or event coverage for responsive websites, online booking systems, or Shopify storefront setup.' },
      { skillOffered: 'Catering & Event Services', skillNeeded: 'Graphic Design & Print Marketing', description: 'Restaurants and caterers exchange catering packages, private dining experiences, or event coordination for menu design, signage, direct mail pieces, and branded packaging.' },
      { skillOffered: 'Home Services & Maintenance', skillNeeded: 'Business Consulting & Operations', description: 'Contractors, landscapers, and maintenance providers trade their skilled labor for business plan development, operations consulting, hiring process design, or financial forecasting.' },
    ],
    regulatoryNotes: 'Small businesses participating in barter must report all exchanges at fair market value as taxable income. Businesses using a barter exchange (including SkillLedger) will receive Form 1099-B reporting gross barter proceeds. The IRS requires barter income to be reported in the tax year the exchange occurs, regardless of when credits are used. State and local sales tax may also apply to bartered services depending on jurisdiction. Consult with a small business accountant familiar with barter reporting requirements.',
    keyStatistic: 'The IRS requires barter income to be reported in the tax year the exchange occurs via Form 1099-B, regardless of when credits are used.',
    faqs: [
      { question: 'How is SkillLedger different from traditional barter exchanges like BizX or ITEX?', answer: 'Traditional barter exchanges like BizX, ITEX, and IMS Barter focus on goods and services between local businesses, often with membership fees and transaction commissions. SkillLedger focuses specifically on professional skill exchange, offers a digital-first platform with lower overhead, and provides tools designed for service-based businesses rather than product-based trades.' },
      { question: 'Do I need to pay taxes on bartered services?', answer: 'Yes. The IRS treats bartered services as taxable income at fair market value. If you receive $1,000 worth of web design in exchange for $1,000 worth of accounting services, both parties must report $1,000 in income. SkillLedger provides year-end summaries to simplify tax reporting.' },
      { question: 'What happens if the services exchanged are not equal in value?', answer: 'SkillLedger uses a credit system so exchanges do not need to be perfectly matched. If you provide $500 worth of photography and receive $300 worth of design work, you retain $200 in credits to use on future exchanges. This removes the need for exact one-to-one matching that limits traditional barter.' },
    ],
  },
  {
    slug: 'real-estate-professionals',
    name: 'Real Estate Professionals',
    description: 'Skill exchange for real estate agents, brokers, property managers, and real estate investors. Trade staging, photography, and marketing expertise.',
    longDescription: `Real estate professionals spend heavily on marketing, photography, staging, and technology to win and close listings. The National Association of Realtors (NAR) reports that the typical agent spends 10% to 20% of their gross commission income on marketing and business development. For an agent earning $80,000 in gross commissions, that represents $8,000 to $16,000 annually in marketing costs. Skill exchange through SkillLedger allows agents to redirect a portion of these expenses into trades that preserve cash while still delivering professional-quality results.

The real estate industry is well suited to skill exchange because its professionals possess high-value, in-demand business knowledge. Understanding local markets, negotiating contracts, evaluating investments, and navigating title and escrow processes are skills that many other professionals need. A tech entrepreneur looking to purchase office space, a designer relocating to a new city, or a small business owner evaluating a commercial lease all benefit from real estate expertise. These professionals are often willing to trade their own skills for guided real estate counsel.

Modern real estate marketing demands professional photography, virtual tours, drone footage, 3D walkthroughs, social media campaigns, and SEO-optimized property listings. Agents who access these services through skill exchange rather than cash payment can maintain the marketing quality that wins listings while keeping more of each commission check.`,
    keyBenefits: [
      'Reduce marketing costs by exchanging real estate expertise for photography, staging, and design',
      'Access professional virtual tours, drone footage, and 3D walkthroughs through skill trades',
      'Build referral relationships with the designers, developers, and marketers who serve your clients',
      'Offer clients enhanced services by partnering with skilled professionals across disciplines',
      'Preserve commission income by trading knowledge for services instead of paying cash',
    ],
    commonPairings: [
      { skillOffered: 'Home Staging & Property Presentation', skillNeeded: 'Professional Photography & Videography', description: 'Staging professionals trade furniture arrangement, decor selection, and curb appeal consulting for listing photos, property video tours, drone footage, and twilight photography.' },
      { skillOffered: 'Market Analysis & Investment Consulting', skillNeeded: 'Web Development & IDX Integration', description: 'Real estate analysts and investor-agents exchange comparative market analyses, investment property evaluations, or portfolio strategy for custom real estate websites with IDX search, lead capture, and CRM integration.' },
      { skillOffered: 'Transaction Coordination & Contract Management', skillNeeded: 'Social Media Marketing & Content', description: 'Transaction coordinators trade timeline management, document preparation, and closing coordination for Instagram Reels, TikTok content, Facebook ad campaigns, and real estate blog writing.' },
      { skillOffered: 'Property Management & Tenant Relations', skillNeeded: 'Legal Review & Lease Drafting', description: 'Property managers exchange maintenance coordination, tenant screening processes, and vacancy marketing for lease agreement review, eviction procedure guidance, and fair housing compliance consulting.' },
    ],
    regulatoryNotes: 'Real estate professionals must comply with NAR Code of Ethics and state real estate commission regulations when entering barter arrangements. Some states require disclosure of material relationships between agents and service providers involved in a transaction. Licensed agents should ensure that barter arrangements do not create undisclosed dual agency situations or conflicts of interest. All bartered services must be reported as income at fair market value. Real estate brokerages may have policies governing agent participation in barter exchanges.',
    keyStatistic: 'NAR Code of Ethics requires disclosure of material relationships in real estate transactions. Barter arrangements with service providers involved in a deal must be disclosed.',
    faqs: [
      { question: 'Can real estate agents barter their professional services?', answer: 'Yes. Real estate agents can exchange their expertise in market analysis, property evaluation, transaction coordination, and other non-commission services. Commission-based compensation is typically governed by brokerage agreements and state licensing laws, so agents should consult their broker and state real estate commission before bartering services directly tied to transaction commissions.' },
      { question: 'How do agents handle barter taxes on real estate services?', answer: 'Bartered services are taxable income at fair market value. If an agent provides $2,000 worth of market analysis consulting in exchange for $2,000 worth of professional photography, both parties report $2,000 in income. Agents should work with a CPA familiar with real estate professional tax rules to ensure proper reporting.' },
      { question: 'What real estate services are most in demand for exchange?', answer: 'Market analysis, buyer consultation, investment property evaluation, and relocation assistance are highly valued by other professionals on SkillLedger. On the other side, agents most commonly seek photography, videography, social media marketing, web development, and graphic design services through exchange.' },
    ],
  },
  {
    slug: 'independent-consultants',
    name: 'Independent Consultants',
    description: 'Skill exchange for freelance consultants, fractional executives, and independent advisors. Trade strategic expertise for the operational services you need.',
    longDescription: `The independent consulting economy has grown dramatically, with MBO Partners reporting 72.9 million independent workers in the United States as of their most recent annual report. Among these, over 3.4 million earn $100,000 or more annually. Independent consultants in strategy, management, technology, finance, and operations possess high-value expertise that commands high rates, but they face the same challenge as every solo professional: they need services outside their specialty to run their businesses effectively.

A management consultant earning $200 per hour still needs a website, a brand identity, a content marketing strategy, and an accounting system. Paying retail rates for these services means spending $10,000 to $30,000 annually on business infrastructure. Skill exchange through SkillLedger allows consultants to trade their strategic expertise for the tactical services they need. A fractional CFO might exchange financial modeling and cash flow analysis for a complete website redesign. A marketing strategy consultant could trade go-to-market planning for custom CRM development.

Consultant-to-consultant exchange is particularly valuable. A strategy consultant who needs help with sales process design can trade with a sales consultant who needs help with pricing strategy. A technology advisor who needs legal counsel can exchange with an attorney who needs IT infrastructure guidance. These peer-level exchanges create lasting professional relationships that often generate referral revenue far exceeding the value of the initial exchange.`,
    keyBenefits: [
      'Trade high-value strategic consulting for design, development, and marketing services',
      'Reduce business infrastructure costs that typically range from $10,000 to $30,000 annually',
      'Build peer-level relationships with other consultants for cross-referral and collaboration',
      'Access specialized skills on-demand without the commitment of hiring employees or agencies',
      'Maintain clean financial documentation for quarterly estimated tax payments and year-end filing',
    ],
    commonPairings: [
      { skillOffered: 'Strategy Consulting & Business Planning', skillNeeded: 'Web Development & Online Presence', description: 'Strategy consultants and business advisors trade competitive analysis, market entry plans, business model design, or board presentation preparation for professional websites, landing pages, and lead generation funnels.' },
      { skillOffered: 'Business Coaching & Executive Development', skillNeeded: 'Graphic Design & Presentation Design', description: 'Executive coaches and leadership consultants exchange coaching sessions, 360 assessment facilitation, or team workshop design for brand identity, pitch decks, speaking engagement materials, and conference booth design.' },
      { skillOffered: 'Financial Consulting & Fractional CFO Services', skillNeeded: 'Content Marketing & Thought Leadership', description: 'Financial consultants trade cash flow modeling, budget forecasting, financial reporting setup, or fundraising preparation for ghostwritten articles, LinkedIn content strategies, newsletter development, and podcast guest booking.' },
      { skillOffered: 'Technology Advisory & Digital Transformation', skillNeeded: 'Video Production & Course Creation', description: 'Technology consultants exchange IT roadmap development, vendor evaluation, system architecture guidance, or cybersecurity assessments for online course production, webinar recording, promotional videos, and YouTube channel setup.' },
    ],
    regulatoryNotes: 'Independent consultants must report bartered services as self-employment income on Schedule C and pay self-employment tax on the fair market value received. Quarterly estimated tax payments should account for barter income. Consultants operating through an LLC or S-corp should reflect barter transactions in business financial records. Professional liability insurance may need to cover services provided through barter arrangements. Consultants with professional certifications (CPA, PMP, etc.) should verify that their certifying body does not restrict barter arrangements.',
    keyStatistic: 'Independent consultants must pay self-employment tax (15.3%) on the fair market value of bartered services received, in addition to income tax.',
    faqs: [
      { question: 'How do independent consultants value their time for exchange?', answer: 'Most consultants use their standard hourly or daily rate as the basis for exchange valuation. If you charge $200 per hour for strategy consulting, you would value your exchange contributions at 200 credits per hour. Use the same rate you would charge a paying client, as the IRS requires fair market value reporting.' },
      { question: 'Can I exchange consulting services for other consulting services?', answer: 'Yes. Consultant-to-consultant exchanges are among the most valuable on SkillLedger. A marketing strategist might trade go-to-market planning with a sales consultant who provides pipeline development methodology. These peer-level exchanges often create lasting referral relationships.' },
      { question: 'How does barter income affect my quarterly estimated tax payments?', answer: 'Barter income is self-employment income and should be included in your quarterly estimated tax calculations. If you receive $5,000 in services through exchange during Q1, that amount should be factored into your Q1 estimated payment. SkillLedger provides real-time exchange tracking to help consultants stay current on their barter income totals.' },
    ],
  },
  {
    slug: 'ecommerce-brands',
    name: 'E-commerce Brands',
    description: 'Skill exchange for online retailers, DTC brands, and e-commerce entrepreneurs. Trade product photography, Shopify development, and Amazon optimization skills.',
    longDescription: `E-commerce brands operate in a visual-first marketplace where product photography, website design, and search optimization directly determine conversion rates and revenue. A well-photographed product listing can increase conversion by 30% or more according to industry benchmarks, but professional product photography sessions typically cost $500 to $5,000 per shoot. Shopify theme customization runs $2,000 to $10,000, and ongoing Amazon listing optimization commands $1,000 to $3,000 monthly from specialized agencies.

For emerging DTC (direct-to-consumer) brands, bootstrapped Shopify stores, and Amazon private label sellers, these costs represent a large portion of operating budget. Skill exchange through SkillLedger allows e-commerce entrepreneurs to access these services by trading their own expertise. A brand owner with supply chain knowledge might exchange sourcing strategy for product photography. An Amazon seller with proven PPC expertise could trade advertising management for Shopify custom development.

The e-commerce ecosystem includes dozens of specialized skills that are hard to master in-house: product photography and lifestyle imagery, Shopify Liquid theme development, Amazon A+ content creation, Google Shopping feed optimization, email marketing automation (Klaviyo, Mailchimp), influencer outreach, conversion rate optimization, and returns/logistics management. SkillLedger connects e-commerce professionals with complementary skills so that each can focus on their strength while accessing the full range of expertise needed to compete.`,
    keyBenefits: [
      'Access professional product photography and lifestyle imagery without large cash outlays',
      'Trade e-commerce expertise for Shopify development, Amazon optimization, and SEO services',
      'Build a network of specialized e-commerce professionals for ongoing collaboration',
      'Improve conversion rates by accessing design and UX expertise through skill exchange',
      'Scale your brand with professional marketing, email automation, and content creation',
    ],
    commonPairings: [
      { skillOffered: 'Product Photography & Visual Content', skillNeeded: 'SEO & Search Optimization', description: 'E-commerce photographers exchange product shots, lifestyle imagery, flat-lay compositions, and 360-degree product views for on-page SEO, Google Shopping feed optimization, category page architecture, and keyword-targeted product descriptions.' },
      { skillOffered: 'Shopify Development & Theme Customization', skillNeeded: 'Content Writing & Product Descriptions', description: 'Shopify developers trade custom theme builds, Liquid template modifications, app integrations, and checkout optimization for product descriptions, collection page copy, about page storytelling, and blog content strategies.' },
      { skillOffered: 'Amazon PPC & Marketplace Optimization', skillNeeded: 'Email Marketing & Automation', description: 'Amazon specialists exchange Sponsored Products campaign management, listing optimization, A+ content creation, and keyword research for Klaviyo flow setup, email template design, abandoned cart sequences, and post-purchase automation.' },
      { skillOffered: 'Supply Chain & Sourcing Expertise', skillNeeded: 'Brand Identity & Packaging Design', description: 'Supply chain professionals trade supplier vetting, cost negotiation, logistics optimization, and inventory forecasting for brand identity development, product packaging design, unboxing experience design, and insert card creation.' },
    ],
    regulatoryNotes: 'E-commerce businesses must report bartered services as income at fair market value. If product inventory is exchanged (rather than services), sales tax obligations may apply depending on jurisdiction. Businesses operating on Amazon must comply with Amazon Terms of Service, which may restrict certain promotional arrangements. FTC endorsement guidelines apply if exchanged services include reviews or testimonials. Shopify store owners should ensure that exchanged development work does not violate Shopify Partner Program terms.',
    keyStatistic: 'When product inventory is bartered rather than services, state sales tax obligations may apply in addition to federal income tax on the fair market value exchanged.',
    faqs: [
      { question: 'Can I exchange e-commerce services across different platforms (Shopify, Amazon, WooCommerce)?', answer: 'Yes. SkillLedger is platform-agnostic. You might trade Shopify development expertise with an Amazon optimization specialist, or exchange WooCommerce customization for product photography that you use across all your sales channels. The credit system allows cross-platform skill trading without requiring a direct skill-for-skill match.' },
      { question: 'How do e-commerce brands typically start with skill exchange?', answer: 'Most e-commerce brands start by identifying their most expensive outsourced service and their most marketable in-house skill. If you spend $3,000 monthly on product photography but have strong email marketing skills, you might offer Klaviyo setup and campaign management in exchange for photography sessions. Start with one exchange to test the process.' },
      { question: 'What if the quality of work received through exchange does not meet my brand standards?', answer: 'SkillLedger includes a reputation and review system that helps you evaluate exchange partners before committing. You can review portfolios, check ratings from previous exchanges, and start with a small test project before engaging in larger exchanges. If work quality falls short, the dispute resolution process protects both parties.' },
    ],
  },
]

export function getIndustryBySlug(slug: string): IndustryData | undefined {
  return industriesData.find((i) => i.slug === slug)
}
