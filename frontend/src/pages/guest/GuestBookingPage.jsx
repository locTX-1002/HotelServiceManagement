import { useState } from 'react'
import { Link } from 'react-router-dom'
import guestClient, { apiError } from '../../api/guestClient'
import { formatVnd } from '../../utils/roomStatus'
import { EASE, errorCls } from '../../utils/ui'
import { roomImage } from '../../utils/roomImages'
import { roomMeta } from '../../utils/roomMeta'

const inputCls =
  'w-full rounded-lg border border-black/15 bg-white px-3.5 py-3 text-sm outline-none placeholder:text-ink-500/40 focus:border-brand-500 focus:ring-2 focus:ring-brand-500/20'
const labelCls = 'mb-1.5 block text-[11px] font-bold uppercase tracking-[0.18em] text-ink-700'

const todayIso = () => new Date().toISOString().slice(0, 10)

// Card kieu booking engine giong het RoomResultCard cua trang le tan (CreateReservationPage.jsx)
// - dung chung roomImage()/roomMeta() (tra theo ten loai phong, khong can du lieu anh tu backend).
// Khac RoomResultCard o cho day la THE LOAI PHONG (khong phai 1 phong cu the) nen khong co so
// phong/tang, thay bang badge "Còn X phòng".
function RoomTypeCard({ rt, idx, selected, onSelect }) {
  const meta = roomMeta(rt.roomTypeName)
  const [imgIdx, setImgIdx] = useState(idx)
  const active = selected?.roomTypeId === rt.roomTypeId
  return (
    <div
      className={`overflow-hidden rounded-2xl bg-white ring-1 ${EASE} ${
        active ? 'ring-2 ring-brand-600/50 shadow-lift' : 'ring-black/5 shadow-soft hover:ring-black/15'
      }`}
    >
      <div className="flex flex-col sm:flex-row">
        <div className="group relative h-48 shrink-0 p-3 sm:h-auto sm:w-56">
          <div className="h-full min-h-40 overflow-hidden rounded-t-[999px] rounded-b-xl">
            <img src={roomImage(rt.roomTypeName, imgIdx)} alt={rt.roomTypeName} className="h-full w-full object-cover" loading="lazy" />
          </div>
          <button
            type="button"
            onClick={() => setImgIdx((imgIdx + 3) % 4)}
            className="absolute left-2 top-1/2 flex h-7 w-7 -translate-y-1/2 items-center justify-center rounded-full bg-white/85 text-xs font-bold text-ink-700 opacity-0 backdrop-blur-sm group-hover:opacity-100"
            aria-label="Ảnh trước"
          >
            ‹
          </button>
          <button
            type="button"
            onClick={() => setImgIdx((imgIdx + 1) % 4)}
            className="absolute right-2 top-1/2 flex h-7 w-7 -translate-y-1/2 items-center justify-center rounded-full bg-white/85 text-xs font-bold text-ink-700 opacity-0 backdrop-blur-sm group-hover:opacity-100"
            aria-label="Ảnh sau"
          >
            ›
          </button>
        </div>
        <div className="flex flex-1 flex-col p-5">
          <div className="flex items-start justify-between gap-3">
            <div>
              <p className="font-display text-xl font-semibold">{rt.roomTypeName}</p>
              <p className="mt-2 text-[11px] uppercase tracking-[0.16em] text-ink-500">
                {meta.capacity} khách &nbsp;·&nbsp; {meta.area} m² &nbsp;·&nbsp; {meta.bed}
              </p>
            </div>
            <span className="shrink-0 rounded-full bg-emerald-50 px-2.5 py-1 text-[11px] font-bold text-emerald-700 ring-1 ring-emerald-600/15">
              Còn {rt.availableCount} phòng
            </span>
          </div>
          <div className="mt-3 flex flex-wrap gap-1.5">
            {meta.amenities.map((a) => (
              <span key={a} className="rounded-full bg-cream-100 px-2.5 py-1 text-[11px] font-medium text-ink-700">
                {a}
              </span>
            ))}
          </div>
          <div className="mt-auto flex items-end justify-between pt-4">
            <p className="font-display text-xl font-semibold tabular-nums">
              {formatVnd(rt.basePrice)}
              <span className="font-sans text-[11px] font-normal text-ink-500"> / đêm</span>
            </p>
            <button
              type="button"
              onClick={() => onSelect(active ? null : rt)}
              className={`rounded-full px-5 py-2 text-[13px] font-bold ${EASE} active:scale-[0.98] ${
                active ? 'bg-emerald-500 text-white' : 'bg-ink-900 text-cream-50 hover:bg-ink-700'
              }`}
            >
              {active ? '✓ Đã chọn' : 'Chọn phòng này'}
            </button>
          </div>
        </div>
      </div>
    </div>
  )
}

