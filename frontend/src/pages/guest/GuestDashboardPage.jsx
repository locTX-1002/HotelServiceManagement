import { useEffect, useRef, useState } from 'react'
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
  // Ghi lai id da goi don phong thanh cong trong phien nay - hien "Đã gửi yêu cầu" thay vi cho bam lai.
  const [housekeepingSent, setHousekeepingSent] = useState({})
  const [housekeepingError, setHousekeepingError] = useState({})
  // Ref dong bo (khac state cap nhat bat dong bo) chan spam-click, dung pattern da dung o cac trang nhan vien.
  const housekeepingBusyRef = useRef({})

  useEffect(() => {
    guestClient
      .get('/api/guest/me/reservations')
      .then((res) => setReservations(res.data ?? []))
      .catch((err) => setError(apiError(err)))
      .finally(() => setLoading(false))
  }, [])

  const requestHousekeeping = (reservationId) => {
    if (housekeepingBusyRef.current[reservationId]) return
    housekeepingBusyRef.current[reservationId] = true
    setHousekeepingError((prev) => ({ ...prev, [reservationId]: '' }))
    guestClient
      .post('/api/guest/me/housekeeping-requests', {})
      .then(() => setHousekeepingSent((prev) => ({ ...prev, [reservationId]: true })))
      .catch((err) => setHousekeepingError((prev) => ({ ...prev, [reservationId]: apiError(err) })))
      .finally(() => {
        housekeepingBusyRef.current[reservationId] = false
      })
  }

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

              {normalizeReservationStatus(r.status) === 'CheckedIn' && (
                <div className="mt-4 border-t border-black/[0.06] pt-4">
                  {housekeepingSent[r.id] ? (
                    <p className="text-[12px] font-semibold text-emerald-700">
                      ✓ Đã gửi yêu cầu dọn phòng — lễ tân sẽ xử lý sớm nhất.
                    </p>
                  ) : (
                    <>
                      <button
                        type="button"
                        onClick={() => requestHousekeeping(r.id)}
                        className="rounded-full bg-brand-600 px-4 py-2 text-[12px] font-bold uppercase tracking-wider text-white transition-all duration-300 ease-[cubic-bezier(0.32,0.72,0,1)] hover:bg-brand-700 active:scale-[0.98]"
                      >
                        Gọi dọn phòng
                      </button>
                      {housekeepingError[r.id] && (
                        <p className="mt-2 text-[12px] font-medium text-amber-800">{housekeepingError[r.id]}</p>
                      )}
                    </>
                  )}
                </div>
              )}
            </div>
          )
        })}
      </div>
    </div>
  )
}
