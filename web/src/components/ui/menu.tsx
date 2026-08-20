import * as React from "react"
import { ChevronRight, Check } from "lucide-react"

export interface MenuItem {
  id: string
  label: string
  icon?: React.ReactNode
  onClick?: () => void
  disabled?: boolean
  divider?: boolean
  submenu?: MenuItem[]
  shortcut?: string
  destructive?: boolean
}

export interface MenuProps {
  items: MenuItem[]
  onClose?: () => void
  className?: string
}

export const Menu = React.forwardRef<HTMLDivElement, MenuProps>(
  ({ items, onClose, className }, ref) => {
    const [openSubmenu, setOpenSubmenu] = React.useState<string | null>(null)

    const handleItemClick = (item: MenuItem) => {
      if (item.disabled) return
      if (!item.submenu) {
        item.onClick?.()
        onClose?.()
      }
    }

    const handleSubmenuToggle = (itemId: string) => {
      setOpenSubmenu((prev) => (prev === itemId ? null : itemId))
    }

    return (
      <div
        ref={ref}
        role="menu"
        className={`
          min-w-[12rem] bg-background border border-border rounded-xl shadow-lg p-1
          ${className || ""}
        `}
      >
        {items.map((item, index) => {
          if (item.divider) {
            return <div key={`divider-${index}`} className="my-1 h-px bg-border" />
          }

          const hasSubmenu = item.submenu && item.submenu.length > 0
          const isSubmenuOpen = openSubmenu === item.id

          return (
            <div key={item.id} className="relative">
              <button
                role="menuitem"
                onClick={() => {
                  if (hasSubmenu) {
                    handleSubmenuToggle(item.id)
                  } else {
                    handleItemClick(item)
                  }
                }}
                disabled={item.disabled}
                className={`
                  w-full flex items-center justify-between px-3 py-2 text-sm rounded-lg
                  transition-colors outline-none
                  ${
                    item.disabled
                      ? "opacity-50 cursor-not-allowed"
                      : item.destructive
                      ? "text-destructive hover:bg-destructive/10 focus:bg-destructive/10"
                      : "text-foreground hover:bg-accent focus:bg-accent"
                  }
                `}
              >
                <span className="flex items-center space-x-2 flex-1">
                  {item.icon && <span className="flex-shrink-0">{item.icon}</span>}
                  <span>{item.label}</span>
                </span>
                {item.shortcut && (
                  <span className="text-xs text-muted-foreground ml-4">{item.shortcut}</span>
                )}
                {hasSubmenu && (
                  <ChevronRight
                    className={`h-4 w-4 ml-2 transition-transform ${
                      isSubmenuOpen ? "rotate-90" : ""
                    }`}
                    aria-hidden="true"
                  />
                )}
              </button>

              {/* Submenu */}
              {hasSubmenu && isSubmenuOpen && (
                <div className="pl-4 mt-1">
                  <Menu items={item.submenu!} onClose={onClose} />
                </div>
              )}
            </div>
          )
        })}
      </div>
    )
  }
)

Menu.displayName = "Menu"

// Navigation menu for header/sidebar
export interface NavItem {
  id: string
  label: string
  href?: string
  icon?: React.ReactNode
  badge?: string | number
  active?: boolean
  disabled?: boolean
  children?: NavItem[]
}

export interface NavigationMenuProps {
  items: NavItem[]
  orientation?: "horizontal" | "vertical"
  onItemClick?: (item: NavItem) => void
  className?: string
}

export const NavigationMenu: React.FC<NavigationMenuProps> = ({
  items,
  orientation = "vertical",
  onItemClick,
  className,
}) => {
  const [expandedItems, setExpandedItems] = React.useState<Set<string>>(new Set())

  const toggleExpanded = (itemId: string) => {
    setExpandedItems((prev) => {
      const newSet = new Set(prev)
      if (newSet.has(itemId)) {
        newSet.delete(itemId)
      } else {
        newSet.add(itemId)
      }
      return newSet
    })
  }

  const renderNavItem = (item: NavItem, depth: number = 0) => {
    const hasChildren = item.children && item.children.length > 0
    const isExpanded = expandedItems.has(item.id)
    const paddingLeft = orientation === "vertical" ? `${depth * 1}rem` : undefined

    return (
      <div key={item.id}>
        <button
          type="button"
          onClick={() => {
            if (hasChildren) {
              toggleExpanded(item.id)
            } else {
              onItemClick?.(item)
            }
          }}
          disabled={item.disabled}
          className={`
            w-full flex items-center justify-between px-4 py-2.5 text-sm font-medium
            transition-colors rounded-lg
            ${
              item.active
                ? "bg-accent text-accent-foreground"
                : item.disabled
                ? "opacity-50 cursor-not-allowed text-muted-foreground"
                : "text-foreground hover:bg-accent hover:text-accent-foreground"
            }
          `}
          style={{ paddingLeft }}
        >
          <span className="flex items-center space-x-3 flex-1 min-w-0">
            {item.icon && <span className="flex-shrink-0">{item.icon}</span>}
            <span className="truncate">{item.label}</span>
            {item.badge && (
              <span className="ml-auto px-2 py-0.5 text-xs font-semibold bg-primary text-primary-foreground rounded-full">
                {item.badge}
              </span>
            )}
          </span>
          {hasChildren && (
            <ChevronRight
              className={`h-4 w-4 ml-2 transition-transform flex-shrink-0 ${
                isExpanded ? "rotate-90" : ""
              }`}
              aria-hidden="true"
            />
          )}
        </button>

        {/* Children */}
        {hasChildren && isExpanded && (
          <div className={orientation === "vertical" ? "mt-1 space-y-1" : "flex flex-col"}>
            {item.children!.map((child) => renderNavItem(child, depth + 1))}
          </div>
        )}
      </div>
    )
  }

  return (
    <nav
      className={`
        ${orientation === "horizontal" ? "flex space-x-2" : "space-y-1"}
        ${className || ""}
      `}
    >
      {items.map((item) => renderNavItem(item))}
    </nav>
  )
}

