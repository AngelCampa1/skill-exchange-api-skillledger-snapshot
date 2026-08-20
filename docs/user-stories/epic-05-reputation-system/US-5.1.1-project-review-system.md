# US-5.1.1: Project Review System

## 📋 User Story
**As a** project participant (client or provider)  
**I want** to leave detailed reviews after project completion  
**So that** the community can make informed decisions about future collaborations

## ✅ Acceptance Criteria
- [ ] Mandatory two-way review system (both client and provider review each other)
- [ ] Multi-dimensional ratings (quality, communication, timeliness, professionalism)
- [ ] Written review with minimum character requirements
- [ ] Photo/screenshot attachments for work quality evidence
- [ ] Blind review system (reviews hidden until both submitted)
- [ ] Public display on user profiles with response options

## 🏗️ Technical Architecture
- **Review Engine**: Dual-submission system with temporal locks
- **Rating Algorithm**: Weighted scoring across multiple dimensions
- **Content Moderation**: Automated profanity filtering and human review
- **Display System**: Profile integration with filtering and sorting

## 🗄️ Database Schema
```sql
-- Project reviews
CREATE TABLE ProjectReviews (
    Id UNIQUEIDENTIFIER PRIMARY KEY,
    ProjectId UNIQUEIDENTIFIER REFERENCES Projects(Id),
    ReviewerId UNIQUEIDENTIFIER REFERENCES Users(Id),
    RevieweeId UNIQUEIDENTIFIER REFERENCES Users(Id),
    OverallRating INT CHECK (OverallRating BETWEEN 1 AND 5),
    QualityRating INT CHECK (QualityRating BETWEEN 1 AND 5),
    CommunicationRating INT CHECK (CommunicationRating BETWEEN 1 AND 5),
    TimelinessRating INT CHECK (TimelinessRating BETWEEN 1 AND 5),
    ReviewText NVARCHAR(2000) NOT NULL,
    IsPublic BIT DEFAULT 0, -- Hidden until both reviews submitted
    CreatedAt DATETIME2 DEFAULT GETUTCDATE(),
    PublishedAt DATETIME2
);
```

## 🔗 Related Stories
- **Depends on**: US-4.1.1 Project Workspace (requires completed projects)
- **Next**: US-5.2.1 Reputation Score Calculation (uses review data)

## 📊 Implementation Status
- 🔴 **Not Started**
- **Estimated Points**: 8
- **Priority**: 🟠 High