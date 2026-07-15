import { useEffect, useMemo, useState } from 'react'
import EmptyState from '../components/EmptyState'
import { EASE, errorCls } from '../utils/ui'
import { useNavigate } from 'react-router-dom'
import client, { isBackendMissing, apiError } from '../api/client'
import ConfirmDialog from '../components/ConfirmDialog'
import ErrorState from '../components/ErrorState'
import { useToast } from '../components/toastContext'
import { MOCK_ACTIVE_STAYS, MOCK_RESERVATIONS, MOCK_SURCHARGE_ITEMS } from '../mock/hotelMock'
import { normalizeReservation } from '../utils/apiShape'
import { fmtDateTime, localNowIso } from '../utils/dates'
import { formatVnd } from '../utils/roomStatus'

const SkeletonRows = () => (
  <div className="mt-6 space-y-3">
    {Array.from({ length: 3 }).map((_, i) => (
      <div key={i} className="h-16 animate-pulse rounded-2xl bg-cream-200" />
    ))}
  </div>
)

// Biên nhận sau check-out: tổng tiền phòng + dịch vụ vừa tính từ backend, để lễ tân báo khách trước khi thu tiền.
function ReceiptDialog({ receipt, onClose, onPay }) {
  if (!receipt) return null
  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center px-6">
      <div onClick={onClose} className="absolute inset-0 bg-ink-900/30" />
      <div className="bezel-shell relative w-full max-w-sm">
        <div className="bezel-core px-6 py-7 text-center">
          <span className="mx-auto flex h-10 w-8 items-end justify-center rounded-t-full rounded-b-md bg-emerald-50 pb-1 text-sm font-bold text-emerald-700 ring-1 ring-emerald-600/15">
            ✓
          </span>
          <p className="mt-4 font-display text-xl font-semibold tracking-tight">Check-out thành công</p>
          <p className="mt-1 text-[13px] text-ink-500">
            Phòng {receipt.roomNumber} · {receipt.guestName}
          </p>

          <div className="mt-5 space-y-2 rounded-xl bg-white p-4 text-left ring-1 ring-black/5">
            <div className="flex items-center justify-between text-[13px]">
              <span className="text-ink-500">Tiền phòng</span>
              <span className="font-semibold tabular-nums">{formatVnd(receipt.totalRoomCharges)}</span>
            </div>
            <div className="flex items-center justify-between text-[13px]">
              <span className="text-ink-500">Tiền dịch vụ</span>
              <span className="font-semibold tabular-nums">{formatVnd(receipt.totalServiceCharges)}</span>
            </div>
            {receipt.totalSurchargeCharges > 0 && (
              <div className="flex items-center justify-between text-[13px]">
                <span className="text-ink-500">Phụ thu / đền bù</span>
                <span className="font-semibold tabular-nums text-amber-800">{formatVnd(receipt.totalSurchargeCharges)}</span>
              </div>
            )}
            <div className="flex items-center justify-between border-t border-black/[0.06] pt-2 text-sm">
              <span className="font-semibold text-ink-900">Tổng cộng</span>
              <span className="font-display text-lg font-semibold tabular-nums text-brand-700">{formatVnd(receipt.totalAmount)}</span>
            </div>
          </div>

          <div className="mt-6 flex gap-2.5">
            <button
              onClick={onClose}
              className={`flex-1 rounded-full py-2.5 text-[13px] font-semibold text-ink-700 ring-1 ring-black/10 ${EASE} hover:bg-white`}
            >
              Đóng
            </button>
            <button
              onClick={onPay}
              className={`flex-1 rounded-full bg-brand-600 py-2.5 text-[13px] font-bold text-white ${EASE} hover:bg-brand-700 active:scale-[0.98]`}
            >
              Ghi nhận thanh toán
            </button>
          </div>
        </div>
      </div>
    </div>
  )
}

