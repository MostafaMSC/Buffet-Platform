import { useEffect, useState } from 'react'

/// Holds a value still until it stops changing, so a slider or a text box doesn't fire a
/// request per keystroke.
export function useDebounced<T>(value: T, delayMs = 350): T {
  const [debounced, setDebounced] = useState(value)

  useEffect(() => {
    const timer = setTimeout(() => setDebounced(value), delayMs)
    return () => clearTimeout(timer)
  }, [value, delayMs])

  return debounced
}
