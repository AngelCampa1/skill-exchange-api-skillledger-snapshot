'use client'

import React from'react'
import Link from'next/link'
import { cn } from'@/lib/utils'
import {
  TrendingUp,
  Users,
  Briefcase,
  MessageSquare,
  Star,
  FolderPlus,
  Search,
  Wallet,
  BarChart3,
  Activity,
  Target,
  Zap,
  ArrowUpRight,
  ArrowRight,
  CheckCircle2
} from'lucide-react'

interface StatCardProps {
  title: string
  value: string | number
  change: string
  icon: React.ComponentType<{ className?: string }>
  trend?:'up' |'down' |'neutral'
  color?:'green' |'blue' |'purple' |'orange'
  animated?: boolean
}

interface ActivityItemProps {
  icon: React.ComponentType<{ className?: string }>
  title: string
  description: string
  time: string
  status:'completed' |'in-progress' |'pending'
}

interface QuickActionProps {
  title: string
  description: string
  icon: React.ComponentType<{ className?: string }>
  href: string
  status?:'available' |'coming-soon' |'premium'
  badge?: string
  gradient?:'primary' |'secondary' |'accent'
}

function StatCard({ title, value, change, icon: IconComponent, trend ='neutral', color ='blue', animated = true }: StatCardProps) {
  return (
    <div className={cn("card-interactive p-6 relative overflow-hidden group cursor-pointer",
      animated &&"hover:lift hover:scale-105"
    )}>
      {/* Animated Gradient Background */}
      <div className="absolute inset-0 bg-gradient-to-br from-primary/5 to-transparent opacity-0 group-hover:opacity-100 transition-opacity duration-500"></div>

      {/* Animated Icon */}
      <div className="relative z-10 mb-4">
        <div className={cn("w-12 h-12 rounded-2xl flex items-center justify-center transition-all duration-300 group-hover:scale-110",
          color ==='green' &&"bg-gradient-to-br from-success to-success",
          color ==='blue' &&"bg-gradient-to-br from-info to-info",
          color ==='purple' &&"bg-gradient-to-br from-primary to-primary",
          color ==='orange' &&"bg-gradient-to-br from-warning to-warning"
        )}>
          <IconComponent className={cn("w-6 h-6 animate-float-3d",
            color ==='green' &&"text-success-foreground",
            color ==='blue' &&"text-info-foreground",
            color ==='purple' &&"text-primary-foreground",
            color ==='orange' &&"text-warning-foreground"
          )} />
        </div>

        {/* Trend Indicator */}
        {trend !=='neutral' && (
          <div className="absolute -top-2 -right-2">
            <div className={cn("flex items-center px-2 py-1 text-xs font-bold rounded-full",
              trend ==='up' &&"bg-success/10 text-success",
              trend ==='down' &&"bg-destructive/10 text-destructive"
            )}>
              {trend ==='up' ? (
                <ArrowUpRight className="w-3 h-3 animate-bounce-in" />
              ) : (
                <ArrowUpRight className="w-3 h-3 rotate-180 animate-bounce-in" />
              )}
            </div>
          </div>
        )}
      </div>

      <div className="space-y-3 relative z-10">
        <h3 className="text-subheading text-foreground group-hover:text-primary transition-colors duration-300">{title}</h3>
        <div className="flex items-baseline justify-between">
          <span className="text-4xl font-bold tracking-tight text-foreground">{value}</span>
          <span className={cn("text-sm font-medium",
            trend ==='up' &&"text-success",
            trend ==='down' &&"text-destructive",
            trend ==='neutral' &&"text-muted-foreground"
          )}>
            {change}
          </span>
        </div>

        {/* Animated Progress Bar */}
        <div className="mt-4 h-2 bg-muted rounded-full overflow-hidden">
          <div className={cn("h-full rounded-full transition-all duration-1000",
            trend ==='up' &&"bg-gradient-to-r from-success to-success animate-progress-pulse",
            trend ==='down' &&"bg-gradient-to-r from-destructive to-destructive animate-progress-pulse",
            trend ==='neutral' &&"bg-muted"
          )}></div>
        </div>
      </div>
    </div>
  )
}

