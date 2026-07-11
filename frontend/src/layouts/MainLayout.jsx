import { useEffect, useState } from 'react'
import { NavLink, Outlet, useLocation, useNavigate } from 'react-router-dom'
import client, { isBackendMissing } from '../api/client'
import ErrorBoundary from '../components/ErrorBoundary'
import SlideOver from '../components/SlideOver'
import { useToast } from '../components/toastContext'
import { ROLE_LABEL, canAccess, homeFor } from '../utils/roles'
import { clearSession, getToken, getUser, saveSession } from '../utils/session'

const EASE = 'transition-all duration-300 ease-[cubic-bezier(0.32,0.72,0,1)]'
const inputCls =
  'w-full rounded-xl bg-white px-3.5 py-2.5 text-sm ring-1 ring-black/10 outline-none placeholder:text-ink-500/50 focus:ring-2 focus:ring-brand-500/40'
const labelCls = 'mb-1.5 block text-[12px] font-semibold text-ink-700'

// Form đổi mật khẩu trong ngăn kéo - POST /api/auth/change-password
// { currentPassword, newPassword, confirmPassword } -> 200 { message } | 400 { message }
function ChangePasswordDrawer({ open, onClose }) {
  const toast = useToast()
  const [form, setForm] = useState({ currentPassword: '', newPassword: '', confirmPassword: '' })
  const [error, setError] = useState('')
  const [saving, setSaving] = useState(false)

  // Mở lại thì xóa dữ liệu lần trước, không giữ mật khẩu cũ trong state
  useEffect(() => {
    if (open) { setForm({ currentPassword: '', newPassword: '', confirmPassword: '' }); setError('') }
  }, [open])

  const submit = (e) => {
    e.preventDefault()
    if (form.newPassword.length < 6) return setError('Mật khẩu mới phải từ 6 ký tự.')
    if (form.newPassword !== form.confirmPassword) return setError('Xác nhận mật khẩu chưa khớp.')

    setError('')
    setSaving(true)
    client
      .post('/api/auth/change-password', form)
      .then(() => {
        toast.success('Đã đổi mật khẩu')
        onClose()
      })
      .catch((err) => setError(
        isBackendMissing(err)
          ? 'Không kết nối được máy chủ. Vui lòng thử lại sau.'
          : err.response?.data?.message ?? 'Máy chủ báo lỗi. Thử lại sau ít phút.',
      ))
      .finally(() => setSaving(false))
  }

  return (
    <SlideOver open={open} eyebrow="tài khoản" title="Đổi mật khẩu" onClose={onClose}>
      <form onSubmit={submit} className="space-y-5">
        <div>
          <label htmlFor="pw-current" className={labelCls}>Mật khẩu hiện tại *</label>
          <input
            id="pw-current"
            type="password"
            autoComplete="current-password"
            className={inputCls}
            value={form.currentPassword}
            onChange={(e) => setForm({ ...form, currentPassword: e.target.value })}
          />
        </div>
        <div>
          <label htmlFor="pw-new" className={labelCls}>Mật khẩu mới *</label>
          <input
            id="pw-new"
            type="password"
            autoComplete="new-password"
            className={inputCls}
            value={form.newPassword}
            onChange={(e) => setForm({ ...form, newPassword: e.target.value })}
          />
        </div>
        <div>
          <label htmlFor="pw-confirm" className={labelCls}>Nhập lại mật khẩu mới *</label>
          <input
            id="pw-confirm"
            type="password"
            autoComplete="new-password"
            className={inputCls}
            value={form.confirmPassword}
            onChange={(e) => setForm({ ...form, confirmPassword: e.target.value })}
          />
        </div>

        {error && (
          <p className="rounded-lg bg-amber-50 px-3.5 py-2.5 text-[12px] font-medium text-amber-800 ring-1 ring-amber-600/15">{error}</p>
        )}

        <button
          type="submit"
          disabled={saving || !form.currentPassword || !form.newPassword || !form.confirmPassword}
          className={`w-full rounded-full bg-brand-600 py-3 text-[13px] font-bold text-white ${EASE} hover:bg-brand-700 active:scale-[0.98] disabled:opacity-40`}
        >
          {saving ? 'Đang đổi…' : 'Đổi mật khẩu'}
        </button>
      </form>
    </SlideOver>
  )
}

