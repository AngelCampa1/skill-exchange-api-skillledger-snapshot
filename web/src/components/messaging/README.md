# Messaging System Implementation

This directory contains a complete real-time messaging system built for SkillLedger workspaces using React, TypeScript, and SignalR.

## 🚀 Features

### Core Messaging
- ✅ Real-time messaging with SignalR
- ✅ Text, file, image, voice, and system messages
- ✅ Message editing and deletion
- ✅ Reply threading with contextual display
- ✅ Message status indicators (sent, delivered, read)
- ✅ Auto-scroll to new messages

### Real-time Features
- ✅ Live typing indicators
- ✅ Connection status monitoring
- ✅ Automatic reconnection with exponential backoff
- ✅ Online/offline state management
- ✅ Real-time message delivery

### File & Media
- ✅ Drag-and-drop file upload
- ✅ Image preview and full-screen view
- ✅ File download functionality
- ✅ Voice message recording and playback
- ✅ Upload progress tracking

### Interactive Elements
- ✅ Emoji reactions with picker
- ✅ Full-text message search with highlighting
- ✅ Message pagination and history loading
- ✅ Rich emoji picker integration

### User Experience
- ✅ Responsive design (mobile & desktop)
- ✅ Toast notifications for new messages
- ✅ Browser notification support
- ✅ Participant online status
- ✅ Message grouping by time and sender

## 📁 File Structure

```
messaging/
├── index.ts                    # Exports all components
├── MessageCenter.tsx           # Main messaging interface
├── MessageList.tsx            # Message list with grouping
├── MessageItem.tsx            # Individual message component
├── MessageInput.tsx           # Message input with file upload
├── TypingIndicators.tsx       # Real-time typing indicators
├── ConnectionStatusIndicator.tsx # Connection status display
├── EmojiReactions.tsx         # Message reactions UI
├── MessageSearch.tsx          # Full-text search interface
├── MessageNotifications.tsx   # Toast notifications & browser notifications
└── __tests__/                 # Test files
    ├── MessageCenter.test.tsx
    └── MessageInput.test.tsx
```

## 🔧 Services

```
services/
├── signalRService.ts          # SignalR connection management
└── messagingApiService.ts     # HTTP API calls
```

## 🎯 Usage

### Basic Implementation

```tsx
import { MessageCenter } from './components/messaging';

function WorkspacePage() {
  return (
    <MessageCenter
      workspaceId="workspace-123"
      currentUserId="user-456" 
      workspaceTitle="Project Alpha"
      participants={[
        { id: 'user-123', name: 'John Doe', avatar: '/avatar1.jpg', isOnline: true },
        { id: 'user-456', name: 'Jane Smith', avatar: '/avatar2.jpg', isOnline: false }
      ]}
    />
  );
}
```

### With Notifications

```tsx
import { WorkspaceMessaging } from './components/workspace/WorkspaceMessaging';

function App() {
  return (
    <WorkspaceMessaging
      workspaceId="workspace-123"
      currentUserId="user-456"
      workspaceTitle="Project Alpha"
    />
  );
}
```

## 🔌 SignalR Integration

The system connects to `/api/hubs/messaging` with the following events:

**Client → Server**
- `JoinWorkspace(workspaceId)`
- `LeaveWorkspace(workspaceId)`
- `SendTypingIndicator(workspaceId)`
- `StopTypingIndicator(workspaceId)`
- `MarkMessageAsRead(messageId)`

**Server → Client**
- `MessageReceived(message)`
- `MessageUpdated(message)`
- `MessageDeleted(messageId)`
- `ReactionAdded(messageId, reaction)`
- `ReactionRemoved(messageId, userId, emoji)`
- `UserStartedTyping(workspaceId, user)`
- `UserStoppedTyping(workspaceId, userId)`
- `MessageRead(messageId, userId, readAt)`
- `UserJoined(workspaceId, userId, userName)`
- `UserLeft(workspaceId, userId, userName)`

