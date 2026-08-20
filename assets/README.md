# SkillLedger Assets Directory

## Overview
This directory contains all visual assets, branding materials, and marketing resources for SkillLedger's production deployment.

## Directory Structure

### 🎨 Branding (`/branding/`)
Core brand identity assets including logos, color palettes, fonts, and brand guidelines.

- **`/logos/`** - Logo variations in multiple formats
  - `/primary/` - Main logo with text
  - `/icon-only/` - Symbol only (favicons, mobile)
  - `/monochrome/` - Single color versions
  - `/reversed/` - White versions for dark backgrounds
- **`/colors/`** - Color palette files and swatches
- **`/fonts/`** - Web fonts and typography assets
- **`/guidelines/`** - Brand standards and usage guides

### 📱 Social Media (`/social/`)
Assets optimized for social media platforms and sharing.

- **`/og-images/`** - Open Graph images for link sharing
- **`/twitter-cards/`** - Twitter-specific card images
- **`/linkedin-assets/`** - LinkedIn banners and profile images
- **`/facebook-assets/`** - Facebook covers and promotional images

### 📧 Email (`/email/`)
Email marketing and transactional email assets.

- **`/templates/`** - Email template backgrounds and layouts
- **`/headers/`** - Email header images and logos
- **`/signatures/`** - Email signature assets

### 📊 Presentations (`/presentations/`)
Assets for presentations, pitch decks, and corporate materials.

- **`/templates/`** - Slide templates and layouts
- **`/backgrounds/`** - Background images and patterns
- **`/slide-components/`** - Reusable slide elements

### 🎯 Marketing (`/marketing/`)
Marketing campaign assets and promotional materials.

- **`/banners/`** - Web banners and display advertisements
- **`/advertisements/`** - Social media ads and promotional graphics
- **`/print-materials/`** - Business cards, letterheads, brochures

## Asset Guidelines

### File Naming Convention
```
skillledger-[type]-[variant]-[size].[format]

Examples:
- skillledger-logo-primary-horizontal.svg
- skillledger-icon-monochrome-32x32.png
- skillledger-og-homepage-1200x630.png
```

### Required Formats
- **Logos**: SVG (vector), PNG (raster), optional PDF for print
- **Icons**: PNG in multiple sizes (16x16, 32x32, 64x64, etc.)
- **Social**: PNG or JPEG optimized for each platform
- **Print**: PDF with CMYK color profile

### Optimization Requirements
- **SVG**: Optimize with SVGO, remove unnecessary metadata
- **PNG**: Compress with TinyPNG or similar tools
- **JPEG**: Quality 85-90% for optimal balance
- **WebP**: Provide WebP versions with PNG fallbacks

## Asset Status

### ✅ Completed
- Directory structure created
- Comprehensive visual assets guide
- Branding guidelines documentation

### 🔄 In Progress
- Logo design and variations
- Color palette implementation
- Typography system setup

### 📋 To Do
- [ ] Create primary logo in SVG format
- [ ] Generate favicon set for web deployment
- [ ] Design social media asset templates
- [ ] Create email header and signature assets
- [ ] Develop presentation template system
- [ ] Setup automated asset optimization pipeline

## Usage Guidelines

### For Developers
1. Reference `/docs/production/branding/visual-assets-guide.md` for implementation details
2. Use CSS custom properties for colors and spacing
3. Implement responsive image techniques with srcset
4. Optimize asset loading with preload hints

### For Designers
1. Follow brand guidelines for all asset creation
2. Use version control for asset updates
3. Coordinate with development team for implementation
4. Maintain consistency across all platforms

### For Marketing
1. Use approved assets from appropriate directories
2. Follow file naming conventions
3. Request new assets through proper channels
4. Ensure brand compliance in all materials

## Maintenance

### Regular Tasks
- **Weekly**: Review and optimize new assets
- **Monthly**: Audit asset usage and consistency
- **Quarterly**: Update brand guidelines if needed
- **Annually**: Comprehensive brand asset review

### Version Control
All assets are version controlled with the main repository:
- Use semantic versioning for major brand updates
- Tag releases when significant asset sets are updated
- Document changes in commit messages
- Maintain backward compatibility when possible

## Support

For questions about assets or branding guidelines:
- Review the visual assets guide: `/docs/production/branding/visual-assets-guide.md`
- Contact the brand guardian for approval of new assets
- Submit asset requests through the designated process
- Report brand compliance issues immediately

---

**Last Updated**: 2025-09-22  
**Maintainer**: SkillLedger Brand Team  
**Documentation**: `/docs/production/branding/visual-assets-guide.md`