# US-4.2.1: Real-time Messaging & Communication

## 📋 User Story
**As a** project participant  
**I want** to communicate with my collaborator in real-time within the workspace  
**So that** we can coordinate effectively and maintain a record of all project discussions

## ✅ Acceptance Criteria
- [ ] Real-time chat with typing indicators and read receipts
- [ ] Message history with full-text search capabilities
- [ ] File sharing through drag-and-drop interface
- [ ] Emoji reactions and message threading for better organization
- [ ] Notification system for new messages and mentions
- [ ] Message encryption for sensitive project discussions

## 🏗️ Technical Architecture
- **Real-time Engine**: SignalR for instant message delivery
- **Message Storage**: Encrypted message history with full indexing
- **Notification System**: Push notifications across all devices
- **Security**: End-to-end encryption with forward secrecy

## 🗄️ Database Schema
```sql
-- Workspace messages
CREATE TABLE WorkspaceMessages (
    Id UNIQUEIDENTIFIER PRIMARY KEY,
    WorkspaceId UNIQUEIDENTIFIER REFERENCES ProjectWorkspaces(Id),
    SenderId UNIQUEIDENTIFIER REFERENCES Users(Id),
    MessageText NVARCHAR(MAX),
    MessageType INT DEFAULT 0, -- Text, File, System, Milestone
    AttachmentUrl NVARCHAR(500),
    IsEdited BIT DEFAULT 0,
    CreatedAt DATETIME2 DEFAULT GETUTCDATE(),
    EditedAt DATETIME2
);
```

## 🔗 Related Stories
- **Depends on**: US-4.1.1 Project Workspace Creation (requires workspace)
- **Next**: US-4.3.1 Milestone & Deliverable Tracking (adds structure to communication)

## 📊 Implementation Status
- 🔴 **Not Started**
- **Estimated Points**: 13
- **Priority**: 🟠 High