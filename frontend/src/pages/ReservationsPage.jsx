import { useEffect, useMemo, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import client, { isBackendMissing } from '../api/client'
import ConfirmDialog from '../components/ConfirmDialog'
import ErrorState from '../components/ErrorState'
import { useToast } from '../components/toastContext'
import { MOCK_RESERVATIONS } from '../mock/hotelMock'
import { normalizeReservation } from '../utils/apiShape'
import { fmtShort } from '../utils/dates'

const EASE = 'transition-all duration-300 ease-[cubic-bezier(0.32,0.72,0,1)]'

// Nhãn + màu cho 5 trạng thái đặt phòng (khớp enum backend)
const RES_STATUS = {
  Pending: { label: 'Chờ xác nhận', badge: 'bg-amber-50 text-amber-800 ring-1 ring-amber-600/15', dot: 'bg-amber-500' },
  Confirmed: { label: 'Đã xác nhận', badge: 'bg-sky-50 text-sky-700 ring-1 ring-sky-600/15', dot: 'bg-sky-500' },
  CheckedIn: { label: 'Đã nhận phòng', badge: 'bg-emerald-50 text-emerald-700 ring-1 ring-emerald-600/15', dot: 'bg-emerald-500' },
  Completed: { label: 'Hoàn tất', badge: 'bg-stone-100 text-stone-600 ring-1 ring-stone-500/15', dot: 'bg-stone-400' },
  Cancelled: { label: 'Đã hủy', badge: 'bg-rose-50 text-rose-700 ring-1 ring-rose-600/15', dot: 'bg-rose-400' },
}
const STATUS_ORDER = ['Pending', 'Confirmed', 'CheckedIn', 'Completed', 'Cancelled']

// Chỉ hủy được khi chưa nhận phòng và chưa hủy
const canCancel = (r) => r.status === 'Pending' || r.status === 'Confirmed'
const dkey = (d) => String(d).slice(0, 10)

const apiError = (err) =>
  isBackendMissing(err)
    ? 'Không kết nối được máy chủ. Vui lòng thử lại sau.'
    : err.response?.data?.message ?? 'Máy chủ báo lỗi. Thử lại sau ít phút.'

export default function ReservationsPage() {
  const navigate = useNavigate()
  const toast = useToast()
  const [list, setList] = useState(null)
  const [usingMock, setUsingMock] = useState(false)
  const [loadError, setLoadError] = useState(false)
  const [statusFilter, setStatusFilter] = useState('all')
  const [search, setSearch] = useState('')
  const [toCancel, setToCancel] = useState(null)
  const [cancelling, setCancelling] = useState(false)
  const [cancelError, setCancelError] = useState('')

  const load = () => {
    setLoadError(false)
    client
      .get('/api/reservations')
      .then((res) => { setList(res.data.map(normalizeReservation)); setUsingMock(false) })
      .catch((err) => {
        if (isBackendMissing(err)) { setList(MOCK_RESERVATIONS.map(normalizeReservation)); setUsingMock(true) }
        else setLoadError(true) // lỗi thật: không che bằng mock
      })
  }
  useEffect(load, [])

  const visible = useMemo(() => {
    const q = search.trim().toLowerCase()
    return (list ?? [])
      .filter((r) => (statusFilter === 'all' || r.status === statusFilter) &&
        (!q || (r.bookingCode ?? '').toLowerCase().includes(q) || (r.guestName ?? '').toLowerCase().includes(q) || String(r.roomNumber).includes(q)))
      .sort((a, b) => dkey(b.checkInDate).localeCompare(dkey(a.checkInDate)))
  }, [list, statusFilter, search])

  const count = (st) => (list ?? []).filter((r) => r.status === st).length
  const chips = [
    { key: 'all', label: 'Tất cả', n: (list ?? []).length },
    ...STATUS_ORDER.map((key) => ({ key, label: RES_STATUS[key].label, n: count(key), dot: RES_STATUS[key].dot })).filter((c) => c.n > 0),
  ]

  const confirmCancel = () => {
    setCancelError('')
    setCancelling(true)
    client
      .patch(`/api/reservations/${toCancel.reservationId}/cancel`)
      .then(() => {
        toast.success(`Đã hủy đặt phòng ${toCancel.bookingCode}`)
        setToCancel(null)
        load()
      })
      .catch((err) => setCancelError(apiError(err)))
      .finally(() => setCancelling(false))
  }

  return (
    <div>
      {/* Header */}
      <div className="flex flex-wrap items-end justify-between gap-4">
        <div>
          <p className="font-display text-[15px] italic capitalize text-brand-600">lễ tân · đặt phòng</p>
          <h1 className="mt-1 font-display text-4xl font-semibold tracking-tight">Danh sách đặt phòng</h1>
          <p className="mt-1 text-sm text-ink-500">Theo dõi và hủy các lượt đặt phòng theo trạng thái.</p>
        </div>
        <button
          onClick={() => navigate('/reservations/new')}
          className={`rounded-full bg-ink-900 px-5 py-2.5 text-[13px] font-bold text-cream-50 ${EASE} hover:bg-ink-700 active:scale-[0.98]`}
        >
          + Tạo đặt phòng
        </button>
      </div>

      {/* Bộ lọc: chips trạng thái + tìm */}
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
              Dữ liệu mẫu
            </span>
          )}
          <input
            className="w-52 rounded-full bg-white px-4 py-2 text-[13px] ring-1 ring-black/10 outline-none placeholder:text-ink-500/50 focus:ring-2 focus:ring-brand-500/40"
            placeholder="Tìm mã / khách / phòng…"
            value={search}
            onChange={(e) => setSearch(e.target.value)}
          />
        </div>
      </div>

      {/* Bảng */}
      {list === null && !loadError && (
        <div className="mt-6 space-y-3">
          {Array.from({ length: 5 }).map((_, i) => (
            <div key={i} className="h-16 animate-pulse rounded-2xl bg-cream-200" />
          ))}
        </div>
      )}

      {loadError && <div className="mt-6"><ErrorState onRetry={load} /></div>}

      {!loadError && list !== null && visible.length > 0 && (
        <div className="card-rise mt-6 bezel-shell">
          <div className="bezel-core overflow-hidden">
            <div className="overflow-x-auto">
              <table className="w-full min-w-[720px] text-left">
                <thead>
                  <tr className="border-b border-black/[0.06] text-[10px] font-bold uppercase tracking-[0.18em] text-ink-500">
                    <th className="px-5 py-3.5">Mã đặt phòng</th>
                    <th className="px-5 py-3.5">Khách</th>
                    <th className="px-5 py-3.5">Phòng</th>
                    <th className="px-5 py-3.5">Ngày ở</th>
                    <th className="px-5 py-3.5">Trạng thái</th>
                    <th className="px-5 py-3.5 text-right">Thao tác</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-black/[0.05]">
                  {visible.map((r) => {
                    const s = RES_STATUS[r.status] ?? RES_STATUS.Pending
                    return (
                      <tr key={r.reservationId} className={`${EASE} hover:bg-cream-50/60`}>
                        <td className="px-5 py-3.5">
                          <span className="font-display text-base font-semibold tabular-nums">{r.bookingCode}</span>
                        </td>
                        <td className="px-5 py-3.5">
                          <p className="text-sm font-semibold">{r.guestName || '—'}</p>
                          {r.guestPhoneNumber && <p className="text-[11px] tabular-nums text-ink-500">{r.guestPhoneNumber}</p>}
                        </td>
                        <td className="px-5 py-3.5">
                          <p className="text-sm font-semibold tabular-nums">{r.roomNumber}</p>
                          <p className="text-[11px] text-ink-500">{r.typeName}</p>
                        </td>
                        <td className="px-5 py-3.5 text-sm tabular-nums text-ink-700">
                          {fmtShort(dkey(r.checkInDate))} → {fmtShort(dkey(r.checkOutDate))}
                        </td>
                        <td className="px-5 py-3.5">
                          <span className={`rounded-full px-2.5 py-1 text-[11px] font-semibold ${s.badge}`}>{s.label}</span>
                        </td>
                        <td className="px-5 py-3.5 text-right">
                          {canCancel(r) ? (
                            <button
                              onClick={() => { setCancelError(''); setToCancel(r) }}
                              className={`rounded-full px-3.5 py-1.5 text-[12px] font-semibold text-rose-700 ring-1 ring-rose-600/20 ${EASE} hover:bg-rose-50`}
                            >
                              Hủy
                            </button>
                          ) : (
                            <span className="text-[12px] text-ink-500/50">—</span>
                          )}
                        </td>
                      </tr>
                    )
                  })}
                </tbody>
              </table>
            </div>
          </div>
        </div>
      )}

      {!loadError && list !== null && visible.length === 0 && (
        <div className="mt-6 flex flex-col items-center rounded-2xl border border-dashed border-black/10 bg-white/60 px-6 py-14">
          <span className="h-12 w-9 rounded-t-full rounded-b-md border-2 border-dashed border-brand-600/30" />
          <p className="mt-4 font-display text-lg italic text-ink-700">
            {(list ?? []).length === 0 ? 'Chưa có lượt đặt phòng nào' : 'Không có đặt phòng khớp bộ lọc'}
          </p>
          {(list ?? []).length === 0 ? (
            <button onClick={() => navigate('/reservations/new')} className="mt-2 text-[12px] font-bold uppercase tracking-wider text-brand-600 hover:underline">
              Tạo đặt phòng đầu tiên
            </button>
          ) : (
            <button onClick={() => { setStatusFilter('all'); setSearch('') }} className="mt-2 text-[12px] font-bold uppercase tracking-wider text-brand-600 hover:underline">
              Xóa bộ lọc
            </button>
          )}
        </div>
      )}

      {/* Xác nhận hủy */}
      <ConfirmDialog
        open={toCancel !== null}
        title={`Hủy đặt phòng ${toCancel?.bookingCode ?? ''}?`}
        message={`Lượt đặt của ${toCancel?.guestName ?? 'khách'} sẽ chuyển sang trạng thái Đã hủy và phòng được mở lại. Hành động không hoàn tác được.`}
        confirmLabel="Hủy đặt phòng"
        busy={cancelling}
        error={cancelError}
        onConfirm={confirmCancel}
        onCancel={() => setToCancel(null)}
      />
    </div>
  )
}
