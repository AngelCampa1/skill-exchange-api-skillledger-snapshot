using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SkillLedger.Core.DTOs;
using SkillLedger.Core.Entities;
using SkillLedger.Core.Enums;
using SkillLedger.Core.Interfaces;
using SkillLedger.Infrastructure.Data;
using System.Diagnostics;

namespace SkillLedger.Infrastructure.Services
{
    /// <summary>
    /// Service for managing workspace messaging functionality
    /// </summary>
    public class MessagingService : IMessagingService
    {
        private readonly SkillLedgerDbContext _context;
        private readonly IEncryptionService _encryptionService;
        private readonly IAuditLogService _auditLogService;
        private readonly ILogger<MessagingService> _logger;

        public MessagingService(
            SkillLedgerDbContext context,
            IEncryptionService encryptionService,
            IAuditLogService auditLogService,
            ILogger<MessagingService> logger)
        {
            _context = context;
            _encryptionService = encryptionService;
            _auditLogService = auditLogService;
            _logger = logger;
        }

        public async Task<MessageDto> SendMessageAsync(SendMessageRequest request, Guid senderId)
        {
            // BUG-038 FIX: Check for existing message with same idempotency key
            if (!string.IsNullOrWhiteSpace(request.IdempotencyKey))
            {
                var existingMessage = await _context.WorkspaceMessages
                    .Include(m => m.Sender)
                    .FirstOrDefaultAsync(m =>
                        m.IdempotencyKey == request.IdempotencyKey &&
                        m.SenderId == senderId &&
                        m.WorkspaceId == request.WorkspaceId);

                if (existingMessage != null)
                {
                    _logger.LogInformation(
                        "Returning existing message for idempotency key: {IdempotencyKey}, Message ID: {MessageId}",
                        request.IdempotencyKey, existingMessage.Id);

                    // Return existing message
                    return new MessageDto
                    {
                        Id = existingMessage.Id,
                        WorkspaceId = existingMessage.WorkspaceId,
                        SenderId = existingMessage.SenderId,
                        SenderName = existingMessage.Sender.UserName ?? "Unknown",
                        MessageText = await DecryptMessageTextAsync(existingMessage.MessageText),
                        MessageType = existingMessage.MessageType,
                        Status = existingMessage.Status,
                        ReplyToMessageId = existingMessage.ReplyToMessageId,
                        AttachmentUrl = existingMessage.AttachmentUrl,
                        AttachmentFileName = existingMessage.AttachmentFileName,
                        AttachmentSize = existingMessage.AttachmentSize,
                        AttachmentMimeType = existingMessage.AttachmentMimeType,
                        IsEdited = existingMessage.IsEdited,
                        CreatedAt = existingMessage.CreatedAt,
                        EditedAt = existingMessage.EditedAt,
                        ReadAt = existingMessage.ReadAt
                    };
                }
            }

            // Validate workspace access
            if (!await HasMessagingAccessAsync(request.WorkspaceId, senderId))
            {
                throw new UnauthorizedAccessException("User does not have access to this workspace");
            }

            // Validate reply-to message if specified
            WorkspaceMessage? replyToMessage = null;
            if (request.ReplyToMessageId.HasValue)
            {
                replyToMessage = await _context.WorkspaceMessages
                    .FirstOrDefaultAsync(m => m.Id == request.ReplyToMessageId.Value && m.WorkspaceId == request.WorkspaceId);

                if (replyToMessage == null)
                {
                    throw new ArgumentException("Reply-to message not found in this workspace");
                }
            }

            // Create message entity
            var message = new WorkspaceMessage
            {
                WorkspaceId = request.WorkspaceId,
                SenderId = senderId,
                MessageText = await EncryptMessageTextAsync(request.MessageText),
                MessageType = request.MessageType,
                Status = MessageStatus.Sent,
                ReplyToMessageId = request.ReplyToMessageId,
                AttachmentUrl = request.AttachmentUrl,
                AttachmentFileName = request.AttachmentFileName,
                AttachmentSize = request.AttachmentSize,
                AttachmentMimeType = request.AttachmentMimeType,
                IdempotencyKey = request.IdempotencyKey, // BUG-038 FIX: Store idempotency key
                SenderIpAddress = request.IpAddress,
                SenderUserAgent = request.UserAgent,
                CreatedAt = DateTime.UtcNow
            };

            _context.WorkspaceMessages.Add(message);
            await _context.SaveChangesAsync();

            // Log the activity
            await _auditLogService.LogEventAsync(
                senderId,
                "SendMessage",
                request.IpAddress ?? "",
                request.UserAgent,
                true,
                $"Sent message in workspace {request.WorkspaceId}");

            return await MapToMessageDto(message, senderId);
        }

