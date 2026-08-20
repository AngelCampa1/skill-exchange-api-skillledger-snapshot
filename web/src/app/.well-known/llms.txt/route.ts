import { NextResponse } from 'next/server'

export function GET() {
  return NextResponse.redirect(new URL('/llms.txt', 'https://skillledger.app'), 301)
}
