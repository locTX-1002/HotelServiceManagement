import { useEffect, useState } from 'react'
import { Link, useSearchParams } from 'react-router-dom'
import guestClient, { apiError } from '../../api/guestClient'
import { formatVnd } from '../../utils/roomStatus'
import { EASE, errorCls, inputCls, labelCls, openDatePicker } from '../../utils/ui'
import { roomImage } from '../../utils/roomImages'
import { roomMeta } from '../../utils/roomMeta'
import { localToday as today, addDays, fmtShort } from '../../utils/dates'

// Rap khuon dung 3-buoc cua trang dat phong le tan (CreateReservationPage.jsx) de dong bo giao
// dien toan he thong - chi bo phan rieng cua le tan (nhap thong tin khach, CCCD, walk-in, coc):
// khach da dang nhap san nen biet la ai, khong thu coc online (chua co cong thanh toan that), va
// KHONG dung du lieu mau khi mat ket noi (mock lam khach tuong dat duoc phong ao).
function Steps({ current, onBack }) {
  const items = ['Ngày ở & khách', 'Chọn loại phòng', 'Xác nhận']
  return (
    <div className="flex items-center justify-center gap-3">
      {items.map((label, i) => {
        const n = i + 1
        const state = n < current ? 'done' : n === current ? 'active' : 'todo'
        const Tag = state === 'done' ? 'button' : 'div'
        return (
          <div key={label} className="flex items-center gap-3">
            <Tag
              onClick={state === 'done' ? () => onBack(n) : undefined}
              className={`flex items-center gap-2 ${state === 'done' ? 'cursor-pointer hover:opacity-70' : ''} ${EASE}`}
            >
              <span
                className={`flex h-6 w-6 items-center justify-center rounded-full text-[11px] font-bold ${EASE} ${
                  state === 'done' ? 'bg-emerald-500 text-white' : state === 'active' ? 'bg-ink-900 text-cream-50' : 'bg-black/[0.07] text-ink-500'
                }`}
              >
                {state === 'done' ? '✓' : n}
              </span>
              <span className={`text-[13px] ${state === 'active' ? 'font-bold' : 'font-medium text-ink-500'}`}>{label}</span>
            </Tag>
            {n < 3 && <span className={`h-px w-10 sm:w-16 ${EASE} ${n < current ? 'bg-emerald-500/50' : 'bg-black/10'}`} />}
          </div>
        )
      })}
    </div>
  )
}

