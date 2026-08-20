import * as React from "react"
import Image from "next/image"
import { Circle, CheckCircle, AlertCircle, Clock } from "lucide-react"

export interface TimelineItem {
  id: string
  title: string
  description?: string
  timestamp?: string | Date
  icon?: React.ReactNode
  status?: "completed" | "current" | "upcoming" | "error"
  metadata?: React.ReactNode
}

export interface TimelineProps {
  items: TimelineItem[]
  variant?: "default" | "compact"
  orientation?: "vertical" | "horizontal"
  className?: string
}

export const Timeline: React.FC<TimelineProps> = ({
  items,
  variant = "default",
  orientation = "vertical",
  className,
}) => {
  const getStatusIcon = (item: TimelineItem) => {
    if (item.icon) return item.icon

    switch (item.status) {
      case "completed":
        return <CheckCircle className="h-5 w-5" />
      case "error":
        return <AlertCircle className="h-5 w-5" />
      case "current":
        return <Clock className="h-5 w-5" />
      default:
        return <Circle className="h-5 w-5" />
    }
  }

  const getStatusColors = (status?: string) => {
    switch (status) {
      case "completed":
        return "bg-success text-success-foreground border-success"
      case "error":
        return "bg-destructive text-destructive-foreground border-destructive"
      case "current":
        return "bg-primary text-primary-foreground border-primary"
      default:
        return "bg-muted text-muted-foreground border-border"
    }
  }

  const getLineColors = (status?: string) => {
    switch (status) {
      case "completed":
        return "bg-success"
      case "error":
        return "bg-destructive"
      case "current":
        return "bg-primary"
      default:
        return "bg-border"
    }
  }

  const formatTimestamp = (timestamp?: string | Date) => {
    if (!timestamp) return null
    const date = timestamp instanceof Date ? timestamp : new Date(timestamp)
    return date.toLocaleString(undefined, {
      month: "short",
      day: "numeric",
      year: "numeric",
      hour: "2-digit",
      minute: "2-digit",
    })
  }

  if (orientation === "horizontal") {
    return (
      <div className={`w-full overflow-x-auto ${className || ""}`}>
        <div className="flex items-start min-w-max px-4">
          {items.map((item, index) => (
            <div key={item.id} className="flex items-start">
              <div className="flex flex-col items-center">
                {/* Icon */}
                <div
                  className={`
                    flex items-center justify-center w-10 h-10 rounded-full border-2
                    ${getStatusColors(item.status)}
                  `}
                >
                  {getStatusIcon(item)}
                </div>

                {/* Content */}
                <div className="mt-4 text-center max-w-[200px]">
                  <div className="text-sm font-semibold text-foreground">{item.title}</div>
                  {item.description && (
                    <p className="text-xs text-muted-foreground mt-1">{item.description}</p>
                  )}
                  {item.timestamp && (
                    <time className="text-xs text-muted-foreground mt-1 block">
                      {formatTimestamp(item.timestamp)}
                    </time>
                  )}
                  {item.metadata && <div className="mt-2">{item.metadata}</div>}
                </div>
              </div>

              {/* Connector Line */}
              {index < items.length - 1 && (
                <div
                  className={`h-0.5 w-24 mt-5 mx-2 ${getLineColors(item.status)}`}
                  aria-hidden="true"
                />
              )}
            </div>
          ))}
        </div>
      </div>
    )
  }

  return (
    <div className={`space-y-0 ${className || ""}`}>
      {items.map((item, index) => (
        <div key={item.id} className="flex items-start">
          {/* Icon Column */}
          <div className="flex flex-col items-center mr-4">
            <div
              className={`
                flex items-center justify-center w-10 h-10 rounded-full border-2 flex-shrink-0
                ${getStatusColors(item.status)}
              `}
            >
              {getStatusIcon(item)}
            </div>
            {index < items.length - 1 && (
              <div
                className={`w-0.5 flex-1 min-h-[40px] ${getLineColors(item.status)}`}
                aria-hidden="true"
              />
            )}
          </div>

          {/* Content Column */}
          <div className={`flex-1 ${index < items.length - 1 ? "pb-8" : ""}`}>
            <div className="flex items-start justify-between">
              <div className="flex-1">
                <h3 className="text-sm font-semibold text-foreground">{item.title}</h3>
                {item.description && (
                  <p
                    className={`text-sm text-muted-foreground ${
                      variant === "compact" ? "mt-0.5" : "mt-1"
                    }`}
                  >
                    {item.description}
                  </p>
                )}
                {item.metadata && (
                  <div className={variant === "compact" ? "mt-1" : "mt-2"}>{item.metadata}</div>
                )}
              </div>
              {item.timestamp && (
                <time className="text-xs text-muted-foreground ml-4 flex-shrink-0">
                  {formatTimestamp(item.timestamp)}
                </time>
              )}
            </div>
          </div>
        </div>
      ))}
    </div>
  )
}

