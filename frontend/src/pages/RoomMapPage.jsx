import { useEffect, useMemo, useState } from 'react'
import client from '../api/client'
import { ROOM_STATUS, formatVnd } from '../utils/roomStatus'
import { MOCK_ROOM_MAP, MOCK_ROOM_TYPES } from '../mock/hotelMock'
import { roomImage, roomImagePosition } from '../utils/roomImages'

const EASE = 'transition-all duration-300 ease-[cubic-bezier(0.32,0.72,0,1)]'

function StatusFlag({ status }) {
  const s = ROOM_STATUS[status] ?? ROOM_STATUS.Maintenance
  return (
    <span className="inline-flex items-center gap-1.5 rounded-md bg-white/90 px-2 py-1 text-[10px] font-bold tracking-wide text-ink-700 backdrop-blur-sm">
      <span className={`h-1.5 w-1.5 rounded-full ${s.dot}`} />
      {s.label}
    </span>
  )
}

function RoomCard({ room, imgIdx = 0 }) {
  const dimmed = room.status === 'Maintenance' || room.status === 'Cleaning'
  return (
    <div className={`group overflow-hidden rounded-xl bg-white ring-1 ring-black/5 ${EASE} hover:-translate-y-1 hover:shadow-lift hover:ring-brand-500/30 cursor-pointer`}>
      <div className="relative h-28 overflow-hidden">
        <img
          src={roomImage(room.typeName, imgIdx)}
          alt={`Phòng ${room.roomNumber} - ${room.typeName}`}
          loading="lazy"
          className={`h-full w-full object-cover ${roomImagePosition(imgIdx)} ${EASE} duration-700 group-hover:scale-[1.06] ${dimmed ? 'opacity-75 saturate-50' : ''}`}
        />
        <div className="absolute inset-0 bg-gradient-to-t from-ink-900/50 via-transparent to-transparent" />
        <div className="absolute left-2 top-2"><StatusFlag status={room.status} /></div>
        <p className="absolute bottom-2 left-3 font-display text-2xl font-semibold text-white drop-shadow-sm">{room.roomNumber}</p>
      </div>
      <div className="px-3.5 py-2.5">
        <div className="flex items-baseline justify-between gap-2">
          <p className="text-[12px] font-semibold text-ink-700">{room.typeName}</p>
          <p className="text-[11px] tabular-nums text-ink-500">{formatVnd(room.basePrice)}<span className="opacity-60">/đêm</span></p>
        </div>
        <p className="mt-0.5 h-4 truncate text-[11px] italic text-ink-500">
          {room.guestName ? `Khách: ${room.guestName}` : ' '}
        </p>
      </div>
    </div>
  )
}

function SkeletonCard() {
  return (
    <div className="overflow-hidden rounded-xl bg-white ring-1 ring-black/5">
      <div className="h-28 animate-pulse bg-cream-200" />
      <div className="space-y-2 px-3.5 py-3">
        <div className="h-3 w-2/3 animate-pulse rounded bg-cream-200" />
        <div className="h-3 w-1/3 animate-pulse rounded bg-cream-200" />
      </div>
    </div>
  )
}

const selectCls =
  'rounded-lg bg-transparent px-3 py-2 text-[13px] font-medium text-ink-700 ring-1 ring-black/10 outline-none focus:ring-2 focus:ring-brand-500/40 hover:ring-black/20'

