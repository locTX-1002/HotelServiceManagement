import { useEffect, useMemo, useState } from 'react'
import client, { isBackendMissing } from '../api/client'
import ConfirmDialog from '../components/ConfirmDialog'
import ErrorState from '../components/ErrorState'
import RoomsTabs from '../components/RoomsTabs'
import SlideOver from '../components/SlideOver'
import { useToast } from '../components/toastContext'
import { MOCK_ROOMS, MOCK_ROOM_TYPES_FULL } from '../mock/hotelMock'
import { denormalizeStatus, normalizeRoom, normalizeRoomType } from '../utils/apiShape'
import { roomImage } from '../utils/roomImages'
import { ROOM_STATUS, formatVnd } from '../utils/roomStatus'

const EASE = 'transition-all duration-300 ease-[cubic-bezier(0.32,0.72,0,1)]'
const inputCls =
  'w-full rounded-xl bg-white px-3.5 py-2.5 text-sm ring-1 ring-black/10 outline-none placeholder:text-ink-500/50 focus:ring-2 focus:ring-brand-500/40'
const labelCls = 'mb-1.5 block text-[12px] font-semibold text-ink-700'

const EMPTY_FORM = { roomNumber: '', floor: 1, roomTypeId: '', status: 'Available' }

const apiError = (err) =>
  isBackendMissing(err)
    ? 'Không gọi được API /api/rooms (backend chưa chạy hoặc chưa merge auth). Thử lại khi backend sẵn sàng.'
    : err.response?.data?.message ?? 'Máy chủ báo lỗi. Thử lại sau ít phút.'

// Nghiệp vụ: không cho xóa phòng đang phục vụ khách - phải check-out / hủy đặt trước
const deleteBlocked = (room) =>
  room?.status === 'Occupied' || room?.status === 'Reserved'
    ? 'Phòng đang có khách hoặc đã có đặt phòng. Check-out hoặc hủy đặt phòng trước khi xóa.'
    : ''