// `match` ghi đè active mặc định: mục Phòng sáng ở cả /rooms lẫn /rooms/types
// nhưng không được sáng ở /rooms/map (mục riêng của Sơ đồ phòng)
const MENU = [
  { to: '/dashboard', label: 'Tổng quan' },
  { to: '/rooms/map', label: 'Sơ đồ phòng' },
  { to: '/rooms', label: 'Phòng', match: (p) => p === '/rooms' || p.startsWith('/rooms/types') },
  { to: '/reservations', label: 'Đặt phòng', match: (p) => p.startsWith('/reservations') },
  { to: '/checkin-checkout', label: 'Check-in' },
  { to: '/service-orders', label: 'Dịch vụ' },
  { to: '/reports', label: 'Báo cáo' },
]

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
  const [menuOpen, setMenuOpen] = useState(false)
  const [pwOpen, setPwOpen] = useState(false)

  // Chuyển trang xong thì tự gập menu mobile lại
  useEffect(() => { setMenuOpen(false) }, [pathname])

  // Backend chạy thì làm mới thông tin user từ /api/auth/me; token hỏng sẽ bị
  // interceptor 401 đưa về /login. Backend chưa có endpoint -> giữ user lúc login.
  useEffect(() => {
    if (getUser()?.isDemo) return // phiên demo dùng token giả -> gọi /api/auth/me sẽ 401 và bị đá về /login
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

  // Menu chỉ hiện mục thuộc quyền xem của vai trò (nguồn: utils/roles.js)
  const visibleMenu = MENU.filter((item) => canAccess(user?.role, item.to))

  return (
    <div className="flex min-h-screen flex-col bg-cream-100">
      {/* Lớp hạt phim mỏng phủ toàn app - chất "quiet luxury" kiểu Amanoi */}
      <div className="grain-overlay" />
      {/* Nav ngang - logo vòm, menu gạch chân terracotta, avatar phải */}
      <header className="sticky top-0 z-20 border-b border-black/[0.06] bg-cream-50/95 backdrop-blur">
        <div className="mx-auto flex max-w-6xl items-center justify-between gap-4 px-6 py-3">
          <button onClick={() => navigate(homeFor(user?.role))} className="flex shrink-0 items-center gap-2.5">
            <span className="flex h-9 w-8 items-end justify-center rounded-t-full rounded-b-md bg-brand-600 pb-1.5 font-display text-[13px] font-bold text-white">
              H
            </span>
            <span className="text-left">
              <p className="font-display text-lg font-bold leading-none tracking-tight">HSMS</p>
              <p className="mt-0.5 text-[8px] font-bold tracking-[0.28em] text-brand-600">HOTEL & SERVICE</p>
            </span>
          </button>

          <nav className="hidden items-center gap-6 overflow-x-auto md:flex">
            {visibleMenu.map((item) => (
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
            {/* Phiên demo dùng token giả, gọi API đổi mật khẩu sẽ 401 -> ẩn luôn nút */}
            {!user?.isDemo && (
              <button
                onClick={() => setPwOpen(true)}
                title="Đổi mật khẩu"
                className={`hidden rounded-full px-3.5 py-1.5 text-[12px] font-semibold text-ink-500 ring-1 ring-black/10 ${EASE} hover:bg-white hover:text-ink-900 sm:block`}
              >
                Đổi mật khẩu
              </button>
            )}
            <button
              onClick={logout}
              className={`hidden rounded-full px-3.5 py-1.5 text-[12px] font-semibold text-ink-500 ring-1 ring-black/10 ${EASE} hover:bg-white hover:text-ink-900 sm:block`}
            >
              Đăng xuất
            </button>
            {/* Nút mở menu trên màn hình nhỏ - nav ngang bị ẩn dưới md */}
            <button
              onClick={() => setMenuOpen(!menuOpen)}
              aria-label={menuOpen ? 'Đóng menu' : 'Mở menu'}
              aria-expanded={menuOpen}
              className={`flex h-9 w-9 items-center justify-center rounded-full text-ink-700 ring-1 ring-black/10 ${EASE} hover:bg-white md:hidden`}
            >
              <svg width="16" height="16" viewBox="0 0 16 16" fill="none" aria-hidden>
                {menuOpen ? (
                  <path d="M3 3l10 10M13 3L3 13" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" />
                ) : (
                  <path d="M2 4.5h12M2 8h12M2 11.5h12" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" />
                )}
              </svg>
            </button>
          </div>
        </div>

        {/* Menu mobile xổ xuống dưới header - cùng bộ mục đã lọc theo vai trò */}
        {menuOpen && (
          <nav className="card-rise border-t border-black/[0.06] bg-cream-50 px-3 pb-3 pt-1.5 md:hidden">
            {visibleMenu.map((item) => (
              <NavLink
                key={item.to}
                to={item.to}
                end={item.to === '/reservations/new'}
                className={({ isActive }) =>
                  `block rounded-xl px-3.5 py-2.5 text-sm ${EASE} ${
                    (item.match ? item.match(pathname) : isActive)
                      ? 'bg-white font-bold text-ink-900 shadow-soft'
                      : 'font-medium text-ink-500 hover:text-ink-900'
                  }`
                }
              >
                {item.label}
              </NavLink>
            ))}
            {!user?.isDemo && (
              <button
                onClick={() => setPwOpen(true)}
                className={`mt-1.5 block w-full rounded-xl border-t border-black/[0.06] px-3.5 pb-2 pt-3 text-left text-sm font-semibold text-ink-500 ${EASE} hover:text-ink-900`}
              >
                Đổi mật khẩu
              </button>
            )}
            <button
              onClick={logout}
              className={`block w-full rounded-xl px-3.5 py-2.5 text-left text-sm font-semibold text-ink-500 ${EASE} hover:text-ink-900 sm:hidden`}
            >
              Đăng xuất
            </button>
          </nav>
        )}
      </header>

      <main className="mx-auto w-full max-w-6xl flex-1 px-6 py-8">
        {/* Trang con hỏng thì chỉ vùng này báo lỗi, nav + header vẫn dùng được */}
        <ErrorBoundary resetKey={pathname}>
          <Outlet />
        </ErrorBoundary>
      </main>

      <ChangePasswordDrawer open={pwOpen} onClose={() => setPwOpen(false)} />
    </div>
  )
}