        public async Task<MessageDto> EditMessageAsync(Guid messageId, EditMessageRequest request, Guid userId)
        {
            var message = await _context.WorkspaceMessages
                .Include(m => m.Reactions)
                    .ThenInclude(r => r.User)
                .FirstOrDefaultAsync(m => m.Id == messageId);

            if (message == null)
            {
                throw new ArgumentException("Message not found");
            }

            // Validate access and permissions
            if (!await HasMessagingAccessAsync(message.WorkspaceId, userId))
            {
                throw new UnauthorizedAccessException("User does not have access to this workspace");
            }

            if (!message.CanBeEditedBy(userId))
            {
                throw new UnauthorizedAccessException("User cannot edit this message");
            }

            // Update message
            message.MessageText = await EncryptMessageTextAsync(request.MessageText);
            message.MarkAsEdited();

            await _context.SaveChangesAsync();

            // Log the activity
            await _auditLogService.LogEventAsync(
                userId,
                "EditMessage",
                request.IpAddress ?? "",
                request.UserAgent,
                true,
                $"Edited message {messageId}");

            return await MapToMessageDto(message, userId);
        }

        public async Task<bool> DeleteMessageAsync(Guid messageId, Guid userId)
        {
            // Get message with workspace info in one query for better performance
            var message = await _context.WorkspaceMessages
                .FirstOrDefaultAsync(m => m.Id == messageId);

            if (message == null)
            {
                return false;
            }

            // Validate access and permissions
            if (!await HasMessagingAccessAsync(message.WorkspaceId, userId))
            {
                throw new UnauthorizedAccessException("User does not have access to this workspace");
            }

            if (!message.CanBeDeletedBy(userId))
            {
                throw new UnauthorizedAccessException("User cannot delete this message");
            }

            // Use bulk delete for better performance - check if using relational database
            bool isRelational = _context.Database.IsRelational();

            if (isRelational)
            {
                // Use transaction for relational databases
                await using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    // BUG-CRITICAL-001 FIX: Replace SQL injection-prone ExecuteSqlAsync with LINQ
                    // Delete related reactions using LINQ for type safety
                    var reactions = await _context.MessageReactions
                        .Where(mr => mr.MessageId == messageId)
                        .ToListAsync();

                    if (reactions.Any())
                    {
                        _context.MessageReactions.RemoveRange(reactions);
                    }

                    // Delete the message
                    _context.WorkspaceMessages.Remove(message);
                    await _context.SaveChangesAsync();

                    await transaction.CommitAsync();
                    return true;
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }
            else
            {
                // In-memory database: find and delete reactions first, then message
                var reactions = await _context.MessageReactions
                    .Where(r => r.MessageId == messageId)
                    .ToListAsync();

                _context.MessageReactions.RemoveRange(reactions);
                _context.WorkspaceMessages.Remove(message);
                await _context.SaveChangesAsync();

                return true;
            }
        }

