import React from 'react'
import { render, screen } from '@testing-library/react'
import { StatCard, StatGroup, CompactStat, MetricComparison, KPICard } from '../stats'

// Mock lucide-react icons
jest.mock('lucide-react', () => ({
  TrendingUp: ({ className }: any) => <svg data-testid="trending-up" className={className} />,
  TrendingDown: ({ className }: any) => <svg data-testid="trending-down" className={className} />,
  Minus: ({ className }: any) => <svg data-testid="minus" className={className} />,
  ArrowUpRight: ({ className }: any) => <svg data-testid="arrow-up-right" className={className} />,
  ArrowDownRight: ({ className }: any) => <svg data-testid="arrow-down-right" className={className} />,
}))

describe('StatCard Component', () => {
  // ============================================
  // Initial Render (5 tests)
  // ============================================
  describe('Initial Render', () => {
    it('should render with required props', () => {
      render(<StatCard label="Total Users" value="1,234" />)

      expect(screen.getByText('Total Users')).toBeInTheDocument()
      expect(screen.getByText('1,234')).toBeInTheDocument()
    })

    it('should render with numeric value', () => {
      render(<StatCard label="Revenue" value={5000} />)

      expect(screen.getByText('5000')).toBeInTheDocument()
    })

    it('should render subtitle when provided', () => {
      render(<StatCard label="Sales" value="100" subtitle="Last 30 days" />)

      expect(screen.getByText('Last 30 days')).toBeInTheDocument()
    })

    it('should render custom icon when provided', () => {
      const CustomIcon = () => <div data-testid="custom-icon">Icon</div>

      render(<StatCard label="Test" value="123" icon={<CustomIcon />} />)

      expect(screen.getByTestId('custom-icon')).toBeInTheDocument()
    })

    it('should apply custom className', () => {
      const { container } = render(<StatCard label="Test" value="123" className="custom-class" />)

      const card = container.firstChild
      expect(card).toHaveClass('custom-class')
    })
  })

  // ============================================
  // Variants (5 tests)
  // ============================================
  describe('Variants', () => {
    it('should apply default variant styling', () => {
      const { container } = render(<StatCard label="Test" value="123" variant="default" />)

      const card = container.querySelector('.border-border')
      expect(card).toBeInTheDocument()
    })

    it('should apply primary variant styling', () => {
      const { container } = render(<StatCard label="Test" value="123" variant="primary" />)

      const card = container.querySelector('.border-primary\\/20')
      expect(card).toBeInTheDocument()
    })

    it('should apply success variant styling', () => {
      const { container } = render(<StatCard label="Test" value="123" variant="success" />)

      const card = container.querySelector('.border-success\\/20')
      expect(card).toBeInTheDocument()
    })

    it('should apply warning variant styling', () => {
      const { container } = render(<StatCard label="Test" value="123" variant="warning" />)

      const card = container.querySelector('.border-warning\\/20')
      expect(card).toBeInTheDocument()
    })

    it('should apply error variant styling', () => {
      const { container } = render(<StatCard label="Test" value="123" variant="error" />)

      const card = container.querySelector('.border-destructive\\/20')
      expect(card).toBeInTheDocument()
    })
  })

  // ============================================
  // Trend Display (8 tests)
  // ============================================
  describe('Trend Display', () => {
    it('should display positive trend with TrendingUp icon', () => {
      render(<StatCard label="Test" value="123" trend={{ value: 12.5 }} />)

      expect(screen.getByTestId('trending-up')).toBeInTheDocument()
      expect(screen.getByText('12.5%')).toBeInTheDocument()
    })

    it('should display negative trend with TrendingDown icon', () => {
      render(<StatCard label="Test" value="123" trend={{ value: -8.3 }} />)

      expect(screen.getByTestId('trending-down')).toBeInTheDocument()
      expect(screen.getByText('8.3%')).toBeInTheDocument()
    })

    it('should display zero trend with Minus icon', () => {
      render(<StatCard label="Test" value="123" trend={{ value: 0 }} />)

      expect(screen.getByTestId('minus')).toBeInTheDocument()
      expect(screen.getByText('0%')).toBeInTheDocument()
    })

    it('should display trend label when provided', () => {
      render(<StatCard label="Test" value="123" trend={{ value: 10, label: 'from last month' }} />)

      expect(screen.getByText('from last month')).toBeInTheDocument()
    })

    it('should show positive trend when isPositive is true', () => {
      render(<StatCard label="Test" value="123" trend={{ value: -5, isPositive: true }} />)

      // Should show TrendingUp even though value is negative
      expect(screen.getByTestId('trending-up')).toBeInTheDocument()
    })

    it('should show negative trend when isPositive is false', () => {
      render(<StatCard label="Test" value="123" trend={{ value: 10, isPositive: false }} />)

      // Should show TrendingDown even though value is positive
      expect(screen.getByTestId('trending-down')).toBeInTheDocument()
    })

    it('should apply success color to positive trends', () => {
      const { container } = render(<StatCard label="Test" value="123" trend={{ value: 15 }} />)

      const trendElement = container.querySelector('.text-success')
      expect(trendElement).toBeInTheDocument()
    })

    it('should apply destructive color to negative trends', () => {
      const { container } = render(<StatCard label="Test" value="123" trend={{ value: -15 }} />)

      const trendElement = container.querySelector('.text-destructive')
      expect(trendElement).toBeInTheDocument()
    })
  })

  // ============================================
  // Loading State (3 tests)
  // ============================================
  describe('Loading State', () => {
    it('should show loading skeleton when loading', () => {
      const { container } = render(<StatCard label="Test" value="123" loading />)

      const skeleton = container.querySelector('.animate-pulse')
      expect(skeleton).toBeInTheDocument()
    })

    it('should not show value when loading', () => {
      render(<StatCard label="Test" value="123" loading />)

      expect(screen.queryByText('123')).not.toBeInTheDocument()
    })

    it('should not show trend when loading', () => {
      render(<StatCard label="Test" value="123" loading trend={{ value: 10 }} />)

      expect(screen.queryByText('10%')).not.toBeInTheDocument()
    })
  })

  // ============================================
  // Integration (2 tests)
  // ============================================
  describe('Integration', () => {
    it('should render complete stat card with all features', () => {
      const CustomIcon = () => <div data-testid="icon">$</div>

      render(
        <StatCard
          label="Revenue"
          value="$45,231"
          subtitle="Total earnings"
          trend={{ value: 12.5, label: 'from last month' }}
          variant="success"
          icon={<CustomIcon />}
          className="custom"
        />
      )

      expect(screen.getByText('Revenue')).toBeInTheDocument()
      expect(screen.getByText('$45,231')).toBeInTheDocument()
      expect(screen.getByText('Total earnings')).toBeInTheDocument()
      expect(screen.getByText('12.5%')).toBeInTheDocument()
      expect(screen.getByText('from last month')).toBeInTheDocument()
      expect(screen.getByTestId('icon')).toBeInTheDocument()
    })

    it('should handle all props together without conflicts', () => {
      render(
        <StatCard
          label="Active Users"
          value={1234}
          subtitle="Online now"
          trend={{ value: -5.2, isPositive: false, label: 'vs yesterday' }}
          variant="primary"
          loading={false}
        />
      )

      expect(screen.getByText('Active Users')).toBeInTheDocument()
      expect(screen.getByText('1234')).toBeInTheDocument()
      expect(screen.getByText('Online now')).toBeInTheDocument()
    })
  })
})

