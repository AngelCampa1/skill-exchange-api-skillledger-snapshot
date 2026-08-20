/**
 * Mock implementation of SignalR service for testing
 */

export const mockSignalRService = {
  connect: jest.fn().mockResolvedValue(void 0),
  disconnect: jest.fn().mockResolvedValue(void 0),
  getConnectionState: jest.fn().mockReturnValue({
    status: 'connected',
    reconnectAttempts: 0
  }),
  joinWorkspace: jest.fn().mockResolvedValue(void 0),
  leaveWorkspace: jest.fn().mockResolvedValue(void 0),
  sendMessage: jest.fn().mockResolvedValue(void 0),
  startTyping: jest.fn().mockResolvedValue(void 0),
  stopTyping: jest.fn().mockResolvedValue(void 0),
  markMessageAsRead: jest.fn().mockResolvedValue(void 0),
  on: jest.fn(),
  off: jest.fn(),
  isConnected: jest.fn().mockReturnValue(true)
};

export const signalRService = mockSignalRService;