        public async Task<bool> MarkMessageAsReadAsync(Guid messageId, Guid userId)
        {
            var message = await _context.WorkspaceMessages
                .FirstOrDefaultAsync(m => m.Id == messageId);

            if (message == null)
            {
                return false;
            }

            // Validate access
            if (!await HasMessagingAccessAsync(message.WorkspaceId, userId))
            {
                throw new UnauthorizedAccessException("User does not have access to this workspace");
            }

            // Only mark as read if not sent by the same user and not already read
            if (message.SenderId != userId && message.Status != MessageStatus.Read)
            {
                message.MarkAsRead();
                await _context.SaveChangesAsync();
            }

            return true;
        }

        public async Task<int> MarkAllMessagesAsReadAsync(Guid workspaceId, Guid userId)
        {
            // Validate access
            if (!await HasMessagingAccessAsync(workspaceId, userId))
            {
                throw new UnauthorizedAccessException("User does not have access to this workspace");
            }

            var unreadMessages = await _context.WorkspaceMessages
                .Where(m => m.WorkspaceId == workspaceId &&
                           m.SenderId != userId &&
                           m.Status != MessageStatus.Read)
                .ToListAsync();

            foreach (var message in unreadMessages)
            {
                message.MarkAsRead();
            }

            await _context.SaveChangesAsync();

            // Log the activity
            await _auditLogService.LogEventAsync(
                userId,
                "MarkAllMessagesRead",
                "",
                "",
                true,
                $"Marked {unreadMessages.Count} messages as read in workspace {workspaceId}");

            return unreadMessages.Count;
        }

        public async Task<MessageHistoryResponse> GetMessageHistoryAsync(MessageHistoryRequest request, Guid userId)
        {
            // Validate access
            if (!await HasMessagingAccessAsync(request.WorkspaceId, userId))
            {
                throw new UnauthorizedAccessException("User does not have access to this workspace");
            }

            // PERFORMANCE FIX: Use AsSplitQuery to prevent cartesian explosion
            var query = _context.WorkspaceMessages
                .Include(m => m.Sender)
                .Include(m => m.ReplyToMessage)
                    .ThenInclude(r => r!.Sender)
                .Include(m => m.Reactions)
                    .ThenInclude(r => r.User)
                .AsSplitQuery()
                .Where(m => m.WorkspaceId == request.WorkspaceId);

            // Apply filters
            if (request.BeforeDate.HasValue)
                query = query.Where(m => m.CreatedAt < request.BeforeDate.Value);

            if (request.AfterDate.HasValue)
                query = query.Where(m => m.CreatedAt > request.AfterDate.Value);

            if (!string.IsNullOrWhiteSpace(request.SearchQuery))
            {
                // SECURITY-005 FIX: Escape LIKE special characters to prevent pattern injection
                var decryptedQuery = request.SearchQuery.ToLower()
                    .Replace("[", "[[]")
                    .Replace("%", "[%]")
                    .Replace("_", "[_]");
                query = query.Where(m => m.MessageText != null &&
                    EF.Functions.Like(m.MessageText.ToLower(), $"%{decryptedQuery}%"));
            }

            if (request.MessageType.HasValue)
                query = query.Where(m => m.MessageType == request.MessageType.Value);

            if (request.SenderId.HasValue)
                query = query.Where(m => m.SenderId == request.SenderId.Value);

            // Get total count
            var totalCount = await query.CountAsync();

            // Apply pagination and ordering
            var messages = await query
                .OrderByDescending(m => m.CreatedAt)
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync();

            // Map to DTOs
            var messageDtos = new List<MessageDto>();
            foreach (var message in messages)
            {
                messageDtos.Add(await MapToMessageDto(message, userId));
            }

            return new MessageHistoryResponse
            {
                Messages = messageDtos,
                TotalCount = totalCount,
                PageNumber = request.PageNumber,
                PageSize = request.PageSize,
                HasNextPage = request.PageNumber * request.PageSize < totalCount,
                HasPreviousPage = request.PageNumber > 1
            };
        }