export default function RoomMapPage() {
  const [floors, setFloors] = useState(null) // null = đang tải
  const [usingMock, setUsingMock] = useState(false)
  const [floorFilter, setFloorFilter] = useState('all')
  const [typeFilter, setTypeFilter] = useState('all')
  const [statusFilter, setStatusFilter] = useState('all')

  const load = () => {
    setFloors(null)
    client
      .get('/api/rooms/map')
      .then((res) => { setFloors(res.data); setUsingMock(false) })
      .catch(() => { setFloors(MOCK_ROOM_MAP); setUsingMock(true) })
  }
  useEffect(load, [])

  const allRooms = useMemo(() => (floors ?? []).flatMap((f) => f.rooms), [floors])
  const count = (st) => allRooms.filter((r) => r.status === st).length

  const visibleFloors = useMemo(
    () =>
      (floors ?? [])
        .filter((f) => floorFilter === 'all' || f.floor === Number(floorFilter))
        .map((f) => ({
          ...f,
          rooms: f.rooms.filter(
            (r) =>
              (typeFilter === 'all' || r.typeName === typeFilter) &&
              (statusFilter === 'all' || r.status === statusFilter),
          ),
        }))
        .filter((f) => f.rooms.length > 0),
    [floors, floorFilter, typeFilter, statusFilter],
  )

  const stats = [
    { label: 'tổng phòng', value: allRooms.length },
    { label: 'trống', value: count('Available'), dot: 'bg-emerald-500' },
    { label: 'đang ở', value: count('Occupied'), dot: 'bg-rose-500' },
    { label: 'đã đặt', value: count('Reserved'), dot: 'bg-sky-500' },
    { label: 'dọn · bảo trì', value: count('Cleaning') + count('Maintenance'), dot: 'bg-amber-500' },
  ]

  return (
    <div className="mx-auto max-w-6xl">
      <div className="flex flex-wrap items-end justify-between gap-4">
        <div>
          <p className="font-display text-[15px] italic text-brand-600">tình trạng khách sạn hôm nay</p>
          <h1 className="mt-1 font-display text-4xl font-semibold tracking-tight">Sơ đồ phòng</h1>
        </div>
        <div className="flex items-center gap-3">
          {usingMock && (
            <p className="flex items-center gap-1.5 text-[11px] font-medium text-ink-500">
              <span className="h-1.5 w-1.5 animate-pulse rounded-full bg-amber-500" />
              dữ liệu mẫu, chờ API
            </p>
          )}
          <button
            onClick={load}
            className={`rounded-full px-4 py-2 text-[13px] font-semibold text-ink-700 ring-1 ring-black/10 ${EASE} hover:bg-white hover:ring-black/20 active:scale-[0.98] focus-visible:ring-2 focus-visible:ring-brand-500/50`}
          >
            Làm mới
          </button>
        </div>
      </div>

      {/* Dải số liệu: 1 khối, ngăn bằng hairline thay vì 5 hộp rời */}
      <div className="mt-7 grid grid-cols-2 overflow-hidden rounded-2xl bg-white ring-1 ring-black/5 shadow-soft sm:grid-cols-5 sm:divide-x sm:divide-black/5">
        {stats.map((st) => (
          <div key={st.label} className="px-5 py-4">
            <p className="font-display text-3xl font-semibold tabular-nums leading-none">{st.value}</p>
            <p className="mt-1.5 flex items-center gap-1.5 text-[11px] font-medium text-ink-500">
              {st.dot && <span className={`h-1.5 w-1.5 rounded-full ${st.dot}`} />}
              {st.label}
            </p>
          </div>
        ))}
      </div>

      <div className="mt-6 flex flex-wrap items-center gap-2.5">
        <select className={selectCls} value={floorFilter} onChange={(e) => setFloorFilter(e.target.value)}>
          <option value="all">Tất cả tầng</option>
          {(floors ?? []).map((f) => (
            <option key={f.floor} value={f.floor}>Tầng {f.floor}</option>
          ))}
        </select>
        <select className={selectCls} value={typeFilter} onChange={(e) => setTypeFilter(e.target.value)}>
          <option value="all">Tất cả loại phòng</option>
          {MOCK_ROOM_TYPES.map((t) => (
            <option key={t} value={t}>{t}</option>
          ))}
        </select>
        <select className={selectCls} value={statusFilter} onChange={(e) => setStatusFilter(e.target.value)}>
          <option value="all">Tất cả trạng thái</option>
          {Object.entries(ROOM_STATUS).map(([key, s]) => (
            <option key={key} value={key}>{s.label}</option>
          ))}
        </select>
      </div>

      {floors === null && (
        <div className="mt-8 grid grid-cols-2 gap-4 sm:grid-cols-3 lg:grid-cols-4">
          {Array.from({ length: 8 }).map((_, i) => <SkeletonCard key={i} />)}
        </div>
      )}

      {visibleFloors.map((f) => (
        <section key={f.floor} className="mt-9">
          <div className="mb-3.5 flex items-baseline gap-3">
            <h2 className="font-display text-xl font-semibold italic">Tầng {f.floor}</h2>
            <p className="text-[11px] tabular-nums text-ink-500">{f.rooms.length} phòng</p>
            <div className="h-px flex-1 self-center bg-black/[0.07]" />
          </div>
          <div className="grid grid-cols-2 gap-4 sm:grid-cols-3 lg:grid-cols-4">
            {f.rooms.map((room, idx) => (
              <RoomCard key={room.roomId} room={room} imgIdx={idx} />
            ))}
          </div>
        </section>
      ))}

      {floors !== null && visibleFloors.length === 0 && (
        <div className="mt-10 rounded-2xl border border-dashed border-black/10 bg-white/60 p-10 text-center">
          <p className="font-display text-lg italic text-ink-700">Không có phòng nào khớp bộ lọc</p>
          <p className="mt-1 text-[13px] text-ink-500">Thử bỏ bớt điều kiện lọc phía trên.</p>
        </div>
      )}
    </div>
  )
}
