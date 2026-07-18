import { useEffect, useRef, useState } from 'react'
import client, { apiError } from '../api/client'
import { Link } from 'react-router-dom'
import { normalizeHousekeepingStatus, normalizeHousekeepingRequestType, normalizeReservationStatus } from '../utils/apiShape'

const REQUEST_TYPE_LABEL = {
  Cleaning: 'Dọn phòng',
  ExtraTowels: 'Thêm khăn',
  ExtraWater: 'Thêm nước',
  Other: 'Khác',
}
import { EASE } from '../utils/ui'

const POLL_INTERVAL_MS = 15000
// "Sắp trả phòng" tính từ khi còn <= 2 tiếng tới giờ trả dự kiến (gồm cả đã quá giờ mà chưa check-out).
const CHECKOUT_SOON_MS = 2 * 60 * 60 * 1000

// Chuông thông báo dùng chung 2 việc cho Admin/Manager/Receptionist: yêu cầu dọn phòng từ khách, và
// cảnh báo sắp đến giờ trả phòng - đúng yêu cầu gốc "chuông thực hiện được nhiều chức năng". Cảnh báo
// trả phòng KHÔNG cần entity/API riêng - tính thẳng từ GET /api/stays/active đã có sẵn plannedCheckOut,
// tránh phình thêm hạ tầng cho 1 thứ chỉ cần đọc, không cần lưu trạng thái.
export default function NotificationBell() {
  const [requests, setRequests] = useState([])
  const [checkoutSoon, setCheckoutSoon] = useState([])
  // Dat phong khach tu tao dang cho le tan duyet - phai co cham do + muc rieng trong chuong,
  // khong thi don online nam im trong trang Dat phong ma khong ai biet de xac nhan.
  const [pendingBookings, setPendingBookings] = useState([])
  const [open, setOpen] = useState(false)
  const [error, setError] = useState('')
  const busyRef = useRef({})
  const containerRef = useRef(null)

  const fetchAll = () => {
    Promise.all([
      client.get('/api/housekeeping-requests'),
      client.get('/api/stays/active'),
      client.get('/api/reservations'),
    ])
      .then(([hkRes, staysRes, resvRes]) => {
        setRequests(hkRes.data ?? [])
        const now = Date.now()
        const soon = (staysRes.data ?? []).filter(
          (s) => new Date(s.plannedCheckOut).getTime() - now <= CHECKOUT_SOON_MS,
        )
        setCheckoutSoon(soon)
        setPendingBookings((resvRes.data ?? []).filter((r) => normalizeReservationStatus(r.status) === 'Pending'))
        setError('')
      })
      .catch((err) => setError(apiError(err)))
  }

  useEffect(() => {
    fetchAll()
    const timer = setInterval(fetchAll, POLL_INTERVAL_MS)
    return () => clearInterval(timer)
  }, [])

  // Bấm ra ngoài thì đóng dropdown
  useEffect(() => {
    if (!open) return
    const onClickOutside = (e) => {
      if (containerRef.current && !containerRef.current.contains(e.target)) setOpen(false)
    }
    document.addEventListener('mousedown', onClickOutside)
    return () => document.removeEventListener('mousedown', onClickOutside)
  }, [open])

  const act = (id, action) => {
    if (busyRef.current[id]) return
    busyRef.current[id] = true
    client
      .patch(`/api/housekeeping-requests/${id}/${action}`)
      .then(() => fetchAll())
      .catch((err) => setError(apiError(err)))
      .finally(() => {
        busyRef.current[id] = false
      })
  }

  const formatTime = (d) => new Date(d).toLocaleTimeString('vi-VN', { hour: '2-digit', minute: '2-digit' })
  const pendingCount = requests.filter((r) => normalizeHousekeepingStatus(r.status) === 'Pending').length
  const badgeCount = pendingCount + checkoutSoon.length + pendingBookings.length

  return (
    <div className="relative" ref={containerRef}>
      <button
        type="button"
        onClick={() => setOpen((v) => !v)}
        aria-label="Thông báo"
        title="Thông báo"
        className={`relative flex h-9 w-9 items-center justify-center rounded-full text-ink-500 ring-1 ring-black/10 ${EASE} hover:bg-white hover:text-ink-900`}
      >
        <svg width="15" height="15" viewBox="0 0 16 16" fill="none" aria-hidden>
          <path
            d="M8 1.5C5.8 1.5 4.2 3.3 4.2 5.5c0 3-1.5 4-1.5 5h10.6c0-1-1.5-2-1.5-5 0-2.2-1.6-4-3.8-4z"
            stroke="currentColor" strokeWidth="1.3" strokeLinecap="round" strokeLinejoin="round"
          />
          <path d="M6.5 13a1.6 1.6 0 003 0" stroke="currentColor" strokeWidth="1.3" strokeLinecap="round" />
        </svg>
        {badgeCount > 0 && (
          <span className="absolute -right-0.5 -top-0.5 flex h-4 min-w-4 items-center justify-center rounded-full bg-rose-600 px-1 text-[9px] font-bold text-white">
            {badgeCount}
          </span>
        )}
      </button>

      {open && (
        // Dinh vi kieu fixed + inset-x tren mobile (khong phu thuoc vi tri nut chuong trong header,
        // luon nam gon trong man hinh) - tu sm tro len quay lai kieu absolute neo theo nut nhu cu,
        // vi man hinh du rong de w-80 khong bao gio tran.
        <div className="card-rise fixed inset-x-4 top-16 z-30 rounded-2xl bg-cream-50 p-3 shadow-lift ring-1 ring-black/[0.06] sm:absolute sm:inset-x-auto sm:right-0 sm:top-11 sm:w-80">
          {error && <p className="px-2 py-1 text-[12px] text-amber-800">{error}</p>}

          <p className="px-2 py-1 text-[11px] font-bold uppercase tracking-wider text-ink-500">Chờ xác nhận đặt phòng</p>
          {pendingBookings.length === 0 ? (
            <p className="px-2 pb-2 text-[12px] text-ink-500">Không có đặt phòng nào chờ duyệt.</p>
          ) : (
            <div className="mb-2 space-y-1.5">
              {pendingBookings.slice(0, 4).map((r) => (
                <div key={r.id} className="rounded-xl bg-white p-3 ring-1 ring-black/[0.05]">
                  <div className="flex items-center justify-between gap-2">
                    <span className="text-[13px] font-bold">Phòng {r.roomNumber}</span>
                    <span className="rounded-full bg-rose-50 px-2 py-0.5 text-[10px] font-bold uppercase tracking-wide text-rose-700 ring-1 ring-rose-600/15">
                      Chờ duyệt
                    </span>
                  </div>
                  <p className="mt-0.5 text-[12px] text-ink-500">{r.guestName} · {r.bookingCode}</p>
                </div>
              ))}
              <Link
                to="/reservations"
                onClick={() => setOpen(false)}
                className="block px-2 pb-1 text-[12px] font-semibold text-brand-600 underline-offset-2 hover:underline"
              >
                Mở trang Đặt phòng để xác nhận →
              </Link>
            </div>
          )}

          <p className="px-2 py-1 text-[11px] font-bold uppercase tracking-wider text-ink-500">Sắp trả phòng</p>
          {checkoutSoon.length === 0 ? (
            <p className="px-2 pb-2 text-[12px] text-ink-500">Chưa có phòng nào sắp tới giờ trả.</p>
          ) : (
            <div className="mb-2 space-y-1.5">
              {checkoutSoon.map((s) => {
                const overdue = new Date(s.plannedCheckOut).getTime() < Date.now()
                return (
                  <div key={s.stayId} className="rounded-xl bg-white p-3 ring-1 ring-black/[0.05]">
                    <div className="flex items-center justify-between gap-2">
                      <span className="text-[13px] font-bold">Phòng {s.roomNumber}</span>
                      <span
                        className={`rounded-full px-2 py-0.5 text-[10px] font-bold uppercase tracking-wide ${
                          overdue
                            ? 'bg-rose-50 text-rose-700 ring-1 ring-rose-600/15'
                            : 'bg-amber-50 text-amber-800 ring-1 ring-amber-600/15'
                        }`}
                      >
                        {overdue ? 'Quá giờ' : formatTime(s.plannedCheckOut)}
                      </span>
                    </div>
                    <p className="mt-0.5 text-[12px] text-ink-500">{s.guestName} · {s.bookingCode}</p>
                  </div>
                )
              })}
            </div>
          )}

          <p className="px-2 py-1 text-[11px] font-bold uppercase tracking-wider text-ink-500">Yêu cầu dọn phòng</p>
          {requests.length === 0 && (
            <p className="px-2 py-3 text-[12px] text-ink-500">Không có yêu cầu nào đang chờ.</p>
          )}
          <div className="max-h-60 space-y-1.5 overflow-y-auto">
            {requests.map((r) => {
              const status = normalizeHousekeepingStatus(r.status)
              return (
                <div key={r.id} className="rounded-xl bg-white p-3 ring-1 ring-black/[0.05]">
                  <div className="flex items-center justify-between gap-2">
                    <span className="text-[13px] font-bold">Phòng {r.roomNumber}</span>
                    <span
                      className={`rounded-full px-2 py-0.5 text-[10px] font-bold uppercase tracking-wide ${
                        status === 'Pending'
                          ? 'bg-rose-50 text-rose-700 ring-1 ring-rose-600/15'
                          : 'bg-sky-50 text-sky-700 ring-1 ring-sky-600/15'
                      }`}
                    >
                      {status === 'Pending' ? 'Mới' : 'Đang xử lý'}
                    </span>
                  </div>
                  <p className="mt-0.5 text-[12px] text-ink-500">
                    {r.guestName} · {REQUEST_TYPE_LABEL[normalizeHousekeepingRequestType(r.requestType)] ?? 'Khác'}
                  </p>
                  {r.note && <p className="mt-1 text-[12px] italic text-ink-700">"{r.note}"</p>}
                  <div className="mt-2 flex justify-end gap-1.5">
                    {status === 'Pending' && (
                      <button
                        type="button"
                        onClick={() => act(r.id, 'acknowledge')}
                        className={`rounded-full px-3 py-1 text-[11px] font-bold text-ink-700 ring-1 ring-black/10 ${EASE} hover:bg-cream-50`}
                      >
                        Xác nhận
                      </button>
                    )}
                    <button
                      type="button"
                      onClick={() => act(r.id, 'complete')}
                      className={`rounded-full bg-brand-600 px-3 py-1 text-[11px] font-bold text-white ${EASE} hover:bg-brand-700`}
                    >
                      Hoàn tất
                    </button>
                  </div>
                </div>
              )
            })}
          </div>
        </div>
      )}
    </div>
  )
}
