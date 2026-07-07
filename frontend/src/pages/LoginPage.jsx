import { useState } from 'react'
import { useNavigate } from 'react-router-dom'

const EASE = 'transition-all duration-300 ease-[cubic-bezier(0.32,0.72,0,1)]'
const EASE_SLOW = 'transition-all duration-700 ease-[cubic-bezier(0.32,0.72,0,1)]'
const inputCls =
  'w-full rounded-lg border border-black/15 bg-white px-3.5 py-3 text-sm outline-none placeholder:text-ink-500/40 focus:border-brand-500 focus:ring-2 focus:ring-brand-500/20'
const labelCls = 'mb-1.5 block text-[11px] font-bold uppercase tracking-[0.18em] text-ink-700'

export default function LoginPage() {
  const [email, setEmail] = useState('receptionist@hotel.com')
  const [password, setPassword] = useState('')
  const [error, setError] = useState('')
  const navigate = useNavigate()

  const onSubmit = (e) => {
    e.preventDefault()
    setError('API login chưa nối trên FE - dùng "Vào thẳng" để xem giao diện, hoặc gọi trực tiếp /api/auth/login (đã chạy thật).')
  }

  return (
    <div className="grid min-h-[100dvh] lg:grid-cols-2">
      <div className="grain-overlay" />

      {/* Vế ảnh: full-bleed, chỉ hiện từ lg trở lên để form luôn là trọng tâm trên mobile */}
      <div className="relative hidden overflow-hidden lg:block">
        <img src="/img/login-hero.jpg" alt="" className="absolute inset-0 h-full w-full object-cover" />
        <div className="absolute inset-0 bg-gradient-to-t from-ink-900/80 via-ink-900/10 to-ink-900/30" />
        <p className="absolute bottom-12 left-12 right-12 font-display text-3xl font-medium leading-[1.25] text-white [text-wrap:balance]">
          Kỳ nghỉ trọn vẹn bắt đầu từ một ca trực chỉn chu.
        </p>
      </div>

      <div className="relative flex flex-col items-center justify-center bg-cream-100 px-6 py-12">
        {/* Double-bezel: khối vỏ ngoài mềm bọc lấy card form, tạo chiều sâu vật lý */}
        <div className="w-full max-w-md bezel-shell">
          <div className="bezel-core px-8 py-10 sm:px-12 sm:py-14">
            <div className="flex flex-col items-center text-center">
              <span className="flex h-12 w-10 items-end justify-center rounded-t-full rounded-b-md bg-brand-600 pb-2 font-display text-lg font-bold text-white shadow-lift">
                H
              </span>
              <p className="mt-3 font-display text-3xl font-semibold tracking-tight">HSMS</p>
              <p className="mt-1 text-[9px] font-semibold tracking-[0.32em] text-ink-500">★★★★★ HOTEL & SERVICE MANAGEMENT</p>
            </div>

            <h1 className="mt-10 text-center font-display text-3xl font-medium tracking-tight">Chào mừng trở lại</h1>
            <p className="mt-2 text-center text-sm leading-relaxed text-ink-500">
              Đăng nhập bằng tài khoản nhân viên để bắt đầu ca làm việc.
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
                className={`group/cta flex w-full items-center justify-center gap-3 rounded-full bg-brand-600 py-3.5 text-[13px] font-bold uppercase tracking-[0.15em] text-white ${EASE_SLOW} hover:bg-brand-700 active:scale-[0.98]`}
              >
                Đăng nhập
                <span className={`flex h-6 w-6 items-center justify-center rounded-full bg-white/15 ${EASE_SLOW} group-hover/cta:translate-x-0.5`}>›</span>
              </button>
            </form>

            <button
              type="button"
              onClick={() => navigate('/rooms/map')}
              className={`mx-auto mt-4 block w-max text-[12px] font-semibold text-ink-500 underline-offset-4 ${EASE} hover:text-ink-900 hover:underline`}
            >
              Vào thẳng không cần đăng nhập (dev only)
            </button>

            <div className="mt-10 border-t border-black/[0.07] pt-5 text-center">
              <p className="text-[11px] font-bold uppercase tracking-[0.18em] text-ink-500">Tài khoản demo</p>
              <p className="mt-2 text-[12px] leading-relaxed text-ink-500">
                admin<span className="text-ink-700">/Admin123!</span> · manager<span className="text-ink-700">/Manager123!</span>
              </p>
              <p className="text-[12px] leading-relaxed text-ink-500">
                receptionist<span className="text-ink-700">/Receptionist123!</span> · service<span className="text-ink-700">/Service123!</span>
              </p>
              <p className="mt-1 text-[11px] text-ink-500/70">đuôi email chung: @hotel.com</p>
            </div>
          </div>
        </div>

        <p className="mt-6 text-[11px] text-ink-500/60">Group 2, SE1919, FPT University</p>
      </div>
    </div>
  )
}
