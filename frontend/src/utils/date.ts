export function formatDateOnly(date: Date): string {
  const y = date.getFullYear()
  const m = String(date.getMonth() + 1).padStart(2, '0')
  const d = String(date.getDate()).padStart(2, '0')
  return `${y}-${m}-${d}`
}

export function dateWithOffset(offsetDays: number): string {
  const date = new Date()
  date.setDate(date.getDate() + offsetDays)
  return formatDateOnly(date)
}
