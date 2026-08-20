import { logger } from '@/utils/logger';
import React, { useState, useEffect, useCallback } from 'react';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Progress } from '@/components/ui/progress';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs';
import { 
  Calendar,
  Clock,
  CheckCircle2,
  AlertCircle,
  Plus,
  FileText,
  User,
  Star
} from 'lucide-react';
import { 
  ProjectMilestone, 
  ProjectProgress, 
  MilestoneStatus, 
  MilestonePriority,
  MilestoneFilter,
  PaginatedMilestones,
  MilestoneUpdate
} from '@/types/milestone';
import { formatDistanceToNow, format, isPast } from 'date-fns';
import { AUTH_CONFIG } from '../../constants/auth';

interface MilestoneTrackerProps {
  projectId: string;
  userRole: 'client' | 'provider';
  currentUserId: string;
}

export const MilestoneTracker: React.FC<MilestoneTrackerProps> = ({
  projectId,
  userRole,
  currentUserId
}) => {
  const [milestones, setMilestones] = useState<ProjectMilestone[]>([]);
  const [projectProgress, setProjectProgress] = useState<ProjectProgress | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [activeTab, setActiveTab] = useState('overview');
  const [filter, setFilter] = useState<MilestoneFilter>({ projectId });

  // Real-time milestone update listener
  // NOTE: Currently uses window events as a placeholder. Data fetching is real (via /api/milestone).
  // TODO: Replace window events with actual SignalR connection when milestone hub is implemented.
  useEffect(() => {
    const connectToMilestoneHub = async () => {
      try {
        logger.debug('Connecting to milestone tracking', { projectId, component: 'MilestoneTracker' });

        // Handle real-time milestone updates (currently via window events, to be replaced with SignalR)
        const handleMilestoneUpdate = (update: MilestoneUpdate) => {
          if (update.projectId === projectId) {
            loadMilestones();
            loadProjectProgress();
          }
        };

        // Set up event listeners (placeholder for SignalR events)
        window.addEventListener('milestone-updated', handleMilestoneUpdate as any);
        
        return () => {
          window.removeEventListener('milestone-updated', handleMilestoneUpdate as any);
        };
      } catch (error) {
        logger.error('Failed to connect to milestone hub', error, { component: 'MilestoneTracker' });
        return undefined;
      }
    };

    let cleanup: (() => void) | undefined;
    connectToMilestoneHub().then(cleanupFn => {
      cleanup = cleanupFn;
    }).catch((error: unknown) => {
      logger.error('Failed to connect to milestone hub', error, { context: 'MilestoneTracker' });
    });

    return () => {
      if (cleanup) {
        cleanup();
      }
    };
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [projectId]);

  const loadMilestones = useCallback(async () => {
    try {
      setLoading(true);
      const response = await fetch(`/api/milestone?projectId=${projectId}`, {
        credentials: AUTH_CONFIG.CREDENTIALS,
        headers: {
          // BUG-FE-002 FIX: Removed - Auth via httpOnly cookies,
          'Content-Type': 'application/json',
        },
      });

      if (response.ok) {
        const data: PaginatedMilestones = await response.json();
        setMilestones(data.items);
      } else {
        setError('Failed to load milestones');
      }
    } catch (err) {
      setError('Network error loading milestones');
    } finally {
      setLoading(false);
    }
  }, [projectId]);

  const loadProjectProgress = useCallback(async () => {
    try {
      const response = await fetch(`/api/milestone/projects/${projectId}/progress`, {
        credentials: AUTH_CONFIG.CREDENTIALS,
        headers: {
          // BUG-FE-002 FIX: Removed - Auth via httpOnly cookies,
          'Content-Type': 'application/json',
        },
      });

      if (response.ok) {
        const progress: ProjectProgress = await response.json();
        setProjectProgress(progress);
      }
    } catch (err) {
      logger.error('Failed to load project progress', err, { component: 'MilestoneTracker' });
    }
  }, [projectId]);

  useEffect(() => {
    loadMilestones();
    loadProjectProgress();
  }, [loadMilestones, loadProjectProgress]);

  const getStatusColor = (status: MilestoneStatus) => {
    switch (status) {
      case MilestoneStatus.NotStarted:
        return 'bg-muted text-foreground';
      case MilestoneStatus.InProgress:
        return 'bg-primary/10 text-primary';
      case MilestoneStatus.PendingReview:
        return 'bg-warning/10 text-warning';
      case MilestoneStatus.Completed:
        return 'bg-success/10 text-success';
      case MilestoneStatus.Cancelled:
        return 'bg-destructive/10 text-destructive';
      default:
        return 'bg-muted text-foreground';
    }
  };

  const getPriorityColor = (priority: MilestonePriority) => {
    switch (priority) {
      case MilestonePriority.Low:
        return 'bg-muted text-muted-foreground';
      case MilestonePriority.Medium:
        return 'bg-primary/10 text-primary';
      case MilestonePriority.High:
        return 'bg-warning/10 text-warning';
      case MilestonePriority.Critical:
        return 'bg-destructive/10 text-destructive';
      default:
        return 'bg-muted text-muted-foreground';
    }
  };

  const handleMilestoneAction = async (milestoneId: string, action: string) => {
    try {
      const response = await fetch(`/api/milestone/${milestoneId}/${action}`, {
        method: 'POST',
        credentials: AUTH_CONFIG.CREDENTIALS,
        headers: {
          // BUG-FE-002 FIX: Removed - Auth via httpOnly cookies,
          'Content-Type': 'application/json',
        },
      });

      if (response.ok) {
        await loadMilestones();
        await loadProjectProgress();
      } else {
        const errorData = await response.json();
        setError(errorData.message || `Failed to ${action} milestone`);
      }
    } catch (err) {
      setError(`Network error during ${action}`);
    }
  };

  if (loading) {
    return (
      <Card>
        <CardContent className="flex items-center justify-center p-6">
          <div className="flex items-center space-x-2">
            <div className="animate-spin rounded-full h-4 w-4 border-b-2 border-primary"></div>
            <span>Loading milestones...</span>
          </div>
        </CardContent>
      </Card>
    );
  }

  if (error) {
    return (
      <Card>
        <CardContent className="p-6">
          <div className="flex items-center space-x-2 text-destructive">
            <AlertCircle className="h-4 w-4" />
            <span>{error}</span>
          </div>
        </CardContent>
      </Card>
    );
  }

  return (
    <div className="space-y-6">
      {/* Project Progress Overview */}
      {projectProgress && (
        <Card>
          <CardHeader>
            <CardTitle className="flex items-center justify-between">
              <span>Project Progress</span>
              <Badge variant="outline">
                {projectProgress.completedMilestones} of {projectProgress.totalMilestones} completed
              </Badge>
            </CardTitle>
          </CardHeader>
          <CardContent>
            <div className="space-y-4">
              <div className="flex items-center justify-between text-sm">
                <span>Overall Progress</span>
                <span className="font-medium">{Math.round(projectProgress.overallProgressPercentage)}%</span>
              </div>
              <Progress value={projectProgress.overallProgressPercentage} className="h-2" />
              
              <div className="grid grid-cols-2 md:grid-cols-4 gap-4 mt-4">
                <div className="text-center">
                  <div className="text-2xl font-bold text-primary">{projectProgress.inProgressMilestones}</div>
                  <div className="text-xs text-muted-foreground">In Progress</div>
                </div>
                <div className="text-center">
                  <div className="text-2xl font-bold text-success">{projectProgress.completedMilestones}</div>
                  <div className="text-xs text-muted-foreground">Completed</div>
                </div>
                <div className="text-center">
                  <div className="text-2xl font-bold text-destructive">{projectProgress.overdueMilestones}</div>
                  <div className="text-xs text-muted-foreground">Overdue</div>
                </div>
                <div className="text-center">
                  <div className="text-2xl font-bold text-foreground">{projectProgress.totalMilestones}</div>
                  <div className="text-xs text-muted-foreground">Total</div>
                </div>
              </div>
            </div>
          </CardContent>
        </Card>
      )}

      {/* Milestone Tabs */}
      <Tabs value={activeTab} onValueChange={setActiveTab}>
        <TabsList className="grid w-full grid-cols-4">
          <TabsTrigger value="overview">Overview</TabsTrigger>
          <TabsTrigger value="active">Active</TabsTrigger>
          <TabsTrigger value="completed">Completed</TabsTrigger>
          <TabsTrigger value="upcoming">Upcoming</TabsTrigger>
        </TabsList>

        <TabsContent value="overview" className="space-y-4">
          <div className="flex items-center justify-between">
            <h3 className="text-lg font-semibold">All Milestones</h3>
            {userRole === 'client' && (
              <Button size="sm" className="flex items-center space-x-2">
                <Plus className="h-4 w-4" />
                <span>Add Milestone</span>
              </Button>
            )}
          </div>

          <div className="grid gap-4">
            {milestones.map((milestone) => (
              <MilestoneCard 
                key={milestone.id} 
                milestone={milestone} 
                userRole={userRole}
                currentUserId={currentUserId}
                onAction={handleMilestoneAction}
              />
            ))}

            {milestones.length === 0 && (
              <Card>
                <CardContent className="text-center py-8">
                  <FileText className="h-12 w-12 text-muted-foreground mx-auto mb-4" />
                  <h3 className="text-lg font-medium text-foreground mb-2">No milestones yet</h3>
                  <p className="text-muted-foreground mb-4">Get started by creating your first project milestone</p>
                  {userRole === 'client' && (
                    <Button>
                      <Plus className="h-4 w-4 mr-2" />
                      Create First Milestone
                    </Button>
                  )}
                </CardContent>
              </Card>
            )}
          </div>
        </TabsContent>

        <TabsContent value="active" className="space-y-4">
          <div className="grid gap-4">
            {milestones
              .filter(m => m.status === MilestoneStatus.InProgress || m.status === MilestoneStatus.PendingReview)
              .map((milestone) => (
                <MilestoneCard 
                  key={milestone.id} 
                  milestone={milestone} 
                  userRole={userRole}
                  currentUserId={currentUserId}
                  onAction={handleMilestoneAction}
                />
              ))}
          </div>
        </TabsContent>

        <TabsContent value="completed" className="space-y-4">
          <div className="grid gap-4">
            {milestones
              .filter(m => m.status === MilestoneStatus.Completed)
              .map((milestone) => (
                <MilestoneCard 
                  key={milestone.id} 
                  milestone={milestone} 
                  userRole={userRole}
                  currentUserId={currentUserId}
                  onAction={handleMilestoneAction}
                />
              ))}
          </div>
        </TabsContent>

        <TabsContent value="upcoming" className="space-y-4">
          <div className="grid gap-4">
            {milestones
              .filter(m => m.dueDate && !isPast(new Date(m.dueDate)) && m.status !== MilestoneStatus.Completed)
              .sort((a, b) => new Date(a.dueDate!).getTime() - new Date(b.dueDate!).getTime())
              .map((milestone) => (
                <MilestoneCard 
                  key={milestone.id} 
                  milestone={milestone} 
                  userRole={userRole}
                  currentUserId={currentUserId}
                  onAction={handleMilestoneAction}
                />
              ))}
          </div>
        </TabsContent>
      </Tabs>
    </div>
  );
};

// Individual Milestone Card Component
interface MilestoneCardProps {
  milestone: ProjectMilestone;
  userRole: 'client' | 'provider';
  currentUserId: string;
  onAction: (milestoneId: string, action: string) => void;
}

const MilestoneCard: React.FC<MilestoneCardProps> = ({ 
  milestone, 
  userRole, 
  currentUserId, 
  onAction 
}) => {
  const isOverdue = milestone.isOverdue;
  const canUserAct = (userRole === 'client' && milestone.canBeApproved) || 
                    (userRole === 'provider' && (milestone.canBeStarted || milestone.canBeSubmitted));

  return (
    <Card className={`transition-all hover:shadow-md ${isOverdue ? 'border-destructive bg-destructive/10' : ''}`}>
      <CardHeader className="pb-3">
        <div className="flex items-start justify-between">
          <div className="space-y-1">
            <CardTitle className="text-lg">{milestone.title}</CardTitle>
            <p className="text-sm text-muted-foreground">{milestone.description}</p>
          </div>
          <div className="flex items-center space-x-2">
            <Badge className={`getPriorityColor(milestone.priority)`}>
              <Star className="h-3 w-3 mr-1" />
              {milestone.priority}
            </Badge>
            <Badge className={`getStatusColor(milestone.status)`}>
              {milestone.status}
            </Badge>
          </div>
        </div>
      </CardHeader>

      <CardContent className="space-y-4">
        {/* Progress and Timeline */}
        <div className="space-y-2">
          <div className="flex items-center justify-between text-sm">
            <span>Progress</span>
            <span className="font-medium">{Math.round(milestone.weightPercentage)}%</span>
          </div>
          <Progress value={milestone.weightPercentage} className="h-1" />
        </div>

        {/* Due Date and Assignment */}
        <div className="flex items-center justify-between text-sm text-muted-foreground">
          <div className="flex items-center space-x-4">
            {milestone.dueDate && (
              <div className={`flex items-center space-x-1 ${isOverdue ? 'text-destructive' : ''}`}>
                <Calendar className="h-4 w-4" />
                <span>
                  Due {formatDistanceToNow(new Date(milestone.dueDate), { addSuffix: true })}
                </span>
              </div>
            )}
            
            {milestone.assignedToUserName && (
              <div className="flex items-center space-x-1">
                <User className="h-4 w-4" />
                <span>Assigned to {milestone.assignedToUserName}</span>
              </div>
            )}
          </div>

          <div className="flex items-center space-x-1">
            <Clock className="h-4 w-4" />
            <span>Updated {formatDistanceToNow(new Date(milestone.updatedAt), { addSuffix: true })}</span>
          </div>
        </div>

        {/* Submissions Summary */}
        {milestone.submissions.length > 0 && (
          <div className="border-t pt-3">
            <div className="text-sm text-muted-foreground mb-2">
              {milestone.submissions.length} submission{milestone.submissions.length !== 1 ? 's' : ''}
            </div>
            <div className="flex flex-wrap gap-1">
              {milestone.submissions.map((submission) => (
                <Badge
                  key={submission.id}
                  variant="outline"
                  className={`text-xs ${submission.isReviewed ?
                    (submission.isApproved ? 'bg-success/10 text-success' : 'bg-destructive/10 text-destructive')
                    : 'bg-warning/10 text-warning'
                  }`}
                >
                  {submission.title}
                </Badge>
              ))}
            </div>
          </div>
        )}

        {/* Action Buttons */}
        {canUserAct && (
          <div className="border-t pt-3 flex space-x-2">
            {milestone.canBeStarted && userRole === 'provider' && (
              <Button 
                size="sm" 
                onClick={() => onAction(milestone.id, 'start')}
                className="flex items-center space-x-1"
              >
                <CheckCircle2 className="h-4 w-4" />
                <span>Start Work</span>
              </Button>
            )}
            
            {milestone.canBeSubmitted && userRole === 'provider' && (
              <Button 
                size="sm" 
                variant="outline" 
                onClick={() => onAction(milestone.id, 'submit')}
                className="flex items-center space-x-1"
              >
                <FileText className="h-4 w-4" />
                <span>Submit for Review</span>
              </Button>
            )}
            
            {milestone.canBeApproved && userRole === 'client' && (
              <>
                <Button 
                  size="sm" 
                  onClick={() => onAction(milestone.id, 'approve')}
                  className="flex items-center space-x-1"
                >
                  <CheckCircle2 className="h-4 w-4" />
                  <span>Approve</span>
                </Button>
                <Button 
                  size="sm" 
                  variant="outline" 
                  onClick={() => onAction(milestone.id, 'request-revisions')}
                >
                  Request Revisions
                </Button>
              </>
            )}
          </div>
        )}
      </CardContent>
    </Card>
  );
};