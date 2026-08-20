interface DefinitionBoxProps {
  term: string
  children: React.ReactNode
}

export function DefinitionBox({ term, children }: DefinitionBoxProps) {
  return (
    <aside className="my-6 rounded-lg border border-border bg-muted/50 p-5">
      <div className="flex items-start gap-3">
        <svg
          xmlns="http://www.w3.org/2000/svg"
          viewBox="0 0 24 24"
          fill="none"
          stroke="currentColor"
          strokeWidth={2}
          strokeLinecap="round"
          strokeLinejoin="round"
          className="h-5 w-5 text-muted-foreground shrink-0 mt-0.5"
          aria-hidden="true"
        >
          <path d="M4 19.5v-15A2.5 2.5 0 0 1 6.5 2H20v20H6.5a2.5 2.5 0 0 1 0-5H20" />
        </svg>
        <div>
          <dt className="text-sm font-bold text-foreground mb-1">{term}</dt>
          <dd className="text-sm text-muted-foreground leading-relaxed [&>p]:mb-0">
            {children}
          </dd>
        </div>
      </div>
    </aside>
  )
}
