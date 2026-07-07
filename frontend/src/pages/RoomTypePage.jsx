import { useEffect, useState } from 'react'
import client, { isBackendMissing } from '../api/client'
import ConfirmDialog from '../components/ConfirmDialog'
import ErrorState from '../components/ErrorState'
import RoomsTabs from '../components/RoomsTabs'
import SlideOver from '../components/SlideOver'
import { useToast } from '../components/toastContext'
import { MOCK_ROOM_TYPES_FULL } from '../mock/hotelMock'
import { roomImage } from '../utils/roomImages'
import { formatVnd } from '../utils/roomStatus'

const EASE = 'transition-all duration-300 ease-[cubic-bezier(0.32,0.72,0,1)]'
const inputCls =
  'w-full rounded-xl bg-white px-3.5 py-2.5 text-sm ring-1 ring-black/10 outline-none placeholder:text-ink-500/50 focus:ring-2 focus:ring-brand-500/40'
const labelCls = 'mb-1.5 block text-[12px] font-semibold text-ink-700'

const EMPTY_FORM = { typeName: '', capacity: 2, basePrice: '' }

// Lỗi mutation: backend chưa có endpoint thì nói thẳng đang chờ task nào,
// còn lỗi thật thì hiện message của máy chủ - không che bằng mock.
const apiError = (err) =>
  isBackendMissing(err)
    ? 'API /api/room-types chưa sẵn sàng (T2 - Khoa). Form đã hoạt động, backend nối xong là chạy.'
    : err.response?.data?.message ?? 'Máy chủ báo lỗi. Thử lại sau ít phút.'

