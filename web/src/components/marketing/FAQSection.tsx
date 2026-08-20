interface FAQSectionProps {
  faqs: Array<{ question: string; answer: string }>
  title?: string
}

export function FAQSection({ faqs, title = 'Frequently Asked Questions' }: FAQSectionProps) {
  if (!faqs.length) return null
  return (
    <section className="max-w-3xl">
      <h2 className="text-2xl font-bold mb-8">{title}</h2>
      <div className="space-y-4">
        {faqs.map((faq, i) => (
          <details key={i} className="border border-border rounded-xl p-6 group">
            <summary className="font-bold cursor-pointer list-none flex items-center justify-between">
              {faq.question}
              <span className="text-muted-foreground ml-4 flex-shrink-0">+</span>
            </summary>
            <p className="text-muted-foreground leading-relaxed mt-4">{faq.answer}</p>
          </details>
        ))}
      </div>
    </section>
  )
}
