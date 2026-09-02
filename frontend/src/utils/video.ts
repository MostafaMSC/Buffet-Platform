/**
 * Turns a video page URL (Facebook/YouTube/Instagram) into an embeddable iframe src,
 * so restaurants can paste a link to a video they already posted and have it play
 * inline instead of sending the visitor away. Returns null for anything we don't
 * know how to embed — the caller should fall back to a plain external link.
 */
export function getVideoEmbedUrl(url: string): string | null {
  let parsed: URL
  try {
    parsed = new URL(url)
  } catch {
    return null
  }

  const host = parsed.hostname.replace(/^www\./, '')

  if (host === 'facebook.com' || host === 'fb.watch' || host === 'm.facebook.com') {
    return `https://www.facebook.com/plugins/video.php?href=${encodeURIComponent(url)}&show_text=false`
  }

  if (host === 'youtube.com' || host === 'youtu.be') {
    let videoId: string | null = null
    if (host === 'youtu.be') {
      videoId = parsed.pathname.slice(1)
    } else if (parsed.pathname === '/watch') {
      videoId = parsed.searchParams.get('v')
    } else if (parsed.pathname.startsWith('/shorts/')) {
      videoId = parsed.pathname.split('/')[2]
    } else if (parsed.pathname.startsWith('/embed/')) {
      videoId = parsed.pathname.split('/')[2]
    }
    return videoId ? `https://www.youtube.com/embed/${videoId}` : null
  }

  if (host === 'instagram.com') {
    const match = parsed.pathname.match(/^\/(p|reel|tv)\/([^/]+)/)
    return match ? `https://www.instagram.com/${match[1]}/${match[2]}/embed` : null
  }

  return null
}

const DIRECT_VIDEO_EXTENSIONS = ['.mp4', '.webm', '.mov']

/**
 * True for a URL that points at an actual video file (an upload we're hosting
 * ourselves, or any other direct .mp4/.webm/.mov link) rather than a page on an
 * external platform. These play natively via <video>, which is far more reliable
 * than depending on a third party's embed/privacy rules.
 */
export function isDirectVideoFile(url: string | null | undefined): url is string {
  if (!url) return false
  const path = url.split('?')[0].toLowerCase()
  return DIRECT_VIDEO_EXTENSIONS.some((ext) => path.endsWith(ext))
}
