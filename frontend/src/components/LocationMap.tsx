import { useEffect, useMemo, useRef, useState } from 'react'
import {
  GeolocateControl,
  LngLatBounds,
  MapLibreMap,
  Marker,
  NavigationControl,
  setRTLTextPlugin,
  type GeolocatePositionEvent,
  type MapMouseEvent,
} from 'maplibre-gl'
import { useTranslation } from 'react-i18next'
import 'maplibre-gl/dist/maplibre-gl.css'

/// OpenFreeMap's public instance: OpenStreetMap data, no API key, no quota and no billing
/// account behind it. Attribution is a condition of that, and the style ships the control
/// that renders it — so the attribution control stays on.
///
/// Overridable through VITE_MAP_STYLE_URL because whether a tile host is reachable is a
/// property of the network the app is served from, not of this code — swapping provider
/// should not need a rebuild of anything but the env file.
const STYLE_URL = import.meta.env.VITE_MAP_STYLE_URL || 'https://tiles.openfreemap.org/styles/liberty'

/// If the style has not loaded by now the tile host is unreachable, blocked, or too slow to
/// be useful. Long enough not to trip on a slow mobile connection.
const STYLE_TIMEOUT_MS = 10_000

/// @mapbox/mapbox-gl-rtl-text v0.4.0 (BSD-2-Clause), vendored into public/vendor rather than
/// imported: setRTLTextPlugin wants a plain script URL it can load inside a worker, and the
/// package's exports map does not expose its bundled dist build to a bundler anyway.
const RTL_PLUGIN_URL = '/vendor/mapbox-gl-rtl-text.js'

/// Baghdad. An unpinned venue should open somewhere the owner recognises rather than at 0,0.
const FALLBACK_CENTER: [number, number] = [44.3661, 33.3152]

/// Six decimals is roughly 11cm — far past what a hand-dropped pin can mean, and it keeps
/// the stored value short enough to read in the database.
const round = (n: number) => Math.round(n * 1e6) / 1e6

let rtlPluginRequested = false

/// Arabic place names join their letters and run right to left. Without this plugin MapLibre
/// draws them as disconnected glyphs in reverse, which on an Arabic-first product is worse
/// than showing no labels at all. It registers once per page, not once per map.
function ensureRtlPlugin() {
  if (rtlPluginRequested) return
  rtlPluginRequested = true
  try {
    setRTLTextPlugin(RTL_PLUGIN_URL, true)
  } catch {
    // Another bundle copy already registered it; there is nothing to undo.
  }
}

export interface MapPoint {
  id: number
  latitude: number
  longitude: number
  label: string
}

