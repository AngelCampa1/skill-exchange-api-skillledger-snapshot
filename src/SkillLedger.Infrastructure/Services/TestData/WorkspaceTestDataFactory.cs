using Bogus;
using SkillLedger.Core.Entities;
using SkillLedger.Core.Enums;

namespace SkillLedger.Infrastructure.Services.TestData;

/// <summary>
/// Factory for creating test collaboration data (workspaces, messages, documents)
/// </summary>
public class WorkspaceTestDataFactory
{
    private readonly Faker _faker;

    public WorkspaceTestDataFactory()
    {
        _faker = new Faker();
    }

    /// <summary>
    /// Creates workspaces for in-progress projects
    /// </summary>
    public List<ProjectWorkspace> CreateWorkspacesForProjects(List<Project> projects, List<User> users)
    {
        var workspaces = new List<ProjectWorkspace>();

        foreach (var project in projects)
        {
            // Only create workspaces for in-progress or completed projects with providers
            if (project.ProviderId == null)
                continue;

            if (project.Status != ProjectStatus.InProgress &&
                project.Status != ProjectStatus.Completed)
                continue;

            var client = users.FirstOrDefault(u => u.Id == project.ClientId);
            var provider = users.FirstOrDefault(u => u.Id == project.ProviderId.Value);

            if (client == null || provider == null)
                continue;

            workspaces.Add(new ProjectWorkspace
            {
                Id = Guid.NewGuid(),
                ProjectId = project.Id,
                ClientId = project.ClientId,
                ProviderId = project.ProviderId.Value,
                Status = project.Status == ProjectStatus.Completed ? WorkspaceStatus.Archived : WorkspaceStatus.Active,
                CreatedAt = project.StartDate ?? project.CreatedAt,
                ArchivedAt = project.Status == ProjectStatus.Completed ? project.CompletedAt : null
            });
        }

        return workspaces;
    }

    /// <summary>
    /// Creates messages for active workspaces
    /// </summary>
    public List<WorkspaceMessage> CreateMessagesForWorkspaces(
        List<ProjectWorkspace> workspaces,
        List<Project> projects,
        List<User> users)
    {
        var messages = new List<WorkspaceMessage>();

        foreach (var workspace in workspaces.Where(w => w.Status == WorkspaceStatus.Active).Take(5)) // Limit to first 5 for performance
        {
            var project = projects.FirstOrDefault(p => p.Id == workspace.ProjectId);
            if (project == null || !project.ProviderId.HasValue)
                continue;

            var client = users.FirstOrDefault(u => u.Id == project.ClientId);
            var provider = users.FirstOrDefault(u => u.Id == project.ProviderId.Value);

            if (client == null || provider == null)
                continue;

            // Create conversation between client and provider
            messages.AddRange(CreateConversation(workspace.Id, client, provider, project));
        }

        return messages;
    }

    private List<WorkspaceMessage> CreateConversation(
        Guid workspaceId,
        User client,
        User provider,
        Project project)
    {
        var messages = new List<WorkspaceMessage>();
        var baseTime = project.StartDate ?? project.CreatedAt;

        // Initial message from client
        messages.Add(CreateMessage(
            workspaceId,
            client.Id,
            $"Hi {provider.FirstName}, excited to start working on this project!",
            baseTime.AddHours(1)
        ));

        // Provider response
        messages.Add(CreateMessage(
            workspaceId,
            provider.Id,
            $"Hello {client.FirstName}! Thank you for choosing me. I've reviewed the requirements and have a few questions.",
            baseTime.AddHours(3)
        ));

        // Client reply
        messages.Add(CreateMessage(
            workspaceId,
            client.Id,
            "Sure, go ahead! I'm here to answer any questions.",
            baseTime.AddHours(4)
        ));

        // Provider question
        messages.Add(CreateMessage(
            workspaceId,
            provider.Id,
            "For the deliverables, do you have a preferred technology stack or should I recommend one?",
            baseTime.AddHours(4.5)
        ));

        // Client answer
        messages.Add(CreateMessage(
            workspaceId,
            client.Id,
            "I'm open to your recommendations. Please suggest what you think would work best.",
            baseTime.AddHours(5)
        ));

        // Provider milestone update
        messages.Add(CreateMessage(
            workspaceId,
            provider.Id,
            "Great! I've started working on the first milestone. I'll have an update for you by end of week.",
            baseTime.AddDays(1)
        ));

        // Client acknowledgment
        messages.Add(CreateMessage(
            workspaceId,
            client.Id,
            "Perfect! Looking forward to seeing the progress.",
            baseTime.AddDays(1).AddHours(2)
        ));

        // Progress update
        messages.Add(CreateMessage(
            workspaceId,
            provider.Id,
            "Milestone 1 is complete! I've uploaded the deliverables for your review.",
            baseTime.AddDays(7)
        ));

        // Client review
        messages.Add(CreateMessage(
            workspaceId,
            client.Id,
            "This looks great! Approved. Moving forward with milestone 2.",
            baseTime.AddDays(7).AddHours(4)
        ));

        return messages;
    }

    private WorkspaceMessage CreateMessage(
        Guid workspaceId,
        Guid senderId,
        string content,
        DateTime sentAt)
    {
        return new WorkspaceMessage
        {
            Id = Guid.NewGuid(),
            WorkspaceId = workspaceId,
            SenderId = senderId,
            MessageText = content,
            MessageType = MessageType.Text,
            Status = MessageStatus.Read,
            IsEdited = false,
            CreatedAt = sentAt,
            IdempotencyKey = Guid.NewGuid().ToString()
        };
    }

