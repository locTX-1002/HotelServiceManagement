import { ROOM_STATUS } from '../utils/roomStatus'

export default function StatusBadge({ status }) {
  const s = ROOM_STATUS[status] ?? { label: status, badge: 'bg-gray-100 text-gray-600' }
  return (
    <span className={`inline-block rounded-full px-2 py-0.5 text-xs font-medium ${s.badge}`}>
      {s.label}
    </span>
  )
}
