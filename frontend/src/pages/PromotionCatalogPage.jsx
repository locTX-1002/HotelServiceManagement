import { useEffect, useMemo, useState } from 'react'
import { EASE, errorCls, inputCls, labelCls, openDatePicker } from '../utils/ui'
import client, { isBackendMissing, apiError } from '../api/client'
import ErrorState from '../components/ErrorState'
import SlideOver from '../components/SlideOver'
import { useToast } from '../components/toastContext'
import { MOCK_PROMOTIONS } from '../mock/hotelMock'
import { formatVnd } from '../utils/roomStatus'
import { localToday } from '../utils/dates'

const EMPTY_FORM = { code: '', description: '', type: 'Percentage', value: '', startDate: localToday(), endDate: localToday(), isActive: true }

// Quản lý mã khuyến mãi - GET/POST/PUT /api/promotions. Backend không có DELETE:
// mã hết dùng thì tắt isActive qua PUT (soft-off), giống bảng giá dịch vụ.
export default function PromotionCatalogPage() {
  const toast = useToast()
  const [promotions, setPromotions] = useState(null)
  const [usingMock, setUsingMock] = useState(false)
  const [loadError, setLoadError] = useState(false)
  const [drawer, setDrawer] = useState(null) // { mode: 'create' } | { mode: 'edit', item }
  const [form, setForm] = useState(EMPTY_FORM)
  const [formError, setFormError] = useState('')
  const [saving, setSaving] = useState(false)
  const [togglingId, setTogglingId] = useState(null)

  const load = () => {
    setLoadError(false)
    client
      .get('/api/promotions')
      .then((res) => { setPromotions(res.data); setUsingMock(false) })
      .catch((err) => {
        if (isBackendMissing(err)) { setPromotions(MOCK_PROMOTIONS); setUsingMock(true) }
        else setLoadError(true) // lỗi thật: không che bằng mock
      })
  }
  useEffect(load, [])

  const openCreate = () => {
    setForm(EMPTY_FORM)
    setFormError('')
    setDrawer({ mode: 'create' })
  }
  const openEdit = (item) => {
    setForm({
      code: item.code,
      description: item.description ?? '',
      type: item.type,
      value: item.value,
      startDate: String(item.startDate).slice(0, 10),
      endDate: String(item.endDate).slice(0, 10),
      isActive: item.isActive,
    })
    setFormError('')
    setDrawer({ mode: 'edit', item })
  }

  const validate = () => {
    const code = form.code.trim()
    if (!code) return 'Nhập mã khuyến mãi.'
    const dup = (promotions ?? []).some(
      (p) => p.code.trim().toUpperCase() === code.toUpperCase() && p.id !== drawer?.item?.id,
    )
    if (dup) return 'Mã khuyến mãi này đã tồn tại.'
    const value = Number(form.value)
    if (!value || value <= 0) return 'Giá trị phải lớn hơn 0.'
    if (form.type === 'Percentage' && value > 100) return 'Giá trị phần trăm không được vượt quá 100.'
    if (!form.startDate || !form.endDate) return 'Chọn đủ ngày bắt đầu và kết thúc.'
    if (form.endDate < form.startDate) return 'Ngày kết thúc phải sau ngày bắt đầu.'
    return ''
  }

  const submit = (e) => {
    e.preventDefault()
    const msg = validate()
    if (msg) return setFormError(msg)

    setFormError('')
    setSaving(true)
    const payload = {
      code: form.code.trim(),
      description: form.description.trim() || undefined,
      type: form.type,
      value: Number(form.value),
      startDate: form.startDate,
      endDate: form.endDate,
      isActive: drawer?.item?.isActive ?? true,
    }
    const req =
      drawer.mode === 'edit'
        ? client.put(`/api/promotions/${drawer.item.id}`, payload)
        : client.post('/api/promotions', payload)
    req
      .then(() => {
        toast.success(drawer.mode === 'edit' ? `Đã lưu mã ${payload.code}` : `Đã thêm mã ${payload.code}`)
        setDrawer(null)
        load()
      })
      .catch((err) => setFormError(apiError(err)))
      .finally(() => setSaving(false))
  }

  // Bật / tắt mã: PUT nguyên bản ghi với isActive đảo lại
  const toggleActive = (item) => {
    setTogglingId(item.id)
    client
      .put(`/api/promotions/${item.id}`, {
        code: item.code,
        description: item.description,
        type: item.type,
        value: item.value,
        startDate: item.startDate,
        endDate: item.endDate,
        isActive: !item.isActive,
      })
      .then(() => {
        toast.success(item.isActive ? `Đã tắt mã ${item.code}` : `Đã bật lại mã ${item.code}`)
        load()
      })
      .catch((err) => toast.error(apiError(err)))
      .finally(() => setTogglingId(null))
  }

  const editing = drawer?.mode === 'edit'
  const valuePreview = useMemo(() => {
    const v = Number(form.value)
    if (!v || v <= 0) return null
    return form.type === 'Percentage' ? `${v}%` : formatVnd(v)
  }, [form.value, form.type])

  return (
    <div>
      {/* Header */}
      <div className="flex flex-wrap items-end justify-between gap-4">
        <div>
          <p className="font-display text-[15px] italic capitalize text-brand-600">quản lý · khuyến mãi</p>
          <h1 className="mt-1 font-display text-4xl font-semibold tracking-tight">Khuyến mãi</h1>
          <p className="mt-1 text-sm text-ink-500">Mã giảm giá áp dụng lúc tạo hoá đơn — theo % hoặc số tiền cố định.</p>
        </div>
        <button
          onClick={openCreate}
          className={`rounded-full bg-ink-900 px-5 py-2.5 text-[13px] font-bold text-cream-50 ${EASE} hover:bg-ink-700 active:scale-[0.98]`}
        >
          + Thêm mã
        </button>
      </div>

      {usingMock && (
        <span className="mt-4 inline-block rounded-full bg-amber-50 px-2.5 py-1 text-[11px] font-bold text-amber-800 ring-1 ring-amber-600/20">
          Dữ liệu mẫu
        </span>
      )}

      {/* Bảng mã khuyến mãi */}
      {promotions === null && !loadError && (
        <div className="mt-6 space-y-3">
          {Array.from({ length: 4 }).map((_, i) => (
            <div key={i} className="h-16 animate-pulse rounded-2xl bg-cream-200" />
          ))}
        </div>
      )}

      {loadError && <div className="mt-6"><ErrorState onRetry={load} /></div>}

      {!loadError && promotions !== null && promotions.length > 0 && (
        <div className="card-rise mt-6 bezel-shell">
          <div className="bezel-core overflow-hidden">
            <div className="overflow-x-auto">
              <table className="w-full min-w-[680px] text-left">
                <thead>
                  <tr className="border-b border-black/[0.06] text-[10px] font-bold uppercase tracking-[0.18em] text-ink-500">
                    <th className="px-5 py-3.5">Mã</th>
                    <th className="px-5 py-3.5">Giá trị</th>
                    <th className="px-5 py-3.5">Thời hạn</th>
                    <th className="px-5 py-3.5">Trạng thái</th>
                    <th className="px-5 py-3.5 text-right">Thao tác</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-black/[0.05]">
                  {promotions.map((p) => (
                    <tr key={p.id} className={`${EASE} hover:bg-cream-50/60`}>
                      <td className="px-5 py-3.5">
                        <p className="font-display text-lg font-semibold tracking-tight">{p.code}</p>
                        {p.description && <p className="text-[11px] text-ink-500">{p.description}</p>}
                      </td>
                      <td className="px-5 py-3.5">
                        <span className="text-sm font-semibold tabular-nums">
                          {p.type === 'Percentage' ? `${p.value}%` : formatVnd(p.value)}
                        </span>
                      </td>
                      <td className="px-5 py-3.5 text-sm tabular-nums text-ink-700">
                        {String(p.startDate).slice(0, 10)} → {String(p.endDate).slice(0, 10)}
                      </td>
                      <td className="px-5 py-3.5">
                        {p.isActive ? (
                          <span className="rounded-full bg-emerald-50 px-2.5 py-1 text-[11px] font-semibold text-emerald-700 ring-1 ring-emerald-600/15">Đang bật</span>
                        ) : (
                          <span className="rounded-full bg-stone-100 px-2.5 py-1 text-[11px] font-semibold text-stone-600 ring-1 ring-stone-500/15">Đã tắt</span>
                        )}
                      </td>
                      <td className="px-5 py-3.5 text-right">
                        <button
                          onClick={() => openEdit(p)}
                          className={`rounded-full px-3.5 py-1.5 text-[12px] font-semibold text-ink-700 ring-1 ring-black/10 ${EASE} hover:bg-cream-100`}
                        >
                          Sửa
                        </button>
                        <button
                          onClick={() => toggleActive(p)}
                          disabled={togglingId === p.id}
                          className={`ml-2 rounded-full px-3.5 py-1.5 text-[12px] font-semibold ring-1 ${EASE} disabled:opacity-40 ${
                            p.isActive
                              ? 'text-rose-700 ring-rose-600/20 hover:bg-rose-50'
                              : 'text-emerald-700 ring-emerald-600/20 hover:bg-emerald-50'
                          }`}
                        >
                          {p.isActive ? 'Tắt' : 'Bật'}
                        </button>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </div>
        </div>
      )}

      {!loadError && promotions !== null && promotions.length === 0 && (
        <div className="mt-6 flex flex-col items-center rounded-2xl border border-dashed border-black/10 bg-white/60 px-6 py-14">
          <span className="h-12 w-9 rounded-t-full rounded-b-md border-2 border-dashed border-brand-600/30" />
          <p className="mt-4 font-display text-lg italic text-ink-700">Chưa có mã khuyến mãi nào</p>
          <button onClick={openCreate} className="mt-2 text-[12px] font-bold uppercase tracking-wider text-brand-600 hover:underline">
            Thêm mã khuyến mãi
          </button>
        </div>
      )}

      {/* Form thêm / sửa mã */}
      <SlideOver
        open={drawer !== null}
        eyebrow={editing ? 'chỉnh sửa' : 'thêm mới'}
        title={editing ? `Sửa mã ${drawer.item.code}` : 'Thêm mã khuyến mãi'}
        onClose={() => setDrawer(null)}
      >
        <form onSubmit={submit} className="space-y-5">
          <div>
            <label htmlFor="promo-code" className={labelCls}>Mã *</label>
            <input
              id="promo-code"
              className={inputCls}
              placeholder="SUMMER10"
              value={form.code}
              onChange={(e) => setForm({ ...form, code: e.target.value.toUpperCase() })}
            />
          </div>
          <div>
            <label htmlFor="promo-desc" className={labelCls}>Mô tả (tuỳ chọn)</label>
            <input
              id="promo-desc"
              className={inputCls}
              placeholder="Giảm giá hè 2026"
              value={form.description}
              onChange={(e) => setForm({ ...form, description: e.target.value })}
            />
          </div>
          <div className="grid grid-cols-2 gap-3">
            <div>
              <label htmlFor="promo-type" className={labelCls}>Loại *</label>
              <select
                id="promo-type"
                className={inputCls}
                value={form.type}
                onChange={(e) => setForm({ ...form, type: e.target.value })}
              >
                <option value="Percentage">Phần trăm (%)</option>
                <option value="FixedAmount">Số tiền cố định</option>
              </select>
            </div>
            <div>
              <label htmlFor="promo-value" className={labelCls}>Giá trị *</label>
              <input
                id="promo-value"
                type="number"
                min="0"
                className={inputCls}
                placeholder={form.type === 'Percentage' ? '10' : '100000'}
                value={form.value}
                onChange={(e) => setForm({ ...form, value: e.target.value })}
              />
              {valuePreview && <p className="mt-1.5 text-[12px] tabular-nums text-ink-500">= {valuePreview}</p>}
            </div>
          </div>
          <div className="grid grid-cols-2 gap-3">
            <div>
              <label htmlFor="promo-start" className={labelCls}>Bắt đầu *</label>
              <input
                id="promo-start"
                type="date"
                className={inputCls}
                value={form.startDate}
                onClick={openDatePicker}
                onChange={(e) => setForm({ ...form, startDate: e.target.value })}
              />
            </div>
            <div>
              <label htmlFor="promo-end" className={labelCls}>Kết thúc *</label>
              <input
                id="promo-end"
                type="date"
                className={inputCls}
                value={form.endDate}
                min={form.startDate}
                onClick={openDatePicker}
                onChange={(e) => setForm({ ...form, endDate: e.target.value })}
              />
            </div>
          </div>

          {formError && (
            <p className={errorCls}>{formError}</p>
          )}

          <button
            type="submit"
            disabled={saving}
            className={`w-full rounded-full bg-brand-600 py-3 text-[13px] font-bold text-white ${EASE} hover:bg-brand-700 active:scale-[0.98] disabled:opacity-40`}
          >
            {saving ? 'Đang lưu…' : editing ? 'Lưu thay đổi' : 'Thêm mã'}
          </button>
        </form>
      </SlideOver>
    </div>
  )
}
