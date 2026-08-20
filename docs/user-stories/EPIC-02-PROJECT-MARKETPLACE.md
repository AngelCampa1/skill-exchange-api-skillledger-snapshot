# Epic 2: Project Marketplace
## Project Posting, Discovery & Application Management

*Streamlined implementation guide focusing on architecture and requirements*

---

## 🎯 Epic Overview

**Goal**: Create a structured and professional marketplace where users can post well-defined, deliverable-based projects and efficiently find qualified professionals to execute them.

**Business Value**: Enables the core value exchange of SkillLedger by facilitating project-based collaboration between clients and service providers.

---

## US-2.1.1: Structured Project Creation

### 📋 User Story
**As a** verified client  
**I want** to post a structured project with clear deliverables and fair market value  
**So that** providers understand the scope, requirements, and can make informed decisions about applying

### ✅ Acceptance Criteria
- [ ] Project title with length limits (100 chars) and profanity filtering
- [ ] Rich text description with XSS protection (max 5000 chars)
- [ ] Deliverables checklist builder (min 1, max 10 items)
- [ ] Timeline with start/end dates and milestone support
- [ ] Skills taxonomy selection (min 1, max 5 relevant skills)
- [ ] Credit budget specification (project value in credits)
- [ ] Content moderation queue for all new projects
- [ ] Draft save functionality for incomplete projects

### 🏗️ Technical Architecture

#### Backend (.NET 9 API)
- **Project Entity**: GUID ID, client reference, structured deliverables, skills mapping
- **Content Moderation**: Azure Content Safety API integration, human review queue
- **Search & Discovery**: Full-text search with Azure Cognitive Search
- **Validation**: Business rules for budget limits, timeline validation, skill requirements

#### Frontend (Next.js 14)
- **Multi-step Form**: Progressive disclosure, draft saving, real-time validation
- **Rich Text Editor**: Secure content input with XSS protection
- **Skills Selector**: Searchable taxonomy with proficiency levels
- **Budget Calculator**: Credit budget estimation and validation

#### Mobile (React Native)
- **Responsive Forms**: Touch-optimized input controls
- **Offline Drafts**: Local storage with sync capabilities
- **Camera Integration**: Direct photo upload for project details

### 🗄️ Database Schema
```sql
-- Core project structure
CREATE TABLE Projects (
    Id UNIQUEIDENTIFIER PRIMARY KEY,
    ClientId UNIQUEIDENTIFIER REFERENCES Users(Id),
    Title NVARCHAR(100) NOT NULL,
    Description NVARCHAR(MAX) NOT NULL,
    Status INT DEFAULT 0, -- Draft, Published, InProgress, Completed
    CreditBudget INT CHECK (CreditBudget BETWEEN 50 AND 5000),
    StartDate DATETIME2,
    EndDate DATETIME2 CHECK (EndDate > GETUTCDATE()),
    ModerationStatus INT DEFAULT 0,
    CreatedAt DATETIME2 DEFAULT GETUTCDATE()
);

-- Project deliverables
CREATE TABLE ProjectDeliverables (
    Id UNIQUEIDENTIFIER PRIMARY KEY,
    ProjectId UNIQUEIDENTIFIER REFERENCES Projects(Id),
    Title NVARCHAR(100) NOT NULL,
    Description NVARCHAR(500) NOT NULL,
    SortOrder INT NOT NULL,
    IsCompleted BIT DEFAULT 0
);

-- Skills requirement mapping
CREATE TABLE ProjectSkills (
    ProjectId UNIQUEIDENTIFIER REFERENCES Projects(Id),
    SkillId UNIQUEIDENTIFIER REFERENCES Skills(Id),
    RequiredLevel INT CHECK (RequiredLevel BETWEEN 1 AND 4),
    PRIMARY KEY (ProjectId, SkillId)
);
```

---

## US-2.2.1: Advanced Project Discovery

### 📋 User Story
**As a** service provider  
**I want** to search and filter projects by skills, budget, and timeline  
**So that** I can efficiently find opportunities that match my expertise and availability

### ✅ Acceptance Criteria
- [ ] Full-text search across titles and descriptions
- [ ] Multi-faceted filtering (skills, budget range, timeline, location)
- [ ] Sorting options (newest, highest budget, deadline proximity)
- [ ] Saved search alerts with email notifications
- [ ] Recommended projects based on profile matching
- [ ] View analytics (view count, application count)

### 🏗️ Technical Architecture
- **Search Engine**: Azure Cognitive Search with custom scoring profiles
- **Recommendation Engine**: ML-based matching using user profiles and project requirements
- **Real-time Updates**: SignalR for live project status updates
- **Analytics**: Application Insights for usage tracking and optimization

---

## US-2.3.1: Project Application System

