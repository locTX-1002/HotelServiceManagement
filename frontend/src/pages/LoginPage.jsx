import { useState } from 'react'
import { Navigate, useLocation, useNavigate } from 'react-router-dom'
import client, { isBackendMissing } from '../api/client'
import { homeFor } from '../utils/roles'
import { getToken, getUser, saveSession, startDemoSession } from '../utils/session'

const EASE = 'transition-all duration-300 ease-[cubic-bezier(0.32,0.72,0,1)]'
const inputCls =
  'w-full rounded-lg border border-black/15 bg-white px-3.5 py-3 text-sm outline-none placeholder:text-ink-500/40 focus:border-brand-500 focus:ring-2 focus:ring-brand-500/20'
const labelCls = 'mb-1.5 block text-[11px] font-bold uppercase tracking-[0.18em] text-ink-700'

// Login tối giản nền trắng theo mẫu booking engine - POST /api/auth/login, lưu phiên vào localStorage
export default function LoginPage() {
  const [email, setEmail] = useState('receptionist@hotel.com')
  const [password, setPassword] = useState('')
  const [showPassword, setShowPassword] = useState(false)
  const [error, setError] = useState('')
  const [loading, setLoading] = useState(false)
  const navigate = useNavigate()
  const location = useLocation()
  const from = location.state?.from ?? null

  // Còn phiên cũ (vd bấm nút back) thì khỏi đăng nhập lại
  if (getToken()) return <Navigate to={homeFor(getUser()?.role)} replace />

  const onSubmit = (e) => {
    e.preventDefault()
    if (loading) return
    setError('')
    setLoading(true)
    client
      .post('/api/auth/login', { email: email.trim(), password })
      .then((res) => {
        const { token, user } = res.data ?? {}
        // Không lưu phiên hỏng nếu backend trả 200 mà thiếu token
        if (!token) return setError('Máy chủ trả về thiếu token — báo backend kiểm tra /api/auth/login.')
        saveSession(token, user)
        // Về trang bị chặn trước đó, không có thì về trang chính theo vai trò
        navigate(from ?? homeFor(user?.role), { replace: true })
      })
      .catch((err) => {
        if (err.response?.status === 401) setError('Email hoặc mật khẩu không đúng.')
        else if (isBackendMissing(err)) setError('API đăng nhập chưa sẵn sàng (T2 backend). Dùng "Vào thẳng" bên dưới để xem giao diện.')
        else setError(err.response?.data?.message ?? 'Máy chủ báo lỗi. Thử lại sau ít phút.')
      })
      .finally(() => setLoading(false))
  }

  // Phiên demo để xem UI khi backend chưa có login - token giả sẽ bị 401 khi backend bật auth
  const enterDemo = () => {
    startDemoSession()
    navigate(from ?? '/dashboard', { replace: true })
  }

  return (
    <div className="flex min-h-screen flex-col items-center bg-cream-50 px-6">
      <div className="flex w-full max-w-sm flex-1 flex-col justify-center py-10">
        {/* Logo vòm đồng bộ với nav */}
        <div className="flex flex-col items-center text-center">
          <span className="flex h-12 w-10 items-end justify-center rounded-t-full rounded-b-md bg-brand-600 pb-2 font-display text-lg font-bold text-white">
            H
          </span>
          <p className="mt-3 font-display text-3xl font-semibold tracking-tight">HSMS</p>
          <p className="mt-1 text-[9px] font-semibold tracking-[0.32em] text-ink-500">★★★★★ HOTEL & SERVICE MANAGEMENT</p>
        </div>

        <h1 className="mt-10 text-2xl font-extrabold tracking-tight">Chào mừng trở lại!</h1>
        <p className="mt-2 text-sm leading-relaxed text-ink-500">
          Đăng nhập bằng tài khoản nhân viên để bắt đầu ca làm việc và truy cập mọi chức năng theo vai trò của bạn.
        </p>

        <form onSubmit={onSubmit} className="mt-8 space-y-5">
          <div>
            <label className={labelCls}>E-mail address</label>
            <input
              type="email"
              required
              autoComplete="email"
              className={inputCls}
              value={email}
              onChange={(e) => setEmail(e.target.value)}
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

          {error && <p className="rounded-lg bg-amber-50 px-3.5 py-2.5 text-[12px] font-medium text-amber-800 ring-1 ring-amber-600/15">{error}</p>}

          <button
            type="submit"
            disabled={loading}
            className={`inline-flex items-center gap-2.5 rounded-full bg-brand-600 px-8 py-3 text-[13px] font-bold uppercase tracking-[0.12em] text-white ${EASE} hover:bg-brand-700 active:scale-[0.98] disabled:opacity-50`}
          >
            {loading ? 'Đang đăng nhập…' : <>Đăng nhập <span aria-hidden>›</span></>}
          </button>
        </form>

        <button
          type="button"
          onClick={enterDemo}
          className={`mt-4 w-max text-[12px] font-semibold text-ink-500 underline-offset-4 ${EASE} hover:text-ink-900 hover:underline`}
        >
          Vào thẳng không cần đăng nhập (dev only)
        </button>

        <div className="mt-12 border-t border-black/[0.07] pt-5">
          <p className="text-[11px] font-bold uppercase tracking-[0.18em] text-ink-500">Tài khoản demo</p>
          <p className="mt-2 text-[12px] leading-relaxed text-ink-500">
            admin · manager · receptionist · service <span className="text-ink-700">@hotel.com</span> — mật khẩu <span className="font-semibold text-ink-700">123456</span>
          </p>
        </div>
      </div>
      <p className="pb-5 text-[11px] text-ink-500/60">Group 2 · SE1919 · FPT University</p>
    </div>
  )
}
