import * as React from "react"
import { TrendingUp, TrendingDown, Minus, ArrowUpRight, ArrowDownRight } from "lucide-react"

export interface StatCardProps {
  label: string
  value: string | number
  icon?: React.ReactNode
  trend?: {
    value: number
    label?: string
    isPositive?: boolean
  }
  subtitle?: string
  variant?: "default" | "primary" | "success" | "warning" | "error"
  loading?: boolean
  className?: string
}

export const StatCard: React.FC<StatCardProps> = ({
  label,
  value,
  icon,
  trend,
  subtitle,
  variant = "default",
  loading = false,
  className,
}) => {
  const variantColors = {
    default: "border-border bg-background",
    primary: "border-primary/20 bg-primary/5",
    success: "border-success/20 bg-success/5",
    warning: "border-warning/20 bg-warning/5",
    error: "border-destructive/20 bg-destructive/5",
  }

  const iconColors = {
    default: "text-muted-foreground",
    primary: "text-primary",
    success: "text-success",
    warning: "text-warning",
    error: "text-destructive",
  }

  const getTrendIcon = () => {
    if (!trend) return null
    if (trend.value === 0) return <Minus className="h-4 w-4" />
    if (trend.isPositive ?? trend.value > 0) {
      return <TrendingUp className="h-4 w-4" />
    }
    return <TrendingDown className="h-4 w-4" />
  }

  const getTrendColor = () => {
    if (!trend) return ""
    if (trend.value === 0) return "text-muted-foreground"
    if (trend.isPositive ?? trend.value > 0) {
      return "text-success"
    }
    return "text-destructive"
  }

  return (
    <div
      className={`
        p-6 rounded-xl border transition-all duration-200 hover:shadow-md
        ${variantColors[variant]}
        ${className || ""}
      `}
    >
      <div className="flex items-start justify-between">
        <div className="flex-1">
          <p className="text-sm font-medium text-muted-foreground">{label}</p>
          {loading ? (
            <div className="mt-2 h-8 w-24 bg-muted animate-pulse rounded" />
          ) : (
            <p className="mt-2 text-3xl font-bold text-foreground">{value}</p>
          )}
          {subtitle && (
            <p className="mt-1 text-xs text-muted-foreground">{subtitle}</p>
          )}
          {trend && !loading && (
            <div className={`mt-2 flex items-center space-x-1 text-sm font-medium ${getTrendColor()}`}>
              {getTrendIcon()}
              <span>{Math.abs(trend.value)}%</span>
              {trend.label && (
                <span className="text-muted-foreground font-normal">
                  {trend.label}
                </span>
              )}
            </div>
          )}
        </div>
        {icon && (
          <div className={`p-3 rounded-lg bg-muted/50 ${iconColors[variant]}`}>
            {icon}
          </div>
        )}
      </div>
    </div>
  )
}

StatCard.displayName = "StatCard"

// Stat Group for displaying multiple stats in a grid
export interface StatGroupProps {
  stats: StatCardProps[]
  columns?: 1 | 2 | 3 | 4
  className?: string
}

export const StatGroup: React.FC<StatGroupProps> = ({
  stats,
  columns = 3,
  className,
}) => {
  const gridCols = {
    1: "grid-cols-1",
    2: "grid-cols-1 md:grid-cols-2",
    3: "grid-cols-1 md:grid-cols-2 lg:grid-cols-3",
    4: "grid-cols-1 md:grid-cols-2 lg:grid-cols-4",
  }

  return (
    <div className={`grid gap-4 ${gridCols[columns]} ${className || ""}`}>
      {stats.map((stat, index) => (
        <StatCard key={index} {...stat} />
      ))}
    </div>
  )
}

StatGroup.displayName = "StatGroup"

// Compact stat for inline display
export interface CompactStatProps {
  label: string
  value: string | number
  icon?: React.ReactNode
  trend?: number
  size?: "sm" | "md" | "lg"
  className?: string
}

export const CompactStat: React.FC<CompactStatProps> = ({
  label,
  value,
  icon,
  trend,
  size = "md",
  className,
}) => {
  const sizeClasses = {
    sm: {
      container: "space-y-1",
      label: "text-xs",
      value: "text-lg",
      icon: "h-4 w-4",
    },
    md: {
      container: "space-y-1",
      label: "text-sm",
      value: "text-2xl",
      icon: "h-5 w-5",
    },
    lg: {
      container: "space-y-2",
      label: "text-base",
      value: "text-3xl",
      icon: "h-6 w-6",
    },
  }

  const classes = sizeClasses[size]

  return (
    <div className={`${classes.container} ${className || ""}`}>
      <div className="flex items-center space-x-2">
        {icon && (
          <div className="text-muted-foreground">{icon}</div>
        )}
        <span className={`${classes.label} font-medium text-muted-foreground`}>
          {label}
        </span>
      </div>
      <div className="flex items-baseline space-x-2">
        <span className={`${classes.value} font-bold text-foreground`}>{value}</span>
        {trend !== undefined && trend !== 0 && (
          <span
            className={`text-sm font-medium ${
              trend > 0
                ? "text-success"
                : "text-destructive"
            }`}
          >
            {trend > 0 ? "↑" : "↓"} {Math.abs(trend)}%
          </span>
        )}
      </div>
    </div>
  )
}

