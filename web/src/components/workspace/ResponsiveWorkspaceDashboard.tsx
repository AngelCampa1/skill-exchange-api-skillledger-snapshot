import React, { useState, useEffect } from 'react';
import { WorkspaceDashboard } from './WorkspaceDashboard';
import { MobileWorkspaceDashboard } from './MobileWorkspaceDashboard';
import { AUTH_CONFIG } from '../../constants/auth';
import { useIsMobile } from '../../hooks/useMediaQuery'; // BUG-FE-002 FIX: Use shared hook

interface WorkspaceData {
  workspaceId: string;
  projectTitle: string;
  projectDescription: string;
  clientName: string;
  providerName: string;
  status: 'Active' | 'Archived' | 'Deleted';
  createdAt: string;
  archivedAt?: string;
  timelineData?: string;
  milestoneData?: string;
  integrationStatus?: string;
  lastSyncedAt?: string;
}

interface ResponsiveWorkspaceDashboardProps {
  workspaceId: string;
  currentUserId: string;
  isClient: boolean;
}

// BUG-FE-002 FIX: Removed local useMediaQuery implementation with memory leak
// The local implementation had 'matches' in dependency array causing unnecessary re-renders
// Now using the properly implemented shared hook from hooks/useMediaQuery.ts

export const ResponsiveWorkspaceDashboard: React.FC<ResponsiveWorkspaceDashboardProps> = ({
  workspaceId,
  currentUserId,
  isClient
}) => {
  const [workspaceData, setWorkspaceData] = useState<WorkspaceData | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const isMobile = useIsMobile(); // BUG-FE-002 FIX: Use shared hook instead of local implementation

  const fetchWorkspaceData = async () => {
    try {
      setLoading(true);
      const response = await fetch(`/api/workspace/${workspaceId}`, {
        credentials: AUTH_CONFIG.CREDENTIALS,
        headers: {
          // BUG-FE-002 FIX: Removed - Auth via httpOnly cookies,
          'Content-Type': 'application/json',
        },
      });

      if (response.ok) {
        const data = await response.json();
        setWorkspaceData(data);
      } else {
        setError('Failed to load workspace data');
      }
    } catch (err) {
      setError('An error occurred while loading workspace data');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchWorkspaceData();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [workspaceId]);

  const handleArchiveWorkspace = async () => {
    if (!confirm('Are you sure you want to archive this workspace?')) return;

    try {
      const response = await fetch(`/api/workspace/${workspaceId}/archive`, {
        method: 'POST',
        credentials: AUTH_CONFIG.CREDENTIALS,
        headers: {
          // BUG-FE-002 FIX: Removed - Auth via httpOnly cookies,
          'Content-Type': 'application/json',
        },
      });

      if (response.ok) {
        await fetchWorkspaceData(); // Refresh data
      } else {
        setError('Failed to archive workspace');
      }
    } catch (err) {
      setError('An error occurred while archiving workspace');
    }
  };

  const handleUpdateTimeline = async (timelineData: object) => {
    try {
      const response = await fetch(`/api/workspace/${workspaceId}/timeline`, {
        method: 'PUT',
        credentials: AUTH_CONFIG.CREDENTIALS,
        headers: {
          // BUG-FE-002 FIX: Removed - Auth via httpOnly cookies,
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({ timelineData }),
      });

      if (response.ok) {
        await fetchWorkspaceData(); // Refresh data
      } else {
        setError('Failed to update timeline');
      }
    } catch (err) {
      setError('An error occurred while updating timeline');
    }
  };

  if (loading) {
    return (
      <div className="flex items-center justify-center min-h-screen">
        <div className="text-lg">Loading workspace...</div>
      </div>
    );
  }

  if (error) {
    return (
      <div className="flex items-center justify-center min-h-screen">
        <div className="text-center">
          <div className="text-destructive mb-4">{error}</div>
          <button
            onClick={fetchWorkspaceData}
            className="px-4 py-2 bg-primary text-primary-foreground rounded hover:bg-primary/90"
          >
            Retry
          </button>
        </div>
      </div>
    );
  }

  if (!workspaceData) {
    return (
      <div className="flex items-center justify-center min-h-screen">
        <div className="text-lg">Workspace not found</div>
      </div>
    );
  }

  if (isMobile) {
    return (
      <MobileWorkspaceDashboard
        workspaceData={workspaceData}
        isClient={isClient}
        onArchiveWorkspace={handleArchiveWorkspace}
        onUpdateTimeline={handleUpdateTimeline}
      />
    );
  }

  return (
    <WorkspaceDashboard
      workspaceId={workspaceId}
      currentUserId={currentUserId}
      isClient={isClient}
    />
  );
};