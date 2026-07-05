import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { MOCK_ROOM_TYPES } from '../mock/hotelMock'

const EASE = 'transition-all duration-300 ease-[cubic-bezier(0.32,0.72,0,1)]'

const today = () => new Date().toISOString().slice(0, 10)
const addDays = (dateStr, n) => {
  const d = new Date(dateStr)
  d.setDate(d.getDate() + n)
  return d.toISOString().slice(0, 10)
}

const cellLabel = 'text-[10px] font-bold uppercase tracking-[0.18em] text-ink-500'
const cellInput = 'mt-1 w-full bg-transparent text-sm font-semibold text-ink-900 outline-none'

/* Trang chủ kiểu web khách sạn: hero full màn hình + thanh đặt phòng (mẫu International Hotel) */
export default function HomePage() {
  const navigate = useNavigate()
  const [checkIn, setCheckIn] = useState(today())
  const [checkOut, setCheckOut] = useState(addDays(today(), 1))
  const [guests, setGuests] = useState(2)
  const [roomType, setRoomType] = useState('all')

  const book = () => {
    const q = new URLSearchParams({ checkIn, checkOut, guests: String(guests), roomType })
    navigate(`/reservations/new?${q}`)
  }

  return (
    <div className="relative min-h-[100dvh] overflow-hidden bg-ink-900">
      <img src="/img/login-hero.jpg" alt="" className="absolute inset-0 h-full w-full object-cover" />
      <div className="absolute inset-0 bg-gradient-to-t from-ink-900/85 via-ink-900/25 to-ink-900/45" />

      {/* Nav trên cùng - đè lên ảnh như mẫu */}
      <header className="relative z-10 flex items-center justify-between px-6 py-5 sm:px-10">
        <div>
          <p className="font-display text-2xl font-semibold tracking-tight text-white">HSMS</p>
          <p className="text-[9px] font-semibold tracking-[0.3em] text-white/60">★★★★★ HOTEL & SERVICE</p>
        </div>
        <nav className="hidden items-center gap-7 text-[12px] font-semibold uppercase tracking-[0.14em] text-white/85 lg:flex">
          <span className="cursor-default hover:text-white">Phòng nghỉ</span>
          <span className="cursor-default hover:text-white">Nhà hàng</span>
          <span className="cursor-default hover:text-white">Giặt ủi</span>
          <span className="cursor-default hover:text-white">Ưu đãi</span>
          <span className="cursor-default hover:text-white">Liên hệ</span>
        </nav>
        <div className="flex items-center gap-4">
          <span className="hidden text-[13px] font-semibold text-white/85 sm:block">☏ 1900 636 999</span>
          <button
            onClick={() => navigate('/login')}
            className={`rounded-full px-5 py-2 text-[12px] font-bold uppercase tracking-wider text-white ring-1 ring-white/50 ${EASE} hover:bg-white hover:text-ink-900`}
          >
            Nhân viên
          </button>
        </div>
      </header>

      {/* Khối chữ lớn giữa màn - kiểu SUMMER IS HERE */}
      <div className="relative z-10 mt-[16vh] px-6 sm:px-10 lg:mt-[20vh]">
        <p className="font-display text-lg italic text-white/80">mùa hè 2026</p>
        <h1 className="mt-2 max-w-3xl font-display text-5xl font-semibold leading-[1.05] text-white sm:text-7xl">
          Kỳ nghỉ trọn vẹn bắt đầu từ đây
        </h1>
        <p className="mt-4 max-w-xl text-[15px] text-white/75">
          Phòng nghỉ tinh tươm, nhà hàng và giặt ủi phục vụ tận phòng — đặt trong 30 giây ngay bên dưới.
        </p>
      </div>

      {/* Thanh đặt phòng dưới đáy - TRAVEL DATES | GUESTS | ROOMS | BOOK NOW */}
      <div className="absolute inset-x-0 bottom-0 z-10 p-4 sm:bottom-8 sm:px-10">
        <div className="mx-auto grid max-w-5xl grid-cols-2 overflow-hidden rounded-2xl bg-white shadow-lift sm:grid-cols-[1fr_1fr_0.7fr_1fr_auto]">
          <div className="border-b border-r border-black/[0.07] px-5 py-4 sm:border-b-0">
            <p className={cellLabel}>Ngày nhận phòng</p>
            <input type="date" className={cellInput} value={checkIn} min={today()}
              onChange={(e) => { setCheckIn(e.target.value); if (e.target.value >= checkOut) setCheckOut(addDays(e.target.value, 1)) }} />
          </div>
          <div className="border-b border-black/[0.07] px-5 py-4 sm:border-b-0 sm:border-r">
            <p className={cellLabel}>Ngày trả phòng</p>
            <input type="date" className={cellInput} value={checkOut} min={addDays(checkIn, 1)} onChange={(e) => setCheckOut(e.target.value)} />
          </div>
          <div className="border-r border-black/[0.07] px-5 py-4">
            <p className={cellLabel}>Khách</p>
            <div className="mt-1 flex items-center gap-2">
              <button onClick={() => setGuests(Math.max(1, guests - 1))} className="text-sm font-bold text-ink-500 hover:text-ink-900">−</button>
              <span className="w-6 text-center text-sm font-bold tabular-nums">{guests}</span>
              <button onClick={() => setGuests(Math.min(8, guests + 1))} className="text-sm font-bold text-ink-500 hover:text-ink-900">+</button>
            </div>
          </div>
          <div className="px-5 py-4">
            <p className={cellLabel}>Loại phòng</p>
            <select className={`${cellInput} cursor-pointer`} value={roomType} onChange={(e) => setRoomType(e.target.value)}>
              <option value="all">Tất cả</option>
              {MOCK_ROOM_TYPES.map((t) => <option key={t} value={t}>{t}</option>)}
            </select>
          </div>
          <button
            onClick={book}
            className={`col-span-2 bg-brand-600 px-10 py-4 text-sm font-bold uppercase tracking-[0.15em] text-white ${EASE} hover:bg-brand-700 active:scale-[0.99] sm:col-span-1`}
          >
            Đặt ngay
          </button>
        </div>
      </div>
    </div>
  )
}
