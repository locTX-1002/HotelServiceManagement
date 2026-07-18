import { useState } from 'react'
import { Link, useNavigate, useSearchParams } from 'react-router-dom'
import { EASE, errorCls } from '../../utils/ui'
import guestClient, { isBackendMissing } from '../../api/guestClient'

const inputCls =
  'w-full rounded-lg border border-black/15 bg-white px-3.5 py-3 text-sm outline-none placeholder:text-ink-500/40 focus:border-brand-500 focus:ring-2 focus:ring-brand-500/20'
const labelCls = 'mb-1.5 block text-[11px] font-bold uppercase tracking-[0.18em] text-ink-700'

// Buoc 2 cua quen mat khau: mo tu link trong email (?token=...), nhap mat khau moi.
export default function GuestResetPasswordWithTokenPage() {
  const [searchParams] = useSearchParams()
  const token = searchParams.get('token') ?? ''
  const [newPassword, setNewPassword] = useState('')
  const [confirmPassword, setConfirmPassword] = useState('')
  const [error, setError] = useState('')
  const [loading, setLoading] = useState(false)
  const [done, setDone] = useState(false)
  const navigate = useNavigate()

  const onSubmit = (e) => {
    e.preventDefault()
    if (loading) return
    setError('')
    if (newPassword !== confirmPassword) {
      setError('Mật khẩu nhập lại không khớp.')
      return
    }
    setLoading(true)
    guestClient
      .post('/api/guest/auth/reset-password', { token, newPassword })
      .then(() => setDone(true))
      .catch((err) => {
        if (err.response?.status === 401) setError('Liên kết đã hết hạn hoặc không hợp lệ. Hãy yêu cầu gửi lại.')
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
            </div>

            {!token ? (
              <>
                <h1 className="mt-10 font-display text-3xl font-medium tracking-tight">Thiếu liên kết</h1>
                <p className="mt-2 text-sm leading-relaxed text-ink-500">
                  Truy cập trang này qua liên kết trong email đặt lại mật khẩu.
                </p>
                <Link
                  to="/guest/quen-mat-khau"
                  className={`mt-8 inline-flex items-center gap-2.5 rounded-full bg-brand-600 px-8 py-3 text-[13px] font-bold uppercase tracking-[0.12em] text-white ${EASE} hover:bg-brand-700 active:scale-[0.98]`}
                >
                  Yêu cầu liên kết mới
                </Link>
              </>
            ) : done ? (
              <>
                <h1 className="mt-10 font-display text-3xl font-medium tracking-tight">Đã đặt lại mật khẩu</h1>
                <p className="mt-2 text-sm leading-relaxed text-ink-500">Bạn có thể đăng nhập bằng mật khẩu mới ngay bây giờ.</p>
                <button
                  type="button"
                  onClick={() => navigate('/guest/dang-nhap', { replace: true })}
                  className={`mt-8 inline-flex items-center gap-2.5 rounded-full bg-brand-600 px-8 py-3 text-[13px] font-bold uppercase tracking-[0.12em] text-white ${EASE} hover:bg-brand-700 active:scale-[0.98]`}
                >
                  Đăng nhập <span aria-hidden>›</span>
                </button>
              </>
            ) : (
              <>
                <h1 className="mt-10 font-display text-4xl font-medium tracking-tight">Đặt lại mật khẩu</h1>
                <p className="mt-2 text-sm leading-relaxed text-ink-500">Nhập mật khẩu mới cho tài khoản của bạn.</p>

                <form onSubmit={onSubmit} className="mt-8 space-y-5">
                  <div>
                    <label className={labelCls}>Mật khẩu mới</label>
                    <input
                      type="password"
                      required
                      minLength={6}
                      autoComplete="new-password"
                      className={inputCls}
                      placeholder="Tối thiểu 6 ký tự"
                      value={newPassword}
                      onChange={(e) => setNewPassword(e.target.value)}
                    />
                  </div>
                  <div>
                    <label className={labelCls}>Nhập lại mật khẩu</label>
                    <input
                      type="password"
                      required
                      autoComplete="new-password"
                      className={inputCls}
                      value={confirmPassword}
                      onChange={(e) => setConfirmPassword(e.target.value)}
                    />
                  </div>

                  {error && <p className={errorCls}>{error}</p>}

                  <button
                    type="submit"
                    disabled={loading}
                    className={`inline-flex items-center gap-2.5 rounded-full bg-brand-600 px-8 py-3 text-[13px] font-bold uppercase tracking-[0.12em] text-white ${EASE} hover:bg-brand-700 active:scale-[0.98] disabled:opacity-50`}
                  >
                    {loading ? 'Đang xử lý…' : <>Đặt lại mật khẩu <span aria-hidden>›</span></>}
                  </button>
                </form>
              </>
            )}
          </div>
        </div>
      </div>
      <p className="pb-5 text-[11px] text-ink-500/60">Group 2 · SE1919 · FPT University</p>
    </div>
  )
}
