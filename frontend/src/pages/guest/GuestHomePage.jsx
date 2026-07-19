import { useEffect, useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import guestClient, { apiError } from '../../api/guestClient'
import { normalizeReservationStatus } from '../../utils/apiShape'
import { getGuest } from '../../utils/guestSession'
import { EASE, inputCls, labelCls, openDatePicker } from '../../utils/ui'
import { roomImage } from '../../utils/roomImages'
import RoomServiceModal from '../../components/RoomServiceModal'
import { localToday as today, addDays } from '../../utils/dates'

const formatDate = (d) => new Date(d).toLocaleDateString('vi-VN', { day: '2-digit', month: '2-digit' })

// Đếm số đêm còn lại của kỳ lưu trú - cho khách biết còn ở bao lâu mà không phải tự trừ ngày
const nightsLeft = (checkOut) => {
  const ms = new Date(String(checkOut).slice(0, 10)).getTime() - new Date(today()).getTime()
  return Math.max(0, Math.round(ms / 86400000))
}

// Trang chủ của khách sau khi đăng nhập. Trước đây đăng nhập xong rơi thẳng vào danh sách lịch sử
// đặt phòng - không có chỗ nào để "làm gì đó". Trang này đưa 3 việc khách thực sự cần lên trước:
// đang ở thì gọi dịch vụ phòng, sắp tới thì xem lịch, còn lại thì tìm phòng mới.
export default function GuestHomePage() {
  const guest = getGuest()
  const navigate = useNavigate()
  const [reservations, setReservations] = useState([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')
  const [serviceOpen, setServiceOpen] = useState(false)

  const [checkIn, setCheckIn] = useState(today())
  const [checkOut, setCheckOut] = useState(addDays(today(), 1))
  const [guests, setGuests] = useState(2)
  const [voucher, setVoucher] = useState('')

  useEffect(() => {
    guestClient
      .get('/api/guest/me/reservations')
      .then((res) => setReservations(res.data ?? []))
      .catch((err) => setError(apiError(err)))
      .finally(() => setLoading(false))
  }, [])

  const byStatus = (st) => reservations.filter((r) => normalizeReservationStatus(r.status) === st)
  const staying = byStatus('CheckedIn')[0] // kỳ lưu trú đang diễn ra (nếu có)
  const upcoming = [...byStatus('Confirmed'), ...byStatus('Pending')]
    .sort((a, b) => String(a.checkInDate).localeCompare(String(b.checkInDate)))[0]

  const search = () => {
    const q = new URLSearchParams({ checkIn, checkOut, guests: String(guests) })
    if (voucher.trim()) q.set('voucher', voucher.trim())
    navigate(`/guest/dat-phong-moi?${q}`)
  }

  return (
    <div className="mx-auto max-w-3xl">
      {/* Hero chào khách */}
      <div className="relative h-48 overflow-hidden rounded-2xl sm:h-56">
        <img src="/img/v1.jpg" alt="" className="h-full w-full object-cover" />
        <div className="absolute inset-0 bg-gradient-to-r from-ink-900/75 to-ink-900/15" />
        <div className="absolute left-7 top-[42%] -translate-y-1/2 text-white">
          <p className="font-display text-[14px] italic text-white/80">xin chào</p>
          <h1 className="mt-1 font-display text-3xl font-semibold tracking-tight">{guest?.fullName ?? 'Khách lưu trú'}</h1>
          <p className="mt-1.5 text-[13px] text-white/70">
            {staying ? 'Chúc bạn có kỳ nghỉ thoải mái.' : 'Kỳ lưu trú tiếp theo của bạn bắt đầu từ đây.'}
          </p>
        </div>
      </div>

      {/* Thanh tìm phòng nổi đè lên hero */}
      <div className="relative z-10 mx-4 -mt-9 flex flex-wrap items-end gap-x-4 gap-y-3 rounded-2xl bg-white p-4 ring-1 ring-black/5 shadow-lift">
        <div className="min-w-32 flex-1">
          <label className={labelCls}>Nhận phòng</label>
          <input
            type="date" className={inputCls} value={checkIn} min={today()} onClick={openDatePicker}
            onChange={(e) => { setCheckIn(e.target.value); if (e.target.value >= checkOut) setCheckOut(addDays(e.target.value, 1)) }}
          />
        </div>
        <div className="min-w-32 flex-1">
          <label className={labelCls}>Trả phòng</label>
          <input
            type="date" className={inputCls} value={checkOut} min={addDays(checkIn, 1)} onClick={openDatePicker}
            onChange={(e) => setCheckOut(e.target.value)}
          />
        </div>
        <div>
          <label className={labelCls}>Khách</label>
          <div className="flex items-center gap-1 rounded-xl bg-white ring-1 ring-black/10">
            <button type="button" onClick={() => setGuests(Math.max(1, guests - 1))} className="px-2.5 py-2.5 text-sm font-bold text-ink-500 hover:text-ink-900">−</button>
            <span className="w-6 text-center text-sm font-bold tabular-nums">{guests}</span>
            <button type="button" onClick={() => setGuests(Math.min(8, guests + 1))} className="px-2.5 py-2.5 text-sm font-bold text-ink-500 hover:text-ink-900">+</button>
          </div>
        </div>
        <div className="min-w-28 flex-1">
          <label className={labelCls}>Mã khuyến mãi</label>
          <input className={inputCls} placeholder="Nhập nếu có" value={voucher} onChange={(e) => setVoucher(e.target.value)} />
        </div>
        <button
          type="button" onClick={search}
          className={`h-11 rounded-full bg-brand-600 px-6 text-sm font-bold text-white ${EASE} hover:bg-brand-700 active:scale-[0.98]`}
        >
          Tìm phòng
        </button>
      </div>

      {loading && <p className="mt-8 text-sm text-ink-500">Đang tải…</p>}
      {error && (
        <p className="mt-8 rounded-lg bg-amber-50 px-3.5 py-2.5 text-[12px] font-medium text-amber-800 ring-1 ring-amber-600/15">{error}</p>
      )}

      {/* Đang lưu trú: phòng + ô Dịch vụ phòng (mở bảng chọn) */}
      {!loading && staying && (
        <section className="mt-8">
          <p className="text-[11px] font-bold uppercase tracking-[0.18em] text-ink-500">Bạn đang ở</p>
          <div className="card-rise mt-3 overflow-hidden rounded-2xl bg-cream-50 ring-1 ring-black/[0.06]">
            <div className="flex flex-col sm:flex-row">
              {/* Ảnh khung cố định - không giãn theo chiều cao nội dung bên phải */}
              <div className="h-36 shrink-0 sm:h-auto sm:w-48">
                <img src={roomImage(staying.roomTypeName, 0)} alt={staying.roomTypeName} className="h-full w-full object-cover" />
              </div>
              <div className="flex-1 p-5">
                <p className="font-display text-2xl font-semibold">Phòng {staying.roomNumber}</p>
                <p className="mt-0.5 text-[13px] text-ink-500">
                  {staying.roomTypeName} · trả phòng {formatDate(staying.checkOutDate)}
                  {nightsLeft(staying.checkOutDate) > 0 && ` · còn ${nightsLeft(staying.checkOutDate)} đêm`}
                </p>

                <button
                  type="button"
                  onClick={() => setServiceOpen(true)}
                  className={`mt-4 flex w-full items-center gap-3.5 rounded-xl bg-white p-4 text-left ring-1 ring-black/[0.07] ${EASE} hover:ring-brand-600/40 active:scale-[0.99]`}
                >
                  <span className="flex h-11 w-9 shrink-0 items-end justify-center rounded-t-full rounded-b-md bg-brand-50 pb-1.5 text-base ring-1 ring-brand-600/15">
                    🛎
                  </span>
                  <span className="min-w-0 flex-1">
                    <span className="block font-display text-base font-semibold">Dịch vụ phòng</span>
                    <span className="block text-[12px] text-ink-500">Dọn phòng · Thêm khăn · Thêm nước · Gọi đồ ăn</span>
                  </span>
                  <span className="shrink-0 text-lg text-ink-500">›</span>
                </button>
              </div>
            </div>
          </div>
        </section>
      )}

      {/* Sắp tới: lịch gần nhất */}
      {!loading && upcoming && (
        <section className="mt-8">
          <p className="text-[11px] font-bold uppercase tracking-[0.18em] text-ink-500">Sắp tới</p>
          <Link
            to="/guest/dat-phong-cua-toi"
            className={`card-rise mt-3 flex items-center gap-4 rounded-2xl bg-cream-50 p-4 ring-1 ring-black/[0.06] ${EASE} hover:ring-brand-600/30`}
          >
            <div className="h-16 w-20 shrink-0 overflow-hidden rounded-xl">
              <img src={roomImage(upcoming.roomTypeName, 0)} alt="" className="h-full w-full object-cover" />
            </div>
            <div className="min-w-0 flex-1">
              <p className="font-display text-lg font-semibold">Phòng {upcoming.roomNumber} · {upcoming.roomTypeName}</p>
              <p className="mt-0.5 text-[12px] text-ink-500">
                {formatDate(upcoming.checkInDate)} → {formatDate(upcoming.checkOutDate)} · {upcoming.numberOfGuests} khách
                {normalizeReservationStatus(upcoming.status) === 'Pending' && ' · đang chờ lễ tân xác nhận'}
              </p>
            </div>
            <span className="shrink-0 text-lg text-ink-500">›</span>
          </Link>
        </section>
      )}

      {/* Lối tắt */}
      <section className="mt-8">
        <p className="text-[11px] font-bold uppercase tracking-[0.18em] text-ink-500">Lối tắt</p>
        <div className="mt-3 grid gap-3 sm:grid-cols-3">
          {[
            { to: '/guest/dat-phong-moi', title: 'Đặt phòng mới', desc: 'Chọn ngày và loại phòng' },
            { to: '/guest/dat-phong-cua-toi', title: 'Đặt phòng của tôi', desc: `${reservations.length} lượt đặt` },
            { to: '/guest/ho-so', title: 'Hồ sơ', desc: 'Thông tin & mật khẩu' },
          ].map((s) => (
            <Link
              key={s.to} to={s.to}
              className={`rounded-2xl bg-cream-50 p-4 ring-1 ring-black/[0.06] ${EASE} hover:ring-brand-600/30 hover:bg-white`}
            >
              <p className="font-display text-base font-semibold">{s.title}</p>
              <p className="mt-0.5 text-[12px] text-ink-500">{s.desc}</p>
            </Link>
          ))}
        </div>
      </section>

      {!loading && !error && reservations.length === 0 && (
        <p className="mt-8 text-sm text-ink-500">
          Chưa có đặt phòng nào gắn với tài khoản này — dùng thanh tìm phòng phía trên để đặt lượt đầu tiên.
        </p>
      )}

      {serviceOpen && staying && (
        <RoomServiceModal
          stay={{ roomNumber: staying.roomNumber, roomTypeName: staying.roomTypeName }}
          onClose={() => setServiceOpen(false)}
        />
      )}
    </div>
  )
}
