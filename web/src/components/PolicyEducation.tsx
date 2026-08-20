'use client';

import React, { useState } from 'react';
import { Card, CardHeader, CardTitle, CardContent } from './ui/card';
import { Button } from './ui/button';

interface PolicySection {
  id: string;
  title: string;
  description: string;
  content: string;
  examples: {
    good: string[];
    bad: string[];
  };
  penalties: string[];
  icon: string;
}

const policyData: PolicySection[] = [
  {
    id: 'fake-reviews',
    title: 'Fake Reviews & Gaming',
    description: 'Understanding what constitutes review manipulation and gaming behavior',
    icon: '🎭',
    content: `
Review gaming includes any attempt to artificially inflate or manipulate ratings through:
• Creating fake positive reviews
• Coordinating with others to exchange fake reviews
• Using multiple accounts to review the same work
• Paying for or incentivizing fake reviews
• Retaliatory negative reviews
• Reviews from accounts that haven't actually used the service

Our AI systems monitor for suspicious patterns including unusual review timing, content similarity, IP addresses, and user behavior patterns.
    `,
    examples: {
      good: [
        'Leaving honest feedback based on actual project experience',
        'Providing specific details about what worked well or could improve',
        'Reviewing only projects you\'ve genuinely collaborated on',
        'Being constructive and professional in criticism'
      ],
      bad: [
        'Asking friends to leave positive reviews without working with you',
        'Creating multiple accounts to boost your ratings',
        'Copying review text from other platforms or users',
        'Offering to trade positive reviews with other users',
        'Leaving reviews immediately after account creation',
        'Using review farms or paid services'
      ]
    },
    penalties: [
      'First offense: Warning and review removal',
      'Repeated offense: Temporary review restrictions (7-30 days)',
      'Serious gaming: Account suspension (30-90 days)',
      'Commercial gaming operations: Permanent ban'
    ]
  },
  {
    id: 'content-plagiarism',
    title: 'Content Originality',
    description: 'Guidelines for original content and proper attribution',
    icon: '📝',
    content: `
All content on SkillLedger must be original or properly attributed. This includes:
• Project descriptions and proposals
• Portfolio samples and case studies
• Profile information and skills descriptions
• Messages and communications
• Review text and feedback

We use advanced similarity detection to identify copied content across the platform and from external sources.
    `,
    examples: {
      good: [
        'Writing original project descriptions in your own words',
        'Creating unique portfolio samples that showcase your work',
        'Properly attributing quotes, ideas, or inspiration from others',
        'Using your own photos and media or properly licensed content'
      ],
      bad: [
        'Copying project descriptions from other users or websites',
        'Using someone else\'s portfolio work as your own',
        'Plagiarizing content from blogs, articles, or other sources',
        'Reusing the same generic content across multiple projects'
      ]
    },
    penalties: [
      'Content removal and warning for minor violations',
      'Profile restrictions for repeated plagiarism',
      'Account suspension for systematic content theft',
      'Permanent ban for commercial plagiarism operations'
    ]
  },
  {
    id: 'identity-verification',
    title: 'Identity & Authenticity',
    description: 'Requirements for truthful identity and credential representation',
    icon: '🆔',
    content: `
Users must provide accurate identity information and honestly represent their skills and credentials:
• Real names and verified contact information
• Accurate skill levels and certifications
• Truthful work history and experience
• Legitimate business credentials
• Authentic portfolio samples

False identity information undermines trust and violates platform terms.
    `,
    examples: {
      good: [
        'Using your real name and professional identity',
        'Accurately representing your skill level (beginner, intermediate, expert)',
        'Providing legitimate certifications and credentials',
        'Showing authentic samples of your actual work'
      ],
      bad: [
        'Using fake names or impersonating others',
        'Claiming false certifications or degrees',
        'Exaggerating skill levels or experience significantly',
        'Using AI-generated or stolen portfolio samples as your own work'
      ]
    },
    penalties: [
      'Profile suspension pending verification',
      'Mandatory re-verification of identity and credentials',
      'Account restrictions until truthful information provided',
      'Permanent ban for identity fraud or impersonation'
    ]
  },
  {
    id: 'network-abuse',
    title: 'Network Integrity',
    description: 'Preventing coordinated manipulation and network abuse',
    icon: '🕸️',
    content: `
Coordinated efforts to manipulate the platform are strictly prohibited:
• Operating multiple accounts for the same individual
• Coordinating reviews, ratings, or recommendations
• Creating fake connection networks
• Using bots or automation for artificial engagement
• Sharing accounts or credentials with others

Our systems detect suspicious network patterns and coordinated behavior.
    `,
    examples: {
      good: [
        'Maintaining one account per individual',
        'Building genuine professional relationships',
        'Earning endorsements through real collaborative work',
        'Growing your network organically over time'
      ],
      bad: [
        'Operating multiple accounts to appear as different people',
        'Coordinating with others to exchange fake endorsements',
        'Using bots to automatically like, follow, or engage',
        'Creating fake accounts to inflate your network size'
      ]
    },
    penalties: [
      'All duplicate accounts permanently banned',
      'Primary account suspended pending investigation',
      'Loss of all artificially gained ratings and endorsements',
      'Permanent ban for commercial network manipulation'
    ]
  }
];

