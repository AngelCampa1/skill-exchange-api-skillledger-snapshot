'use client'

import Image from 'next/image'

interface LogoProps {
  size?: 'small' | 'medium' | 'large' | 'hero'
  showText?: boolean
  className?: string
  isLinkable?: boolean
  altText?: string
}

const sizeMap = {
  small: { width: 40, height: 40 },
  medium: { width: 60, height: 60 },
  large: { width: 80, height: 80 },
  hero: { width: 160, height: 160 }
}

export function Logo({ size = 'medium', showText = true, className = '', isLinkable = false, altText }: LogoProps) {
  const dimensions = sizeMap[size]
  const isHeroSize = size === 'hero'

  // Generate contextual alt text based on usage
  const getAltText = () => {
    if (altText) return altText
    if (isLinkable) return 'SkillLedger - Navigate to homepage'
    if (isHeroSize) return 'SkillLedger - Professional Skills Collaboration Platform'
    return 'SkillLedger Logo'
  }

  return (
    <div className={`flex items-center ${showText ? 'space-x-3' : ''} ${className} group`}>
      <div className="relative">
        {/* Enhanced animated glow effect for logo */}
        <div className={`absolute inset-0 bg-gradient-to-br from-primary/30 to-secondary/20 rounded-full blur-xl ${isHeroSize ? 'opacity-60' : 'opacity-0 group-hover:opacity-100'} transition-all duration-500 animate-pulse`} aria-hidden="true"></div>
        <div className={`absolute inset-0 bg-gradient-to-br from-primary/20 to-secondary/10 rounded-full blur-lg ${isHeroSize ? 'opacity-80' : 'opacity-0 group-hover:opacity-100'} transition-all duration-500`} aria-hidden="true"></div>

        {/* Logo image with enhanced styling */}
        <div className={`relative z-10 flex items-center justify-center ${isHeroSize ? 'shadow-2xl shadow-primary/30' : 'shadow-lg group-hover:shadow-primary/30'} transition-all duration-300 group-hover:scale-110 ${isHeroSize ? 'p-4' : ''} rounded-full`}>
          <Image
            src="/logo.svg"
            alt={getAltText()}
            width={dimensions.width}
            height={dimensions.height}
            className="object-contain drop-shadow-lg"
            priority
          />
        </div>
      </div>

      {/* Logo text */}
      {showText && (
        <span className="font-black tracking-tight group-hover:text-primary transition-colors duration-300"
              style={{ fontSize: size === 'hero' ? '2.5rem' : size === 'large' ? '1.5rem' : size === 'small' ? '1rem' : '1.25rem' }}>
          SkillLedger
        </span>
      )}
    </div>
  )
}