// Khach tu dat phong online - chon loai phong (khong chon so phong cu the, he thong tu gan luc
// xac nhan), luon o trang thai "Chờ xác nhận" - le tan phai duyet, khong tu dong xac nhan vi day
// la luong tu phuc vu chua co xac minh danh tinh (giong het tinh than thiet ke dang ky tu do).
export default function GuestBookingPage() {
  const [checkInDate, setCheckInDate] = useState('')
  const [checkOutDate, setCheckOutDate] = useState('')
  const [numberOfGuests, setNumberOfGuests] = useState(2)
  const [searching, setSearching] = useState(false)
  const [searchError, setSearchError] = useState('')
  const [roomTypes, setRoomTypes] = useState(null)

  const [bookingType, setBookingType] = useState(null)
  const [specialRequests, setSpecialRequests] = useState('')
  const [booking, setBooking] = useState(false)
  const [bookingError, setBookingError] = useState('')
  const [bookingSuccess, setBookingSuccess] = useState(null)

  const search = (e) => {
    e.preventDefault()
    if (searching) return
    setSearchError('')
    setBookingSuccess(null)
    if (!checkInDate || !checkOutDate) {
      setSearchError('Vui lòng chọn ngày nhận và trả phòng.')
      return
    }
    if (checkOutDate <= checkInDate) {
      setSearchError('Ngày trả phòng phải sau ngày nhận phòng.')
      return
    }
    setSearching(true)
    setRoomTypes(null)
    setBookingType(null)
    guestClient
      .get('/api/guest/available-room-types', {
        params: { checkInDate, checkOutDate, numberOfGuests },
      })
      .then((res) => setRoomTypes(res.data ?? []))
      .catch((err) => setSearchError(apiError(err)))
      .finally(() => setSearching(false))
  }

  const confirmBooking = () => {
    if (booking || !bookingType) return
    setBooking(true)
    setBookingError('')
    guestClient
      .post('/api/guest/reservations', {
        roomTypeId: bookingType.roomTypeId,
        numberOfGuests,
        checkInDate,
        checkOutDate,
        specialRequests: specialRequests.trim() || undefined,
      })
      .then((res) => {
        setBookingSuccess(res.data)
        setRoomTypes(null)
        setBookingType(null)
        setSpecialRequests('')
      })
      .catch((err) => {
        if (err.response?.status === 409) {
          setBookingError('Loại phòng này vừa hết trống cho khoảng ngày đã chọn — vui lòng tìm lại.')
        } else {
          setBookingError(apiError(err))
        }
      })
      .finally(() => setBooking(false))
  }

  return (
    <div>
      <p className="font-display text-[13px] italic text-brand-600">đặt phòng mới</p>
      <h1 className="mt-1 font-display text-3xl font-semibold tracking-tight">Tìm phòng trống</h1>
      <p className="mt-2 text-sm text-ink-500">
        Chọn ngày nhận/trả phòng — yêu cầu sẽ ở trạng thái "Chờ xác nhận" cho tới khi lễ tân duyệt.
      </p>

      {bookingSuccess && (
        <div className="mt-6 rounded-2xl bg-emerald-50 p-5 ring-1 ring-emerald-600/15">
          <p className="text-[13px] font-bold text-emerald-800">
            ✓ Đã gửi yêu cầu đặt phòng {bookingSuccess.bookingCode}
          </p>
          <p className="mt-1 text-[12px] text-emerald-700">
            Trạng thái: Chờ xác nhận — lễ tân sẽ xác nhận sớm nhất. Bạn có thể theo dõi trong{' '}
            <Link to="/guest/dashboard" className="font-semibold underline underline-offset-2">
              Đặt phòng của tôi
            </Link>
            .
          </p>
        </div>
      )}

      <form onSubmit={search} className="mt-8 max-w-lg space-y-4 rounded-2xl bg-cream-50 p-5 ring-1 ring-black/[0.06]">
        <div className="grid grid-cols-2 gap-3">
          <div>
            <label className={labelCls}>Nhận phòng</label>
            <input
              type="date"
              required
              min={todayIso()}
              className={inputCls}
              value={checkInDate}
              onChange={(e) => setCheckInDate(e.target.value)}
            />
          </div>
          <div>
            <label className={labelCls}>Trả phòng</label>
            <input
              type="date"
              required
              min={checkInDate || todayIso()}
              className={inputCls}
              value={checkOutDate}
              onChange={(e) => setCheckOutDate(e.target.value)}
            />
          </div>
        </div>
        <div>
          <label className={labelCls}>Số khách</label>
          <input
            type="number"
            required
            min={1}
            className={inputCls}
            value={numberOfGuests}
            onChange={(e) => setNumberOfGuests(Math.max(1, Number(e.target.value) || 1))}
          />
        </div>

        {searchError && <p className={errorCls}>{searchError}</p>}

        <button
          type="submit"
          disabled={searching}
          className={`rounded-full bg-brand-600 px-6 py-2.5 text-[12px] font-bold uppercase tracking-wider text-white ${EASE} hover:bg-brand-700 active:scale-[0.98] disabled:opacity-50`}
        >
          {searching ? 'Đang tìm…' : 'Tìm phòng trống'}
        </button>
      </form>

      {roomTypes && roomTypes.length === 0 && (
        <p className="mt-6 text-sm text-ink-500">Không còn loại phòng nào trống cho khoảng ngày này.</p>
      )}

      {roomTypes && roomTypes.length > 0 && (
        <div className="mt-6 max-w-2xl space-y-4">
          {roomTypes.map((rt, idx) => (
            <RoomTypeCard key={rt.roomTypeId} rt={rt} idx={idx} selected={bookingType} onSelect={setBookingType} />
          ))}
        </div>
      )}

      {bookingType && (
        <div className="mt-6 max-w-lg overflow-hidden rounded-2xl bg-cream-50 ring-1 ring-black/[0.06]">
          <img
            src={roomImage(bookingType.roomTypeName, 0)}
            alt=""
            className="h-32 w-full object-cover opacity-80"
          />
          <div className="p-5">
            <p className="font-display text-lg font-semibold">Xác nhận đặt phòng</p>
            <p className="mt-1 text-[12px] text-ink-500">
              {bookingType.roomTypeName} · {checkInDate} → {checkOutDate} · {numberOfGuests} khách ·{' '}
              {formatVnd(bookingType.basePrice)} / đêm
            </p>
            <label className={`${labelCls} mt-4`}>Yêu cầu đặc biệt (không bắt buộc)</label>
            <textarea
              rows={2}
              maxLength={500}
              className={inputCls}
              placeholder="VD: phòng tầng cao, không hút thuốc…"
              value={specialRequests}
              onChange={(e) => setSpecialRequests(e.target.value)}
            />

            {bookingError && <p className={`mt-3 ${errorCls}`}>{bookingError}</p>}

            <div className="mt-3 flex gap-2.5">
              <button
                type="button"
                onClick={() => setBookingType(null)}
                className={`rounded-full px-5 py-2.5 text-[12px] font-semibold text-ink-700 ring-1 ring-black/10 ${EASE} hover:bg-white`}
              >
                Huỷ
              </button>
              <button
                type="button"
                onClick={confirmBooking}
                disabled={booking}
                className={`flex-1 rounded-full bg-brand-600 px-6 py-2.5 text-[12px] font-bold uppercase tracking-wider text-white ${EASE} hover:bg-brand-700 disabled:opacity-50`}
              >
                {booking ? 'Đang gửi…' : 'Xác nhận đặt phòng'}
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  )
}
