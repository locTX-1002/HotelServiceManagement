import { NavLink, Outlet, useNavigate } from 'react-router-dom'

const EASE = 'transition-all duration-300 ease-[cubic-bezier(0.32,0.72,0,1)]'

const MENU = [
  { to: '/dashboard', label: 'Tổng quan' },
  { to: '/rooms/map', label: 'Sơ đồ phòng' },
  { to: '/reservations/new', label: 'Đặt phòng' },
  { to: '/checkin-checkout', label: 'Check-in' },
  { to: '/service-orders', label: 'Dịch vụ' },
  { to: '/reports', label: 'Báo cáo' },
]

export default function MainLayout() {
  const navigate = useNavigate()

  const logout = () => {
    localStorage.removeItem('token')
    navigate('/login')
  }

  return (
    <div className="flex min-h-screen flex-col bg-cream-100">
      {/* Nav ngang - logo vòm, menu gạch chân terracotta, avatar phải */}
      <header className="sticky top-0 z-20 border-b border-black/[0.06] bg-cream-50/95 backdrop-blur">
        <div className="mx-auto flex max-w-6xl items-center justify-between gap-4 px-6 py-3">
          <button onClick={() => navigate('/dashboard')} className="flex shrink-0 items-center gap-2.5">
            <span className="flex h-9 w-8 items-end justify-center rounded-t-full rounded-b-md bg-brand-600 pb-1.5 font-display text-[13px] font-bold text-white">
              H
            </span>
            <span className="text-left">
              <p className="font-display text-lg font-bold leading-none tracking-tight">HSMS</p>
              <p className="mt-0.5 text-[8px] font-bold tracking-[0.28em] text-brand-600">HOTEL & SERVICE</p>
            </span>
          </button>

          <nav className="hidden items-center gap-6 overflow-x-auto md:flex">
            {MENU.map((item) => (
              <NavLink
                key={item.to}
                to={item.to}
                end={item.to === '/reservations/new'}
                className={({ isActive }) =>
                  `whitespace-nowrap border-b-2 pb-0.5 text-[13.5px] ${EASE} ${
                    isActive
                      ? 'border-brand-600 font-bold text-ink-900'
                      : 'border-transparent font-medium text-ink-500 hover:text-ink-900'
                  }`
                }
              >
                {item.label}
              </NavLink>
            ))}
          </nav>

          <div className="flex shrink-0 items-center gap-2.5">
            <span
              title="Receptionist Demo"
              className="flex h-9 w-9 items-center justify-center rounded-full bg-emerald-900 text-[11px] font-bold text-cream-50"
            >
              LT
            </span>
            <button
              onClick={logout}
              className={`hidden rounded-full px-3.5 py-1.5 text-[12px] font-semibold text-ink-500 ring-1 ring-black/10 ${EASE} hover:bg-white hover:text-ink-900 sm:block`}
            >
              Đăng xuất
            </button>
          </div>
        </div>
      </header>

      <main className="mx-auto w-full max-w-6xl flex-1 px-6 py-8">
        <Outlet />
      </main>
    </div>
  )
}
