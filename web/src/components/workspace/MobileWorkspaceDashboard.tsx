import { logger } from '@/utils/logger';
import React from 'react';
import { Card, CardContent } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';

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

interface MobileWorkspaceDashboardProps {
  workspaceData: WorkspaceData;
  isClient: boolean;
  onArchiveWorkspace: () => void;
  onUpdateTimeline: (data: object) => void;
}

export const MobileWorkspaceDashboard: React.FC<MobileWorkspaceDashboardProps> = ({
  workspaceData,
  isClient,
  onArchiveWorkspace,
  onUpdateTimeline
}) => {
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

  return (
    <div className="container mx-auto px-4 py-6 max-w-md">
      {/* Header */}
      <div className="mb-6">
        <div className="flex items-center justify-between mb-3">
          <h1 className="text-xl font-bold truncate flex-1 mr-2">{workspaceData.projectTitle}</h1>
          {getStatusBadge(workspaceData.status)}
        </div>
        <p className="text-muted-foreground text-sm mb-3 line-clamp-2">{workspaceData.projectDescription}</p>
        <div className="space-y-1 text-xs text-muted-foreground">
          <div>Client: <strong>{workspaceData.clientName}</strong></div>
          <div>Provider: <strong>{workspaceData.providerName}</strong></div>
          <div>Created: {new Date(workspaceData.createdAt).toLocaleDateString()}</div>
        </div>
      </div>

      {/* Quick Actions */}
      <Card className="mb-6">
        <CardContent className="p-4">
          <h3 className="text-lg font-semibold mb-3">Quick Actions</h3>
          <div className="grid grid-cols-1 gap-2">
            <Button 
              variant="outline" 
              size="sm"
              className="w-full justify-start"
              onClick={() => logger.debug('Navigate to messages', { component: 'MobileWorkspaceDashboard' })}
            >
              💬 Messages
            </Button>
            <Button 
              variant="outline" 
              size="sm"
              className="w-full justify-start"
              onClick={() => logger.debug('Navigate to files', { component: 'MobileWorkspaceDashboard' })}
            >
              📁 Files
            </Button>
            <Button 
              variant="outline" 
              size="sm"
              className="w-full justify-start"
              onClick={() => logger.debug('Navigate to escrow', { component: 'MobileWorkspaceDashboard' })}
            >
              💰 Escrow
            </Button>
          </div>
        </CardContent>
      </Card>

      {/* Integration Status */}
      <Card className="mb-6">
        <CardContent className="p-4">
          <h3 className="text-lg font-semibold mb-3">Integration Status</h3>
          <div className="space-y-2">
            <div className="flex items-center justify-between">
              <span className="text-sm">Status:</span>
              <Badge 
                variant={workspaceData.integrationStatus === 'initialized' ? 'success' : 'warning'}
                className="text-xs"
              >
                {workspaceData.integrationStatus || 'Unknown'}
              </Badge>
            </div>
            {workspaceData.lastSyncedAt && (
              <div className="text-xs text-muted-foreground">
                Last Sync: {new Date(workspaceData.lastSyncedAt).toLocaleString()}
              </div>
            )}
          </div>
        </CardContent>
      </Card>

      {/* Timeline */}
      <Card className="mb-6">
        <CardContent className="p-4">
          <h3 className="text-lg font-semibold mb-3">Timeline</h3>
          {workspaceData.timelineData ? (
            <div className="bg-muted p-3 rounded text-xs">
              <pre className="whitespace-pre-wrap break-words">
                {JSON.stringify(safeJsonParse(workspaceData.timelineData, 'timelineData') || {}, null, 2)}
              </pre>
            </div>
          ) : (
            <div className="text-muted-foreground italic text-sm">No timeline data available</div>
          )}
          <Button
            onClick={() => onUpdateTimeline({ milestones: ['Mobile Milestone 1', 'Mobile Milestone 2'] })}
            variant="outline"
            size="sm"
            className="w-full mt-3"
          >
            Update Timeline
          </Button>
        </CardContent>
      </Card>

      {/* Milestones */}
      <Card className="mb-6">
        <CardContent className="p-4">
          <h3 className="text-lg font-semibold mb-3">Milestones</h3>
          {workspaceData.milestoneData ? (
            <div className="bg-muted p-3 rounded text-xs">
              <pre className="whitespace-pre-wrap break-words">
                {JSON.stringify(safeJsonParse(workspaceData.milestoneData, 'milestoneData') || {}, null, 2)}
              </pre>
            </div>
          ) : (
            <div className="text-muted-foreground italic text-sm">No milestone data available</div>
          )}
        </CardContent>
      </Card>

      {/* Archive Action */}
      {isClient && workspaceData.status === 'Active' && (
        <Card>
          <CardContent className="p-4">
            <h3 className="text-lg font-semibold mb-3 text-destructive">Danger Zone</h3>
            <Button
              variant="destructive"
              size="sm"
              className="w-full"
              onClick={onArchiveWorkspace}
            >
              Archive Workspace
            </Button>
            <p className="text-xs text-muted-foreground mt-2">
              This action cannot be undone. The workspace will be archived and no longer accessible.
            </p>
          </CardContent>
        </Card>
      )}
    </div>
  );
};