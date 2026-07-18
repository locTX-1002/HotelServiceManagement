import { useEffect, useRef, useState } from 'react'
import { EASE, errorCls, inputCls, labelCls } from '../utils/ui'
import client, { isBackendMissing, apiError } from '../api/client'
import ErrorState from '../components/ErrorState'
import SlideOver from '../components/SlideOver'
import ConfirmDialog from '../components/ConfirmDialog'
import { useToast } from '../components/toastContext'
import { MOCK_SURCHARGE_ITEMS } from '../mock/hotelMock'
import { formatVnd } from '../utils/roomStatus'

const EMPTY_FORM = { name: '', unit: 'cái', unitPrice: '' }

// Bảng giá phụ thu & đền bù - GET/POST/PUT /api/surcharge-items.
// Nguồn giá niêm yết cho mục "Phụ thu đồ dùng & đền bù" ở màn check-out.
// Backend không có DELETE: món ngừng dùng thì tắt isActive qua PUT (soft-off).
export default function SurchargeCatalogPage() {
  const toast = useToast()
  const [items, setItems] = useState(null)
  const [usingMock, setUsingMock] = useState(false)
  const [loadError, setLoadError] = useState(false)
  const [drawer, setDrawer] = useState(null) // { mode: 'create' } | { mode: 'edit', item }
  const [form, setForm] = useState(EMPTY_FORM)
  const [formError, setFormError] = useState('')
  const [saving, setSaving] = useState(false)
  const [togglingId, setTogglingId] = useState(null)
  const [toDeactivate, setToDeactivate] = useState(null)
  // Khoá đồng bộ chống spam-click - state saving/togglingId cập nhật bất đồng bộ nên vẫn lọt request khi bấm liên tiếp nhanh
  const savingRef = useRef(false)
  const togglingRef = useRef(false)

  const load = () => {
    setLoadError(false)
    client
      .get('/api/surcharge-items')
      .then((res) => { setItems(res.data); setUsingMock(false) })
      .catch((err) => {
        if (isBackendMissing(err)) { setItems(MOCK_SURCHARGE_ITEMS); setUsingMock(true) }
        else setLoadError(true) // lỗi thật: không che bằng mock
      })
  }
  useEffect(load, [])

  const openCreate = () => { setForm(EMPTY_FORM); setFormError(''); setDrawer({ mode: 'create' }) }
  const openEdit = (item) => {
    setForm({ name: item.name, unit: item.unit, unitPrice: item.unitPrice })
    setFormError('')
    setDrawer({ mode: 'edit', item })
  }

  const validate = () => {
    const name = form.name.trim()
    if (!name) return 'Nhập tên món.'
    const dup = (items ?? []).some(
      (i) => i.name.trim().toLowerCase() === name.toLowerCase() && i.id !== drawer?.item?.id,
    )
    if (dup) return 'Tên món này đã tồn tại.'
    if (!form.unit.trim()) return 'Nhập đơn vị (cái, đôi, bộ...).'
    const price = Number(form.unitPrice)
    if (!price || price <= 0) return 'Đơn giá phải lớn hơn 0.'
    return ''
  }

  const submit = (e) => {
    e.preventDefault()
    if (savingRef.current) return
    const msg = validate()
    if (msg) return setFormError(msg)

    savingRef.current = true
    setFormError('')
    setSaving(true)
    const payload = {
      name: form.name.trim(),
      unit: form.unit.trim(),
      unitPrice: Number(form.unitPrice),
      isActive: drawer?.item?.isActive ?? true,
    }
    const req =
      drawer.mode === 'edit'
        ? client.put(`/api/surcharge-items/${drawer.item.id}`, payload)
        : client.post('/api/surcharge-items', payload)
    req
      .then(() => {
        toast.success(drawer.mode === 'edit' ? `Đã lưu ${payload.name}` : `Đã thêm ${payload.name}`)
        setDrawer(null)
        load()
      })
      .catch((err) => setFormError(apiError(err)))
      .finally(() => { savingRef.current = false; setSaving(false) })
  }

  // Bật / tắt món: PUT nguyên món với isActive đảo lại
  const toggleActive = (item) => {
    if (togglingRef.current) return
    togglingRef.current = true
    setTogglingId(item.id)
    client
      .put(`/api/surcharge-items/${item.id}`, {
        name: item.name,
        unit: item.unit,
        unitPrice: item.unitPrice,
        isActive: !item.isActive,
      })
      .then(() => {
        toast.success(item.isActive ? `Đã ngừng dùng ${item.name}` : `Đã dùng lại ${item.name}`)
        load()
      })
      .catch((err) => toast.error(apiError(err)))
      .finally(() => { togglingRef.current = false; setTogglingId(null); setToDeactivate(null) })
  }

  // Ngừng dùng mới cần xác nhận (ẩn khỏi danh sách phụ thu lúc check-out) - dùng lại vô hại, không cần hỏi.
  const requestToggle = (item) => (item.isActive !== false ? setToDeactivate(item) : toggleActive(item))

  const editing = drawer?.mode === 'edit'
  const pricePreview = Number(form.unitPrice) > 0 ? formatVnd(Number(form.unitPrice)) : null

  return (
    <div>
      {/* Header */}
      <div className="flex flex-wrap items-end justify-between gap-4">
        <div>
          <p className="font-display text-[15px] italic capitalize text-brand-600">check-out · phụ thu</p>
          <h1 className="mt-1 font-display text-4xl font-semibold tracking-tight">Bảng giá phụ thu & đền bù</h1>
          <p className="mt-1 text-sm text-ink-500">Giá niêm yết đồ dùng thêm / hư hỏng / thất lạc — lễ tân tick khi trả phòng.</p>
        </div>
        <div className="flex items-center gap-2.5">
          {usingMock && (
            <span className="rounded-full bg-amber-50 px-2.5 py-1 text-[11px] font-bold text-amber-800 ring-1 ring-amber-600/20">
              Dữ liệu mẫu
            </span>
          )}
          <button
            onClick={openCreate}
            className={`rounded-full bg-ink-900 px-5 py-2.5 text-[13px] font-bold text-cream-50 ${EASE} hover:bg-ink-700 active:scale-[0.98]`}
          >
            + Thêm món
          </button>
        </div>
      </div>

      {/* Bảng món phụ thu */}
      {items === null && !loadError && (
        <div className="mt-6 space-y-3">
          {Array.from({ length: 5 }).map((_, i) => (
            <div key={i} className="h-16 animate-pulse rounded-2xl bg-cream-200" />
          ))}
        </div>
      )}

      {loadError && <div className="mt-6"><ErrorState onRetry={load} /></div>}

      {!loadError && items !== null && items.length > 0 && (
        <div className="card-rise mt-6 bezel-shell">
          <div className="bezel-core overflow-hidden">
            <div className="overflow-x-auto">
              <table className="w-full min-w-[560px] text-left">
                <thead>
                  <tr className="border-b border-black/[0.06] text-[10px] font-bold uppercase tracking-[0.18em] text-ink-500">
                    <th className="px-5 py-3.5">Món</th>
                    <th className="px-5 py-3.5">Đơn vị</th>
                    <th className="px-5 py-3.5">Đơn giá</th>
                    <th className="px-5 py-3.5">Trạng thái</th>
                    <th className="px-5 py-3.5 text-right">Thao tác</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-black/[0.05]">
                  {items.map((i) => (
                    <tr key={i.id} className={`${EASE} hover:bg-cream-50/60`}>
                      <td className="px-5 py-3.5">
                        <p className="font-display text-lg font-semibold tracking-tight">{i.name}</p>
                      </td>
                      <td className="px-5 py-3.5 text-sm text-ink-700">{i.unit}</td>
                      <td className="px-5 py-3.5">
                        <span className="text-sm font-semibold tabular-nums">{formatVnd(i.unitPrice)}</span>
                      </td>
                      <td className="px-5 py-3.5">
                        {i.isActive !== false ? (
                          <span className="rounded-full bg-emerald-50 px-2.5 py-1 text-[11px] font-semibold text-emerald-700 ring-1 ring-emerald-600/15">Đang dùng</span>
                        ) : (
                          <span className="rounded-full bg-stone-100 px-2.5 py-1 text-[11px] font-semibold text-stone-600 ring-1 ring-stone-500/15">Ngừng dùng</span>
                        )}
                      </td>
                      <td className="px-5 py-3.5 text-right">
                        <button
                          onClick={() => openEdit(i)}
                          className={`rounded-full px-3.5 py-1.5 text-[12px] font-semibold text-ink-700 ring-1 ring-black/10 ${EASE} hover:bg-cream-100`}
                        >
                          Sửa
                        </button>
                        <button
                          onClick={() => requestToggle(i)}
                          disabled={togglingId === i.id}
                          className={`ml-2 rounded-full px-3.5 py-1.5 text-[12px] font-semibold ring-1 ${EASE} disabled:opacity-40 ${
                            i.isActive !== false
                              ? 'text-rose-700 ring-rose-600/20 hover:bg-rose-50'
                              : 'text-emerald-700 ring-emerald-600/20 hover:bg-emerald-50'
                          }`}
                        >
                          {i.isActive !== false ? 'Ngừng dùng' : 'Dùng lại'}
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

      {!loadError && items !== null && items.length === 0 && (
        <div className="mt-6 flex flex-col items-center rounded-2xl border border-dashed border-black/10 bg-white/60 px-6 py-14">
          <span className="h-12 w-9 rounded-t-full rounded-b-md border-2 border-dashed border-brand-600/30" />
          <p className="mt-4 font-display text-lg italic text-ink-700">Chưa có món phụ thu nào</p>
          <button onClick={openCreate} className="mt-2 text-[12px] font-bold uppercase tracking-wider text-brand-600 hover:underline">
            Thêm món đầu tiên
          </button>
        </div>
      )}

      {/* Form thêm / sửa món */}
      <SlideOver
        open={drawer !== null}
        eyebrow={editing ? 'chỉnh sửa' : 'thêm mới'}
        title={editing ? `Sửa ${drawer.item.name}` : 'Thêm món phụ thu'}
        onClose={() => setDrawer(null)}
      >
        <form onSubmit={submit} className="space-y-5">
          <div>
            <label htmlFor="sur-name" className={labelCls}>Tên món *</label>
            <input
              id="sur-name"
              className={inputCls}
              placeholder="Khăn tắm"
              value={form.name}
              onChange={(e) => setForm({ ...form, name: e.target.value })}
            />
          </div>
          <div>
            <label htmlFor="sur-unit" className={labelCls}>Đơn vị *</label>
            <input
              id="sur-unit"
              className={inputCls}
              placeholder="cái"
              value={form.unit}
              onChange={(e) => setForm({ ...form, unit: e.target.value })}
            />
          </div>
          <div>
            <label htmlFor="sur-price" className={labelCls}>Đơn giá (VND) *</label>
            <input
              id="sur-price"
              type="number"
              min="0"
              step="5000"
              className={inputCls}
              placeholder="80000"
              value={form.unitPrice}
              onChange={(e) => setForm({ ...form, unitPrice: e.target.value })}
            />
            {pricePreview && <p className="mt-1.5 text-[12px] tabular-nums text-ink-500">= {pricePreview}</p>}
          </div>

          {formError && (
            <p className={errorCls}>{formError}</p>
          )}

          <button
            type="submit"
            disabled={saving}
            className={`w-full rounded-full bg-brand-600 py-3 text-[13px] font-bold text-white ${EASE} hover:bg-brand-700 active:scale-[0.98] disabled:opacity-40`}
          >
            {saving ? 'Đang lưu…' : editing ? 'Lưu thay đổi' : 'Thêm món'}
          </button>
        </form>
      </SlideOver>

      <ConfirmDialog
        open={toDeactivate !== null}
        title={`Ngừng dùng ${toDeactivate?.name ?? ''}?`}
        message="Món sẽ ẩn khỏi danh sách phụ thu lúc check-out. Bạn có thể dùng lại bất cứ lúc nào."
        confirmLabel="Ngừng dùng"
        busyLabel="Đang cập nhật…"
        busy={togglingId === toDeactivate?.id}
        onConfirm={() => toggleActive(toDeactivate)}
        onCancel={() => setToDeactivate(null)}
      />
    </div>
  )
}
