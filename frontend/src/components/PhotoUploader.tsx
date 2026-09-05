import { useRef, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { api } from '../api/client'
import { apiError } from '../utils/format'

interface Props {
  urls: string[]
  onChange: (urls: string[]) => void
  maxPhotos?: number
}

export function PhotoUploader({ urls, onChange, maxPhotos = 6 }: Props) {
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
      onChange([...urls, res.data.url])
    } catch (err: unknown) {
      // A rejected upload used to reject silently: the file simply never appeared and the
      // owner was left guessing whether it was too large, the wrong format, or still going.
      setError(apiError(err, t('services.photoUploadError'), t))
    } finally {
      setUploading(false)
      if (inputRef.current) inputRef.current.value = ''
    }
  }

  const removeAt = (index: number) => {
    onChange(urls.filter((_, i) => i !== index))
  }

  return (
    <div>
      <div className="photo-thumb-row">
        {urls.map((url, i) => (
          <div className="photo-thumb" key={url}>
            <img src={url} alt="" />
            <button type="button" onClick={() => removeAt(i)} aria-label="remove">
              ×
            </button>
          </div>
        ))}
      </div>
      {urls.length < maxPhotos && (
        <div style={{ marginTop: '0.6rem' }}>
          <input
            ref={inputRef}
            type="file"
            accept="image/png,image/jpeg,image/webp"
            onChange={(e) => {
              const file = e.target.files?.[0]
              if (file) handleFile(file)
            }}
          />
          {uploading && <span style={{ marginInlineStart: '0.5rem' }}>{t('services.uploading')}</span>}
        </div>
      )}
      {error && <div className="alert bad" style={{ marginTop: 'var(--sp-3)' }}>{error}</div>}
    </div>
  )
}
