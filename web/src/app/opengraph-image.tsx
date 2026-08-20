import { ImageResponse } from 'next/og'

export const alt = 'SkillLedger - Professional Collaboration Platform'
export const size = { width: 1200, height: 630 }
export const contentType = 'image/png'

export default function Image() {
  return new ImageResponse(
    (
      <div style={{
        background: 'linear-gradient(135deg, #0f172a 0%, #1e293b 50%, #0f172a 100%)',
        width: '100%',
        height: '100%',
        display: 'flex',
        flexDirection: 'column',
        alignItems: 'center',
        justifyContent: 'center',
        fontFamily: 'sans-serif',
      }}>
        <div style={{ fontSize: 72, fontWeight: 900, color: '#f8fafc', letterSpacing: '-2px', marginBottom: 16 }}>
          SkillLedger
        </div>
        <div style={{ fontSize: 28, color: '#94a3b8', fontWeight: 500 }}>
          Professional Collaboration Platform
        </div>
        <div style={{
          marginTop: 40,
          fontSize: 20,
          color: '#64748b',
          maxWidth: 700,
          textAlign: 'center',
          lineHeight: 1.5
        }}>
          Exchange skills, build reputation, and collaborate with professionals worldwide
        </div>
      </div>
    ),
    { ...size }
  )
}
