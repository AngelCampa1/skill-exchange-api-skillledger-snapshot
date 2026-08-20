import * as React from "react"

export interface TooltipProps {
  content: string | React.ReactNode
  children: React.ReactElement
  side?: "top" | "right" | "bottom" | "left"
  delay?: number
  disabled?: boolean
}

export const Tooltip: React.FC<TooltipProps> = ({
  content,
  children,
  side = "top",
  delay = 200,
  disabled = false,
}) => {
  const [isVisible, setIsVisible] = React.useState(false)
  const [coords, setCoords] = React.useState({ x: 0, y: 0 })
  const triggerRef = React.useRef<HTMLDivElement>(null)
  const tooltipRef = React.useRef<HTMLDivElement>(null)
  const timeoutRef = React.useRef<NodeJS.Timeout | undefined>(undefined)

  const calculatePosition = React.useCallback(() => {
    if (!triggerRef.current || !tooltipRef.current) return

    const triggerRect = triggerRef.current.getBoundingClientRect()
    const tooltipRect = tooltipRef.current.getBoundingClientRect()
    const gap = 8

    let x = 0
    let y = 0

    switch (side) {
      case "top":
        x = triggerRect.left + triggerRect.width / 2 - tooltipRect.width / 2
        y = triggerRect.top - tooltipRect.height - gap
        break
      case "right":
        x = triggerRect.right + gap
        y = triggerRect.top + triggerRect.height / 2 - tooltipRect.height / 2
        break
      case "bottom":
        x = triggerRect.left + triggerRect.width / 2 - tooltipRect.width / 2
        y = triggerRect.bottom + gap
        break
      case "left":
        x = triggerRect.left - tooltipRect.width - gap
        y = triggerRect.top + triggerRect.height / 2 - tooltipRect.height / 2
        break
    }

    // Keep tooltip within viewport
    const padding = 8
    x = Math.max(padding, Math.min(x, window.innerWidth - tooltipRect.width - padding))
    y = Math.max(padding, Math.min(y, window.innerHeight - tooltipRect.height - padding))

    setCoords({ x, y })
  }, [side])

  const handleMouseEnter = () => {
    if (disabled) return
    timeoutRef.current = setTimeout(() => {
      setIsVisible(true)
    }, delay)
  }

  const handleMouseLeave = () => {
    if (timeoutRef.current) {
      clearTimeout(timeoutRef.current)
    }
    setIsVisible(false)
  }

  React.useEffect(() => {
    if (isVisible) {
      calculatePosition()
      window.addEventListener("scroll", calculatePosition)
      window.addEventListener("resize", calculatePosition)
    }

    return () => {
      window.removeEventListener("scroll", calculatePosition)
      window.removeEventListener("resize", calculatePosition)
    }
  }, [isVisible, calculatePosition])

  const arrowClasses = {
    top: "bottom-[-4px] left-1/2 -translate-x-1/2 border-l-transparent border-r-transparent border-b-transparent",
    right: "left-[-4px] top-1/2 -translate-y-1/2 border-t-transparent border-b-transparent border-l-transparent",
    bottom: "top-[-4px] left-1/2 -translate-x-1/2 border-l-transparent border-r-transparent border-t-transparent",
    left: "right-[-4px] top-1/2 -translate-y-1/2 border-t-transparent border-b-transparent border-r-transparent",
  }

  return (
    <>
      <div
        ref={triggerRef}
        className="inline-block"
        onMouseEnter={handleMouseEnter}
        onMouseLeave={handleMouseLeave}
        onFocus={handleMouseEnter}
        onBlur={handleMouseLeave}
      >
        {children}
      </div>

      {isVisible && !disabled && (
        <div
          ref={tooltipRef}
          role="tooltip"
          className="fixed z-[100] px-3 py-2 text-sm text-popover-foreground bg-popover rounded-lg shadow-lg border border-border animate-in fade-in-0 zoom-in-95 duration-200"
          style={{
            left: `${coords.x}px`,
            top: `${coords.y}px`,
          }}
        >
          {content}
          <div
            className={`absolute w-0 h-0 border-4 border-popover ${arrowClasses[side]}`}
            aria-hidden="true"
          />
        </div>
      )}
    </>
  )
}

// Simple tooltip for common use cases
export interface SimpleTooltipProps {
  text: string
  children: React.ReactElement
}

export const SimpleTooltip: React.FC<SimpleTooltipProps> = ({ text, children }) => {
  return (
    <Tooltip content={text} side="top" delay={300}>
      {children}
    </Tooltip>
  )
}