// Dialog check-out: xác nhận trả phòng + tick phụ thu đồ dùng/hư hỏng (cộng vào hoá đơn).
// Gửi surcharges: [{ surchargeItemId, quantity }] trong body check-out; qty 0 thì bỏ qua.
function CheckoutDialog({ stay, items, busy, error, onConfirm, onCancel }) {
  const [qty, setQty] = useState({})
  useEffect(() => { if (stay) setQty({}) }, [stay]) // mở lượt khác thì xoá số lượng cũ
  if (!stay) return null

  const active = (items ?? []).filter((i) => i.isActive !== false)
  const bump = (id, d) => setQty((cur) => ({ ...cur, [id]: Math.max(0, (cur[id] ?? 0) + d) }))
  const lines = active.map((i) => ({ ...i, quantity: qty[i.id] ?? 0 })).filter((l) => l.quantity > 0)
  const surchargeTotal = lines.reduce((s, l) => s + l.unitPrice * l.quantity, 0)

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center px-6 py-8">
      <div onClick={busy ? undefined : onCancel} className="absolute inset-0 bg-ink-900/30" />
      <div className="bezel-shell relative w-full max-w-md">
        <div className="bezel-core flex max-h-[85vh] flex-col px-6 py-6">
          <p className="font-display text-xl font-semibold tracking-tight">Check-out phòng {stay.roomNumber}?</p>
          <p className="mt-1 text-[13px] text-ink-500">
            {stay.guestName ?? 'Khách'} — hệ thống tính tiền phòng + dịch vụ và tạo hoá đơn. Không hoàn tác được.
          </p>

          {active.length > 0 && (
            <div className="mt-4 flex min-h-0 flex-col">
              <p className="text-[11px] font-bold uppercase tracking-[0.18em] text-ink-500">Phụ thu đồ dùng & đền bù</p>
              <p className="mt-0.5 text-[12px] text-ink-500">Chọn số lượng món khách dùng thêm / làm hư / thất lạc.</p>
              <div className="mt-3 min-h-0 flex-1 space-y-1.5 overflow-y-auto pr-1">
                {active.map((i) => {
                  const n = qty[i.id] ?? 0
                  return (
                    <div key={i.id} className={`flex items-center gap-3 rounded-xl px-3 py-2 ring-1 ${n > 0 ? 'bg-amber-50/60 ring-amber-600/20' : 'bg-white ring-black/[0.06]'}`}>
                      <div className="min-w-0 flex-1">
                        <p className="truncate text-[13px] font-semibold text-ink-800">{i.name}</p>
                        <p className="text-[11px] tabular-nums text-ink-500">{formatVnd(i.unitPrice)}/{i.unit}</p>
                      </div>
                      {n > 0 && <span className="text-[12px] font-semibold tabular-nums text-amber-800">{formatVnd(i.unitPrice * n)}</span>}
                      <div className="flex items-center gap-1 rounded-lg bg-white ring-1 ring-black/10">
                        <button type="button" onClick={() => bump(i.id, -1)} disabled={n === 0} className="px-2.5 py-1 text-sm font-bold text-ink-500 hover:text-ink-900 disabled:opacity-30">−</button>
                        <span className="w-6 text-center text-[13px] font-bold tabular-nums">{n}</span>
                        <button type="button" onClick={() => bump(i.id, 1)} className="px-2.5 py-1 text-sm font-bold text-ink-500 hover:text-ink-900">+</button>
                      </div>
                    </div>
                  )
                })}
              </div>
              <div className="mt-2 flex items-center justify-between border-t border-black/[0.06] pt-2 text-[13px]">
                <span className="font-semibold text-ink-700">Tổng phụ thu</span>
                <span className="font-display text-base font-semibold tabular-nums text-amber-800">{formatVnd(surchargeTotal)}</span>
              </div>
            </div>
          )}

          {error && <p className={`mt-4 ${errorCls}`}>{error}</p>}

          <div className="mt-5 flex gap-2.5">
            <button
              onClick={onCancel}
              disabled={busy}
              className={`flex-1 rounded-full py-2.5 text-[13px] font-semibold text-ink-700 ring-1 ring-black/10 ${EASE} hover:bg-white disabled:opacity-50`}
            >
              Hủy
            </button>
            <button
              onClick={() => onConfirm(lines.map((l) => ({ surchargeItemId: l.id, quantity: l.quantity })))}
              disabled={busy}
              className={`flex-1 rounded-full bg-rose-600 py-2.5 text-[13px] font-bold text-white ${EASE} hover:bg-rose-700 active:scale-[0.98] disabled:opacity-50`}
            >
              {busy ? 'Đang check-out…' : 'Check-out'}
            </button>
          </div>
        </div>
      </div>
    </div>
  )
}

