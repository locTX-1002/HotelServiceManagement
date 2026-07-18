import { useEffect, useMemo, useRef, useState } from 'react'
import { EASE, errorCls, inputCls, labelCls } from '../utils/ui'
import client, { isBackendMissing, apiError } from '../api/client'
import ErrorState from '../components/ErrorState'
import SlideOver from '../components/SlideOver'
import ConfirmDialog from '../components/ConfirmDialog'
import { useToast } from '../components/toastContext'
import { MOCK_SERVICE_CATEGORIES, MOCK_SERVICE_ITEMS } from '../mock/hotelMock'
import { formatVnd } from '../utils/roomStatus'

const EMPTY_FORM = { serviceCategoryId: '', serviceName: '', unitPrice: '' }

// Bảng giá dịch vụ - GET /api/service-categories + GET/POST/PUT /api/service-items.
// Backend không có DELETE: món ngừng bán thì tắt isAvailable qua PUT (soft-off).
export default function ServiceCatalogPage() {
  const toast = useToast()
  const [categories, setCategories] = useState([])
  const [items, setItems] = useState(null)
  const [usingMock, setUsingMock] = useState(false)
  const [loadError, setLoadError] = useState(false)
  const [catFilter, setCatFilter] = useState('all')
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
      .get('/api/service-items')
      .then((res) => { setItems(res.data); setUsingMock(false) })
      .catch((err) => {
        if (isBackendMissing(err)) { setItems(MOCK_SERVICE_ITEMS); setUsingMock(true) }
        else setLoadError(true) // lỗi thật: không che bằng mock
      })
    // Danh mục chỉ phục vụ ô chọn + chips lọc; chỉ dùng mock khi backend chưa có
    client
      .get('/api/service-categories')
      .then((res) => setCategories(res.data))
      .catch((err) => { if (isBackendMissing(err)) setCategories(MOCK_SERVICE_CATEGORIES) })
  }
  useEffect(load, [])

  const visible = useMemo(
    () => (items ?? []).filter((i) => catFilter === 'all' || i.serviceCategoryId === catFilter),
    [items, catFilter],
  )

  const chips = [
    { key: 'all', label: 'Tất cả', n: (items ?? []).length },
    ...categories
      .filter((c) => c.isActive !== false)
      .map((c) => ({ key: c.id, label: c.categoryName, n: (items ?? []).filter((i) => i.serviceCategoryId === c.id).length })),
  ]

  const openCreate = () => {
    const firstActive = categories.find((c) => c.isActive !== false)
    setForm({ ...EMPTY_FORM, serviceCategoryId: firstActive?.id ?? '' })
    setFormError('')
    setDrawer({ mode: 'create' })
  }
  const openEdit = (item) => {
    setForm({ serviceCategoryId: item.serviceCategoryId, serviceName: item.serviceName, unitPrice: item.unitPrice })
    setFormError('')
    setDrawer({ mode: 'edit', item })
  }

  const validate = () => {
    const name = form.serviceName.trim()
    if (!name) return 'Nhập tên dịch vụ.'
    const dup = (items ?? []).some(
      (i) => i.serviceName.trim().toLowerCase() === name.toLowerCase() && i.id !== drawer?.item?.id,
    )
    if (dup) return 'Tên dịch vụ này đã tồn tại.'
    if (!form.serviceCategoryId) return 'Chọn danh mục.'
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
      serviceCategoryId: Number(form.serviceCategoryId),
      serviceName: form.serviceName.trim(),
      unitPrice: Number(form.unitPrice),
      isAvailable: drawer?.item?.isAvailable ?? true,
    }
    const req =
      drawer.mode === 'edit'
        ? client.put(`/api/service-items/${drawer.item.id}`, payload)
        : client.post('/api/service-items', payload)
    req
      .then(() => {
        toast.success(drawer.mode === 'edit' ? `Đã lưu ${payload.serviceName}` : `Đã thêm dịch vụ ${payload.serviceName}`)
        setDrawer(null)
        load()
      })
      .catch((err) => setFormError(apiError(err)))
      .finally(() => { savingRef.current = false; setSaving(false) })
  }

  // Bật / tắt bán món: PUT nguyên món với isAvailable đảo lại
  const toggleAvailable = (item) => {
    if (togglingRef.current) return
    togglingRef.current = true
    setTogglingId(item.id)
    client
      .put(`/api/service-items/${item.id}`, {
        serviceCategoryId: item.serviceCategoryId,
        serviceName: item.serviceName,
        unitPrice: item.unitPrice,
        isAvailable: !item.isAvailable,
      })
      .then(() => {
        toast.success(item.isAvailable ? `Đã ngừng bán ${item.serviceName}` : `Đã mở bán lại ${item.serviceName}`)
        load()
      })
      .catch((err) => toast.error(apiError(err)))
      .finally(() => { togglingRef.current = false; setTogglingId(null); setToDeactivate(null) })
  }

  // Ngừng bán mới cần xác nhận (ẩn món khỏi order đang diễn ra) - mở bán lại vô hại, không cần hỏi.
  const requestToggle = (item) => (item.isAvailable !== false ? setToDeactivate(item) : toggleAvailable(item))

  const editing = drawer?.mode === 'edit'
  const pricePreview = Number(form.unitPrice) > 0 ? formatVnd(Number(form.unitPrice)) : null

  return (
    <div>
      {/* Header */}
      <div className="flex flex-wrap items-end justify-between gap-4">
        <div>
          <p className="font-display text-[15px] italic capitalize text-brand-600">dịch vụ · bảng giá</p>
          <h1 className="mt-1 font-display text-4xl font-semibold tracking-tight">Bảng giá dịch vụ</h1>
          <p className="mt-1 text-sm text-ink-500">Món và đơn giá theo danh mục — nguồn dữ liệu cho màn hình gọi dịch vụ.</p>
        </div>
        <button
          onClick={openCreate}
          className={`rounded-full bg-ink-900 px-5 py-2.5 text-[13px] font-bold text-cream-50 ${EASE} hover:bg-ink-700 active:scale-[0.98]`}
        >
          + Thêm dịch vụ
        </button>
      </div>

      {/* Chips lọc theo danh mục */}
      <div className="mt-5 flex flex-wrap items-center gap-3">
        <div className="inline-flex flex-wrap items-center gap-0.5 rounded-full bg-white p-1 ring-1 ring-black/10 shadow-soft">
          {chips.map((c) => {
            const active = catFilter === c.key
            return (
              <button
                key={c.key}
                onClick={() => setCatFilter(c.key)}
                className={`flex items-center gap-1.5 rounded-full px-3.5 py-1.5 text-[12px] font-semibold ${EASE} active:scale-[0.97] ${
                  active ? 'bg-ink-900 text-cream-50' : 'text-ink-500 hover:text-ink-900'
                }`}
              >
                {c.label}
                <span className={`tabular-nums font-medium ${active ? 'text-cream-50/60' : 'text-ink-500/50'}`}>{c.n}</span>
              </button>
            )
          })}
        </div>
        {usingMock && (
          <span className="ml-auto rounded-full bg-amber-50 px-2.5 py-1 text-[11px] font-bold text-amber-800 ring-1 ring-amber-600/20">
            Dữ liệu mẫu
          </span>
        )}
      </div>

      {/* Bảng món dịch vụ */}
      {items === null && !loadError && (
        <div className="mt-6 space-y-3">
          {Array.from({ length: 5 }).map((_, i) => (
            <div key={i} className="h-16 animate-pulse rounded-2xl bg-cream-200" />
          ))}
        </div>
      )}

      {loadError && <div className="mt-6"><ErrorState onRetry={load} /></div>}

      {!loadError && items !== null && visible.length > 0 && (
        <div className="card-rise mt-6 bezel-shell">
          <div className="bezel-core overflow-hidden">
            <div className="overflow-x-auto">
              <table className="w-full min-w-[620px] text-left">
                <thead>
                  <tr className="border-b border-black/[0.06] text-[10px] font-bold uppercase tracking-[0.18em] text-ink-500">
                    <th className="px-5 py-3.5">Dịch vụ</th>
                    <th className="px-5 py-3.5">Danh mục</th>
                    <th className="px-5 py-3.5">Đơn giá</th>
                    <th className="px-5 py-3.5">Trạng thái</th>
                    <th className="px-5 py-3.5 text-right">Thao tác</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-black/[0.05]">
                  {visible.map((i) => (
                    <tr key={i.id} className={`${EASE} hover:bg-cream-50/60`}>
                      <td className="px-5 py-3.5">
                        <p className="font-display text-lg font-semibold tracking-tight">{i.serviceName}</p>
                      </td>
                      <td className="px-5 py-3.5 text-sm text-ink-700">{i.categoryName}</td>
                      <td className="px-5 py-3.5">
                        <span className="text-sm font-semibold tabular-nums">{formatVnd(i.unitPrice)}</span>
                      </td>
                      <td className="px-5 py-3.5">
                        {i.isAvailable !== false ? (
                          <span className="rounded-full bg-emerald-50 px-2.5 py-1 text-[11px] font-semibold text-emerald-700 ring-1 ring-emerald-600/15">Đang bán</span>
                        ) : (
                          <span className="rounded-full bg-stone-100 px-2.5 py-1 text-[11px] font-semibold text-stone-600 ring-1 ring-stone-500/15">Ngừng bán</span>
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
                            i.isAvailable !== false
                              ? 'text-rose-700 ring-rose-600/20 hover:bg-rose-50'
                              : 'text-emerald-700 ring-emerald-600/20 hover:bg-emerald-50'
                          }`}
                        >
                          {i.isAvailable !== false ? 'Ngừng bán' : 'Mở bán'}
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

      {!loadError && items !== null && visible.length === 0 && (
        <div className="mt-6 flex flex-col items-center rounded-2xl border border-dashed border-black/10 bg-white/60 px-6 py-14">
          <span className="h-12 w-9 rounded-t-full rounded-b-md border-2 border-dashed border-brand-600/30" />
          <p className="mt-4 font-display text-lg italic text-ink-700">
            {items.length === 0 ? 'Chưa có dịch vụ nào' : 'Danh mục này chưa có món'}
          </p>
          <button onClick={openCreate} className="mt-2 text-[12px] font-bold uppercase tracking-wider text-brand-600 hover:underline">
            Thêm dịch vụ
          </button>
        </div>
      )}

      {/* Form thêm / sửa món */}
      <SlideOver
        open={drawer !== null}
        eyebrow={editing ? 'chỉnh sửa' : 'thêm mới'}
        title={editing ? `Sửa ${drawer.item.serviceName}` : 'Thêm dịch vụ'}
        onClose={() => setDrawer(null)}
      >
        <form onSubmit={submit} className="space-y-5">
          <div>
            <label htmlFor="svc-name" className={labelCls}>Tên dịch vụ *</label>
            <input
              id="svc-name"
              className={inputCls}
              placeholder="Bữa sáng buffet"
              value={form.serviceName}
              onChange={(e) => setForm({ ...form, serviceName: e.target.value })}
            />
          </div>
          <div>
            <label htmlFor="svc-category" className={labelCls}>Danh mục *</label>
            <select
              id="svc-category"
              className={inputCls}
              value={form.serviceCategoryId}
              onChange={(e) => setForm({ ...form, serviceCategoryId: e.target.value })}
            >
              <option value="">— Chọn danh mục —</option>
              {/* Giữ lại danh mục hiện tại của món dù đã tắt - không thì form Sửa hiện select trống */}
              {categories.filter((c) => c.isActive !== false || (editing && c.id === drawer.item.serviceCategoryId)).map((c) => (
                <option key={c.id} value={c.id}>{c.categoryName}{c.isActive === false ? ' (đã tắt)' : ''}</option>
              ))}
            </select>
          </div>
          <div>
            <label htmlFor="svc-price" className={labelCls}>Đơn giá (VND) *</label>
            <input
              id="svc-price"
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
            {saving ? 'Đang lưu…' : editing ? 'Lưu thay đổi' : 'Thêm dịch vụ'}
          </button>
        </form>
      </SlideOver>

      <ConfirmDialog
        open={toDeactivate !== null}
        title={`Ngừng bán ${toDeactivate?.serviceName ?? ''}?`}
        message="Món sẽ ẩn khỏi danh sách gọi dịch vụ. Bạn có thể mở bán lại bất cứ lúc nào."
        confirmLabel="Ngừng bán"
        busyLabel="Đang cập nhật…"
        busy={togglingId === toDeactivate?.id}
        onConfirm={() => toggleAvailable(toDeactivate)}
        onCancel={() => setToDeactivate(null)}
      />
    </div>
  )
}