/// One map for three jobs. Passing onPick makes it the owner's location picker — click or drag
/// to place the pin. Passing points plots a whole result set and frames them together. Neither
/// makes it the read-only single-pin map a guest sees on a restaurant page.
export default function LocationMap({
  latitude,
  longitude,
  onPick,
  height = 300,
  label,
  points,
  highlightedId,
  onSelect,
}: {
  latitude?: number | null
  longitude?: number | null
  onPick?: (lat: number, lng: number) => void
  height?: number | string
  label?: string
  points?: MapPoint[]
  highlightedId?: number | null
  onSelect?: (id: number) => void
}) {
  const { t } = useTranslation()
  const [failed, setFailed] = useState(false)
  const holder = useRef<HTMLDivElement>(null)
  const map = useRef<MapLibreMap | null>(null)
  const marker = useRef<Marker | null>(null)
  const pins = useRef(new Map<number, Marker>())
  const select = useRef(onSelect)
  useEffect(() => { select.current = onSelect })

  // Rebuild the pins only when the set of places actually changes — not when the parent
  // re-renders because a card was hovered.
  const pointsKey = useMemo(
    () => (points ?? []).map((p) => `${p.id}:${p.latitude}:${p.longitude}`).join('|'),
    [points],
  )

  // The call site passes an inline arrow, so onPick is a new function on every render. Reading
  // it through a ref keeps the map a mount-only effect — rebuilding a GL map per keystroke
  // elsewhere in the form would be both slow and visibly jarring.
  const pick = useRef(onPick)
  useEffect(() => { pick.current = onPick })

  const editable = onPick != null
  const interactive = editable || onSelect != null

  useEffect(() => {
    const container = holder.current
    if (!container) return
    ensureRtlPlugin()

    const m = new MapLibreMap({
      container,
      style: STYLE_URL,
      center: latitude != null && longitude != null ? [longitude, latitude] : FALLBACK_CENTER,
      // Street level once there is a pin to look at, city level while there is not.
      zoom: latitude != null && longitude != null ? 15 : 10,
    })
    m.addControl(new NavigationControl({ showCompass: false }), 'top-right')

    // An unreachable tile host otherwise leaves an empty grey rectangle with nothing to
    // explain it. Only a failure *before* the style loads counts: once the map is up, a
    // single missing tile is not worth covering the whole thing over.
    let styleLoaded = false
    const timer = window.setTimeout(() => { if (!styleLoaded) setFailed(true) }, STYLE_TIMEOUT_MS)
    m.on('load', () => {
      styleLoaded = true
      window.clearTimeout(timer)
      setFailed(false)
    })
    m.on('error', () => { if (!styleLoaded) setFailed(true) })

    if (editable) {
      m.on('click', (e: MapMouseEvent) => pick.current?.(round(e.lngLat.lat), round(e.lngLat.lng)))
      // An owner standing in their own restaurant gets the pin for free.
      const locate = new GeolocateControl({ trackUserLocation: false, showAccuracyCircle: false })
      locate.on('geolocate', (e: GeolocatePositionEvent) => {
        pick.current?.(round(e.coords.latitude), round(e.coords.longitude))
      })
      m.addControl(locate, 'top-right')
      m.getCanvas().style.cursor = 'crosshair'
    } else {
      // Nothing to choose here, so keep the page scrolling past the map instead of zooming it.
      m.scrollZoom.disable()
    }

    map.current = m
    return () => {
      window.clearTimeout(timer)
      m.remove()
      map.current = null
      marker.current = null
    }
    // Mount-only: the pin is synced by the effect below, and rebuilding the map would drop
    // whatever the owner had panned to.
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [editable])

  useEffect(() => {
    const m = map.current
    if (!m || points) return

    if (latitude == null || longitude == null) {
      marker.current?.remove()
      marker.current = null
      return
    }

    if (marker.current) {
      marker.current.setLngLat([longitude, latitude])
      return
    }

    const mk = new Marker({ color: '#c2410c', draggable: editable })
      .setLngLat([longitude, latitude])
      .addTo(m)
    if (editable) {
      mk.on('dragend', () => {
        const p = mk.getLngLat()
        pick.current?.(round(p.lat), round(p.lng))
      })
    }
    marker.current = mk
    m.easeTo({ center: [longitude, latitude], zoom: Math.max(m.getZoom(), 15), duration: 400 })
  }, [latitude, longitude, editable, points])

  // Plot a result set and frame it. Markers carry their own element so the highlight can be
  // toggled with a class instead of tearing the marker down and building it again.
  useEffect(() => {
    const m = map.current
    if (!m || !points) return

    for (const pin of pins.current.values()) pin.remove()
    pins.current.clear()
    if (points.length === 0) return

    const bounds = new LngLatBounds()
    for (const p of points) {
      // MapLibre positions a marker by writing transform on this element, so anything of ours
      // that animates transform has to live on a child — styling the marker itself would
      // overwrite the translate and fling the pin into the corner of the map.
      const el = document.createElement('button')
      el.type = 'button'
      el.className = 'map-marker'
      el.title = p.label
      el.setAttribute('aria-label', p.label)
      const dot = document.createElement('span')
      dot.className = 'map-marker-dot'
      el.appendChild(dot)
      el.addEventListener('click', (ev) => { ev.stopPropagation(); select.current?.(p.id) })
      pins.current.set(p.id, new Marker({ element: el }).setLngLat([p.longitude, p.latitude]).addTo(m))
      bounds.extend([p.longitude, p.latitude])
    }
    // A single result has no extent to fit, so frame it at street level instead.
    if (points.length === 1) m.easeTo({ center: [points[0].longitude, points[0].latitude], zoom: 14, duration: 300 })
    else m.fitBounds(bounds, { padding: 56, maxZoom: 15, duration: 300 })
  }, [pointsKey, points])

  useEffect(() => {
    for (const [id, pin] of pins.current) {
      pin.getElement().classList.toggle('active', id === highlightedId)
    }
  }, [highlightedId, pointsKey])

  return (
    // The height goes on the wrapper, not the map: a percentage height on the map alone
    // resolves against a wrapper that has none, collapsing the whole thing to a couple of
    // pixels while the markers still render at coordinates nobody can click.
    <div className="location-map-wrap" style={{ height }}>
      <div
        ref={holder}
        className="location-map"
        role={interactive ? 'application' : 'img'}
        aria-label={label}
      />
      {failed && (
        <div className="location-map-fallback" role="status">
          <span>{t('map.unavailable')}</span>
        </div>
      )}
    </div>
  )
}
