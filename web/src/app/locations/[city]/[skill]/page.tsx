import { notFound } from'next/navigation'
import type { Metadata } from'next'
import Link from'next/link'
import { citiesData, getCityBySlug } from'@/lib/data/cities-data'
import { categoriesData, getCategoryBySlug } from'@/lib/data/categories-data'
import { skillToCategorySlug, findArticlesForCategory, findScenariosForCategory, findComparisonsForCategory } from'@/lib/cross-links'
import {
  buildPublicPageMetadata,
  generateLocalBusinessSchema,
  generateFAQSchema,
  generateBreadcrumbSchema,
  SITE_CONFIG,
} from'@/lib/seo'
import { JsonLd } from'@/components/marketing/JsonLd'
import { FunnelCTA } from'@/components/marketing/FunnelCTA'
import { FunnelLinks } from'@/components/marketing/FunnelLinks'

export const dynamicParams = false

interface Props {
  params: Promise<{ city: string; skill: string }>
}

export async function generateStaticParams() {
  return citiesData.flatMap((city) =>
    categoriesData.map((cat) => ({ city: city.slug, skill: cat.slug }))
  )
}

export async function generateMetadata({ params }: Props): Promise<Metadata> {
  const { city: citySlug, skill } = await params
  const city = getCityBySlug(citySlug)
  const cat = getCategoryBySlug(skill)
  if (!city || !cat) return {}
  return buildPublicPageMetadata(
    `${cat.name} Professionals in ${city.city}`,
    `Find and exchange ${cat.name.toLowerCase()} services with professionals in ${city.city}, ${city.state}. ~${cat.averageCreditRate} credits/hr on SkillLedger. 30-day free trial.`,
    `/locations/${citySlug}/${skill}`,
    [`${cat.name} ${city.city}`, `${city.city} ${cat.name.toLowerCase()} freelancers`,'skill exchange']
  )
}

