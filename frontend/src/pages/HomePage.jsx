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
const TYPE_DESC = {
  Standard: 'Gọn gàng và đủ đầy cho chuyến đi ngắn ngày — giường đôi êm, bàn làm việc nhỏ và cửa sổ đón nắng sớm.',
  Deluxe: 'Rộng hơn một nhịp thở, thêm minibar và góc nhìn thành phố — lựa chọn được đặt nhiều nhất của chúng tôi.',
  Suite: 'Phòng khách riêng tách biệt khỏi giường ngủ, bồn tắm dài và ánh đèn ấm — dành cho kỳ nghỉ không vội vã.',
  'Family Room': 'Hai giường lớn, ban công và góc nhỏ cho trẻ em — cả nhà ở cùng nhau mà không ai phải nhường ai.',
}

const eyebrow = 'text-[10px] font-bold uppercase tracking-[0.3em] text-brand-600'
const cellLabel = 'text-[10px] font-bold uppercase tracking-[0.18em] text-ink-500'
const cellInput = 'mt-1 w-full bg-transparent text-sm font-semibold text-ink-900 outline-none'

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
    <div className="bg-cream-50">
      {/* ===== HERO ===== */}
      <section className="relative flex min-h-[100dvh] flex-col">
        <img src="/img/login-hero.jpg" alt="" className="absolute inset-0 h-full w-full object-cover" />
        <div className="absolute inset-0 bg-gradient-to-t from-ink-900/85 via-ink-900/25 to-ink-900/45" />

        <header className="relative z-10 flex items-center justify-between px-6 py-6 sm:px-12">
          <div>
            <p className="font-display text-2xl font-semibold tracking-tight text-white">HSMS</p>
            <p className="mt-0.5 text-[8px] font-semibold tracking-[0.42em] text-white/60">HOTEL & SUITES — EST. 2026</p>
          </div>
          <nav className="hidden items-center gap-8 text-[11px] font-semibold uppercase tracking-[0.2em] text-white/80 lg:flex">
            <a href="#rooms" className={`${EASE} hover:text-white`}>Phòng nghỉ</a>
            <a href="#services" className={`${EASE} hover:text-white`}>Dịch vụ</a>
            <a href="#footer" className={`${EASE} hover:text-white`}>Liên hệ</a>
          </nav>
          <div className="flex items-center gap-5">
            <span className="hidden text-[12px] font-medium tracking-wide text-white/80 sm:block">1900 636 999</span>
            <button
              onClick={() => navigate('/login')}
              className={`rounded-full px-5 py-2 text-[11px] font-bold uppercase tracking-[0.15em] text-white ring-1 ring-white/40 ${EASE} hover:bg-white hover:text-ink-900`}
            >
              Nhân viên
            </button>
          </div>
        </header>

        <div className="relative z-10 flex flex-1 flex-col items-center justify-center px-6 text-center">
          <p className="text-[10px] font-bold uppercase tracking-[0.42em] text-white/70">Khách sạn · Nhà hàng · Giặt ủi</p>
          <h1 className="mt-5 max-w-4xl font-display text-5xl font-medium leading-[1.04] text-white [text-wrap:balance] sm:text-7xl">
            Kỳ nghỉ trọn vẹn bắt đầu từ đây
          </h1>
          <p className="mt-6 max-w-md text-[14px] leading-relaxed text-white/70">
            Chín phòng nghỉ được chăm chút từng chi tiết, dịch vụ phục vụ tận nơi trong suốt kỳ lưu trú.
          </p>
        </div>

        <div className="relative z-10 px-4 pb-8 sm:px-12">
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
              className={`col-span-2 bg-brand-600 px-10 py-4 text-sm font-bold uppercase tracking-[0.18em] text-white ${EASE} hover:bg-brand-700 active:scale-[0.99] sm:col-span-1`}
            >
              Đặt ngay
            </button>
          </div>
        </div>
      </section>

      {/* ===== TUYÊN NGÔN — một câu serif giữa trang ===== */}
      <section className="mx-auto max-w-3xl px-6 py-24 text-center sm:py-32">
        <p className={eyebrow}>Về chúng tôi</p>
        <p className="mt-7 font-display text-2xl font-medium leading-snug text-ink-900 [text-wrap:balance] sm:text-[2rem]">
          Một khách sạn nhỏ vận hành như một lời hứa — phòng luôn sẵn sàng trước khi bạn đến,
          <em className="text-brand-600"> bữa sáng nóng</em> và
          <em className="text-brand-600"> áo sơ mi được ủi phẳng</em> trước khi bạn kịp nhớ ra mình cần chúng.
        </p>
        <div className="mx-auto mt-10 h-px w-16 bg-brand-600/40" />
      </section>

      {/* ===== PHÒNG NGHỈ — hàng editorial so le, có số thứ tự ===== */}
      <section id="rooms" className="mx-auto max-w-6xl px-6 pb-8 sm:px-12">
        <div className="mb-14 flex items-end justify-between">
          <div>
            <p className={eyebrow}>Phòng nghỉ & Suite</p>
            <h2 className="mt-3 font-display text-4xl font-medium tracking-tight">Chọn không gian của bạn</h2>
          </div>
          <p className="hidden text-[12px] text-ink-500 sm:block">04 hạng phòng · từ {formatVnd(500000)}/đêm</p>
        </div>

        <div className="space-y-20 pb-20">
          {MOCK_ROOM_TYPES.map((t, i) => {
            const meta = roomMeta(t)
            const flip = i % 2 === 1
            return (
              <div key={t} className={`grid items-center gap-8 lg:grid-cols-12 ${flip ? '' : ''}`}>
                <div className={`group relative overflow-hidden lg:col-span-7 ${flip ? 'lg:order-2' : ''}`}>
                  <img
                    src={roomImage(t, 0)}
                    alt={t}
                    loading="lazy"
                    className={`aspect-[16/10] w-full object-cover ${EASE} duration-[1.2s] group-hover:scale-[1.04]`}
                  />
                </div>
                <div className={`lg:col-span-5 ${flip ? 'lg:order-1 lg:pr-6' : 'lg:pl-6'}`}>
                  <p className="font-display text-sm italic text-ink-500">0{i + 1} — 04</p>
                  <h3 className="mt-3 font-display text-3xl font-medium tracking-tight">{t}</h3>
                  <p className="mt-2 text-[12px] uppercase tracking-[0.18em] text-ink-500">
                    {meta.capacity} khách &nbsp;·&nbsp; {meta.area} m² &nbsp;·&nbsp; {meta.bed}
                  </p>
                  <p className="mt-5 max-w-md text-[14px] leading-relaxed text-ink-700">{TYPE_DESC[t]}</p>
                  <div className="mt-7 flex items-center gap-8">
                    <p className="font-display text-xl font-semibold tabular-nums">
                      {formatVnd(TYPE_PRICES[t])}
                      <span className="font-sans text-[11px] font-normal text-ink-500"> / đêm</span>
                    </p>
                    <button
                      onClick={() => book(t)}
                      className={`group/btn relative pb-1 text-[11px] font-bold uppercase tracking-[0.22em] text-ink-900 ${EASE}`}
                    >
                      Đặt phòng này
                      <span className={`absolute bottom-0 left-0 h-px w-full bg-ink-900/30 ${EASE} group-hover/btn:bg-brand-600`} />
                    </button>
                  </div>
                </div>
              </div>
            )
          })}
        </div>
      </section>

      {/* ===== DỊCH VỤ — dải nền tối full width ===== */}
      <section id="services" className="bg-ink-900 text-cream-50">
        <div className="mx-auto grid max-w-6xl gap-0 px-6 py-24 sm:px-12 lg:grid-cols-2 lg:gap-16">
          <div>
            <p className="text-[10px] font-bold uppercase tracking-[0.3em] text-brand-500">Dịch vụ tận phòng</p>
            <h2 className="mt-3 font-display text-4xl font-medium tracking-tight [text-wrap:balance]">
              Mọi thứ đến tận cửa, đúng lúc bạn cần
            </h2>
          </div>
          <div className="mt-10 space-y-10 lg:mt-2">
            <div className="border-t border-white/10 pt-8">
              <p className="font-display text-xl font-medium">Nhà hàng</p>
              <p className="mt-2.5 max-w-md text-[14px] leading-relaxed text-cream-50/60">
                Bữa sáng, bữa tối và đồ uống phục vụ tận phòng trong suốt kỳ lưu trú.
                Gọi món qua lễ tân — chi phí tính thẳng vào hóa đơn khi trả phòng.
              </p>
            </div>
            <div className="border-t border-white/10 pt-8">
              <p className="font-display text-xl font-medium">Giặt ủi</p>
              <p className="mt-2.5 max-w-md text-[14px] leading-relaxed text-cream-50/60">
                Nhận và trả đồ trong ngày: áo sơ mi, quần âu, ủi phẳng.
                Đồ của bạn quay về tủ trước giờ hẹn tiếp theo.
              </p>
            </div>
          </div>
        </div>
      </section>

      {/* ===== FOOTER ===== */}
      <footer id="footer" className="bg-cream-50">
        <div className="mx-auto max-w-6xl px-6 pb-10 pt-20 sm:px-12">
          <div className="grid gap-10 border-b border-black/[0.07] pb-14 lg:grid-cols-3">
            <div>
              <p className="font-display text-3xl font-semibold tracking-tight">HSMS</p>
              <p className="mt-1 text-[9px] font-semibold tracking-[0.42em] text-ink-500">HOTEL & SUITES — EST. 2026</p>
            </div>
            <div className="text-[13px] leading-loose text-ink-700">
              <p className={`${eyebrow} mb-3`}>Liên hệ</p>
              <p>1900 636 999</p>
              <p>hsms@hotel.com</p>
            </div>
            <div className="text-[13px] leading-loose text-ink-700">
              <p className={`${eyebrow} mb-3`}>Khám phá</p>
              <a href="#rooms" className="block hover:text-brand-600">Phòng nghỉ & Suite</a>
              <a href="#services" className="block hover:text-brand-600">Dịch vụ tận phòng</a>
              <button onClick={() => navigate('/login')} className="block hover:text-brand-600">Cổng nhân viên</button>
            </div>
          </div>
          <p className="pt-6 text-[11px] text-ink-500/60">
            Hotel & Service Management System — Đồ án Software Engineering · Group 2 · SE1919 · FPT University · 2026
          </p>
        </div>
      </footer>
    </div>
  )
}
