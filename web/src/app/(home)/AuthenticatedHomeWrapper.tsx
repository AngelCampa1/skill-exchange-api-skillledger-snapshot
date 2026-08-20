'use client'

import dynamic from 'next/dynamic'

const AuthenticatedHome = dynamic(() => import('./AuthenticatedHome'), {
  ssr: false,
  loading: () => null,
})

export default function AuthenticatedHomeWrapper() {
  return <AuthenticatedHome />
}
