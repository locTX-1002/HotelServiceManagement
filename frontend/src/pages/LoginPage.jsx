import { useState } from 'react'
import { useNavigate } from 'react-router-dom'

const EASE = 'transition-all duration-300 ease-[cubic-bezier(0.32,0.72,0,1)]'
const inputCls =
  'w-full rounded-xl bg-white px-3.5 py-2.5 text-sm ring-1 ring-black/10 outline-none placeholder:text-ink-500/50 focus:ring-2 focus:ring-brand-500/40'

// UI login (Lộc hỗ trợ theme) - nối POST /api/auth/login là task của Phúc (T2)
export default function LoginPage() {
  const [email, setEmail] = useState('receptionist@hotel.com')
  const [password, setPassword] = useState('')
  const [error, setError] = useState('')
  const navigate = useNavigate()

  const onSubmit = (e) => {
    e.preventDefault()
    setError('API login chưa sẵn sàng (task T2). Tạm thời dùng "Vào thẳng" để xem giao diện.')
  }

  return (
    <div className="flex min-h-screen">
      {/* Ảnh hero - ẩn trên mobile */}
      <div className="relative hidden w-1/2 lg:block">
        <img src="/img/login-hero.jpg" alt="" className="absolute inset-0 h-full w-full object-cover" />
        <div className="absolute inset-0 bg-gradient-to-t from-ink-900/80 via-ink-900/20 to-transparent" />
        <div className="absolute bottom-0 left-0 p-10">
          <p className="text-[11px] font-bold uppercase tracking-[0.28em] text-white/70">Hotel & Service Management</p>
          <p className="mt-3 max-w-md font-display text-3xl font-medium italic leading-snug text-white">
            "Mỗi căn phòng là một câu chuyện được chăm chút."
          </p>
          <p className="mt-4 text-[12px] font-medium text-white/60">Group 2 · SE1919 · FPT University</p>
        </div>
      </div>

      {/* Form */}
      <div className="flex flex-1 items-center justify-center bg-cream-100 px-6">
        <form onSubmit={onSubmit} className="w-full max-w-sm">
          <p className="text-[11px] font-bold uppercase tracking-[0.22em] text-brand-600">Chào mừng trở lại</p>
          <h1 className="mt-2 font-display text-4xl font-semibold tracking-tight">HSMS</h1>
          <p className="mt-2 text-sm text-ink-500">Đăng nhập bằng tài khoản nhân viên để bắt đầu ca làm việc.</p>

          <div className="mt-8 space-y-4">
            <div>
              <label className="mb-1.5 block text-[12px] font-semibold text-ink-700">Email</label>
              <input className={inputCls} value={email} onChange={(e) => setEmail(e.target.value)} />
            </div>
            <div>
              <label className="mb-1.5 block text-[12px] font-semibold text-ink-700">Mật khẩu</label>
              <input type="password" className={inputCls} placeholder="••••••" value={password} onChange={(e) => setPassword(e.target.value)} />
            </div>
          </div>

          {error && <p className="mt-4 rounded-xl bg-amber-50 px-3.5 py-2.5 text-[12px] font-medium text-amber-800 ring-1 ring-amber-600/15">{error}</p>}

          <button
            type="submit"
            className={`mt-6 w-full rounded-full bg-ink-900 py-3 text-sm font-bold text-cream-50 ${EASE} hover:bg-ink-700 active:scale-[0.98]`}
          >
            Đăng nhập
          </button>
          <button
            type="button"
            onClick={() => navigate('/rooms/map')}
            className={`mt-3 w-full rounded-full py-2.5 text-[13px] font-semibold text-ink-500 ring-1 ring-black/10 ${EASE} hover:bg-white`}
          >
            Vào thẳng (dev only)
          </button>

          <p className="mt-8 text-center text-[11px] text-ink-500/70">
            Tài khoản demo: receptionist@hotel.com · 123456
          </p>
        </form>
      </div>
    </div>
  )
}