export default function RoomTypePage() {
  const toast = useToast()
  const [types, setTypes] = useState(null)
  const [usingMock, setUsingMock] = useState(false)
  const [loadError, setLoadError] = useState(false)
  const [drawer, setDrawer] = useState(null) // { mode: 'create' } | { mode: 'edit', item }
  const [form, setForm] = useState(EMPTY_FORM)
  const [formError, setFormError] = useState('')
  const [saving, setSaving] = useState(false)
  const [toDelete, setToDelete] = useState(null)
  const [deleting, setDeleting] = useState(false)
  const [deleteError, setDeleteError] = useState('')

  const load = () => {
    setLoadError(false)
    client
      .get('/api/room-types')
      .then((res) => { setTypes(res.data); setUsingMock(false) })
      .catch((err) => {
        if (isBackendMissing(err)) { setTypes(MOCK_ROOM_TYPES_FULL); setUsingMock(true) }
        else setLoadError(true) // lỗi thật: không che bằng mock
      })
  }
  useEffect(load, [])

  const openCreate = () => { setForm(EMPTY_FORM); setFormError(''); setDrawer({ mode: 'create' }) }
  const openEdit = (item) => {
    setForm({ typeName: item.typeName, capacity: item.capacity, basePrice: item.basePrice })
    setFormError('')
    setDrawer({ mode: 'edit', item })
  }

  const validate = () => {
    const name = form.typeName.trim()
    if (!name) return 'Nhập tên loại phòng.'
    const dup = (types ?? []).some(
      (t) => t.typeName.trim().toLowerCase() === name.toLowerCase() && t.roomTypeId !== drawer?.item?.roomTypeId,
    )
    if (dup) return 'Tên loại phòng này đã tồn tại.'
    const capacity = Number(form.capacity)
    if (!Number.isInteger(capacity) || capacity < 1 || capacity > 20) return 'Sức chứa phải là số nguyên từ 1 đến 20.'
    const price = Number(form.basePrice)
    if (!price || price <= 0) return 'Giá mỗi đêm phải lớn hơn 0.'
    return ''
  }

  const submit = (e) => {
    e.preventDefault()
    const msg = validate()
    if (msg) return setFormError(msg)

    setFormError('')
    setSaving(true)
    const payload = {
      typeName: form.typeName.trim(),
      capacity: Number(form.capacity),
      basePrice: Number(form.basePrice),
      isActive: drawer?.item?.isActive ?? true,
    }
    const req =
      drawer.mode === 'edit'
        ? client.put(`/api/room-types/${drawer.item.roomTypeId}`, payload)
        : client.post('/api/room-types', payload)
    req
      .then(() => {
        toast.success(drawer.mode === 'edit' ? `Đã lưu ${payload.typeName}` : `Đã thêm loại phòng ${payload.typeName}`)
        setDrawer(null)
        load()
      })
      .catch((err) => setFormError(apiError(err)))
      .finally(() => setSaving(false))
  }

  const confirmDelete = () => {
    setDeleteError('')
    setDeleting(true)
    client
      .delete(`/api/room-types/${toDelete.roomTypeId}`)
      .then(() => {
        toast.success(`Đã xóa loại phòng ${toDelete.typeName}`)
        setToDelete(null)
        load()
      })
      .catch((err) => setDeleteError(apiError(err)))
      .finally(() => setDeleting(false))
  }

  const editing = drawer?.mode === 'edit'
  const pricePreview = Number(form.basePrice) > 0 ? formatVnd(Number(form.basePrice)) : null

  return (
    <div>
      {/* Header: tiêu đề trái, tabs + nút thêm phải */}
      <div className="flex flex-wrap items-end justify-between gap-4">
        <div>
          <p className="font-display text-[15px] italic capitalize text-brand-600">quản lý khách sạn</p>
          <h1 className="mt-1 font-display text-4xl font-semibold tracking-tight">Loại phòng</h1>
          <p className="mt-1 text-sm text-ink-500">Sức chứa và giá mỗi đêm của từng hạng phòng — nguồn dữ liệu cho sơ đồ phòng và đặt phòng.</p>
        </div>
        <div className="flex flex-wrap items-center gap-2.5">
          <RoomsTabs />
          <button
            onClick={openCreate}
            className={`rounded-full bg-ink-900 px-5 py-2.5 text-[13px] font-bold text-cream-50 ${EASE} hover:bg-ink-700 active:scale-[0.98]`}
          >
            + Thêm loại phòng
          </button>
        </div>
      </div>

      {usingMock && (
        <span className="mt-4 inline-block rounded-full bg-amber-50 px-2.5 py-1 text-[11px] font-bold text-amber-800 ring-1 ring-amber-600/20">
          Dữ liệu mẫu — chờ API
        </span>
      )}

      {/* Bảng loại phòng */}
      {types === null && !loadError && (
        <div className="mt-6 space-y-3">
          {Array.from({ length: 4 }).map((_, i) => (
            <div key={i} className="h-16 animate-pulse rounded-2xl bg-cream-200" />
          ))}
        </div>
      )}

      {loadError && (
        <div className="mt-6"><ErrorState onRetry={load} /></div>
      )}

      {!loadError && types !== null && types.length > 0 && (
        <div className="card-rise mt-6 overflow-hidden rounded-2xl bg-white ring-1 ring-black/5 shadow-soft">
          <div className="overflow-x-auto">
          <table className="w-full min-w-[560px] text-left">
            <thead>
              <tr className="border-b border-black/[0.06] text-[10px] font-bold uppercase tracking-[0.18em] text-ink-500">
                <th className="px-5 py-3.5">Loại phòng</th>
                <th className="px-5 py-3.5">Sức chứa</th>
                <th className="px-5 py-3.5">Giá mỗi đêm</th>
                <th className="px-5 py-3.5">Trạng thái</th>
                <th className="px-5 py-3.5 text-right">Thao tác</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-black/[0.05]">
              {types.map((t) => (
                <tr key={t.roomTypeId} className={`${EASE} hover:bg-cream-50/60`}>
                  <td className="px-5 py-3.5">
                    <div className="flex items-center gap-3.5">
                      {/* Ảnh loại phòng trong ô cửa vòm mini */}
                      <img
                        src={roomImage(t.typeName, 0)}
                        alt={t.typeName}
                        loading="lazy"
                        className="h-12 w-9 shrink-0 rounded-t-full rounded-b-md object-cover"
                      />
                      <p className="font-display text-lg font-semibold tracking-tight">{t.typeName}</p>
                    </div>
                  </td>
                  <td className="px-5 py-3.5 text-sm tabular-nums text-ink-700">{t.capacity} khách</td>
                  <td className="px-5 py-3.5">
                    <span className="text-sm font-semibold tabular-nums">{formatVnd(t.basePrice)}</span>
                    <span className="text-[11px] text-ink-500"> /đêm</span>
                  </td>
                  <td className="px-5 py-3.5">
                    {t.isActive !== false ? (
                      <span className="rounded-full bg-emerald-50 px-2.5 py-1 text-[11px] font-semibold text-emerald-700 ring-1 ring-emerald-600/15">Đang dùng</span>
                    ) : (
                      <span className="rounded-full bg-stone-100 px-2.5 py-1 text-[11px] font-semibold text-stone-600 ring-1 ring-stone-500/15">Ngừng dùng</span>
                    )}
                  </td>
                  <td className="px-5 py-3.5 text-right">
                    <button
                      onClick={() => openEdit(t)}
                      className={`rounded-full px-3.5 py-1.5 text-[12px] font-semibold text-ink-700 ring-1 ring-black/10 ${EASE} hover:bg-cream-100`}
                    >
                      Sửa
                    </button>
                    <button
                      onClick={() => { setDeleteError(''); setToDelete(t) }}
                      className={`ml-2 rounded-full px-3.5 py-1.5 text-[12px] font-semibold text-rose-700 ring-1 ring-rose-600/20 ${EASE} hover:bg-rose-50`}
                    >
                      Xóa
                    </button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
          </div>
        </div>
      )}

      {!loadError && types !== null && types.length === 0 && (
        <div className="mt-6 flex flex-col items-center rounded-2xl border border-dashed border-black/10 bg-white/60 px-6 py-14">
          <span className="h-12 w-9 rounded-t-full rounded-b-md border-2 border-dashed border-brand-600/30" />
          <p className="mt-4 font-display text-lg italic text-ink-700">Chưa có loại phòng nào</p>
          <button onClick={openCreate} className="mt-2 text-[12px] font-bold uppercase tracking-wider text-brand-600 hover:underline">
            Thêm loại phòng đầu tiên
          </button>
        </div>
      )}

      {/* Form thêm / sửa */}
      <SlideOver
        open={drawer !== null}
        eyebrow={editing ? 'chỉnh sửa' : 'thêm mới'}
        title={editing ? `Sửa ${drawer.item.typeName}` : 'Thêm loại phòng'}
        onClose={() => setDrawer(null)}
      >
        <form onSubmit={submit} className="space-y-5">
          <div>
            <label htmlFor="rt-name" className={labelCls}>Tên loại phòng *</label>
            <input
              id="rt-name"
              className={inputCls}
              placeholder="Deluxe"
              value={form.typeName}
              onChange={(e) => setForm({ ...form, typeName: e.target.value })}
            />
          </div>
          <div>
            <label className={labelCls}>Sức chứa (khách) *</label>
            <div className="inline-flex items-center gap-1 rounded-xl bg-white ring-1 ring-black/10">
              <button
                type="button"
                onClick={() => setForm({ ...form, capacity: Math.max(1, Number(form.capacity) - 1) })}
                className="px-3.5 py-2.5 text-sm font-bold text-ink-500 hover:text-ink-900"
              >
                −
              </button>
              <span className="w-8 text-center text-sm font-bold tabular-nums">{form.capacity}</span>
              <button
                type="button"
                onClick={() => setForm({ ...form, capacity: Math.min(20, Number(form.capacity) + 1) })}
                className="px-3.5 py-2.5 text-sm font-bold text-ink-500 hover:text-ink-900"
              >
                +
              </button>
            </div>
          </div>
          <div>
            <label htmlFor="rt-price" className={labelCls}>Giá mỗi đêm (VND) *</label>
            <input
              id="rt-price"
              type="number"
              min="0"
              step="50000"
              className={inputCls}
              placeholder="800000"
              value={form.basePrice}
              onChange={(e) => setForm({ ...form, basePrice: e.target.value })}
            />
            {pricePreview && <p className="mt-1.5 text-[12px] tabular-nums text-ink-500">= {pricePreview} /đêm</p>}
          </div>

          {formError && (
            <p className="rounded-lg bg-amber-50 px-3.5 py-2.5 text-[12px] font-medium text-amber-800 ring-1 ring-amber-600/15">{formError}</p>
          )}

          <button
            type="submit"
            disabled={saving}
            className={`w-full rounded-full bg-brand-600 py-3 text-[13px] font-bold text-white ${EASE} hover:bg-brand-700 active:scale-[0.98] disabled:opacity-40`}
          >
            {saving ? 'Đang lưu…' : editing ? 'Lưu thay đổi' : 'Thêm loại phòng'}
          </button>
        </form>
      </SlideOver>

      {/* Xác nhận xóa */}
      <ConfirmDialog
        open={toDelete !== null}
        title={`Xóa loại phòng ${toDelete?.typeName ?? ''}?`}
        message="Các phòng đang thuộc loại này sẽ cần chuyển sang loại khác. Hành động không hoàn tác được."
        busy={deleting}
        error={deleteError}
        onConfirm={confirmDelete}
        onCancel={() => setToDelete(null)}
      />
    </div>
  )
}
