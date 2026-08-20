import * as React from "react"
import { Search, Command as CommandIcon } from "lucide-react"

export interface CommandItem {
  id: string
  label: string
  description?: string
  icon?: React.ReactNode
  keywords?: string[]
  onSelect: () => void
  group?: string
  shortcut?: string
}

export interface CommandPaletteProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  items: CommandItem[]
  placeholder?: string
  emptyMessage?: string
  closeOnSelect?: boolean
  className?: string
}

export const CommandPalette: React.FC<CommandPaletteProps> = ({
  open,
  onOpenChange,
  items,
  placeholder = "Type a command or search...",
  emptyMessage = "No results found.",
  closeOnSelect = true,
  className,
}) => {
  const [search, setSearch] = React.useState("")
  const [selectedIndex, setSelectedIndex] = React.useState(0)
  const inputRef = React.useRef<HTMLInputElement>(null)
  const listRef = React.useRef<HTMLDivElement>(null)

  const filteredItems = React.useMemo(() => {
    if (!search) return items

    const searchLower = search.toLowerCase()
    return items.filter(
      (item) =>
        item.label.toLowerCase().includes(searchLower) ||
        item.description?.toLowerCase().includes(searchLower) ||
        item.keywords?.some((keyword) => keyword.toLowerCase().includes(searchLower))
    )
  }, [items, search])

  // BUG-001 & BUG-034 FIX: Pre-calculate indices in useMemo to avoid mutable globalIndex
  const { groupedItems, itemIndexMap } = React.useMemo(() => {
    const groups = new Map<string, CommandItem[]>()

    filteredItems.forEach((item) => {
      const group = item.group || "Commands"
      if (!groups.has(group)) {
        groups.set(group, [])
      }
      groups.get(group)!.push(item)
    })

    // Pre-calculate global indices for each item
    const indexMap = new Map<string, number>()
    let currentIndex = 0
    groups.forEach((groupItems) => {
      groupItems.forEach((item) => {
        indexMap.set(item.id, currentIndex++)
      })
    })

    return {
      groupedItems: Array.from(groups.entries()),
      itemIndexMap: indexMap
    }
  }, [filteredItems])

  React.useEffect(() => {
    if (open) {
      setSearch("")
      setSelectedIndex(0)
      setTimeout(() => inputRef.current?.focus(), 0)
    }
  }, [open])

  React.useEffect(() => {
    setSelectedIndex(0)
  }, [search])

  React.useEffect(() => {
    const handleEscape = (e: KeyboardEvent) => {
      if (e.key === "Escape" && open) {
        onOpenChange(false)
      }
    }

    const handleKeyDown = (e: KeyboardEvent) => {
      if (!open) return

      if (e.key === "ArrowDown") {
        e.preventDefault()
        setSelectedIndex((prev) => Math.min(prev + 1, filteredItems.length - 1))
      } else if (e.key === "ArrowUp") {
        e.preventDefault()
        setSelectedIndex((prev) => Math.max(prev - 1, 0))
      } else if (e.key === "Enter") {
        e.preventDefault()
        const item = filteredItems[selectedIndex]
        if (item) {
          item.onSelect()
          if (closeOnSelect) {
            onOpenChange(false)
          }
        }
      }
    }

    if (open) {
      document.addEventListener("keydown", handleEscape)
      document.addEventListener("keydown", handleKeyDown)
    }

    return () => {
      document.removeEventListener("keydown", handleEscape)
      document.removeEventListener("keydown", handleKeyDown)
    }
  }, [open, filteredItems, selectedIndex, closeOnSelect, onOpenChange])

  React.useEffect(() => {
    if (listRef.current) {
      const selectedElement = listRef.current.querySelector(`[data-index="${selectedIndex}"]`)
      if (selectedElement) {
        selectedElement.scrollIntoView({ block: "nearest" })
      }
    }
  }, [selectedIndex])

  const handleItemClick = (item: CommandItem) => {
    item.onSelect()
    if (closeOnSelect) {
      onOpenChange(false)
    }
  }

  if (!open) return null

  return (
    <div
      className="fixed inset-0 z-50 flex items-start justify-center pt-[20vh]"
      role="dialog"
      aria-modal="true"
      aria-label="Command palette"
    >
      {/* Backdrop */}
      <div
        className="fixed inset-0 bg-overlay/80 backdrop-blur-sm animate-in fade-in-0"
        onClick={() => onOpenChange(false)}
        aria-hidden="true"
      />

      {/* Command Palette */}
      <div
        className={`
          relative z-50 w-full max-w-2xl bg-background rounded-xl shadow-lg border border-border
          animate-in fade-in-0 zoom-in-95 slide-in-from-top-4 duration-200
          ${className || ""}
        `}
      >
        {/* Search Input */}
        <div className="flex items-center border-b border-border px-4">
          <Search className="h-5 w-5 text-muted-foreground flex-shrink-0" aria-hidden="true" />
          <input
            ref={inputRef}
            type="text"
            value={search}
            onChange={(e) => setSearch(e.target.value)}
            placeholder={placeholder}
            className="flex-1 bg-transparent px-4 py-4 text-sm text-foreground placeholder:text-muted-foreground focus:outline-none"
            aria-label="Search commands"
          />
          <kbd className="hidden sm:inline-flex items-center gap-1 rounded border border-border bg-muted px-2 py-1 text-xs text-muted-foreground">
            <span className="text-xs">ESC</span>
          </kbd>
        </div>

        {/* Results */}
        <div
          ref={listRef}
          className="max-h-96 overflow-y-auto p-2"
          role="listbox"
        >
          {filteredItems.length === 0 ? (
            <div className="px-4 py-12 text-center text-sm text-muted-foreground">
              {emptyMessage}
            </div>
          ) : (
            groupedItems.map(([group, groupItems]) => (
              <div key={group} className="mb-2 last:mb-0">
                <div className="px-3 py-2 text-xs font-semibold text-muted-foreground uppercase tracking-wide">
                  {group}
                </div>
                <div className="space-y-1">
                  {groupItems.map((item) => {
                    // BUG-001 FIX: Use pre-calculated index from itemIndexMap
                    const itemIndex = itemIndexMap.get(item.id) ?? 0
                    const isSelected = itemIndex === selectedIndex

                    return (
                      <button
                        key={item.id}
                        type="button"
                        role="option"
                        aria-selected={isSelected}
                        data-index={itemIndex}
                        onClick={() => handleItemClick(item)}
                        onMouseEnter={() => setSelectedIndex(itemIndex)}
                        className={`
                          w-full flex items-center space-x-3 px-3 py-2.5 rounded-lg
                          text-sm transition-colors outline-none
                          ${
                            isSelected
                              ? "bg-accent text-accent-foreground"
                              : "text-foreground hover:bg-accent hover:text-accent-foreground"
                          }
                        `}
                      >
                        {item.icon && (
                          <div className="flex-shrink-0 text-muted-foreground">
                            {item.icon}
                          </div>
                        )}
                        <div className="flex-1 text-left space-y-0.5 min-w-0">
                          <div className="font-medium truncate">{item.label}</div>
                          {item.description && (
                            <div className="text-xs text-muted-foreground truncate">
                              {item.description}
                            </div>
                          )}
                        </div>
                        {item.shortcut && (
                          <kbd className="hidden sm:inline-flex items-center gap-1 rounded border border-border bg-muted px-2 py-1 text-xs text-muted-foreground">
                            {item.shortcut}
                          </kbd>
                        )}
                      </button>
                    )
                  })}
                </div>
              </div>
            ))
          )}
        </div>

        {/* Footer hint */}
        <div className="border-t border-border px-4 py-3 text-xs text-muted-foreground flex items-center justify-between">
          <div className="flex items-center space-x-4">
            <span className="flex items-center space-x-1">
              <kbd className="inline-flex items-center rounded border border-border bg-muted px-1.5 py-0.5">
                ↑↓
              </kbd>
              <span>Navigate</span>
            </span>
            <span className="flex items-center space-x-1">
              <kbd className="inline-flex items-center rounded border border-border bg-muted px-1.5 py-0.5">
                ↵
              </kbd>
              <span>Select</span>
            </span>
          </div>
          <span className="flex items-center space-x-1">
            <kbd className="inline-flex items-center rounded border border-border bg-muted px-1.5 py-0.5">
              ESC
            </kbd>
            <span>Close</span>
          </span>
        </div>
      </div>
    </div>
  )
}

CommandPalette.displayName = "CommandPalette"

// Hook for easier command palette management with keyboard shortcut
export const useCommandPalette = (shortcut: string = "k") => {
  const [isOpen, setIsOpen] = React.useState(false)

  React.useEffect(() => {
    const handleKeyDown = (e: KeyboardEvent) => {
      if ((e.metaKey || e.ctrlKey) && e.key === shortcut) {
        e.preventDefault()
        setIsOpen((prev) => !prev)
      }
    }

    document.addEventListener("keydown", handleKeyDown)
    return () => document.removeEventListener("keydown", handleKeyDown)
  }, [shortcut])

  return {
    isOpen,
    open: () => setIsOpen(true),
    close: () => setIsOpen(false),
    toggle: () => setIsOpen((prev) => !prev),
    setIsOpen,
  }
}
