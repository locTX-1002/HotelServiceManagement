import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { MOCK_ROOM_TYPES } from '../mock/hotelMock'
import { roomImage } from '../utils/roomImages'
import { roomMeta } from '../utils/roomMeta'
import { formatVnd } from '../utils/roomStatus'
import { localToday as today, addDays } from '../utils/dates'
import { Reveal } from '../utils/useInView'

const EASE = 'transition-all duration-300 ease-[cubic-bezier(0.32,0.72,0,1)]'
const EASE_SLOW = 'transition-all duration-700 ease-[cubic-bezier(0.32,0.72,0,1)]'

// Nút "Đặt phòng" kiểu button-in-button: mũi tên nằm trong vòng tròn riêng, tách khỏi chữ
function BookButton({ onClick, dark }) {
  return (
    <button
      onClick={onClick}
      className={`group/btn inline-flex items-center gap-3 rounded-full py-2 pl-5 pr-2 text-[11px] font-bold uppercase tracking-[0.18em] ${EASE_SLOW} active:scale-[0.97] ${
        dark ? 'bg-ink-900 text-cream-50 hover:bg-ink-700' : 'bg-brand-600 text-white hover:bg-brand-700'
      }`}
    >
      Đặt phòng
      <span className={`flex h-6 w-6 items-center justify-center rounded-full bg-white/15 ${EASE_SLOW} group-hover/btn:translate-x-0.5 group-hover/btn:bg-white/25`}>
        ↗
      </span>
    </button>
  )
}

const TYPE_PRICES = { Standard: 500000, Deluxe: 800000, Suite: 1200000, 'Family Room': 1500000 }

const cellLabel = 'text-[10px] font-bold uppercase tracking-[0.18em] text-ink-500'
const cellInput = 'mt-1 w-full bg-transparent text-sm font-semibold text-ink-900 outline-none'

// Ô ảnh loại phòng kiểu tấm bìa: ảnh phủ kín, thông tin nổi trên gradient tối ở đáy
function RoomTile({ type, featured, wide, onBook }) {
  const meta = roomMeta(type)
  return (
    <div
      className={`group relative overflow-hidden rounded-2xl ${
        featured ? 'aspect-[4/3] lg:aspect-auto lg:h-full lg:min-h-[24rem]' : wide ? 'aspect-[21/9]' : 'aspect-[4/3]'
      }`}
    >
      <img
        src={roomImage(type, 0)}
        alt={type}
        loading="lazy"
        className={`absolute inset-0 h-full w-full object-cover ${EASE_SLOW} duration-[1.4s] group-hover:scale-[1.06]`}
      />
      <div className="absolute inset-0 bg-gradient-to-t from-ink-900/85 via-ink-900/10 to-transparent" />
      <div className="absolute inset-x-0 bottom-0 p-6">
        <h3 className="font-display text-2xl font-medium text-white">{type}</h3>
        <p className="mt-1 text-[12px] text-white/70">
          {meta.capacity} khách · {meta.area} m² · {formatVnd(TYPE_PRICES[type])}/đêm
        </p>
        <button
          onClick={() => onBook(type)}
          className={`mt-3 text-[11px] font-bold uppercase tracking-[0.16em] text-white underline-offset-4 ${EASE} hover:underline`}
        >
          Đặt phòng →
        </button>
      </div>
    </div>
  )
}