export default async function LocationSkillPage({ params }: Props) {
  const { city: citySlug, skill } = await params
  const city = getCityBySlug(citySlug)
  const cat = getCategoryBySlug(skill)
  if (!city || !cat) notFound()

  const isTopSkill = city.topSkills.some((s) => skillToCategorySlug(s) === cat.slug)

  const demandText =
    cat.demandLevel ==='high'
      ? `${cat.name} ranks among the most requested services on SkillLedger. ${isTopSkill ? `In ${city.city}, it is consistently one of the top traded skills.` : `${city.city} professionals are increasingly active in this category as they look to grow their businesses through exchange.`}`
      : cat.demandLevel ==='medium'
        ? `${cat.name} sees steady trading volume in ${city.city}. Professionals here use it to get quality ${cat.name.toLowerCase()} work done without committing to long-term retainers.`
        : `${cat.name} is a specialist category in ${city.city}. Fewer providers list here, which means exchanges tend to be easier to negotiate.`

  const bullets = [
    `${city.city}'s economy, led by industries like ${city.topSkills[0] ||'professional services'}, creates steady demand for ${cat.name.toLowerCase()} work from people who understand local business needs.`,
    `At ~${cat.averageCreditRate} credits/hr, ${cat.name} exchanges in ${city.city} compare well against typical ${city.state} freelance rates.`,
    isTopSkill
      ? `${cat.name} is already one of the most-traded skills in ${city.city}. You are more likely to find active exchange partners here.`
      : `Because SkillLedger supports remote exchanges, ${city.city} professionals can trade ${cat.name.toLowerCase()} services with people in their area or anywhere in the country.`,
  ]

  const faqs = [
    {
      question: `How do I find ${cat.name} professionals in ${city.city} on SkillLedger?`,
      answer: `Filter the SkillLedger marketplace by the ${cat.name} category and refine by location to see professionals in ${city.city}, ${city.state}. You can also browse nationally, since most ${cat.name.toLowerCase()} work can be done remotely. Each profile lists the provider's credit rate, completed exchanges, and reputation score.`,
    },
    {
      question: `What's the average credit rate for ${cat.name} work in ${city.city}?`,
      answer: `The platform-wide average for ${cat.name} is ${cat.averageCreditRate} credits per hour. Rates in ${city.city} can differ depending on specialization, experience, and project complexity. If hourly pricing does not fit your engagement, you can negotiate a flat project-based rate instead.`,
    },
    {
      question: `Can I exchange ${cat.name} services remotely with ${city.city} professionals?`,
      answer: `Yes. Most ${cat.name.toLowerCase()} exchanges on SkillLedger happen remotely. ${city.city} professionals on the platform are comfortable with async collaboration, and SkillLedger's built-in messaging, project management, and escrow tools all support remote work.`,
    },
  ]

  const relatedCategories = categoriesData.filter((c) => c.slug !== cat.slug).slice(0, 5)

  const relatedArticles = findArticlesForCategory(cat.slug).slice(0, 3)
  const relatedScenarios = findScenariosForCategory(cat.slug).slice(0, 3)
  const funnelComparisons = findComparisonsForCategory(cat.slug).slice(0, 3)

  const localBusinessSchema = generateLocalBusinessSchema(city, cat)
  const faqSchema = generateFAQSchema(faqs)
  const breadcrumbSchema = generateBreadcrumbSchema([
    { name:'Home', url: SITE_CONFIG.url },
    { name:'Locations', url: `${SITE_CONFIG.url}/locations` },
    { name: city.city, url: `${SITE_CONFIG.url}/skill-exchange/${city.slug}` },
    { name: cat.name, url: `${SITE_CONFIG.url}/locations/${city.slug}/${cat.slug}` },
  ])

  return (
    <>
      <JsonLd schema={localBusinessSchema} />
      <JsonLd schema={faqSchema} />
      <JsonLd schema={breadcrumbSchema} />
      <div className="min-h-screen bg-background">
        <div className="container-premium py-16 lg:py-24">
          <nav className="text-sm text-muted-foreground mb-8">
            <Link href="/" className="hover:text-foreground">Home</Link>
            {' /'}
            <Link href="/locations" className="hover:text-foreground">Locations</Link>
            {' /'}
            <Link href={`/skill-exchange/${city.slug}`} className="hover:text-foreground">{city.city}</Link>
            {' /'}
            <span>{cat.name}</span>
          </nav>

          <header className="mb-16 max-w-3xl">
            <div className="flex items-center gap-3 mb-4">
              <span className={`text-sm px-3 py-1 rounded-full font-medium ${cat.demandLevel ==='high' ?'bg-green-100  text-green-700' : cat.demandLevel ==='medium' ?'bg-yellow-100  text-yellow-700' :'bg-gray-100  text-gray-600'}`}>
                {cat.demandLevel.charAt(0).toUpperCase() + cat.demandLevel.slice(1)} Demand
              </span>
              {isTopSkill && (
                <span className="text-sm px-3 py-1 rounded-full font-medium bg-blue-100  text-blue-700">
                  Top Skill in {city.city}
                </span>
              )}
            </div>
            <h1 className="text-4xl lg:text-5xl font-black tracking-tight mb-6">
              {cat.name} Professionals in {city.city}
            </h1>
            <p className="text-xl text-muted-foreground leading-relaxed mb-8">
              {demandText}
            </p>
            <Link href="/register" className="btn-primary">Find {cat.name} Professionals</Link>
          </header>

          <section className="mb-16 max-w-3xl">
            <h2 className="text-2xl font-bold mb-6">Why Exchange {cat.name} in {city.city}</h2>
            <ul className="space-y-4">
              {bullets.map((bullet, i) => (
                <li key={i} className="flex gap-3">
                  <span className="flex-shrink-0 w-6 h-6 rounded-full bg-primary/10 text-primary flex items-center justify-center text-xs font-bold mt-0.5">✓</span>
                  <p className="text-muted-foreground leading-relaxed">{bullet}</p>
                </li>
              ))}
            </ul>
          </section>

          {/* Popular Trade Partners */}
          <section className="mb-12">
            <h2 className="text-xl font-bold mb-4">Popular Trade Partners in {city.city}</h2>
            <div className="grid sm:grid-cols-2 lg:grid-cols-3 gap-3">
              {categoriesData
                .filter((c) => c.slug !== cat.slug)
                .filter((c) => city.topSkills.some((s) => skillToCategorySlug(s) === c.slug) || c.demandLevel ==='high')
                .slice(0, 6)
                .map((other) => (
                  <Link
                    key={other.slug}
                    href={`/trade/${cat.slug}/for/${other.slug}`}
                    className="card-feature p-3 hover:border-primary/30 transition-colors text-sm"
                  >
                    <span className="font-medium">{cat.name}</span>
                    <span className="text-muted-foreground"> for </span>
                    <span className="font-medium">{other.name}</span>
                  </Link>
                ))}
            </div>
          </section>

          <section className="mb-16">
            <h2 className="text-2xl font-bold mb-8">How It Works</h2>
            <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
              {[
                { step:'1', title:'Create your profile', desc: `List your skills, set your credit rate, and describe what you need.` },
                { step:'2', title:'Find a match', desc: `Search for ${cat.name} professionals in ${city.city} or use the match tool to surface good fits.` },
                { step:'3', title:'Agree on terms', desc:'Set deliverables, credit amounts, and a timeline. Both parties confirm before work begins.' },
                { step:'4', title:'Exchange and review', desc:'Credits transfer through escrow when the work is done. Leave a review to build your reputation.' },
              ].map((item) => (
                <div key={item.step} className="border border-border rounded-xl p-5">
                  <div className="w-8 h-8 rounded-full bg-primary/10 text-primary flex items-center justify-center font-bold text-sm mb-3">{item.step}</div>
                  <h3 className="font-bold mb-2">{item.title}</h3>
                  <p className="text-sm text-muted-foreground">{item.desc}</p>
                </div>
              ))}
            </div>
          </section>

          <section className="mb-16 max-w-3xl">
            <h2 className="text-2xl font-bold mb-8">Frequently Asked Questions</h2>
            <div className="space-y-6">
              {faqs.map((faq, i) => (
                <div key={i} className="border border-border rounded-xl p-6">
                  <h3 className="font-bold mb-3">{faq.question}</h3>
                  <p className="text-muted-foreground leading-relaxed">{faq.answer}</p>
                </div>
              ))}
            </div>
          </section>

          {relatedArticles.length > 0 && (
            <section className="mb-12">
              <h2 className="text-xl font-bold mb-4">Related Articles</h2>
              <div className="grid sm:grid-cols-3 gap-4">
                {relatedArticles.map((article) => (
                  <Link key={article.slug} href={`/resources/${article.slug}`} className="card-feature p-4 hover:border-primary/30 transition-colors">
                    <h3 className="font-semibold text-sm mb-1">{article.frontmatter.title}</h3>
                    <p className="text-xs text-muted-foreground line-clamp-2">{article.frontmatter.description}</p>
                  </Link>
                ))}
              </div>
            </section>
          )}

          {relatedScenarios.length > 0 && (
            <section className="mb-12">
              <h2 className="text-xl font-bold mb-4">How-To Guides</h2>
              <div className="grid sm:grid-cols-3 gap-4">
                {relatedScenarios.map((s) => (
                  <Link key={s.slug} href={`/how-to/${s.slug}`} className="card-feature p-4 hover:border-primary/30 transition-colors">
                    <h3 className="font-semibold text-sm">{s.title}</h3>
                  </Link>
                ))}
              </div>
            </section>
          )}

          <section className="mb-16">
            <h2 className="text-2xl font-bold mb-6">Other Categories in {city.city}</h2>
            <div className="flex flex-wrap gap-3">
              {relatedCategories.map((c) => (
                <Link
                  key={c.slug}
                  href={`/locations/${city.slug}/${c.slug}`}
                  className="px-4 py-2 border border-border rounded-lg font-medium hover:border-primary hover:text-primary transition-colors text-sm"
                >
                  {c.name}
                </Link>
              ))}
            </div>
          </section>

          <section className="mb-16">
            <h2 className="text-2xl font-bold mb-4">About {city.city}</h2>
            <p className="text-muted-foreground mb-4">
              <Link href={`/skill-exchange/${city.slug}`} className="text-primary hover:underline">
                View all skill exchanges in {city.city}, {city.state} →
              </Link>
            </p>
          </section>

          <FunnelLinks
            stage="tofu"
            comparisons={funnelComparisons}
            howToGuides={relatedScenarios.map((s) => ({ slug: s.slug, title: s.title, skillOffered: s.skillOffered, skillNeeded: s.skillNeeded }))}
          />
          <FunnelCTA stage="tofu" />
        </div>
      </div>
    </>
  )
}