### 📋 User Story
**As a** qualified provider  
**I want** to submit applications with cover letters and proposed timelines  
**So that** clients can evaluate my fit for their projects

### ✅ Acceptance Criteria
- [ ] Cover letter with rich text formatting (max 2000 chars)
- [ ] Proposed timeline with milestone breakdown
- [ ] Portfolio attachments with size limits
- [ ] Application status tracking
- [ ] Withdrawal option before selection
- [ ] Anti-spam measures to prevent application flooding

### 🏗️ Technical Architecture
- **Application Management**: Status workflow with automated notifications
- **File Storage**: Azure Blob Storage with virus scanning
- **Spam Prevention**: Rate limiting, reputation scoring, content analysis
- **Communication**: Secure messaging between clients and applicants

---

## US-2.4.1: Provider Selection & Matching

### 📋 User Story
**As a** project client  
**I want** to review applications and select the best provider  
**So that** I can choose someone with the right skills and approach for my project

### ✅ Acceptance Criteria
- [ ] Application comparison interface with side-by-side views
- [ ] Provider profile integration with ratings and past work
- [ ] Interview scheduling system with calendar integration
- [ ] Reference check automation
- [ ] Contract generation with standard terms
- [ ] Escrow initiation upon provider selection

### 🏗️ Technical Architecture
- **Selection Workflow**: Multi-stage approval process with audit trail
- **Integration APIs**: Calendar systems (Outlook, Google), video conferencing
- **Contract Management**: Template-based legal document generation
- **Escrow Integration**: Automatic credit hold and release mechanisms

---

## 🔐 Security Requirements

### Content Security
- **Input Validation**: XSS protection, SQL injection prevention
- **Content Moderation**: AI-powered screening with human review escalation
- **File Security**: Virus scanning, file type validation, size restrictions
- **Anti-fraud**: Duplicate project detection, suspicious activity monitoring

### Business Logic Security
- **Budget Validation**: Reasonable FMV-to-credit ratios
- **Timeline Validation**: Realistic project durations
- **Skill Verification**: Provider qualifications matching requirements
- **Application Limits**: Prevent spam applications, rate limiting

### Data Protection
- **Privacy Controls**: Selective information sharing, anonymization options
- **Audit Logging**: All project lifecycle events tracked
- **Access Controls**: Role-based permissions for project management
- **Data Retention**: Automated cleanup of old project data

---

## 🧪 Testing Strategy

### Unit Tests
- Project validation logic
- Search algorithm accuracy
- Application workflow states
- Budget calculation correctness

### Integration Tests
- Content moderation pipeline
- Search engine integration
- File upload and processing
- Email notification delivery

### Performance Tests
- Search response times under load
- Concurrent application processing
- Large file upload handling
- Database query optimization

---

## 📊 Monitoring & Observability

### Business Metrics
- Project creation rate and completion
- Application-to-selection ratio
- Search effectiveness and usage
- Provider satisfaction scores
- Client retention rates

### Technical Metrics
- Search latency and accuracy
- File processing performance
- Application workflow efficiency
- Content moderation effectiveness

### Alerts
- High project rejection rates
- Search performance degradation
- Content moderation failures
- Unusual application patterns

---

## 🚀 Deployment Configuration

### Azure Resources
- **Cognitive Search**: Custom indexes with semantic search
- **Blob Storage**: Hot/cool tiers for file optimization
- **Content Safety**: AI content moderation
- **Service Bus**: Async workflow processing
- **CDN**: Global content delivery for media files

### Configuration Settings
```json
{
  "ProjectLimits": {
    "MaxProjectsPerDay": 5,
    "MaxDeliverablesPerProject": 10,
    "MaxSkillsPerProject": 5,
    "MinCreditBudget": 50,
    "MaxCreditBudget": 5000,
    "MaxDescriptionLength": 5000
  },
  "Search": {
    "MaxResults": 50,
    "SearchTimeoutMs": 3000,
    "FacetTimeout": 1000
  },
  "ContentModeration": {
    "AutoApproveThreshold": 0.8,
    "HumanReviewThreshold": 0.6,
    "BlockingThreshold": 0.3
  }
}
```

---

## 🔗 Dependencies & Prerequisites

### Required User Stories
- US-1.1.1: User Registration (users must exist)
- US-1.2.1: Phone Verification (for project creation)
- Skills taxonomy seeded in database

### External Services
- Azure Cognitive Search configuration
- Content Safety API setup
- Blob Storage containers configured
- Service Bus topics for workflows

### Subsequent Stories
- US-3.2.1: Project Escrow (initiated when provider selected)
- US-4.1.1: Collaboration Workspace (activated after selection)
- US-5.1.1: Project Reviews (completed after project delivery)

This streamlined epic provides the essential architecture for a comprehensive project marketplace without excessive implementation details.