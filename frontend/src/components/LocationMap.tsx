import { useEffect, useRef } from 'react'
import {
  GeolocateControl,
  MapLibreMap,
  Marker,
  NavigationControl,
  setRTLTextPlugin,
  type GeolocatePositionEvent,
  type MapMouseEvent,
} from 'maplibre-gl'
import 'maplibre-gl/dist/maplibre-gl.css'

/// OpenFreeMap's public instance: OpenStreetMap data, no API key, no quota and no billing
/// account behind it. Attribution is a condition of that, and the style ships the control
/// that renders it — so the attribution control stays on.
const STYLE_URL = 'https://tiles.openfreemap.org/styles/liberty'

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

/// One map for both jobs. Passing onPick makes it the owner's location picker — click or drag
/// to place the pin — and leaving it out makes it the read-only map a guest sees.
export default function LocationMap({
  latitude,
  longitude,
  onPick,
  height = 300,
  label,
}: {
  latitude: number | null
  longitude: number | null
  onPick?: (lat: number, lng: number) => void
  height?: number
  label?: string
}) {
  const holder = useRef<HTMLDivElement>(null)
  const map = useRef<MapLibreMap | null>(null)
  const marker = useRef<Marker | null>(null)

  // The call site passes an inline arrow, so onPick is a new function on every render. Reading
  // it through a ref keeps the map a mount-only effect — rebuilding a GL map per keystroke
  // elsewhere in the form would be both slow and visibly jarring.
  const pick = useRef(onPick)
  useEffect(() => { pick.current = onPick })

  const editable = onPick != null

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
    if (!m) return

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
  }, [latitude, longitude, editable])

  return (
    <div
      ref={holder}
      className="location-map"
      style={{ height }}
      role={editable ? 'application' : 'img'}
      aria-label={label}
    />
  )
}