function ActivityItem({ icon: IconComponent, title, description, time, status ='pending' }: ActivityItemProps) {
  return (
    <div className="flex items-start space-x-4 p-4 card-premium hover:lift-subtle transition-all duration-300 group">
      <div className="relative">
        <div className={cn("w-10 h-10 rounded-2xl flex items-center justify-center transition-all duration-300",
          status ==='completed' &&"bg-gradient-to-br from-success to-success",
          status ==='in-progress' &&"bg-gradient-to-br from-info to-info",
          status ==='pending' &&"bg-muted"
        )}>
          <IconComponent className={cn("w-5 h-5 transition-all duration-300",
            status ==='completed' &&"text-success-foreground animate-scale-rotate-in",
            status ==='in-progress' &&"text-info-foreground animate-pulse",
            status ==='pending' &&"text-muted-foreground"
          )} />
        </div>

        {/* Status Indicator */}
        {status ==='completed' && (
          <div className="absolute -top-1 -right-1">
            <CheckCircle2 className="w-4 h-4 text-success animate-bounce-in" />
          </div>
        )}

        {status ==='in-progress' && (
          <div className="absolute -top-1 -right-1">
            <div className="w-4 h-4 bg-info rounded-full animate-ping"></div>
          </div>
        )}
      </div>

      <div className="flex-1 space-y-2">
        <h4 className="text-body font-semibold text-foreground group-hover:text-primary transition-colors duration-300">{title}</h4>
        <p className="text-sm text-muted-foreground">{description}</p>
        <div className="flex items-center space-x-2 text-xs text-muted-foreground">
          <Activity className="w-3 h-3" />
          <span>{time}</span>
        </div>
      </div>
    </div>
  )
}

function QuickAction({ title, description, icon: IconComponent, href, status ='available', badge, gradient ='primary' }: QuickActionProps) {
  const isDisabled = status ==='coming-soon' || status ==='premium'

  return (
    <Link
      href={href}
      className={cn("card-interactive p-8 text-center space-golden-sm relative overflow-hidden transition-all duration-300",
        !isDisabled &&"hover:lift hover:scale-105 cursor-pointer",
        isDisabled &&"opacity-60 cursor-not-allowed"
      )}
    >
      {/* Gradient Background Effect */}
      <div className="absolute inset-0 bg-gradient-to-br opacity-0 transition-opacity duration-500 group-hover:opacity-100"
        style={{
          backgroundImage: gradient ==='primary'
            ?'linear-gradient(135deg, hsl(var(--primary) / 0.1), hsl(var(--secondary) / 0.1))'
            : gradient ==='secondary'
            ?'linear-gradient(135deg, hsl(var(--secondary) / 0.1), hsl(var(--primary) / 0.1))'
            :'linear-gradient(135deg, hsl(var(--accent) / 0.1), hsl(var(--primary) / 0.1))'
        }}
      />

      <div className="relative z-10">
        {/* Animated Icon */}
        <div className="flex justify-center mb-6">
          <div className={cn("w-14 h-14 rounded-3xl flex items-center justify-center transition-all duration-300 group-hover:scale-110 shadow-lg group-hover:shadow-primary/30",
            gradient ==='primary' &&"bg-gradient-to-br from-primary to-primary animate-gradient-shift",
            gradient ==='secondary' &&"bg-gradient-to-br from-secondary to-secondary animate-gradient-shift",
            gradient ==='accent' &&"bg-gradient-to-br from-accent to-accent animate-gradient-shift"
          )}>
            <IconComponent className={cn("w-7 h-7 group-hover:animate-wiggle",
              gradient ==='primary' &&"text-primary-foreground",
              gradient ==='secondary' &&"text-secondary-foreground",
              gradient ==='accent' &&"text-accent-foreground"
            )} />
          </div>
        </div>

        {/* Badge */}
        {badge && (
          <div className="absolute -top-2 -right-2 animate-bounce-in">
            <span className="px-3 py-1 text-xs font-bold bg-gradient-to-r from-primary to-secondary text-primary-foreground rounded-full shadow-md">
              {badge}
            </span>
          </div>
        )}

        {/* Content */}
        <div className="space-md">
          <h3 className="text-subheading text-foreground group-hover:text-primary transition-colors duration-300">{title}</h3>
          <p className="text-body text-muted-foreground leading-relaxed">{description}</p>

          {/* Status-specific content */}
          {status ==='coming-soon' && (
            <div className="pt-4 border-t border-border/30">
              <div className="flex items-center space-x-2">
                <Target className="w-4 h-4 text-muted-foreground animate-pulse" />
                <span className="text-sm text-muted-foreground">Coming soon</span>
              </div>
            </div>
          )}

          {status ==='premium' && (
            <div className="pt-4 border-t border-border/30">
              <div className="flex items-center space-x-2">
                <Zap className="w-4 h-4 text-primary animate-heartbeat" />
                <span className="text-sm text-primary font-semibold">Premium</span>
              </div>
            </div>
          )}

          {status ==='available' && (
            <button className="btn-primary text-sm mt-6 w-full animate-scale-rotate-in hover:scale-105 transition-transform duration-300">
              Get Started
              <ArrowRight className="ml-2 w-4 h-4" />
            </button>
          )}
        </div>
      </div>

      {/* Hover Glow Effect */}
      <div className="absolute inset-0 opacity-0 group-hover:opacity-100 transition-opacity duration-500">
        <div className="absolute inset-0 bg-primary/20 rounded-2xl blur-xl animate-pulse-glow"></div>
      </div>
    </Link>
  )
}

