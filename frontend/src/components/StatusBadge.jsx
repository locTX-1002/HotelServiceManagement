import { ROOM_STATUS } from '../utils/roomStatus'

export default function StatusBadge({ status }) {
  const s = ROOM_STATUS[status] ?? { label: status, dot: 'bg-stone-400', badge: 'bg-stone-100 text-stone-600 ring-1 ring-stone-500/15' }
  return (
    <span className={`inline-flex items-center gap-1.5 rounded-full px-2.5 py-1 text-[11px] font-semibold ${s.badge}`}>
      <span className={`h-1.5 w-1.5 rounded-full ${s.dot}`} />
      {s.label}
    </span>
  )
}
