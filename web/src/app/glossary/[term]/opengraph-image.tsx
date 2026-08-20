import { ImageResponse } from 'next/og'
import { glossaryData, getTermBySlug } from '@/lib/data/glossary-data'

export function generateStaticParams() {
  return glossaryData.map((t) => ({ term: t.slug }))
}

export const alt = 'SkillLedger Glossary Term'
export const size = { width: 1200, height: 630 }
export const contentType = 'image/png'

export default async function Image({ params }: { params: Promise<{ term: string }> }) {
  const { term: termSlug } = await params
  const term = getTermBySlug(termSlug)
  const title = term?.term ?? 'Glossary Term'
  const definition = term?.definition.slice(0, 120) ?? ''

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
            Glossary
          </div>
        </div>
        <div style={{ display: 'flex', flexDirection: 'column', gap: 16 }}>
          <div style={{ fontSize: 56, fontWeight: 900, color: '#f8fafc', lineHeight: 1.2, letterSpacing: '-1px', display: 'flex' }}>
            {title}
          </div>
          {definition && (
            <div style={{ fontSize: 20, color: '#94a3b8', maxWidth: 800, lineHeight: 1.5, display: 'flex' }}>
              {definition}...
            </div>
          )}
        </div>
        <div style={{ fontSize: 16, color: '#64748b', display: 'flex' }}>
          skillledger.app/glossary
        </div>
      </div>
    ),
    { ...size }
  )
}