describe('StatGroup Component', () => {
  const mockStats = [
    { label: 'Stat 1', value: '100' },
    { label: 'Stat 2', value: '200' },
    { label: 'Stat 3', value: '300' },
  ]

  // ============================================
  // Initial Render (3 tests)
  // ============================================
  describe('Initial Render', () => {
    it('should render all stats', () => {
      render(<StatGroup stats={mockStats} />)

      expect(screen.getByText('Stat 1')).toBeInTheDocument()
      expect(screen.getByText('Stat 2')).toBeInTheDocument()
      expect(screen.getByText('Stat 3')).toBeInTheDocument()
    })

    it('should render with default 3-column grid', () => {
      const { container } = render(<StatGroup stats={mockStats} />)

      const grid = container.querySelector('.grid-cols-1.md\\:grid-cols-2.lg\\:grid-cols-3')
      expect(grid).toBeInTheDocument()
    })

    it('should apply custom className', () => {
      const { container } = render(<StatGroup stats={mockStats} className="custom-class" />)

      const grid = container.firstChild
      expect(grid).toHaveClass('custom-class')
    })
  })

  // ============================================
  // Grid Columns (4 tests)
  // ============================================
  describe('Grid Columns', () => {
    it('should render 1-column grid', () => {
      const { container } = render(<StatGroup stats={mockStats} columns={1} />)

      const grid = container.querySelector('.grid-cols-1')
      expect(grid).toBeInTheDocument()
    })

    it('should render 2-column grid', () => {
      const { container } = render(<StatGroup stats={mockStats} columns={2} />)

      const grid = container.querySelector('.grid-cols-1.md\\:grid-cols-2')
      expect(grid).toBeInTheDocument()
    })

    it('should render 3-column grid', () => {
      const { container } = render(<StatGroup stats={mockStats} columns={3} />)

      const grid = container.querySelector('.grid-cols-1.md\\:grid-cols-2.lg\\:grid-cols-3')
      expect(grid).toBeInTheDocument()
    })

    it('should render 4-column grid', () => {
      const { container } = render(<StatGroup stats={mockStats} columns={4} />)

      const grid = container.querySelector('.grid-cols-1.md\\:grid-cols-2.lg\\:grid-cols-4')
      expect(grid).toBeInTheDocument()
    })
  })

  // ============================================
  // Integration (1 test)
  // ============================================
  describe('Integration', () => {
    it('should render stats with all their props', () => {
      const statsWithProps = [
        { label: 'Revenue', value: '$100', variant: 'success' as const, trend: { value: 10 } },
        { label: 'Users', value: '50', variant: 'primary' as const },
      ]

      render(<StatGroup stats={statsWithProps} columns={2} />)

      expect(screen.getByText('Revenue')).toBeInTheDocument()
      expect(screen.getByText('$100')).toBeInTheDocument()
      expect(screen.getByText('Users')).toBeInTheDocument()
      expect(screen.getByText('50')).toBeInTheDocument()
    })
  })
})

