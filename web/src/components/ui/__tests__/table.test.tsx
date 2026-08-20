import React from 'react'
import { render, screen, fireEvent, within } from '@testing-library/react'
import {
  Table,
  TableRoot,
  TableHeader,
  TableBody,
  TableRow,
  TableHead,
  TableCell,
  Column,
} from '../table'

// Mock lucide-react icons
jest.mock('lucide-react', () => ({
  ArrowDown: ({ className }: any) => (
    <svg data-testid="arrow-down" className={className} />
  ),
  ArrowUp: ({ className }: any) => (
    <svg data-testid="arrow-up" className={className} />
  ),
  ArrowUpDown: ({ className }: any) => (
    <svg data-testid="arrow-up-down" className={className} />
  ),
}))

interface User {
  id: number
  name: string
  age: number
  email: string
  joinDate: Date
}

const mockUsers: User[] = [
  { id: 1, name: 'Alice', age: 30, email: 'alice@example.com', joinDate: new Date('2023-01-15') },
  { id: 2, name: 'Bob', age: 25, email: 'bob@example.com', joinDate: new Date('2023-03-20') },
  { id: 3, name: 'Charlie', age: 35, email: 'charlie@example.com', joinDate: new Date('2023-02-10') },
]

const mockColumns: Column<User>[] = [
  { key: 'name', label: 'Name', sortable: true },
  { key: 'age', label: 'Age', sortable: true, align: 'right' },
  { key: 'email', label: 'Email', sortable: false },
]

