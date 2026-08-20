import { logger } from '@/utils/logger';
import React, { useState, useEffect } from 'react';
import { useRouter } from 'next/navigation';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Loader2, MessageSquare, Folder, DollarSign, Archive } from 'lucide-react';
import { AUTH_CONFIG } from '../../constants/auth';

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

interface WorkspaceDashboardProps {
  workspaceId: string;
  currentUserId: string;
  isClient: boolean;
}

export const WorkspaceDashboard: React.FC<WorkspaceDashboardProps> = ({
  workspaceId,
  currentUserId,
  isClient
}) => {
  const router = useRouter();
  const [workspaceData, setWorkspaceData] = useState<WorkspaceData | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  /**
   * SECURITY FIX: Safe JSON.parse wrapper to prevent application crashes
   * from malformed JSON data returned by API
   */
  const safeJsonParse = (jsonString: string, fieldName: string): unknown => {
    try {
      return JSON.parse(jsonString);
    } catch (error) {
      logger.error(`Failed to parse ${fieldName} JSON`, { error, jsonString });
      return null;
    }
  };

  const fetchWorkspaceData = async () => {
    try {
      setLoading(true);
      setError(null); // Clear any previous error before retrying
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

  const getStatusBadge = (status: string) => {
    switch (status) {
      case 'Active':
        return <Badge variant="success">Active</Badge>;
      case 'Archived':
        return <Badge variant="secondary">Archived</Badge>;
      case 'Deleted':
        return <Badge variant="destructive">Deleted</Badge>;
      default:
        return <Badge variant="default">{status}</Badge>;
    }
  };

  if (loading) {
    return (
      <div className="flex items-center justify-center min-h-96">
        <div className="flex flex-col items-center space-y-4">
          <Loader2 className="h-8 w-8 animate-spin text-primary" />
          <div className="text-lg text-muted-foreground">Loading workspace...</div>
        </div>
      </div>
    );
  }

  if (error) {
    return (
      <div className="flex items-center justify-center min-h-96">
        <Card className="w-full max-w-md">
          <CardContent className="p-6">
            <div className="text-center text-destructive mb-4">{error}</div>
            <Button 
              onClick={fetchWorkspaceData} 
              className="w-full"
              variant="outline"
            >
              Retry
            </Button>
          </CardContent>
        </Card>
      </div>
    );
  }

  if (!workspaceData) {
    return (
      <div className="flex items-center justify-center min-h-96">
        <div className="text-lg text-muted-foreground">Workspace not found</div>
      </div>
    );
  }

  return (
    <div className="container mx-auto px-4 py-8 max-w-6xl">
      {/* Header */}
      <div className="mb-8">
        <div className="flex items-center justify-between mb-4">
          <h1 className="text-3xl font-bold">{workspaceData.projectTitle}</h1>
          {getStatusBadge(workspaceData.status)}
        </div>
        <p className="text-muted-foreground mb-4">{workspaceData.projectDescription}</p>
        <div className="flex items-center space-x-6 text-sm text-muted-foreground">
          <span>Client: <strong>{workspaceData.clientName}</strong></span>
          <span>Provider: <strong>{workspaceData.providerName}</strong></span>
          <span>Created: {new Date(workspaceData.createdAt).toLocaleDateString()}</span>
        </div>
      </div>

      {/* Main Content Grid */}
      <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
        {/* Project Overview */}
        <Card className="lg:col-span-2">
          <CardHeader>
            <CardTitle>Project Overview</CardTitle>
          </CardHeader>
          <CardContent>
            <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
              {/* Timeline */}
              <div>
                <h3 className="text-lg font-semibold mb-3">Timeline</h3>
                {workspaceData.timelineData ? (
                  <div className="bg-muted p-4 rounded-lg">
                    <pre className="text-sm whitespace-pre-wrap">
                      {JSON.stringify(safeJsonParse(workspaceData.timelineData, 'timelineData') || {}, null, 2)}
                    </pre>
                  </div>
                ) : (
                  <div className="text-muted-foreground italic">No timeline data available</div>
                )}
                <Button
                  onClick={() => handleUpdateTimeline({ milestones: ['Milestone 1', 'Milestone 2'] })}
                  variant="outline"
                  size="sm"
                  className="mt-2"
                >
                  Update Timeline
                </Button>
              </div>

              {/* Milestones */}
              <div>
                <h3 className="text-lg font-semibold mb-3">Milestones</h3>
                {workspaceData.milestoneData ? (
                  <div className="bg-muted p-4 rounded-lg">
                    <pre className="text-sm whitespace-pre-wrap">
                      {JSON.stringify(safeJsonParse(workspaceData.milestoneData, 'milestoneData') || {}, null, 2)}
                    </pre>
                  </div>
                ) : (
                  <div className="text-muted-foreground italic">No milestone data available</div>
                )}
              </div>
            </div>
          </CardContent>
        </Card>

        {/* Sidebar */}
        <div className="space-y-6">
          {/* Integration Status */}
          <Card>
            <CardHeader>
              <CardTitle>Integration Status</CardTitle>
            </CardHeader>
            <CardContent>
              <div className="space-y-2">
                <div className="flex items-center justify-between">
                  <span>Status:</span>
                  <Badge variant={workspaceData.integrationStatus === 'initialized' ? 'success' : 'warning'}>
                    {workspaceData.integrationStatus || 'Unknown'}
                  </Badge>
                </div>
                {workspaceData.lastSyncedAt && (
                  <div className="flex items-center justify-between text-sm text-muted-foreground">
                    <span>Last Sync:</span>
                    <span>{new Date(workspaceData.lastSyncedAt).toLocaleString()}</span>
                  </div>
                )}
              </div>
            </CardContent>
          </Card>

          {/* Quick Actions */}
          <Card>
            <CardHeader>
              <CardTitle>Quick Actions</CardTitle>
            </CardHeader>
            <CardContent className="space-y-3">
              <Button
                variant="outline"
                className="w-full"
                onClick={() => router.push(`/workspace/${workspaceId}/messages`)}
              >
                <MessageSquare className="w-4 h-4 mr-2" />
                Messages
              </Button>
              <Button
                variant="outline"
                className="w-full"
                onClick={() => router.push(`/workspace/${workspaceId}/files`)}
              >
                <Folder className="w-4 h-4 mr-2" />
                Files
              </Button>
              <Button
                variant="outline"
                className="w-full"
                onClick={() => router.push(`/workspace/${workspaceId}/escrow`)}
              >
                <DollarSign className="w-4 h-4 mr-2" />
                Escrow
              </Button>
              {isClient && workspaceData.status === 'Active' && (
                <Button 
                  variant="destructive" 
                  className="w-full"
                  onClick={handleArchiveWorkspace}
                >
                  <Archive className="w-4 h-4 mr-2" />
                  Archive Workspace
                </Button>
              )}
            </CardContent>
          </Card>
        </div>
      </div>
    </div>
  );
};