    /// <summary>
    /// Creates documents for workspaces
    /// </summary>
    public List<WorkspaceDocument> CreateDocumentsForWorkspaces(
        List<ProjectWorkspace> workspaces,
        List<Project> projects,
        List<User> users)
    {
        var documents = new List<WorkspaceDocument>();

        foreach (var workspace in workspaces.Where(w => w.Status == WorkspaceStatus.Active).Take(5)) // Limit to first 5
        {
            var project = projects.FirstOrDefault(p => p.Id == workspace.ProjectId);
            if (project == null || !project.ProviderId.HasValue)
                continue;

            var provider = users.FirstOrDefault(u => u.Id == project.ProviderId.Value);
            if (provider == null)
                continue;

            // Requirements document
            documents.Add(CreateDocument(
                workspace.Id,
                project.ClientId,
                "Project Requirements.pdf",
                "application/pdf",
                245000,
                project.CreatedAt.AddDays(1)
            ));

            // Design mockups
            documents.Add(CreateDocument(
                workspace.Id,
                provider.Id,
                "Design Mockups.fig",
                "application/octet-stream",
                1024000,
                project.CreatedAt.AddDays(5)
            ));

            // Code deliverable
            if (project.Status == ProjectStatus.InProgress || project.Status == ProjectStatus.Completed)
            {
                documents.Add(CreateDocument(
                    workspace.Id,
                    provider.Id,
                    "Source Code.zip",
                    "application/zip",
                    5242880,
                    project.StartDate?.AddDays(10) ?? DateTime.UtcNow
                ));
            }
        }

        return documents;
    }

    private WorkspaceDocument CreateDocument(
        Guid workspaceId,
        Guid uploadedById,
        string fileName,
        string mimeType,
        long fileSize,
        DateTime uploadedAt)
    {
        return new WorkspaceDocument
        {
            Id = Guid.NewGuid(),
            WorkspaceId = workspaceId,
            FileName = fileName,
            FilePath = $"/uploads/workspace/{workspaceId}/{Guid.NewGuid()}/{fileName}",
            MimeType = mimeType,
            FileSize = fileSize,
            UploadedBy = uploadedById,
            IsDeleted = false,
            CreatedAt = uploadedAt,
            SecurityScanPassed = true
        };
    }

    /// <summary>
    /// Creates project reviews for completed projects
    /// </summary>
    public List<ProjectReview> CreateReviewsForProjects(
        List<Project> projects,
        List<User> users)
    {
        var reviews = new List<ProjectReview>();

        foreach (var project in projects.Where(p => p.Status == ProjectStatus.Completed && p.ProviderId.HasValue))
        {
            var client = users.FirstOrDefault(u => u.Id == project.ClientId);
            var provider = users.FirstOrDefault(u => u.Id == project.ProviderId.Value);

            if (client == null || provider == null)
                continue;

            // Determine review scores based on project
            var (clientRating, providerRating, clientReviewText, providerReviewText) = GetReviewDataForProject(project);

            // Client reviews Provider
            reviews.Add(CreateReview(
                project.Id,
                client.Id,
                provider.Id,
                clientRating,
                clientReviewText,
                project.CompletedAt?.AddDays(2) ?? DateTime.UtcNow
            ));

            // Provider reviews Client
            reviews.Add(CreateReview(
                project.Id,
                provider.Id,
                client.Id,
                providerRating,
                providerReviewText,
                project.CompletedAt?.AddDays(2).AddHours(6) ?? DateTime.UtcNow
            ));
        }

        return reviews;
    }

    private (int clientRating, int providerRating, string clientText, string providerText)
        GetReviewDataForProject(Project project)
    {
        // Project 23 - Excellent mutual reviews
        if (project.Id.ToString().Contains("23"))
        {
            return (
                9,
                10,
                "Exceptional work! David delivered beyond expectations. The brand identity is exactly what we needed.",
                "Perfect client! Clear requirements, timely feedback, and prompt payment. Highly recommended."
            );
        }

        // Project 24 - Outstanding with bonus
        if (project.Id.ToString().Contains("24"))
        {
            return (
                10,
                10,
                "Outstanding performance on critical project. Zero downtime achieved. Highly professional.",
                "Excellent client with clear vision. Great communication throughout the project."
            );
        }

        // Project 26 - Mixed review
        if (project.Id.ToString().Contains("26"))
        {
            return (
                6,
                9,
                "Work was acceptable but delivery was late. Communication could have been better.",
                "Client was professional and payments were on time. Would work with again."
            );
        }

        // Default good reviews
        return (
            8,
            9,
            "Great work overall. Delivered quality results and was responsive to feedback.",
            "Professional client with clear expectations. Smooth project execution."
        );
    }

    private ProjectReview CreateReview(
        Guid projectId,
        Guid reviewerId,
        Guid revieweeId,
        int overallRating,
        string reviewText,
        DateTime createdAt)
    {
        return new ProjectReview
        {
            Id = Guid.NewGuid(),
            ProjectId = projectId,
            ReviewerId = reviewerId,
            RevieweeId = revieweeId,
            OverallRating = overallRating,
            QualityRating = Math.Max(1, overallRating - _faker.Random.Int(0, 1)),
            CommunicationRating = Math.Max(1, overallRating - _faker.Random.Int(0, 1)),
            TimelinessRating = Math.Max(1, overallRating - _faker.Random.Int(0, 2)),
            ProfessionalismRating = Math.Max(1, overallRating - _faker.Random.Int(0, 1)),
            ReviewText = reviewText,
            Status = ProjectReviewStatus.Published,
            CreatedAt = createdAt,
            UpdatedAt = createdAt,
            PublishedAt = createdAt.AddHours(1) // Published after blind review period
        };
    }
}
