/**
 * useUITracking - Reusable hook for tracking UI interactions
 *
 * Automatically tracks:
 * - Button clicks
 * - Modal/dialog open/close
 * - Dropdown/select changes
 * - Tab changes
 * - Filter/sort changes
 */

import { useCallback } from 'react'
import { trackEvent } from '@/utils/analytics'
import { EventCategory } from '@/types/analytics'

interface UITrackingOptions {
  category?: EventCategory
}

interface UITrackingReturn {
  trackButtonClick: (buttonName: string, action?: string, metadata?: Record<string, any>) => void
  trackModalOpen: (modalName: string, trigger?: string) => void
  trackModalClose: (modalName: string, duration?: number) => void
  trackDropdownChange: (dropdownName: string, value: string | string[], previousValue?: string | string[]) => void
  trackTabChange: (tabName: string, fromTab?: string) => void
  trackFilterChange: (filterName: string, value: any, filterType?: string) => void
  trackSortChange: (sortField: string, sortDirection: 'asc' | 'desc') => void
}

export function useUITracking(options: UITrackingOptions = {}): UITrackingReturn {
  const { category = 'ui_interaction' } = options

  const trackButtonClick = useCallback((
    buttonName: string,
    action?: string,
    metadata?: Record<string, any>
  ) => {
    trackEvent({
      name: 'button_click',
      category,
      priority: 'low',
      properties: {
        button_name: buttonName,
        action: action || buttonName,
        ...metadata,
      },
    })
  }, [category])

  const trackModalOpen = useCallback((modalName: string, trigger?: string) => {
    trackEvent({
      name: 'modal_open',
      category,
      priority: 'low',
      properties: {
        modal_name: modalName,
        trigger: trigger || 'unknown',
      },
    })
  }, [category])

  const trackModalClose = useCallback((modalName: string, duration?: number) => {
    trackEvent({
      name: 'modal_close',
      category,
      priority: 'low',
      properties: {
        modal_name: modalName,
        duration,
      },
    })
  }, [category])

  const trackDropdownChange = useCallback((
    dropdownName: string,
    value: string | string[],
    previousValue?: string | string[]
  ) => {
    trackEvent({
      name: 'dropdown_change',
      category,
      priority: 'low',
      properties: {
        dropdown_name: dropdownName,
        value: Array.isArray(value) ? value.join(',') : value,
        previous_value: Array.isArray(previousValue) ? previousValue?.join(',') : previousValue,
        is_multi_select: Array.isArray(value),
      },
    })
  }, [category])

  const trackTabChange = useCallback((tabName: string, fromTab?: string) => {
    trackEvent({
      name: 'tab_change',
      category,
      priority: 'low',
      properties: {
        tab_name: tabName,
        from_tab: fromTab,
      },
    })
  }, [category])

  const trackFilterChange = useCallback((
    filterName: string,
    value: any,
    filterType?: string
  ) => {
    trackEvent({
      name: 'filter_change',
      category,
      priority: 'low',
      properties: {
        filter_name: filterName,
        filter_value: typeof value === 'object' ? JSON.stringify(value) : String(value),
        filter_type: filterType,
      },
    })
  }, [category])

  const trackSortChange = useCallback((
    sortField: string,
    sortDirection: 'asc' | 'desc'
  ) => {
    trackEvent({
      name: 'sort_change',
      category,
      priority: 'low',
      properties: {
        sort_field: sortField,
        sort_direction: sortDirection,
      },
    })
  }, [category])

  return {
    trackButtonClick,
    trackModalOpen,
    trackModalClose,
    trackDropdownChange,
    trackTabChange,
    trackFilterChange,
    trackSortChange,
  }
}
