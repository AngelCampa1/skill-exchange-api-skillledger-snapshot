import * as React from "react"

export interface VirtualListProps<T> {
  items: T[]
  itemHeight: number
  height: number
  renderItem: (item: T, index: number) => React.ReactNode
  overscan?: number
  className?: string
  onEndReached?: () => void
  endReachedThreshold?: number
}

export const VirtualList = <T,>({
  items,
  itemHeight,
  height,
  renderItem,
  overscan = 3,
  className,
  onEndReached,
  endReachedThreshold = 0.8,
}: VirtualListProps<T>) => {
  const [scrollTop, setScrollTop] = React.useState(0)
  const containerRef = React.useRef<HTMLDivElement>(null)

  const totalHeight = items.length * itemHeight
  const visibleCount = Math.ceil(height / itemHeight)
  const startIndex = Math.max(0, Math.floor(scrollTop / itemHeight) - overscan)
  const endIndex = Math.min(items.length - 1, startIndex + visibleCount + overscan * 2)

  const visibleItems = items.slice(startIndex, endIndex + 1)
  const offsetY = startIndex * itemHeight

  const handleScroll = React.useCallback(
    (e: React.UIEvent<HTMLDivElement>) => {
      const target = e.currentTarget
      setScrollTop(target.scrollTop)

      if (onEndReached) {
        const scrollPercentage =
          (target.scrollTop + target.clientHeight) / target.scrollHeight
        if (scrollPercentage >= endReachedThreshold) {
          onEndReached()
        }
      }
    },
    [onEndReached, endReachedThreshold]
  )

  return (
    <div
      ref={containerRef}
      className={`overflow-auto ${className || ""}`}
      style={{ height }}
      onScroll={handleScroll}
    >
      <div style={{ height: totalHeight, position: "relative" }}>
        <div style={{ transform: `translateY(${offsetY}px)` }}>
          {visibleItems.map((item, index) => (
            <div key={startIndex + index} style={{ height: itemHeight }}>
              {renderItem(item, startIndex + index)}
            </div>
          ))}
        </div>
      </div>
    </div>
  )
}

VirtualList.displayName = "VirtualList"

// Infinite scroll list
export interface InfiniteScrollListProps<T> {
  items: T[]
  renderItem: (item: T, index: number) => React.ReactNode
  onLoadMore: () => void
  hasMore: boolean
  loading?: boolean
  height?: number
  className?: string
}

export const InfiniteScrollList = <T,>({
  items,
  renderItem,
  onLoadMore,
  hasMore,
  loading = false,
  height,
  className,
}: InfiniteScrollListProps<T>) => {
  const observerTarget = React.useRef<HTMLDivElement>(null)

  React.useEffect(() => {
    const observer = new IntersectionObserver(
      (entries) => {
        if (entries[0].isIntersecting && hasMore && !loading) {
          onLoadMore()
        }
      },
      { threshold: 0.1 }
    )

    const currentTarget = observerTarget.current
    if (currentTarget) {
      observer.observe(currentTarget)
    }

    return () => {
      if (currentTarget) {
        observer.unobserve(currentTarget)
      }
    }
  }, [hasMore, loading, onLoadMore])

  return (
    <div
      className={`overflow-auto ${className || ""}`}
      style={height ? { height } : undefined}
    >
      <div className="space-y-2">
        {items.map((item, index) => (
          <div key={index}>{renderItem(item, index)}</div>
        ))}

        {hasMore && (
          <div ref={observerTarget} className="py-4 text-center">
            {loading ? (
              <div className="flex items-center justify-center space-x-2">
                <div className="h-5 w-5 animate-spin rounded-full border-2 border-muted-foreground/20 border-t-muted-foreground" />
                <span className="text-sm text-muted-foreground">Loading more...</span>
              </div>
            ) : (
              <span className="text-sm text-muted-foreground">Scroll for more</span>
            )}
          </div>
        )}
      </div>
    </div>
  )
}

InfiniteScrollList.displayName = "InfiniteScrollList"

// Grid list with virtualization
export interface VirtualGridProps<T> {
  items: T[]
  itemWidth: number
  itemHeight: number
  height: number
  gap?: number
  renderItem: (item: T, index: number) => React.ReactNode
  className?: string
}

export const VirtualGrid = <T,>({
  items,
  itemWidth,
  itemHeight,
  height,
  gap = 16,
  renderItem,
  className,
}: VirtualGridProps<T>) => {
  const [scrollTop, setScrollTop] = React.useState(0)
  const [containerWidth, setContainerWidth] = React.useState(0)
  const containerRef = React.useRef<HTMLDivElement>(null)

  React.useEffect(() => {
    if (containerRef.current) {
      setContainerWidth(containerRef.current.clientWidth)
    }

    const handleResize = () => {
      if (containerRef.current) {
        setContainerWidth(containerRef.current.clientWidth)
      }
    }

    window.addEventListener("resize", handleResize)
    return () => window.removeEventListener("resize", handleResize)
  }, [])

  const columnsCount = Math.floor((containerWidth + gap) / (itemWidth + gap)) || 1
  const rowHeight = itemHeight + gap
  const rowsCount = Math.ceil(items.length / columnsCount)
  const totalHeight = rowsCount * rowHeight

  const visibleRowsCount = Math.ceil(height / rowHeight)
  const startRow = Math.max(0, Math.floor(scrollTop / rowHeight) - 1)
  const endRow = Math.min(rowsCount - 1, startRow + visibleRowsCount + 2)

  const visibleItems: Array<{ item: T; index: number }> = []
  for (let row = startRow; row <= endRow; row++) {
    for (let col = 0; col < columnsCount; col++) {
      const index = row * columnsCount + col
      if (index < items.length) {
        visibleItems.push({ item: items[index], index })
      }
    }
  }

  const handleScroll = (e: React.UIEvent<HTMLDivElement>) => {
    setScrollTop(e.currentTarget.scrollTop)
  }

  return (
    <div
      ref={containerRef}
      className={`overflow-auto ${className || ""}`}
      style={{ height }}
      onScroll={handleScroll}
    >
      <div style={{ height: totalHeight, position: "relative" }}>
        <div
          className="grid"
          style={{
            gridTemplateColumns: `repeat(${columnsCount}, ${itemWidth}px)`,
            gap: `${gap}px`,
            transform: `translateY(${startRow * rowHeight}px)`,
          }}
        >
          {visibleItems.map(({ item, index }) => (
            <div key={index} style={{ height: itemHeight }}>
              {renderItem(item, index)}
            </div>
          ))}
        </div>
      </div>
    </div>
  )
}

VirtualGrid.displayName = "VirtualGrid"

// Auto-sizer wrapper for responsive virtual lists
export interface AutoSizerProps {
  children: (size: { width: number; height: number }) => React.ReactNode
  className?: string
}

export const AutoSizer: React.FC<AutoSizerProps> = ({ children, className }) => {
  const [size, setSize] = React.useState({ width: 0, height: 0 })
  const containerRef = React.useRef<HTMLDivElement>(null)

  React.useEffect(() => {
    if (!containerRef.current) return

    const resizeObserver = new ResizeObserver((entries) => {
      const entry = entries[0]
      if (entry) {
        setSize({
          width: entry.contentRect.width,
          height: entry.contentRect.height,
        })
      }
    })

    resizeObserver.observe(containerRef.current)

    return () => {
      resizeObserver.disconnect()
    }
  }, [])

  return (
    <div ref={containerRef} className={`w-full h-full ${className || ""}`}>
      {size.width > 0 && size.height > 0 && children(size)}
    </div>
  )
}

AutoSizer.displayName = "AutoSizer"
