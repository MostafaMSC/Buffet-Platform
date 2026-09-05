import { useEffect, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { getLocations } from '../api/endpoints'
import type { CountryOption } from '../types'
import { Select } from './ui'

/// Areas are grouped under their city, so a restaurant in Erbil isn't picking from a flat
/// list of every neighbourhood in the country.
export function AreaSelect({
  value,
  onChange,
  required,
}: {
  value: number | ''
  onChange: (areaId: number) => void
  required?: boolean
}) {
  const { i18n } = useTranslation()
  const [countries, setCountries] = useState<CountryOption[]>([])

  useEffect(() => { getLocations().then(setCountries).catch(() => setCountries([])) }, [])

  const isAr = i18n.language === 'ar'

  return (
    <Select value={value} onChange={(e) => onChange(Number(e.target.value))} aria-required={required}>
      <option value="" disabled>—</option>
      {countries.flatMap((country) =>
        country.cities.map((city) => (
          <optgroup key={city.id} label={isAr ? city.nameAr : city.nameEn}>
            {city.areas.map((area) => (
              <option key={area.id} value={area.id}>
                {isAr ? area.nameAr : area.nameEn}
              </option>
            ))}
          </optgroup>
        )),
      )}
    </Select>
  )
}