        public async Task<SearchMessagesResponse> SearchMessagesAsync(SearchMessagesRequest request, Guid userId)
        {
            var stopwatch = Stopwatch.StartNew();

            // Validate access
            if (!await HasMessagingAccessAsync(request.WorkspaceId, userId))
            {
                throw new UnauthorizedAccessException("User does not have access to this workspace");
            }

            // Optimized query: Apply database-level filters first
            // PERFORMANCE FIX: Use AsSplitQuery to prevent cartesian explosion
            var baseQuery = _context.WorkspaceMessages
                .Include(m => m.Sender)
                .Include(m => m.ReplyToMessage)
                    .ThenInclude(r => r!.Sender)
                .Include(m => m.Reactions)
                    .ThenInclude(r => r.User)
                .AsSplitQuery()
                .Where(m => m.WorkspaceId == request.WorkspaceId)
                .AsQueryable();

            // Apply filters at database level first (more efficient)
            if (request.MessageType.HasValue)
            {
                baseQuery = baseQuery.Where(m => m.MessageType == request.MessageType.Value);
            }

            // Get filtered messages
            var filteredMessages = await baseQuery
                .OrderByDescending(m => m.CreatedAt)
                .ToListAsync();

            // Filter by search term in memory (after decryption) - only if query provided
            List<WorkspaceMessage> matchingMessages;
            if (!string.IsNullOrWhiteSpace(request.Query))
            {
                var searchTerm = request.Query.ToLower();
                matchingMessages = new List<WorkspaceMessage>();

                // Parallel processing for decryption if many messages
                if (filteredMessages.Count > 50)
                {
                    var searchTasks = filteredMessages.Select(async message =>
                    {
                        if (string.IsNullOrEmpty(message.MessageText)) return false;

                        try
                        {
                            var decryptedText = await _encryptionService.DecryptAsync(message.MessageText);
                            return decryptedText.ToLower().Contains(searchTerm);
                        }
                        catch
                        {
                            // Fallback for non-encrypted messages
                            return message.MessageText.ToLower().Contains(searchTerm);
                        }
                    });

                    var searchResults = await Task.WhenAll(searchTasks);
                    for (int i = 0; i < filteredMessages.Count; i++)
                    {
                        if (searchResults[i])
                        {
                            matchingMessages.Add(filteredMessages[i]);
                        }
                    }
                }
                else
                {
                    // Sequential processing for smaller datasets
                    foreach (var message in filteredMessages)
                    {
                        if (string.IsNullOrEmpty(message.MessageText)) continue;

                        try
                        {
                            var decryptedText = await _encryptionService.DecryptAsync(message.MessageText);
                            if (decryptedText.ToLower().Contains(searchTerm))
                            {
                                matchingMessages.Add(message);
                            }
                        }
                        catch
                        {
                            if (message.MessageText.ToLower().Contains(searchTerm))
                            {
                                matchingMessages.Add(message);
                            }
                        }
                    }
                }
            }
            else
            {
                matchingMessages = filteredMessages;
            }

            // Calculate pagination
            var totalCount = matchingMessages.Count;
            var skip = (request.PageNumber - 1) * request.PageSize;
            var paginatedMessages = matchingMessages
                .Skip(skip)
                .Take(request.PageSize)
                .ToList();

            // Map to DTOs (parallel for better performance)
            var messageDtos = new MessageDto[paginatedMessages.Count];
            var mapTasks = paginatedMessages.Select(async (message, index) =>
            {
                var dto = await MapToMessageDto(message, userId);
                return (index, dto);
            });

            var mapResults = await Task.WhenAll(mapTasks);
            foreach (var (index, dto) in mapResults)
            {
                messageDtos[index] = dto;
            }

            stopwatch.Stop();

            return new SearchMessagesResponse
            {
                Messages = messageDtos.ToList(),
                TotalCount = totalCount,
                PageNumber = request.PageNumber,
                PageSize = request.PageSize,
                Query = request.Query,
                SearchDuration = stopwatch.Elapsed
            };
        }

