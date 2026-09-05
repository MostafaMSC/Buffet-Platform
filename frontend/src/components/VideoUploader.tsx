import { useRef, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { api } from '../api/client'
import { apiError } from '../utils/format'

interface Props {
  url: string | null
  onChange: (url: string | null) => void
}

export function VideoUploader({ url, onChange }: Props) {
  const { t } = useTranslation()
  const inputRef = useRef<HTMLInputElement>(null)
  const [uploading, setUploading] = useState(false)
  const [error, setError] = useState<string | null>(null)

  const handleFile = async (file: File) => {
    setUploading(true)
    setError(null)
    try {
      const formData = new FormData()
      formData.append('file', file)
      const res = await api.post<{ url: string }>('/uploads', formData, {
        headers: { 'Content-Type': 'multipart/form-data' },
      })
      onChange(res.data.url)
    } catch (err: unknown) {
      // Via apiError so the upload validator's own reason ("Unsupported file type…") reaches
      // the owner, instead of the generic wrapper the server sends alongside it.
      setError(apiError(err, t('services.videoUploadError'), t))
    } finally {
      setUploading(false)
      if (inputRef.current) inputRef.current.value = ''
    }
  }

  if (url) {
    return (
      <div>
        <div className="video-upload-preview">
          {/* eslint-disable-next-line jsx-a11y/media-has-caption */}
          <video src={url} controls muted />
          <button type="button" className="btn small secondary" onClick={() => onChange(null)}>
            {t('services.removeVideo')}
          </button>
        </div>
      </div>
    )
  }

  return (
    <div>
      <input
        ref={inputRef}
        type="file"
        accept="video/mp4,video/webm,video/quicktime"
        onChange={(e) => {
          const file = e.target.files?.[0]
          if (file) handleFile(file)
        }}
      />
      {uploading && <span style={{ marginInlineStart: '0.5rem' }}>{t('services.uploading')}</span>}
      {error && <div className="alert bad" style={{ marginTop: 'var(--sp-3)' }}>{error}</div>}
    </div>
  )
}
