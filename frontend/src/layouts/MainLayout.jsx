import { useEffect, useRef, useState } from 'react'
import { EASE, errorCls, initials, inputCls, labelCls } from '../utils/ui'
import { NavLink, Outlet, useLocation, useNavigate } from 'react-router-dom'
import client, { isBackendMissing } from '../api/client'
import ErrorBoundary from '../components/ErrorBoundary'
import SlideOver from '../components/SlideOver'
import { useToast } from '../components/toastContext'
import { ROLE_LABEL, canAccess, homeFor } from '../utils/roles'
import { clearSession, getRefreshToken, getToken, getUser, saveSession } from '../utils/session'

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
          <p className={errorCls}>{error}</p>
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
// nhưng không được sáng ở /rooms/map (mục riêng của Sơ đồ phòng).
// 10 route giờ đã có -> gom theo luồng nghiệp vụ thành dropdown, chỉ 2 mục hay dùng nhất
// (Tổng quan, Sơ đồ phòng) đứng riêng, tránh nav tràn ngang phải cuộn ngang xấu.
const MENU = [
  { to: '/dashboard', label: 'Tổng quan' },
  { to: '/rooms/map', label: 'Sơ đồ phòng' },
  {
    label: 'Vận hành',
    match: (p) => p.startsWith('/reservations') || p === '/checkin-checkout' || p === '/guests',
    items: [
      { to: '/reservations', label: 'Đặt phòng', match: (p) => p.startsWith('/reservations') },
      { to: '/checkin-checkout', label: 'Check-in / Check-out' },
      { to: '/guests', label: 'Khách' },
    ],
  },
  {
    label: 'Dịch vụ',
    match: (p) => p === '/service-orders' || p === '/service-items' || p === '/surcharge-items',
    items: [
      { to: '/service-orders', label: 'Gọi dịch vụ' },
      { to: '/service-items', label: 'Bảng giá dịch vụ' },
      { to: '/surcharge-items', label: 'Giá phụ thu' },
    ],
  },
  {
    label: 'Quản lý',
    match: (p) => p === '/rooms' || p.startsWith('/rooms/types') || p === '/promotions' || p === '/reports' || p === '/users',
    items: [
      { to: '/rooms', label: 'Phòng', match: (p) => p === '/rooms' || p.startsWith('/rooms/types') },
      { to: '/promotions', label: 'Khuyến mãi' },
      { to: '/reports', label: 'Báo cáo' },
      { to: '/users', label: 'Người dùng' },
    ],
  },
]