CompactStat.displayName = "CompactStat"

// Metric comparison component
export interface MetricComparisonProps {
  current: {
    label: string
    value: number
    period: string
  }
  previous: {
    label: string
    value: number
    period: string
  }
  format?: (value: number) => string
  className?: string
}

export const MetricComparison: React.FC<MetricComparisonProps> = ({
  current,
  previous,
  format = (v) => v.toLocaleString(),
  className,
}) => {
  const change = current.value - previous.value
  const percentageChange = previous.value !== 0
    ? ((change / previous.value) * 100).toFixed(1)
    : "0"
  const isPositive = change >= 0

  return (
    <div className={`p-6 rounded-xl border border-border bg-background ${className || ""}`}>
      {/* Current Period */}
      <div className="mb-4">
        <p className="text-sm font-medium text-muted-foreground">{current.label}</p>
        <p className="mt-1 text-3xl font-bold text-foreground">{format(current.value)}</p>
        <p className="text-xs text-muted-foreground">{current.period}</p>
      </div>

      {/* Comparison */}
      <div className="pt-4 border-t border-border">
        <div className="flex items-center justify-between">
          <div>
            <p className="text-sm font-medium text-muted-foreground">{previous.label}</p>
            <p className="text-lg font-semibold text-foreground">{format(previous.value)}</p>
            <p className="text-xs text-muted-foreground">{previous.period}</p>
          </div>
          <div className="text-right">
            <div
              className={`inline-flex items-center space-x-1 px-2 py-1 rounded-full text-sm font-medium ${
                isPositive
                  ? "bg-success/10 text-success"
                  : "bg-destructive/10 text-destructive"
              }`}
            >
              {isPositive ? (
                <ArrowUpRight className="h-4 w-4" />
              ) : (
                <ArrowDownRight className="h-4 w-4" />
              )}
              <span>{percentageChange}%</span>
            </div>
            <p className="mt-1 text-xs text-muted-foreground">
              {isPositive ? "+" : ""}{format(change)} change
            </p>
          </div>
        </div>
      </div>
    </div>
  )
}

MetricComparison.displayName = "MetricComparison"

// Key Performance Indicator (KPI) card
export interface KPICardProps {
  title: string
  value: number
  target: number
  unit?: string
  format?: (value: number) => string
  icon?: React.ReactNode
  showProgress?: boolean
  className?: string
}

export const KPICard: React.FC<KPICardProps> = ({
  title,
  value,
  target,
  unit,
  format = (v) => v.toLocaleString(),
  icon,
  showProgress = true,
  className,
}) => {
  const percentage = (value / target) * 100
  const isOnTrack = percentage >= 100
  const isWarning = percentage >= 70 && percentage < 100
  const isDanger = percentage < 70

  return (
    <div
      className={`p-6 rounded-xl border border-border bg-background hover:shadow-md transition-all ${className || ""}`}
    >
      <div className="flex items-start justify-between mb-4">
        <div>
          <p className="text-sm font-medium text-muted-foreground">{title}</p>
          <div className="mt-2 flex items-baseline space-x-2">
            <span className="text-3xl font-bold text-foreground">{format(value)}</span>
            {unit && <span className="text-sm text-muted-foreground">{unit}</span>}
          </div>
        </div>
        {icon && (
          <div className="p-3 rounded-lg bg-muted/50 text-muted-foreground">
            {icon}
          </div>
        )}
      </div>

      {showProgress && (
        <div className="space-y-2">
          <div className="flex items-center justify-between text-sm">
            <span className="text-muted-foreground">Progress to target</span>
            <span
              className={`font-medium ${
                isOnTrack
                  ? "text-success"
                  : isWarning
                  ? "text-warning"
                  : "text-destructive"
              }`}
            >
              {percentage.toFixed(0)}%
            </span>
          </div>
          <div className="h-2 bg-muted rounded-full overflow-hidden">
            <div
              className={`h-full transition-all duration-500 ${
                isOnTrack
                  ? "bg-success"
                  : isWarning
                  ? "bg-warning"
                  : "bg-destructive"
              }`}
              style={{ width: `${Math.min(percentage, 100)}%` }}
            />
          </div>
          <p className="text-xs text-muted-foreground">
            Target: {format(target)} {unit}
          </p>
        </div>
      )}
    </div>
  )
}

KPICard.displayName = "KPICard"
