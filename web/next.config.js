const path = require('path')

/** @type {import('next').NextConfig} */
const nextConfig = {
  output: 'standalone',
  // Performance monitoring configuration
  productionBrowserSourceMaps: false, // Disable in production for performance

  // Image optimization configuration
  // BUG-38 FIX: Removed wildcard hostname ('**') that allowed SSRF through the
  // Next.js image optimisation proxy. Only specific trusted origins are now listed.
  images: {
    unoptimized: true,
    remotePatterns: [
      {
        protocol: 'https',
        hostname: 'cdn.skillledger.app',
      },
      {
        protocol: 'https',
        hostname: '*.skillledger.app',
      },
      {
        protocol: 'http',
        hostname: 'localhost',
        port: '8030',
        pathname: '/api/**',
      },
    ],
    formats: ['image/avif', 'image/webp'], // Modern formats for better performance
  },

  // Bundle analyzer configuration (development only)
  ...(process.env.ANALYZE === 'true' && {
    webpack: (config, { isServer }) => {
      if (!isServer) {
        config.resolve.alias = {
          ...config.resolve.alias,
          '@': path.resolve(__dirname, 'src'),
        }
      }
      return config
    },
  }),

  async rewrites() {
    // BUG-FE-012 FIX: Use environment variable for API base URL in rewrites
    // In production, set NEXT_PUBLIC_API_URL to your backend URL
    const backendUrl = process.env.NEXT_PUBLIC_API_URL || 'http://localhost:8030'

    return [
      {
        // Rewrite all API calls EXCEPT /api/auth/* (handled by custom API route for cookie forwarding)
        // The negative lookahead (?!auth) excludes paths starting with 'auth'
        source: '/api/:path((?!auth).*)*',
        destination: `${backendUrl}/api/:path*`, // Backend API proxy
      },
    ]
  },
}

module.exports = nextConfig