        public async Task<MessageDto?> GetMessageAsync(Guid messageId, Guid userId)
        {
            // PERFORMANCE FIX: Use AsSplitQuery to prevent cartesian explosion
            var message = await _context.WorkspaceMessages
                .Include(m => m.Sender)
                .Include(m => m.ReplyToMessage)
                    .ThenInclude(r => r!.Sender)
                .Include(m => m.Reactions)
                    .ThenInclude(r => r.User)
                .AsSplitQuery()
                .FirstOrDefaultAsync(m => m.Id == messageId);

            if (message == null)
            {
                return null;
            }

            // Validate access
            if (!await HasMessagingAccessAsync(message.WorkspaceId, userId))
            {
                return null; // Don't reveal existence to unauthorized users
            }

            return await MapToMessageDto(message, userId);
        }

        public async Task<bool> AddReactionAsync(Guid messageId, AddReactionRequest request, Guid userId)
        {
            var message = await _context.WorkspaceMessages
                .FirstOrDefaultAsync(m => m.Id == messageId);

            if (message == null)
            {
                return false;
            }

            // Validate access
            if (!await HasMessagingAccessAsync(message.WorkspaceId, userId))
            {
                throw new UnauthorizedAccessException("User does not have access to this workspace");
            }

            // Check if reaction already exists
            var existingReaction = await _context.MessageReactions
                .FirstOrDefaultAsync(r => r.MessageId == messageId &&
                                        r.UserId == userId &&
                                        r.Emoji == request.Emoji);

            if (existingReaction != null)
            {
                return true; // Reaction already exists
            }

            // Add new reaction
            var reaction = new MessageReaction
            {
                MessageId = messageId,
                UserId = userId,
                Emoji = request.Emoji,
                IpAddress = request.IpAddress,
                CreatedAt = DateTime.UtcNow
            };

            _context.MessageReactions.Add(reaction);
            await _context.SaveChangesAsync();

            // Log the activity
            await _auditLogService.LogEventAsync(
                userId,
                "AddReaction",
                request.IpAddress ?? "",
                "",
                true,
                $"Added reaction {request.Emoji} to message {messageId}");

            return true;
        }

        public async Task<bool> RemoveReactionAsync(Guid messageId, string emoji, Guid userId)
        {
            var reaction = await _context.MessageReactions
                .FirstOrDefaultAsync(r => r.MessageId == messageId &&
                                        r.UserId == userId &&
                                        r.Emoji == emoji);

            if (reaction == null)
            {
                return false;
            }

            // Validate access to workspace
            var message = await _context.WorkspaceMessages
                .FirstOrDefaultAsync(m => m.Id == messageId);

            if (message == null || !await HasMessagingAccessAsync(message.WorkspaceId, userId))
            {
                throw new UnauthorizedAccessException("User does not have access to this workspace");
            }

            _context.MessageReactions.Remove(reaction);
            await _context.SaveChangesAsync();

            // Log the activity
            await _auditLogService.LogEventAsync(
                userId,
                "RemoveReaction",
                "",
                "",
                true,
                $"Removed reaction {emoji} from message {messageId}");

            return true;
        }

