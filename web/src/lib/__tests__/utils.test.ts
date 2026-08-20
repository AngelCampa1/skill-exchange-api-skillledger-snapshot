/**
 * Tests for utils.ts
 *
 * This file validates the cn (className) utility function
 */

import { cn } from '../utils'

describe('utils.ts - cn (className) utility', () => {
  describe('Basic functionality', () => {
    it('should combine multiple class names', () => {
      expect(cn('class1', 'class2', 'class3')).toBe('class1 class2 class3')
    })

    it('should handle single class name', () => {
      expect(cn('single-class')).toBe('single-class')
    })

    it('should handle empty input', () => {
      expect(cn()).toBe('')
    })

    it('should handle undefined and null values', () => {
      expect(cn('class1', undefined, 'class2', null, 'class3')).toBe('class1 class2 class3')
    })

    it('should handle boolean conditional classes', () => {
      const isActive = true
      const isDisabled = false

      expect(cn('base', isActive && 'active', isDisabled && 'disabled')).toBe('base active')
    })
  })

  describe('Object syntax', () => {
    it('should handle object with boolean values', () => {
      expect(
        cn({
          'class1': true,
          'class2': false,
          'class3': true,
        })
      ).toBe('class1 class3')
    })

    it('should combine string and object inputs', () => {
      expect(
        cn('base-class', {
          'active': true,
          'disabled': false,
        })
      ).toBe('base-class active')
    })
  })

  describe('Array syntax', () => {
    it('should handle array of class names', () => {
      expect(cn(['class1', 'class2', 'class3'])).toBe('class1 class2 class3')
    })

    it('should handle nested arrays', () => {
      expect(cn(['class1', ['class2', 'class3']])).toBe('class1 class2 class3')
    })

    it('should handle array with conditional values', () => {
      expect(cn(['class1', false && 'class2', 'class3'])).toBe('class1 class3')
    })
  })

  describe('Complex scenarios', () => {
    it('should handle mix of strings, objects, and arrays', () => {
      expect(
        cn(
          'base',
          ['array-class'],
          { 'object-class': true, 'skip': false },
          'final-class'
        )
      ).toBe('base array-class object-class final-class')
    })

    it('should handle duplicate class names', () => {
      expect(cn('class1', 'class2', 'class1')).toBe('class1 class2 class1')
    })

    it('should preserve whitespace (clsx behavior)', () => {
      expect(cn('  class1  ', 'class2')).toBe('  class1   class2')
    })
  })

  describe('Real-world use cases', () => {
    it('should work with Tailwind CSS utility classes', () => {
      const isError = true
      const isLarge = false

      expect(
        cn(
          'px-4 py-2 rounded',
          isError && 'text-red-500 border-red-500',
          isLarge && 'text-lg'
        )
      ).toBe('px-4 py-2 rounded text-red-500 border-red-500')
    })

    it('should work with component variants', () => {
      const variant = 'primary'
      const size = 'lg'

      expect(
        cn(
          'button',
          (variant as string) === 'primary' && 'bg-blue-500 text-white',
          (variant as string) === 'secondary' && 'bg-gray-500 text-black',
          (size as string) === 'sm' && 'text-sm px-2 py-1',
          (size as string) === 'lg' && 'text-lg px-4 py-2'
        )
      ).toBe('button bg-blue-500 text-white text-lg px-4 py-2')
    })

    it('should work with conditional state classes', () => {
      const state = {
        isActive: true,
        isDisabled: false,
        isLoading: true,
      }

      expect(
        cn(
          'component',
          state.isActive && 'active',
          state.isDisabled && 'disabled',
          state.isLoading && 'loading'
        )
      ).toBe('component active loading')
    })
  })

  describe('Edge cases', () => {
    it('should handle empty strings', () => {
      expect(cn('', 'class1', '', 'class2')).toBe('class1 class2')
    })

    it('should handle only falsy values', () => {
      expect(cn(false, null, undefined, 0, '')).toBe('')
    })

    it('should handle numeric values', () => {
      expect(cn('class1', 0, 'class2', 123)).toBe('class1 class2 123')
    })

    it('should preserve multiple spaces (clsx behavior)', () => {
      expect(cn('class1   class2', 'class3')).toBe('class1   class2 class3')
    })
  })
})
