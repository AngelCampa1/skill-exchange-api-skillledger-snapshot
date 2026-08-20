interface KeyTakeawaysProps {
  children: React.ReactNode
}

export function KeyTakeaways({ children }: KeyTakeawaysProps) {
  return (
    <aside className="my-8 rounded-lg border border-primary/20 bg-primary/5 p-6">
      <div className="flex items-center gap-2 mb-3">
        <svg
          xmlns="http://www.w3.org/2000/svg"
          viewBox="0 0 24 24"
          fill="none"
          stroke="currentColor"
          strokeWidth={2}
          strokeLinecap="round"
          strokeLinejoin="round"
          className="h-5 w-5 text-primary shrink-0"
          aria-hidden="true"
        >
          <path d="M15 14c.2-1 .7-1.7 1.5-2.5 1-.9 1.5-2.2 1.5-3.5A6 6 0 0 0 6 8c0 1 .2 2.2 1.5 3.5.7.7 1.3 1.5 1.5 2.5" />
          <path d="M9 18h6" />
          <path d="M10 22h4" />
        </svg>
        <h4 className="text-base font-bold text-primary">Key Takeaways</h4>
      </div>
      <div className="text-sm text-foreground/90 [&>ul]:mb-0 [&>ul]:ml-4 [&>ul]:list-disc [&>ul]:space-y-1">
        {children}
      </div>
    </aside>
  )
}
