import { useEffect, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { useNavigate, useParams } from 'react-router-dom'
import { createService, getServiceForEdit, updateService } from '../../api/endpoints'
import { PhotoUploader } from '../../components/PhotoUploader'
import { VideoUploader } from '../../components/VideoUploader'
import { Skeleton } from '../../components/ui'
import {
  CUISINES,
  DIETARY_TAGS,
  MEAL_TYPES,
  type Cuisine,
  type DietaryTag,
  type RecurrenceType,
  type ServiceInput,
  type WeekdayName,
} from '../../types'
import { apiError } from '../../utils/format'

const WEEKDAYS: WeekdayName[] = ['Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday', 'Sunday']
const TABS = ['general', 'pricing', 'schedule', 'menuTab', 'rules', 'photos'] as const
type Tab = (typeof TABS)[number]

const BLANK: ServiceInput = {
  serviceType: 'Buffet',
  name: '',
  nameAr: '',
  description: '',
  descriptionAr: '',
  mealType: 'Lunch',
  cuisines: [],
  dietary: [],
  status: 'Active',
  pricingModel: 'PerPerson',
  pricePerAdult: 25000,
  pricePerChild: null,
  childAgeFrom: null,
  childAgeTo: null,
  freeUnderAge: null,
  packagePrice: null,
  packageGuests: null,
  minGuests: 1,
  maxGuests: null,
  durationMinutes: 120,
  opensAt: '13:00',
  closesAt: '16:00',
  recurrence: 'Daily',
  weekdays: [],
  ramadanStartDate: null,
  ramadanEndDate: null,
  oneOffDate: null,
  bookingMode: 'Instant',
  minAdvanceMinutes: 0,
  cancellationCutoffMinutes: null,
  capacity: 50,
  slots: [],
  photoUrls: [],
  videoUrl: null,
  menu: [],
}

/// One form for creating and editing, split into tabs so a restaurant can fill in what it
/// knows now and come back for the menu later.
export function ServiceEditorPage() {
  const { id } = useParams()
  const isNew = !id
  const { t } = useTranslation()
  const navigate = useNavigate()

  const [form, setForm] = useState<ServiceInput | null>(isNew ? BLANK : null)
  const [tab, setTab] = useState<Tab>('general')
  const [saving, setSaving] = useState(false)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    if (isNew) return
    getServiceForEdit(Number(id)).then((s) => {
      setForm({
        serviceType: s.serviceType,
        name: s.name,
        nameAr: s.nameAr,
        description: s.description ?? '',
        descriptionAr: s.descriptionAr ?? '',
        mealType: s.mealType,
        cuisines: s.cuisines,
        dietary: s.dietary,
        status: s.status,
        pricingModel: s.pricingModel,
        pricePerAdult: s.pricePerAdult,
        pricePerChild: s.pricePerChild,
        childAgeFrom: s.childAgeFrom,
        childAgeTo: s.childAgeTo,
        freeUnderAge: s.freeUnderAge,
        packagePrice: s.packagePrice,
        packageGuests: s.packageGuests,
        minGuests: s.minGuests,
        maxGuests: s.maxGuests,
        durationMinutes: s.durationMinutes,
        opensAt: s.opensAt,
        closesAt: s.closesAt,
        recurrence: s.recurrence,
        weekdays: s.weekdays,
        ramadanStartDate: s.ramadanStartDate,
        ramadanEndDate: s.ramadanEndDate,
        oneOffDate: s.oneOffDate,
        bookingMode: s.bookingMode,
        minAdvanceMinutes: s.minAdvanceMinutes,
        cancellationCutoffMinutes: s.cancellationCutoffMinutes,
        capacity: s.capacity,
        slots: s.slots.map((slot) => ({
          startTime: slot.startTime,
          endTime: slot.endTime,
          capacity: slot.capacity,
          bufferMinutes: slot.bufferMinutes,
        })),
        photoUrls: s.photoUrls,
        videoUrl: s.videoUrl,
        menu: s.menu.map((section) => ({
          name: section.name,
          nameAr: section.nameAr,
          items: section.items.map((item) => ({
            name: item.name,
            nameAr: item.nameAr,
            description: item.description,
            descriptionAr: item.descriptionAr,
            dietary: item.dietary,
          })),
        })),
      })
    })
  }, [id, isNew])

  if (!form) return <Skeleton height={420} radius={14} />

  const set = <K extends keyof ServiceInput>(key: K, value: ServiceInput[K]) =>
    setForm((prev) => (prev ? { ...prev, [key]: value } : prev))

  const toggleIn = <T,>(list: T[], value: T): T[] =>
    list.includes(value) ? list.filter((v) => v !== value) : [...list, value]

  const save = async () => {
    setSaving(true)
    setError(null)
    try {
      const payload: ServiceInput = {
        ...form,
        description: form.description || null,
        descriptionAr: form.descriptionAr || null,
        videoUrl: form.videoUrl || null,
        // Capacity and sittings are mutually exclusive; send only the one in use.
        capacity: form.slots.length > 0 ? null : form.capacity,
      }
      if (isNew) {
        await createService(payload)
      } else {
        await updateService(Number(id), payload)
      }
      navigate('/dashboard/services')
    } catch (err) {
      setError(apiError(err, t('common.error')))
    } finally {
      setSaving(false)
    }
  }

  return (
    <div className="stack stack-4">
      <div className="section-head">
        <h1 style={{ fontSize: '1.4rem' }}>{isNew ? t('services.add') : t('services.edit')}</h1>
        <div className="row" style={{ gap: 'var(--sp-2)' }}>
          <button className="btn ghost" onClick={() => navigate('/dashboard/services')}>{t('common.cancel')}</button>
          <button className="btn" onClick={save} disabled={saving}>{t('services.saveService')}</button>
        </div>
      </div>

      <div className="filter-bar">
        {TABS.map((key) => (
          <button key={key} className={`chip ${tab === key ? 'active' : ''}`} onClick={() => setTab(key)}>
            {t(`services.${key === 'menuTab' ? 'menuTab' : key}`)}
          </button>
        ))}
      </div>

      {error && <div className="alert bad">{error}</div>}

      <div className="card card-pad stack stack-4">
        {tab === 'general' && (
          <>
            <div className="form-grid">
              <label className="field">
                <span>{t('services.type')}</span>
                <select value={form.serviceType} onChange={(e) => set('serviceType', e.target.value as ServiceInput['serviceType'])}>
                  <option value="Buffet">{t('serviceType.Buffet')}</option>
                  <option value="SetMenu">{t('serviceType.SetMenu')}</option>
                </select>
              </label>
              <label className="field">
                <span>{t('services.meal')}</span>
                <select value={form.mealType} onChange={(e) => set('mealType', e.target.value as ServiceInput['mealType'])}>
                  {MEAL_TYPES.map((meal) => <option key={meal} value={meal}>{t(`mealType.${meal}`)}</option>)}
                </select>
              </label>
              <label className="field">
                <span>{t('services.status')}</span>
                <select value={form.status} onChange={(e) => set('status', e.target.value as ServiceInput['status'])}>
                  <option value="Active">{t('serviceStatus.Active')}</option>
                  <option value="Paused">{t('serviceStatus.Paused')}</option>
                  <option value="Draft">{t('serviceStatus.Draft')}</option>
                </select>
              </label>
            </div>

            <div className="form-grid">
              <label className="field">
                <span>{t('services.name')}</span>
                <input value={form.name} onChange={(e) => set('name', e.target.value)} required />
              </label>
              <label className="field">
                <span>{t('services.nameAr')}</span>
                <input value={form.nameAr} onChange={(e) => set('nameAr', e.target.value)} dir="rtl" required />
              </label>
            </div>

            <label className="field">
              <span>{t('services.description')}</span>
              <textarea value={form.description ?? ''} onChange={(e) => set('description', e.target.value)} />
            </label>
            <label className="field">
              <span>{t('services.descriptionAr')}</span>
              <textarea value={form.descriptionAr ?? ''} onChange={(e) => set('descriptionAr', e.target.value)} dir="rtl" />
            </label>

            <div className="field">
              <span>{t('services.cuisines')}</span>
              <div className="chip-wrap">
                {CUISINES.map((cuisine) => (
                  <button
                    key={cuisine}
                    type="button"
                    className={`chip sm ${form.cuisines.includes(cuisine) ? 'active' : ''}`}
                    onClick={() => set('cuisines', toggleIn(form.cuisines, cuisine as Cuisine))}
                  >
                    {t(`cuisine.${cuisine}`)}
                  </button>
                ))}
              </div>
            </div>

            <div className="field">
              <span>{t('services.dietary')}</span>
              <div className="chip-wrap">
                {DIETARY_TAGS.map((tag) => (
                  <button
                    key={tag}
                    type="button"
                    className={`chip sm ${form.dietary.includes(tag) ? 'active' : ''}`}
                    onClick={() => set('dietary', toggleIn(form.dietary, tag as DietaryTag))}
                  >
                    {t(`dietary.${tag}`)}
                  </button>
                ))}
              </div>
            </div>
          </>
        )}

        {tab === 'pricing' && (
          <>
            <div className="field">
              <span>{t('services.pricingModel')}</span>
              <div className="chip-wrap">
                <button
                  type="button"
                  className={`chip ${form.pricingModel === 'PerPerson' ? 'active' : ''}`}
                  onClick={() => set('pricingModel', 'PerPerson')}
                >
                  {t('services.perPerson')}
                </button>
                <button
                  type="button"
                  className={`chip ${form.pricingModel === 'PerPackage' ? 'active' : ''}`}
                  onClick={() => set('pricingModel', 'PerPackage')}
                >
                  {t('services.perPackage')}
                </button>
              </div>
            </div>

            {form.pricingModel === 'PerPerson' ? (
              <div className="form-grid">
                <label className="field">
                  <span>{t('services.adultPrice')}</span>
                  <input type="number" min={0} step={500} value={form.pricePerAdult} onChange={(e) => set('pricePerAdult', Number(e.target.value))} />
                </label>
                <label className="field">
                  <span>{t('services.childPrice')}</span>
                  <input
                    type="number"
                    min={0}
                    step={500}
                    value={form.pricePerChild ?? ''}
                    onChange={(e) => set('pricePerChild', e.target.value === '' ? null : Number(e.target.value))}
                  />
                </label>
                <label className="field">
                  <span>{t('services.childAges')} ({t('common.from')})</span>
                  <input type="number" min={0} max={17} value={form.childAgeFrom ?? ''} onChange={(e) => set('childAgeFrom', e.target.value === '' ? null : Number(e.target.value))} />
                </label>
                <label className="field">
                  <span>{t('services.childAges')} ({t('common.to')})</span>
                  <input type="number" min={0} max={17} value={form.childAgeTo ?? ''} onChange={(e) => set('childAgeTo', e.target.value === '' ? null : Number(e.target.value))} />
                </label>
                <label className="field">
                  <span>{t('services.freeUnder')}</span>
                  <input type="number" min={0} max={17} value={form.freeUnderAge ?? ''} onChange={(e) => set('freeUnderAge', e.target.value === '' ? null : Number(e.target.value))} />
                </label>
              </div>
            ) : (
              <div className="form-grid">
                <label className="field">
                  <span>{t('services.packagePrice')}</span>
                  <input type="number" min={0} step={500} value={form.packagePrice ?? ''} onChange={(e) => set('packagePrice', e.target.value === '' ? null : Number(e.target.value))} />
                </label>
                <label className="field">
                  <span>{t('services.packageGuests')}</span>
                  <input type="number" min={1} value={form.packageGuests ?? ''} onChange={(e) => set('packageGuests', e.target.value === '' ? null : Number(e.target.value))} />
                </label>
              </div>
            )}

            <div className="form-grid">
              <label className="field">
                <span>{t('services.minGuests')}</span>
                <input type="number" min={1} value={form.minGuests} onChange={(e) => set('minGuests', Number(e.target.value))} />
              </label>
              <label className="field">
                <span>{t('services.maxGuests')}</span>
                <input type="number" min={1} value={form.maxGuests ?? ''} onChange={(e) => set('maxGuests', e.target.value === '' ? null : Number(e.target.value))} />
              </label>
              <label className="field">
                <span>{t('services.duration')}</span>
                <input type="number" min={15} step={15} value={form.durationMinutes ?? ''} onChange={(e) => set('durationMinutes', e.target.value === '' ? null : Number(e.target.value))} />
              </label>
            </div>
          </>
        )}

        {tab === 'schedule' && (
          <>
            <div className="form-grid">
              <label className="field">
                <span>{t('services.opensAt')}</span>
                <input type="time" value={form.opensAt} onChange={(e) => set('opensAt', e.target.value)} />
              </label>
              <label className="field">
                <span>{t('services.closesAt')}</span>
                <input type="time" value={form.closesAt} onChange={(e) => set('closesAt', e.target.value)} />
              </label>
              <label className="field">
                <span>{t('services.recurrence')}</span>
                <select value={form.recurrence} onChange={(e) => set('recurrence', e.target.value as RecurrenceType)}>
                  {(['Daily', 'SpecificWeekdays', 'RamadanMode', 'OneOff'] as RecurrenceType[]).map((r) => (
                    <option key={r} value={r}>{t(`recurrence.${r}`)}</option>
                  ))}
                </select>
              </label>
            </div>

            {form.recurrence === 'SpecificWeekdays' && (
              <div className="field">
                <span>{t('services.weekdays')}</span>
                <div className="chip-wrap">
                  {WEEKDAYS.map((day) => (
                    <button
                      key={day}
                      type="button"
                      className={`chip sm ${form.weekdays.includes(day) ? 'active' : ''}`}
                      onClick={() => set('weekdays', toggleIn(form.weekdays, day))}
                    >
                      {t(`weekday.${day}`)}
                    </button>
                  ))}
                </div>
              </div>
            )}

            {form.recurrence === 'RamadanMode' && (
              <div className="form-grid">
                <label className="field">
                  <span>{t('common.from')}</span>
                  <input type="date" value={form.ramadanStartDate ?? ''} onChange={(e) => set('ramadanStartDate', e.target.value || null)} />
                </label>
                <label className="field">
                  <span>{t('common.to')}</span>
                  <input type="date" value={form.ramadanEndDate ?? ''} onChange={(e) => set('ramadanEndDate', e.target.value || null)} />
                </label>
              </div>
            )}

            {form.recurrence === 'OneOff' && (
              <label className="field" style={{ maxWidth: 240 }}>
                <span>{t('booking.date')}</span>
                <input type="date" value={form.oneOffDate ?? ''} onChange={(e) => set('oneOffDate', e.target.value || null)} />
              </label>
            )}

            <div className="divider" />

            <div className="field">
              <span>{t('services.capacityMode')}</span>
              <div className="chip-wrap">
                <button
                  type="button"
                  className={`chip ${form.slots.length === 0 ? 'active' : ''}`}
                  onClick={() => set('slots', [])}
                >
                  {t('services.wholeWindow')}
                </button>
                <button
                  type="button"
                  className={`chip ${form.slots.length > 0 ? 'active' : ''}`}
                  onClick={() => form.slots.length === 0 && set('slots', [{ startTime: form.opensAt, endTime: form.closesAt, capacity: 40, bufferMinutes: 0 }])}
                >
                  {t('services.slots')}
                </button>
              </div>
            </div>

            {form.slots.length === 0 ? (
              <label className="field" style={{ maxWidth: 240 }}>
                <span>{t('services.capacity')}</span>
                <input type="number" min={1} value={form.capacity ?? ''} onChange={(e) => set('capacity', e.target.value === '' ? null : Number(e.target.value))} />
              </label>
            ) : (
              <div className="stack stack-3">
                {form.slots.map((slot, index) => (
                  <div className="row wrap" key={index} style={{ gap: 'var(--sp-3)', alignItems: 'flex-end' }}>
                    <label className="field" style={{ width: 120 }}>
                      <span>{t('common.from')}</span>
                      <input
                        type="time"
                        value={slot.startTime}
                        onChange={(e) => set('slots', form.slots.map((s, i) => (i === index ? { ...s, startTime: e.target.value } : s)))}
                      />
                    </label>
                    <label className="field" style={{ width: 120 }}>
                      <span>{t('common.to')}</span>
                      <input
                        type="time"
                        value={slot.endTime}
                        onChange={(e) => set('slots', form.slots.map((s, i) => (i === index ? { ...s, endTime: e.target.value } : s)))}
                      />
                    </label>
                    <label className="field" style={{ width: 120 }}>
                      <span>{t('services.capacity')}</span>
                      <input
                        type="number"
                        min={1}
                        value={slot.capacity}
                        onChange={(e) => set('slots', form.slots.map((s, i) => (i === index ? { ...s, capacity: Number(e.target.value) } : s)))}
                      />
                    </label>
                    <label className="field" style={{ width: 110 }}>
                      <span>{t('services.buffer')}</span>
                      <input
                        type="number"
                        min={0}
                        value={slot.bufferMinutes}
                        onChange={(e) => set('slots', form.slots.map((s, i) => (i === index ? { ...s, bufferMinutes: Number(e.target.value) } : s)))}
                      />
                    </label>
                    <button className="btn ghost sm" onClick={() => set('slots', form.slots.filter((_, i) => i !== index))}>
                      {t('services.removeSlot')}
                    </button>
                  </div>
                ))}
                <button
                  className="btn secondary sm"
                  onClick={() => set('slots', [...form.slots, { startTime: form.opensAt, endTime: form.closesAt, capacity: 40, bufferMinutes: 0 }])}
                >
                  + {t('services.addSlot')}
                </button>
              </div>
            )}
          </>
        )}

        {tab === 'menuTab' && (
          <div className="stack stack-4">
            {form.menu.length === 0 && <p className="small muted">{t('services.noMenu')}</p>}

            {form.menu.map((section, si) => (
              <div className="card card-pad-sm stack stack-3" key={si}>
                <div className="form-grid">
                  <label className="field">
                    <span>{t('services.sectionName')}</span>
                    <input
                      value={section.name}
                      onChange={(e) => set('menu', form.menu.map((s, i) => (i === si ? { ...s, name: e.target.value } : s)))}
                    />
                  </label>
                  <label className="field">
                    <span>{t('services.sectionNameAr')}</span>
                    <input
                      dir="rtl"
                      value={section.nameAr}
                      onChange={(e) => set('menu', form.menu.map((s, i) => (i === si ? { ...s, nameAr: e.target.value } : s)))}
                    />
                  </label>
                </div>

                {section.items.map((item, ii) => (
                  <div className="row wrap" key={ii} style={{ gap: 'var(--sp-2)', alignItems: 'flex-end' }}>
                    <label className="field grow" style={{ minWidth: 150 }}>
                      <span>{t('services.itemName')}</span>
                      <input
                        value={item.name}
                        onChange={(e) => set('menu', form.menu.map((s, i) => (i === si
                          ? { ...s, items: s.items.map((it, j) => (j === ii ? { ...it, name: e.target.value } : it)) }
                          : s)))}
                      />
                    </label>
                    <label className="field grow" style={{ minWidth: 150 }}>
                      <span>{t('services.itemNameAr')}</span>
                      <input
                        dir="rtl"
                        value={item.nameAr}
                        onChange={(e) => set('menu', form.menu.map((s, i) => (i === si
                          ? { ...s, items: s.items.map((it, j) => (j === ii ? { ...it, nameAr: e.target.value } : it)) }
                          : s)))}
                      />
                    </label>
                    <button
                      className="btn ghost sm"
                      onClick={() => set('menu', form.menu.map((s, i) => (i === si ? { ...s, items: s.items.filter((_, j) => j !== ii) } : s)))}
                    >
                      ×
                    </button>
                  </div>
                ))}

                <div className="row" style={{ gap: 'var(--sp-2)' }}>
                  <button
                    className="btn secondary sm"
                    onClick={() => set('menu', form.menu.map((s, i) => (i === si
                      ? { ...s, items: [...s.items, { name: '', nameAr: '', description: null, descriptionAr: null, dietary: [] }] }
                      : s)))}
                  >
                    + {t('services.addItem')}
                  </button>
                  <button className="btn ghost sm" onClick={() => set('menu', form.menu.filter((_, i) => i !== si))}>
                    {t('services.delete')}
                  </button>
                </div>
              </div>
            ))}

            <button
              className="btn secondary"
              onClick={() => set('menu', [...form.menu, { name: '', nameAr: '', items: [] }])}
            >
              + {t('services.addSection')}
            </button>
          </div>
        )}

        {tab === 'rules' && (
          <div className="form-grid">
            <label className="field">
              <span>{t('services.bookingMode')}</span>
              <select value={form.bookingMode} onChange={(e) => set('bookingMode', e.target.value as ServiceInput['bookingMode'])}>
                <option value="Instant">{t('bookingMode.Instant')}</option>
                <option value="Request">{t('bookingMode.Request')}</option>
              </select>
            </label>
            <label className="field">
              <span>{t('services.minAdvance')}</span>
              <input type="number" min={0} step={15} value={form.minAdvanceMinutes} onChange={(e) => set('minAdvanceMinutes', Number(e.target.value))} />
            </label>
            <label className="field">
              <span>{t('services.cancellationCutoff')}</span>
              <input
                type="number"
                min={0}
                step={30}
                value={form.cancellationCutoffMinutes ?? ''}
                onChange={(e) => set('cancellationCutoffMinutes', e.target.value === '' ? null : Number(e.target.value))}
              />
              <span className="hint">{t('services.cancellationCutoffHint')}</span>
            </label>
          </div>
        )}

        {tab === 'photos' && (
          <div className="stack stack-4">
            <PhotoUploader urls={form.photoUrls} onChange={(urls) => set('photoUrls', urls)} />
            <VideoUploader url={form.videoUrl} onChange={(url) => set('videoUrl', url)} />
          </div>
        )}
      </div>
    </div>
  )
}
