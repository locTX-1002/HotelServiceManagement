import { useState } from 'react'
import { Link, Navigate, useLocation, useNavigate } from 'react-router-dom'
import { EASE, errorCls } from '../../utils/ui'
import guestClient, { isBackendMissing } from '../../api/guestClient'
import { getGuestToken, saveGuestSession } from '../../utils/guestSession'

const inputCls =
  'w-full rounded-lg border border-black/15 bg-white px-3.5 py-3 text-sm outline-none placeholder:text-ink-500/40 focus:border-brand-500 focus:ring-2 focus:ring-brand-500/20'
const labelCls = 'mb-1.5 block text-[11px] font-bold uppercase tracking-[0.18em] text-ink-700'

// Dang nhap cho khach (guest portal) - phong theo dung mau LoginPage.jsx cua nhan vien nhung
// tach rieng session/API, dang nhap bang SDT (khach khong co email luc dat phong).
export default function GuestLoginPage() {
  const [phoneNumber, setPhoneNumber] = useState('')
  const [password, setPassword] = useState('')
  const [showPassword, setShowPassword] = useState(false)
  const [error, setError] = useState('')
  const [loading, setLoading] = useState(false)
  const navigate = useNavigate()
  const location = useLocation()
  const from = location.state?.from ?? null

  if (getGuestToken()) return <Navigate to={from ?? '/guest/dashboard'} replace />

  const onSubmit = (e) => {
    e.preventDefault()
    if (loading) return
    setError('')
    setLoading(true)
    guestClient
      .post('/api/guest/auth/login', { phoneNumber: phoneNumber.trim(), password })
      .then((res) => {
        saveGuestSession(res.data)
        navigate(from ?? '/guest/dashboard', { replace: true })
      })
      .catch((err) => {
        if (err.response?.status === 401) setError('Số điện thoại hoặc mật khẩu không đúng.')
        else if (isBackendMissing(err)) setError('Không kết nối được máy chủ. Vui lòng thử lại sau.')
        else setError(err.response?.data?.message ?? 'Máy chủ báo lỗi. Thử lại sau ít phút.')
      })
      .finally(() => setLoading(false))
  }

  return (
    <div className="relative flex min-h-screen flex-col items-center bg-cream-100 px-6">
      <div className="grain-overlay" />
      <Link
        to="/"
        className={`absolute left-5 top-5 flex items-center gap-1.5 text-[12px] font-semibold text-ink-500 ${EASE} hover:text-ink-900`}
      >
        <svg width="12" height="12" viewBox="0 0 16 16" fill="none" aria-hidden>
          <path d="M10 3L5 8L10 13" stroke="currentColor" strokeWidth="1.6" strokeLinecap="round" strokeLinejoin="round" />
        </svg>
        Trang chủ
      </Link>
      <div className="flex w-full max-w-md flex-1 flex-col justify-center py-10">
        <div className="bezel-shell">
          <div className="bezel-core px-7 py-9 sm:px-10 sm:py-12">
            <div className="flex flex-col items-center text-center">
              <span className="flex h-12 w-10 items-end justify-center rounded-t-full rounded-b-md bg-brand-600 pb-2 font-display text-lg font-bold text-white">
                H
              </span>
              <p className="mt-3 font-display text-3xl font-semibold tracking-tight">HSMS</p>
              <p className="mt-1 text-[9px] font-semibold tracking-[0.32em] text-ink-500">CỔNG THÔNG TIN KHÁCH LƯU TRÚ</p>
            </div>

            <h1 className="mt-10 font-display text-4xl font-medium tracking-tight">Xin chào</h1>
            <p className="mt-2 text-sm leading-relaxed text-ink-500">
              Đăng nhập để xem thông tin đặt phòng của bạn.
            </p>

            <form onSubmit={onSubmit} className="mt-8 space-y-5">
              <div>
                <label className={labelCls}>Số điện thoại</label>
                <input
                  type="tel"
                  required
                  autoComplete="tel"
                  className={inputCls}
                  placeholder="09xxxxxxxx"
                  value={phoneNumber}
                  onChange={(e) => setPhoneNumber(e.target.value)}
                />
              </div>
              <div>
                <label className={labelCls}>Mật khẩu</label>
                <div className="relative">
                  <input
                    type={showPassword ? 'text' : 'password'}
                    required
                    autoComplete="current-password"
                    className={`${inputCls} pr-16`}
                    placeholder="••••••"
                    value={password}
                    onChange={(e) => setPassword(e.target.value)}
                  />
                  <button
                    type="button"
                    onClick={() => setShowPassword(!showPassword)}
                    className={`absolute inset-y-0 right-0 flex items-center px-3.5 text-[11px] font-bold uppercase tracking-wider text-ink-500 ${EASE} hover:text-ink-900`}
                  >
                    {showPassword ? 'Ẩn' : 'Hiện'}
                  </button>
                </div>
              </div>

              {error && <p className={errorCls}>{error}</p>}

              <button
                type="submit"
                disabled={loading}
                className={`inline-flex items-center gap-2.5 rounded-full bg-brand-600 px-8 py-3 text-[13px] font-bold uppercase tracking-[0.12em] text-white ${EASE} hover:bg-brand-700 active:scale-[0.98] disabled:opacity-50`}
              >
                {loading ? 'Đang đăng nhập…' : <>Đăng nhập <span aria-hidden>›</span></>}
              </button>
            </form>

            <div className="mt-8 space-y-2 border-t border-black/[0.07] pt-4 text-[12px] leading-relaxed text-ink-500">
              <p>
                Chưa có tài khoản?{' '}
                <Link to="/guest/dang-ky" className={`font-semibold text-brand-600 underline-offset-4 ${EASE} hover:underline`}>
                  Đăng ký ngay
                </Link>
              </p>
              <p>
                Quên mật khẩu?{' '}
                <Link to="/guest/quen-mat-khau" className={`font-semibold text-brand-600 underline-offset-4 ${EASE} hover:underline`}>
                  Đặt lại qua email
                </Link>
              </p>
            </div>
          </div>
        </div>
      </div>
      <p className="pb-5 text-[11px] text-ink-500/60">Group 2 · SE1919 · FPT University</p>
    </div>
  )
}
