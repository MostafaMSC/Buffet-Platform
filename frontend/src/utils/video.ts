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
