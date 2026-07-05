import { useEffect, useMemo, useState } from 'react'
import client from '../api/client'
import StatusBadge from '../components/StatusBadge'
import { ROOM_STATUS, formatVnd } from '../utils/roomStatus'
import { MOCK_ROOM_MAP, MOCK_ROOM_TYPES } from '../mock/hotelMock'

const EASE = 'transition-all duration-300 ease-[cubic-bezier(0.32,0.72,0,1)]'

function RoomCard({ room }) {
  const s = ROOM_STATUS[room.status] ?? ROOM_STATUS.Maintenance
  return (
    <div className={`group relative overflow-hidden rounded-2xl bg-white ring-1 ring-black/5 shadow-soft ${EASE} hover:-translate-y-0.5 hover:shadow-lift cursor-pointer`}>
      <div className={`absolute inset-x-0 top-0 h-1 ${s.strip}`} />
      <div className="p-4 pt-5">
        <div className="flex items-start justify-between gap-2">
          <div>
            <p className="text-xl font-extrabold tracking-tight">{room.roomNumber}</p>
            <p className="mt-0.5 text-xs font-medium text-ink-500">{room.typeName}</p>
          </div>
          <StatusBadge status={room.status} />
        </div>
        <div className="mt-4 flex items-end justify-between">
          <p className="text-[13px] font-semibold text-ink-700">
            {formatVnd(room.basePrice)}
            <span className="text-[11px] font-normal text-ink-500"> / đêm</span>
          </p>
          {room.guestName && (
            <p className="max-w-[55%] truncate text-right text-[11px] text-ink-500" title={room.guestName}>
              {room.guestName}
            </p>
          )}
        </div>
      </div>
    </div>
  )
}

function SummaryChip({ label, value, dot }) {
  return (
    <div className="flex items-center gap-3 rounded-2xl bg-white px-4 py-3 ring-1 ring-black/5 shadow-soft">
      {dot && <span className={`h-2.5 w-2.5 rounded-full ${dot}`} />}
      <div>
        <p className="text-lg font-extrabold leading-none">{value}</p>
        <p className="mt-1 text-[11px] font-medium text-ink-500">{label}</p>
      </div>
    </div>
  )
}

const selectCls =
  'rounded-xl bg-white px-3.5 py-2 text-sm font-medium text-ink-700 ring-1 ring-black/10 outline-none focus:ring-2 focus:ring-brand-500/40'

export default function RoomMapPage() {
  const [floors, setFloors] = useState([])
  const [usingMock, setUsingMock] = useState(false)
  const [floorFilter, setFloorFilter] = useState('all')
  const [typeFilter, setTypeFilter] = useState('all')
  const [statusFilter, setStatusFilter] = useState('all')

  const load = () => {
    client
      .get('/api/rooms/map')
      .then((res) => { setFloors(res.data); setUsingMock(false) })
      .catch(() => { setFloors(MOCK_ROOM_MAP); setUsingMock(true) })
  }
  useEffect(load, [])

  const allRooms = useMemo(() => floors.flatMap((f) => f.rooms), [floors])
  const count = (st) => allRooms.filter((r) => r.status === st).length

  const visibleFloors = useMemo(
    () =>
      floors
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

  return (
    <div className="mx-auto max-w-6xl">
      <div className="flex flex-wrap items-end justify-between gap-4">
        <div>
          <h1 className="text-2xl font-extrabold tracking-tight">Sơ đồ phòng</h1>
          <p className="mt-1 text-sm text-ink-500">
            {allRooms.length} phòng · cập nhật theo thời gian thực khi lễ tân thao tác
          </p>
        </div>
        <button
          onClick={load}
          className={`rounded-full bg-ink-900 px-5 py-2.5 text-sm font-semibold text-cream-50 ${EASE} hover:bg-ink-700 active:scale-[0.98]`}
        >
          Làm mới
        </button>
      </div>

      {usingMock && (
        <p className="mt-4 rounded-xl bg-brand-50 px-4 py-2.5 text-[13px] font-medium text-brand-700 ring-1 ring-brand-600/10">
          Đang hiển thị dữ liệu mẫu — sẽ tự chuyển sang dữ liệu thật khi API /api/rooms/map sẵn sàng.
        </p>
      )}

      <div className="mt-6 grid grid-cols-2 gap-3 sm:grid-cols-3 lg:grid-cols-5">
        <SummaryChip label="Tổng phòng" value={allRooms.length} />
        <SummaryChip label="Trống" value={count('Available')} dot="bg-emerald-500" />
        <SummaryChip label="Đang ở" value={count('Occupied')} dot="bg-rose-500" />
        <SummaryChip label="Đã đặt" value={count('Reserved')} dot="bg-sky-500" />
        <SummaryChip label="Dọn / Bảo trì" value={count('Cleaning') + count('Maintenance')} dot="bg-amber-500" />
      </div>

      <div className="mt-6 flex flex-wrap items-center gap-3">
        <select className={selectCls} value={floorFilter} onChange={(e) => setFloorFilter(e.target.value)}>
          <option value="all">Tất cả tầng</option>
          {floors.map((f) => (
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
        <div className="ml-auto hidden flex-wrap items-center gap-2 md:flex">
          {Object.keys(ROOM_STATUS).map((key) => (
            <StatusBadge key={key} status={key} />
          ))}
        </div>
      </div>

      {visibleFloors.map((f) => (
        <section key={f.floor} className="mt-8">
          <div className="mb-3 flex items-center gap-3">
            <h2 className="text-sm font-bold uppercase tracking-[0.14em] text-ink-500">Tầng {f.floor}</h2>
            <div className="h-px flex-1 bg-black/5" />
          </div>
          <div className="grid grid-cols-2 gap-4 sm:grid-cols-3 lg:grid-cols-4 xl:grid-cols-5">
            {f.rooms.map((room) => (
              <RoomCard key={room.roomId} room={room} />
            ))}
          </div>
        </section>
      ))}

      {visibleFloors.length === 0 && (
        <div className="mt-10 rounded-2xl border border-dashed border-black/10 bg-white p-10 text-center text-sm text-ink-500">
          Không có phòng nào khớp bộ lọc.
        </div>
      )}
    </div>
  )
}
