export interface WorkspaceDocument {
  id: string;
  fileName: string;
  originalFileName: string;
  filePath: string;
  mimeType: string;
  fileSize: number;
  uploadedAt: string;
  uploadedById: string;
  uploaderName: string;
  folderId?: string;
  folderName?: string;
  description?: string;
  version: number;
  parentDocumentId?: string;
  isDeleted: boolean;
  deletedAt?: string;
  deletedBy?: string;
  downloadCount: number;
  lastAccessedAt?: string;
  securityScanPassed: boolean;
  securityScanResult?: string;
  tags?: string[];
  metadata?: Record<string, any>;
}

export interface DocumentFolder {
  id: string;
  name: string;
  description?: string;
  parentFolderId?: string;
  createdAt: string;
  createdById: string;
  creatorName: string;
  documentCount: number;
  fullPath: string;
  sortOrder: number;
  isDeleted: boolean;
  permissions?: DocumentPermission[];
}

export interface DocumentShare {
  id: string;
  documentId: string;
  shareToken: string;
  permission: SharePermission;
  expiresAt?: string;
  accessCount: number;
  maxAccesses?: number;
  createdAt: string;
  createdById: string;
  isRevoked: boolean;
  revokedAt?: string;
  password?: string;
}

export interface DocumentAccess {
  id: string;
  documentId: string;
  userId: string;
  userName: string;
  accessType: AccessType;
  accessedAt: string;
  ipAddress?: string;
  userAgent?: string;
  metadata?: Record<string, any>;
}

export interface DocumentPermission {
  id: string;
  documentId?: string;
  folderId?: string;
  userId: string;
  userName: string;
  permission: DocumentPermissionLevel;
  grantedAt: string;
  grantedById: string;
  expiresAt?: string;
}

export interface DocumentVersion {
  id: string;
  documentId: string;
  versionNumber: number;
  fileName: string;
  filePath: string;
  fileSize: number;
  uploadedAt: string;
  uploadedById: string;
  uploaderName: string;
  changeDescription?: string;
  isCurrentVersion: boolean;
}

export interface DocumentSearchResult {
  documents: WorkspaceDocument[];
  folders: DocumentFolder[];
  totalCount: number;
  facets: SearchFacets;
  searchTime: number;
}

export interface SearchFacets {
  fileTypes: { [mimeType: string]: number };
  uploaders: { [userId: string]: { name: string; count: number } };
  folders: { [folderId: string]: { name: string; count: number } };
  dateRanges: { [range: string]: number };
  fileSizes: { [range: string]: number };
}

export interface AdvancedSearchFilter {
  query?: string;
  fileTypes?: string[];
  uploaderIds?: string[];
  folderIds?: string[];
  dateRange?: {
    start?: string;
    end?: string;
  };
  sizeRange?: {
    min?: number;
    max?: number;
  };
  tags?: string[];
  hasDescription?: boolean;
  sortBy?: 'name' | 'size' | 'uploadedAt' | 'lastAccessedAt' | 'downloadCount';
  sortOrder?: 'asc' | 'desc';
}

export interface FilePreviewData {
  id: string;
  fileName: string;
  mimeType: string;
  fileSize: number;
  downloadUrl: string;
  previewUrl?: string;
  thumbnailUrl?: string;
  extractedText?: string;
  metadata?: Record<string, any>;
  canPreview: boolean;
  canEdit: boolean;
}

export interface BatchOperation {
  type: 'move' | 'copy' | 'delete' | 'archive' | 'share' | 'setPermissions';
  itemIds: string[];
  targetFolderId?: string;
  permissions?: DocumentPermissionLevel[];
  userIds?: string[];
  shareSettings?: {
    permission: SharePermission;
    expiresAt?: string;
    maxAccesses?: number;
    password?: string;
  };
}

export interface DocumentUploadRequest {
  file: File;
  workspaceId: string;
  folderId?: string;
  description?: string;
  tags?: string[];
  overwriteExisting?: boolean;
  parentDocumentId?: string; // For versioning
}

export interface DocumentAnalytics {
  totalDocuments: number;
  totalSize: number;
  downloadCount: number;
  topDownloads: WorkspaceDocument[];
  recentUploads: WorkspaceDocument[];
  storageByType: { [mimeType: string]: number };
  activityTrend: { date: string; uploads: number; downloads: number }[];
}

// Enums
export enum SharePermission {
  View = 'View',
  Download = 'Download',
  Edit = 'Edit'
}

export enum DocumentPermissionLevel {
  Read = 'Read',
  Write = 'Write',
  Admin = 'Admin'
}

export enum AccessType {
  View = 'View',
  Download = 'Download',
  Edit = 'Edit',
  Delete = 'Delete',
  Share = 'Share',
  ChangePermissions = 'ChangePermissions'
}

export enum DocumentStatus {
  Active = 'Active',
  Archived = 'Archived',
  Deleted = 'Deleted',
  Quarantined = 'Quarantined'
}

export enum FileCategory {
  Document = 'Document',
  Image = 'Image',
  Video = 'Video',
  Audio = 'Audio',
  Archive = 'Archive',
  Code = 'Code',
  Other = 'Other'
}