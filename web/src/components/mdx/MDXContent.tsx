import { MDXRemote } from 'next-mdx-remote/rsc'
import remarkGfm from 'remark-gfm'
import Link from 'next/link'
import { KeyTakeaways } from './KeyTakeaways'
import { DefinitionBox } from './DefinitionBox'

interface MDXContentProps {
  content: string
}

// Safe link component: blocks javascript: and data: hrefs to prevent XSS.
// Internal links (starting with /) use Next.js <Link> for client-side navigation.
// External links get target="_blank" and rel="noopener noreferrer nofollow".
// Exported for unit testing.
export function SafeLink({ href, children, ...props }: React.AnchorHTMLAttributes<HTMLAnchorElement>) {
  const isSafe = !href || (!href.startsWith('javascript:') && !href.startsWith('data:'))
  if (!isSafe) return <span>{children}</span>

  const linkClass = 'text-primary underline underline-offset-2 hover:opacity-80 transition-opacity'

  if (href?.startsWith('/')) {
    return (
      <Link href={href} className={linkClass}>
        {children}
      </Link>
    )
  }

  const isExternal = href?.startsWith('http')
  return (
    <a
      href={href}
      {...props}
      {...(isExternal ? { target: '_blank', rel: 'noopener noreferrer nofollow' } : {})}
      className={linkClass}
    >
      {children}
    </a>
  )
}

const mdxComponents = {
  KeyTakeaways,
  DefinitionBox,
  a: SafeLink,
  // Basic heading styles (no @tailwindcss/typography plugin required)
  h1: (props: React.HTMLAttributes<HTMLHeadingElement>) => <h1 className="text-3xl font-bold mt-8 mb-4" {...props} />,
  h2: (props: React.HTMLAttributes<HTMLHeadingElement>) => <h2 className="text-2xl font-bold mt-8 mb-3 border-b border-border pb-2" {...props} />,
  h3: (props: React.HTMLAttributes<HTMLHeadingElement>) => <h3 className="text-xl font-semibold mt-6 mb-2" {...props} />,
  p: (props: React.HTMLAttributes<HTMLParagraphElement>) => <p className="mb-4 leading-relaxed text-foreground/90" {...props} />,
  ul: (props: React.HTMLAttributes<HTMLUListElement>) => <ul className="mb-4 ml-6 list-disc space-y-1" {...props} />,
  ol: (props: React.OlHTMLAttributes<HTMLOListElement>) => <ol className="mb-4 ml-6 list-decimal space-y-1" {...props} />,
  li: (props: React.LiHTMLAttributes<HTMLLIElement>) => <li className="leading-relaxed" {...props} />,
  blockquote: (props: React.BlockquoteHTMLAttributes<HTMLQuoteElement>) => <blockquote className="border-l-4 border-primary/40 pl-4 italic text-muted-foreground my-4" {...props} />,
  table: (props: React.TableHTMLAttributes<HTMLTableElement>) => <div className="overflow-x-auto mb-4"><table className="w-full text-sm border-collapse" {...props} /></div>,
  th: (props: React.ThHTMLAttributes<HTMLTableCellElement>) => <th className="border border-border bg-muted px-4 py-2 text-left font-semibold" {...props} />,
  td: (props: React.TdHTMLAttributes<HTMLTableCellElement>) => <td className="border border-border px-4 py-2" {...props} />,
  code: (props: React.HTMLAttributes<HTMLElement>) => <code className="bg-muted px-1.5 py-0.5 rounded text-sm font-mono" {...props} />,
  pre: (props: React.HTMLAttributes<HTMLPreElement>) => <pre className="bg-muted p-4 rounded-xl overflow-x-auto mb-4 text-sm font-mono" {...props} />,
  hr: () => <hr className="border-border my-8" />,
}

export function MDXContent({ content }: MDXContentProps) {
  return (
    <div className="text-base leading-relaxed max-w-none">
      <MDXRemote
        source={content}
        components={mdxComponents}
        options={{
          mdxOptions: {
            remarkPlugins: [remarkGfm],
          },
        }}
      />
    </div>
  )
}
