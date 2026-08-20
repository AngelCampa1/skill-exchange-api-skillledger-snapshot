/**
 * TypingIndicators - Shows when users are typing in the conversation
 */

import React from 'react';
import { TypingIndicator } from '../../types/messaging';

interface TypingIndicatorsProps {
  typingUsers: TypingIndicator[];
}

export const TypingIndicators: React.FC<TypingIndicatorsProps> = ({
  typingUsers
}) => {
  const activeTypingUsers = typingUsers.filter(user => user.isActive);
  
  if (activeTypingUsers.length === 0) {
    return null;
  }

  const renderTypingText = () => {
    if (activeTypingUsers.length === 1) {
      return `${activeTypingUsers[0].userName} is typing...`;
    } else if (activeTypingUsers.length === 2) {
      return `${activeTypingUsers[0].userName} and ${activeTypingUsers[1].userName} are typing...`;
    } else {
      return `${activeTypingUsers.length} people are typing...`;
    }
  };

  return (
    <div className="flex items-center space-x-3 px-4 py-2 text-sm text-muted-foreground">
      <div className="flex items-center space-x-2">
        {/* Typing animation dots */}
        <div className="flex items-center space-x-1">
          <div
            className="w-2 h-2 bg-muted-foreground/60 rounded-full animate-bounce"
            style={{ animationDelay: '0ms' }}
          />
          <div
            className="w-2 h-2 bg-muted-foreground/60 rounded-full animate-bounce"
            style={{ animationDelay: '150ms' }}
          />
          <div
            className="w-2 h-2 bg-muted-foreground/60 rounded-full animate-bounce"
            style={{ animationDelay: '300ms' }}
          />
        </div>
        <span className="italic">{renderTypingText()}</span>
      </div>
    </div>
  );
};