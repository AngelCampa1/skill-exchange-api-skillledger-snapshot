import Link from'next/link'
import { FolderPlus, Search, Wallet, ArrowRight, Calculator, FileText, Scale, BookOpenText } from'lucide-react'
import { ThemeToggle } from'@/components/ThemeToggle'
import { Logo } from'@/components/Logo'
import AuthenticatedHomeWrapper from'./AuthenticatedHomeWrapper'
import { TrustBadges } from'@/components/TrustBadges'
import { SocialProof } from'@/components/SocialProof'
import { NewsletterSignup } from'@/components/NewsletterSignup'
import { categoriesData } from'@/lib/data/categories-data'
import { scenariosData } from'@/lib/data/scenarios-data'
import { citiesData } from'@/lib/data/cities-data'
import { industriesData } from'@/lib/data/industries-data'
import { comparisonsData } from'@/lib/data/comparisons-data'
import { getAllArticles } from'@/lib/content'
import { generateWebPageSchema, generateOrganizationSchema, generateWebSiteSchema, generateSoftwareApplicationSchema, generateFAQSchema, SITE_CONFIG } from'@/lib/seo'
import { JsonLd } from'@/components/marketing/JsonLd'

const homeFaqs = [
  {
    question:'What is SkillLedger?',
    answer:'SkillLedger is a professional collaboration platform where freelancers and businesses exchange services using a credit-based barter system. You earn credits by providing your expertise and spend them on services you need. No cash changes hands.',
  },
  {
    question:'How does skill exchange work on SkillLedger?',
    answer:'Post your professional skills and set your credit rate based on market value. Browse the marketplace, propose exchanges with clear scope and timelines, and use built-in escrow to protect both parties. Credits transfer only when both sides confirm satisfaction.',
  },
  {
    question:'How much does SkillLedger cost?',
    answer:'All plans include a 30-day free trial — no charge until the trial ends, cancel anytime. Plans start at $19/month (Professional), $49/month (Business), or $99/month (Enterprise). A credit card is required to activate the trial.',
  },
  {
    question:'What can you exchange on SkillLedger?',
    answer:'SkillLedger supports 19 professional skill categories including web development, graphic design, marketing, legal review, accounting, copywriting, photography, and more. Any professional service that can be delivered remotely or locally is eligible.',
  },
  {
    question:'How do credits work on SkillLedger?',
    answer:'Credits represent fair market value for professional services. Each professional sets their own credit rate. When you complete work, you earn credits equal to the agreed value. You then spend those credits to hire other professionals. No cash, no commissions.',
  },
]

const featuredCategories = categoriesData
  .filter((c) => c.demandLevel ==='high')
  .slice(0, 6)
const featuredScenarios = scenariosData.slice(0, 3)
const featuredCities = citiesData.slice(0, 6)
const featuredIndustries = industriesData.slice(0, 6)
const featuredComparisons = comparisonsData.slice(0, 3)

