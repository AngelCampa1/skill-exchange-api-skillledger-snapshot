import * as React from "react"
import { X, Info, CheckCircle, AlertTriangle, AlertCircle } from "lucide-react"

export interface NotificationBannerProps {
  message: string
  variant?: "info" | "success" | "warning" | "error"
  position?: "top" | "bottom"
  dismissible?: boolean
  onDismiss?: () => void
  action?: {
    label: string
    onClick: () => void
  }
  icon?: React.ReactNode
  className?: string
}

export const NotificationBanner: React.FC<NotificationBannerProps> = ({
  message,
  variant = "info",
  position = "top",
  dismissible = true,
  onDismiss,
  action,
  icon,
  className,
}) => {
  const [isVisible, setIsVisible] = React.useState(true)

  const handleDismiss = () => {
    setIsVisible(false)
    onDismiss?.()
  }

  if (!isVisible) return null

  const variantConfig = {
    info: {
      bg: "bg-info",
      text: "text-info-foreground",
      icon: <Info className="h-5 w-5" />,
    },
    success: {
      bg: "bg-success",
      text: "text-success-foreground",
      icon: <CheckCircle className="h-5 w-5" />,
    },
    warning: {
      bg: "bg-warning",
      text: "text-warning-foreground",
      icon: <AlertTriangle className="h-5 w-5" />,
    },
    error: {
      bg: "bg-destructive",
      text: "text-destructive-foreground",
      icon: <AlertCircle className="h-5 w-5" />,
    },
  }

  const config = variantConfig[variant]
  const positionClasses = position === "top" ? "top-0" : "bottom-0"

  return (
    <div
      role="alert"
      className={`
        fixed left-0 right-0 z-50 ${positionClasses}
        ${config.bg} ${config.text}
        animate-in slide-in-from-top-full
        ${className || ""}
      `}
    >
      <div className="container mx-auto px-4 py-3">
        <div className="flex items-center justify-between gap-4">
          <div className="flex items-center gap-3 flex-1 min-w-0">
            {icon || config.icon}
            <p className="text-sm font-medium truncate">{message}</p>
          </div>

          <div className="flex items-center gap-2 flex-shrink-0">
            {action && (
              <button
                type="button"
                onClick={action.onClick}
                className="px-3 py-1.5 text-sm font-medium bg-white/20 hover:bg-white/30 rounded-full transition-colors"
              >
                {action.label}
              </button>
            )}
            {dismissible && (
              <button
                type="button"
                onClick={handleDismiss}
                className="p-1 hover:bg-white/20 rounded-full transition-colors"
                aria-label="Dismiss notification"
              >
                <X className="h-5 w-5" />
              </button>
            )}
          </div>
        </div>
      </div>
    </div>
  )
}

NotificationBanner.displayName = "NotificationBanner"

// Inline banner variant for use within page content
export interface InlineBannerProps {
  message: string
  variant?: "info" | "success" | "warning" | "error"
  dismissible?: boolean
  onDismiss?: () => void
  action?: {
    label: string
    onClick: () => void
  }
  icon?: React.ReactNode
  className?: string
}

export const InlineBanner: React.FC<InlineBannerProps> = ({
  message,
  variant = "info",
  dismissible = true,
  onDismiss,
  action,
  icon,
  className,
}) => {
  const [isVisible, setIsVisible] = React.useState(true)

  const handleDismiss = () => {
    setIsVisible(false)
    onDismiss?.()
  }

  if (!isVisible) return null

  const variantConfig = {
    info: {
      bg: "bg-info/10",
      border: "border-info/20",
      text: "text-info",
      icon: <Info className="h-5 w-5" />,
      button: "text-info hover:bg-info/20",
    },
    success: {
      bg: "bg-success/10",
      border: "border-success/20",
      text: "text-success",
      icon: <CheckCircle className="h-5 w-5" />,
      button: "text-success hover:bg-success/20",
    },
    warning: {
      bg: "bg-warning/10",
      border: "border-warning/20",
      text: "text-warning",
      icon: <AlertTriangle className="h-5 w-5" />,
      button: "text-warning hover:bg-warning/20",
    },
    error: {
      bg: "bg-destructive/10",
      border: "border-destructive/20",
      text: "text-destructive",
      icon: <AlertCircle className="h-5 w-5" />,
      button: "text-destructive hover:bg-destructive/20",
    },
  }

  const config = variantConfig[variant]

  return (
    <div
      role="alert"
      className={`
        border rounded-xl p-4
        ${config.bg} ${config.border} ${config.text}
        animate-in fade-in-0 slide-in-from-top-1
        ${className || ""}
      `}
    >
      <div className="flex items-start gap-3">
        <div className="flex-shrink-0 mt-0.5">{icon || config.icon}</div>
        <div className="flex-1 min-w-0">
          <p className="text-sm font-medium">{message}</p>
        </div>
        <div className="flex items-center gap-2 flex-shrink-0">
          {action && (
            <button
              type="button"
              onClick={action.onClick}
              className={`px-3 py-1.5 text-sm font-medium rounded-full transition-colors ${config.button}`}
            >
              {action.label}
            </button>
          )}
          {dismissible && (
            <button
              type="button"
              onClick={handleDismiss}
              className={`p-1 rounded-full transition-colors ${config.button}`}
              aria-label="Dismiss notification"
            >
              <X className="h-4 w-4" />
            </button>
          )}
        </div>
      </div>
    </div>
  )
}

InlineBanner.displayName = "InlineBanner"

// Hook for managing notification banners
export interface NotificationConfig {
  id: string
  message: string
  variant?: "info" | "success" | "warning" | "error"
  duration?: number
  action?: {
    label: string
    onClick: () => void
  }
}

export const useNotificationBanner = () => {
  const [notifications, setNotifications] = React.useState<NotificationConfig[]>([])

  const show = React.useCallback((config: Omit<NotificationConfig, "id">) => {
    const id = Math.random().toString(36).substring(2, 9)
    const notification: NotificationConfig = { id, ...config }

    setNotifications((prev) => [...prev, notification])

    if (config.duration !== 0) {
      setTimeout(() => {
        setNotifications((prev) => prev.filter((n) => n.id !== id))
      }, config.duration || 5000)
    }

    return id
  }, [])

  const dismiss = React.useCallback((id: string) => {
    setNotifications((prev) => prev.filter((n) => n.id !== id))
  }, [])

  const dismissAll = React.useCallback(() => {
    setNotifications([])
  }, [])

  const success = React.useCallback(
    (message: string, options?: Omit<NotificationConfig, "id" | "message" | "variant">) => {
      return show({ message, variant: "success", ...options })
    },
    [show]
  )

  const error = React.useCallback(
    (message: string, options?: Omit<NotificationConfig, "id" | "message" | "variant">) => {
      return show({ message, variant: "error", ...options })
    },
    [show]
  )

  const warning = React.useCallback(
    (message: string, options?: Omit<NotificationConfig, "id" | "message" | "variant">) => {
      return show({ message, variant: "warning", ...options })
    },
    [show]
  )

  const info = React.useCallback(
    (message: string, options?: Omit<NotificationConfig, "id" | "message" | "variant">) => {
      return show({ message, variant: "info", ...options })
    },
    [show]
  )

  return {
    notifications,
    show,
    dismiss,
    dismissAll,
    success,
    error,
    warning,
    info,
  }
}
