import * as React from "react"
import { ChevronRight, Home } from "lucide-react"
import Link from "next/link"

export interface BreadcrumbItem {
  label: string
  href?: string
  icon?: React.ReactNode
}

export interface BreadcrumbProps {
  items: BreadcrumbItem[]
  showHome?: boolean
  homeHref?: string
  separator?: React.ReactNode
  className?: string
}

export const Breadcrumb: React.FC<BreadcrumbProps> = ({
  items,
  showHome = true,
  homeHref = "/",
  separator,
  className,
}) => {
  const defaultSeparator = <ChevronRight className="h-4 w-4 text-muted-foreground" aria-hidden="true" />

  const allItems: BreadcrumbItem[] = showHome
    ? [{ label: "Home", href: homeHref, icon: <Home className="h-4 w-4" aria-hidden="true" /> }, ...items]
    : items

  return (
    <nav aria-label="Breadcrumb" className={className}>
      <ol className="flex items-center space-x-2 text-sm">
        {allItems.map((item, index) => {
          const isLast = index === allItems.length - 1

          return (
            <li key={index} className="flex items-center space-x-2">
              {index > 0 && (
                <span className="flex items-center" aria-hidden="true">
                  {separator || defaultSeparator}
                </span>
              )}

              {isLast || !item.href ? (
                <span
                  className={`flex items-center space-x-1.5 ${
                    isLast
                      ? "font-medium text-foreground"
                      : "text-muted-foreground"
                  }`}
                  aria-current={isLast ? "page" : undefined}
                >
                  {item.icon && <span>{item.icon}</span>}
                  <span>{item.label}</span>
                </span>
              ) : (
                <Link
                  href={item.href}
                  className="flex items-center space-x-1.5 text-muted-foreground hover:text-foreground transition-colors"
                >
                  {item.icon && <span>{item.icon}</span>}
                  <span>{item.label}</span>
                </Link>
              )}
            </li>
          )
        })}
      </ol>
    </nav>
  )
}

// Simple breadcrumb for common use cases
export interface SimpleBreadcrumbProps {
  items: string[]
  className?: string
}

export const SimpleBreadcrumb: React.FC<SimpleBreadcrumbProps> = ({ items, className }) => {
  const breadcrumbItems: BreadcrumbItem[] = items.map((label, index) => ({
    label,
    // Last item has no href (current page)
    href: index < items.length - 1 ? "#" : undefined,
  }))

  return <Breadcrumb items={breadcrumbItems} showHome={false} className={className} />
}
