import { useEffect, useState } from 'react'
import { EASE, errorCls } from '../utils/ui'
import { Link, Navigate, useLocation, useNavigate } from 'react-router-dom'
import client, { isBackendMissing } from '../api/client'
import { homeFor } from '../utils/roles'
import { getToken, getUser, readAuthResponse, saveSession } from '../utils/session'

const inputCls =
  'w-full rounded-lg border border-black/15 bg-white px-3.5 py-3 text-sm outline-none placeholder:text-ink-500/40 focus:border-brand-500 focus:ring-2 focus:ring-brand-500/20'
const labelCls = 'mb-1.5 block text-[11px] font-bold uppercase tracking-[0.18em] text-ink-700'

// Login tối giản nền trắng theo mẫu booking engine - POST /api/auth/login, lưu phiên vào localStorage
export default function LoginPage() {
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [showPassword, setShowPassword] = useState(false)
  const [error, setError] = useState('')
  const [loading, setLoading] = useState(false)
  const [forgotOpen, setForgotOpen] = useState(false)
  const navigate = useNavigate()
  const location = useLocation()
  const from = location.state?.from ?? null

  // Esc để đóng hướng dẫn quên mật khẩu
  useEffect(() => {
    if (!forgotOpen) return
    const onKey = (e) => e.key === 'Escape' && setForgotOpen(false)
    window.addEventListener('keydown', onKey)
    return () => window.removeEventListener('keydown', onKey)
  }, [forgotOpen])

  // Email soạn sẵn gửi Quản trị viên - điền luôn email đang gõ ở ô đăng nhập nếu có
  const mailHref =
    `mailto:admin@hotel.com?subject=${encodeURIComponent('[HSMS] Nhờ cấp lại mật khẩu')}` +
    `&body=${encodeURIComponent(`Chào Quản trị viên,\n\nNhờ anh/chị cấp lại mật khẩu cho tài khoản: ${email.trim() || '(điền email của bạn)'}\n\nCảm ơn!`)}`

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
        // Đọc được cả shape phẳng (accessToken + field phẳng) lẫn lồng ({token, user})
        const { token, user } = readAuthResponse(res.data)
        // Không lưu phiên hỏng nếu backend trả 200 mà thiếu token
        if (!token) return setError('Máy chủ trả về thiếu token — báo backend kiểm tra /api/auth/login.')
        saveSession(token, user)
        // Về trang bị chặn trước đó, không có thì về trang chính theo vai trò
        navigate(from ?? homeFor(user?.role), { replace: true })
      })
      .catch((err) => {
        if (err.response?.status === 401) setError('Email hoặc mật khẩu không đúng.')
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
        {/* Double-bezel: vỏ vòm mềm bọc lấy card đăng nhập - chất Amanoi */}
        <div className="bezel-shell">
          <div className="bezel-core px-7 py-9 sm:px-10 sm:py-12">
        {/* Logo vòm đồng bộ với nav */}
        <div className="flex flex-col items-center text-center">
          <span className="flex h-12 w-10 items-end justify-center rounded-t-full rounded-b-md bg-brand-600 pb-2 font-display text-lg font-bold text-white">
            H
          </span>
          <p className="mt-3 font-display text-3xl font-semibold tracking-tight">HSMS</p>
          <p className="mt-1 text-[9px] font-semibold tracking-[0.32em] text-ink-500">★★★★★ HOTEL & SERVICE MANAGEMENT</p>
        </div>

        <h1 className="mt-10 font-display text-4xl font-medium tracking-tight">Chào mừng trở lại</h1>
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
              placeholder="ten@hotel.com"
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

          {error && <p className={errorCls}>{error}</p>}

          <button
            type="submit"
            disabled={loading}
            className={`inline-flex items-center gap-2.5 rounded-full bg-brand-600 px-8 py-3 text-[13px] font-bold uppercase tracking-[0.12em] text-white ${EASE} hover:bg-brand-700 active:scale-[0.98] disabled:opacity-50`}
          >
            {loading ? 'Đang đăng nhập…' : <>Đăng nhập <span aria-hidden>›</span></>}
          </button>
        </form>

        <p className="mt-8 border-t border-black/[0.07] pt-4 text-[12px] leading-relaxed text-ink-500">
          Quên mật khẩu?{' '}
          <button
            type="button"
            onClick={() => setForgotOpen(true)}
            className={`font-semibold text-brand-600 underline-offset-4 ${EASE} hover:underline`}
          >
            Xem cách cấp lại
          </button>
        </p>
          </div>
        </div>
      </div>
      <p className="pb-5 text-[11px] text-ink-500/60">Group 2 · SE1919 · FPT University</p>

      {/* Hướng dẫn cấp lại mật khẩu - HSMS là hệ thống nội bộ nên không tự reset
          qua email; Quản trị viên cấp lại bằng nút "Cấp lại MK" ở trang Người dùng */}
      {forgotOpen && (
        <div className="fixed inset-0 z-50 flex items-center justify-center px-6">
          <div onClick={() => setForgotOpen(false)} className="absolute inset-0 bg-ink-900/30" />
          <div className="relative w-full max-w-md rounded-2xl bg-cream-50 p-7 shadow-lift">
            <p className="font-display text-[13px] italic text-brand-600">hỗ trợ tài khoản</p>
            <p className="mt-1 font-display text-2xl font-semibold tracking-tight">Quên mật khẩu?</p>
            <p className="mt-2 text-[13px] leading-relaxed text-ink-500">
              Đây là hệ thống nội bộ nên mật khẩu không tự đặt lại qua email — Quản trị viên sẽ cấp lại cho bạn trong ít phút.
            </p>
            <ol className="mt-5 space-y-3">
              {[
                ['1', 'Báo cho Quản trị viên — bấm nút bên dưới để soạn sẵn email.'],
                ['2', 'Quản trị viên mở trang Người dùng, bấm "Cấp lại MK" cho tài khoản của bạn rồi báo lại mật khẩu tạm.'],
                ['3', 'Đăng nhập bằng mật khẩu tạm, sau đó bấm "Đổi mật khẩu" trên thanh menu để đặt mật khẩu riêng.'],
              ].map(([n, text]) => (
                <li key={n} className="flex gap-3 text-[13px] leading-relaxed text-ink-700">
                  <span className="flex h-6 w-6 shrink-0 items-center justify-center rounded-full bg-brand-600/10 text-[11px] font-bold text-brand-700">{n}</span>
                  <span>{text}</span>
                </li>
              ))}
            </ol>
            <div className="mt-6 flex flex-wrap justify-end gap-2.5">
              <button
                type="button"
                onClick={() => setForgotOpen(false)}
                className={`rounded-full px-5 py-2.5 text-[13px] font-semibold text-ink-700 ring-1 ring-black/10 ${EASE} hover:bg-white`}
              >
                Đóng
              </button>
              <a
                href={mailHref}
                className={`rounded-full bg-brand-600 px-5 py-2.5 text-[13px] font-bold text-white ${EASE} hover:bg-brand-700 active:scale-[0.98]`}
              >
                Soạn email cho Quản trị viên
              </a>
            </div>
          </div>
        </div>
      )}
    </div>
  )
}
