import { NavLink, Outlet, useNavigate } from 'react-router-dom'

const EASE = 'transition-all duration-300 ease-[cubic-bezier(0.32,0.72,0,1)]'

const MENU = [
  { to: '/dashboard', label: 'Tổng quan' },
  { to: '/rooms/map', label: 'Sơ đồ phòng' },
  { to: '/rooms', label: 'Phòng & loại phòng' },
  { to: '/reservations', label: 'Đặt phòng' },
  { to: '/reservations/new', label: 'Tạo đặt phòng' },
  { to: '/checkin-checkout', label: 'Check-in / Check-out' },
  { to: '/service-orders', label: 'Dịch vụ' },
  { to: '/reports', label: 'Báo cáo' },
]

const todayLabel = new Intl.DateTimeFormat('vi-VN', { weekday: 'long', day: '2-digit', month: '2-digit', year: 'numeric' }).format(new Date())

export default function MainLayout() {
  const navigate = useNavigate()

  const logout = () => {
    localStorage.removeItem('token')
    navigate('/login')
  }

  return (
    <div className="flex min-h-screen">
      <aside className="flex w-60 shrink-0 flex-col bg-ink-900 text-cream-50">
        <div className="px-5 pb-4 pt-6">
          <p className="text-lg font-extrabold tracking-tight">HSMS</p>
          <p className="text-[11px] font-medium text-cream-50/50">Hotel & Service Management</p>
        </div>
        <nav className="flex flex-1 flex-col gap-1 px-3">
          {MENU.map((item) => (
            <NavLink
              key={item.to}
              to={item.to}
              end={item.to === '/reservations' || item.to === '/rooms'}
              className={({ isActive }) =>
                `rounded-xl px-3.5 py-2.5 text-[13px] font-medium ${EASE} ${
                  isActive ? 'bg-white/10 font-semibold text-white' : 'text-cream-50/70 hover:bg-white/5 hover:text-white'
                }`
              }
            >
              {item.label}
            </NavLink>
          ))}
        </nav>
        <div className="px-5 py-4 text-[11px] text-cream-50/40">Group 2 · SE1919</div>
      </aside>

      <div className="flex min-w-0 flex-1 flex-col">
        <header className="sticky top-0 z-10 flex items-center justify-between border-b border-black/5 bg-cream-50/90 px-6 py-3 backdrop-blur">
          <p className="text-sm font-medium capitalize text-ink-500">{todayLabel}</p>
          <div className="flex items-center gap-3">
            <div className="flex items-center gap-2.5">
              <span className="flex h-8 w-8 items-center justify-center rounded-full bg-brand-50 text-[11px] font-bold text-brand-700 ring-1 ring-brand-600/15">LT</span>
              <span className="hidden text-sm font-semibold sm:block">Receptionist Demo</span>
            </div>
            <button
              onClick={logout}
              className={`rounded-full px-4 py-1.5 text-[13px] font-semibold text-ink-700 ring-1 ring-black/10 ${EASE} hover:bg-white active:scale-[0.98]`}
            >
              Đăng xuất
            </button>
          </div>
        </header>
        <main className="flex-1 px-6 py-7">
          <Outlet />
        </main>
      </div>
    </div>
  )
}
