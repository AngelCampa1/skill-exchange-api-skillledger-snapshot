import React from 'react'
import { render, screen } from '@testing-library/react'
import '@testing-library/jest-dom'
import { Card, CardHeader, CardTitle, CardDescription, CardContent, CardFooter } from '../card'

describe('Card', () => {
  // ========================================
  // Card Tests
  // ========================================
  describe('Card', () => {
    it('should render as div element', () => {
      const { container } = render(<Card>Card content</Card>)
      const card = container.querySelector('div')
      expect(card).toBeInTheDocument()
      expect(card).toHaveTextContent('Card content')
    })

    it('should apply default classes', () => {
      const { container } = render(<Card>Content</Card>)
      const card = container.firstChild as HTMLElement
      expect(card).toHaveClass('rounded-2xl')
      expect(card).toHaveClass('border')
      expect(card).toHaveClass('border-border')
      expect(card).toHaveClass('bg-card')
      expect(card).toHaveClass('text-card-foreground')
      expect(card).toHaveClass('shadow-sm')
    })

    it('should apply custom className', () => {
      const { container } = render(<Card className="custom-card">Content</Card>)
      const card = container.firstChild as HTMLElement
      expect(card).toHaveClass('custom-card')
      expect(card).toHaveClass('rounded-2xl')
    })

    it('should forward refs correctly', () => {
      const ref = React.createRef<HTMLDivElement>()
      render(<Card ref={ref}>Content</Card>)
      expect(ref.current).toBeInstanceOf(HTMLDivElement)
    })

    it('should render children correctly', () => {
      render(
        <Card>
          <div>Child content</div>
        </Card>
      )
      expect(screen.getByText('Child content')).toBeInTheDocument()
    })

    it('should spread additional props', () => {
      render(<Card data-testid="card" id="card-1">Content</Card>)
      const card = screen.getByTestId('card')
      expect(card).toHaveAttribute('id', 'card-1')
    })

    it('should handle empty className', () => {
      const { container } = render(<Card className="">Content</Card>)
      const card = container.firstChild as HTMLElement
      expect(card).toHaveClass('rounded-2xl')
    })

    it('should handle empty children', () => {
      const { container } = render(<Card />)
      const card = container.firstChild as HTMLElement
      expect(card).toBeInTheDocument()
    })
  })

  // ========================================
  // CardHeader Tests
  // ========================================
  describe('CardHeader', () => {
    it('should render as div element', () => {
      const { container } = render(<CardHeader>Header</CardHeader>)
      const header = screen.getByText('Header')
      expect(header.tagName).toBe('DIV')
    })

    it('should apply default classes', () => {
      render(<CardHeader>Header</CardHeader>)
      const header = screen.getByText('Header')
      expect(header).toHaveClass('flex')
      expect(header).toHaveClass('flex-col')
      expect(header).toHaveClass('space-y-3')
      expect(header).toHaveClass('p-8')
    })

    it('should apply custom className', () => {
      render(<CardHeader className="custom-header">Header</CardHeader>)
      const header = screen.getByText('Header')
      expect(header).toHaveClass('custom-header')
      expect(header).toHaveClass('flex')
    })

    it('should forward refs correctly', () => {
      const ref = React.createRef<HTMLDivElement>()
      render(<CardHeader ref={ref}>Header</CardHeader>)
      expect(ref.current).toBeInstanceOf(HTMLDivElement)
    })

    it('should render children correctly', () => {
      render(
        <CardHeader>
          <span>Header content</span>
        </CardHeader>
      )
      expect(screen.getByText('Header content')).toBeInTheDocument()
    })

    it('should spread additional props', () => {
      render(<CardHeader data-testid="header" id="header-1">Header</CardHeader>)
      const header = screen.getByTestId('header')
      expect(header).toHaveAttribute('id', 'header-1')
    })

    it('should handle empty children', () => {
      const { container } = render(<CardHeader />)
      const header = container.querySelector('.flex.flex-col')
      expect(header).toBeInTheDocument()
    })
  })

  // ========================================
  // CardTitle Tests
  // ========================================
  describe('CardTitle', () => {
    it('should render as h3 element', () => {
      render(<CardTitle>Title</CardTitle>)
      const title = screen.getByText('Title')
      expect(title.tagName).toBe('H3')
    })

    it('should apply default classes', () => {
      render(<CardTitle>Title</CardTitle>)
      const title = screen.getByText('Title')
      expect(title).toHaveClass('text-subheading')
      expect(title).toHaveClass('text-foreground')
    })

    it('should apply custom className', () => {
      render(<CardTitle className="custom-title">Title</CardTitle>)
      const title = screen.getByText('Title')
      expect(title).toHaveClass('custom-title')
      expect(title).toHaveClass('text-subheading')
    })

    it('should forward refs correctly', () => {
      const ref = React.createRef<HTMLParagraphElement>()
      render(<CardTitle ref={ref}>Title</CardTitle>)
      expect(ref.current).toBeInstanceOf(HTMLHeadingElement)
      expect(ref.current?.tagName).toBe('H3')
    })

    it('should render children correctly', () => {
      render(
        <CardTitle>
          <span>Title text</span>
        </CardTitle>
      )
      expect(screen.getByText('Title text')).toBeInTheDocument()
    })

    it('should spread additional props', () => {
      render(<CardTitle data-testid="title" id="title-1">Title</CardTitle>)
      const title = screen.getByTestId('title')
      expect(title).toHaveAttribute('id', 'title-1')
    })

    it('should handle empty children', () => {
      const { container } = render(<CardTitle />)
      const title = container.querySelector('h3')
      expect(title).toBeInTheDocument()
    })
  })

  // ========================================
  // CardDescription Tests
  // ========================================
  describe('CardDescription', () => {
    it('should render as p element', () => {
      render(<CardDescription>Description</CardDescription>)
      const description = screen.getByText('Description')
      expect(description.tagName).toBe('P')
    })

    it('should apply default classes', () => {
      render(<CardDescription>Description</CardDescription>)
      const description = screen.getByText('Description')
      expect(description).toHaveClass('text-body')
      expect(description).toHaveClass('text-muted-foreground')
    })

    it('should apply custom className', () => {
      render(<CardDescription className="custom-desc">Description</CardDescription>)
      const description = screen.getByText('Description')
      expect(description).toHaveClass('custom-desc')
      expect(description).toHaveClass('text-body')
    })

    it('should forward refs correctly', () => {
      const ref = React.createRef<HTMLParagraphElement>()
      render(<CardDescription ref={ref}>Description</CardDescription>)
      expect(ref.current).toBeInstanceOf(HTMLParagraphElement)
    })

    it('should render children correctly', () => {
      render(
        <CardDescription>
          <span>Description text</span>
        </CardDescription>
      )
      expect(screen.getByText('Description text')).toBeInTheDocument()
    })

    it('should spread additional props', () => {
      render(<CardDescription data-testid="desc" id="desc-1">Description</CardDescription>)
      const description = screen.getByTestId('desc')
      expect(description).toHaveAttribute('id', 'desc-1')
    })

    it('should handle empty children', () => {
      const { container } = render(<CardDescription />)
      const description = container.querySelector('p')
      expect(description).toBeInTheDocument()
    })
  })

  // ========================================
  // CardContent Tests
  // ========================================
  describe('CardContent', () => {
    it('should render as div element', () => {
      render(<CardContent>Content</CardContent>)
      const content = screen.getByText('Content')
      expect(content.tagName).toBe('DIV')
    })

    it('should apply default classes', () => {
      render(<CardContent>Content</CardContent>)
      const content = screen.getByText('Content')
      expect(content).toHaveClass('p-8')
      expect(content).toHaveClass('pt-0')
    })

    it('should apply custom className', () => {
      render(<CardContent className="custom-content">Content</CardContent>)
      const content = screen.getByText('Content')
      expect(content).toHaveClass('custom-content')
      expect(content).toHaveClass('p-8')
    })

    it('should forward refs correctly', () => {
      const ref = React.createRef<HTMLDivElement>()
      render(<CardContent ref={ref}>Content</CardContent>)
      expect(ref.current).toBeInstanceOf(HTMLDivElement)
    })

    it('should render children correctly', () => {
      render(
        <CardContent>
          <div>Content text</div>
        </CardContent>
      )
      expect(screen.getByText('Content text')).toBeInTheDocument()
    })

    it('should spread additional props', () => {
      render(<CardContent data-testid="content" id="content-1">Content</CardContent>)
      const content = screen.getByTestId('content')
      expect(content).toHaveAttribute('id', 'content-1')
    })

    it('should handle empty children', () => {
      const { container } = render(<CardContent />)
      const content = container.querySelector('.p-8.pt-0')
      expect(content).toBeInTheDocument()
    })
  })

  // ========================================
  // CardFooter Tests
  // ========================================
  describe('CardFooter', () => {
    it('should render as div element', () => {
      render(<CardFooter>Footer</CardFooter>)
      const footer = screen.getByText('Footer')
      expect(footer.tagName).toBe('DIV')
    })

    it('should apply default classes', () => {
      render(<CardFooter>Footer</CardFooter>)
      const footer = screen.getByText('Footer')
      expect(footer).toHaveClass('flex')
      expect(footer).toHaveClass('items-center')
      expect(footer).toHaveClass('p-8')
      expect(footer).toHaveClass('pt-0')
    })

    it('should apply custom className', () => {
      render(<CardFooter className="custom-footer">Footer</CardFooter>)
      const footer = screen.getByText('Footer')
      expect(footer).toHaveClass('custom-footer')
      expect(footer).toHaveClass('flex')
    })

    it('should forward refs correctly', () => {
      const ref = React.createRef<HTMLDivElement>()
      render(<CardFooter ref={ref}>Footer</CardFooter>)
      expect(ref.current).toBeInstanceOf(HTMLDivElement)
    })

    it('should render children correctly', () => {
      render(
        <CardFooter>
          <button>Action</button>
        </CardFooter>
      )
      expect(screen.getByText('Action')).toBeInTheDocument()
    })

    it('should spread additional props', () => {
      render(<CardFooter data-testid="footer" id="footer-1">Footer</CardFooter>)
      const footer = screen.getByTestId('footer')
      expect(footer).toHaveAttribute('id', 'footer-1')
    })

    it('should handle empty children', () => {
      const { container } = render(<CardFooter />)
      const footer = container.querySelector('.flex.items-center')
      expect(footer).toBeInTheDocument()
    })
  })

  // ========================================
  // Edge Cases Tests
  // ========================================
  describe('Edge Cases', () => {
    it('should handle numeric children in Card', () => {
      render(<Card>{0}</Card>)
      expect(screen.getByText('0')).toBeInTheDocument()
    })

    it('should handle null children in Card', () => {
      const { container } = render(<Card>{null}</Card>)
      const card = container.firstChild as HTMLElement
      expect(card).toBeInTheDocument()
    })

    it('should handle undefined children in Card', () => {
      const { container } = render(<Card>{undefined}</Card>)
      const card = container.firstChild as HTMLElement
      expect(card).toBeInTheDocument()
    })

    it('should handle multiple children in CardHeader', () => {
      render(
        <CardHeader>
          <span>First</span>
          <span>Second</span>
        </CardHeader>
      )
      expect(screen.getByText('First')).toBeInTheDocument()
      expect(screen.getByText('Second')).toBeInTheDocument()
    })

    it('should handle long content in CardDescription', () => {
      const longText = 'A'.repeat(1000)
      render(<CardDescription>{longText}</CardDescription>)
      expect(screen.getByText(longText)).toBeInTheDocument()
    })

    it('should handle complex nested children in CardContent', () => {
      render(
        <CardContent>
          <div>
            <ul>
              <li>Item 1</li>
              <li>Item 2</li>
            </ul>
          </div>
        </CardContent>
      )
      expect(screen.getByText('Item 1')).toBeInTheDocument()
      expect(screen.getByText('Item 2')).toBeInTheDocument()
    })
  })

  // ========================================
  // Integration Tests
  // ========================================
  describe('Integration', () => {
    it('should work with all components together', () => {
      render(
        <Card>
          <CardHeader>
            <CardTitle>Card Title</CardTitle>
            <CardDescription>Card description text</CardDescription>
          </CardHeader>
          <CardContent>
            <p>Card content goes here</p>
          </CardContent>
          <CardFooter>
            <button>Action</button>
          </CardFooter>
        </Card>
      )

      expect(screen.getByText('Card Title')).toBeInTheDocument()
      expect(screen.getByText('Card description text')).toBeInTheDocument()
      expect(screen.getByText('Card content goes here')).toBeInTheDocument()
      expect(screen.getByText('Action')).toBeInTheDocument()
    })

    it('should work with custom classes on all components', () => {
      render(
        <Card className="custom-card">
          <CardHeader className="custom-header">
            <CardTitle className="custom-title">Title</CardTitle>
            <CardDescription className="custom-desc">Description</CardDescription>
          </CardHeader>
          <CardContent className="custom-content">Content</CardContent>
          <CardFooter className="custom-footer">Footer</CardFooter>
        </Card>
      )

      const card = screen.getByText('Title').closest('.custom-card')
      expect(card).toHaveClass('custom-card')
      expect(screen.getByText('Title').parentElement).toHaveClass('custom-header')
      expect(screen.getByText('Title')).toHaveClass('custom-title')
      expect(screen.getByText('Description')).toHaveClass('custom-desc')
      expect(screen.getByText('Content')).toHaveClass('custom-content')
      expect(screen.getByText('Footer')).toHaveClass('custom-footer')
    })

    it('should maintain refs on all components', () => {
      const cardRef = React.createRef<HTMLDivElement>()
      const headerRef = React.createRef<HTMLDivElement>()
      const titleRef = React.createRef<HTMLParagraphElement>()
      const descRef = React.createRef<HTMLParagraphElement>()
      const contentRef = React.createRef<HTMLDivElement>()
      const footerRef = React.createRef<HTMLDivElement>()

      render(
        <Card ref={cardRef}>
          <CardHeader ref={headerRef}>
            <CardTitle ref={titleRef}>Title</CardTitle>
            <CardDescription ref={descRef}>Description</CardDescription>
          </CardHeader>
          <CardContent ref={contentRef}>Content</CardContent>
          <CardFooter ref={footerRef}>Footer</CardFooter>
        </Card>
      )

      expect(cardRef.current).toBeInstanceOf(HTMLDivElement)
      expect(headerRef.current).toBeInstanceOf(HTMLDivElement)
      expect(titleRef.current).toBeInstanceOf(HTMLHeadingElement)
      expect(descRef.current).toBeInstanceOf(HTMLParagraphElement)
      expect(contentRef.current).toBeInstanceOf(HTMLDivElement)
      expect(footerRef.current).toBeInstanceOf(HTMLDivElement)
    })

    it('should work with partial components', () => {
      render(
        <Card>
          <CardHeader>
            <CardTitle>Title only</CardTitle>
          </CardHeader>
          <CardContent>Content without footer</CardContent>
        </Card>
      )

      expect(screen.getByText('Title only')).toBeInTheDocument()
      expect(screen.getByText('Content without footer')).toBeInTheDocument()
    })

    it('should work with only CardContent', () => {
      render(
        <Card>
          <CardContent>Simple card with just content</CardContent>
        </Card>
      )

      expect(screen.getByText('Simple card with just content')).toBeInTheDocument()
    })

    it('should work with multiple CardFooter children', () => {
      render(
        <Card>
          <CardFooter>
            <button>Cancel</button>
            <button>Submit</button>
          </CardFooter>
        </Card>
      )

      expect(screen.getByText('Cancel')).toBeInTheDocument()
      expect(screen.getByText('Submit')).toBeInTheDocument()
    })

    it('should spread props on all components in integration', () => {
      render(
        <Card data-testid="card" id="card-1">
          <CardHeader data-testid="header" id="header-1">
            <CardTitle data-testid="title" id="title-1">Title</CardTitle>
          </CardHeader>
        </Card>
      )

      expect(screen.getByTestId('card')).toHaveAttribute('id', 'card-1')
      expect(screen.getByTestId('header')).toHaveAttribute('id', 'header-1')
      expect(screen.getByTestId('title')).toHaveAttribute('id', 'title-1')
    })
  })
})
