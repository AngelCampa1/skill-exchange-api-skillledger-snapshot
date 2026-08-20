# US-5.2.1: Reputation Score Calculation

## 📋 User Story
**As a** platform user  
**I want** a comprehensive reputation score that reflects my project history  
**So that** my credibility is accurately represented to potential collaborators

## ✅ Acceptance Criteria
- [x] Weighted algorithm combining multiple factors (reviews, completion rate, responsiveness)
- [x] Separate scores for different skill categories
- [x] Decay mechanism for very old reviews to reflect current performance
- [x] Bonus points for consistent high performance streaks
- [x] Penalty system for project cancellations or disputes
- [x] Transparent score breakdown for users to understand their rating

## 🏗️ Technical Architecture
- **Scoring Algorithm**: Multi-factor weighted calculation with time decay
- **Real-time Updates**: Score recalculation on new review or project completion
- **Category-based**: Different scores for different types of work
- **Performance Analytics**: Trend tracking and improvement suggestions

## 🗄️ Database Schema
```sql
-- User reputation scores
CREATE TABLE UserReputationScores (
    Id UNIQUEIDENTIFIER PRIMARY KEY,
    UserId UNIQUEIDENTIFIER UNIQUE REFERENCES Users(Id),
    OverallScore DECIMAL(4,2) CHECK (OverallScore BETWEEN 0 AND 5),
    ProjectCompletionRate DECIMAL(3,2), -- 0.00 to 1.00
    AverageResponseTime INT, -- Hours
    TotalProjectsCompleted INT DEFAULT 0,
    LastUpdated DATETIME2 DEFAULT GETUTCDATE()
);

-- Category-specific reputation
CREATE TABLE CategoryReputationScores (
    Id UNIQUEIDENTIFIER PRIMARY KEY,
    UserId UNIQUEIDENTIFIER REFERENCES Users(Id),
    SkillCategoryId UNIQUEIDENTIFIER REFERENCES SkillCategories(Id),
    Score DECIMAL(4,2) CHECK (Score BETWEEN 0 AND 5),
    ProjectCount INT DEFAULT 0,
    LastProjectAt DATETIME2
);
```

## 🔗 Related Stories
- **Depends on**: US-5.1.1 Project Review System (requires review data)
- **Next**: US-5.3.1 Anti-Gaming & Fraud Detection (protects score integrity)

## 📊 Implementation Status
- ✅ **COMPLETED**
- **Delivered Points**: 13
- **Priority**: 🟠 High
- **Completion Date**: January 11, 2025
- **Test Coverage**: 38/38 reputation tests passing (100%)
- **Build Status**: Successful with zero compilation errors