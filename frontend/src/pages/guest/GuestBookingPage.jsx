import { useState } from 'react'
import { Link } from 'react-router-dom'
import guestClient, { apiError } from '../../api/guestClient'
import { formatVnd } from '../../utils/roomStatus'
import { EASE, errorCls } from '../../utils/ui'

const inputCls =
  'w-full rounded-lg border border-black/15 bg-white px-3.5 py-3 text-sm outline-none placeholder:text-ink-500/40 focus:border-brand-500 focus:ring-2 focus:ring-brand-500/20'
const labelCls = 'mb-1.5 block text-[11px] font-bold uppercase tracking-[0.18em] text-ink-700'

const todayIso = () => new Date().toISOString().slice(0, 10)

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
        <div className="mt-6 grid grid-cols-1 gap-4 sm:grid-cols-2">
          {roomTypes.map((rt) => (
            <div key={rt.roomTypeId} className="card-rise rounded-2xl bg-cream-50 p-5 ring-1 ring-black/[0.06]">
              <div className="flex items-center justify-between gap-2">
                <span className="font-display text-lg font-semibold">{rt.roomTypeName}</span>
                <span className="rounded-full bg-emerald-50 px-2.5 py-0.5 text-[11px] font-bold text-emerald-700 ring-1 ring-emerald-600/15">
                  Còn {rt.availableCount} phòng
                </span>
              </div>
              <p className="mt-1 text-[12px] text-ink-500">Sức chứa tối đa {rt.capacity} người</p>
              <p className="mt-2 font-display text-xl font-semibold text-brand-600">
                {formatVnd(rt.basePrice)} <span className="text-[11px] font-normal text-ink-500">/ đêm</span>
              </p>
              <button
                type="button"
                onClick={() => setBookingType(rt)}
                className={`mt-3 rounded-full px-4 py-2 text-[12px] font-bold uppercase tracking-wider ${EASE} ${
                  bookingType?.roomTypeId === rt.roomTypeId
                    ? 'bg-brand-600 text-white'
                    : 'bg-white text-ink-700 ring-1 ring-black/10 hover:bg-cream-50'
                }`}
              >
                {bookingType?.roomTypeId === rt.roomTypeId ? 'Đã chọn' : 'Đặt phòng này'}
              </button>
            </div>
          ))}
        </div>
      )}

      {bookingType && (
        <div className="mt-6 max-w-lg rounded-2xl bg-cream-50 p-5 ring-1 ring-black/[0.06]">
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
      )}
    </div>
  )
}
