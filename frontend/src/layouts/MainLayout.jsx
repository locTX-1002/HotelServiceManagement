import { NavLink, Outlet, useNavigate } from 'react-router-dom'

const MENU = [
  { to: '/dashboard', label: 'Dashboard' },
  { to: '/rooms/map', label: 'Room Map' },
  { to: '/rooms', label: 'Rooms' },
  { to: '/reservations', label: 'Reservations' },
  { to: '/checkin-checkout', label: 'Check-in / Check-out' },
  { to: '/service-orders', label: 'Service Orders' },
  { to: '/reports', label: 'Reports' },
]

export default function MainLayout() {
  const navigate = useNavigate()

  const logout = () => {
    localStorage.removeItem('token')
    navigate('/login')
  }

  return (
    <div className="flex min-h-screen bg-gray-50">
      <aside className="w-56 shrink-0 bg-slate-800 text-white">
        <div className="px-4 py-4 text-lg font-bold">HSMS</div>
        <nav className="flex flex-col gap-1 px-2">
          {MENU.map((item) => (
            <NavLink
              key={item.to}
              to={item.to}
              className={({ isActive }) =>
                `rounded px-3 py-2 text-sm ${isActive ? 'bg-slate-600 font-semibold' : 'hover:bg-slate-700'}`
              }
            >
              {item.label}
            </NavLink>
          ))}
        </nav>
      </aside>

      <div className="flex flex-1 flex-col">
        <header className="flex items-center justify-between border-b bg-white px-6 py-3">
          <h1 className="text-base font-semibold text-gray-800">Hotel and Service Management System</h1>
          <button onClick={logout} className="rounded bg-slate-100 px-3 py-1.5 text-sm hover:bg-slate-200">
            Logout
          </button>
        </header>
        <main className="flex-1 p-6">
          <Outlet />
        </main>
      </div>
    </div>
  )
}
