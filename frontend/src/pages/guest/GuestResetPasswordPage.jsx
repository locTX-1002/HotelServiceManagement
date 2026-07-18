import { useState } from 'react'
import { Link } from 'react-router-dom'
import { EASE, errorCls } from '../../utils/ui'
import guestClient, { isBackendMissing } from '../../api/guestClient'

const inputCls =
  'w-full rounded-lg border border-black/15 bg-white px-3.5 py-3 text-sm outline-none placeholder:text-ink-500/40 focus:border-brand-500 focus:ring-2 focus:ring-brand-500/20'
const labelCls = 'mb-1.5 block text-[11px] font-bold uppercase tracking-[0.18em] text-ink-700'

// Buoc 1 cua quen mat khau: nhap SDT, he thong gui link dat lai qua email (neu tai khoan co email).
// Buoc 2 (nhap mat khau moi tu link) o GuestResetPasswordWithTokenPage.jsx.
export default function GuestResetPasswordPage() {
  const [phoneNumber, setPhoneNumber] = useState('')
  const [error, setError] = useState('')
  const [loading, setLoading] = useState(false)
  const [done, setDone] = useState(false)

  const onSubmit = (e) => {
    e.preventDefault()
    if (loading) return
    setError('')
    setLoading(true)
    guestClient
      .post('/api/guest/auth/forgot-password', { phoneNumber: phoneNumber.trim() })
      .then(() => setDone(true))
      .catch((err) => {
        if (isBackendMissing(err)) setError('Không kết nối được máy chủ. Vui lòng thử lại sau.')
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

            {done ? (
              <>
                <h1 className="mt-10 font-display text-3xl font-medium tracking-tight">Đã gửi yêu cầu</h1>
                <p className="mt-2 text-sm leading-relaxed text-ink-500">
                  Nếu số điện thoại này có tài khoản và có email, một liên kết đặt lại mật khẩu đã được gửi tới email đó —
                  kiểm tra hộp thư đến (và mục Spam). Liên kết có hiệu lực trong 30 phút.
                </p>
                <p className="mt-3 text-[12px] leading-relaxed text-ink-500">
                  Chưa có email trên tài khoản? Liên hệ lễ tân để được hỗ trợ.
                </p>
              </>
            ) : (
              <>
                <h1 className="mt-10 font-display text-4xl font-medium tracking-tight">Quên mật khẩu</h1>
                <p className="mt-2 text-sm leading-relaxed text-ink-500">
                  Nhập số điện thoại đăng ký, chúng tôi sẽ gửi liên kết đặt lại mật khẩu tới email trên tài khoản.
                </p>

                <form onSubmit={onSubmit} className="mt-8 space-y-5">
                  <div>
                    <label className={labelCls}>Số điện thoại</label>
                    <input
                      type="tel"
                      required
                      className={inputCls}
                      placeholder="09xxxxxxxx"
                      value={phoneNumber}
                      onChange={(e) => setPhoneNumber(e.target.value)}
                    />
                  </div>

                  {error && <p className={errorCls}>{error}</p>}

                  <button
                    type="submit"
                    disabled={loading}
                    className={`inline-flex items-center gap-2.5 rounded-full bg-brand-600 px-8 py-3 text-[13px] font-bold uppercase tracking-[0.12em] text-white ${EASE} hover:bg-brand-700 active:scale-[0.98] disabled:opacity-50`}
                  >
                    {loading ? 'Đang gửi…' : <>Gửi liên kết đặt lại <span aria-hidden>›</span></>}
                  </button>
                </form>

                <p className="mt-8 border-t border-black/[0.07] pt-4 text-[12px] leading-relaxed text-ink-500">
                  Nhớ mật khẩu rồi?{' '}
                  <Link to="/guest/dang-nhap" className={`font-semibold text-brand-600 underline-offset-4 ${EASE} hover:underline`}>
                    Đăng nhập
                  </Link>
                </p>
              </>
            )}
          </div>
        </div>
      </div>
      <p className="pb-5 text-[11px] text-ink-500/60">Group 2 · SE1919 · FPT University</p>
    </div>
  )
}