// Card kieu booking engine giong het RoomResultCard cua trang le tan - dung chung
// roomImage()/roomMeta() (tra theo ten loai phong, khong can du lieu anh tu backend). Khac
// RoomResultCard o cho day la THE LOAI PHONG (khong phai 1 phong cu the) nen khong co so
// phong/tang, thay bang badge "Còn X phòng".
// Modal chi tiet loai phong - anh lon + mo ta (tu backend) + tien nghi, theo dung pattern modal
// cua RoomTypeSelect ben HomePage (backdrop toi, Escape dong, panel cream cuon doc).
function RoomTypeDetailModal({ rt, selected, onSelect, onClose }) {
  const meta = roomMeta(rt.roomTypeName)
  const [imgIdx, setImgIdx] = useState(0)
  const active = selected?.roomTypeId === rt.roomTypeId

  useEffect(() => {
    const onKey = (e) => e.key === 'Escape' && onClose()
    window.addEventListener('keydown', onKey)
    return () => window.removeEventListener('keydown', onKey)
  }, [onClose])

  return (
    <div className="fixed inset-0 z-50 flex items-end justify-center px-4 pb-4 sm:items-center">
      <div onClick={onClose} className="absolute inset-0 bg-ink-900/40" />
      <div className="card-rise relative max-h-[85vh] w-full max-w-lg overflow-y-auto rounded-2xl bg-cream-50 shadow-lift">
        <div className="group relative">
          <img src={roomImage(rt.roomTypeName, imgIdx)} alt={rt.roomTypeName} className="h-52 w-full object-cover sm:h-60" />
          <button
            type="button"
            onClick={() => setImgIdx((imgIdx + 3) % 4)}
            className="absolute left-3 top-1/2 flex h-8 w-8 -translate-y-1/2 items-center justify-center rounded-full bg-white/85 text-sm font-bold text-ink-700 backdrop-blur-sm"
            aria-label="Ảnh trước"
          >
            ‹
          </button>
          <button
            type="button"
            onClick={() => setImgIdx((imgIdx + 1) % 4)}
            className="absolute right-3 top-1/2 flex h-8 w-8 -translate-y-1/2 items-center justify-center rounded-full bg-white/85 text-sm font-bold text-ink-700 backdrop-blur-sm"
            aria-label="Ảnh sau"
          >
            ›
          </button>
          {/* Bo dem goc anh kieu booking engine ("1/4") thay cho cham tron */}
          <span className="absolute bottom-2.5 left-3 rounded-full bg-ink-900/70 px-2.5 py-0.5 text-[11px] font-semibold text-white">
            {imgIdx + 1}/4
          </span>
          <button
            type="button"
            onClick={onClose}
            className="absolute right-3 top-3 flex h-8 w-8 items-center justify-center rounded-full bg-white/85 text-sm font-bold text-ink-700 backdrop-blur-sm"
            aria-label="Đóng"
          >
            ✕
          </button>
        </div>

        {/* Dai thumbnail doi goc nhin - bam truc tiep tung goc phong thay vi chi bam ‹ › mo */}
        <div className="flex gap-2 px-6 pt-4">
          {[0, 1, 2, 3].map((i) => (
            <button
              key={i}
              type="button"
              onClick={() => setImgIdx(i)}
              className={`h-14 w-20 shrink-0 overflow-hidden rounded-lg ring-2 ${EASE} ${
                i === imgIdx ? 'ring-brand-600' : 'ring-transparent opacity-60 hover:opacity-100'
              }`}
              aria-label={`Góc ảnh ${i + 1}`}
            >
              <img src={roomImage(rt.roomTypeName, i)} alt="" className="h-full w-full object-cover" />
            </button>
          ))}
        </div>

        <div className="p-6 pt-4">
          <div className="flex items-start justify-between gap-3">
            <p className="font-display text-2xl font-semibold">{rt.roomTypeName}</p>
            <span className="shrink-0 rounded-full bg-emerald-50 px-2.5 py-1 text-[11px] font-bold text-emerald-700 ring-1 ring-emerald-600/15">
              Còn {rt.availableCount} phòng
            </span>
          </div>
          <p className="mt-1.5 text-[11px] uppercase tracking-[0.16em] text-ink-500">
            {meta.capacity} khách &nbsp;·&nbsp; {meta.area} m² &nbsp;·&nbsp; {meta.bed}
          </p>

          <p className="mt-3.5 text-sm leading-relaxed text-ink-700">{rt.description || meta.desc}</p>

          <p className="mt-4 text-[11px] font-bold uppercase tracking-[0.18em] text-ink-500">Tiện nghi</p>
          <div className="mt-2 grid grid-cols-2 gap-x-4 gap-y-1.5">
            {meta.amenities.map((a) => (
              <p key={a} className="flex items-center gap-2 text-[12.5px] text-ink-700">
                <span className="font-bold text-emerald-600">✓</span>
                {a}
              </p>
            ))}
          </div>

          <div className="mt-5 flex items-end justify-between border-t border-black/[0.06] pt-4">
            <p className="font-display text-xl font-semibold tabular-nums">
              {formatVnd(rt.basePrice)}
              <span className="font-sans text-[11px] font-normal text-ink-500"> / đêm</span>
            </p>
            <button
              type="button"
              onClick={() => {
                onSelect(active ? null : rt)
                onClose()
              }}
              className={`rounded-full px-5 py-2 text-[13px] font-bold ${EASE} active:scale-[0.98] ${
                active ? 'bg-emerald-500 text-white' : 'bg-ink-900 text-cream-50 hover:bg-ink-700'
              }`}
            >
              {active ? '✓ Đã chọn' : 'Chọn loại phòng này'}
            </button>
          </div>
        </div>
      </div>
    </div>
  )
}