export default function RoomPage() {
  const toast = useToast()
  const [rooms, setRooms] = useState(null)
  const [types, setTypes] = useState([])
  const [usingMock, setUsingMock] = useState(false)
  const [loadError, setLoadError] = useState(false)
  const [statusFilter, setStatusFilter] = useState('all')
  const [search, setSearch] = useState('')
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
      .get('/api/rooms')
      .then((res) => { setRooms(res.data.map(normalizeRoom)); setUsingMock(false) })
      .catch((err) => {
        if (isBackendMissing(err)) { setRooms(MOCK_ROOMS); setUsingMock(true) }
        else setLoadError(true) // lỗi thật: không che bằng mock
      })
    // Loại phòng chỉ phục vụ ô chọn trong form + tra tên, lỗi thì âm thầm dùng mock
    client
      .get('/api/room-types')
      .then((res) => setTypes(res.data.map(normalizeRoomType)))
      .catch(() => setTypes(MOCK_ROOM_TYPES_FULL))
  }
  useEffect(load, [])

  // Tra thông tin loại phòng: ưu tiên field backend trả kèm, thiếu thì tra theo roomTypeId
  const typeOf = (room) => types.find((t) => t.roomTypeId === room.roomTypeId)
  const typeNameOf = (room) => room.typeName ?? typeOf(room)?.typeName ?? '—'
  const priceOf = (room) => room.basePrice ?? typeOf(room)?.basePrice

  const visible = useMemo(() => {
    const q = search.trim().toLowerCase()
    return (rooms ?? [])
      .filter((r) => (statusFilter === 'all' || r.status === statusFilter) && (!q || r.roomNumber.toLowerCase().includes(q)))
      .sort((a, b) => a.floor - b.floor || a.roomNumber.localeCompare(b.roomNumber, 'vi', { numeric: true }))
  }, [rooms, statusFilter, search])

  const count = (st) => (rooms ?? []).filter((r) => r.status === st).length
  const chips = [
    { key: 'all', label: 'Tất cả', n: (rooms ?? []).length },
    ...Object.entries(ROOM_STATUS)
      .map(([key, s]) => ({ key, label: s.label, n: count(key), dot: s.dot }))
      .filter((c) => c.n > 0),
  ]

  const openCreate = () => {
    setForm({ ...EMPTY_FORM, roomTypeId: types[0]?.roomTypeId ?? '' })
    setFormError('')
    setDrawer({ mode: 'create' })
  }
  const openEdit = (item) => {
    setForm({ roomNumber: item.roomNumber, floor: item.floor, roomTypeId: item.roomTypeId, status: item.status })
    setFormError('')
    setDrawer({ mode: 'edit', item })
  }

  const validate = () => {
    const num = form.roomNumber.trim()
    if (!num) return 'Nhập số phòng.'
    const dup = (rooms ?? []).some(
      (r) => r.roomNumber.trim().toLowerCase() === num.toLowerCase() && r.roomId !== drawer?.item?.roomId,
    )
    if (dup) return 'Số phòng này đã tồn tại.'
    const floor = Number(form.floor)
    if (!Number.isInteger(floor) || floor < 1 || floor > 50) return 'Tầng phải là số nguyên từ 1 đến 50.'
    if (!form.roomTypeId) return 'Chọn loại phòng.'
    return ''
  }

  const submit = (e) => {
    e.preventDefault()
    const msg = validate()
    if (msg) return setFormError(msg)

    setFormError('')
    setSaving(true)
    const payload = {
      roomNumber: form.roomNumber.trim(),
      floor: Number(form.floor),
      roomTypeId: Number(form.roomTypeId),
      status: denormalizeStatus(form.status), // backend nhận enum dạng số
      isActive: drawer?.item?.isActive ?? true,
    }
    const req =
      drawer.mode === 'edit'
        ? client.put(`/api/rooms/${drawer.item.roomId}`, payload)
        : client.post('/api/rooms', payload)
    req
      .then(() => {
        toast.success(drawer.mode === 'edit' ? `Đã lưu phòng ${payload.roomNumber}` : `Đã thêm phòng ${payload.roomNumber}`)
        setDrawer(null)
        load()
      })
      .catch((err) => setFormError(apiError(err)))
      .finally(() => setSaving(false))
  }

  const confirmDelete = () => {
    const blocked = deleteBlocked(toDelete)
    if (blocked) return setDeleteError(blocked)

    setDeleteError('')
    setDeleting(true)
    client
      .delete(`/api/rooms/${toDelete.roomId}`)
      .then(() => {
        toast.success(`Đã xóa phòng ${toDelete.roomNumber}`)
        setToDelete(null)
        load()
      })
      .catch((err) => setDeleteError(apiError(err)))
      .finally(() => setDeleting(false))
  }

  const editing = drawer?.mode === 'edit'

  return (
    <div>
      {/* Header: tiêu đề trái, tabs + nút thêm phải */}
      <div className="flex flex-wrap items-end justify-between gap-4">
        <div>
          <p className="font-display text-[15px] italic capitalize text-brand-600">quản lý khách sạn</p>
          <h1 className="mt-1 font-display text-4xl font-semibold tracking-tight">Danh sách phòng</h1>
          <p className="mt-1 text-sm text-ink-500">Thêm, sửa số phòng và loại phòng — trạng thái vận hành theo dõi ở sơ đồ phòng.</p>
        </div>
        <div className="flex flex-wrap items-center gap-2.5">
          <RoomsTabs />
          <button
            onClick={openCreate}
            className={`rounded-full bg-ink-900 px-5 py-2.5 text-[13px] font-bold text-cream-50 ${EASE} hover:bg-ink-700 active:scale-[0.98]`}
          >
            + Thêm phòng
          </button>
        </div>
      </div>

      {/* Bộ lọc: chips trạng thái + ô tìm theo số phòng */}
      <div className="mt-5 flex flex-wrap items-center gap-3">
        <div className="inline-flex flex-wrap items-center gap-0.5 rounded-full bg-white p-1 ring-1 ring-black/10 shadow-soft">
          {chips.map((c) => {
            const active = statusFilter === c.key
            return (
              <button
                key={c.key}
                onClick={() => setStatusFilter(c.key)}
                className={`flex items-center gap-1.5 rounded-full px-3.5 py-1.5 text-[12px] font-semibold ${EASE} active:scale-[0.97] ${
                  active ? 'bg-ink-900 text-cream-50' : 'text-ink-500 hover:text-ink-900'
                }`}
              >
                {c.dot && <span className={`h-1.5 w-1.5 rounded-full ${c.dot}`} />}
                {c.label}
                <span className={`tabular-nums font-medium ${active ? 'text-cream-50/60' : 'text-ink-500/50'}`}>{c.n}</span>
              </button>
            )
          })}
        </div>
        <div className="ml-auto flex items-center gap-2.5">
          {usingMock && (
            <span className="rounded-full bg-amber-50 px-2.5 py-1 text-[11px] font-bold text-amber-800 ring-1 ring-amber-600/20">
              Dữ liệu mẫu — chờ API
            </span>
          )}
          <input
            className="w-44 rounded-full bg-white px-4 py-2 text-[13px] ring-1 ring-black/10 outline-none placeholder:text-ink-500/50 focus:ring-2 focus:ring-brand-500/40"
            placeholder="Tìm số phòng…"
            value={search}
            onChange={(e) => setSearch(e.target.value)}
          />
        </div>
      </div>

      {/* Bảng phòng */}
      {rooms === null && !loadError && (
        <div className="mt-6 space-y-3">
          {Array.from({ length: 5 }).map((_, i) => (
            <div key={i} className="h-16 animate-pulse rounded-2xl bg-cream-200" />
          ))}
        </div>
      )}

      {loadError && (
        <div className="mt-6"><ErrorState onRetry={load} /></div>
      )}

      {!loadError && rooms !== null && visible.length > 0 && (
        <div className="card-rise mt-6 overflow-hidden rounded-2xl bg-white ring-1 ring-black/5 shadow-soft">
          <div className="overflow-x-auto">
          <table className="w-full min-w-[620px] text-left">
            <thead>
              <tr className="border-b border-black/[0.06] text-[10px] font-bold uppercase tracking-[0.18em] text-ink-500">
                <th className="px-5 py-3.5">Phòng</th>
                <th className="px-5 py-3.5">Tầng</th>
                <th className="px-5 py-3.5">Loại phòng</th>
                <th className="px-5 py-3.5">Trạng thái</th>
                <th className="px-5 py-3.5 text-right">Thao tác</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-black/[0.05]">
              {visible.map((r, idx) => {
                const s = ROOM_STATUS[r.status] ?? ROOM_STATUS.Maintenance
                return (
                  <tr key={r.roomId} className={`${EASE} hover:bg-cream-50/60`}>
                    <td className="px-5 py-3.5">
                      <div className="flex items-center gap-3.5">
                        <img
                          src={roomImage(typeNameOf(r), idx)}
                          alt={typeNameOf(r)}
                          loading="lazy"
                          className="h-12 w-9 shrink-0 rounded-t-full rounded-b-md object-cover"
                        />
                        <p className="font-display text-xl font-semibold tabular-nums tracking-tight">{r.roomNumber}</p>
                      </div>
                    </td>
                    <td className="px-5 py-3.5 text-sm tabular-nums text-ink-700">Tầng {r.floor}</td>
                    <td className="px-5 py-3.5">
                      <p className="text-sm font-semibold">{typeNameOf(r)}</p>
                      {priceOf(r) != null && (
                        <p className="text-[11px] tabular-nums text-ink-500">{formatVnd(priceOf(r))} /đêm</p>
                      )}
                    </td>
                    <td className="px-5 py-3.5">
                      <span className={`rounded-full px-2.5 py-1 text-[11px] font-semibold ${s.badge}`}>{s.label}</span>
                    </td>
                    <td className="px-5 py-3.5 text-right">
                      <button
                        onClick={() => openEdit(r)}
                        className={`rounded-full px-3.5 py-1.5 text-[12px] font-semibold text-ink-700 ring-1 ring-black/10 ${EASE} hover:bg-cream-100`}
                      >
                        Sửa
                      </button>
                      <button
                        onClick={() => { setDeleteError(deleteBlocked(r)); setToDelete(r) }}
                        className={`ml-2 rounded-full px-3.5 py-1.5 text-[12px] font-semibold text-rose-700 ring-1 ring-rose-600/20 ${EASE} hover:bg-rose-50`}
                      >
                        Xóa
                      </button>
                    </td>
                  </tr>
                )
              })}
            </tbody>
          </table>
          </div>
        </div>
      )}

      {!loadError && rooms !== null && visible.length === 0 && (
        <div className="mt-6 flex flex-col items-center rounded-2xl border border-dashed border-black/10 bg-white/60 px-6 py-14">
          <span className="h-12 w-9 rounded-t-full rounded-b-md border-2 border-dashed border-brand-600/30" />
          <p className="mt-4 font-display text-lg italic text-ink-700">
            {rooms.length === 0 ? 'Chưa có phòng nào' : 'Không có phòng khớp bộ lọc'}
          </p>
          {rooms.length === 0 ? (
            <button onClick={openCreate} className="mt-2 text-[12px] font-bold uppercase tracking-wider text-brand-600 hover:underline">
              Thêm phòng đầu tiên
            </button>
          ) : (
            <button
              onClick={() => { setStatusFilter('all'); setSearch('') }}
              className="mt-2 text-[12px] font-bold uppercase tracking-wider text-brand-600 hover:underline"
            >
              Xóa bộ lọc
            </button>
          )}
        </div>
      )}

      {/* Form thêm / sửa */}
      <SlideOver
        open={drawer !== null}
        eyebrow={editing ? 'chỉnh sửa' : 'thêm mới'}
        title={editing ? `Sửa phòng ${drawer.item.roomNumber}` : 'Thêm phòng'}
        onClose={() => setDrawer(null)}
      >
        <form onSubmit={submit} className="space-y-5">
          <div>
            <label htmlFor="room-number" className={labelCls}>Số phòng *</label>
            <input
              id="room-number"
              className={inputCls}
              placeholder="101"
              value={form.roomNumber}
              onChange={(e) => setForm({ ...form, roomNumber: e.target.value })}
            />
          </div>
          <div>
            <label className={labelCls}>Tầng *</label>
            <div className="inline-flex items-center gap-1 rounded-xl bg-white ring-1 ring-black/10">
              <button
                type="button"
                onClick={() => setForm({ ...form, floor: Math.max(1, Number(form.floor) - 1) })}
                className="px-3.5 py-2.5 text-sm font-bold text-ink-500 hover:text-ink-900"
              >
                −
              </button>
              <span className="w-8 text-center text-sm font-bold tabular-nums">{form.floor}</span>
              <button
                type="button"
                onClick={() => setForm({ ...form, floor: Math.min(50, Number(form.floor) + 1) })}
                className="px-3.5 py-2.5 text-sm font-bold text-ink-500 hover:text-ink-900"
              >
                +
              </button>
            </div>
          </div>
          <div>
            <label htmlFor="room-type" className={labelCls}>Loại phòng *</label>
            <select
              id="room-type"
              className={inputCls}
              value={form.roomTypeId}
              onChange={(e) => setForm({ ...form, roomTypeId: e.target.value })}
            >
              <option value="">— Chọn loại phòng —</option>
              {types.map((t) => (
                <option key={t.roomTypeId} value={t.roomTypeId}>
                  {t.typeName} — {formatVnd(t.basePrice)}/đêm
                </option>
              ))}
            </select>
          </div>
          {editing ? (
            <div>
              <label htmlFor="room-status" className={labelCls}>Trạng thái</label>
              <select
                id="room-status"
                className={inputCls}
                value={form.status}
                onChange={(e) => setForm({ ...form, status: e.target.value })}
              >
                {Object.entries(ROOM_STATUS).map(([key, s]) => (
                  <option key={key} value={key}>{s.label}</option>
                ))}
              </select>
            </div>
          ) : (
            <p className="text-[12px] text-ink-500">Phòng mới sẽ ở trạng thái <span className="font-semibold text-emerald-700">Trống</span>, sẵn sàng đón khách.</p>
          )}

          {formError && (
            <p className="rounded-lg bg-amber-50 px-3.5 py-2.5 text-[12px] font-medium text-amber-800 ring-1 ring-amber-600/15">{formError}</p>
          )}

          <button
            type="submit"
            disabled={saving}
            className={`w-full rounded-full bg-brand-600 py-3 text-[13px] font-bold text-white ${EASE} hover:bg-brand-700 active:scale-[0.98] disabled:opacity-40`}
          >
            {saving ? 'Đang lưu…' : editing ? 'Lưu thay đổi' : 'Thêm phòng'}
          </button>
        </form>
      </SlideOver>

      {/* Xác nhận xóa */}
      <ConfirmDialog
        open={toDelete !== null}
        title={`Xóa phòng ${toDelete?.roomNumber ?? ''}?`}
        message="Lịch sử đặt phòng liên quan có thể bị ảnh hưởng. Hành động không hoàn tác được."
        busy={deleting}
        error={deleteError}
        onConfirm={confirmDelete}
        onCancel={() => setToDelete(null)}
      />
    </div>
  )
}
