import * as React from "react"
import { Check, ChevronRight } from "lucide-react"

export interface DropdownMenuItem {
  label: string
  onClick?: () => void
  icon?: React.ReactNode
  shortcut?: string
  disabled?: boolean
  destructive?: boolean
  divider?: boolean
}

export interface DropdownMenuProps {
  trigger: React.ReactElement<{ onClick?: React.MouseEventHandler; [key: string]: unknown }>
  items: DropdownMenuItem[]
  align?: "start" | "end"
  className?: string
}

export const DropdownMenu: React.FC<DropdownMenuProps> = ({
  trigger,
  items,
  align = "start",
  className,
}) => {
  const [isOpen, setIsOpen] = React.useState(false)
  const menuRef = React.useRef<HTMLDivElement>(null)
  const triggerRef = React.useRef<HTMLDivElement>(null)

  React.useEffect(() => {
    const handleClickOutside = (event: MouseEvent) => {
      if (
        menuRef.current &&
        triggerRef.current &&
        !menuRef.current.contains(event.target as Node) &&
        !triggerRef.current.contains(event.target as Node)
      ) {
        setIsOpen(false)
      }
    }

    const handleEscape = (event: KeyboardEvent) => {
      if (event.key === "Escape") {
        setIsOpen(false)
      }
    }

    if (isOpen) {
      document.addEventListener("mousedown", handleClickOutside)
      document.addEventListener("keydown", handleEscape)
    }

    return () => {
      document.removeEventListener("mousedown", handleClickOutside)
      document.removeEventListener("keydown", handleEscape)
    }
  }, [isOpen])

  const handleItemClick = (item: DropdownMenuItem) => {
    if (!item.disabled && item.onClick) {
      item.onClick()
      setIsOpen(false)
    }
  }

  const triggerElement = React.cloneElement(trigger, {
    onClick: (e: React.MouseEvent) => {
      trigger.props.onClick?.(e)
      setIsOpen(!isOpen)
    },
    ref: triggerRef,
    "aria-expanded": isOpen,
    "aria-haspopup": "true",
  })

  return (
    <div className="relative inline-block">
      {triggerElement}
      {isOpen && (
        <div
          ref={menuRef}
          role="menu"
          className={`absolute z-50 mt-2 min-w-[12rem] overflow-hidden rounded-xl border border-border bg-background p-1 shadow-lg animate-in fade-in-0 zoom-in-95 ${
            align === "end" ? "right-0" : "left-0"
          } ${className || ""}`}
        >
          {items.map((item, index) => {
            if (item.divider) {
              return <div key={`divider-${index}`} className="my-1 h-px bg-border" />
            }

            return (
              <button
                key={index}
                role="menuitem"
                onClick={() => handleItemClick(item)}
                disabled={item.disabled}
                className={`
                  w-full flex items-center justify-between px-3 py-2 text-sm rounded-lg
                  transition-colors outline-none
                  ${item.disabled
                    ? "opacity-50 cursor-not-allowed"
                    : item.destructive
                    ? "text-destructive hover:bg-destructive/10 focus:bg-destructive/10"
                    : "text-foreground hover:bg-accent focus:bg-accent"
                  }
                `}
              >
                <span className="flex items-center space-x-2">
                  {item.icon && <span className="flex-shrink-0">{item.icon}</span>}
                  <span>{item.label}</span>
                </span>
                {item.shortcut && (
                  <span className="text-xs text-muted-foreground ml-4">{item.shortcut}</span>
                )}
              </button>
            )
          })}
        </div>
      )}
    </div>
  )
}

DropdownMenu.displayName = "DropdownMenu"

// Context Menu variant (right-click)
export interface ContextMenuProps {
  children: React.ReactNode
  items: DropdownMenuItem[]
  className?: string
}

export const ContextMenu: React.FC<ContextMenuProps> = ({
  children,
  items,
  className,
}) => {
  const [isOpen, setIsOpen] = React.useState(false)
  const [position, setPosition] = React.useState({ x: 0, y: 0 })
  const menuRef = React.useRef<HTMLDivElement>(null)

  React.useEffect(() => {
    const handleClickOutside = () => {
      setIsOpen(false)
    }

    const handleEscape = (event: KeyboardEvent) => {
      if (event.key === "Escape") {
        setIsOpen(false)
      }
    }

    if (isOpen) {
      document.addEventListener("click", handleClickOutside)
      document.addEventListener("keydown", handleEscape)
    }

    return () => {
      document.removeEventListener("click", handleClickOutside)
      document.removeEventListener("keydown", handleEscape)
    }
  }, [isOpen])

  const handleContextMenu = (e: React.MouseEvent) => {
    e.preventDefault()
    setPosition({ x: e.clientX, y: e.clientY })
    setIsOpen(true)
  }

  const handleItemClick = (item: DropdownMenuItem) => {
    if (!item.disabled && item.onClick) {
      item.onClick()
      setIsOpen(false)
    }
  }

  return (
    <>
      <div onContextMenu={handleContextMenu}>{children}</div>
      {isOpen && (
        <div
          ref={menuRef}
          role="menu"
          className={`fixed z-50 min-w-[12rem] overflow-hidden rounded-xl border border-border bg-background p-1 shadow-lg animate-in fade-in-0 zoom-in-95 ${className || ""}`}
          style={{
            top: `${position.y}px`,
            left: `${position.x}px`,
          }}
        >
          {items.map((item, index) => {
            if (item.divider) {
              return <div key={`divider-${index}`} className="my-1 h-px bg-border" />
            }

            return (
              <button
                key={index}
                role="menuitem"
                onClick={() => handleItemClick(item)}
                disabled={item.disabled}
                className={`
                  w-full flex items-center justify-between px-3 py-2 text-sm rounded-lg
                  transition-colors outline-none
                  ${item.disabled
                    ? "opacity-50 cursor-not-allowed"
                    : item.destructive
                    ? "text-destructive hover:bg-destructive/10 focus:bg-destructive/10"
                    : "text-foreground hover:bg-accent focus:bg-accent"
                  }
                `}
              >
                <span className="flex items-center space-x-2">
                  {item.icon && <span className="flex-shrink-0">{item.icon}</span>}
                  <span>{item.label}</span>
                </span>
                {item.shortcut && (
                  <span className="text-xs text-muted-foreground ml-4">{item.shortcut}</span>
                )}
              </button>
            )
          })}
        </div>
      )}
    </>
  )
}

ContextMenu.displayName = "ContextMenu"
