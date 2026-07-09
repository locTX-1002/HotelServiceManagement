import { NavLink } from 'react-router-dom'

const EASE = 'transition-all duration-300 ease-[cubic-bezier(0.32,0.72,0,1)]'

const TABS = [
  { to: '/rooms', label: 'Danh sách phòng', end: true },
  { to: '/rooms/types', label: 'Loại phòng', end: false },
]

// Chuyển qua lại giữa 2 trang quản trị Phòng / Loại phòng
// - cùng kiểu segmented control với bộ lọc trạng thái của sơ đồ phòng.
export default function RoomsTabs() {
  return (
    <div className="inline-flex items-center gap-0.5 rounded-full bg-white p-1 ring-1 ring-black/10 shadow-soft">
      {TABS.map((t) => (
        <NavLink
          key={t.to}
          to={t.to}
          end={t.end}
          className={({ isActive }) =>
            `rounded-full px-4 py-1.5 text-[12px] font-semibold ${EASE} ${
              isActive ? 'bg-ink-900 text-cream-50' : 'text-ink-500 hover:text-ink-900'
            }`
          }
        >
          {t.label}
        </NavLink>
      ))}
    </div>
  )
}