export function EnhancedDashboardContent() {
  return (
    <div className="space-y-10 stagger-children">
      {/* Stats Grid */}
      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6">
        <StatCard
          title="Total Projects"
          value="24"
          change="+12% from last month"
          icon={Briefcase}
          trend="up"
          color="blue"
        />
        <StatCard
          title="Active Collaborations"
          value="142"
          change="+8% from last week"
          icon={Users}
          trend="up"
          color="green"
        />
        <StatCard
          title="Credits Available"
          value="12,450"
          change="+2,340 this month"
          icon={Wallet}
          trend="up"
          color="purple"
        />
        <StatCard
          title="Success Rate"
          value="94%"
          change="+2% from last quarter"
          icon={Target}
          trend="up"
          color="orange"
        />
      </div>

      {/* Recent Activity */}
      <div className="card-elevated p-8 animate-slide-in">
        <div className="space-golden-lg">
          <div className="flex items-center justify-between mb-6">
            <h2 className="text-heading text-foreground">
              <span className="bg-gradient-to-r from-primary to-secondary bg-clip-text text-transparent">
                Recent Activity
              </span>
            </h2>
            <Link href="/dashboard" className="btn-ghost text-sm hover:scale-105 transition-transform duration-300">
              View All
              <ArrowRight className="ml-2 w-4 h-4" />
            </Link>
          </div>

          <div className="space-golden-md">
            <ActivityItem
              icon={FolderPlus}
              title="Project Alpha Launched"
              description="Successfully initiated new collaboration project with team of 5"
              time="2 hours ago"
              status="completed"
            />
            <ActivityItem
              icon={MessageSquare}
              title="Contract Negotiated"
              description="Finalized terms for web development project"
              time="5 hours ago"
              status="completed"
            />
            <ActivityItem
              icon={Star}
              title="Milestone Achieved"
              description="Completed first phase of mobile app development"
              time="1 day ago"
              status="in-progress"
            />
            <ActivityItem
              icon={BarChart3}
              title="Performance Review"
              description="Q4 analytics and performance metrics review"
              time="3 days ago"
              status="pending"
            />
          </div>
        </div>
      </div>

      {/* Quick Actions */}
      <div className="space-y-6">
        <div className="space-y-3">
          <h2 className="text-heading text-foreground">
            <span className="bg-gradient-to-r from-primary to-secondary bg-clip-text text-transparent">
              Quick Actions
            </span>
          </h2>
          <p className="text-lg text-muted-foreground">Access key features and launch your next collaboration</p>
        </div>

        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-8">
          <QuickAction
            title="Create Project"
            description="Start a new collaboration with advanced tools"
            icon={FolderPlus}
            href="/create-project"
            status="available"
            gradient="primary"
          />
          <QuickAction
            title="Browse Projects"
            description="Discover opportunities from talented professionals"
            icon={Search}
            href="/projects/search"
            status="available"
            gradient="secondary"
          />
          <QuickAction
            title="Join Collaboration"
            description="Connect with other professionals in your field"
            icon={Users}
            href="/projects/search"
            status="premium"
            badge="PRO"
          />
          <QuickAction
            title="Premium Analytics"
            description="Advanced insights and detailed reporting"
            icon={BarChart3}
            href="/dashboard"
            status="coming-soon"
            gradient="accent"
          />
        </div>
      </div>

      {/* Enhanced Wallet Preview */}
      <div className="card-interactive p-8 hover:lift hover:scale-102 transition-all duration-300">
        <div className="flex items-center justify-between mb-6">
          <h3 className="text-subheading text-foreground">Wallet Overview</h3>
          <div className="flex items-center space-x-2">
            <div className="relative">
              <Wallet className="w-5 h-5 text-primary animate-pulse" />
              <div className="absolute -top-1 -right-1 w-3 h-3 bg-primary-foreground rounded-full animate-ping"></div>
            </div>
            <span className="text-sm text-muted-foreground">Active Balance</span>
          </div>
          <Link href="/wallet" className="btn-primary text-sm hover:scale-105 transition-transform duration-300">
            Manage Wallet
            <ArrowRight className="ml-2 w-4 h-4" />
          </Link>
        </div>

        <div className="grid grid-cols-3 gap-4">
          <div className="text-center space-y-2">
            <div className="text-3xl font-bold text-primary">12,450</div>
            <div className="text-sm text-muted-foreground">Available Credits</div>
          </div>
          <div className="text-center space-y-2">
            <div className="text-3xl font-bold text-success">+2,340</div>
            <div className="text-sm text-muted-foreground">Monthly Growth</div>
          </div>
          <div className="text-center space-y-2">
            <div className="text-3xl font-bold text-primary">94%</div>
            <div className="text-sm text-muted-foreground">Success Rate</div>
          </div>
        </div>
      </div>

      {/* Trending Projects */}
      <div className="card-interactive p-8 hover:lift hover:scale-102 transition-all duration-300">
        <div className="flex items-center justify-between mb-6">
          <h3 className="text-subheading text-foreground">Trending Projects</h3>
          <Link href="/projects/search" className="btn-ghost text-sm hover:scale-105 transition-transform duration-300">
            View All
            <TrendingUp className="ml-2 w-4 h-4" />
          </Link>
        </div>

        <div className="space-golden-md">
          <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
            <div className="flex items-center space-x-3 p-4 card-premium hover:lift-subtle transition-all duration-300">
              <div className="w-10 h-10 bg-gradient-to-br from-info to-info rounded-xl flex items-center justify-center animate-pulse">
                <Briefcase className="w-5 h-5 text-info-foreground" />
              </div>
              <div>
                <h4 className="text-body font-semibold text-foreground">Mobile App Development</h4>
                <p className="text-sm text-muted-foreground">React Native • iOS & Android</p>
                <div className="flex items-center justify-between mt-2">
                  <span className="text-xs text-muted-foreground">15 collaborators</span>
                  <span className="status-success text-xs">Active</span>
                </div>
              </div>
            </div>
            <div className="flex items-center space-x-3 p-4 card-premium hover:lift-subtle transition-all duration-300">
              <div className="w-10 h-10 bg-gradient-to-br from-success to-success rounded-xl flex items-center justify-center animate-pulse">
                <Star className="w-5 h-5 text-success-foreground" />
              </div>
              <div>
                <h4 className="text-body font-semibold text-foreground">Design System</h4>
                <p className="text-sm text-muted-foreground">UI/UX • Figma • Prototyping</p>
                <div className="flex items-center justify-between mt-2">
                  <span className="text-xs text-muted-foreground">8 collaborators</span>
                  <span className="status-success text-xs">Active</span>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  )
}