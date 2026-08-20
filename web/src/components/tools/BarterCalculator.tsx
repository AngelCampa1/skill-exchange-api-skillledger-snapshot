'use client'

import { useState, useMemo } from'react'
import { categoriesData } from'@/lib/data/categories-data'

export function BarterCalculator() {
  const [categoryA, setCategoryA] = useState(categoriesData[0].slug)
  const [categoryB, setCategoryB] = useState(categoriesData[1].slug)

  const selectedCategoryA = useMemo(
    () => categoriesData.find((c) => c.slug === categoryA) || categoriesData[0],
    [categoryA]
  )
  const selectedCategoryB = useMemo(
    () => categoriesData.find((c) => c.slug === categoryB) || categoriesData[1],
    [categoryB]
  )

  const [rateA, setRateA] = useState(selectedCategoryA.averageCreditRate)
  const [hoursA, setHoursA] = useState(1)
  const [rateB, setRateB] = useState(selectedCategoryB.averageCreditRate)

  const fmvA = rateA * hoursA
  const hoursB = rateB > 0 ? (rateA * hoursA) / rateB : 0
  const fmvB = rateB * hoursB

  function handleCategoryAChange(slug: string) {
    setCategoryA(slug)
    const cat = categoriesData.find((c) => c.slug === slug)
    if (cat) setRateA(cat.averageCreditRate)
  }

  function handleCategoryBChange(slug: string) {
    setCategoryB(slug)
    const cat = categoriesData.find((c) => c.slug === slug)
    if (cat) setRateB(cat.averageCreditRate)
  }

  return (
    <div className="space-y-8">
      {/* Calculator inputs */}
      <div className="grid grid-cols-1 md:grid-cols-2 gap-8">
        {/* Party A */}
        <div className="card-feature p-6">
          <h3 className="text-lg font-bold mb-4">Party A &mdash; Provider</h3>
          <div className="space-y-4">
            <div>
              <label htmlFor="categoryA" className="block text-sm font-medium mb-1">
                Skill Category
              </label>
              <select
                id="categoryA"
                value={categoryA}
                onChange={(e) => handleCategoryAChange(e.target.value)}
                className="w-full rounded-lg border border-border bg-background px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-primary"
              >
                {categoriesData.map((cat) => (
                  <option key={cat.slug} value={cat.slug}>
                    {cat.name}
                  </option>
                ))}
              </select>
            </div>
            <div>
              <label htmlFor="rateA" className="block text-sm font-medium mb-1">
                Hourly Rate (credits)
              </label>
              <input
                id="rateA"
                type="number"
                min={1}
                value={rateA}
                onChange={(e) => setRateA(Math.max(1, Number(e.target.value)))}
                className="w-full rounded-lg border border-border bg-background px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-primary"
              />
            </div>
            <div>
              <label htmlFor="hoursA" className="block text-sm font-medium mb-1">
                Hours Provided
              </label>
              <input
                id="hoursA"
                type="number"
                min={0.25}
                step={0.25}
                value={hoursA}
                onChange={(e) => setHoursA(Math.max(0.25, Number(e.target.value)))}
                className="w-full rounded-lg border border-border bg-background px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-primary"
              />
            </div>
          </div>
        </div>

        {/* Party B */}
        <div className="card-feature p-6">
          <h3 className="text-lg font-bold mb-4">Party B &mdash; Recipient</h3>
          <div className="space-y-4">
            <div>
              <label htmlFor="categoryB" className="block text-sm font-medium mb-1">
                Skill Category
              </label>
              <select
                id="categoryB"
                value={categoryB}
                onChange={(e) => handleCategoryBChange(e.target.value)}
                className="w-full rounded-lg border border-border bg-background px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-primary"
              >
                {categoriesData.map((cat) => (
                  <option key={cat.slug} value={cat.slug}>
                    {cat.name}
                  </option>
                ))}
              </select>
            </div>
            <div>
              <label htmlFor="rateB" className="block text-sm font-medium mb-1">
                Hourly Rate (credits)
              </label>
              <input
                id="rateB"
                type="number"
                min={1}
                value={rateB}
                onChange={(e) => setRateB(Math.max(1, Number(e.target.value)))}
                className="w-full rounded-lg border border-border bg-background px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-primary"
              />
            </div>
            <div className="pt-6">
              <p className="text-sm text-muted-foreground">
                Hours are calculated automatically based on the exchange rate.
              </p>
            </div>
          </div>
        </div>
      </div>

      {/* Result */}
      <div className="card-feature p-6 bg-primary/5 border-primary/20">
        <h3 className="text-lg font-bold mb-4">Exchange Result</h3>
        <p className="text-base leading-relaxed mb-4">
          Party A provides{''}
          <span className="font-bold">{hoursA}</span> hour{hoursA !== 1 ?'s' :''} of{''}
          <span className="font-bold">{selectedCategoryA.name}</span>
          {''}&rarr;{''}
          Party B provides{''}
          <span className="font-bold">{hoursB.toFixed(2)}</span> hour{hoursB !== 1 ?'s' :''} of{''}
          <span className="font-bold">{selectedCategoryB.name}</span>
        </p>

        <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
          <div className="rounded-lg border border-border bg-background p-4">
            <p className="text-sm text-muted-foreground mb-1">FMV &mdash; Party A</p>
            <p className="text-2xl font-bold">{fmvA.toLocaleString()} credits</p>
          </div>
          <div className="rounded-lg border border-border bg-background p-4">
            <p className="text-sm text-muted-foreground mb-1">FMV &mdash; Party B</p>
            <p className="text-2xl font-bold">{fmvB.toLocaleString()} credits</p>
          </div>
        </div>
      </div>

      {/* Tax reminder */}
      <div className="rounded-lg border border-amber-300 bg-amber-50   p-5">
        <p className="text-sm font-semibold text-amber-800  mb-1">
          Tax Reminder
        </p>
        <p className="text-sm text-amber-700">
          Under IRC &sect; 61, both parties must report the fair market value of services received
          as taxable income. Consult a qualified tax professional for guidance specific to your
          situation.
        </p>
      </div>
    </div>
  )
}
