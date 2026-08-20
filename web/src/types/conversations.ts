/**
 * TypeScript types for conversations/workspaces list
 * Mirrors backend WorkspaceListDto and WorkspaceDashboardDto
 */

export enum WorkspaceStatus {
  Active = 1,
  Archived = 2,
  Deleted = 3
}

/**
 * Conversation preview for list display
 * Maps to WorkspaceListDto from backend
 */
export interface ConversationPreview {
  id: string;
  projectTitle: string;
  otherParticipantName: string;
  status: WorkspaceStatus;
  createdAt: string;
  lastActivity?: string;
  isClient: boolean;
  /** Client-side computed/enriched fields */
  unreadCount?: number;
  lastMessagePreview?: string;
  otherParticipantAvatar?: string;
}

/**
 * Detailed workspace/conversation data
 * Maps to WorkspaceDashboardDto from backend
 */
export interface ConversationDetails {
  workspaceId: string;
  projectTitle: string;
  projectDescription: string;
  clientName: string;
  providerName: string;
  status: WorkspaceStatus;
  createdAt: string;
  archivedAt?: string;
  timelineData?: string;
  milestoneData?: string;
  integrationStatus?: string;
  lastSyncedAt?: string;
}

/**
 * State for the conversations hook
 */
export interface ConversationsState {
  conversations: ConversationPreview[];
  isLoading: boolean;
  error: string | null;
  selectedId: string | null;
}

/**
 * Participant info for conversation header
 */
export interface ConversationParticipant {
  id: string;
  name: string;
  avatar?: string;
  isOnline?: boolean;
  role: 'client' | 'provider';
}