NavigationMenu.displayName = "NavigationMenu"

// Breadcrumb-style navigation
export interface BreadcrumbNavItem {
  label: string
  href?: string
  icon?: React.ReactNode
}

export interface BreadcrumbNavProps {
  items: BreadcrumbNavItem[]
  onItemClick?: (item: BreadcrumbNavItem, index: number) => void
  separator?: React.ReactNode
  className?: string
}

export const BreadcrumbNav: React.FC<BreadcrumbNavProps> = ({
  items,
  onItemClick,
  separator = <ChevronRight className="h-4 w-4" />,
  className,
}) => {
  return (
    <nav aria-label="Breadcrumb" className={className}>
      <ol className="flex items-center space-x-2 text-sm">
        {items.map((item, index) => {
          const isLast = index === items.length - 1

          return (
            <li key={index} className="flex items-center space-x-2">
              {index > 0 && (
                <span className="text-muted-foreground" aria-hidden="true">
                  {separator}
                </span>
              )}
              {isLast ? (
                <span className="flex items-center space-x-2 font-medium text-foreground">
                  {item.icon && <span>{item.icon}</span>}
                  <span aria-current="page">{item.label}</span>
                </span>
              ) : (
                <button
                  type="button"
                  onClick={() => onItemClick?.(item, index)}
                  className="flex items-center space-x-2 text-muted-foreground hover:text-foreground transition-colors"
                >
                  {item.icon && <span>{item.icon}</span>}
                  <span>{item.label}</span>
                </button>
              )}
            </li>
          )
        })}
      </ol>
    </nav>
  )
}

BreadcrumbNav.displayName = "BreadcrumbNav"

// Select menu (similar to native select)
export interface SelectMenuOption {
  value: string
  label: string
  description?: string
  icon?: React.ReactNode
  disabled?: boolean
}

export interface SelectMenuProps {
  options: SelectMenuOption[]
  value?: string
  onChange?: (value: string) => void
  placeholder?: string
  className?: string
}

export const SelectMenu = React.forwardRef<HTMLDivElement, SelectMenuProps>(
  ({ options, value, onChange, placeholder = "Select an option...", className }, ref) => {
    const [isOpen, setIsOpen] = React.useState(false)
    const selectedOption = options.find((opt) => opt.value === value)

    const handleSelect = (option: SelectMenuOption) => {
      if (!option.disabled) {
        onChange?.(option.value)
        setIsOpen(false)
      }
    }

    return (
      <div ref={ref} className={`relative ${className || ""}`}>
        <button
          type="button"
          onClick={() => setIsOpen(!isOpen)}
          className="w-full flex items-center justify-between px-4 py-2.5 bg-background border border-border rounded-lg hover:border-primary focus:outline-none focus:ring-2 focus:ring-primary/20"
        >
          <span className="flex items-center space-x-2">
            {selectedOption?.icon && <span>{selectedOption.icon}</span>}
            <span className={selectedOption ? "text-foreground" : "text-muted-foreground"}>
              {selectedOption?.label || placeholder}
            </span>
          </span>
          <ChevronRight
            className={`h-4 w-4 transition-transform ${isOpen ? "rotate-90" : ""}`}
            aria-hidden="true"
          />
        </button>

        {isOpen && (
          <div className="absolute z-50 w-full mt-2 bg-background border border-border rounded-xl shadow-lg p-1 max-h-64 overflow-y-auto">
            {options.map((option) => (
              <button
                key={option.value}
                type="button"
                onClick={() => handleSelect(option)}
                disabled={option.disabled}
                className={`
                  w-full flex items-center justify-between px-3 py-2.5 text-sm rounded-lg
                  transition-colors
                  ${
                    option.disabled
                      ? "opacity-50 cursor-not-allowed"
                      : option.value === value
                      ? "bg-accent text-accent-foreground"
                      : "hover:bg-accent hover:text-accent-foreground"
                  }
                `}
              >
                <span className="flex items-center space-x-2 flex-1">
                  {option.icon && <span>{option.icon}</span>}
                  <span className="flex-1 text-left">
                    <div className="font-medium">{option.label}</div>
                    {option.description && (
                      <div className="text-xs text-muted-foreground">{option.description}</div>
                    )}
                  </span>
                </span>
                {option.value === value && (
                  <Check className="h-4 w-4 ml-2" aria-hidden="true" />
                )}
              </button>
            ))}
          </div>
        )}
      </div>
    )
  }
)

SelectMenu.displayName = "SelectMenu"