export default function Home() {
  const recentArticles = getAllArticles().slice(0, 3)
  const faqSchema = generateFAQSchema(homeFaqs)

  const webPageSchema = generateWebPageSchema({
    name:'SkillLedger — Professional Collaboration Platform',
    description: SITE_CONFIG.description,
    url: SITE_CONFIG.url,
  })
  const orgSchema = generateOrganizationSchema()
  const siteSchema = generateWebSiteSchema()
  const appSchema = generateSoftwareApplicationSchema()

  return (
    <>
      <JsonLd schema={webPageSchema} />
      <JsonLd schema={orgSchema} />
      <JsonLd schema={siteSchema} />
      <JsonLd schema={appSchema} />
      <JsonLd schema={faqSchema} />
      {/* Static landing page — rendered server-side for SEO */}
      <div className="min-h-screen bg-background overflow-x-hidden">
        {/* Hero Section */}
        <section className="relative min-h-[70vh] flex items-center justify-center overflow-hidden">
          {/* Premium positioning for theme toggle */}
          <div className="absolute top-8 right-8 z-10">
            <ThemeToggle />
          </div>

          {/* Enhanced background decoration */}
          <div className="absolute inset-0 bg-gradient-to-br from-primary/3 via-transparent to-secondary/2 pointer-events-none" aria-hidden="true"></div>
          <div className="absolute top-1/4 left-1/4 w-48 h-48 sm:w-72 sm:h-72 lg:w-96 lg:h-96 bg-primary/4 rounded-full blur-3xl pointer-events-none animate-float" aria-hidden="true"></div>
          <div className="absolute bottom-1/4 right-1/4 w-32 h-32 sm:w-48 sm:h-48 lg:w-64 lg:h-64 bg-secondary/3 rounded-full blur-2xl pointer-events-none animate-float" style={{animationDelay:'3s'}} aria-hidden="true"></div>
          <div className="absolute top-1/2 right-1/3 w-24 h-24 sm:w-36 sm:h-36 lg:w-48 lg:h-48 bg-primary/2 rounded-full blur-xl pointer-events-none animate-float" style={{animationDelay:'1.5s'}} aria-hidden="true"></div>

          {/* Hero content */}
          <div className="container-hero animate-fade-in relative z-10 py-16">
            <div className="space-golden-xl">
              <div className="space-golden-lg flex flex-col items-center mb-8">
                <div className="animate-slide-in mb-8">
                  <Logo size="hero" showText={false} />
                </div>
                <h1 className="text-display text-foreground animate-slide-in mb-6">
                  SkillLedger
                </h1>
                <div className="max-w-3xl mx-auto space-golden-md text-center">
                  <p className="text-heading text-muted-foreground leading-relaxed font-medium">
                    Trade Your Skills. Skip the Invoice.
                  </p>
                  <p className="text-body text-muted-foreground/90 leading-relaxed max-w-2xl mx-auto">
                    Join 19 skill categories across 50+ US cities. Offer your expertise, get services you need. Protected by escrow, valued in credits.
                  </p>
                </div>
              </div>

              <div className="flex flex-col gap-4 sm:flex-row sm:justify-center sm:gap-6 animate-scale-in">
                <Link href="/register" className="btn-primary hover:scale-105 transition-all duration-300 shadow-lg hover:shadow-xl">
                  Start Exchanging Free
                </Link>
                <Link href="/skill-match" className="btn-secondary hover:scale-105 transition-all duration-300">
                  Find Your Skill Match
                </Link>
              </div>
              <p className="text-sm text-muted-foreground mt-4 animate-fade-in">Start your 30-day free trial today. Cancel anytime.</p>
            </div>
          </div>
        </section>

        {/* Trust Badges */}
        <TrustBadges />

        {/* Features Section */}
        <section className="py-24 lg:py-32 relative">
          <div className="container-premium">
            <div className="text-center mb-20 animate-fade-in">
              <h2 className="text-3xl lg:text-4xl font-black tracking-tight mb-6">
                <span className="bg-gradient-to-r from-primary to-secondary bg-clip-text text-transparent">
                  Why Choose SkillLedger?
                </span>
              </h2>
              <p className="text-lg text-muted-foreground max-w-2xl mx-auto leading-relaxed">
                Built for professionals who would rather trade skills than write checks.
              </p>
            </div>

            <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-8 lg:gap-12">
              {/* Feature 1 */}
              <div className="card-feature p-8 text-center space-golden-md animate-slide-in" style={{animationDelay:'0.1s'}}>
                <div className="flex justify-center mb-6">
                  <div className="p-4 bg-gradient-to-br from-primary/20 to-primary/10 rounded-2xl">
                    <FolderPlus className="w-8 h-8 text-primary" />
                  </div>
                </div>
                <h3 className="text-subheading text-foreground mb-4">Project Management</h3>
                <p className="text-body text-muted-foreground leading-relaxed">
                  Set up exchange projects with clear deliverables, milestones, and credit terms. Both sides know what to expect before work starts.
                </p>
              </div>

              {/* Feature 2 */}
              <div className="card-feature p-8 text-center space-golden-md animate-slide-in" style={{animationDelay:'0.2s'}}>
                <div className="flex justify-center mb-6">
                  <div className="p-4 bg-gradient-to-br from-secondary/20 to-secondary/10 rounded-2xl">
                    <Search className="w-8 h-8 text-secondary" />
                  </div>
                </div>
                <h3 className="text-subheading text-foreground mb-4">Talent Discovery</h3>
                <p className="text-body text-muted-foreground leading-relaxed">
                  Search by skill, location, or credit rate. Every profile shows verified work history and reputation scores from past exchanges.
                </p>
              </div>

              {/* Feature 3 */}
              <div className="card-feature p-8 text-center space-golden-md animate-slide-in" style={{animationDelay:'0.3s'}}>
                <div className="flex justify-center mb-6">
                  <div className="p-4 bg-gradient-to-br from-primary/20 via-secondary/15 to-primary/10 rounded-2xl">
                    <Wallet className="w-8 h-8 text-primary" />
                  </div>
                </div>
                <h3 className="text-subheading text-foreground mb-4">Credit Exchange</h3>
                <p className="text-body text-muted-foreground leading-relaxed">
                  Track your credit balance, earnings history, and exchange portfolio. Every transaction is valued at fair market rate.
                </p>
              </div>
            </div>
          </div>
        </section>

        {/* Social Proof */}
        <SocialProof />

        {/* Browse by Category */}
        <section className="py-24 lg:py-32 bg-muted/30">
          <div className="container-premium">
            <div className="flex items-end justify-between mb-12">
              <div>
                <h2 className="text-3xl lg:text-4xl font-black tracking-tight mb-4">
                  <span className="bg-gradient-to-r from-primary to-secondary bg-clip-text text-transparent">
                    Browse by Category
                  </span>
                </h2>
                <p className="text-lg text-muted-foreground max-w-xl">
                  Explore in-demand professional skills available for exchange.
                </p>
              </div>
              <Link href="/categories" className="hidden sm:flex items-center gap-2 text-sm font-medium text-primary hover:text-primary/80 transition-colors">
                View all categories <ArrowRight className="w-4 h-4" />
              </Link>
            </div>
            <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-6">
              {featuredCategories.map((cat) => (
                <Link
                  key={cat.slug}
                  href={`/categories/${cat.slug}`}
                  className="card-feature p-6 hover:shadow-lg transition-all duration-200 group"
                >
                  <div className="flex items-start justify-between mb-3">
                    <h3 className="text-lg font-bold group-hover:text-primary transition-colors">{cat.name}</h3>
                    <span className="text-xs px-2 py-1 rounded-full bg-green-100  text-green-700">high demand</span>
                  </div>
                  <p className="text-sm text-muted-foreground mb-4 leading-relaxed line-clamp-2">{cat.description}</p>
                  <div className="flex flex-wrap gap-1">
                    {cat.sampleSkills.slice(0, 3).map((skill) => (
                      <span key={skill} className="text-xs bg-primary/10 text-primary px-2 py-0.5 rounded">{skill}</span>
                    ))}
                  </div>
                </Link>
              ))}
            </div>
            <div className="mt-8 text-center sm:hidden">
              <Link href="/categories" className="text-sm font-medium text-primary hover:text-primary/80 transition-colors">
                View all categories &rarr;
              </Link>
            </div>
          </div>
        </section>

        {/* How-To Guides */}
        <section className="py-24 lg:py-32">
          <div className="container-premium">
            <div className="flex items-end justify-between mb-12">
              <div>
                <h2 className="text-3xl lg:text-4xl font-black tracking-tight mb-4">
                  <span className="bg-gradient-to-r from-primary to-secondary bg-clip-text text-transparent">
                    How It Works
                  </span>
                </h2>
                <p className="text-lg text-muted-foreground max-w-xl">
                  See real examples of how professionals exchange skills on SkillLedger.
                </p>
              </div>
              <Link href="/how-to" className="hidden sm:flex items-center gap-2 text-sm font-medium text-primary hover:text-primary/80 transition-colors">
                View all guides <ArrowRight className="w-4 h-4" />
              </Link>
            </div>
            <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
              {featuredScenarios.map((scenario) => (
                <Link
                  key={scenario.slug}
                  href={`/how-to/${scenario.slug}`}
                  className="card-feature p-6 hover:shadow-lg transition-all duration-200 group"
                >
                  <h3 className="text-lg font-bold group-hover:text-primary transition-colors mb-3 line-clamp-2">{scenario.title}</h3>
                  <p className="text-sm text-muted-foreground mb-4 leading-relaxed line-clamp-3">{scenario.description}</p>
                  <div className="flex flex-wrap gap-1.5">
                    <span className="text-xs bg-primary/10 text-primary px-2 py-0.5 rounded">{scenario.skillOffered}</span>
                    <span className="text-xs text-muted-foreground">for</span>
                    <span className="text-xs bg-secondary/10 text-secondary px-2 py-0.5 rounded">{scenario.skillNeeded}</span>
                  </div>
                </Link>
              ))}
            </div>
            <div className="mt-8 text-center sm:hidden">
              <Link href="/how-to" className="text-sm font-medium text-primary hover:text-primary/80 transition-colors">
                View all guides &rarr;
              </Link>
            </div>
          </div>
        </section>

        {/* Latest Resources */}
        <section className="py-24 lg:py-32 bg-muted/30">
          <div className="container-premium">
            <div className="flex items-end justify-between mb-12">
              <div>
                <h2 className="text-3xl lg:text-4xl font-black tracking-tight mb-4">
                  <span className="bg-gradient-to-r from-primary to-secondary bg-clip-text text-transparent">
                    Latest Resources
                  </span>
                </h2>
                <p className="text-lg text-muted-foreground max-w-xl">
                  Guides and articles to help you succeed with professional skill exchange.
                </p>
              </div>
              <Link href="/resources" className="hidden sm:flex items-center gap-2 text-sm font-medium text-primary hover:text-primary/80 transition-colors">
                View all articles <ArrowRight className="w-4 h-4" />
              </Link>
            </div>
            <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
              {recentArticles.map((article) => (
                <Link
                  key={article.slug}
                  href={`/resources/${article.slug}`}
                  className="card-feature p-6 hover:shadow-lg transition-all duration-200 group"
                >
                  <h3 className="text-lg font-bold group-hover:text-primary transition-colors mb-2 line-clamp-2">{article.frontmatter.title}</h3>
                  <p className="text-sm text-muted-foreground mb-3 leading-relaxed line-clamp-3">{article.frontmatter.description}</p>
                  <div className="flex items-center gap-3 text-xs text-muted-foreground">
                    <time dateTime={article.frontmatter.publishedAt}>
                      {new Date(article.frontmatter.publishedAt).toLocaleDateString('en-US', {
                        year:'numeric', month:'long', day:'numeric'
                      })}
                    </time>
                    <span>{article.readingTime}</span>
                  </div>
                </Link>
              ))}
            </div>
            <div className="mt-8 text-center sm:hidden">
              <Link href="/resources" className="text-sm font-medium text-primary hover:text-primary/80 transition-colors">
                View all articles &rarr;
              </Link>
            </div>
          </div>
        </section>

        {/* Tools & Resources */}
        <section className="py-24 lg:py-32">
          <div className="container-premium">
            <div className="text-center mb-12">
              <h2 className="text-3xl lg:text-4xl font-black tracking-tight mb-4">
                <span className="bg-gradient-to-r from-primary to-secondary bg-clip-text text-transparent">
                  Tools & Resources
                </span>
              </h2>
              <p className="text-lg text-muted-foreground max-w-xl mx-auto">
                Free tools to help you value, document, and understand skill exchanges.
              </p>
            </div>
            <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-6">
              <Link href="/tools/barter-valuation-calculator" className="card-feature p-6 hover:shadow-lg transition-all duration-200 group text-center">
                <div className="flex justify-center mb-4">
                  <div className="p-3 bg-gradient-to-br from-primary/20 to-primary/10 rounded-xl">
                    <Calculator className="w-6 h-6 text-primary" />
                  </div>
                </div>
                <h3 className="font-bold group-hover:text-primary transition-colors mb-2">Credit Calculator</h3>
                <p className="text-sm text-muted-foreground">Calculate fair exchange values for your skills</p>
              </Link>
              <Link href="/resources/templates" className="card-feature p-6 hover:shadow-lg transition-all duration-200 group text-center">
                <div className="flex justify-center mb-4">
                  <div className="p-3 bg-gradient-to-br from-secondary/20 to-secondary/10 rounded-xl">
                    <FileText className="w-6 h-6 text-secondary" />
                  </div>
                </div>
                <h3 className="font-bold group-hover:text-primary transition-colors mb-2">Contract Templates</h3>
                <p className="text-sm text-muted-foreground">Free barter agreement and NDA templates</p>
              </Link>
              <Link href="/compare" className="card-feature p-6 hover:shadow-lg transition-all duration-200 group text-center">
                <div className="flex justify-center mb-4">
                  <div className="p-3 bg-gradient-to-br from-primary/20 to-primary/10 rounded-xl">
                    <Scale className="w-6 h-6 text-primary" />
                  </div>
                </div>
                <h3 className="font-bold group-hover:text-primary transition-colors mb-2">Platform Comparisons</h3>
                <p className="text-sm text-muted-foreground">See how we compare to Fiverr, Upwork, and more</p>
              </Link>
              <Link href="/glossary" className="card-feature p-6 hover:shadow-lg transition-all duration-200 group text-center">
                <div className="flex justify-center mb-4">
                  <div className="p-3 bg-gradient-to-br from-secondary/20 to-secondary/10 rounded-xl">
                    <BookOpenText className="w-6 h-6 text-secondary" />
                  </div>
                </div>
                <h3 className="font-bold group-hover:text-primary transition-colors mb-2">Glossary</h3>
                <p className="text-sm text-muted-foreground">Learn the language of skill exchange and barter</p>
              </Link>
            </div>
          </div>
        </section>

        {/* Find Professionals Near You */}
        <section className="py-24 lg:py-32 bg-muted/30">
          <div className="container-premium">
            <div className="flex items-end justify-between mb-12">
              <div>
                <h2 className="text-3xl lg:text-4xl font-black tracking-tight mb-4">
                  <span className="bg-gradient-to-r from-primary to-secondary bg-clip-text text-transparent">
                    Find Professionals Near You
                  </span>
                </h2>
                <p className="text-lg text-muted-foreground max-w-xl">
                  Connect with professionals in your city for local skill exchanges.
                </p>
              </div>
              <Link href="/skill-exchange" className="hidden sm:flex items-center gap-2 text-sm font-medium text-primary hover:text-primary/80 transition-colors">
                View all cities <ArrowRight className="w-4 h-4" />
              </Link>
            </div>
            <div className="grid grid-cols-2 sm:grid-cols-3 lg:grid-cols-6 gap-4">
              {featuredCities.map((city) => (
                <Link
                  key={city.slug}
                  href={`/skill-exchange/${city.slug}`}
                  className="card-feature p-5 hover:shadow-lg transition-all duration-200 group text-center"
                >
                  <h3 className="font-bold group-hover:text-primary transition-colors mb-1">{city.city}</h3>
                  <p className="text-xs text-muted-foreground">{city.state}</p>
                </Link>
              ))}
            </div>
            <div className="mt-8 text-center sm:hidden">
              <Link href="/skill-exchange" className="text-sm font-medium text-primary hover:text-primary/80 transition-colors">
                View all cities &rarr;
              </Link>
            </div>
          </div>
        </section>

        {/* Industries */}
        <section className="py-24 lg:py-32">
          <div className="container-premium">
            <div className="flex items-end justify-between mb-12">
              <div>
                <h2 className="text-3xl lg:text-4xl font-black tracking-tight mb-4">
                  <span className="bg-gradient-to-r from-primary to-secondary bg-clip-text text-transparent">
                    Industries We Serve
                  </span>
                </h2>
                <p className="text-lg text-muted-foreground max-w-xl">
                  Tailored skill exchange guides for professionals across industries.
                </p>
              </div>
              <Link href="/industries" className="hidden sm:flex items-center gap-2 text-sm font-medium text-primary hover:text-primary/80 transition-colors">
                View all industries <ArrowRight className="w-4 h-4" />
              </Link>
            </div>
            <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-6">
              {featuredIndustries.map((industry) => (
                <Link
                  key={industry.slug}
                  href={`/industries/${industry.slug}`}
                  className="card-feature p-6 hover:shadow-lg transition-all duration-200 group"
                >
                  <h3 className="text-lg font-bold group-hover:text-primary transition-colors mb-2">{industry.name}</h3>
                  <p className="text-sm text-muted-foreground leading-relaxed line-clamp-3">{industry.description}</p>
                </Link>
              ))}
            </div>
            <div className="mt-8 text-center sm:hidden">
              <Link href="/industries" className="text-sm font-medium text-primary hover:text-primary/80 transition-colors">
                View all industries &rarr;
              </Link>
            </div>
          </div>
        </section>

        {/* Platform Comparisons */}
        <section className="py-24 lg:py-32 bg-muted/30">
          <div className="container-premium">
            <div className="flex items-end justify-between mb-12">
              <div>
                <h2 className="text-3xl lg:text-4xl font-black tracking-tight mb-4">
                  <span className="bg-gradient-to-r from-primary to-secondary bg-clip-text text-transparent">
                    Platform Comparisons
                  </span>
                </h2>
                <p className="text-lg text-muted-foreground max-w-xl">
                  See how SkillLedger compares to other platforms and approaches.
                </p>
              </div>
              <Link href="/compare" className="hidden sm:flex items-center gap-2 text-sm font-medium text-primary hover:text-primary/80 transition-colors">
                View all comparisons <ArrowRight className="w-4 h-4" />
              </Link>
            </div>
            <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
              {featuredComparisons.map((comparison) => (
                <Link
                  key={comparison.slug}
                  href={`/compare/${comparison.slug}`}
                  className="card-feature p-6 hover:shadow-lg transition-all duration-200 group"
                >
                  <h3 className="text-lg font-bold group-hover:text-primary transition-colors mb-2 line-clamp-2">{comparison.title}</h3>
                  <p className="text-sm text-muted-foreground leading-relaxed line-clamp-3">{comparison.description}</p>
                </Link>
              ))}
            </div>
            <div className="mt-8 text-center sm:hidden">
              <Link href="/compare" className="text-sm font-medium text-primary hover:text-primary/80 transition-colors">
                View all comparisons &rarr;
              </Link>
            </div>
          </div>
        </section>

        {/* FAQ */}
        <section className="py-24 lg:py-32">
          <div className="container-premium">
            <div className="text-center mb-12">
              <h2 className="text-3xl lg:text-4xl font-black tracking-tight mb-4">
                <span className="bg-gradient-to-r from-primary to-secondary bg-clip-text text-transparent">
                  Frequently Asked Questions
                </span>
              </h2>
              <p className="text-lg text-muted-foreground max-w-xl mx-auto">
                Everything you need to know about exchanging professional skills on SkillLedger.
              </p>
            </div>
            <div className="max-w-3xl mx-auto space-y-6">
              {homeFaqs.map((faq) => (
                <div key={faq.question} className="border border-border rounded-xl p-6">
                  <h3 className="font-bold mb-3">{faq.question}</h3>
                  <p className="text-muted-foreground leading-relaxed">{faq.answer}</p>
                </div>
              ))}
            </div>
          </div>
        </section>

        {/* Newsletter Signup */}
        <NewsletterSignup variant="section" />

        {/* Final CTA */}
        <section className="py-24 lg:py-32">
          <div className="container-premium text-center">
            <h2 className="text-3xl lg:text-4xl font-black tracking-tight mb-6">
              Ready to Start Exchanging Skills?
            </h2>
            <p className="text-lg text-muted-foreground max-w-xl mx-auto mb-10">
              Trade your skills for services you need. No cash, no commissions. Try it free for 30 days.
            </p>
            <div className="flex flex-col gap-4 sm:flex-row sm:justify-center sm:gap-6">
              <Link href="/register" className="btn-primary hover:scale-105 transition-all duration-300 shadow-lg hover:shadow-xl">
                Start Free Trial
              </Link>
              <Link href="/resources" className="btn-secondary hover:scale-105 transition-all duration-300">
                Learn More
              </Link>
            </div>
          </div>
        </section>
      </div>

      {/* Authenticated dashboard — loaded client-side only, overlays when authenticated */}
      <AuthenticatedHomeWrapper />
    </>
  )
}