const EXPERIENCES = [
  { title: 'Nhà hàng', desc: 'Bữa sáng, bữa tối và đồ uống phục vụ tận phòng suốt kỳ lưu trú.', img: '/img/v1.jpg' },
  { title: 'Giặt ủi', desc: 'Nhận và trả trong ngày, áo sơ mi và quần âu được ủi phẳng.', img: '/img/v2.jpg' },
  { title: 'Đưa đón sân bay', desc: 'Xe riêng đón tận nơi, đặt trước qua lễ tân ít nhất 2 giờ.', img: '/img/v3.jpg' },
  { title: 'Dịch vụ phòng 24/7', desc: 'Gọi món, yêu cầu thêm khăn hay gối vào bất kỳ giờ nào.', img: '/img/suite.jpg' },
]

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

  const goStaff = () => navigate('/login')

  return (
    <div className="bg-cream-50">
      <div className="grain-overlay" />

      {/* ===== HERO ===== */}
      <section className="relative flex min-h-[100dvh] flex-col">
        <img src="/img/login-hero.jpg" alt="" className="absolute inset-0 h-full w-full object-cover" />
        <div className="absolute inset-0 bg-gradient-to-t from-ink-900/85 via-ink-900/25 to-ink-900/45" />

        <header className="relative z-10 flex items-center justify-between px-6 py-3.5 sm:px-12">
          <p className="font-display text-2xl font-semibold tracking-tight text-white">HSMS</p>
          <nav className="hidden items-center gap-8 text-[11px] font-semibold uppercase tracking-[0.2em] text-white/80 lg:flex">
            <a href="#rooms" className={`${EASE} hover:text-white`}>Phòng nghỉ</a>
            <a href="#services" className={`${EASE} hover:text-white`}>Dịch vụ</a>
            <a href="#footer" className={`${EASE} hover:text-white`}>Liên hệ</a>
          </nav>
          <div className="flex items-center gap-5">
            <span className="hidden text-[12px] font-medium tracking-wide text-white/80 sm:block">1900 636 999</span>
            <button
              onClick={goStaff}
              className={`rounded-full px-5 py-2 text-[11px] font-bold uppercase tracking-[0.15em] text-white ring-1 ring-white/40 ${EASE} hover:bg-white hover:text-ink-900`}
            >
              Nhân viên
            </button>
          </div>
        </header>

        <div className="relative z-10 flex flex-1 flex-col items-center justify-center px-6 text-center">
          <h1 className="max-w-4xl font-display text-5xl font-medium leading-[1.1] text-white [text-wrap:balance] sm:text-7xl">
            Kỳ nghỉ trọn vẹn bắt đầu từ đây
          </h1>
          <p className="mt-6 max-w-md text-[14px] leading-relaxed text-white/70">
            Chín phòng nghỉ được chăm chút từng chi tiết, cùng dịch vụ phục vụ tận nơi suốt kỳ lưu trú.
          </p>
        </div>

        <div className="relative z-10 px-4 pb-10 sm:px-12">
          <div className="mx-auto max-w-5xl bezel-shell">
            <div className="bezel-core grid grid-cols-2 overflow-hidden sm:grid-cols-[1fr_1fr_0.7fr_1fr_auto]">
              <div className="border-b border-r border-black/[0.06] px-5 py-4 sm:border-b-0">
                <p className={cellLabel}>Ngày nhận phòng</p>
                <input type="date" className={cellInput} value={checkIn} min={today()}
                  onChange={(e) => { setCheckIn(e.target.value); if (e.target.value >= checkOut) setCheckOut(addDays(e.target.value, 1)) }} />
              </div>
              <div className="border-b border-black/[0.06] px-5 py-4 sm:border-b-0 sm:border-r">
                <p className={cellLabel}>Ngày trả phòng</p>
                <input type="date" className={cellInput} value={checkOut} min={addDays(checkIn, 1)} onChange={(e) => setCheckOut(e.target.value)} />
              </div>
              <div className="border-r border-black/[0.06] px-5 py-4">
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
                className={`group/cta col-span-2 flex items-center justify-center gap-3 bg-brand-600 px-8 py-4 text-sm font-bold uppercase tracking-[0.18em] text-white ${EASE_SLOW} hover:bg-brand-700 active:scale-[0.98] sm:col-span-1`}
              >
                Đặt phòng
                <span className={`flex h-6 w-6 items-center justify-center rounded-full bg-white/15 ${EASE_SLOW} group-hover/cta:translate-x-0.5`}>↗</span>
              </button>
            </div>
          </div>
        </div>
      </section>

      {/* ===== TUYÊN NGÔN: một câu serif giữa trang, không eyebrow ===== */}
      <section className="mx-auto max-w-3xl px-6 py-28 text-center sm:py-40">
        <Reveal>
          <p className="font-display text-2xl font-medium leading-[1.3] text-ink-900 [text-wrap:balance] sm:text-[2rem]">
            Một khách sạn nhỏ vận hành như một lời hứa: phòng luôn sẵn sàng trước khi bạn đến,
            <em className="text-brand-600"> bữa sáng nóng</em> và
            <em className="text-brand-600"> áo sơ mi được ủi phẳng</em> trước khi bạn kịp nhớ ra mình cần chúng.
          </p>
          <div className="mx-auto mt-10 h-px w-16 bg-brand-600/40" />
        </Reveal>
      </section>

      {/* ===== PHÒNG NGHỈ: lưới bento bất đối xứng, Suite làm tâm điểm ===== */}
      <section id="rooms" className="mx-auto max-w-6xl px-6 pb-24 sm:px-12">
        <Reveal>
          <div className="mb-10 flex items-end justify-between gap-4">
            <div>
              <p className="text-[10px] font-bold uppercase tracking-[0.3em] text-brand-600">Phòng nghỉ</p>
              <h2 className="mt-3 font-display text-4xl font-medium tracking-tight">Chọn không gian của bạn</h2>
            </div>
            <p className="hidden shrink-0 text-[12px] text-ink-500 sm:block">Từ {formatVnd(500000)} một đêm</p>
          </div>
        </Reveal>

        <Reveal delay={80}>
          <div className="grid grid-cols-1 gap-5 lg:grid-cols-12 lg:gap-6">
            <div className="lg:col-span-7">
              <RoomTile type="Suite" featured onBook={book} />
            </div>
            <div className="flex flex-col gap-5 lg:col-span-5 lg:gap-6">
              <RoomTile type="Deluxe" onBook={book} />
              <RoomTile type="Standard" onBook={book} />
            </div>
            <div className="lg:col-span-12">
              <RoomTile type="Family Room" wide onBook={book} />
            </div>
          </div>
        </Reveal>
      </section>

      {/* ===== DỊCH VỤ: dải nền tối full width, dải ảnh cuộn ngang, không eyebrow ===== */}
      <section id="services" className="bg-ink-900 text-cream-50">
        <div className="mx-auto max-w-6xl px-6 py-28 sm:px-12 sm:py-36">
          <Reveal>
            <h2 className="max-w-lg font-display text-4xl font-medium tracking-tight [text-wrap:balance]">
              Mọi thứ đến tận cửa phòng
            </h2>
          </Reveal>

          <Reveal delay={120}>
            <div className="mt-14 flex snap-x snap-mandatory gap-5 overflow-x-auto pb-4 sm:gap-6">
              {EXPERIENCES.map((s) => (
                <div key={s.title} className="w-72 shrink-0 snap-start sm:w-80">
                  <div className="relative aspect-[4/5] overflow-hidden rounded-2xl ring-1 ring-white/10">
                    <img src={s.img} alt={s.title} loading="lazy" className="absolute inset-0 h-full w-full object-cover" />
                    <div className="absolute inset-0 bg-gradient-to-t from-ink-900/90 via-ink-900/15 to-transparent" />
                    <div className="absolute inset-x-0 bottom-0 p-5">
                      <p className="font-display text-xl font-medium text-white">{s.title}</p>
                      <p className="mt-2 text-[13px] leading-relaxed text-white/70">{s.desc}</p>
                    </div>
                  </div>
                </div>
              ))}
            </div>
          </Reveal>
        </div>
      </section>

      {/* ===== CTA ĐÓNG: dải ảnh full-bleed, một lời mời ngắn trước footer ===== */}
      <section className="relative flex min-h-[60vh] items-center justify-center overflow-hidden px-6 py-24 text-center">
        <img src="/img/v3.jpg" alt="" className="absolute inset-0 h-full w-full object-cover" />
        <div className="absolute inset-0 bg-ink-900/70" />
        <Reveal className="relative z-10">
          <h2 className="font-display text-3xl font-medium text-white [text-wrap:balance] sm:text-5xl">
            Kỳ nghỉ tiếp theo của bạn đang chờ
          </h2>
          <p className="mx-auto mt-4 max-w-sm text-[14px] text-white/70">
            Chọn ngày, chọn phòng, chúng tôi lo phần còn lại.
          </p>
          <div className="mt-8 flex justify-center">
            <BookButton onClick={() => book()} />
          </div>
        </Reveal>
      </section>

      {/* ===== FOOTER ===== */}
      <footer id="footer" className="bg-cream-50">
        <div className="mx-auto max-w-6xl px-6 pb-10 pt-20 sm:px-12">
          <div className="grid gap-10 border-b border-black/[0.07] pb-14 lg:grid-cols-3">
            <div>
              <p className="font-display text-3xl font-semibold tracking-tight">HSMS</p>
              <p className="mt-1 text-[9px] font-semibold tracking-[0.42em] text-ink-500">HOTEL &amp; SUITES · TỪ 2026</p>
            </div>
            <div className="text-[13px] leading-loose text-ink-700">
              <p className="mb-3 text-sm font-semibold text-ink-900">Liên hệ</p>
              <p>1900 636 999</p>
              <p>hsms@hotel.com</p>
            </div>
            <div className="text-[13px] leading-loose text-ink-700">
              <p className="mb-3 text-sm font-semibold text-ink-900">Khám phá</p>
              <a href="#rooms" className="block hover:text-brand-600">Phòng nghỉ</a>
              <a href="#services" className="block hover:text-brand-600">Dịch vụ</a>
              <button onClick={goStaff} className="block hover:text-brand-600">Nhân viên</button>
            </div>
          </div>
          <p className="pt-6 text-[11px] text-ink-500/60">Hotel & Service Management System</p>
          <p className="text-[11px] text-ink-500/60">Đồ án Software Engineering, Group 2, SE1919, FPT University, 2026</p>
        </div>
      </footer>
    </div>
  )
}
