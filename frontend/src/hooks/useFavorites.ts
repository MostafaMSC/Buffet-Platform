import { useCallback, useEffect, useState } from 'react'

const STORAGE_KEY = 'buffet_favorites'
const EVENT = 'buffet-favorites-changed'

/// Favourites live on the device. The platform has no customer accounts — bookings are
/// keyed by phone number and a confirmation code — so there is no server-side "me" to hang
/// a favourites list off yet. Everything goes through this hook, so moving it behind an
/// API later is a change in one file rather than in every card.
function read(): number[] {
  try {
    const raw = localStorage.getItem(STORAGE_KEY)
    return raw ? (JSON.parse(raw) as number[]) : []
  } catch {
    return []
  }
}

function write(ids: number[]) {
  try {
    localStorage.setItem(STORAGE_KEY, JSON.stringify(ids))
  } catch {
    // A private window with storage blocked still works; the list just won't persist.
  }
  window.dispatchEvent(new CustomEvent(EVENT))
}

export function useFavorites() {
  const [ids, setIds] = useState<number[]>(read)

  useEffect(() => {
    const sync = () => setIds(read())
    window.addEventListener(EVENT, sync)
    window.addEventListener('storage', sync)
    return () => {
      window.removeEventListener(EVENT, sync)
      window.removeEventListener('storage', sync)
    }
  }, [])

  const toggle = useCallback((serviceId: number) => {
    const current = read()
    write(current.includes(serviceId) ? current.filter((id) => id !== serviceId) : [...current, serviceId])
  }, [])

  const isFavorite = useCallback((serviceId: number) => ids.includes(serviceId), [ids])

  return { ids, toggle, isFavorite, count: ids.length }
}
