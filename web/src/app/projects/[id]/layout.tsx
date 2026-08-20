import { Metadata } from 'next'
import { SITE_CONFIG, TARGET_KEYWORDS, generateProjectSchema } from '@/lib/seo'
import { JsonLd } from '@/components/marketing/JsonLd'

interface ProjectLayoutProps {
  children: React.ReactNode
  params: Promise<{ id: string }>
}

// Fetch project data for metadata generation
async function getProject(id: string) {
  try {
    // Use absolute URL for server-side fetch
    const baseUrl = process.env.NEXT_PUBLIC_API_URL || 'http://localhost:8030'
    const response = await fetch(`${baseUrl}/api/project/${id}`, {
      next: { revalidate: 3600 }, // Cache for 1 hour
    })

    if (!response.ok) {
      return null
    }

    return response.json()
  } catch {
    return null
  }
}

export async function generateMetadata({
  params,
}: {
  params: Promise<{ id: string }>
}): Promise<Metadata> {
  const { id } = await params
  const project = await getProject(id)

  // Default metadata if project not found
  if (!project) {
    return {
      title: 'Project Details',
      description: 'View project details and apply on SkillLedger.',
      robots: {
        index: false,
        follow: true,
      },
    }
  }

  const title = project.title
  const description =
    project.shortDescription ||
    project.description?.substring(0, 160) ||
    `View details and apply for ${project.title} on SkillLedger.`

  return {
    title,
    description,
    keywords: [
      ...TARGET_KEYWORDS,
      ...(project.requiredSkillNames || []),
      'project opportunity',
      'collaboration',
    ].join(', '),
    alternates: {
      canonical: `${SITE_CONFIG.url}/projects/${id}`,
    },
    openGraph: {
      type: 'article',
      url: `${SITE_CONFIG.url}/projects/${id}`,
      title: `${title} | ${SITE_CONFIG.name}`,
      description,
      siteName: SITE_CONFIG.name,
      images: [
        {
          url: `${SITE_CONFIG.url}/android-chrome-512x512.png`,
          width: 512,
          height: 512,
          alt: title,
        },
      ],
    },
    twitter: {
      card: 'summary',
      title: `${title} | ${SITE_CONFIG.name}`,
      description,
    },
  }
}

export default async function ProjectLayout({
  children,
  params,
}: ProjectLayoutProps) {
  const { id } = await params
  const project = await getProject(id)

  return (
    <>
      {/* Inject JSON-LD structured data if project exists */}
      {project && (
        <JsonLd
          schema={generateProjectSchema({
            id,
            title: project.title,
            description: project.description || project.shortDescription || '',
            category: project.category,
            createdAt: project.createdAt,
            creator: project.client
              ? {
                  name: project.client.displayName || project.client.userName || 'Unknown',
                  url: project.client.id
                    ? `${SITE_CONFIG.url}/profile/${project.client.id}`
                    : undefined,
                }
              : undefined,
          })}
        />
      )}
      {children}
    </>
  )
}