export default function PolicyEducation() {
  const [selectedPolicy, setSelectedPolicy] = useState<string>('fake-reviews');
  const [completedSections, setCompletedSections] = useState<Set<string>>(new Set());

  const currentPolicy = policyData.find(p => p.id === selectedPolicy);

  const markAsCompleted = (policyId: string) => {
    setCompletedSections(new Set(Array.from(completedSections).concat(policyId)));
  };

  const allCompleted = completedSections.size === policyData.length;

  return (
    <div className="max-w-6xl mx-auto p-6">
      <div className="mb-8">
        <h1 className="text-3xl font-bold text-foreground">Platform Policy Education</h1>
        <p className="text-muted-foreground mt-2">
          Learn about our community guidelines and how to maintain a trustworthy reputation
        </p>
      </div>

      {/* Progress Overview */}
      <Card className="mb-8">
        <CardHeader>
          <CardTitle className="flex items-center gap-2">
            📚 Learning Progress
            {allCompleted && <span className="text-success">✅</span>}
          </CardTitle>
        </CardHeader>
        <CardContent>
          <div className="flex items-center gap-4 mb-4">
            <div className="flex-1 bg-muted rounded-full h-2">
              <div
                className="bg-primary h-2 rounded-full transition-all duration-300"
                style={{ width: `${(completedSections.size / policyData.length) * 100}%` }}
              />
            </div>
            <span className="text-sm font-medium">
              {completedSections.size} of {policyData.length} completed
            </span>
          </div>

          {allCompleted && (
            <div className="bg-success/10 border border-success/20 rounded p-3">
              <p className="text-success font-medium">🎉 Congratulations!</p>
              <p className="text-success text-sm">
                You've completed all policy education sections. This helps maintain our community's trust and integrity.
              </p>
            </div>
          )}
        </CardContent>
      </Card>

      <div className="grid grid-cols-1 lg:grid-cols-4 gap-6">
        {/* Policy Navigation */}
        <Card className="lg:col-span-1">
          <CardHeader>
            <CardTitle className="text-lg">Topics</CardTitle>
          </CardHeader>
          <CardContent>
            <div className="space-y-2">
              {policyData.map(policy => (
                <button
                  key={policy.id}
                  onClick={() => setSelectedPolicy(policy.id)}
                  className={`w-full text-left p-3 rounded-lg transition-colors ${
                    selectedPolicy === policy.id
                      ? 'bg-primary/10 border-2 border-primary/20 text-primary'
                      : 'bg-muted hover:bg-muted/80 border-2 border-transparent'
                  }`}
                >
                  <div className="flex items-center justify-between">
                    <div className="flex items-center gap-2">
                      <span className="text-lg">{policy.icon}</span>
                      <span className="font-medium text-sm">{policy.title}</span>
                    </div>
                    {completedSections.has(policy.id) && (
                      <span className="text-success text-sm">✓</span>
                    )}
                  </div>
                </button>
              ))}
            </div>
          </CardContent>
        </Card>

        {/* Policy Content */}
        <div className="lg:col-span-3">
          {currentPolicy && (
            <div className="space-y-6">
              {/* Header */}
              <Card>
                <CardHeader>
                  <div className="flex items-center gap-3">
                    <span className="text-3xl">{currentPolicy.icon}</span>
                    <div>
                      <CardTitle className="text-2xl">{currentPolicy.title}</CardTitle>
                      <p className="text-muted-foreground mt-1">{currentPolicy.description}</p>
                    </div>
                  </div>
                </CardHeader>
              </Card>

              {/* Overview */}
              <Card>
                <CardHeader>
                  <CardTitle>Overview</CardTitle>
                </CardHeader>
                <CardContent>
                  <div className="prose max-w-none">
                    <div className="whitespace-pre-line text-muted-foreground">
                      {currentPolicy.content}
                    </div>
                  </div>
                </CardContent>
              </Card>

              {/* Examples */}
              <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
                <Card>
                  <CardHeader>
                    <CardTitle className="flex items-center gap-2 text-success">
                      ✅ Good Practices
                    </CardTitle>
                  </CardHeader>
                  <CardContent>
                    <ul className="space-y-2">
                      {currentPolicy.examples.good.map((example, index) => (
                        <li key={index} className="flex items-start gap-2">
                          <span className="text-success mt-1">•</span>
                          <span className="text-sm text-muted-foreground">{example}</span>
                        </li>
                      ))}
                    </ul>
                  </CardContent>
                </Card>

                <Card>
                  <CardHeader>
                    <CardTitle className="flex items-center gap-2 text-destructive">
                      ❌ Violations
                    </CardTitle>
                  </CardHeader>
                  <CardContent>
                    <ul className="space-y-2">
                      {currentPolicy.examples.bad.map((example, index) => (
                        <li key={index} className="flex items-start gap-2">
                          <span className="text-destructive mt-1">•</span>
                          <span className="text-sm text-muted-foreground">{example}</span>
                        </li>
                      ))}
                    </ul>
                  </CardContent>
                </Card>
              </div>

              {/* Penalties */}
              <Card>
                <CardHeader>
                  <CardTitle className="flex items-center gap-2">
                    ⚖️ Enforcement & Penalties
                  </CardTitle>
                </CardHeader>
                <CardContent>
                  <div className="bg-warning/10 border border-warning/20 rounded p-4">
                    <p className="font-medium text-warning mb-2">
                      Violations of this policy may result in:
                    </p>
                    <ul className="space-y-1">
                      {currentPolicy.penalties.map((penalty, index) => (
                        <li key={index} className="flex items-start gap-2">
                          <span className="text-warning mt-1">•</span>
                          <span className="text-sm text-warning">{penalty}</span>
                        </li>
                      ))}
                    </ul>
                  </div>
                </CardContent>
              </Card>

              {/* Action Button */}
              <Card>
                <CardContent className="pt-6">
                  <div className="flex items-center justify-between">
                    <p className="text-sm text-muted-foreground">
                      {completedSections.has(currentPolicy.id)
                        ? 'You have completed this section'
                        : 'Mark as completed once you\'ve read and understood this policy'
                      }
                    </p>
                    <Button
                      onClick={() => markAsCompleted(currentPolicy.id)}
                      disabled={completedSections.has(currentPolicy.id)}
                      variant={completedSections.has(currentPolicy.id) ? "outline" : "default"}
                    >
                      {completedSections.has(currentPolicy.id) ? 'Completed ✓' : 'Mark Complete'}
                    </Button>
                  </div>
                </CardContent>
              </Card>
            </div>
          )}
        </div>
      </div>

      {/* Quick Reference */}
      <Card className="mt-8">
        <CardHeader>
          <CardTitle>Quick Reference</CardTitle>
        </CardHeader>
        <CardContent>
          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4">
            <div className="text-center p-4 bg-success/10 rounded-lg">
              <div className="text-2xl mb-2">✅</div>
              <div className="font-medium text-success">Be Authentic</div>
              <div className="text-xs text-success">Use real identity and honest skills</div>
            </div>
            <div className="text-center p-4 bg-primary/10 rounded-lg">
              <div className="text-2xl mb-2">🤝</div>
              <div className="font-medium text-primary">Earn Trust</div>
              <div className="text-xs text-primary">Build genuine professional relationships</div>
            </div>
            <div className="text-center p-4 bg-accent/10 rounded-lg">
              <div className="text-2xl mb-2">📝</div>
              <div className="font-medium text-accent-foreground">Create Original</div>
              <div className="text-xs text-accent-foreground">Write unique content and reviews</div>
            </div>
            <div className="text-center p-4 bg-warning/10 rounded-lg">
              <div className="text-2xl mb-2">🛡️</div>
              <div className="font-medium text-warning">Follow Rules</div>
              <div className="text-xs text-warning">Respect platform policies and community</div>
            </div>
          </div>
        </CardContent>
      </Card>
    </div>
  );
}