describe('CompactStat Component', () => {
  // ============================================
  // Initial Render (4 tests)
  // ============================================
  describe('Initial Render', () => {
    it('should render with required props', () => {
      render(<CompactStat label="Total Sales" value="$5,000" />)

      expect(screen.getByText('Total Sales')).toBeInTheDocument()
      expect(screen.getByText('$5,000')).toBeInTheDocument()
    })

    it('should render numeric value', () => {
      render(<CompactStat label="Count" value={42} />)

      expect(screen.getByText('42')).toBeInTheDocument()
    })

    it('should render custom icon', () => {
      const Icon = () => <div data-testid="custom-icon">Icon</div>

      render(<CompactStat label="Test" value="100" icon={<Icon />} />)

      expect(screen.getByTestId('custom-icon')).toBeInTheDocument()
    })

    it('should apply custom className', () => {
      const { container } = render(<CompactStat label="Test" value="100" className="custom" />)

      const wrapper = container.firstChild
      expect(wrapper).toHaveClass('custom')
    })
  })

  // ============================================
  // Size Variants (3 tests)
  // ============================================
  describe('Size Variants', () => {
    it('should apply small size classes', () => {
      const { container } = render(<CompactStat label="Test" value="100" size="sm" />)

      const label = container.querySelector('.text-xs')
      const value = container.querySelector('.text-lg')
      expect(label).toBeInTheDocument()
      expect(value).toBeInTheDocument()
    })

    it('should apply medium size classes (default)', () => {
      const { container } = render(<CompactStat label="Test" value="100" size="md" />)

      const label = container.querySelector('.text-sm')
      const value = container.querySelector('.text-2xl')
      expect(label).toBeInTheDocument()
      expect(value).toBeInTheDocument()
    })

    it('should apply large size classes', () => {
      const { container } = render(<CompactStat label="Test" value="100" size="lg" />)

      const label = container.querySelector('.text-base')
      const value = container.querySelector('.text-3xl')
      expect(label).toBeInTheDocument()
      expect(value).toBeInTheDocument()
    })
  })

  // ============================================
  // Trend Display (4 tests)
  // ============================================
  describe('Trend Display', () => {
    it('should display positive trend', () => {
      render(<CompactStat label="Test" value="100" trend={15.5} />)

      expect(screen.getByText(/↑/)).toBeInTheDocument()
      expect(screen.getByText(/15.5%/)).toBeInTheDocument()
    })

    it('should display negative trend', () => {
      render(<CompactStat label="Test" value="100" trend={-8.2} />)

      expect(screen.getByText(/↓/)).toBeInTheDocument()
      expect(screen.getByText(/8.2%/)).toBeInTheDocument()
    })

    it('should not display zero trend', () => {
      render(<CompactStat label="Test" value="100" trend={0} />)

      expect(screen.queryByText(/↑/)).not.toBeInTheDocument()
      expect(screen.queryByText(/↓/)).not.toBeInTheDocument()
    })

    it('should apply correct colors to trends', () => {
      const { container, rerender } = render(<CompactStat label="Test" value="100" trend={10} />)

      let trendElement = container.querySelector('.text-success')
      expect(trendElement).toBeInTheDocument()

      rerender(<CompactStat label="Test" value="100" trend={-10} />)

      trendElement = container.querySelector('.text-destructive')
      expect(trendElement).toBeInTheDocument()
    })
  })

  // ============================================
  // Integration (1 test)
  // ============================================
  describe('Integration', () => {
    it('should render complete compact stat', () => {
      const Icon = () => <div data-testid="icon">📊</div>

      render(
        <CompactStat
          label="Revenue"
          value="$12,345"
          icon={<Icon />}
          trend={8.5}
          size="lg"
          className="custom"
        />
      )

      expect(screen.getByText('Revenue')).toBeInTheDocument()
      expect(screen.getByText('$12,345')).toBeInTheDocument()
      expect(screen.getByTestId('icon')).toBeInTheDocument()
      expect(screen.getByText(/8.5%/)).toBeInTheDocument()
    })
  })
})