describe('Table Component', () => {
  // ============================================
  // Basic Rendering (8 tests)
  // ============================================
  describe('Basic Rendering', () => {
    it('should render table with data', () => {
      render(
        <Table
          data={mockUsers}
          columns={mockColumns}
          keyExtractor={(row) => row.id}
        />
      )

      expect(screen.getByText('Alice')).toBeInTheDocument()
      expect(screen.getByText('Bob')).toBeInTheDocument()
      expect(screen.getByText('Charlie')).toBeInTheDocument()
    })

    it('should render column headers', () => {
      render(
        <Table
          data={mockUsers}
          columns={mockColumns}
          keyExtractor={(row) => row.id}
        />
      )

      expect(screen.getByText('Name')).toBeInTheDocument()
      expect(screen.getByText('Age')).toBeInTheDocument()
      expect(screen.getByText('Email')).toBeInTheDocument()
    })

    it('should render all data rows', () => {
      const { container } = render(
        <Table
          data={mockUsers}
          columns={mockColumns}
          keyExtractor={(row) => row.id}
        />
      )

      const rows = container.querySelectorAll('tbody tr')
      expect(rows).toHaveLength(3)
    })

    it('should apply custom className', () => {
      const { container } = render(
        <Table
          data={mockUsers}
          columns={mockColumns}
          keyExtractor={(row) => row.id}
          className="custom-table"
        />
      )

      const wrapper = container.querySelector('.custom-table')
      expect(wrapper).toBeInTheDocument()
    })

    it('should render with minimum required props', () => {
      render(
        <Table
          data={[{ id: 1, value: 'test' }]}
          columns={[{ key: 'value', label: 'Value' }]}
          keyExtractor={(row) => row.id}
        />
      )

      expect(screen.getByText('test')).toBeInTheDocument()
    })

    it('should use keyExtractor for row keys', () => {
      const { container } = render(
        <Table
          data={mockUsers}
          columns={mockColumns}
          keyExtractor={(row) => `user-${row.id}`}
        />
      )

      const rows = container.querySelectorAll('tbody tr')
      // Keys are React internal - just verify rows render correctly
      expect(rows).toHaveLength(3)
      expect(within(rows[0] as HTMLElement).getByText('Alice')).toBeInTheDocument()
    })

    it('should render column widths when specified', () => {
      const columnsWithWidth: Column<User>[] = [
        { key: 'name', label: 'Name', width: '200px' },
        { key: 'age', label: 'Age', width: '100px' },
      ]

      const { container } = render(
        <Table
          data={mockUsers}
          columns={columnsWithWidth}
          keyExtractor={(row) => row.id}
        />
      )

      const headers = container.querySelectorAll('th')
      expect(headers[0]).toHaveStyle({ width: '200px' })
      expect(headers[1]).toHaveStyle({ width: '100px' })
    })

    it('should render table structure correctly', () => {
      const { container } = render(
        <Table
          data={mockUsers}
          columns={mockColumns}
          keyExtractor={(row) => row.id}
        />
      )

      expect(container.querySelector('table') as HTMLElement).toBeInTheDocument()
      expect(container.querySelector('thead') as HTMLElement).toBeInTheDocument()
      expect(container.querySelector('tbody') as HTMLElement).toBeInTheDocument()
    })
  })

  // ============================================
  // Sorting - Uncontrolled (12 tests)
  // ============================================
  describe('Sorting - Uncontrolled', () => {
    it('should show sortable icon for sortable columns', () => {
      render(
        <Table
          data={mockUsers}
          columns={mockColumns}
          keyExtractor={(row) => row.id}
        />
      )

      // Name and Age are sortable
      const sortButtons = screen.getAllByRole('button')
      expect(sortButtons.length).toBeGreaterThanOrEqual(2)
    })

    it('should show ArrowUpDown icon when column is not sorted', () => {
      render(
        <Table
          data={mockUsers}
          columns={mockColumns}
          keyExtractor={(row) => row.id}
        />
      )

      const upDownIcons = screen.getAllByTestId('arrow-up-down')
      expect(upDownIcons.length).toBeGreaterThan(0)
    })

    it('should sort strings in ascending order on first click', () => {
      const { container } = render(
        <Table
          data={mockUsers}
          columns={mockColumns}
          keyExtractor={(row) => row.id}
        />
      )

      const nameButton = screen.getByLabelText('Sort by Name')
      fireEvent.click(nameButton)

      const rows = container.querySelectorAll('tbody tr')
      const firstRowName = within(rows[0] as HTMLElement).getByText('Alice')
      expect(firstRowName).toBeInTheDocument()
    })

    it('should toggle to descending order on second click', () => {
      const { container } = render(
        <Table
          data={mockUsers}
          columns={mockColumns}
          keyExtractor={(row) => row.id}
        />
      )

      const nameButton = screen.getByLabelText('Sort by Name')
      fireEvent.click(nameButton) // asc
      fireEvent.click(nameButton) // desc

      const rows = container.querySelectorAll('tbody tr')
      const firstRowName = within(rows[0] as HTMLElement).getByText('Charlie')
      expect(firstRowName).toBeInTheDocument()
    })

    it('should show ArrowUp icon when sorted ascending', () => {
      render(
        <Table
          data={mockUsers}
          columns={mockColumns}
          keyExtractor={(row) => row.id}
        />
      )

      const nameButton = screen.getByLabelText('Sort by Name')
      fireEvent.click(nameButton)

      expect(screen.getByTestId('arrow-up')).toBeInTheDocument()
    })

    it('should show ArrowDown icon when sorted descending', () => {
      render(
        <Table
          data={mockUsers}
          columns={mockColumns}
          keyExtractor={(row) => row.id}
        />
      )

      const nameButton = screen.getByLabelText('Sort by Name')
      fireEvent.click(nameButton) // asc
      fireEvent.click(nameButton) // desc

      expect(screen.getByTestId('arrow-down')).toBeInTheDocument()
    })

    it('should sort numbers correctly', () => {
      const { container } = render(
        <Table
          data={mockUsers}
          columns={mockColumns}
          keyExtractor={(row) => row.id}
        />
      )

      const ageButton = screen.getByLabelText('Sort by Age')
      fireEvent.click(ageButton)

      const rows = container.querySelectorAll('tbody tr')
      const firstRowAge = within(rows[0] as HTMLElement).getByText('25')
      expect(firstRowAge).toBeInTheDocument()
    })

    it('should sort numbers descending', () => {
      const { container } = render(
        <Table
          data={mockUsers}
          columns={mockColumns}
          keyExtractor={(row) => row.id}
        />
      )

      const ageButton = screen.getByLabelText('Sort by Age')
      fireEvent.click(ageButton) // asc
      fireEvent.click(ageButton) // desc

      const rows = container.querySelectorAll('tbody tr')
      const firstRowAge = within(rows[0] as HTMLElement).getByText('35')
      expect(firstRowAge).toBeInTheDocument()
    })

    it('should not sort non-sortable columns', () => {
      const { container } = render(
        <Table
          data={mockUsers}
          columns={mockColumns}
          keyExtractor={(row) => row.id}
        />
      )

      const emailHeader = screen.getByText('Email')
      fireEvent.click(emailHeader)

      // Data should remain in original order
      const rows = container.querySelectorAll('tbody tr')
      const firstRowName = within(rows[0] as HTMLElement).getByText('Alice')
      expect(firstRowName).toBeInTheDocument()
    })

    it('should sort dates correctly', () => {
      const dateColumns: Column<User>[] = [
        { key: 'name', label: 'Name' },
        {
          key: 'joinDate',
          label: 'Join Date',
          sortable: true,
          render: (row, value) => value.toLocaleDateString(),
        },
      ]

      const { container } = render(
        <Table
          data={mockUsers}
          columns={dateColumns}
          keyExtractor={(row) => row.id}
        />
      )

      const dateButton = screen.getByLabelText('Sort by Join Date')
      fireEvent.click(dateButton)

      const rows = container.querySelectorAll('tbody tr')
      const firstRowName = within(rows[0] as HTMLElement).getByText('Alice') // Jan 15 is earliest
      expect(firstRowName).toBeInTheDocument()
    })

    it('should handle switching between different column sorts', () => {
      const { container } = render(
        <Table
          data={mockUsers}
          columns={mockColumns}
          keyExtractor={(row) => row.id}
        />
      )

      // Sort by name first
      const nameButton = screen.getByLabelText('Sort by Name')
      fireEvent.click(nameButton)

      // Then sort by age
      const ageButton = screen.getByLabelText('Sort by Age')
      fireEvent.click(ageButton)

      const rows = container.querySelectorAll('tbody tr')
      const firstRowAge = within(rows[0] as HTMLElement).getByText('25')
      expect(firstRowAge).toBeInTheDocument()
    })

    it('should handle equal values in sort', () => {
      const duplicateData = [
        ...mockUsers,
        { id: 4, name: 'Alice', age: 25, email: 'alice2@example.com', joinDate: new Date() },
      ]

      const { container } = render(
        <Table
          data={duplicateData}
          columns={mockColumns}
          keyExtractor={(row) => row.id}
        />
      )

      const nameButton = screen.getByLabelText('Sort by Name')
      fireEvent.click(nameButton)

      const rows = container.querySelectorAll('tbody tr')
      expect(rows).toHaveLength(4)
    })
  })

  // ============================================
  // Sorting - Controlled (8 tests)
  // ============================================
  describe('Sorting - Controlled', () => {
    it('should use controlled sortBy prop', () => {
      const { container } = render(
        <Table
          data={mockUsers}
          columns={mockColumns}
          keyExtractor={(row) => row.id}
          sortBy="age"
          sortDirection="asc"
        />
      )

      const rows = container.querySelectorAll('tbody tr')
      const firstRowAge = within(rows[0] as HTMLElement).getByText('25')
      expect(firstRowAge).toBeInTheDocument()
    })

    it('should use controlled sortDirection prop', () => {
      const { container } = render(
        <Table
          data={mockUsers}
          columns={mockColumns}
          keyExtractor={(row) => row.id}
          sortBy="age"
          sortDirection="desc"
        />
      )

      const rows = container.querySelectorAll('tbody tr')
      const firstRowAge = within(rows[0] as HTMLElement).getByText('35')
      expect(firstRowAge).toBeInTheDocument()
    })

    it('should call onSort when clicking sortable column', () => {
      const onSort = jest.fn()

      render(
        <Table
          data={mockUsers}
          columns={mockColumns}
          keyExtractor={(row) => row.id}
          onSort={onSort}
        />
      )

      const nameButton = screen.getByLabelText('Sort by Name')
      fireEvent.click(nameButton)

      expect(onSort).toHaveBeenCalledWith('name', 'asc')
    })

    it('should call onSort with desc on second click', () => {
      const onSort = jest.fn()

      const { rerender } = render(
        <Table
          data={mockUsers}
          columns={mockColumns}
          keyExtractor={(row) => row.id}
          sortBy="name"
          sortDirection="asc"
          onSort={onSort}
        />
      )

      const nameButton = screen.getByLabelText('Sort by Name')
      fireEvent.click(nameButton)

      expect(onSort).toHaveBeenCalledWith('name', 'desc')
    })

    it('should show correct icon for controlled sort', () => {
      render(
        <Table
          data={mockUsers}
          columns={mockColumns}
          keyExtractor={(row) => row.id}
          sortBy="name"
          sortDirection="asc"
        />
      )

      expect(screen.getByTestId('arrow-up')).toBeInTheDocument()
    })

    it('should show correct icon for controlled desc sort', () => {
      render(
        <Table
          data={mockUsers}
          columns={mockColumns}
          keyExtractor={(row) => row.id}
          sortBy="name"
          sortDirection="desc"
        />
      )

      expect(screen.getByTestId('arrow-down')).toBeInTheDocument()
    })

    it('should not update internal state when controlled', () => {
      const onSort = jest.fn()

      render(
        <Table
          data={mockUsers}
          columns={mockColumns}
          keyExtractor={(row) => row.id}
          sortBy="name"
          sortDirection="asc"
          onSort={onSort}
        />
      )

      const ageButton = screen.getByLabelText('Sort by Age')
      fireEvent.click(ageButton)

      expect(onSort).toHaveBeenCalledWith('age', 'asc')
      // Icon should still show name column as sorted
      expect(screen.getByTestId('arrow-up')).toBeInTheDocument()
    })

    it('should handle mixed type sorting as strings', () => {
      const mixedData = [
        { id: 1, value: 'text' },
        { id: 2, value: 123 },
        { id: 3, value: null },
      ]

      const { container } = render(
        <Table
          data={mixedData}
          columns={[{ key: 'value', label: 'Value', sortable: true }]}
          keyExtractor={(row) => row.id}
        />
      )

      const sortButton = screen.getByLabelText('Sort by Value')
      fireEvent.click(sortButton)

      const rows = container.querySelectorAll('tbody tr')
      expect(rows).toHaveLength(3)
    })
  })

  // ============================================
  // Visual Variants (8 tests)
  // ============================================
  describe('Visual Variants', () => {
    it('should apply striped styling', () => {
      const { container } = render(
        <Table
          data={mockUsers}
          columns={mockColumns}
          keyExtractor={(row) => row.id}
          striped
        />
      )

      const rows = container.querySelectorAll('tbody tr')
      expect(rows[1]).toHaveClass('bg-muted/30') // Second row (index 1)
    })

    it('should not stripe first row', () => {
      const { container } = render(
        <Table
          data={mockUsers}
          columns={mockColumns}
          keyExtractor={(row) => row.id}
          striped
        />
      )

      const rows = container.querySelectorAll('tbody tr')
      expect(rows[0]).not.toHaveClass('bg-muted/30')
    })

    it('should apply hoverable styling by default', () => {
      const { container } = render(
        <Table
          data={mockUsers}
          columns={mockColumns}
          keyExtractor={(row) => row.id}
        />
      )

      const rows = container.querySelectorAll('tbody tr')
      expect(rows[0]).toHaveClass('hover:bg-muted/50')
    })

    it('should disable hoverable styling when hoverable=false', () => {
      const { container } = render(
        <Table
          data={mockUsers}
          columns={mockColumns}
          keyExtractor={(row) => row.id}
          hoverable={false}
        />
      )

      const rows = container.querySelectorAll('tbody tr')
      expect(rows[0]).not.toHaveClass('hover:bg-muted/50')
    })

    it('should apply bordered styling', () => {
      const { container } = render(
        <Table
          data={mockUsers}
          columns={mockColumns}
          keyExtractor={(row) => row.id}
          bordered
        />
      )

      const headerRow = container.querySelector('thead tr')
      expect(headerRow).toHaveClass('border-x')
    })

    it('should apply compact spacing', () => {
      const { container } = render(
        <Table
          data={mockUsers}
          columns={mockColumns}
          keyExtractor={(row) => row.id}
          compact
        />
      )

      const cells = container.querySelectorAll('td')
      expect(cells[0]).toHaveClass('px-3', 'py-2')
    })

    it('should apply default spacing when not compact', () => {
      const { container } = render(
        <Table
          data={mockUsers}
          columns={mockColumns}
          keyExtractor={(row) => row.id}
        />
      )

      const cells = container.querySelectorAll('td')
      expect(cells[0]).toHaveClass('px-4', 'py-3')
    })

    it('should combine multiple visual variants', () => {
      const { container } = render(
        <Table
          data={mockUsers}
          columns={mockColumns}
          keyExtractor={(row) => row.id}
          striped
          bordered
          compact
          hoverable={false}
        />
      )

      const rows = container.querySelectorAll('tbody tr')
      expect(rows[1]).toHaveClass('bg-muted/30') // striped
      expect(rows[0]).toHaveClass('border-x') // bordered
      expect(rows[0]).not.toHaveClass('hover:bg-muted/50') // hoverable=false
    })
  })

  // ============================================
  // Column Alignment (6 tests)
  // ============================================
  describe('Column Alignment', () => {
    it('should align left by default', () => {
      const { container } = render(
        <Table
          data={mockUsers}
          columns={[{ key: 'name', label: 'Name' }]}
          keyExtractor={(row) => row.id}
        />
      )

      const header = container.querySelector('th')
      expect(header).toHaveClass('text-left')
    })

    it('should align center when specified', () => {
      const { container } = render(
        <Table
          data={mockUsers}
          columns={[{ key: 'name', label: 'Name', align: 'center' }]}
          keyExtractor={(row) => row.id}
        />
      )

      const header = container.querySelector('th')
      expect(header).toHaveClass('text-center')
    })

    it('should align right when specified', () => {
      const { container } = render(
        <Table
          data={mockUsers}
          columns={[{ key: 'age', label: 'Age', align: 'right' }]}
          keyExtractor={(row) => row.id}
        />
      )

      const header = container.querySelector('th')
      expect(header).toHaveClass('text-right')
    })

    it('should apply alignment to table cells', () => {
      const { container } = render(
        <Table
          data={mockUsers}
          columns={[{ key: 'age', label: 'Age', align: 'right' }]}
          keyExtractor={(row) => row.id}
        />
      )

      const cells = container.querySelectorAll('td')
      expect(cells[0]).toHaveClass('text-right')
    })

    it('should support different alignments per column', () => {
      const columns: Column<User>[] = [
        { key: 'name', label: 'Name', align: 'left' },
        { key: 'age', label: 'Age', align: 'center' },
        { key: 'email', label: 'Email', align: 'right' },
      ]

      const { container } = render(
        <Table data={mockUsers} columns={columns} keyExtractor={(row) => row.id} />
      )

      const headers = container.querySelectorAll('th')
      expect(headers[0]).toHaveClass('text-left')
      expect(headers[1]).toHaveClass('text-center')
      expect(headers[2]).toHaveClass('text-right')
    })

    it('should align both header and cells consistently', () => {
      const { container } = render(
        <Table
          data={mockUsers}
          columns={[{ key: 'age', label: 'Age', align: 'center' }]}
          keyExtractor={(row) => row.id}
        />
      )

      const header = container.querySelector('th')
      const cell = container.querySelector('td')

      expect(header).toHaveClass('text-center')
      expect(cell).toHaveClass('text-center')
    })
  })

  // ============================================
  // Loading and Empty States (6 tests)
  // ============================================
  describe('Loading and Empty States', () => {
    it('should display loading state', () => {
      render(
        <Table
          data={mockUsers}
          columns={mockColumns}
          keyExtractor={(row) => row.id}
          loading
        />
      )

      expect(screen.getByText('Loading...')).toBeInTheDocument()
    })

    it('should show spinner in loading state', () => {
      const { container } = render(
        <Table
          data={mockUsers}
          columns={mockColumns}
          keyExtractor={(row) => row.id}
          loading
        />
      )

      const spinner = container.querySelector('.animate-spin')
      expect(spinner).toBeInTheDocument()
    })

    it('should not display data when loading', () => {
      render(
        <Table
          data={mockUsers}
          columns={mockColumns}
          keyExtractor={(row) => row.id}
          loading
        />
      )

      expect(screen.queryByText('Alice')).not.toBeInTheDocument()
    })

    it('should display default empty message', () => {
      render(
        <Table data={[]} columns={mockColumns} keyExtractor={(row) => row.id} />
      )

      expect(screen.getByText('No data available')).toBeInTheDocument()
    })

    it('should display custom empty message', () => {
      render(
        <Table
          data={[]}
          columns={mockColumns}
          keyExtractor={(row) => row.id}
          emptyMessage="No users found"
        />
      )

      expect(screen.getByText('No users found')).toBeInTheDocument()
    })

    it('should span empty message across all columns', () => {
      const { container } = render(
        <Table data={[]} columns={mockColumns} keyExtractor={(row) => row.id} />
      )

      const emptyCell = container.querySelector('td[colspan]')
      expect(emptyCell).toHaveAttribute('colspan', '3')
    })
  })

  // ============================================
  // Row Interactions (5 tests)
  // ============================================
  describe('Row Interactions', () => {
    it('should call onRowClick when row is clicked', () => {
      const onRowClick = jest.fn()

      render(
        <Table
          data={mockUsers}
          columns={mockColumns}
          keyExtractor={(row) => row.id}
          onRowClick={onRowClick}
        />
      )

      const firstRow = screen.getByText('Alice').closest('tr')
      fireEvent.click(firstRow!)

      expect(onRowClick).toHaveBeenCalledWith(mockUsers[0])
    })

    it('should apply cursor-pointer when onRowClick is provided', () => {
      const { container } = render(
        <Table
          data={mockUsers}
          columns={mockColumns}
          keyExtractor={(row) => row.id}
          onRowClick={jest.fn()}
        />
      )

      const rows = container.querySelectorAll('tbody tr')
      expect(rows[0]).toHaveClass('cursor-pointer')
    })

    it('should not apply cursor-pointer when onRowClick is not provided', () => {
      const { container } = render(
        <Table
          data={mockUsers}
          columns={mockColumns}
          keyExtractor={(row) => row.id}
        />
      )

      const rows = container.querySelectorAll('tbody tr')
      expect(rows[0]).not.toHaveClass('cursor-pointer')
    })

    it('should call onRowClick with correct row data', () => {
      const onRowClick = jest.fn()

      render(
        <Table
          data={mockUsers}
          columns={mockColumns}
          keyExtractor={(row) => row.id}
          onRowClick={onRowClick}
        />
      )

      const secondRow = screen.getByText('Bob').closest('tr')
      fireEvent.click(secondRow!)

      expect(onRowClick).toHaveBeenCalledWith(mockUsers[1])
      expect(onRowClick).toHaveBeenCalledTimes(1)
    })

    it('should handle multiple row clicks', () => {
      const onRowClick = jest.fn()

      render(
        <Table
          data={mockUsers}
          columns={mockColumns}
          keyExtractor={(row) => row.id}
          onRowClick={onRowClick}
        />
      )

      const firstRow = screen.getByText('Alice').closest('tr')
      const secondRow = screen.getByText('Bob').closest('tr')

      fireEvent.click(firstRow!)
      fireEvent.click(secondRow!)

      expect(onRowClick).toHaveBeenCalledTimes(2)
    })
  })

  // ============================================
  // Custom Render Functions (5 tests)
  // ============================================
  describe('Custom Render Functions', () => {
    it('should use custom render function when provided', () => {
      const customColumns: Column<User>[] = [
        {
          key: 'name',
          label: 'Name',
          render: (row) => <strong data-testid="custom-name">{row.name}</strong>,
        },
      ]

      render(
        <Table
          data={mockUsers}
          columns={customColumns}
          keyExtractor={(row) => row.id}
        />
      )

      const customElements = screen.getAllByTestId('custom-name')
      expect(customElements).toHaveLength(3)
      expect(customElements[0]).toBeInTheDocument()
      expect(customElements[0].tagName).toBe('STRONG')
    })

    it('should pass row and value to custom render', () => {
      const renderFn = jest.fn((row, value) => <span>{value}</span>)

      const customColumns: Column<User>[] = [
        { key: 'name', label: 'Name', render: renderFn },
      ]

      render(
        <Table
          data={mockUsers}
          columns={customColumns}
          keyExtractor={(row) => row.id}
        />
      )

      expect(renderFn).toHaveBeenCalledWith(mockUsers[0], 'Alice')
    })

    it('should display raw value when no render function', () => {
      render(
        <Table
          data={mockUsers}
          columns={[{ key: 'age', label: 'Age' }]}
          keyExtractor={(row) => row.id}
        />
      )

      expect(screen.getByText('30')).toBeInTheDocument()
    })

    it('should support complex custom renders', () => {
      const customColumns: Column<User>[] = [
        {
          key: 'email',
          label: 'Contact',
          render: (row, value) => (
            <div>
              <div data-testid="email-link">{value}</div>
              <div className="text-xs text-muted-foreground">{row.name}</div>
            </div>
          ),
        },
      ]

      render(
        <Table
          data={[mockUsers[0]]}
          columns={customColumns}
          keyExtractor={(row) => row.id}
        />
      )

      expect(screen.getByTestId('email-link')).toHaveTextContent('alice@example.com')
      expect(screen.getByText('Alice')).toBeInTheDocument()
    })

    it('should handle render function returning null', () => {
      const customColumns: Column<User>[] = [
        { key: 'name', label: 'Name', render: () => null },
      ]

      const { container } = render(
        <Table
          data={mockUsers}
          columns={customColumns}
          keyExtractor={(row) => row.id}
        />
      )

      const cells = container.querySelectorAll('td')
      expect(cells[0]).toBeEmptyDOMElement()
    })
  })

  // ============================================
  // Integration Tests (3 tests)
  // ============================================
  describe('Integration', () => {
    it('should work with all features combined', () => {
      const onRowClick = jest.fn()
      const onSort = jest.fn()

      const { container } = render(
        <Table
          data={mockUsers}
          columns={mockColumns}
          keyExtractor={(row) => row.id}
          onRowClick={onRowClick}
          onSort={onSort}
          striped
          bordered
          compact
          hoverable
          loading={false}
        />
      )

      // Test sorting
      const nameButton = screen.getByLabelText('Sort by Name')
      fireEvent.click(nameButton)
      expect(onSort).toHaveBeenCalledWith('name', 'asc')

      // Test row click
      const firstRow = screen.getByText('Alice').closest('tr')
      fireEvent.click(firstRow!)
      expect(onRowClick).toHaveBeenCalledWith(mockUsers[0])

      // Test visual variants
      const rows = container.querySelectorAll('tbody tr')
      expect(rows[1]).toHaveClass('bg-muted/30') // striped
      expect(rows[0]).toHaveClass('border-x') // bordered
    })

    it('should handle sorting with custom render', () => {
      const customColumns: Column<User>[] = [
        {
          key: 'name',
          label: 'Name',
          sortable: true,
          render: (row) => <strong>{row.name.toUpperCase()}</strong>,
        },
      ]

      const { container } = render(
        <Table
          data={mockUsers}
          columns={customColumns}
          keyExtractor={(row) => row.id}
        />
      )

      const nameButton = screen.getByLabelText('Sort by Name')
      fireEvent.click(nameButton)

      const rows = container.querySelectorAll('tbody tr')
      const firstRowName = within(rows[0] as HTMLElement).getByText('ALICE')
      expect(firstRowName).toBeInTheDocument()
    })

    it('should preserve data integrity through multiple operations', () => {
      const onRowClick = jest.fn()

      const { container } = render(
        <Table
          data={mockUsers}
          columns={mockColumns}
          keyExtractor={(row) => row.id}
          onRowClick={onRowClick}
        />
      )

      // Sort by age
      const ageButton = screen.getByLabelText('Sort by Age')
      fireEvent.click(ageButton)

      // Click second row (should be age 30, Alice)
      const rows = container.querySelectorAll('tbody tr')
      fireEvent.click(rows[1])

      expect(onRowClick).toHaveBeenCalledWith(mockUsers[0]) // Alice with all her data
    })
  })

  // ============================================
  // Wrapper Components (12 tests)
  // ============================================
  describe('Wrapper Components', () => {
    describe('TableRoot', () => {
      it('should render table element', () => {
        const { container } = render(
          <TableRoot>
            <tbody>
              <tr>
                <td>Test</td>
              </tr>
            </tbody>
          </TableRoot>
        )

        expect(container.querySelector('table') as HTMLElement).toBeInTheDocument()
      })

      it('should apply custom className', () => {
        const { container } = render(
          <TableRoot className="custom-class">
            <tbody />
          </TableRoot>
        )

        const table = container.querySelector('table')
        expect(table).toHaveClass('custom-class')
      })
    })

    describe('TableHeader', () => {
      it('should render thead element', () => {
        const { container } = render(
          <TableRoot>
            <TableHeader>
              <tr>
                <th>Header</th>
              </tr>
            </TableHeader>
          </TableRoot>
        )

        expect(container.querySelector('thead') as HTMLElement).toBeInTheDocument()
      })

      it('should apply default styling', () => {
        const { container } = render(
          <TableRoot>
            <TableHeader>
              <tr />
            </TableHeader>
          </TableRoot>
        )

        const thead = container.querySelector('thead')
        expect(thead).toHaveClass('border-b', 'border-border', 'bg-muted/50')
      })
    })

    describe('TableBody', () => {
      it('should render tbody element', () => {
        const { container } = render(
          <TableRoot>
            <TableBody>
              <tr>
                <td>Data</td>
              </tr>
            </TableBody>
          </TableRoot>
        )

        expect(container.querySelector('tbody') as HTMLElement).toBeInTheDocument()
      })

      it('should apply custom className', () => {
        const { container } = render(
          <TableRoot>
            <TableBody className="custom-body">
              <tr />
            </TableBody>
          </TableRoot>
        )

        const tbody = container.querySelector('tbody')
        expect(tbody).toHaveClass('custom-body')
      })
    })

    describe('TableRow', () => {
      it('should render tr element', () => {
        const { container } = render(
          <TableRoot>
            <TableBody>
              <TableRow>
                <td>Cell</td>
              </TableRow>
            </TableBody>
          </TableRoot>
        )

        expect(container.querySelector('tr') as HTMLElement).toBeInTheDocument()
      })

      it('should apply hoverable by default', () => {
        const { container } = render(
          <TableRoot>
            <TableBody>
              <TableRow>
                <td>Cell</td>
              </TableRow>
            </TableBody>
          </TableRoot>
        )

        const row = container.querySelector('tr')
        expect(row).toHaveClass('hover:bg-muted/50')
      })

      it('should disable hoverable when hoverable=false', () => {
        const { container } = render(
          <TableRoot>
            <TableBody>
              <TableRow hoverable={false}>
                <td>Cell</td>
              </TableRow>
            </TableBody>
          </TableRoot>
        )

        const row = container.querySelector('tr')
        expect(row).not.toHaveClass('hover:bg-muted/50')
      })
    })

    describe('TableHead', () => {
      it('should render th element with scope', () => {
        const { container } = render(
          <TableRoot>
            <TableHeader>
              <tr>
                <TableHead>Header</TableHead>
              </tr>
            </TableHeader>
          </TableRoot>
        )

        const th = container.querySelector('th')
        expect(th).toBeInTheDocument()
        expect(th).toHaveAttribute('scope', 'col')
      })

      it('should apply default styling', () => {
        const { container } = render(
          <TableRoot>
            <TableHeader>
              <tr>
                <TableHead>Header</TableHead>
              </tr>
            </TableHeader>
          </TableRoot>
        )

        const th = container.querySelector('th')
        expect(th).toHaveClass('px-4', 'py-3', 'text-sm', 'font-semibold')
      })
    })

    describe('TableCell', () => {
      it('should render td element', () => {
        const { container } = render(
          <TableRoot>
            <TableBody>
              <tr>
                <TableCell>Data</TableCell>
              </tr>
            </TableBody>
          </TableRoot>
        )

        expect(container.querySelector('td') as HTMLElement).toBeInTheDocument()
        expect(screen.getByText('Data')).toBeInTheDocument()
      })

      it('should apply default styling', () => {
        const { container } = render(
          <TableRoot>
            <TableBody>
              <tr>
                <TableCell>Data</TableCell>
              </tr>
            </TableBody>
          </TableRoot>
        )

        const td = container.querySelector('td')
        expect(td).toHaveClass('px-4', 'py-3', 'text-sm')
      })
    })
  })
})