function RoomTypeCard({ rt, idx, selected, onSelect, onDetail }) {
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
          {/* Re chuot vao anh -> hien mo ta so cua loai phong (khong can mo modal moi biet) */}
          <div
            className={`pointer-events-none absolute inset-x-3 bottom-3 rounded-xl bg-ink-900/75 p-3 text-[12px] leading-relaxed text-white opacity-0 backdrop-blur-sm ${EASE} group-hover:opacity-100`}
          >
            {rt.description || meta.desc}
          </div>
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
            <div className="flex items-center gap-4">
              <button
                type="button"
                onClick={() => onDetail(rt)}
                className="text-[12px] font-semibold text-brand-600 underline-offset-4 hover:underline"
              >
                Xem chi tiết
              </button>
              <button
                type="button"
                onClick={() => onSelect(active ? null : rt)}
                className={`rounded-full px-5 py-2 text-[13px] font-bold ${EASE} active:scale-[0.98] ${
                  active ? 'bg-emerald-500 text-white' : 'bg-ink-900 text-cream-50 hover:bg-ink-700'
                }`}
              >
                {active ? '✓ Đã chọn' : 'Chọn loại phòng này'}
              </button>
            </div>
          </div>
        </div>
      </div>
    </div>
  )
}

export default function GuestBookingPage() {
  const [step, setStep] = useState(1)
  const [checkInDate, setCheckInDate] = useState(today())
  const [checkOutDate, setCheckOutDate] = useState(addDays(today(), 1))
  const [numberOfGuests, setNumberOfGuests] = useState(2)
  const [searching, setSearching] = useState(false)
  const [searchError, setSearchError] = useState('')
  const [roomTypes, setRoomTypes] = useState([])

  const [bookingType, setBookingType] = useState(null)
  const [detailType, setDetailType] = useState(null)
  const [specialRequests, setSpecialRequests] = useState('')
  // Voucher luon co san tren khung tim kiem (kieu booking engine) - he thong chi ap ma vao hoa don
  // luc thanh toan (nghiep vu san co phia le tan), nen ma khach nhap o day duoc ghi vao yeu cau
  // dat phong de le tan ap dung khi thu tien, khong tru tien truc tiep luc dat.
  const [voucher, setVoucher] = useState('')
  const [booking, setBooking] = useState(false)
  const [bookingError, setBookingError] = useState('')
  const [bookingSuccess, setBookingSuccess] = useState(null)

  const [searchParams] = useSearchParams()

  const nights = Math.max(Math.round((new Date(checkOutDate) - new Date(checkInDate)) / 86400000), 0)

  const search = (ci = checkInDate, co = checkOutDate, g = numberOfGuests) => {
    setSearchError('')
    setSearching(true)
    setBookingType(null)
    guestClient
      .get('/api/guest/available-room-types', { params: { checkInDate: ci, checkOutDate: co, numberOfGuests: g } })
      .then((res) => setRoomTypes(res.data ?? []))
      .catch((err) => {
        setSearchError(apiError(err))
        setRoomTypes([])
      })
      .finally(() => setSearching(false))
    setStep(2)
  }

  // Den tu trang chu ("Đặt phòng →"/thanh tim kiem cong khai): nhan ngay + so khach qua query,
  // dien san va tim luon - khach khong phai nhap lai sau khi qua man dang nhap.
  useEffect(() => {
    const ci = searchParams.get('checkIn')
    if (!ci) return
    const co = searchParams.get('checkOut') ?? addDays(ci, 1)
    const g = Math.max(1, Number(searchParams.get('guests')) || 2)
    setCheckInDate(ci)
    setCheckOutDate(co)
    setNumberOfGuests(g)
    search(ci, co, g)
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [])

  const confirmBooking = () => {
    if (booking || !bookingType) return
    setBooking(true)
    setBookingError('')
    const trimmedVoucher = voucher.trim().toUpperCase()
    const note = [specialRequests.trim(), trimmedVoucher ? `[Mã khuyến mãi: ${trimmedVoucher}]` : '']
      .filter(Boolean)
      .join(' — ')
    guestClient
      .post('/api/guest/reservations', {
        roomTypeId: bookingType.roomTypeId,
        numberOfGuests,
        checkInDate,
        checkOutDate,
        specialRequests: note || undefined,
      })
      .then((res) => setBookingSuccess(res.data))
      .catch((err) => {
        if (err.response?.status === 409) {
          setBookingError('Loại phòng này vừa hết trống cho khoảng ngày đã chọn — vui lòng quay lại tìm.')
        } else {
          setBookingError(apiError(err))
        }
      })
      .finally(() => setBooking(false))
  }

  // Man hinh sau khi gui - kieu "boarding pass" giong man thanh cong cua le tan, nhung nhan luon
  // "Chờ xác nhận" vi day la tu phuc vu, chua duoc le tan duyet.
  if (bookingSuccess) {
    return (
      <div className="mx-auto max-w-lg pt-10 text-center">
        <p className="font-display text-[15px] italic text-brand-600">đã gửi yêu cầu đặt phòng</p>
        <div className="mt-4 rounded-2xl bg-white p-8 ring-1 ring-black/5 shadow-lift">
          <p className="text-[11px] font-bold uppercase tracking-[0.25em] text-ink-500">Mã đặt phòng</p>
          <p className="mt-2 font-display text-5xl font-semibold tracking-tight text-brand-600">{bookingSuccess.bookingCode}</p>
          <span className="mt-3 inline-block rounded-full bg-amber-50 px-3 py-1 text-[11px] font-bold uppercase tracking-wide text-amber-800 ring-1 ring-amber-600/15">
            Chờ xác nhận
          </span>
          <div className="my-5 border-t border-dashed border-black/10" />
          <p className="text-sm text-ink-700">{bookingSuccess.roomTypeName}</p>
          <p className="mt-1 text-sm text-ink-500">
            {fmtShort(checkInDate)} → {fmtShort(checkOutDate)} · {nights} đêm
          </p>
          <p className="mt-3 text-[12px] text-ink-500">
            Lễ tân sẽ xác nhận sớm nhất — theo dõi trong{' '}
            <Link to="/guest/dashboard" className="font-semibold text-brand-600 underline underline-offset-2">
              Đặt phòng của tôi
            </Link>
            .
          </p>
        </div>
        <button
          type="button"
          onClick={() => window.location.reload()}
          className={`mt-6 rounded-full bg-ink-900 px-6 py-2.5 text-sm font-bold text-cream-50 ${EASE} hover:bg-ink-700`}
        >
          Đặt phòng khác
        </button>
      </div>
    )
  }

  return (
    <div className="pb-24">
      <Steps current={step} onBack={setStep} />

      {step === 1 && (
        <div className="mt-6">
          {/* Hero chu giua + hoa van gach ngang theo mau template "Unique Experience / — ENJOY WITH US —" */}
          <div className="relative h-56 overflow-hidden rounded-2xl">
            <img src="/img/v2.jpg" alt="" className="h-full w-full object-cover" />
            <div className="absolute inset-0 bg-ink-900/50" />
            <div className="absolute inset-0 flex flex-col items-center justify-center px-6 text-center text-white">
              <p className="font-display text-[15px] italic text-white/80">cổng khách · đặt phòng mới</p>
              <p className="mt-2 font-display text-4xl font-medium">Kỳ lưu trú tiếp theo</p>
              <div className="mt-3.5 flex items-center gap-3 text-[11px] font-bold uppercase tracking-[0.3em] text-white/80">
                <span className="h-px w-10 bg-white/40" />
                Trải nghiệm cùng HSMS
                <span className="h-px w-10 bg-white/40" />
              </div>
            </div>
          </div>

          <div className="relative z-10 mx-4 -mt-8 flex flex-wrap items-end gap-x-5 gap-y-4 rounded-2xl bg-white p-5 ring-1 ring-black/5 shadow-lift">
            <div className="min-w-36 flex-1">
              <label className={labelCls}>Nhận phòng</label>
              <input
                type="date"
                className={inputCls}
                value={checkInDate}
                min={today()}
                onClick={openDatePicker}
                onChange={(e) => {
                  setCheckInDate(e.target.value)
                  if (e.target.value >= checkOutDate) setCheckOutDate(addDays(e.target.value, 1))
                }}
              />
            </div>
            <div className="min-w-36 flex-1">
              <label className={labelCls}>Trả phòng</label>
              <input
                type="date"
                className={inputCls}
                value={checkOutDate}
                min={addDays(checkInDate, 1)}
                onClick={openDatePicker}
                onChange={(e) => setCheckOutDate(e.target.value)}
              />
            </div>
            <div>
              <label className={labelCls}>Số khách</label>
              <div className="flex items-center gap-1 rounded-xl bg-white ring-1 ring-black/10">
                <button
                  type="button"
                  onClick={() => setNumberOfGuests(Math.max(1, numberOfGuests - 1))}
                  className="px-3 py-2.5 text-sm font-bold text-ink-500 hover:text-ink-900"
                >
                  −
                </button>
                <span className="w-8 text-center text-sm font-bold tabular-nums">{numberOfGuests}</span>
                <button
                  type="button"
                  onClick={() => setNumberOfGuests(Math.min(8, numberOfGuests + 1))}
                  className="px-3 py-2.5 text-sm font-bold text-ink-500 hover:text-ink-900"
                >
                  +
                </button>
              </div>
            </div>
            <div className="min-w-36 flex-1">
              <label className={labelCls}>Mã khuyến mãi</label>
              <input
                className={inputCls}
                placeholder="Nhập nếu có"
                value={voucher}
                onChange={(e) => setVoucher(e.target.value)}
              />
            </div>
            <button
              type="button"
              onClick={() => search()}
              disabled={nights <= 0}
              className={`h-11 rounded-full bg-brand-600 px-7 text-sm font-bold text-white ${EASE} hover:bg-brand-700 active:scale-[0.98] disabled:opacity-40`}
            >
              Tìm phòng trống
            </button>

            <div className="flex w-full flex-wrap items-center gap-2 border-t border-black/[0.06] pt-3.5">
              <span className="text-[10px] font-bold uppercase tracking-[0.18em] text-ink-500">Chọn nhanh</span>
              {[
                { label: 'Hôm nay · 1 đêm', start: 0, nights: 1 },
                { label: 'Hôm nay · 2 đêm', start: 0, nights: 2 },
                { label: 'Ngày mai · 1 đêm', start: 1, nights: 1 },
              ].map((q) => (
                <button
                  key={q.label}
                  type="button"
                  onClick={() => {
                    const ci = addDays(today(), q.start)
                    setCheckInDate(ci)
                    setCheckOutDate(addDays(ci, q.nights))
                  }}
                  className={`rounded-full px-3 py-1.5 text-[12px] font-semibold text-ink-700 ring-1 ring-black/10 ${EASE} hover:bg-cream-100 hover:ring-black/20 active:scale-[0.97]`}
                >
                  {q.label}
                </button>
              ))}
            </div>
          </div>
          {nights > 0 && (
            <p className="mt-3 text-center text-[12px] text-ink-500">
              {nights} đêm · {fmtShort(checkInDate)} → {fmtShort(checkOutDate)} · {numberOfGuests} khách
            </p>
          )}
        </div>
      )}

      {step === 2 && (
        <div className="mt-6">
          <div className="flex flex-wrap items-center justify-between gap-3 rounded-2xl bg-white px-5 py-3.5 ring-1 ring-black/5 shadow-soft">
            <div className="flex flex-wrap items-center gap-x-4 gap-y-1 text-[13px] font-semibold">
              <span className="tabular-nums">
                {fmtShort(checkInDate)} → {fmtShort(checkOutDate)}
              </span>
              <span className="text-ink-500/40">|</span>
              <span>{numberOfGuests} khách</span>
              <span className="text-ink-500/40">|</span>
              <span>{nights} đêm</span>
              <button
                type="button"
                onClick={() => setStep(1)}
                className="text-[11px] font-bold uppercase tracking-[0.14em] text-brand-600 underline-offset-4 hover:underline"
              >
                Đổi tìm kiếm
              </button>
            </div>
          </div>

          {searching && <p className="mt-4 text-sm text-ink-500">Đang tìm phòng trống…</p>}
          {searchError && <p className={`mt-4 ${errorCls}`}>{searchError}</p>}

          {!searching && !searchError && (
            <div className="mt-4 space-y-4">
              {roomTypes.map((rt, idx) => (
                <RoomTypeCard
                  key={rt.roomTypeId}
                  rt={rt}
                  idx={idx}
                  selected={bookingType}
                  onSelect={setBookingType}
                  onDetail={setDetailType}
                />
              ))}
              {roomTypes.length === 0 && (
                <div className="rounded-2xl border border-dashed border-black/10 bg-white/60 p-10 text-center">
                  <p className="font-display text-lg italic text-ink-700">Không còn loại phòng nào trống</p>
                  <p className="mt-1 text-[13px] text-ink-500">Thử giảm số khách hoặc đổi khoảng ngày.</p>
                </div>
              )}
            </div>
          )}
        </div>
      )}

      {/* BUOC 3: 2 cot nhu trang le tan - card trang ben trai (quy trinh + yeu cau dac biet),
          card toi tom tat gia ben phai. Khong de card toi dung don doc giua trang sang. */}
      {step === 3 && bookingType && (
        <div className="mt-6 grid gap-6 lg:grid-cols-5">
          <div className="rounded-2xl bg-white p-6 ring-1 ring-black/5 shadow-soft lg:col-span-3">
            <h2 className="text-sm font-bold uppercase tracking-[0.14em] text-ink-500">Sau khi gửi yêu cầu</h2>
            <div className="mt-4 space-y-4">
              {[
                { n: 1, title: 'Lễ tân xác nhận', desc: 'Yêu cầu ở trạng thái "Chờ xác nhận" — lễ tân sẽ giữ phòng và xác nhận với bạn sớm nhất.' },
                { n: 2, title: 'Nhận phòng tại quầy', desc: `Đến khách sạn ngày ${fmtShort(checkInDate)}, mang theo CMND/CCCD để lễ tân làm thủ tục nhận phòng.` },
                { n: 3, title: 'Thanh toán khi trả phòng', desc: 'Tiền phòng và dịch vụ (nếu có) thanh toán tại quầy — chưa cần trả trước khoản nào lúc này.' },
              ].map((s) => (
                <div key={s.n} className="flex gap-3.5">
                  <span className="flex h-7 w-7 shrink-0 items-center justify-center rounded-full bg-cream-100 font-display text-[13px] font-semibold text-brand-600">
                    {s.n}
                  </span>
                  <div>
                    <p className="text-[13px] font-bold">{s.title}</p>
                    <p className="mt-0.5 text-[12px] leading-relaxed text-ink-500">{s.desc}</p>
                  </div>
                </div>
              ))}
            </div>

            <div className="mt-6 border-t border-black/[0.06] pt-5">
              <label className={labelCls}>Yêu cầu đặc biệt (tuỳ chọn)</label>
              <textarea
                rows={2}
                maxLength={500}
                className={inputCls}
                placeholder="Giường phụ, tầng cao, không hút thuốc..."
                value={specialRequests}
                onChange={(e) => setSpecialRequests(e.target.value)}
              />
            </div>

            <button
              type="button"
              onClick={() => setStep(2)}
              className="mt-5 text-[12px] font-semibold text-brand-600 underline-offset-2 hover:underline"
            >
              ← Chọn loại phòng khác
            </button>
          </div>

          <div className="lg:col-span-2">
            <div className="overflow-hidden rounded-2xl bg-ink-900 text-cream-50 shadow-lift">
              <img src={roomImage(bookingType.roomTypeName, 0)} alt="" className="h-32 w-full object-cover opacity-80" />
              <div className="p-5">
                <p className="font-display text-lg font-semibold">{bookingType.roomTypeName}</p>
                <div className="mt-3 space-y-1.5 text-sm">
                  <p className="flex justify-between">
                    <span className="text-cream-50/60">Ngày ở</span>
                    <span>{fmtShort(checkInDate)} → {fmtShort(checkOutDate)}</span>
                  </p>
                  <p className="flex justify-between">
                    <span className="text-cream-50/60">Số khách</span>
                    <span>{numberOfGuests} người</span>
                  </p>
                  <p className="flex justify-between">
                    <span className="text-cream-50/60">Số đêm × giá</span>
                    <span className="tabular-nums">{nights} × {formatVnd(bookingType.basePrice)}</span>
                  </p>
                  <p className="flex justify-between border-t border-white/10 pt-2 text-base">
                    <span className="text-cream-50/60">Tạm tính</span>
                    <span className="font-display font-semibold tabular-nums">{formatVnd(bookingType.basePrice * nights)}</span>
                  </p>
                  {voucher.trim() && (
                    <p className="flex justify-between text-emerald-300">
                      <span className="text-cream-50/60">Mã khuyến mãi</span>
                      <span className="font-semibold">{voucher.trim().toUpperCase()} · áp dụng khi thanh toán</span>
                    </p>
                  )}
                </div>
                <button
                  type="button"
                  onClick={confirmBooking}
                  disabled={booking}
                  className={`mt-5 w-full rounded-full bg-brand-500 py-3 text-sm font-bold text-white ${EASE} hover:bg-brand-600 active:scale-[0.98] disabled:opacity-40`}
                >
                  {booking ? 'Đang gửi…' : 'Gửi yêu cầu đặt phòng'}
                </button>
                <p className="mt-2 text-center text-[11px] text-cream-50/50">
                  Chưa thu tiền — lễ tân xác nhận rồi thanh toán tại quầy.
                </p>
                {bookingError && <p className="mt-3 text-[12px] font-medium text-amber-300">{bookingError}</p>}
              </div>
            </div>
          </div>
        </div>
      )}

      {detailType && (
        <RoomTypeDetailModal
          rt={detailType}
          selected={bookingType}
          onSelect={setBookingType}
          onClose={() => setDetailType(null)}
        />
      )}

      {/* Sticky bar khi da chon loai phong o buoc 2 - kieu BOOK NOW cua trang le tan */}
      <div className={`fixed inset-x-0 bottom-0 z-20 ${EASE} duration-500 ${step === 2 && bookingType ? 'translate-y-0' : 'translate-y-full'}`}>
        <div className="mx-auto flex max-w-5xl items-center justify-between gap-4 rounded-t-2xl bg-ink-900 px-6 py-4 text-cream-50 shadow-lift">
          {bookingType && (
            <p className="text-sm">
              <span className="font-display font-semibold">{bookingType.roomTypeName}</span>
              <span className="text-cream-50/60"> · {nights} đêm · </span>
              <span className="font-display font-semibold tabular-nums">{formatVnd(bookingType.basePrice * nights)}</span>
            </p>
          )}
          <button
            type="button"
            onClick={() => setStep(3)}
            className={`rounded-full bg-brand-500 px-7 py-2.5 text-sm font-bold text-white ${EASE} hover:bg-brand-600 active:scale-[0.98]`}
          >
            Tiếp tục →
          </button>
        </div>
      </div>
    </div>
  )
}