## 🌐 API Endpoints

The messaging system uses these HTTP endpoints:

- `POST /api/messaging/send` - Send new message
- `PUT /api/messaging/{id}/edit` - Edit message
- `DELETE /api/messaging/{id}` - Delete message
- `GET /api/messaging/history` - Get message history
- `GET /api/messaging/search` - Search messages
- `POST /api/messaging/{id}/reactions` - Add reaction
- `DELETE /api/messaging/{id}/reactions/{emoji}` - Remove reaction
- `POST /api/messaging/{id}/read` - Mark as read
- `POST /api/messaging/upload` - Upload files

## 🎨 UI Components

### MessageCenter
Main container component that orchestrates all messaging functionality.

**Props:**
- `workspaceId: string` - Unique workspace identifier
- `currentUserId: string` - Current user ID
- `workspaceTitle: string` - Display title
- `participants: Participant[]` - Workspace participants
- `className?: string` - Additional CSS classes

### MessageInput
Rich input component with file upload and emoji support.

**Features:**
- Auto-resizing textarea
- Drag-and-drop file upload
- Voice message recording
- Emoji picker integration
- Typing indicators
- Reply threading

### MessageItem
Individual message display with reactions and actions.

**Supports:**
- Text messages with editing
- Image attachments with preview
- File attachments with download
- Voice messages with playback
- System and milestone messages
- Emoji reactions
- Message threading (replies)

## 📱 Responsive Design

The messaging interface adapts to different screen sizes:

- **Desktop**: Full-featured interface with sidebar panels
- **Tablet**: Optimized layout with collapsible panels  
- **Mobile**: Streamlined interface with touch-friendly controls

## 🔔 Notifications

### Toast Notifications
- Appear for new messages from other users
- Show message preview and sender
- Auto-dismiss after 5 seconds
- Click to navigate to message

### Browser Notifications
- Requests permission on first use
- Shows when tab is not active
- Includes sender avatar and message preview

## 🧪 Testing

Comprehensive test coverage includes:
- Component rendering and interactions
- SignalR event handling
- File upload functionality
- Message sending and editing
- Search functionality
- Connection state management

Run tests:
```bash
npm test -- --testPathPattern=messaging
```

## 🎛️ Configuration

### Environment Variables
- Authentication tokens handled via `localStorage.getItem('token')`
- SignalR endpoint: `/api/hubs/messaging`
- File upload endpoint: `/api/messaging/upload`

### Customization
- Update UI components in `/components/ui/` for consistent styling
- Modify SignalR connection settings in `signalRService.ts`
- Adjust API endpoints in `messagingApiService.ts`

## 🔐 Security

- JWT token authentication for all requests
- Rate limiting on message sending
- File upload validation and size limits
- XSS protection with proper content sanitization
- CSRF protection on API endpoints

## 🚀 Performance

- Message virtualization for large conversations
- Image lazy loading and optimization
- Debounced search with caching
- Connection pooling and keep-alive
- Optimized re-rendering with React.memo

## 🔄 State Management

The messaging system uses React hooks for state management:
- `useState` for component state
- `useEffect` for side effects and cleanup
- `useCallback` for memoized functions
- `useRef` for DOM references and timers
- Custom hooks for notifications and SignalR

## 📚 Dependencies

- **@microsoft/signalr**: Real-time communication
- **emoji-picker-react**: Emoji selection interface
- **date-fns**: Date formatting and manipulation
- **react-dropzone**: File drag-and-drop functionality
- **lucide-react**: Icon library

## 🤝 Integration

This messaging system integrates with:
- SkillLedger workspace system
- User authentication and permissions
- File storage and management
- Notification systems
- Audit logging

## 📈 Future Enhancements

Potential improvements for future development:
- Message threading with nested replies
- Video calling integration
- Screen sharing capabilities
- Message translation
- Advanced file previews
- Message scheduling
- Custom emoji reactions
- Message templates