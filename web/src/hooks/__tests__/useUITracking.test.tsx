/**
 * Tests for useUITracking hook
 */

import { renderHook, act } from '@testing-library/react'
import { useUITracking } from '../useUITracking'
import { trackEvent } from '@/utils/analytics'

// Mock analytics
jest.mock('@/utils/analytics', () => ({
  trackEvent: jest.fn(),
}))

const mockTrackEvent = trackEvent as jest.MockedFunction<typeof trackEvent>

describe('useUITracking', () => {
  beforeEach(() => {
    jest.clearAllMocks()
  })

  describe('trackButtonClick', () => {
    it('should track button click with button name', () => {
      const { result } = renderHook(() => useUITracking())

      act(() => {
        result.current.trackButtonClick('submit-button')
      })

      expect(mockTrackEvent).toHaveBeenCalledWith({
        name: 'button_click',
        category: 'ui_interaction',
        priority: 'low',
        properties: {
          button_name: 'submit-button',
          action: 'submit-button',
        },
      })
    })

    it('should track button click with custom action', () => {
      const { result } = renderHook(() => useUITracking())

      act(() => {
        result.current.trackButtonClick('delete-button', 'delete_project')
      })

      expect(mockTrackEvent).toHaveBeenCalledWith({
        name: 'button_click',
        category: 'ui_interaction',
        priority: 'low',
        properties: {
          button_name: 'delete-button',
          action: 'delete_project',
        },
      })
    })

    it('should track button click with metadata', () => {
      const { result } = renderHook(() => useUITracking())

      act(() => {
        result.current.trackButtonClick('share-button', 'share', {
          project_id: '123',
          share_type: 'email',
        })
      })

      expect(mockTrackEvent).toHaveBeenCalledWith({
        name: 'button_click',
        category: 'ui_interaction',
        priority: 'low',
        properties: {
          button_name: 'share-button',
          action: 'share',
          project_id: '123',
          share_type: 'email',
        },
      })
    })

    it('should use custom category when provided', () => {
      const { result } = renderHook(() => useUITracking({ category: 'navigation' }))

      act(() => {
        result.current.trackButtonClick('test-button')
      })

      expect(mockTrackEvent).toHaveBeenCalledWith(
        expect.objectContaining({
          category: 'navigation',
        })
      )
    })
  })

  describe('trackModalOpen', () => {
    it('should track modal open with modal name', () => {
      const { result } = renderHook(() => useUITracking())

      act(() => {
        result.current.trackModalOpen('delete-confirmation')
      })

      expect(mockTrackEvent).toHaveBeenCalledWith({
        name: 'modal_open',
        category: 'ui_interaction',
        priority: 'low',
        properties: {
          modal_name: 'delete-confirmation',
          trigger: 'unknown',
        },
      })
    })

    it('should track modal open with trigger', () => {
      const { result } = renderHook(() => useUITracking())

      act(() => {
        result.current.trackModalOpen('share-modal', 'share-button')
      })

      expect(mockTrackEvent).toHaveBeenCalledWith({
        name: 'modal_open',
        category: 'ui_interaction',
        priority: 'low',
        properties: {
          modal_name: 'share-modal',
          trigger: 'share-button',
        },
      })
    })
  })

  describe('trackModalClose', () => {
    it('should track modal close without duration', () => {
      const { result } = renderHook(() => useUITracking())

      act(() => {
        result.current.trackModalClose('delete-confirmation')
      })

      expect(mockTrackEvent).toHaveBeenCalledWith({
        name: 'modal_close',
        category: 'ui_interaction',
        priority: 'low',
        properties: {
          modal_name: 'delete-confirmation',
          duration: undefined,
        },
      })
    })

    it('should track modal close with duration', () => {
      const { result } = renderHook(() => useUITracking())

      act(() => {
        result.current.trackModalClose('settings-modal', 5000)
      })

      expect(mockTrackEvent).toHaveBeenCalledWith({
        name: 'modal_close',
        category: 'ui_interaction',
        priority: 'low',
        properties: {
          modal_name: 'settings-modal',
          duration: 5000,
        },
      })
    })
  })

  describe('trackDropdownChange', () => {
    it('should track single select dropdown change', () => {
      const { result } = renderHook(() => useUITracking())

      act(() => {
        result.current.trackDropdownChange('status-filter', 'active')
      })

      expect(mockTrackEvent).toHaveBeenCalledWith({
        name: 'dropdown_change',
        category: 'ui_interaction',
        priority: 'low',
        properties: {
          dropdown_name: 'status-filter',
          value: 'active',
          previous_value: undefined,
          is_multi_select: false,
        },
      })
    })

    it('should track multi-select dropdown change', () => {
      const { result } = renderHook(() => useUITracking())

      act(() => {
        result.current.trackDropdownChange('skills-filter', ['javascript', 'typescript'])
      })

      expect(mockTrackEvent).toHaveBeenCalledWith({
        name: 'dropdown_change',
        category: 'ui_interaction',
        priority: 'low',
        properties: {
          dropdown_name: 'skills-filter',
          value: 'javascript,typescript',
          previous_value: undefined,
          is_multi_select: true,
        },
      })
    })

    it('should track dropdown change with previous value', () => {
      const { result } = renderHook(() => useUITracking())

      act(() => {
        result.current.trackDropdownChange('sort-by', 'date', 'name')
      })

      expect(mockTrackEvent).toHaveBeenCalledWith({
        name: 'dropdown_change',
        category: 'ui_interaction',
        priority: 'low',
        properties: {
          dropdown_name: 'sort-by',
          value: 'date',
          previous_value: 'name',
          is_multi_select: false,
        },
      })
    })

    it('should track multi-select with previous values', () => {
      const { result } = renderHook(() => useUITracking())

      act(() => {
        result.current.trackDropdownChange(
          'categories',
          ['web', 'mobile', 'design'],
          ['web', 'mobile']
        )
      })

      expect(mockTrackEvent).toHaveBeenCalledWith({
        name: 'dropdown_change',
        category: 'ui_interaction',
        priority: 'low',
        properties: {
          dropdown_name: 'categories',
          value: 'web,mobile,design',
          previous_value: 'web,mobile',
          is_multi_select: true,
        },
      })
    })
  })

  describe('trackTabChange', () => {
    it('should track tab change without previous tab', () => {
      const { result } = renderHook(() => useUITracking())

      act(() => {
        result.current.trackTabChange('profile')
      })

      expect(mockTrackEvent).toHaveBeenCalledWith({
        name: 'tab_change',
        category: 'ui_interaction',
        priority: 'low',
        properties: {
          tab_name: 'profile',
          from_tab: undefined,
        },
      })
    })

    it('should track tab change with previous tab', () => {
      const { result } = renderHook(() => useUITracking())

      act(() => {
        result.current.trackTabChange('settings', 'profile')
      })

      expect(mockTrackEvent).toHaveBeenCalledWith({
        name: 'tab_change',
        category: 'ui_interaction',
        priority: 'low',
        properties: {
          tab_name: 'settings',
          from_tab: 'profile',
        },
      })
    })
  })

  describe('trackFilterChange', () => {
    it('should track simple filter change', () => {
      const { result } = renderHook(() => useUITracking())

      act(() => {
        result.current.trackFilterChange('status', 'active')
      })

      expect(mockTrackEvent).toHaveBeenCalledWith({
        name: 'filter_change',
        category: 'ui_interaction',
        priority: 'low',
        properties: {
          filter_name: 'status',
          filter_value: 'active',
          filter_type: undefined,
        },
      })
    })

    it('should track filter change with type', () => {
      const { result } = renderHook(() => useUITracking())

      act(() => {
        result.current.trackFilterChange('price', '100-500', 'range')
      })

      expect(mockTrackEvent).toHaveBeenCalledWith({
        name: 'filter_change',
        category: 'ui_interaction',
        priority: 'low',
        properties: {
          filter_name: 'price',
          filter_value: '100-500',
          filter_type: 'range',
        },
      })
    })

    it('should track object filter change', () => {
      const { result } = renderHook(() => useUITracking())

      act(() => {
        result.current.trackFilterChange('date-range', { start: '2024-01-01', end: '2024-12-31' }, 'date')
      })

      expect(mockTrackEvent).toHaveBeenCalledWith({
        name: 'filter_change',
        category: 'ui_interaction',
        priority: 'low',
        properties: {
          filter_name: 'date-range',
          filter_value: '{"start":"2024-01-01","end":"2024-12-31"}',
          filter_type: 'date',
        },
      })
    })

    it('should track number filter change', () => {
      const { result } = renderHook(() => useUITracking())

      act(() => {
        result.current.trackFilterChange('min-rating', 4.5, 'number')
      })

      expect(mockTrackEvent).toHaveBeenCalledWith({
        name: 'filter_change',
        category: 'ui_interaction',
        priority: 'low',
        properties: {
          filter_name: 'min-rating',
          filter_value: '4.5',
          filter_type: 'number',
        },
      })
    })
  })

  describe('trackSortChange', () => {
    it('should track ascending sort', () => {
      const { result } = renderHook(() => useUITracking())

      act(() => {
        result.current.trackSortChange('name', 'asc')
      })

      expect(mockTrackEvent).toHaveBeenCalledWith({
        name: 'sort_change',
        category: 'ui_interaction',
        priority: 'low',
        properties: {
          sort_field: 'name',
          sort_direction: 'asc',
        },
      })
    })

    it('should track descending sort', () => {
      const { result } = renderHook(() => useUITracking())

      act(() => {
        result.current.trackSortChange('date', 'desc')
      })

      expect(mockTrackEvent).toHaveBeenCalledWith({
        name: 'sort_change',
        category: 'ui_interaction',
        priority: 'low',
        properties: {
          sort_field: 'date',
          sort_direction: 'desc',
        },
      })
    })
  })
})