Timeline.displayName = "Timeline"

// Activity Timeline - specialized for activity feeds
export interface ActivityTimelineItem {
  id: string
  user: {
    name: string
    avatar?: string
  }
  action: string
  target?: string
  timestamp: string | Date
  icon?: React.ReactNode
}

export interface ActivityTimelineProps {
  items: ActivityTimelineItem[]
  className?: string
}

export const ActivityTimeline: React.FC<ActivityTimelineProps> = ({ items, className }) => {
  const formatRelativeTime = (timestamp: string | Date) => {
    const date = timestamp instanceof Date ? timestamp : new Date(timestamp)
    const now = new Date()
    const diffInSeconds = Math.floor((now.getTime() - date.getTime()) / 1000)

    if (diffInSeconds < 60) return "just now"
    if (diffInSeconds < 3600) return `${Math.floor(diffInSeconds / 60)}m ago`
    if (diffInSeconds < 86400) return `${Math.floor(diffInSeconds / 3600)}h ago`
    if (diffInSeconds < 604800) return `${Math.floor(diffInSeconds / 86400)}d ago`

    return date.toLocaleDateString(undefined, { month: "short", day: "numeric" })
  }

  return (
    <div className={`space-y-4 ${className || ""}`}>
      {items.map((item, index) => (
        <div key={item.id} className="flex items-start space-x-3">
          {/* Avatar */}
          <div className="flex-shrink-0">
            {item.user.avatar ? (
              <div className="w-8 h-8 rounded-full bg-muted overflow-hidden relative">
                <Image
                  src={item.user.avatar}
                  alt={item.user.name}
                  fill
                  className="object-cover"
                  unoptimized
                />
              </div>
            ) : (
              <div className="w-8 h-8 rounded-full bg-primary text-primary-foreground flex items-center justify-center text-xs font-semibold">
                {item.user.name
                  .split(" ")
                  .map((n) => n[0])
                  .join("")
                  .toUpperCase()
                  .slice(0, 2)}
              </div>
            )}
          </div>

          {/* Content */}
          <div className="flex-1 min-w-0">
            <p className="text-sm text-foreground">
              <span className="font-semibold">{item.user.name}</span>{" "}
              <span className="text-muted-foreground">{item.action}</span>
              {item.target && <span className="font-medium"> {item.target}</span>}
            </p>
            <time className="text-xs text-muted-foreground">
              {formatRelativeTime(item.timestamp)}
            </time>
          </div>

          {/* Icon */}
          {item.icon && (
            <div className="flex-shrink-0 text-muted-foreground">{item.icon}</div>
          )}
        </div>
      ))}
    </div>
  )
}

ActivityTimeline.displayName = "ActivityTimeline"

// Simple event timeline
export interface EventTimelineItem {
  id: string
  date: string | Date
  events: {
    id: string
    time: string
    title: string
    description?: string
  }[]
}

export interface EventTimelineProps {
  items: EventTimelineItem[]
  className?: string
}

export const EventTimeline: React.FC<EventTimelineProps> = ({ items, className }) => {
  const formatDate = (date: string | Date) => {
    const d = date instanceof Date ? date : new Date(date)
    return d.toLocaleDateString(undefined, {
      weekday: "long",
      month: "long",
      day: "numeric",
      year: "numeric",
    })
  }

  return (
    <div className={`space-y-6 ${className || ""}`}>
      {items.map((item) => (
        <div key={item.id}>
          {/* Date Header */}
          <div className="sticky top-0 bg-background/95 backdrop-blur-sm py-2 border-b border-border mb-4">
            <h3 className="text-sm font-semibold text-foreground">{formatDate(item.date)}</h3>
          </div>

          {/* Events */}
          <div className="space-y-3 pl-4 border-l-2 border-border">
            {item.events.map((event) => (
              <div key={event.id} className="relative pl-6 pb-3">
                {/* Time Marker */}
                <div className="absolute left-[-9px] top-1 w-4 h-4 rounded-full bg-primary border-2 border-background" />

                <div className="flex items-baseline space-x-2">
                  <time className="text-xs font-medium text-muted-foreground min-w-[60px]">
                    {event.time}
                  </time>
                  <div className="flex-1">
                    <h4 className="text-sm font-medium text-foreground">{event.title}</h4>
                    {event.description && (
                      <p className="text-xs text-muted-foreground mt-0.5">{event.description}</p>
                    )}
                  </div>
                </div>
              </div>
            ))}
          </div>
        </div>
      ))}
    </div>
  )
}

EventTimeline.displayName = "EventTimeline"
