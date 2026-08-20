/**
 * Tests for document.ts type definitions
 *
 * This file validates that document types and enums are correctly defined
 */

import {
  SharePermission,
  DocumentPermissionLevel,
  AccessType,
  DocumentStatus,
  FileCategory,
} from '@/types/document'

describe('document types', () => {
  describe('SharePermission enum', () => {
    it('should have all share permission values defined', () => {
      expect(SharePermission.View).toBe('View')
      expect(SharePermission.Download).toBe('Download')
      expect(SharePermission.Edit).toBe('Edit')
    })

    it('should have exactly 3 permission values', () => {
      const permissions = Object.values(SharePermission)
      expect(permissions).toHaveLength(3)
    })

    it('should be usable in type guards', () => {
      const permission: SharePermission = SharePermission.View
      expect(Object.values(SharePermission).includes(permission)).toBe(true)
    })
  })

  describe('DocumentPermissionLevel enum', () => {
    it('should have all permission level values defined', () => {
      expect(DocumentPermissionLevel.Read).toBe('Read')
      expect(DocumentPermissionLevel.Write).toBe('Write')
      expect(DocumentPermissionLevel.Admin).toBe('Admin')
    })

    it('should have exactly 3 permission levels', () => {
      const levels = Object.values(DocumentPermissionLevel)
      expect(levels).toHaveLength(3)
    })

    it('should support hierarchical permission checks', () => {
      const userLevel = DocumentPermissionLevel.Write
      const isReadAllowed = [DocumentPermissionLevel.Read, DocumentPermissionLevel.Write, DocumentPermissionLevel.Admin].includes(userLevel)
      const isAdminAllowed = [DocumentPermissionLevel.Admin].includes(userLevel)

      expect(isReadAllowed).toBe(true)
      expect(isAdminAllowed).toBe(false)
    })
  })

  describe('AccessType enum', () => {
    it('should have all access type values defined', () => {
      expect(AccessType.View).toBe('View')
      expect(AccessType.Download).toBe('Download')
      expect(AccessType.Edit).toBe('Edit')
      expect(AccessType.Delete).toBe('Delete')
      expect(AccessType.Share).toBe('Share')
      expect(AccessType.ChangePermissions).toBe('ChangePermissions')
    })

    it('should have exactly 6 access types', () => {
      const types = Object.values(AccessType)
      expect(types).toHaveLength(6)
    })

    it('should support permission checking logic', () => {
      const allowedActions = [AccessType.View, AccessType.Download, AccessType.Edit]
      expect(allowedActions.includes(AccessType.View)).toBe(true)
      expect(allowedActions.includes(AccessType.Delete)).toBe(false)
    })
  })

  describe('DocumentStatus enum', () => {
    it('should have all document status values defined', () => {
      expect(DocumentStatus.Active).toBe('Active')
      expect(DocumentStatus.Archived).toBe('Archived')
      expect(DocumentStatus.Deleted).toBe('Deleted')
      expect(DocumentStatus.Quarantined).toBe('Quarantined')
    })

    it('should have exactly 4 status values', () => {
      const statuses = Object.values(DocumentStatus)
      expect(statuses).toHaveLength(4)
    })

    it('should support status filtering', () => {
      const visibleStatuses = [DocumentStatus.Active, DocumentStatus.Archived]
      const hiddenStatuses = [DocumentStatus.Deleted, DocumentStatus.Quarantined]

      expect(visibleStatuses).toContain(DocumentStatus.Active)
      expect(hiddenStatuses).toContain(DocumentStatus.Quarantined)
    })
  })

  describe('FileCategory enum', () => {
    it('should have basic file category values defined', () => {
      expect(FileCategory.Document).toBe('Document')
      expect(FileCategory.Image).toBe('Image')
    })

    it('should have at least 2 category values', () => {
      const categories = Object.values(FileCategory)
      expect(categories.length).toBeGreaterThanOrEqual(2)
    })
  })

  describe('Enum value uniqueness', () => {
    it('should have unique SharePermission values', () => {
      const values = Object.values(SharePermission)
      const uniqueValues = new Set(values)
      expect(uniqueValues.size).toBe(values.length)
    })

    it('should have unique DocumentPermissionLevel values', () => {
      const values = Object.values(DocumentPermissionLevel)
      const uniqueValues = new Set(values)
      expect(uniqueValues.size).toBe(values.length)
    })

    it('should have unique AccessType values', () => {
      const values = Object.values(AccessType)
      const uniqueValues = new Set(values)
      expect(uniqueValues.size).toBe(values.length)
    })

    it('should have unique DocumentStatus values', () => {
      const values = Object.values(DocumentStatus)
      const uniqueValues = new Set(values)
      expect(uniqueValues.size).toBe(values.length)
    })
  })

  describe('Type safety validation', () => {
    it('should enforce SharePermission type constraints', () => {
      const permission: SharePermission = SharePermission.View
      const isValid = (p: string): p is SharePermission => {
        return Object.values(SharePermission).includes(p as SharePermission)
      }

      expect(isValid(permission)).toBe(true)
      expect(isValid('InvalidPermission')).toBe(false)
    })

    it('should enforce DocumentStatus type constraints', () => {
      const status: DocumentStatus = DocumentStatus.Active
      const isValidStatus = (s: string): s is DocumentStatus => {
        return Object.values(DocumentStatus).includes(s as DocumentStatus)
      }

      expect(isValidStatus(status)).toBe(true)
      expect(isValidStatus('InvalidStatus')).toBe(false)
    })
  })
})
