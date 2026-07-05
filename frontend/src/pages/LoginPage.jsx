import { useState } from 'react'
import { useNavigate } from 'react-router-dom'

// Skeleton Day 0: form login UI. Nối POST /api/auth/login khi backend auth xong (T2).
export default function LoginPage() {
  const [email, setEmail] = useState('receptionist@hotel.com')
  const [password, setPassword] = useState('')
  const [error, setError] = useState('')
  const navigate = useNavigate()

  const onSubmit = (e) => {
    e.preventDefault()
    setError('API login chưa sẵn sàng (task T2 06/07). Tạm thời bấm "Vào thẳng" để xem layout.')
  }

  return (
    <div className="flex min-h-screen items-center justify-center bg-slate-100">
      <form onSubmit={onSubmit} className="w-80 rounded-lg bg-white p-6 shadow">
        <h1 className="mb-4 text-center text-xl font-bold text-slate-800">HSMS Login</h1>
        <label className="mb-1 block text-sm text-gray-600">Email</label>
        <input
          className="mb-3 w-full rounded border px-3 py-2 text-sm"
          value={email}
          onChange={(e) => setEmail(e.target.value)}
        />
        <label className="mb-1 block text-sm text-gray-600">Password</label>
        <input
          type="password"
          className="mb-3 w-full rounded border px-3 py-2 text-sm"
          value={password}
          onChange={(e) => setPassword(e.target.value)}
        />
        {error && <p className="mb-3 text-xs text-red-600">{error}</p>}
        <button type="submit" className="w-full rounded bg-slate-800 py-2 text-sm font-semibold text-white hover:bg-slate-700">
          Login
        </button>
        <button
          type="button"
          onClick={() => navigate('/dashboard')}
          className="mt-2 w-full rounded border py-2 text-xs text-gray-500 hover:bg-gray-50"
        >
          Vào thẳng (dev only)
        </button>
      </form>
    </div>
  )
}
