import * as React from "react"

// Bar Chart
export interface BarChartProps {
  data: {
    label: string
    value: number
    color?: string
  }[]
  height?: number
  showValues?: boolean
  showGrid?: boolean
  className?: string
}

export const BarChart: React.FC<BarChartProps> = ({
  data,
  height = 300,
  showValues = false,
  showGrid = true,
  className,
}) => {
  const maxValue = Math.max(...data.map((d) => d.value))

  return (
    <div className={`w-full ${className || ""}`}>
      <div className="relative" style={{ height }}>
        {/* Grid lines */}
        {showGrid && (
          <div className="absolute inset-0 flex flex-col justify-between">
            {[...Array(5)].map((_, i) => (
              <div key={i} className="border-t border-border" />
            ))}
          </div>
        )}

        {/* Bars */}
        <div className="absolute inset-0 flex items-end justify-around gap-2 px-4">
          {data.map((item, index) => {
            const barHeight = (item.value / maxValue) * 100
            const barColor = item.color || "bg-primary"

            return (
              <div key={index} className="flex-1 flex flex-col items-center gap-2">
                <div className="w-full flex flex-col items-center">
                  {showValues && (
                    <span className="text-xs font-medium text-foreground mb-1">
                      {item.value}
                    </span>
                  )}
                  <div
                    className={`w-full ${barColor} rounded-t-lg transition-all duration-500 hover:opacity-80`}
                    style={{ height: `${barHeight}%` }}
                  />
                </div>
              </div>
            )
          })}
        </div>
      </div>

      {/* Labels */}
      <div className="flex justify-around gap-2 mt-2 px-4">
        {data.map((item, index) => (
          <div key={index} className="flex-1 text-center">
            <span className="text-xs text-muted-foreground truncate block">
              {item.label}
            </span>
          </div>
        ))}
      </div>
    </div>
  )
}

BarChart.displayName = "BarChart"

// Line Chart (simplified)
export interface LineChartProps {
  data: {
    label: string
    value: number
  }[]
  height?: number
  color?: string
  fill?: boolean
  showPoints?: boolean
  className?: string
}

export const LineChart: React.FC<LineChartProps> = ({
  data,
  height = 200,
  color = "stroke-primary",
  fill = false,
  showPoints = true,
  className,
}) => {
  const maxValue = Math.max(...data.map((d) => d.value))
  const minValue = Math.min(...data.map((d) => d.value))
  const range = maxValue - minValue || 1

  const points = data.map((item, index) => {
    const x = (index / (data.length - 1)) * 100
    const y = 100 - ((item.value - minValue) / range) * 100
    return { x, y, value: item.value }
  })

  const pathData = points
    .map((point, index) => `${index === 0 ? "M" : "L"} ${point.x} ${point.y}`)
    .join(" ")

  const fillPath = fill
    ? `${pathData} L 100 100 L 0 100 Z`
    : ""

  return (
    <div className={`w-full ${className || ""}`}>
      <svg
        viewBox="0 0 100 100"
        preserveAspectRatio="none"
        className="w-full"
        style={{ height }}
      >
        {fill && (
          <path
            d={fillPath}
            className="fill-primary/10"
          />
        )}
        <path
          d={pathData}
          className={`fill-none ${color}`}
          strokeWidth="0.5"
        />
        {showPoints &&
          points.map((point, index) => (
            <circle
              key={index}
              cx={point.x}
              cy={point.y}
              r="1"
              className="fill-primary"
            />
          ))}
      </svg>

      {/* Labels */}
      <div className="flex justify-between mt-2">
        {data.map((item, index) => (
          <div key={index} className="text-xs text-muted-foreground">
            {item.label}
          </div>
        ))}
      </div>
    </div>
  )
}

LineChart.displayName = "LineChart"

// Donut/Pie Chart
export interface DonutChartProps {
  data: {
    label: string
    value: number
    color?: string
  }[]
  size?: number
  donut?: boolean
  showLegend?: boolean
  className?: string
}

