import { useState } from 'react'
import { Link, Navigate, useNavigate } from 'react-router-dom'
import { EASE, errorCls } from '../../utils/ui'
import guestClient, { isBackendMissing } from '../../api/guestClient'
import PortalSwitch from '../../components/PortalSwitch'
import { getGuestToken, saveGuestSession } from '../../utils/guestSession'

const inputCls =
  'w-full rounded-lg border border-black/15 bg-white px-3.5 py-3 text-sm outline-none placeholder:text-ink-500/40 focus:border-brand-500 focus:ring-2 focus:ring-brand-500/20'
const labelCls = 'mb-1.5 block text-[11px] font-bold uppercase tracking-[0.18em] text-ink-700'

// Khach tu dang ky tu do bang SDT - khong can chung minh so huu 1 dat phong cu the (xac minh danh
// tinh that dien ra o quay le tan luc check-in). He thong tu khop SDT voi dat phong da co san
// (neu co) de khach thay ngay, khong co thi tao tai khoan trong cho lan dat phong tiep theo.
export default function GuestRegisterPage() {
  const [fullName, setFullName] = useState('')
  const [email, setEmail] = useState('')
  const [phoneNumber, setPhoneNumber] = useState('')
  const [password, setPassword] = useState('')
  const [confirmPassword, setConfirmPassword] = useState('')
  const [error, setError] = useState('')
  const [loading, setLoading] = useState(false)
  const navigate = useNavigate()

  if (getGuestToken()) return <Navigate to="/guest/dashboard" replace />

  const onSubmit = (e) => {
    e.preventDefault()
    if (loading) return
    setError('')
    if (password !== confirmPassword) {
      setError('Mật khẩu nhập lại không khớp.')
      return
    }
    setLoading(true)
    guestClient
      .post('/api/guest/auth/register', {
        fullName: fullName.trim(),
        email: email.trim() || null,
        phoneNumber: phoneNumber.trim(),
        password,
      })
      .then((res) => {
        saveGuestSession(res.data)
        navigate('/guest/dashboard', { replace: true })
      })
      .catch((err) => {
        if (err.response?.status === 409) setError('Số điện thoại này đã có tài khoản. Vui lòng đăng nhập.')
        else if (isBackendMissing(err)) setError('Không kết nối được máy chủ. Vui lòng thử lại sau.')
        else setError(err.response?.data?.message ?? 'Máy chủ báo lỗi. Thử lại sau ít phút.')
      })
      .finally(() => setLoading(false))
  }

  return (
    <div className="relative flex min-h-screen bg-cream-100">
      {/* Nua trai anh + overlay - dong bo bo cuc tach doi voi trang dang nhap */}
      <div className="relative hidden w-1/2 lg:block">
        <img src="/img/login-hero.jpg" alt="" className="absolute inset-0 h-full w-full object-cover" />
        <div className="absolute inset-0 bg-gradient-to-t from-ink-900/80 via-ink-900/25 to-ink-900/30" />
        <div className="absolute bottom-12 left-12 right-12 text-white">
          <p className="font-display text-[15px] italic text-white/80">★★★★★ hotel & service</p>
          <p className="mt-2 font-display text-4xl font-medium leading-tight">Kỳ nghỉ của bạn bắt đầu từ đây</p>
          <p className="mt-2.5 max-w-md text-sm leading-relaxed text-white/70">
            Đặt phòng, gọi dịch vụ và theo dõi kỳ lưu trú — tất cả trong một tài khoản.
          </p>
        </div>
      </div>

      <div className="relative flex flex-1 flex-col items-center px-6">
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
      <div className="flex w-full max-w-md flex-1 flex-col justify-center py-6">
        <div className="bezel-shell">
          <div className="bezel-core px-7 py-7 sm:px-10 sm:py-8">
            <PortalSwitch active="guest" />
            <div className="mt-6 flex flex-col items-center text-center">
              <span className="flex h-12 w-10 items-end justify-center rounded-t-full rounded-b-md bg-brand-600 pb-2 font-display text-lg font-bold text-white">
                H
              </span>
              <p className="mt-3 font-display text-3xl font-semibold tracking-tight">HSMS</p>
              <p className="mt-1 text-[9px] font-semibold tracking-[0.32em] text-ink-500">CỔNG THÔNG TIN KHÁCH LƯU TRÚ</p>
            </div>

            <h1 className="mt-6 font-display text-4xl font-medium tracking-tight">Tạo tài khoản</h1>
            <p className="mt-2 text-sm leading-relaxed text-ink-500">
              Đăng ký bằng số điện thoại của bạn. Nếu bạn đã từng đặt phòng, hệ thống sẽ tự nối vào đúng thông tin đó.
            </p>

            <form onSubmit={onSubmit} className="mt-6 space-y-4">
              <div>
                <label className={labelCls}>Họ và tên</label>
                <input
                  required
                  className={inputCls}
                  placeholder="Nguyễn Văn A"
                  value={fullName}
                  onChange={(e) => setFullName(e.target.value)}
                />
              </div>
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
              <div>
                <label className={labelCls}>Email (không bắt buộc)</label>
                <input
                  type="email"
                  className={inputCls}
                  placeholder="Dùng để đặt lại mật khẩu sau này"
                  value={email}
                  onChange={(e) => setEmail(e.target.value)}
                />
              </div>
              <div>
                <label className={labelCls}>Mật khẩu</label>
                <input
                  type="password"
                  required
                  minLength={6}
                  autoComplete="new-password"
                  className={inputCls}
                  placeholder="Tối thiểu 6 ký tự"
                  value={password}
                  onChange={(e) => setPassword(e.target.value)}
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
                {loading ? 'Đang tạo tài khoản…' : <>Tạo tài khoản <span aria-hidden>›</span></>}
              </button>
            </form>

            <p className="mt-6 border-t border-black/[0.07] pt-4 text-[12px] leading-relaxed text-ink-500">
              Đã có tài khoản?{' '}
              <Link to="/guest/dang-nhap" className={`font-semibold text-brand-600 underline-offset-4 ${EASE} hover:underline`}>
                Đăng nhập
              </Link>
            </p>
          </div>
        </div>
      </div>
      <p className="pb-5 text-[11px] text-ink-500/60">Group 2 · SE1919 · FPT University</p>
      </div>
    </div>
  )
}
