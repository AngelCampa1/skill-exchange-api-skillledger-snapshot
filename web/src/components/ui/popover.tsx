import * as React from "react"

export interface PopoverProps {
  trigger: React.ReactElement<{ onClick?: React.MouseEventHandler; [key: string]: unknown }>
  content: React.ReactNode
  side?: "top" | "right" | "bottom" | "left"
  align?: "start" | "center" | "end"
  open?: boolean
  onOpenChange?: (open: boolean) => void
  closeOnClickOutside?: boolean
  className?: string
}

export const Popover: React.FC<PopoverProps> = ({
  trigger,
  content,
  side = "bottom",
  align = "center",
  open: controlledOpen,
  onOpenChange,
  closeOnClickOutside = true,
  className,
}) => {
  const [internalOpen, setInternalOpen] = React.useState(false)
  const popoverRef = React.useRef<HTMLDivElement>(null)
  const triggerRef = React.useRef<HTMLDivElement>(null)

  const isOpen = controlledOpen !== undefined ? controlledOpen : internalOpen

  const setIsOpen = React.useCallback((newOpen: boolean) => {
    if (controlledOpen === undefined) {
      setInternalOpen(newOpen)
    }
    onOpenChange?.(newOpen)
  }, [controlledOpen, onOpenChange])

  const toggleOpen = () => {
    setIsOpen(!isOpen)
  }

  React.useEffect(() => {
    const handleClickOutside = (event: MouseEvent) => {
      if (
        closeOnClickOutside &&
        isOpen &&
        popoverRef.current &&
        triggerRef.current &&
        !popoverRef.current.contains(event.target as Node) &&
        !triggerRef.current.contains(event.target as Node)
      ) {
        setIsOpen(false)
      }
    }

    const handleEscape = (event: KeyboardEvent) => {
      if (event.key === "Escape" && isOpen) {
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
  }, [isOpen, closeOnClickOutside, setIsOpen])

  const [position, setPosition] = React.useState({ top: 0, left: 0 })

  React.useEffect(() => {
    if (isOpen && triggerRef.current && popoverRef.current) {
      const triggerRect = triggerRef.current.getBoundingClientRect()
      const popoverRect = popoverRef.current.getBoundingClientRect()
      const gap = 8

      let top = 0
      let left = 0

      // Calculate position based on side
      switch (side) {
        case "top":
          top = triggerRect.top - popoverRect.height - gap
          break
        case "bottom":
          top = triggerRect.bottom + gap
          break
        case "left":
          left = triggerRect.left - popoverRect.width - gap
          break
        case "right":
          left = triggerRect.right + gap
          break
      }

      // Calculate alignment
      if (side === "top" || side === "bottom") {
        switch (align) {
          case "start":
            left = triggerRect.left
            break
          case "center":
            left = triggerRect.left + triggerRect.width / 2 - popoverRect.width / 2
            break
          case "end":
            left = triggerRect.right - popoverRect.width
            break
        }
      } else {
        switch (align) {
          case "start":
            top = triggerRect.top
            break
          case "center":
            top = triggerRect.top + triggerRect.height / 2 - popoverRect.height / 2
            break
          case "end":
            top = triggerRect.bottom - popoverRect.height
            break
        }
      }

      // Keep within viewport
      const padding = 8
      left = Math.max(padding, Math.min(left, window.innerWidth - popoverRect.width - padding))
      top = Math.max(padding, Math.min(top, window.innerHeight - popoverRect.height - padding))

      setPosition({ top, left })
    }
  }, [isOpen, side, align])

  const triggerElement = React.cloneElement(trigger, {
    onClick: (e: React.MouseEvent) => {
      trigger.props.onClick?.(e)
      toggleOpen()
    },
    ref: triggerRef,
  })

  return (
    <>
      {triggerElement}
      {isOpen && (
        <div
          ref={popoverRef}
          className={`fixed z-50 rounded-xl border border-border bg-background p-4 shadow-lg animate-in fade-in-0 zoom-in-95 ${className || ""}`}
          style={{
            top: `${position.top}px`,
            left: `${position.left}px`,
          }}
          role="dialog"
          aria-modal="false"
        >
          {content}
        </div>
      )}
    </>
  )
}

Popover.displayName = "Popover"
