import Link from 'next/link'

interface RelatedItem {
  title: string
  href: string
  description?: string
}

interface RelatedContentProps {
  items: RelatedItem[]
  title?: string
}

export function RelatedContent({ items, title = 'Related Resources' }: RelatedContentProps) {
  if (!items.length) return null
  return (
    <section>
      <h2 className="text-xl font-bold mb-6">{title}</h2>
      <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-4">
        {items.map((item) => (
          <Link
            key={item.href}
            href={item.href}
            className="border border-border rounded-xl p-5 hover:border-primary/50 hover:shadow-md transition-all duration-200 group"
          >
            <h3 className="font-semibold mb-2 group-hover:text-primary transition-colors text-sm">{item.title}</h3>
            {item.description && (
              <p className="text-xs text-muted-foreground leading-relaxed">{item.description}</p>
            )}
          </Link>
        ))}
      </div>
    </section>
  )
}