describe('MetricComparison Component', () => {
  const mockData = {
    current: { label: 'This Month', value: 5000, period: 'Jan 2024' },
    previous: { label: 'Last Month', value: 4000, period: 'Dec 2023' },
  }

  // ============================================
  // Initial Render (4 tests)
  // ============================================
  describe('Initial Render', () => {
    it('should render current period data', () => {
      render(<MetricComparison {...mockData} />)

      expect(screen.getByText('This Month')).toBeInTheDocument()
      expect(screen.getByText('5,000')).toBeInTheDocument()
      expect(screen.getByText('Jan 2024')).toBeInTheDocument()
    })

    it('should render previous period data', () => {
      render(<MetricComparison {...mockData} />)

      expect(screen.getByText('Last Month')).toBeInTheDocument()
      expect(screen.getByText('4,000')).toBeInTheDocument()
      expect(screen.getByText('Dec 2023')).toBeInTheDocument()
    })

    it('should use custom format function', () => {
      const format = (v: number) => `$${v.toFixed(2)}`

      render(<MetricComparison {...mockData} format={format} />)

      expect(screen.getByText('$5000.00')).toBeInTheDocument()
      expect(screen.getByText('$4000.00')).toBeInTheDocument()
    })

    it('should apply custom className', () => {
      const { container } = render(<MetricComparison {...mockData} className="custom" />)

      const wrapper = container.firstChild
      expect(wrapper).toHaveClass('custom')
    })
  })

  // ============================================
  // Comparison Logic (5 tests)
  // ============================================
  describe('Comparison Logic', () => {
    it('should calculate positive percentage change', () => {
      render(<MetricComparison {...mockData} />)

      // (5000 - 4000) / 4000 * 100 = 25%
      expect(screen.getByText('25.0%')).toBeInTheDocument()
    })

    it('should calculate negative percentage change', () => {
      const data = {
        current: { label: 'This Month', value: 3000, period: 'Jan 2024' },
        previous: { label: 'Last Month', value: 4000, period: 'Dec 2023' },
      }

      render(<MetricComparison {...data} />)

      // (3000 - 4000) / 4000 * 100 = -25%
      expect(screen.getByText('-25.0%')).toBeInTheDocument()
    })

    it('should show ArrowUpRight for positive change', () => {
      render(<MetricComparison {...mockData} />)

      expect(screen.getByTestId('arrow-up-right')).toBeInTheDocument()
    })

    it('should show ArrowDownRight for negative change', () => {
      const data = {
        current: { label: 'This Month', value: 3000, period: 'Jan 2024' },
        previous: { label: 'Last Month', value: 4000, period: 'Dec 2023' },
      }

      render(<MetricComparison {...data} />)

      expect(screen.getByTestId('arrow-down-right')).toBeInTheDocument()
    })

    it('should handle zero previous value', () => {
      const data = {
        current: { label: 'This Month', value: 5000, period: 'Jan 2024' },
        previous: { label: 'Last Month', value: 0, period: 'Dec 2023' },
      }

      render(<MetricComparison {...data} />)

      // Should default to 0% when previous is 0
      expect(screen.getByText('0%')).toBeInTheDocument()
    })
  })

  // ============================================
  // Styling (2 tests)
  // ============================================
  describe('Styling', () => {
    it('should apply success styling for positive change', () => {
      const { container } = render(<MetricComparison {...mockData} />)

      const badge = container.querySelector('.bg-success\\/10')
      expect(badge).toBeInTheDocument()
    })

    it('should apply destructive styling for negative change', () => {
      const data = {
        current: { label: 'This Month', value: 3000, period: 'Jan 2024' },
        previous: { label: 'Last Month', value: 4000, period: 'Dec 2023' },
      }

      const { container } = render(<MetricComparison {...data} />)

      const badge = container.querySelector('.bg-destructive\\/10')
      expect(badge).toBeInTheDocument()
    })
  })

  // ============================================
  // Integration (1 test)
  // ============================================
  describe('Integration', () => {
    it('should display complete comparison with formatting', () => {
      const format = (v: number) => `$${v.toLocaleString()}`

      render(<MetricComparison {...mockData} format={format} />)

      expect(screen.getByText('This Month')).toBeInTheDocument()
      expect(screen.getByText('$5,000')).toBeInTheDocument()
      expect(screen.getByText('Last Month')).toBeInTheDocument()
      expect(screen.getByText('$4,000')).toBeInTheDocument()
      expect(screen.getByText('25.0%')).toBeInTheDocument()
      expect(screen.getByText('+$1,000 change')).toBeInTheDocument()
    })
  })
})

