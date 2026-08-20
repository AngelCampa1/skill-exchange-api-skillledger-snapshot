import * as React from "react"
import { X } from "lucide-react"
import { Button } from "./button"

export interface DrawerProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  side?: "left" | "right" | "top" | "bottom"
  size?: "sm" | "md" | "lg" | "xl" | "full"
  title?: string
  description?: string
  children: React.ReactNode
  footer?: React.ReactNode
  closeButton?: boolean
  closeOnClickOutside?: boolean
  closeOnEscape?: boolean
  className?: string
}

export const Drawer: React.FC<DrawerProps> = ({
  open,
  onOpenChange,
  side = "right",
  size = "md",
  title,
  description,
  children,
  footer,
  closeButton = true,
  closeOnClickOutside = true,
  closeOnEscape = true,
  className,
}) => {
  const drawerRef = React.useRef<HTMLDivElement>(null)
  const closeButtonRef = React.useRef<HTMLButtonElement>(null)
  // BUG-008 FIX: Track if overflow was modified to safely reset
  const wasOverflowModified = React.useRef(false)

  React.useEffect(() => {
    const handleClickOutside = (event: MouseEvent) => {
      if (
        closeOnClickOutside &&
        open &&
        drawerRef.current &&
        !drawerRef.current.contains(event.target as Node)
      ) {
        onOpenChange(false)
      }
    }

    const handleEscape = (event: KeyboardEvent) => {
      if (closeOnEscape && event.key === "Escape" && open) {
        onOpenChange(false)
      }
    }

    if (open) {
      document.addEventListener("mousedown", handleClickOutside)
      document.addEventListener("keydown", handleEscape)
      document.body.style.overflow = "hidden"
      wasOverflowModified.current = true
    }

    return () => {
      document.removeEventListener("mousedown", handleClickOutside)
      document.removeEventListener("keydown", handleEscape)
      // BUG-008 FIX: Only reset overflow if we modified it
      if (wasOverflowModified.current) {
        document.body.style.overflow = "unset"
        wasOverflowModified.current = false
      }
    }
  }, [open, closeOnClickOutside, closeOnEscape, onOpenChange])

  // BUG-042 FIX: Focus close button when drawer opens
  React.useEffect(() => {
    if (open && closeButton && closeButtonRef.current) {
      // Small delay to ensure drawer animation starts
      const timer = setTimeout(() => {
        closeButtonRef.current?.focus()
      }, 50)
      return () => clearTimeout(timer)
    }
  }, [open, closeButton])

  if (!open) return null

  const sizeClasses = {
    left: {
      sm: "w-80",
      md: "w-96",
      lg: "w-[32rem]",
      xl: "w-[48rem]",
      full: "w-full",
    },
    right: {
      sm: "w-80",
      md: "w-96",
      lg: "w-[32rem]",
      xl: "w-[48rem]",
      full: "w-full",
    },
    top: {
      sm: "h-80",
      md: "h-96",
      lg: "h-[32rem]",
      xl: "h-[48rem]",
      full: "h-full",
    },
    bottom: {
      sm: "h-80",
      md: "h-96",
      lg: "h-[32rem]",
      xl: "h-[48rem]",
      full: "h-full",
    },
  }

  const positionClasses = {
    left: "left-0 top-0 h-full",
    right: "right-0 top-0 h-full",
    top: "top-0 left-0 w-full",
    bottom: "bottom-0 left-0 w-full",
  }

  const animationClasses = {
    left: "animate-in slide-in-from-left-full",
    right: "animate-in slide-in-from-right-full",
    top: "animate-in slide-in-from-top-full",
    bottom: "animate-in slide-in-from-bottom-full",
  }

  return (
    <div
      className="fixed inset-0 z-50 flex"
      role="dialog"
      aria-modal="true"
      aria-labelledby={title ? "drawer-title" : undefined}
      aria-describedby={description ? "drawer-description" : undefined}
    >
      {/* Backdrop */}
      <div
        className="fixed inset-0 bg-overlay/80 backdrop-blur-sm animate-in fade-in-0"
        aria-hidden="true"
      />

      {/* Drawer Panel */}
      <div
        ref={drawerRef}
        className={`
          fixed z-50 bg-background shadow-xl
          ${positionClasses[side]}
          ${sizeClasses[side][size]}
          ${animationClasses[side]}
          ${className || ""}
        `}
      >
        <div className="flex h-full flex-col">
          {/* Header */}
          {(title || description || closeButton) && (
            <div className="flex items-start justify-between border-b border-border p-6">
              <div className="flex-1 space-y-1">
                {title && (
                  <h2
                    id="drawer-title"
                    className="text-lg font-semibold text-foreground"
                  >
                    {title}
                  </h2>
                )}
                {description && (
                  <p
                    id="drawer-description"
                    className="text-sm text-muted-foreground"
                  >
                    {description}
                  </p>
                )}
              </div>
              {/* BUG-050 FIX: Always render close button container for consistent layout */}
              {closeButton && (
                <button
                  ref={closeButtonRef}
                  type="button"
                  onClick={() => onOpenChange(false)}
                  className="ml-4 rounded-lg p-2 text-muted-foreground hover:bg-muted hover:text-foreground transition-colors focus:outline-none focus:ring-2 focus:ring-ring"
                  aria-label="Close drawer"
                >
                  <X className="h-5 w-5" aria-hidden="true" />
                </button>
              )}
            </div>
          )}

          {/* Content */}
          <div className="flex-1 overflow-y-auto p-6">{children}</div>

          {/* Footer */}
          {footer && (
            <div className="border-t border-border p-6">{footer}</div>
          )}
        </div>
      </div>
    </div>
  )
}

Drawer.displayName = "Drawer"

// Sheet variant (alias for Drawer)
export const Sheet = Drawer
Sheet.displayName = "Sheet"

// Hook for easier drawer management
export const useDrawer = () => {
  const [isOpen, setIsOpen] = React.useState(false)

  const open = React.useCallback(() => setIsOpen(true), [])
  const close = React.useCallback(() => setIsOpen(false), [])
  const toggle = React.useCallback(() => setIsOpen((prev) => !prev), [])

  return {
    isOpen,
    open,
    close,
    toggle,
    setIsOpen,
  }
}
