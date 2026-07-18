import { Link, Outlet, useNavigate } from 'react-router-dom'
import { EASE } from '../utils/ui'
import guestClient from '../api/guestClient'
import { clearGuestSession, getGuest, getGuestRefreshToken } from '../utils/guestSession'

// Shell rieng cho khu vuc khach (guest portal) - CHU Y khong dung MainLayout: MainLayout gan voi
// phien/quyen nhan vien (RequireRole, danh sach menu theo Role), khong lien quan gi toi khach.
export default function GuestLayout() {
  const navigate = useNavigate()
  const guest = getGuest()

  const logout = () => {
    const refreshToken = getGuestRefreshToken()
    if (refreshToken) guestClient.post('/api/guest/auth/logout', { refreshToken }).catch(() => {})
    clearGuestSession()
    navigate('/guest/dang-nhap')
  }

  return (
    <div className="min-h-screen bg-cream-100">
      <div className="grain-overlay" />
      <header className="border-b border-black/[0.07] bg-cream-50/80 backdrop-blur">
        <div className="mx-auto flex max-w-3xl items-center justify-between px-6 py-4">
          <Link to="/" className={`flex items-center gap-2 font-display text-lg font-semibold ${EASE} hover:opacity-70`}>
            <span className="flex h-8 w-7 items-end justify-center rounded-t-full rounded-b-md bg-brand-600 pb-1.5 font-display text-sm font-bold text-white">
              H
            </span>
            HSMS
          </Link>
          <div className="flex items-center gap-4">
            {guest && <span className="text-[13px] font-medium text-ink-700">{guest.fullName}</span>}
            <button
              type="button"
              onClick={logout}
              className={`rounded-full px-4 py-2 text-[12px] font-bold uppercase tracking-wider text-ink-700 ring-1 ring-black/10 ${EASE} hover:bg-white`}
            >
              Đăng xuất
            </button>
          </div>
        </div>
      </header>
      <main className="mx-auto max-w-3xl px-6 py-10">
        <Outlet />
      </main>
    </div>
  )
}