        public async Task<bool> UpdateTypingIndicatorAsync(Guid workspaceId, Guid userId, string? connectionId = null)
        {
            // Validate access
            if (!await HasMessagingAccessAsync(workspaceId, userId))
            {
                throw new UnauthorizedAccessException("User does not have access to this workspace");
            }

            var indicator = await _context.TypingIndicators
                .FirstOrDefaultAsync(t => t.WorkspaceId == workspaceId && t.UserId == userId);

            if (indicator == null)
            {
                // Create new indicator
                indicator = new TypingIndicator
                {
                    WorkspaceId = workspaceId,
                    UserId = userId,
                    ConnectionId = connectionId,
                    LastTypingAt = DateTime.UtcNow
                };
                _context.TypingIndicators.Add(indicator);
            }
            else
            {
                // Update existing indicator
                indicator.UpdateTyping();
                indicator.ConnectionId = connectionId;
            }

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> StopTypingIndicatorAsync(Guid workspaceId, Guid userId, string? connectionId = null)
        {
            var query = _context.TypingIndicators
                .Where(t => t.WorkspaceId == workspaceId && t.UserId == userId);

            if (!string.IsNullOrEmpty(connectionId))
            {
                query = query.Where(t => t.ConnectionId == connectionId);
            }

            var indicators = await query.ToListAsync();

            if (indicators.Any())
            {
                _context.TypingIndicators.RemoveRange(indicators);
                await _context.SaveChangesAsync();
            }

            return true;
        }

        public async Task<List<TypingIndicatorDto>> GetTypingIndicatorsAsync(Guid workspaceId, Guid? excludeUserId = null)
        {
            var query = _context.TypingIndicators
                .Include(t => t.User)
                .Where(t => t.WorkspaceId == workspaceId);

            if (excludeUserId.HasValue)
            {
                query = query.Where(t => t.UserId != excludeUserId.Value);
            }

            var indicators = await query.ToListAsync();

            return indicators
                .Where(t => t.IsActive())
                .Select(t => new TypingIndicatorDto
                {
                    UserId = t.UserId,
                    UserName = t.User.Email ?? t.User.UserName ?? "Unknown User",
                    LastTypingAt = t.LastTypingAt,
                    IsActive = t.IsActive()
                })
                .ToList();
        }

        public async Task<int> CleanupInactiveTypingIndicatorsAsync()
        {
            var cutoffTime = DateTime.UtcNow.AddSeconds(-5);

            // BUG-CRITICAL-001 FIX: Replace SQL injection-prone ExecuteSqlAsync with LINQ
            // Use LINQ for all database types for type safety and consistency
            var inactiveIndicators = await _context.TypingIndicators
                .Where(t => t.LastTypingAt < cutoffTime)
                .ToListAsync();

            if (inactiveIndicators.Any())
            {
                _context.TypingIndicators.RemoveRange(inactiveIndicators);
                await _context.SaveChangesAsync();
            }

            return inactiveIndicators.Count;
        }

        public async Task<MessageStatsDto> GetMessageStatsAsync(Guid workspaceId, Guid userId)
        {
            // Validate access
            if (!await HasMessagingAccessAsync(workspaceId, userId))
            {
                throw new UnauthorizedAccessException("User does not have access to this workspace");
            }

            var messages = await _context.WorkspaceMessages
                .Where(m => m.WorkspaceId == workspaceId)
                .ToListAsync();

            var totalMessages = messages.Count;
            var unreadMessages = messages.Count(m => m.SenderId != userId && m.Status != MessageStatus.Read);
            var lastMessage = messages.OrderByDescending(m => m.CreatedAt).FirstOrDefault();

            var messagesByType = messages
                .GroupBy(m => m.MessageType)
                .ToDictionary(g => g.Key, g => g.Count());

            var reactions = await _context.MessageReactions
                .Where(r => r.Message.WorkspaceId == workspaceId)
                .GroupBy(r => r.Emoji)
                .Select(g => new { Emoji = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .Take(5)
                .ToDictionaryAsync(x => x.Emoji, x => x.Count);

            return new MessageStatsDto
            {
                WorkspaceId = workspaceId,
                TotalMessages = totalMessages,
                UnreadMessages = unreadMessages,
                LastMessageAt = lastMessage?.CreatedAt,
                MessagesByType = messagesByType,
                TopReactions = reactions
            };
        }

        public async Task<bool> HasMessagingAccessAsync(Guid workspaceId, Guid userId)
        {
            var workspace = await _context.ProjectWorkspaces
                .FirstOrDefaultAsync(w => w.Id == workspaceId &&
                    (w.ClientId == userId || w.ProviderId == userId) &&
                    w.Status == WorkspaceStatus.Active);

            return workspace != null;
        }

        public async Task<int> GetUnreadMessageCountAsync(Guid userId)
        {
            // Get all workspaces user has access to
            var workspaceIds = await _context.ProjectWorkspaces
                .Where(w => (w.ClientId == userId || w.ProviderId == userId) &&
                           w.Status == WorkspaceStatus.Active)
                .Select(w => w.Id)
                .ToListAsync();

            if (!workspaceIds.Any())
            {
                return 0;
            }

            return await _context.WorkspaceMessages
                .CountAsync(m => workspaceIds.Contains(m.WorkspaceId) &&
                                m.SenderId != userId &&
                                m.Status != MessageStatus.Read);
        }

        public async Task<int> GetWorkspaceUnreadCountAsync(Guid workspaceId, Guid userId)
        {
            // Validate access
            if (!await HasMessagingAccessAsync(workspaceId, userId))
            {
                return 0;
            }

            return await _context.WorkspaceMessages
                .CountAsync(m => m.WorkspaceId == workspaceId &&
                                m.SenderId != userId &&
                                m.Status != MessageStatus.Read);
        }

        private async Task<string?> EncryptMessageTextAsync(string? text)
        {
            if (string.IsNullOrEmpty(text))
                return text;

            return await _encryptionService.EncryptAsync(text);
        }

        private async Task<string?> DecryptMessageTextAsync(string? encryptedText)
        {
            if (string.IsNullOrEmpty(encryptedText))
                return encryptedText;

            return await _encryptionService.DecryptAsync(encryptedText);
        }

        private async Task<MessageDto> MapToMessageDto(WorkspaceMessage message, Guid currentUserId)
        {
            return new MessageDto
            {
                Id = message.Id,
                WorkspaceId = message.WorkspaceId,
                SenderId = message.SenderId,
                SenderName = message.Sender?.Email ?? message.Sender?.UserName ?? "Unknown User",
                SenderAvatar = "", // Will be populated from Profile if needed
                MessageText = await DecryptMessageTextAsync(message.MessageText),
                MessageType = message.MessageType,
                Status = message.Status,
                IsEdited = message.IsEdited,
                CreatedAt = message.CreatedAt,
                EditedAt = message.EditedAt,
                ReadAt = message.ReadAt,
                ReplyToMessageId = message.ReplyToMessageId,
                ReplyToMessage = message.ReplyToMessage != null ? new MessageDto
                {
                    Id = message.ReplyToMessage.Id,
                    SenderId = message.ReplyToMessage.SenderId,
                    SenderName = message.ReplyToMessage.Sender?.Email ?? message.ReplyToMessage.Sender?.UserName ?? "Unknown User",
                    MessageText = await DecryptMessageTextAsync(message.ReplyToMessage.MessageText),
                    MessageType = message.ReplyToMessage.MessageType,
                    CreatedAt = message.ReplyToMessage.CreatedAt
                } : null,
                AttachmentUrl = message.AttachmentUrl,
                AttachmentFileName = message.AttachmentFileName,
                AttachmentSize = message.AttachmentSize,
                AttachmentMimeType = message.AttachmentMimeType,
                Reactions = message.Reactions?.Select(r => new MessageReactionDto
                {
                    Id = r.Id,
                    UserId = r.UserId,
                    UserName = r.User?.Email ?? r.User?.UserName ?? "Unknown User",
                    Emoji = r.Emoji,
                    CreatedAt = r.CreatedAt
                }).ToList() ?? new List<MessageReactionDto>(),
                CanEdit = message.CanBeEditedBy(currentUserId),
                CanDelete = message.CanBeDeletedBy(currentUserId)
            };
        }
    }
}