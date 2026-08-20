import { ImageResponse } from 'next/og'
import { featuresData } from '@/lib/data/features-data'

export function generateStaticParams() {
  return featuresData.map((f) => ({ slug: f.slug }))
}

export const alt = 'SkillLedger Feature'
export const size = { width: 1200, height: 630 }
export const contentType = 'image/png'

export default async function Image({ params }: { params: Promise<{ slug: string }> }) {
  const { slug } = await params
  const feature = featuresData.find((f) => f.slug === slug)
  const title = feature?.name ?? 'Feature'
  const subtitle = feature?.tagline ?? ''

  return new ImageResponse(
    (
      <div style={{
        background: 'linear-gradient(135deg, #0f172a 0%, #1e293b 50%, #0f172a 100%)',
        width: '100%',
        height: '100%',
        display: 'flex',
        flexDirection: 'column',
        justifyContent: 'space-between',
        padding: '60px',
        fontFamily: 'sans-serif',
      }}>
        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
          <div style={{ fontSize: 32, fontWeight: 900, color: '#f8fafc', letterSpacing: '-1px', display: 'flex' }}>
            SkillLedger
          </div>
          <div style={{
            fontSize: 14,
            fontWeight: 700,
            color: '#818cf8',
            background: 'rgba(129, 140, 248, 0.15)',
            padding: '6px 16px',
            borderRadius: 999,
            textTransform: 'uppercase',
            letterSpacing: '1px',
            display: 'flex',
          }}>
            Feature
          </div>
        </div>
        <div style={{ display: 'flex', flexDirection: 'column', gap: 16 }}>
          <div style={{ fontSize: 52, fontWeight: 900, color: '#f8fafc', lineHeight: 1.2, letterSpacing: '-1px', maxWidth: 900, display: 'flex' }}>
            {title}
          </div>
          {subtitle && (
            <div style={{ fontSize: 20, color: '#94a3b8', maxWidth: 800, display: 'flex' }}>
              {subtitle}
            </div>
          )}
        </div>
        <div style={{ fontSize: 16, color: '#64748b', display: 'flex' }}>
          skillledger.app/features
        </div>
      </div>
    ),
    { ...size }
  )
}
