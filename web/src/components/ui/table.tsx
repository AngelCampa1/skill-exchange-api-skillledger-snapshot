import * as React from "react"
import { ArrowDown, ArrowUp, ArrowUpDown } from "lucide-react"

export interface Column<T> {
  key: string
  label: string
  sortable?: boolean
  render?: (row: T, value: any) => React.ReactNode
  width?: string
  align?: "left" | "center" | "right"
}

export interface TableProps<T> {
  data: T[]
  columns: Column<T>[]
  keyExtractor: (row: T) => string | number
  onRowClick?: (row: T) => void
  sortBy?: string
  sortDirection?: "asc" | "desc"
  onSort?: (key: string, direction: "asc" | "desc") => void
  loading?: boolean
  emptyMessage?: string
  striped?: boolean
  hoverable?: boolean
  bordered?: boolean
  compact?: boolean
  className?: string
}

export const Table = <T,>({
  data,
  columns,
  keyExtractor,
  onRowClick,
  sortBy: controlledSortBy,
  sortDirection: controlledSortDirection,
  onSort,
  loading = false,
  emptyMessage = "No data available",
  striped = false,
  hoverable = true,
  bordered = false,
  compact = false,
  className,
}: TableProps<T>) => {
  const [internalSortBy, setInternalSortBy] = React.useState<string | undefined>()
  const [internalSortDirection, setInternalSortDirection] = React.useState<"asc" | "desc">("asc")

  const sortBy = controlledSortBy !== undefined ? controlledSortBy : internalSortBy
  const sortDirection = controlledSortDirection !== undefined ? controlledSortDirection : internalSortDirection

  const handleSort = (key: string) => {
    const column = columns.find((col) => col.key === key)
    if (!column?.sortable) return

    let newDirection: "asc" | "desc" = "asc"
    if (sortBy === key) {
      newDirection = sortDirection === "asc" ? "desc" : "asc"
    }

    if (controlledSortBy === undefined) {
      setInternalSortBy(key)
      setInternalSortDirection(newDirection)
    }
    onSort?.(key, newDirection)
  }

  const sortedData = React.useMemo(() => {
    if (!sortBy) return data

    return [...data].sort((a, b) => {
      const aValue = (a as any)[sortBy]
      const bValue = (b as any)[sortBy]

      if (aValue === bValue) return 0

      let comparison = 0
      if (typeof aValue === "string" && typeof bValue === "string") {
        comparison = aValue.localeCompare(bValue)
      } else if (typeof aValue === "number" && typeof bValue === "number") {
        comparison = aValue - bValue
      } else if (aValue instanceof Date && bValue instanceof Date) {
        comparison = aValue.getTime() - bValue.getTime()
      } else {
        comparison = String(aValue).localeCompare(String(bValue))
      }

      return sortDirection === "asc" ? comparison : -comparison
    })
  }, [data, sortBy, sortDirection])

  const getSortIcon = (columnKey: string) => {
    if (sortBy !== columnKey) {
      return <ArrowUpDown className="h-4 w-4 text-muted-foreground" aria-hidden="true" />
    }
    return sortDirection === "asc" ? (
      <ArrowUp className="h-4 w-4 text-foreground" aria-hidden="true" />
    ) : (
      <ArrowDown className="h-4 w-4 text-foreground" aria-hidden="true" />
    )
  }

  const getAlignmentClass = (align?: "left" | "center" | "right") => {
    switch (align) {
      case "center":
        return "text-center"
      case "right":
        return "text-right"
      default:
        return "text-left"
    }
  }

  return (
    <div className={`w-full overflow-x-auto ${className || ""}`}>
      <table className="w-full border-collapse">
        <thead>
          <tr
            className={`border-b border-border bg-muted/50 ${
              bordered ? "border-x" : ""
            }`}
          >
            {columns.map((column) => (
              <th
                key={column.key}
                scope="col"
                className={`
                  ${compact ? "px-3 py-2" : "px-4 py-3"}
                  text-sm font-semibold text-foreground
                  ${getAlignmentClass(column.align)}
                  ${bordered ? "border-r border-border last:border-r-0" : ""}
                `}
                style={{ width: column.width }}
              >
                {column.sortable ? (
                  <button
                    type="button"
                    onClick={() => handleSort(column.key)}
                    className="inline-flex items-center space-x-2 hover:text-primary transition-colors"
                    aria-label={`Sort by ${column.label}`}
                  >
                    <span>{column.label}</span>
                    {getSortIcon(column.key)}
                  </button>
                ) : (
                  column.label
                )}
              </th>
            ))}
          </tr>
        </thead>
        <tbody>
          {loading ? (
            <tr>
              <td
                colSpan={columns.length}
                className="px-4 py-12 text-center text-sm text-muted-foreground"
              >
                <div className="flex items-center justify-center space-x-2">
                  <div className="h-5 w-5 animate-spin rounded-full border-2 border-muted-foreground/20 border-t-muted-foreground" />
                  <span>Loading...</span>
                </div>
              </td>
            </tr>
          ) : sortedData.length === 0 ? (
            <tr>
              <td
                colSpan={columns.length}
                className="px-4 py-12 text-center text-sm text-muted-foreground"
              >
                {emptyMessage}
              </td>
            </tr>
          ) : (
            sortedData.map((row, rowIndex) => (
              <tr
                key={keyExtractor(row)}
                onClick={() => onRowClick?.(row)}
                className={`
                  border-b border-border last:border-b-0
                  ${bordered ? "border-x" : ""}
                  ${striped && rowIndex % 2 === 1 ? "bg-muted/30" : ""}
                  ${hoverable ? "hover:bg-muted/50 transition-colors" : ""}
                  ${onRowClick ? "cursor-pointer" : ""}
                `}
              >
                {columns.map((column) => {
                  const value = (row as any)[column.key]
                  const content = column.render ? column.render(row, value) : value

                  return (
                    <td
                      key={column.key}
                      className={`
                        ${compact ? "px-3 py-2" : "px-4 py-3"}
                        text-sm text-foreground
                        ${getAlignmentClass(column.align)}
                        ${bordered ? "border-r border-border last:border-r-0" : ""}
                      `}
                    >
                      {content}
                    </td>
                  )
                })}
              </tr>
            ))
          )}
        </tbody>
      </table>
    </div>
  )
}

