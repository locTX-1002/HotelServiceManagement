import { useState } from 'react'
import { useNavigate } from 'react-router-dom'

const EASE = 'transition-all duration-300 ease-[cubic-bezier(0.32,0.72,0,1)]'
const inputCls =
  'w-full rounded-lg border border-black/15 bg-white px-3.5 py-3 text-sm outline-none placeholder:text-ink-500/40 focus:border-brand-500 focus:ring-2 focus:ring-brand-500/20'
const labelCls = 'mb-1.5 block text-[11px] font-bold uppercase tracking-[0.18em] text-ink-700'

// Login tối giản nền trắng theo mẫu booking engine - nối POST /api/auth/login là task T2 (Phúc)
export default function LoginPage() {
  const [email, setEmail] = useState('receptionist@hotel.com')
  const [password, setPassword] = useState('')
  const [error, setError] = useState('')
  const navigate = useNavigate()

  const onSubmit = (e) => {
    e.preventDefault()
    setError('API login chưa sẵn sàng (task T2). Dùng "Vào thẳng" để xem giao diện.')
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
            <input className={inputCls} value={email} onChange={(e) => setEmail(e.target.value)} />
          </div>
          <div>
            <label className={labelCls}>Mật khẩu</label>
            <input type="password" className={inputCls} placeholder="••••••" value={password} onChange={(e) => setPassword(e.target.value)} />
          </div>

          {error && <p className="rounded-lg bg-amber-50 px-3.5 py-2.5 text-[12px] font-medium text-amber-800 ring-1 ring-amber-600/15">{error}</p>}

          <button
            type="submit"
            className={`inline-flex items-center gap-2.5 rounded-full bg-brand-600 px-8 py-3 text-[13px] font-bold uppercase tracking-[0.12em] text-white ${EASE} hover:bg-brand-700 active:scale-[0.98]`}
          >
            Đăng nhập <span aria-hidden>›</span>
          </button>
        </form>

        <button
          type="button"
          onClick={() => navigate('/rooms/map')}
          className={`mt-4 w-max text-[12px] font-semibold text-ink-500 underline-offset-4 ${EASE} hover:text-ink-900 hover:underline`}
        >
          Vào thẳng không cần đăng nhập (dev only)
        </button>

        <div className="mt-12 border-t border-black/[0.07] pt-5">
          <p className="text-[11px] font-bold uppercase tracking-[0.18em] text-ink-500">Tài khoản demo</p>
          <p className="mt-2 text-[12px] leading-relaxed text-ink-500">
            admin, manager, receptionist, service <span className="text-ink-700">@hotel.com</span>
          </p>
          <p className="text-[12px] leading-relaxed text-ink-500">
            Mật khẩu <span className="font-semibold text-ink-700">123456</span>
          </p>
        </div>
      </div>
      <p className="pb-5 text-[11px] text-ink-500/60">Group 2, SE1919, FPT University</p>
    </div>
  )
}
