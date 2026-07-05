import { useEffect, useMemo, useState } from 'react'
import { useSearchParams } from 'react-router-dom'
import client from '../api/client'
import { formatVnd } from '../utils/roomStatus'
import { MOCK_AVAILABLE_ROOMS, MOCK_ROOM_TYPES } from '../mock/hotelMock'
import { roomImage } from '../utils/roomImages'
import { roomMeta } from '../utils/roomMeta'

const EASE = 'transition-all duration-300 ease-[cubic-bezier(0.32,0.72,0,1)]'
const inputCls =
  'w-full rounded-xl bg-white px-3.5 py-2.5 text-sm ring-1 ring-black/10 outline-none placeholder:text-ink-500/50 focus:ring-2 focus:ring-brand-500/40'
const labelCls = 'mb-1.5 block text-[12px] font-semibold text-ink-700'

import { localToday as today, addDays, fmtShort } from '../utils/dates'

/* Chỉ báo 3 bước - bước đã xong bấm được để quay lại, đường nối fill theo tiến độ */
function Steps({ current, onBack }) {
  const items = ['Ngày ở & khách', 'Chọn phòng', 'Xác nhận']
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

/* Card phòng ngang kiểu booking engine, có carousel ảnh mini */
function RoomResultCard({ room, idx, selected, onSelect }) {
  const meta = roomMeta(room.typeName)
  const [imgIdx, setImgIdx] = useState(idx)
  const active = selected?.roomId === room.roomId
  return (
    <div className={`overflow-hidden rounded-2xl bg-white ring-1 ${EASE} ${active ? 'ring-2 ring-brand-600/50 shadow-lift' : 'ring-black/5 shadow-soft hover:ring-black/15'}`}>
      <div className="flex flex-col sm:flex-row">
        <div className="group relative h-44 shrink-0 overflow-hidden sm:h-auto sm:w-60">
          <img src={roomImage(room.typeName, imgIdx)} alt={room.typeName} className="h-full w-full object-cover" loading="lazy" />
          <button
            onClick={() => setImgIdx((imgIdx + 3) % 4)}
            className="absolute left-2 top-1/2 flex h-7 w-7 -translate-y-1/2 items-center justify-center rounded-full bg-white/85 text-xs font-bold text-ink-700 opacity-0 backdrop-blur-sm group-hover:opacity-100"
            aria-label="Ảnh trước"
          >
            ‹
          </button>
          <button
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
              <p className="font-display text-xl font-semibold">Phòng {room.roomNumber} · {room.typeName}</p>
              <p className="mt-2 text-[11px] uppercase tracking-[0.16em] text-ink-500">
                {meta.capacity} khách &nbsp;·&nbsp; {meta.area} m² &nbsp;·&nbsp; {meta.bed} &nbsp;·&nbsp; Tầng {room.floor}
              </p>
            </div>
          </div>
          <div className="mt-3 flex flex-wrap gap-1.5">
            {meta.amenities.map((a) => (
              <span key={a} className="rounded-full bg-cream-100 px-2.5 py-1 text-[11px] font-medium text-ink-700">{a}</span>
            ))}
          </div>
          <div className="mt-auto flex items-end justify-between pt-4">
            <p className="font-display text-xl font-semibold tabular-nums">
              {formatVnd(room.basePrice)}
              <span className="font-sans text-[11px] font-normal text-ink-500"> / đêm</span>
            </p>
            <button
              onClick={() => onSelect(active ? null : room)}
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

export default function CreateReservationPage() {
  const [step, setStep] = useState(1)
  const [checkIn, setCheckIn] = useState(today())
  const [checkOut, setCheckOut] = useState(addDays(today(), 1))
  const [guests, setGuests] = useState(2)
  const [roomType, setRoomType] = useState('all')
  const [results, setResults] = useState([])
  const [usingMock, setUsingMock] = useState(false)
  const [sortAsc, setSortAsc] = useState(true)
  const [selected, setSelected] = useState(null)
  const [guest, setGuest] = useState({ fullName: '', phoneNumber: '', email: '', identityNumber: '' })
  const [done, setDone] = useState(null)
  const [error, setError] = useState(null)

  const [searchParams] = useSearchParams()

  const nights = useMemo(() => Math.max(Math.round((new Date(checkOut) - new Date(checkIn)) / 86400000), 0), [checkIn, checkOut])

  const search = (ci = checkIn, co = checkOut, g = guests, rt = roomType) => {
    setSelected(null)
    client
      .get('/api/reservations/available-rooms', { params: { checkIn: ci, checkOut: co, roomType: rt, guests: g } })
      .then((res) => { setResults(res.data); setUsingMock(false) })
      .catch(() => {
        setResults(MOCK_AVAILABLE_ROOMS.filter(
          (r) => (rt === 'all' || r.typeName === rt) && roomMeta(r.typeName).capacity >= g,
        ))
        setUsingMock(true)
      })
    setStep(2)
  }

  // Đến từ thanh "Đặt ngay" trang chủ: nhận params và nhảy thẳng bước 2
  useEffect(() => {
    const ci = searchParams.get('checkIn')
    if (!ci) return
    const co = searchParams.get('checkOut') ?? addDays(ci, 1)
    const g = Number(searchParams.get('guests') ?? 2)
    const rt = searchParams.get('roomType') ?? 'all'
    setCheckIn(ci); setCheckOut(co); setGuests(g); setRoomType(rt)
    search(ci, co, g, rt)
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [])

  const confirm = () => {
    setError(null)
    const payload = { ...guest, roomId: selected.roomId, checkInDate: checkIn, checkOutDate: checkOut }
    client
      .post('/api/reservations', payload)
      .then((res) => setDone({ code: res.data.bookingCode ?? 'BK-XXXX' }))
      .catch(() => setError('API /api/reservations chưa sẵn sàng (task T3). Toàn bộ luồng UI đã hoạt động - nối API là chạy.'))
  }

  const sorted = useMemo(() => [...results].sort((a, b) => (sortAsc ? a.basePrice - b.basePrice : b.basePrice - a.basePrice)), [results, sortAsc])

  /* Màn hình thành công - mã booking to kiểu boarding pass */
  if (done) {
    return (
      <div className="mx-auto max-w-lg pt-10 text-center">
        <p className="font-display text-[15px] italic text-emerald-600">đặt phòng thành công</p>
        <div className="mt-4 rounded-2xl bg-white p-8 ring-1 ring-black/5 shadow-lift">
          <p className="text-[11px] font-bold uppercase tracking-[0.25em] text-ink-500">Mã booking</p>
          <p className="mt-2 font-display text-5xl font-semibold tracking-tight text-brand-600">{done.code}</p>
          <div className="my-5 border-t border-dashed border-black/10" />
          <p className="text-sm text-ink-700">{guest.fullName} · Phòng {selected.roomNumber} ({selected.typeName})</p>
          <p className="mt-1 text-sm text-ink-500">{fmtShort(checkIn)} → {fmtShort(checkOut)} · {nights} đêm · {formatVnd(selected.basePrice * nights)}</p>
        </div>
        <button onClick={() => window.location.reload()} className="mt-6 rounded-full bg-ink-900 px-6 py-2.5 text-sm font-bold text-cream-50">
          Tạo đặt phòng mới
        </button>
      </div>
    )
  }

  return (
    <div className="mx-auto max-w-5xl pb-24">
      <Steps current={step} onBack={setStep} />

      {/* BƯỚC 1: hero + thanh tìm kiếm pill */}
      {step === 1 && (
        <div className="mt-6">
          <div className="relative h-44 overflow-hidden rounded-2xl">
            <img src="/img/login-hero.jpg" alt="" className="h-full w-full object-cover" />
            <div className="absolute inset-0 bg-gradient-to-r from-ink-900/75 to-transparent" />
            <div className="absolute left-7 top-1/2 -translate-y-1/2">
              <p className="text-[10px] font-bold uppercase tracking-[0.3em] text-white/70">Lễ tân · Đặt phòng mới</p>
              <p className="mt-2 font-display text-3xl font-medium text-white">Kỳ lưu trú của quý khách</p>
              <p className="mt-1.5 text-[13px] text-white/70">Chọn ngày ở và số khách để tìm phòng trống</p>
            </div>
          </div>

          <div className="relative z-10 mx-4 -mt-8 flex flex-wrap items-end gap-x-5 gap-y-4 rounded-2xl bg-white p-5 ring-1 ring-black/5 shadow-lift">
            <div className="min-w-36 flex-1">
              <label className={labelCls}>Nhận phòng</label>
              <input type="date" className={inputCls} value={checkIn} min={today()}
                onChange={(e) => { setCheckIn(e.target.value); if (e.target.value >= checkOut) setCheckOut(addDays(e.target.value, 1)) }} />
            </div>
            <div className="min-w-36 flex-1">
              <label className={labelCls}>Trả phòng</label>
              <input type="date" className={inputCls} value={checkOut} min={addDays(checkIn, 1)} onChange={(e) => setCheckOut(e.target.value)} />
            </div>
            <div>
              <label className={labelCls}>Số khách</label>
              <div className="flex items-center gap-1 rounded-xl bg-white ring-1 ring-black/10">
                <button onClick={() => setGuests(Math.max(1, guests - 1))} className="px-3 py-2.5 text-sm font-bold text-ink-500 hover:text-ink-900">−</button>
                <span className="w-8 text-center text-sm font-bold tabular-nums">{guests}</span>
                <button onClick={() => setGuests(Math.min(8, guests + 1))} className="px-3 py-2.5 text-sm font-bold text-ink-500 hover:text-ink-900">+</button>
              </div>
            </div>
            <div className="min-w-36">
              <label className={labelCls}>Loại phòng</label>
              <select className={inputCls} value={roomType} onChange={(e) => setRoomType(e.target.value)}>
                <option value="all">Tất cả</option>
                {MOCK_ROOM_TYPES.map((t) => <option key={t} value={t}>{t}</option>)}
              </select>
            </div>
            <button
              onClick={() => search()}
              disabled={nights <= 0}
              className={`h-11 rounded-full bg-brand-600 px-7 text-sm font-bold text-white ${EASE} hover:bg-brand-700 active:scale-[0.98] disabled:opacity-40`}
            >
              Tìm phòng trống
            </button>

            {/* Quick chips cho lễ tân - 1 click set xong cả 2 ngày */}
            <div className="flex w-full flex-wrap items-center gap-2 border-t border-black/[0.06] pt-3.5">
              <span className="text-[10px] font-bold uppercase tracking-[0.18em] text-ink-500">Chọn nhanh</span>
              {[
                { label: 'Hôm nay · 1 đêm', start: 0, nights: 1 },
                { label: 'Hôm nay · 2 đêm', start: 0, nights: 2 },
                { label: 'Ngày mai · 1 đêm', start: 1, nights: 1 },
              ].map((q) => (
                <button
                  key={q.label}
                  onClick={() => { const ci = addDays(today(), q.start); setCheckIn(ci); setCheckOut(addDays(ci, q.nights)) }}
                  className={`rounded-full px-3 py-1.5 text-[12px] font-semibold text-ink-700 ring-1 ring-black/10 ${EASE} hover:bg-cream-100 hover:ring-black/20 active:scale-[0.97]`}
                >
                  {q.label}
                </button>
              ))}
            </div>
          </div>
          {nights > 0 && <p className="mt-3 text-center text-[12px] text-ink-500">{nights} đêm · {fmtShort(checkIn)} → {fmtShort(checkOut)} · {guests} khách</p>}

          {/* Walk-in nhanh: phòng đang trống, click là sang thẳng bước 2 với hôm nay -> mai */}
          <div className="mt-10">
            <div className="mb-4 flex items-baseline gap-3">
              <h2 className="font-display text-xl font-semibold italic">Phòng trống hôm nay</h2>
              <p className="text-[10px] font-bold uppercase tracking-[0.22em] text-ink-500">· khách walk-in nhận phòng ngay</p>
            </div>
            <div className="grid grid-cols-2 gap-4 sm:grid-cols-3 lg:grid-cols-4">
              {MOCK_AVAILABLE_ROOMS.map((room, idx) => (
                <button
                  key={room.roomId}
                  onClick={() => {
                    const ci = today()
                    setCheckIn(ci); setCheckOut(addDays(ci, 1))
                    search(ci, addDays(ci, 1), guests, 'all')
                  }}
                  className={`group rounded-t-[999px] rounded-b-2xl bg-white p-2.5 pb-4 text-center ring-1 ring-black/[0.07] shadow-soft ${EASE} hover:-translate-y-1 hover:shadow-lift`}
                >
                  <div className="overflow-hidden rounded-t-[999px] rounded-b-lg">
                    <img src={roomImage(room.typeName, idx)} alt={room.typeName} loading="lazy"
                      className={`h-20 w-full object-cover ${EASE} duration-700 group-hover:scale-[1.07]`} />
                  </div>
                  <p className="mt-2.5 font-display text-xl font-semibold tabular-nums">{room.roomNumber}</p>
                  <p className="mt-0.5 text-[9px] font-bold uppercase tracking-[0.2em] text-ink-500">{room.typeName}</p>
                  <p className="mt-1 text-[11px] tabular-nums text-ink-500">{formatVnd(room.basePrice)}/đêm</p>
                </button>
              ))}
            </div>
          </div>
        </div>
      )}

      {/* BƯỚC 2: kết quả phòng */}
      {step === 2 && (
        <div className="mt-6">
          <div className="flex flex-wrap items-center justify-between gap-3 rounded-2xl bg-white px-5 py-3.5 ring-1 ring-black/5 shadow-soft">
            <div className="flex flex-wrap items-center gap-x-4 gap-y-1 text-[13px] font-semibold">
              <span className="tabular-nums">{fmtShort(checkIn)} → {fmtShort(checkOut)}</span>
              <span className="text-ink-500/40">|</span>
              <span>{guests} khách</span>
              <span className="text-ink-500/40">|</span>
              <span>{nights} đêm</span>
              <button onClick={() => setStep(1)} className="text-[11px] font-bold uppercase tracking-[0.14em] text-brand-600 underline-offset-4 hover:underline">Đổi tìm kiếm</button>
            </div>
            <button
              onClick={() => setSortAsc(!sortAsc)}
              className="rounded-lg px-3 py-1.5 text-[12px] font-semibold text-ink-700 ring-1 ring-black/10 hover:bg-cream-50"
            >
              Sắp xếp: giá {sortAsc ? 'tăng dần ↑' : 'giảm dần ↓'}
            </button>
          </div>

          {usingMock && (
            <p className="mt-3 flex items-center gap-1.5 text-[11px] font-medium text-ink-500">
              <span className="h-1.5 w-1.5 animate-pulse rounded-full bg-amber-500" /> dữ liệu mẫu - chờ API available-rooms (T3)
            </p>
          )}

          <div className="mt-4 space-y-4">
            {sorted.map((room, idx) => (
              <RoomResultCard key={room.roomId} room={room} idx={idx} selected={selected} onSelect={setSelected} />
            ))}
            {sorted.length === 0 && (
              <div className="rounded-2xl border border-dashed border-black/10 bg-white/60 p-10 text-center">
                <p className="font-display text-lg italic text-ink-700">Không còn phòng phù hợp</p>
                <p className="mt-1 text-[13px] text-ink-500">Thử giảm số khách hoặc đổi khoảng ngày.</p>
              </div>
            )}
          </div>
        </div>
      )}

      {/* BƯỚC 3: thông tin khách + xác nhận */}
      {step === 3 && selected && (
        <div className="mt-6 grid gap-6 lg:grid-cols-5">
          <div className="rounded-2xl bg-white p-6 ring-1 ring-black/5 shadow-soft lg:col-span-3">
            <h2 className="text-sm font-bold uppercase tracking-[0.14em] text-ink-500">Thông tin khách</h2>
            <div className="mt-4 grid gap-4 sm:grid-cols-2">
              <div className="sm:col-span-2">
                <label className={labelCls}>Họ và tên *</label>
                <input className={inputCls} placeholder="Nguyễn Văn A" value={guest.fullName} onChange={(e) => setGuest({ ...guest, fullName: e.target.value })} />
              </div>
              <div>
                <label className={labelCls}>Số điện thoại</label>
                <input className={inputCls} placeholder="09xx xxx xxx" value={guest.phoneNumber} onChange={(e) => setGuest({ ...guest, phoneNumber: e.target.value })} />
              </div>
              <div>
                <label className={labelCls}>CMND / CCCD</label>
                <input className={inputCls} placeholder="0790xxxxxxxx" value={guest.identityNumber} onChange={(e) => setGuest({ ...guest, identityNumber: e.target.value })} />
              </div>
              <div className="sm:col-span-2">
                <label className={labelCls}>Email</label>
                <input className={inputCls} placeholder="khach@email.com" value={guest.email} onChange={(e) => setGuest({ ...guest, email: e.target.value })} />
              </div>
            </div>
            <button onClick={() => setStep(2)} className="mt-5 text-[12px] font-semibold text-brand-600 underline-offset-2 hover:underline">← Chọn phòng khác</button>
          </div>

          <div className="lg:col-span-2">
            <div className="overflow-hidden rounded-2xl bg-ink-900 text-cream-50 shadow-lift">
              <img src={roomImage(selected.typeName, 0)} alt="" className="h-32 w-full object-cover opacity-80" />
              <div className="p-5">
                <p className="font-display text-lg font-semibold">Phòng {selected.roomNumber} · {selected.typeName}</p>
                <div className="mt-3 space-y-1.5 text-sm">
                  <p className="flex justify-between"><span className="text-cream-50/60">Ngày ở</span><span>{fmtShort(checkIn)} → {fmtShort(checkOut)}</span></p>
                  <p className="flex justify-between"><span className="text-cream-50/60">Số đêm × giá</span><span className="tabular-nums">{nights} × {formatVnd(selected.basePrice)}</span></p>
                  <p className="flex justify-between border-t border-white/10 pt-2 text-base"><span className="text-cream-50/60">Tạm tính</span><span className="font-display font-semibold tabular-nums">{formatVnd(selected.basePrice * nights)}</span></p>
                </div>
                <button
                  onClick={confirm}
                  disabled={!guest.fullName.trim()}
                  className={`mt-5 w-full rounded-full bg-brand-500 py-3 text-sm font-bold text-white ${EASE} hover:bg-brand-600 active:scale-[0.98] disabled:opacity-30`}
                >
                  Xác nhận đặt phòng
                </button>
                {!guest.fullName.trim() && <p className="mt-2 text-center text-[11px] text-cream-50/50">Nhập họ tên khách để xác nhận</p>}
                {error && <p className="mt-3 text-[12px] font-medium text-amber-300">{error}</p>}
              </div>
            </div>
          </div>
        </div>
      )}

      {/* Sticky bar khi đã chọn phòng ở bước 2 - kiểu BOOK NOW */}
      <div className={`fixed inset-x-0 bottom-0 z-20 ${EASE} duration-500 ${step === 2 && selected ? 'translate-y-0' : 'translate-y-full'}`}>
        <div className="mx-auto flex max-w-5xl items-center justify-between gap-4 rounded-t-2xl bg-ink-900 px-6 py-4 text-cream-50 shadow-lift">
          {selected && (
            <p className="text-sm">
              <span className="font-display font-semibold">Phòng {selected.roomNumber}</span>
              <span className="text-cream-50/60"> · {nights} đêm · </span>
              <span className="font-display font-semibold tabular-nums">{formatVnd(selected.basePrice * nights)}</span>
            </p>
          )}
          <button
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