Table.displayName = "Table"

// Simple table wrapper components for basic usage
export const TableRoot = React.forwardRef<
  HTMLTableElement,
  React.HTMLAttributes<HTMLTableElement>
>(({ className, ...props }, ref) => (
  <div className="w-full overflow-x-auto">
    <table
      ref={ref}
      className={`w-full border-collapse ${className || ""}`}
      {...props}
    />
  </div>
))

TableRoot.displayName = "TableRoot"

export const TableHeader = React.forwardRef<
  HTMLTableSectionElement,
  React.HTMLAttributes<HTMLTableSectionElement>
>(({ className, ...props }, ref) => (
  <thead
    ref={ref}
    className={`border-b border-border bg-muted/50 ${className || ""}`}
    {...props}
  />
))

TableHeader.displayName = "TableHeader"

export const TableBody = React.forwardRef<
  HTMLTableSectionElement,
  React.HTMLAttributes<HTMLTableSectionElement>
>(({ className, ...props }, ref) => (
  <tbody ref={ref} className={className} {...props} />
))

TableBody.displayName = "TableBody"

export const TableRow = React.forwardRef<
  HTMLTableRowElement,
  React.HTMLAttributes<HTMLTableRowElement> & { hoverable?: boolean }
>(({ className, hoverable = true, ...props }, ref) => (
  <tr
    ref={ref}
    className={`
      border-b border-border last:border-b-0
      ${hoverable ? "hover:bg-muted/50 transition-colors" : ""}
      ${className || ""}
    `}
    {...props}
  />
))

TableRow.displayName = "TableRow"

export const TableHead = React.forwardRef<
  HTMLTableCellElement,
  React.ThHTMLAttributes<HTMLTableCellElement>
>(({ className, ...props }, ref) => (
  <th
    ref={ref}
    scope="col"
    className={`px-4 py-3 text-sm font-semibold text-foreground text-left ${className || ""}`}
    {...props}
  />
))

TableHead.displayName = "TableHead"

export const TableCell = React.forwardRef<
  HTMLTableCellElement,
  React.TdHTMLAttributes<HTMLTableCellElement>
>(({ className, ...props }, ref) => (
  <td
    ref={ref}
    className={`px-4 py-3 text-sm text-foreground ${className || ""}`}
    {...props}
  />
))

TableCell.displayName = "TableCell"
