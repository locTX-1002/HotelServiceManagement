import { useEffect, useState } from 'react'
import guestClient, { apiError } from '../../api/guestClient'
import { formatVnd } from '../../utils/roomStatus'
import { normalizeReservationStatus } from '../../utils/apiShape'
import { getGuest } from '../../utils/guestSession'

// Nhãn + màu trạng thái đặt phòng - đúng theo dict RES_STATUS của ReservationsPage.jsx (trang nhân viên)
// để cùng 1 trạng thái luôn hiện cùng 1 màu/tên dù xem từ phía khách hay phía lễ tân.
const RES_STATUS = {
  Pending: { label: 'Chờ xác nhận', badge: 'bg-amber-50 text-amber-800 ring-1 ring-amber-600/15' },
  Confirmed: { label: 'Đã xác nhận', badge: 'bg-sky-50 text-sky-700 ring-1 ring-sky-600/15' },
  CheckedIn: { label: 'Đang lưu trú', badge: 'bg-emerald-50 text-emerald-700 ring-1 ring-emerald-600/15' },
  Completed: { label: 'Đã trả phòng', badge: 'bg-stone-100 text-stone-600 ring-1 ring-stone-500/15' },
  Cancelled: { label: 'Đã hủy', badge: 'bg-rose-50 text-rose-700 ring-1 ring-rose-600/15' },
  NoShow: { label: 'Không đến', badge: 'bg-orange-50 text-orange-700 ring-1 ring-orange-600/15' },
}

const formatDate = (d) => new Date(d).toLocaleDateString('vi-VN', { day: '2-digit', month: '2-digit', year: 'numeric' })

export default function GuestDashboardPage() {
  const guest = getGuest()
  const [reservations, setReservations] = useState([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')

  useEffect(() => {
    guestClient
      .get('/api/guest/me/reservations')
      .then((res) => setReservations(res.data ?? []))
      .catch((err) => setError(apiError(err)))
      .finally(() => setLoading(false))
  }, [])

  return (
    <div>
      <p className="font-display text-[13px] italic text-brand-600">xin chào</p>
      <h1 className="mt-1 font-display text-3xl font-semibold tracking-tight">{guest?.fullName ?? 'Khách lưu trú'}</h1>
      <p className="mt-2 text-sm text-ink-500">Danh sách đặt phòng của bạn tại khách sạn.</p>

      {loading && <p className="mt-8 text-sm text-ink-500">Đang tải…</p>}
      {error && (
        <p className="mt-8 rounded-lg bg-amber-50 px-3.5 py-2.5 text-[12px] font-medium text-amber-800 ring-1 ring-amber-600/15">
          {error}
        </p>
      )}

      {!loading && !error && reservations.length === 0 && (
        <p className="mt-8 text-sm text-ink-500">Chưa có đặt phòng nào gắn với tài khoản này.</p>
      )}

      <div className="mt-8 space-y-4">
        {reservations.map((r) => {
          const s = RES_STATUS[normalizeReservationStatus(r.status)] ?? RES_STATUS.Pending
          return (
            <div key={r.id} className="card-rise rounded-2xl bg-cream-50 p-5 ring-1 ring-black/[0.06]">
              <div className="flex flex-wrap items-center justify-between gap-2">
                <span className="font-display text-lg font-semibold tabular-nums">{r.bookingCode}</span>
                <span className={`rounded-full px-3 py-1 text-[11px] font-bold uppercase tracking-wide ${s.badge}`}>
                  {s.label}
                </span>
              </div>
              <div className="mt-3 grid grid-cols-2 gap-3 text-[13px] text-ink-700 sm:grid-cols-4">
                <div>
                  <p className="text-[10px] font-bold uppercase tracking-wider text-ink-500">Phòng</p>
                  <p className="mt-0.5 font-semibold">{r.roomNumber} · {r.roomTypeName}</p>
                </div>
                <div>
                  <p className="text-[10px] font-bold uppercase tracking-wider text-ink-500">Nhận phòng</p>
                  <p className="mt-0.5">{formatDate(r.checkInDate)}</p>
                </div>
                <div>
                  <p className="text-[10px] font-bold uppercase tracking-wider text-ink-500">Trả phòng</p>
                  <p className="mt-0.5">{formatDate(r.checkOutDate)}</p>
                </div>
                <div>
                  <p className="text-[10px] font-bold uppercase tracking-wider text-ink-500">Số khách</p>
                  <p className="mt-0.5">{r.numberOfGuests} người</p>
                </div>
              </div>
              {r.specialRequests && (
                <p className="mt-3 text-[12px] italic text-ink-500">Yêu cầu đặc biệt: {r.specialRequests}</p>
              )}
              {r.depositAmount != null && (
                <p className="mt-1 text-[12px] text-emerald-700">Đã đặt cọc: {formatVnd(r.depositAmount)}</p>
              )}
            </div>
          )
        })}
      </div>
    </div>
  )
}