export const DonutChart: React.FC<DonutChartProps> = ({
  data,
  size = 200,
  donut = true,
  showLegend = true,
  className,
}) => {
  const total = data.reduce((sum, item) => sum + item.value, 0)
  const colors = [
    "fill-blue-500",
    "fill-green-500",
    "fill-yellow-500",
    "fill-red-500",
    "fill-purple-500",
    "fill-pink-500",
  ]

  let cumulativePercent = 0

  const slices = data.map((item, index) => {
    const percent = (item.value / total) * 100
    const startAngle = (cumulativePercent / 100) * 360
    const endAngle = ((cumulativePercent + percent) / 100) * 360

    cumulativePercent += percent

    const startRad = (startAngle - 90) * (Math.PI / 180)
    const endRad = (endAngle - 90) * (Math.PI / 180)

    const x1 = 50 + 40 * Math.cos(startRad)
    const y1 = 50 + 40 * Math.sin(startRad)
    const x2 = 50 + 40 * Math.cos(endRad)
    const y2 = 50 + 40 * Math.sin(endRad)

    const largeArc = percent > 50 ? 1 : 0

    const pathData = [
      `M 50 50`,
      `L ${x1} ${y1}`,
      `A 40 40 0 ${largeArc} 1 ${x2} ${y2}`,
      `Z`,
    ].join(" ")

    return {
      pathData,
      color: item.color || colors[index % colors.length],
      label: item.label,
      value: item.value,
      percent: percent.toFixed(1),
    }
  })

  return (
    <div className={`flex flex-col items-center gap-4 ${className || ""}`}>
      <svg width={size} height={size} viewBox="0 0 100 100">
        {slices.map((slice, index) => (
          <path
            key={index}
            d={slice.pathData}
            className={`${slice.color} transition-opacity hover:opacity-80`}
          />
        ))}
        {donut && (
          <circle cx="50" cy="50" r="20" className="fill-background" />
        )}
      </svg>

      {showLegend && (
        <div className="grid grid-cols-2 gap-2 w-full max-w-xs">
          {slices.map((slice, index) => (
            <div key={index} className="flex items-center gap-2">
              <div className={`w-3 h-3 rounded-full ${slice.color}`} />
              <div className="flex-1 min-w-0">
                <div className="text-xs font-medium text-foreground truncate">
                  {slice.label}
                </div>
                <div className="text-xs text-muted-foreground">
                  {slice.value} ({slice.percent}%)
                </div>
              </div>
            </div>
          ))}
        </div>
      )}
    </div>
  )
}

DonutChart.displayName = "DonutChart"

// Sparkline (mini line chart)
export interface SparklineProps {
  data: number[]
  height?: number
  color?: string
  fill?: boolean
  className?: string
}

export const Sparkline: React.FC<SparklineProps> = ({
  data,
  height = 40,
  color = "stroke-primary",
  fill = true,
  className,
}) => {
  if (data.length < 2) return null

  const maxValue = Math.max(...data)
  const minValue = Math.min(...data)
  const range = maxValue - minValue || 1

  const points = data.map((value, index) => {
    const x = (index / (data.length - 1)) * 100
    const y = 100 - ((value - minValue) / range) * 100
    return { x, y }
  })

  const pathData = points
    .map((point, index) => `${index === 0 ? "M" : "L"} ${point.x} ${point.y}`)
    .join(" ")

  const fillPath = fill ? `${pathData} L 100 100 L 0 100 Z` : ""

  return (
    <svg
      viewBox="0 0 100 100"
      preserveAspectRatio="none"
      className={`w-full ${className || ""}`}
      style={{ height }}
    >
      {fill && <path d={fillPath} className="fill-primary/10" />}
      <path d={pathData} className={`fill-none ${color}`} strokeWidth="2" />
    </svg>
  )
}

Sparkline.displayName = "Sparkline"

// Progress Ring
export interface ProgressRingProps {
  value: number
  max?: number
  size?: number
  strokeWidth?: number
  showValue?: boolean
  color?: string
  className?: string
}

export const ProgressRing: React.FC<ProgressRingProps> = ({
  value,
  max = 100,
  size = 120,
  strokeWidth = 8,
  showValue = true,
  color = "stroke-primary",
  className,
}) => {
  const percentage = Math.min((value / max) * 100, 100)
  const radius = (size - strokeWidth) / 2
  const circumference = 2 * Math.PI * radius
  const offset = circumference - (percentage / 100) * circumference

  return (
    <div className={`relative inline-flex items-center justify-center ${className || ""}`}>
      <svg width={size} height={size} className="transform -rotate-90">
        <circle
          cx={size / 2}
          cy={size / 2}
          r={radius}
          className="stroke-muted fill-none"
          strokeWidth={strokeWidth}
        />
        <circle
          cx={size / 2}
          cy={size / 2}
          r={radius}
          className={`fill-none ${color} transition-all duration-300`}
          strokeWidth={strokeWidth}
          strokeLinecap="round"
          strokeDasharray={circumference}
          strokeDashoffset={offset}
        />
      </svg>
      {showValue && (
        <span
          className="absolute text-foreground font-bold"
          style={{ fontSize: size / 6 }}
        >
          {Math.round(percentage)}%
        </span>
      )}
    </div>
  )
}

ProgressRing.displayName = "ProgressRing"
