import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { MOCK_ROOM_TYPES } from '../mock/hotelMock'
import { roomImage } from '../utils/roomImages'
import { roomMeta } from '../utils/roomMeta'
import { formatVnd } from '../utils/roomStatus'

const EASE = 'transition-all duration-300 ease-[cubic-bezier(0.32,0.72,0,1)]'

const today = () => new Date().toISOString().slice(0, 10)
const addDays = (dateStr, n) => {
  const d = new Date(dateStr)
  d.setDate(d.getDate() + n)
  return d.toISOString().slice(0, 10)
}

const TYPE_PRICES = { Standard: 500000, Deluxe: 800000, Suite: 1200000, 'Family Room': 1500000 }

const cellLabel = 'text-[10px] font-bold uppercase tracking-[0.18em] text-ink-500'
const cellInput = 'mt-1 w-full bg-transparent text-sm font-semibold text-ink-900 outline-none'

/* Landing page kiểu web khách sạn: hero + thanh đặt phòng + giới thiệu loại phòng/dịch vụ */
export default function HomePage() {
  const navigate = useNavigate()
  const [checkIn, setCheckIn] = useState(today())
  const [checkOut, setCheckOut] = useState(addDays(today(), 1))
  const [guests, setGuests] = useState(2)
  const [roomType, setRoomType] = useState('all')

  const book = (type = roomType) => {
    const q = new URLSearchParams({ checkIn, checkOut, guests: String(guests), roomType: type })
    navigate(`/reservations/new?${q}`)
  }

  return (
    <div className="bg-cream-100">
      {/* HERO: cao đúng 1 màn hình, các khối xếp flex nên không che nhau, trang vẫn cuộn tiếp xuống dưới */}
      <section className="relative flex min-h-[100dvh] flex-col">
        <img src="/img/login-hero.jpg" alt="" className="absolute inset-0 h-full w-full object-cover" />
        <div className="absolute inset-0 bg-gradient-to-t from-ink-900/85 via-ink-900/25 to-ink-900/45" />

        <header className="relative z-10 flex items-center justify-between px-6 py-5 sm:px-10">
          <div>
            <p className="font-display text-2xl font-semibold tracking-tight text-white">HSMS</p>
            <p className="text-[9px] font-semibold tracking-[0.3em] text-white/60">★★★★★ HOTEL & SERVICE</p>
          </div>
          <nav className="hidden items-center gap-7 text-[12px] font-semibold uppercase tracking-[0.14em] text-white/85 lg:flex">
            <a href="#rooms" className="hover:text-white">Phòng nghỉ</a>
            <a href="#services" className="hover:text-white">Dịch vụ</a>
            <a href="#rooms" className="hover:text-white">Ưu đãi</a>
            <a href="#footer" className="hover:text-white">Liên hệ</a>
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

        <div className="relative z-10 flex flex-1 flex-col justify-center px-6 sm:px-10">
          <p className="font-display text-lg italic text-white/80">mùa hè 2026</p>
          <h1 className="mt-2 max-w-3xl font-display text-4xl font-semibold leading-[1.05] text-white sm:text-6xl lg:text-7xl">
            Kỳ nghỉ trọn vẹn bắt đầu từ đây
          </h1>
          <p className="mt-4 max-w-xl text-[15px] text-white/75">
            Phòng nghỉ tinh tươm, nhà hàng và giặt ủi phục vụ tận phòng — đặt trong 30 giây ngay bên dưới.
          </p>
          <a href="#rooms" className="mt-5 w-max border-b border-white/60 pb-0.5 text-[12px] font-bold uppercase tracking-[0.2em] text-white hover:border-white">
            Khám phá phòng nghỉ ↓
          </a>
        </div>

        {/* Thanh đặt phòng: nằm trong luồng flex, không absolute nên không bị che/cắt */}
        <div className="relative z-10 px-4 pb-6 sm:px-10">
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
              onClick={() => book()}
              className={`col-span-2 bg-brand-600 px-10 py-4 text-sm font-bold uppercase tracking-[0.15em] text-white ${EASE} hover:bg-brand-700 active:scale-[0.99] sm:col-span-1`}
            >
              Đặt ngay
            </button>
          </div>
        </div>
      </section>

      {/* LOẠI PHÒNG */}
      <section id="rooms" className="mx-auto max-w-6xl px-6 py-16 sm:px-10">
        <p className="font-display text-[15px] italic text-brand-600">phòng nghỉ & suite</p>
        <h2 className="mt-1 font-display text-3xl font-semibold tracking-tight">Chọn không gian của bạn</h2>
        <div className="mt-8 grid gap-5 sm:grid-cols-2 lg:grid-cols-4">
          {MOCK_ROOM_TYPES.map((t, i) => {
            const meta = roomMeta(t)
            return (
              <div key={t} className={`group overflow-hidden rounded-2xl bg-white ring-1 ring-black/5 shadow-soft ${EASE} hover:-translate-y-1 hover:shadow-lift`}>
                <div className="h-40 overflow-hidden">
                  <img src={roomImage(t, 0)} alt={t} loading="lazy" className={`h-full w-full object-cover ${EASE} duration-700 group-hover:scale-105`} />
                </div>
                <div className="p-4">
                  <p className="font-display text-lg font-semibold">{t}</p>
                  <p className="mt-1 text-[12px] text-ink-500">👤 {meta.capacity} khách · ⤢ {meta.area} m² · {meta.bed}</p>
                  <div className="mt-3 flex items-end justify-between">
                    <p className="text-[13px] font-semibold tabular-nums text-ink-700">
                      {formatVnd(TYPE_PRICES[t])}<span className="text-[11px] font-normal text-ink-500">/đêm</span>
                    </p>
                    <button onClick={() => book(t)} className="text-[11px] font-bold uppercase tracking-wider text-brand-600 hover:text-brand-700">
                      Đặt ngay →
                    </button>
                  </div>
                </div>
              </div>
            )
          })}
        </div>
      </section>

      {/* DỊCH VỤ */}
      <section id="services" className="bg-cream-200/60 py-16">
        <div className="mx-auto max-w-6xl px-6 sm:px-10">
          <p className="font-display text-[15px] italic text-brand-600">dịch vụ tận phòng</p>
          <h2 className="mt-1 font-display text-3xl font-semibold tracking-tight">Nhà hàng & Giặt ủi</h2>
          <div className="mt-8 grid gap-5 sm:grid-cols-2">
            <div className="rounded-2xl bg-white p-6 ring-1 ring-black/5 shadow-soft">
              <p className="text-2xl">🍽</p>
              <p className="mt-2 font-display text-lg font-semibold">Nhà hàng</p>
              <p className="mt-1.5 text-sm leading-relaxed text-ink-500">
                Bữa sáng, bữa tối và đồ uống phục vụ tận phòng trong suốt kỳ lưu trú. Gọi món qua lễ tân hoặc nhân viên dịch vụ.
              </p>
            </div>
            <div className="rounded-2xl bg-white p-6 ring-1 ring-black/5 shadow-soft">
              <p className="text-2xl">🧺</p>
              <p className="mt-2 font-display text-lg font-semibold">Giặt ủi</p>
              <p className="mt-1.5 text-sm leading-relaxed text-ink-500">
                Nhận và trả đồ trong ngày: giặt áo sơ mi, quần âu, ủi phẳng. Phí dịch vụ tính thẳng vào hóa đơn phòng khi trả phòng.
              </p>
            </div>
          </div>
        </div>
      </section>

      {/* FOOTER */}
      <footer id="footer" className="bg-ink-900 px-6 py-10 text-cream-50 sm:px-10">
        <div className="mx-auto flex max-w-6xl flex-wrap items-end justify-between gap-6">
          <div>
            <p className="font-display text-xl font-semibold">HSMS</p>
            <p className="mt-1 text-[12px] text-cream-50/50">Hotel & Service Management System · Group 2 · SE1919 · FPT University</p>
          </div>
          <div className="text-right text-[13px] text-cream-50/70">
            <p>☏ 1900 636 999 · ✉ hsms@hotel.com</p>
            <p className="mt-1 text-[11px] text-cream-50/40">Đồ án môn Software Engineering · 2026</p>
          </div>
        </div>
      </footer>
    </div>
  )
}
