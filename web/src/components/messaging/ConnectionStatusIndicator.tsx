/**
 * ConnectionStatusIndicator - Shows real-time connection status
 */

import React from 'react';
import { Wifi, WifiOff, AlertCircle, RotateCcw } from 'lucide-react';
import { ConnectionState } from '../../types/messaging';

interface ConnectionStatusIndicatorProps {
  connectionState: ConnectionState;
}

export const ConnectionStatusIndicator: React.FC<ConnectionStatusIndicatorProps> = ({
  connectionState
}) => {
  const getStatusDisplay = () => {
    switch (connectionState.status) {
      case 'connected':
        return {
          icon: <Wifi className="h-3 w-3 text-success" />,
          text: 'Connected',
          className: 'text-success bg-success/10 border-success/20'
        };

      case 'connecting':
        return {
          icon: <RotateCcw className="h-3 w-3 text-warning animate-spin" />,
          text: 'Connecting...',
          className: 'text-warning bg-warning/10 border-warning/20'
        };

      case 'reconnecting':
        return {
          icon: <RotateCcw className="h-3 w-3 text-warning animate-spin" />,
          text: `Reconnecting... (${connectionState.reconnectAttempts})`,
          className: 'text-warning bg-warning/10 border-warning/20'
        };

      case 'disconnected':
        return {
          icon: <WifiOff className="h-3 w-3 text-muted-foreground" />,
          text: 'Disconnected',
          className: 'text-muted-foreground bg-muted border-border'
        };

      case 'error':
        return {
          icon: <AlertCircle className="h-3 w-3 text-destructive" />,
          text: connectionState.error || 'Connection error',
          className: 'text-destructive bg-destructive/10 border-destructive/20'
        };

      default:
        return {
          icon: <WifiOff className="h-3 w-3 text-muted-foreground" />,
          text: 'Unknown status',
          className: 'text-muted-foreground bg-muted border-border'
        };
    }
  };

  const { icon, text, className } = getStatusDisplay();

  // Don't show indicator when connected (keep UI clean)
  if (connectionState.status === 'connected') {
    return null;
  }

  return (
    <div className={`inline-flex items-center space-x-1 px-2 py-1 rounded-full text-xs font-medium border ${className}`}>
      {icon}
      <span className="hidden sm:inline">{text}</span>
    </div>
  );
};