export default function CheckInOutPage() {
  const navigate = useNavigate()
  const toast = useToast()
  const [tab, setTab] = useState('checkin')
  const [search, setSearch] = useState('')

  const [reservations, setReservations] = useState(null)
  const [resUsingMock, setResUsingMock] = useState(false)
  const [resError, setResError] = useState(false)

  const [stays, setStays] = useState(null)
  const [staysUsingMock, setStaysUsingMock] = useState(false)
  const [staysError, setStaysError] = useState(false)

  const [toCheckIn, setToCheckIn] = useState(null)
  const [checkingIn, setCheckingIn] = useState(false)
  const [checkInError, setCheckInError] = useState('')

  const [toCheckOut, setToCheckOut] = useState(null)
  const [checkingOut, setCheckingOut] = useState(false)
  const [checkOutError, setCheckOutError] = useState('')

  const [receipt, setReceipt] = useState(null)
  const [surchargeItems, setSurchargeItems] = useState([]) // danh mục phụ thu cho dialog check-out

  const loadReservations = () => {
    setResError(false)
    client
      .get('/api/reservations')
      .then((res) => { setReservations(res.data.map(normalizeReservation)); setResUsingMock(false) })
      .catch((err) => {
        if (isBackendMissing(err)) { setReservations(MOCK_RESERVATIONS.map(normalizeReservation)); setResUsingMock(true) }
        else setResError(true)
      })
  }

  const loadStays = () => {
    setStaysError(false)
    client
      .get('/api/stays/active')
      .then((res) => { setStays(res.data); setStaysUsingMock(false) })
      .catch((err) => {
        if (isBackendMissing(err)) { setStays(MOCK_ACTIVE_STAYS); setStaysUsingMock(true) }
        else setStaysError(true)
      })
  }

  // Danh mục phụ thu - chỉ dùng mock khi backend chưa có endpoint
  const loadSurchargeItems = () => {
    client
      .get('/api/surcharge-items')
      .then((res) => setSurchargeItems(res.data))
      .catch((err) => { if (isBackendMissing(err)) setSurchargeItems(MOCK_SURCHARGE_ITEMS) })
  }

  useEffect(() => { loadReservations(); loadStays(); loadSurchargeItems() }, [])

  const pendingCheckIn = useMemo(() => {
    const q = search.trim().toLowerCase()
    return (reservations ?? [])
      .filter((r) => r.status === 'Confirmed')
      .filter((r) => !q || r.bookingCode.toLowerCase().includes(q) || (r.guestName ?? '').toLowerCase().includes(q) || String(r.roomNumber).includes(q))
      .sort((a, b) => String(a.checkInDate).localeCompare(String(b.checkInDate)))
  }, [reservations, search])

  const activeStays = useMemo(() => {
    const q = search.trim().toLowerCase()
    return (stays ?? [])
      .filter((s) => !q || s.bookingCode.toLowerCase().includes(q) || (s.guestName ?? '').toLowerCase().includes(q) || String(s.roomNumber).includes(q))
      .sort((a, b) => String(a.plannedCheckOut).localeCompare(String(b.plannedCheckOut)))
  }, [stays, search])

  const confirmCheckIn = () => {
    setCheckInError('')
    setCheckingIn(true)
    client
      // localNowIso (không 'Z'): toISOString gửi UTC làm giờ nhận phòng lưu lệch -7 tiếng khi hiển thị lại
      .post('/api/stays/check-in', { reservationId: toCheckIn.reservationId, actualCheckIn: localNowIso() })
      .then(() => {
        toast.success(`Đã check-in phòng ${toCheckIn.roomNumber} cho ${toCheckIn.guestName}`)
        setToCheckIn(null)
        loadReservations()
        loadStays()
      })
      .catch((err) => setCheckInError(apiError(err)))
      .finally(() => setCheckingIn(false))
  }

  // surcharges: [{ surchargeItemId, quantity }] từ dialog - rỗng thì check-out như cũ
  const confirmCheckOut = (surcharges = []) => {
    setCheckOutError('')
    setCheckingOut(true)
    client
      .post(`/api/stays/${toCheckOut.stayId}/check-out`, surcharges.length ? { surcharges } : {})
      .then((res) => {
        toast.success(`Đã check-out phòng ${toCheckOut.roomNumber}`)
        setReceipt({ ...toCheckOut, ...res.data })
        setToCheckOut(null)
        loadStays()
      })
      .catch((err) => setCheckOutError(apiError(err)))
      .finally(() => setCheckingOut(false))
  }

  const usingMock = tab === 'checkin' ? resUsingMock : staysUsingMock
  const loadError = tab === 'checkin' ? resError : staysError
  const retry = tab === 'checkin' ? loadReservations : loadStays
  const listLoaded = tab === 'checkin' ? reservations !== null : stays !== null

  return (
    <div>
      <div className="flex flex-wrap items-end justify-between gap-4">
        <div>
          <p className="font-display text-[15px] italic capitalize text-brand-600">lễ tân · nhận và trả phòng</p>
          <h1 className="mt-1 font-display text-4xl font-semibold tracking-tight">Check-in / Check-out</h1>
          <p className="mt-1 text-sm text-ink-500">Nhận phòng cho khách đã xác nhận và trả phòng cho khách đang ở.</p>
        </div>
        <button
          onClick={() => navigate('/reservations/new')}
          className={`rounded-full bg-ink-900 px-5 py-2.5 text-[13px] font-bold text-cream-50 ${EASE} hover:bg-ink-700 active:scale-[0.98]`}
        >
          + Tạo đặt phòng
        </button>
      </div>

      {/* Segmented: Chờ nhận phòng / Đang ở */}
      <div className="mt-5 flex flex-wrap items-center gap-3">
        <div className="inline-flex items-center gap-0.5 rounded-full bg-white p-1 ring-1 ring-black/10 shadow-soft">
          <button
            onClick={() => setTab('checkin')}
            className={`flex items-center gap-1.5 rounded-full px-3.5 py-1.5 text-[12px] font-semibold ${EASE} active:scale-[0.97] ${
              tab === 'checkin' ? 'bg-ink-900 text-cream-50' : 'text-ink-500 hover:text-ink-900'
            }`}
          >
            <span className="h-1.5 w-1.5 rounded-full bg-sky-500" />
            Chờ nhận phòng
            <span className={`tabular-nums font-medium ${tab === 'checkin' ? 'text-cream-50/60' : 'text-ink-500/50'}`}>
              {(reservations ?? []).filter((r) => r.status === 'Confirmed').length}
            </span>
          </button>
          <button
            onClick={() => setTab('checkout')}
            className={`flex items-center gap-1.5 rounded-full px-3.5 py-1.5 text-[12px] font-semibold ${EASE} active:scale-[0.97] ${
              tab === 'checkout' ? 'bg-ink-900 text-cream-50' : 'text-ink-500 hover:text-ink-900'
            }`}
          >
            <span className="h-1.5 w-1.5 rounded-full bg-rose-500" />
            Đang ở
            <span className={`tabular-nums font-medium ${tab === 'checkout' ? 'text-cream-50/60' : 'text-ink-500/50'}`}>
              {(stays ?? []).length}
            </span>
          </button>
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

      {!listLoaded && !loadError && <SkeletonRows />}
      {loadError && <div className="mt-6"><ErrorState onRetry={retry} /></div>}

      {/* ===== Tab: Chờ nhận phòng ===== */}
      {!loadError && listLoaded && tab === 'checkin' && (
        pendingCheckIn.length === 0 ? (
          <EmptyState text="Không có đặt phòng nào đang chờ nhận phòng" />
        ) : (
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
                      <th className="px-5 py-3.5 text-right">Thao tác</th>
                    </tr>
                  </thead>
                  <tbody className="divide-y divide-black/[0.05]">
                    {pendingCheckIn.map((r) => (
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
                          {String(r.checkInDate).slice(0, 10)} → {String(r.checkOutDate).slice(0, 10)}
                        </td>
                        <td className="px-5 py-3.5 text-right">
                          <button
                            onClick={() => { setCheckInError(''); setToCheckIn(r) }}
                            className={`rounded-full bg-sky-600 px-4 py-1.5 text-[12px] font-bold text-white ${EASE} hover:bg-sky-700 active:scale-[0.97]`}
                          >
                            Check-in
                          </button>
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            </div>
          </div>
        )
      )}

      {/* ===== Tab: Đang ở ===== */}
      {!loadError && listLoaded && tab === 'checkout' && (
        activeStays.length === 0 ? (
          <EmptyState text="Không có khách nào đang ở" />
        ) : (
          <div className="card-rise mt-6 bezel-shell">
            <div className="bezel-core overflow-hidden">
              <div className="overflow-x-auto">
                <table className="w-full min-w-[720px] text-left">
                  <thead>
                    <tr className="border-b border-black/[0.06] text-[10px] font-bold uppercase tracking-[0.18em] text-ink-500">
                      <th className="px-5 py-3.5">Mã đặt phòng</th>
                      <th className="px-5 py-3.5">Khách</th>
                      <th className="px-5 py-3.5">Phòng</th>
                      <th className="px-5 py-3.5">Nhận phòng</th>
                      <th className="px-5 py-3.5">Dự kiến trả</th>
                      <th className="px-5 py-3.5 text-right">Thao tác</th>
                    </tr>
                  </thead>
                  <tbody className="divide-y divide-black/[0.05]">
                    {activeStays.map((s) => (
                      <tr key={s.stayId} className={`${EASE} hover:bg-cream-50/60`}>
                        <td className="px-5 py-3.5">
                          <span className="font-display text-base font-semibold tabular-nums">{s.bookingCode}</span>
                        </td>
                        <td className="px-5 py-3.5">
                          <p className="text-sm font-semibold">{s.guestName || '—'}</p>
                        </td>
                        <td className="px-5 py-3.5">
                          <p className="text-sm font-semibold tabular-nums">{s.roomNumber}</p>
                        </td>
                        <td className="px-5 py-3.5 text-sm tabular-nums text-ink-700">{fmtDateTime(s.actualCheckIn)}</td>
                        <td className="px-5 py-3.5 text-sm tabular-nums text-ink-700">{fmtDateTime(s.plannedCheckOut)}</td>
                        <td className="px-5 py-3.5 text-right">
                          <button
                            onClick={() => { setCheckOutError(''); setToCheckOut(s) }}
                            className={`rounded-full bg-rose-600 px-4 py-1.5 text-[12px] font-bold text-white ${EASE} hover:bg-rose-700 active:scale-[0.97]`}
                          >
                            Check-out
                          </button>
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            </div>
          </div>
        )
      )}

      <ConfirmDialog
        open={toCheckIn !== null}
        title={`Check-in phòng ${toCheckIn?.roomNumber ?? ''}?`}
        message={`Xác nhận ${toCheckIn?.guestName ?? 'khách'} nhận phòng ngay bây giờ. Phòng sẽ chuyển sang trạng thái Đang ở.`}
        confirmLabel="Check-in"
        busyLabel="Đang check-in…"
        tone="primary"
        busy={checkingIn}
        error={checkInError}
        onConfirm={confirmCheckIn}
        onCancel={() => setToCheckIn(null)}
      />

      <CheckoutDialog
        stay={toCheckOut}
        items={surchargeItems}
        busy={checkingOut}
        error={checkOutError}
        onConfirm={confirmCheckOut}
        onCancel={() => setToCheckOut(null)}
      />

      <ReceiptDialog
        receipt={receipt}
        onClose={() => setReceipt(null)}
        onPay={() => {
          const q = new URLSearchParams({ stayId: receipt.stayId, roomNumber: receipt.roomNumber, guestName: receipt.guestName })
          navigate(`/invoices?${q}`)
        }}
      />
    </div>
  )
}
