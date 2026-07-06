import { useEffect, useState } from 'react'
import { NavLink, Outlet, useLocation, useNavigate } from 'react-router-dom'
import client from '../api/client'
import { clearSession, getToken, getUser, saveSession } from '../utils/session'

const EASE = 'transition-all duration-300 ease-[cubic-bezier(0.32,0.72,0,1)]'

// `match` ghi đè active mặc định: mục Phòng sáng ở cả /rooms lẫn /rooms/types
// nhưng không được sáng ở /rooms/map (mục riêng của Sơ đồ phòng)
const MENU = [
  { to: '/dashboard', label: 'Tổng quan' },
  { to: '/rooms/map', label: 'Sơ đồ phòng' },
  { to: '/rooms', label: 'Phòng', match: (p) => p === '/rooms' || p.startsWith('/rooms/types') },
  { to: '/reservations/new', label: 'Đặt phòng' },
  { to: '/checkin-checkout', label: 'Check-in' },
  { to: '/service-orders', label: 'Dịch vụ' },
  { to: '/reports', label: 'Báo cáo' },
]

// Nhãn vai trò tiếng Việt cho 4 role seed sẵn của backend
const ROLE_LABEL = { Admin: 'Quản trị', Manager: 'Quản lý', Receptionist: 'Lễ tân', ServiceStaff: 'NV dịch vụ' }

// 'Nguyễn Văn An' -> 'NA' cho avatar; tên 1 chữ thì lấy 2 ký tự đầu
const initials = (name) => {
  const p = (name ?? '').trim().split(/\s+/).filter(Boolean)
  if (p.length === 0) return 'NV'
  if (p.length === 1) return p[0].slice(0, 2).toUpperCase()
  return (p[0][0] + p[p.length - 1][0]).toUpperCase()
}

export default function MainLayout() {
  const navigate = useNavigate()
  const { pathname } = useLocation()
  const [user, setUser] = useState(getUser)

  // Backend chạy thì làm mới thông tin user từ /api/auth/me; token hỏng sẽ bị
  // interceptor 401 đưa về /login. Backend chưa có endpoint -> giữ user lúc login.
  useEffect(() => {
    client
      .get('/api/auth/me')
      .then((res) => {
        const me = res.data?.user ?? res.data
        // getToken() kiểm tra lại: nếu user đã đăng xuất trong lúc chờ mạng thì bỏ qua,
        // không được ghi đè phiên rỗng bằng dữ liệu cũ
        if (me?.fullName && getToken()) { setUser(me); saveSession(getToken(), me) }
      })
      .catch(() => {})
  }, [])

  const logout = () => {
    clearSession()
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
                    (item.match ? item.match(pathname) : isActive)
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
              title={user ? `${user.fullName} · ${ROLE_LABEL[user.role] ?? user.role}` : 'Chưa đăng nhập'}
              className="flex h-9 w-9 items-center justify-center rounded-full bg-emerald-900 text-[11px] font-bold text-cream-50"
            >
              {initials(user?.fullName)}
            </span>
            <span className="hidden text-left leading-tight lg:block">
              <p className="max-w-36 truncate text-[12px] font-bold text-ink-900">{user?.fullName ?? 'Nhân viên'}</p>
              <p className="text-[10px] font-semibold uppercase tracking-[0.14em] text-ink-500">
                {ROLE_LABEL[user?.role] ?? user?.role ?? '—'}{user?.isDemo ? ' · demo' : ''}
              </p>
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