/* Mục nav dạng dropdown - gom nhiều route liên quan dưới 1 nhãn, sáng lên nếu đang ở trang con bất kỳ */
function NavGroup({ label, items, active, mobile, onNavigate, pathname }) {
  const [open, setOpen] = useState(false)
  const ref = useRef(null)

  useEffect(() => {
    if (!open) return
    const onDocClick = (e) => { if (ref.current && !ref.current.contains(e.target)) setOpen(false) }
    const onKey = (e) => e.key === 'Escape' && setOpen(false)
    document.addEventListener('mousedown', onDocClick)
    document.addEventListener('keydown', onKey)
    return () => {
      document.removeEventListener('mousedown', onDocClick)
      document.removeEventListener('keydown', onKey)
    }
  }, [open])

  if (mobile) {
    return (
      <div className="py-1">
        <p className={`px-3.5 pb-1 pt-2 text-[10px] font-bold uppercase tracking-[0.18em] ${active ? 'text-brand-600' : 'text-ink-500/70'}`}>{label}</p>
        {items.map((item) => (
          <NavLink
            key={item.to}
            to={item.to}
            onClick={onNavigate}
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
      </div>
    )
  }

  return (
    <div ref={ref} className="relative">
      <button
        onClick={() => setOpen((o) => !o)}
        aria-expanded={open}
        className={`flex items-center gap-1 whitespace-nowrap border-b-2 pb-0.5 text-[13.5px] ${EASE} ${
          active ? 'border-brand-600 font-bold text-ink-900' : 'border-transparent font-medium text-ink-500 hover:text-ink-900'
        }`}
      >
        {label}
        <svg width="9" height="9" viewBox="0 0 9 9" fill="none" className={`mt-0.5 ${EASE} ${open ? 'rotate-180' : ''}`} aria-hidden>
          <path d="M1.5 3L4.5 6L7.5 3" stroke="currentColor" strokeWidth="1.4" strokeLinecap="round" strokeLinejoin="round" />
        </svg>
      </button>
      {open && (
        <div className="card-rise absolute left-1/2 top-full z-30 mt-2.5 w-48 -translate-x-1/2 rounded-xl bg-white p-1.5 shadow-lift ring-1 ring-black/[0.06]">
          {items.map((item) => (
            <NavLink
              key={item.to}
              to={item.to}
              onClick={() => setOpen(false)}
              className={({ isActive }) =>
                `block rounded-lg px-3 py-2 text-[13px] ${EASE} ${
                  (item.match ? item.match(pathname) : isActive)
                    ? 'bg-brand-50 font-bold text-brand-700'
                    : 'font-medium text-ink-700 hover:bg-cream-100'
                }`
              }
            >
              {item.label}
            </NavLink>
          ))}
        </div>
      )}
    </div>
  )
}

export default function MainLayout() {
  const navigate = useNavigate()
  const { pathname } = useLocation()
  const [user, setUser] = useState(getUser)
  const [menuOpen, setMenuOpen] = useState(false)
  const [pwOpen, setPwOpen] = useState(false)

  // Chuyển trang xong thì tự gập menu mobile lại
  useEffect(() => { setMenuOpen(false) }, [pathname])

  // Làm mới thông tin user từ /api/auth/me; token hỏng sẽ bị interceptor 401 đưa về /login.
  // Backend chưa chạy / mất mạng -> giữ nguyên user đã lưu lúc login.
  useEffect(() => {
    client
      .get('/api/auth/me')
      .then((res) => {
        const me = res.data?.user ?? res.data
        // getToken() kiểm tra lại: nếu user đã đăng xuất trong lúc chờ mạng thì bỏ qua,
        // không được ghi đè phiên rỗng bằng dữ liệu cũ
        if (me?.fullName && getToken()) { setUser(me); saveSession(getToken(), getRefreshToken(), me) }
      })
      .catch(() => {})
  }, [])

  const logout = () => {
    // Best-effort: thu hồi refresh token phía server để không ai dùng lại được, nhưng không chặn
    // việc đăng xuất nếu mạng lỗi - vẫn xoá phiên local và điều hướng ngay.
    const refreshToken = getRefreshToken()
    if (refreshToken) client.post('/api/auth/logout', { refreshToken }).catch(() => {})
    clearSession()
    navigate('/login')
  }

  // Menu chỉ hiện mục thuộc quyền xem của vai trò (nguồn: utils/roles.js).
  // Nhóm dropdown mà lọc quyền chỉ còn đúng 1 mục thì hiện thẳng luôn, khỏi làm dropdown 1 dòng vô nghĩa.
  const visibleMenu = MENU.flatMap((item) => {
    if (item.items) {
      const items = item.items.filter((sub) => canAccess(user?.role, sub.to))
      if (items.length === 0) return []
      if (items.length === 1) return [items[0]]
      return [{ ...item, items }]
    }
    return canAccess(user?.role, item.to) ? [item] : []
  })

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

          <nav className="hidden items-center gap-6 md:flex">
            {visibleMenu.map((item) =>
              item.items ? (
                <NavGroup key={item.label} label={item.label} items={item.items} active={item.match(pathname)} pathname={pathname} />
              ) : (
                <NavLink
                  key={item.to}
                  to={item.to}
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
              ),
            )}
          </nav>

          <div className="flex shrink-0 items-center gap-2.5">
            {/* Trong app không còn cách nào quay lại trang chủ công khai - mở tab mới để không mất phiên đang thao tác */}
            <a
              href="/"
              target="_blank"
              rel="noreferrer"
              title="Xem trang chủ"
              aria-label="Xem trang chủ"
              className={`hidden h-9 w-9 items-center justify-center rounded-full text-ink-500 ring-1 ring-black/10 ${EASE} hover:bg-white hover:text-ink-900 sm:flex`}
            >
              <svg width="15" height="15" viewBox="0 0 16 16" fill="none" aria-hidden>
                <path d="M2 7.5L8 2.5L14 7.5" stroke="currentColor" strokeWidth="1.4" strokeLinecap="round" strokeLinejoin="round" />
                <path d="M3.5 6.5V13H12.5V6.5" stroke="currentColor" strokeWidth="1.4" strokeLinecap="round" strokeLinejoin="round" />
              </svg>
            </a>
            <span
              title={user ? `${user.fullName} · ${ROLE_LABEL[user.role] ?? user.role}` : 'Chưa đăng nhập'}
              className="flex h-9 w-9 items-center justify-center rounded-full bg-emerald-900 text-[11px] font-bold text-cream-50"
            >
              {initials(user?.fullName)}
            </span>
            <span className="hidden text-left leading-tight lg:block">
              <p className="max-w-36 truncate text-[12px] font-bold text-ink-900">{user?.fullName ?? 'Nhân viên'}</p>
              <p className="text-[10px] font-semibold uppercase tracking-[0.14em] text-ink-500">
                {ROLE_LABEL[user?.role] ?? user?.role ?? '—'}
              </p>
            </span>
            <button
              onClick={() => setPwOpen(true)}
              title="Đổi mật khẩu"
              className={`hidden rounded-full px-3.5 py-1.5 text-[12px] font-semibold text-ink-500 ring-1 ring-black/10 ${EASE} hover:bg-white hover:text-ink-900 sm:block`}
            >
              Đổi mật khẩu
            </button>
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
            {visibleMenu.map((item) =>
              item.items ? (
                <NavGroup key={item.label} label={item.label} items={item.items} active={item.match(pathname)} pathname={pathname} mobile />
              ) : (
                <NavLink
                  key={item.to}
                  to={item.to}
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
              ),
            )}
            <a
              href="/"
              target="_blank"
              rel="noreferrer"
              className={`mt-1.5 block w-full rounded-xl border-t border-black/[0.06] px-3.5 pb-2 pt-3 text-left text-sm font-semibold text-ink-500 ${EASE} hover:text-ink-900 sm:hidden`}
            >
              Xem trang chủ ↗
            </a>
            {/* sm:hidden giống nút Đăng xuất: từ sm trở lên đã có nút trên header, tránh hiện đúp */}
            <button
              onClick={() => setPwOpen(true)}
              className={`block w-full rounded-xl px-3.5 py-2.5 text-left text-sm font-semibold text-ink-500 ${EASE} hover:text-ink-900 sm:hidden`}
            >
              Đổi mật khẩu
            </button>
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
