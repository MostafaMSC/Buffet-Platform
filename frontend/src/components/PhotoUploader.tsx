import { useRef, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { api } from '../api/client'

interface Props {
  urls: string[]
  onChange: (urls: string[]) => void
  maxPhotos?: number
}

export function PhotoUploader({ urls, onChange, maxPhotos = 6 }: Props) {
  const { t } = useTranslation()
  const inputRef = useRef<HTMLInputElement>(null)
  const [uploading, setUploading] = useState(false)

  const handleFile = async (file: File) => {
    setUploading(true)
    try {
      const formData = new FormData()
      formData.append('file', file)
      const res = await api.post<{ url: string }>('/uploads', formData, {
        headers: { 'Content-Type': 'multipart/form-data' },
      })
      onChange([...urls, res.data.url])
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
          {uploading && <span style={{ marginInlineStart: '0.5rem' }}>{t('offeringForm.uploading')}</span>}
        </div>
      )}
    </div>
  )
}