describe('KPICard Component', () => {
  // ============================================
  // Initial Render (5 tests)
  // ============================================
  describe('Initial Render', () => {
    it('should render with required props', () => {
      render(<KPICard title="Sales Target" value={7500} target={10000} />)

      expect(screen.getByText('Sales Target')).toBeInTheDocument()
      expect(screen.getByText('7,500')).toBeInTheDocument()
    })

    it('should render with custom format function', () => {
      const format = (v: number) => `$${v.toFixed(2)}`

      render(<KPICard title="Revenue" value={5000} target={10000} format={format} />)

      expect(screen.getByText('$5000.00')).toBeInTheDocument()
      expect(screen.getByText(/\$10000\.00/)).toBeInTheDocument()
    })

    it('should render unit when provided', () => {
      render(<KPICard title="Distance" value={75} target={100} unit="km" />)

      const units = screen.getAllByText('km')
      expect(units.length).toBeGreaterThan(0)
    })

    it('should render custom icon', () => {
      const Icon = () => <div data-testid="kpi-icon">Icon</div>

      render(<KPICard title="Test" value={50} target={100} icon={<Icon />} />)

      expect(screen.getByTestId('kpi-icon')).toBeInTheDocument()
    })

    it('should apply custom className', () => {
      const { container } = render(<KPICard title="Test" value={50} target={100} className="custom" />)

      const card = container.firstChild
      expect(card).toHaveClass('custom')
    })
  })

  // ============================================
  // Progress Calculation (5 tests)
  // ============================================
  describe('Progress Calculation', () => {
    it('should calculate correct percentage', () => {
      render(<KPICard title="Test" value={75} target={100} />)

      expect(screen.getByText('75%')).toBeInTheDocument()
    })

    it('should show on-track status for 100%+ progress', () => {
      const { container } = render(<KPICard title="Test" value={100} target={100} />)

      const progressText = screen.getByText('100%')
      expect(progressText).toHaveClass('text-success')

      const progressBar = container.querySelector('.bg-success')
      expect(progressBar).toBeInTheDocument()
    })

    it('should show warning status for 70-99% progress', () => {
      const { container } = render(<KPICard title="Test" value={80} target={100} />)

      const progressText = screen.getByText('80%')
      expect(progressText).toHaveClass('text-warning')

      const progressBar = container.querySelector('.bg-warning')
      expect(progressBar).toBeInTheDocument()
    })

    it('should show danger status for <70% progress', () => {
      const { container } = render(<KPICard title="Test" value={50} target={100} />)

      const progressText = screen.getByText('50%')
      expect(progressText).toHaveClass('text-destructive')

      const progressBar = container.querySelector('.bg-destructive')
      expect(progressBar).toBeInTheDocument()
    })

    it('should cap progress bar at 100%', () => {
      const { container } = render(<KPICard title="Test" value={150} target={100} />)

      const progressBar = container.querySelector('.bg-success')
      expect(progressBar).toHaveStyle({ width: '100%' })
    })
  })

  // ============================================
  // Progress Display (3 tests)
  // ============================================
  describe('Progress Display', () => {
    it('should show progress section by default', () => {
      render(<KPICard title="Test" value={75} target={100} />)

      expect(screen.getByText('Progress to target')).toBeInTheDocument()
      expect(screen.getByText('75%')).toBeInTheDocument()
    })

    it('should hide progress when showProgress is false', () => {
      render(<KPICard title="Test" value={75} target={100} showProgress={false} />)

      expect(screen.queryByText('Progress to target')).not.toBeInTheDocument()
    })

    it('should display target value', () => {
      render(<KPICard title="Test" value={75} target={10000} unit="units" />)

      expect(screen.getByText(/Target: 10,000 units/)).toBeInTheDocument()
    })
  })

  // ============================================
  // Integration (2 tests)
  // ============================================
  describe('Integration', () => {
    it('should render complete KPI card with all features', () => {
      const Icon = () => <div data-testid="icon">📈</div>
      const format = (v: number) => `$${v.toLocaleString()}`

      render(
        <KPICard
          title="Monthly Revenue"
          value={8500}
          target={10000}
          unit="USD"
          format={format}
          icon={<Icon />}
          showProgress
          className="custom"
        />
      )

      expect(screen.getByText('Monthly Revenue')).toBeInTheDocument()
      expect(screen.getByText('$8,500')).toBeInTheDocument()
      expect(screen.getByTestId('icon')).toBeInTheDocument()
      expect(screen.getByText('85%')).toBeInTheDocument()
      expect(screen.getByText(/Target: \$10,000 USD/)).toBeInTheDocument()
    })

    it('should handle all thresholds correctly', () => {
      const { container, rerender } = render(<KPICard title="Test" value={120} target={100} />)

      // 120% - on track (green)
      expect(container.querySelector('.bg-success')).toBeInTheDocument()

      // 85% - warning (yellow)
      rerender(<KPICard title="Test" value={85} target={100} />)
      expect(container.querySelector('.bg-warning')).toBeInTheDocument()

      // 50% - danger (red)
      rerender(<KPICard title="Test" value={50} target={100} />)
      expect(container.querySelector('.bg-destructive')).toBeInTheDocument()
    })
  })
})
