import { useEffect, useMemo, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import client from '../api/client'
import { ROOM_STATUS, formatVnd } from '../utils/roomStatus'
import { MOCK_ROOM_MAP, MOCK_ROOM_TYPES } from '../mock/hotelMock'
import { roomImage } from '../utils/roomImages'

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

/* Ô phòng kiểu bản in: số serif lớn, nền nhuộm nhạt theo trạng thái, sọc chéo khi bảo trì */
const TILE_TINT = {
  Available: 'bg-white',
  Reserved: 'bg-sky-50/70',
  Occupied: 'bg-rose-50/70',
  Cleaning: 'bg-amber-50/70',
  Maintenance: 'bg-[repeating-linear-gradient(135deg,#ffffff,#ffffff_7px,rgba(33,29,24,0.05)_7px,rgba(33,29,24,0.05)_8px)]',
}

function RoomTile({ room, delay, onOpen }) {
  const s = ROOM_STATUS[room.status] ?? ROOM_STATUS.Maintenance
  const muted = room.status === 'Maintenance'
  return (
    <button
      onClick={() => onOpen(room)}
      style={{ animationDelay: `${delay}ms` }}
      className={`card-rise group relative -mb-px -mr-px border border-black/[0.08] p-4 text-left outline-none ${TILE_TINT[room.status] ?? 'bg-white'} ${EASE} hover:z-10 hover:bg-white hover:shadow-lift focus-visible:z-10 focus-visible:ring-2 focus-visible:ring-brand-500/60`}
    >
      <div className="flex items-start justify-between">
        <p className={`font-display text-[2rem] font-semibold leading-none tabular-nums tracking-tight ${muted ? 'text-ink-500' : 'text-ink-900'}`}>
          {room.roomNumber}
        </p>
        <span className={`mt-1 h-2 w-2 rounded-full ${s.dot}`} />
      </div>
      <p className="mt-4 text-[9px] font-bold uppercase tracking-[0.22em] text-ink-500">{room.typeName}</p>
      <p className="mt-1.5 h-4 truncate text-[12px]">
        {room.guestName
          ? <span className="font-display italic text-ink-700">{room.guestName}</span>
          : <span className="tabular-nums text-ink-500">{formatVnd(room.basePrice)}<span className="opacity-60"> /đêm</span></span>}
      </p>
      <p className={`mt-2 text-[10px] font-semibold ${muted ? 'text-ink-500' : 'text-ink-700'}`}>{s.label}</p>
    </button>
  )
}

/* Drawer chi tiết phòng - trượt từ phải */
function RoomDrawer({ room, onClose }) {
  const navigate = useNavigate()
  useEffect(() => {
    const onKey = (e) => e.key === 'Escape' && onClose()
    window.addEventListener('keydown', onKey)
    return () => window.removeEventListener('keydown', onKey)
  }, [onClose])

  return (
    <>
      <div
        onClick={onClose}
        className={`fixed inset-0 z-30 bg-ink-900/30 ${EASE} ${room ? 'opacity-100' : 'pointer-events-none opacity-0'}`}
      />
      <aside
        className={`fixed inset-y-0 right-0 z-40 w-full max-w-sm bg-cream-50 shadow-lift ${EASE} duration-500 ${room ? 'translate-x-0' : 'translate-x-full'}`}
      >
        {room && (
          <div className="flex h-full flex-col">
            <div className="relative h-52 shrink-0 overflow-hidden">
              <img src={roomImage(room.typeName, 0)} alt={room.typeName} className="h-full w-full object-cover" />
              <div className="absolute inset-0 bg-gradient-to-t from-ink-900/70 via-transparent to-transparent" />
              <button
                onClick={onClose}
                className="absolute right-3 top-3 flex h-8 w-8 items-center justify-center rounded-full bg-white/85 text-sm font-bold text-ink-700 backdrop-blur-sm hover:bg-white"
                aria-label="Đóng"
              >
                ✕
              </button>
              <div className="absolute bottom-3 left-4">
                <p className="font-display text-4xl font-semibold text-white">{room.roomNumber}</p>
                <p className="text-[12px] font-medium text-white/80">{room.typeName} · {formatVnd(room.basePrice)}/đêm</p>
              </div>
            </div>

            <div className="flex-1 space-y-5 overflow-y-auto p-5">
              <div className="flex items-center justify-between">
                <p className="text-[12px] font-semibold text-ink-500">Trạng thái hiện tại</p>
                <StatusFlag status={room.status} />
              </div>
              {room.guestName && (
                <div className="rounded-xl bg-white p-4 ring-1 ring-black/5">
                  <p className="text-[11px] font-semibold uppercase tracking-wider text-ink-500">Khách đang gắn với phòng</p>
                  <p className="mt-1.5 font-display text-lg font-semibold">{room.guestName}</p>
                </div>
              )}
              <div className="rounded-xl bg-white p-4 ring-1 ring-black/5">
                <p className="text-[11px] font-semibold uppercase tracking-wider text-ink-500">Thao tác nhanh</p>
                <div className="mt-3 space-y-2">
                  <button
                    onClick={() => navigate('/reservations/new')}
                    disabled={room.status !== 'Available'}
                    className={`w-full rounded-full bg-brand-600 py-2.5 text-[13px] font-bold text-white ${EASE} hover:bg-brand-700 active:scale-[0.98] disabled:opacity-30`}
                  >
                    Đặt phòng này
                  </button>
                  <button
                    disabled
                    title="Task T4 - CheckInPage"
                    className="w-full rounded-full py-2.5 text-[13px] font-semibold text-ink-500 ring-1 ring-black/10 disabled:opacity-50"
                  >
                    Check-in (mở khóa ở T4)
                  </button>
                  <button
                    disabled
                    title="Chờ API PATCH /api/rooms/{id}/status"
                    className="w-full rounded-full py-2.5 text-[13px] font-semibold text-ink-500 ring-1 ring-black/10 disabled:opacity-50"
                  >
                    Đổi trạng thái (chờ API)
                  </button>
                </div>
              </div>
              <p className="text-center text-[11px] italic text-ink-500/70">
                Dữ liệu chi tiết stay/hóa đơn sẽ hiện ở đây khi backend hoàn thành.
              </p>
            </div>
          </div>
        )}
      </aside>
    </>
  )
}

const selectCls =
  'w-full rounded-lg bg-white px-3 py-2 text-[13px] font-medium text-ink-700 ring-1 ring-black/10 outline-none focus:ring-2 focus:ring-brand-500/40 hover:ring-black/20'

export default function RoomMapPage() {
  const [floors, setFloors] = useState(null)
  const [usingMock, setUsingMock] = useState(false)
  const [floorFilter, setFloorFilter] = useState('all')
  const [typeFilter, setTypeFilter] = useState('all')
  const [statusFilter, setStatusFilter] = useState('all')
  const [openRoom, setOpenRoom] = useState(null)

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
  const occupied = count('Occupied') + count('Reserved')

  // Mặt cắt tòa nhà: tầng cao nhất nằm trên cùng
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
        .filter((f) => f.rooms.length > 0)
        .sort((a, b) => a.floor - b.floor),
    [floors, floorFilter, typeFilter, statusFilter],
  )

  const statusRows = Object.entries(ROOM_STATUS).map(([key, s]) => ({
    key, ...s, n: count(key), pct: allRooms.length ? (count(key) / allRooms.length) * 100 : 0,
  }))

  return (
    <div className="mx-auto max-w-7xl lg:grid lg:grid-cols-[290px_1fr] lg:gap-10">
      {/* Rail trái: tiêu đề + công suất + thống kê + bộ lọc */}
      <aside className="lg:sticky lg:top-20 lg:self-start">
        <p className="font-display text-[15px] italic text-brand-600">tình trạng khách sạn hôm nay</p>
        <h1 className="mt-1 font-display text-4xl font-semibold tracking-tight">Sơ đồ phòng</h1>
        {usingMock && (
          <p className="mt-2 flex items-center gap-1.5 text-[11px] font-medium text-ink-500">
            <span className="h-1.5 w-1.5 animate-pulse rounded-full bg-amber-500" />
            dữ liệu mẫu, chờ API
          </p>
        )}

        <div className="mt-6 grid grid-cols-3 gap-3 border-y border-black/[0.07] py-4">
          <div>
            <p className="font-display text-2xl font-semibold tabular-nums leading-none">{allRooms.length}</p>
            <p className="mt-1.5 text-[11px] text-ink-500">tổng phòng</p>
          </div>
          <div>
            <p className="font-display text-2xl font-semibold tabular-nums leading-none">{count('Available')}</p>
            <p className="mt-1.5 text-[11px] text-ink-500">sẵn sàng</p>
          </div>
          <div>
            <p className="font-display text-2xl font-semibold tabular-nums leading-none">
              {allRooms.length ? Math.round((occupied / allRooms.length) * 100) : 0}%
            </p>
            <p className="mt-1.5 text-[11px] text-ink-500">lấp đầy</p>
          </div>
        </div>

        <div className="mt-6 space-y-2.5">
          {statusRows.map((s) => (
            <button
              key={s.key}
              onClick={() => setStatusFilter(statusFilter === s.key ? 'all' : s.key)}
              className={`w-full text-left ${EASE} ${statusFilter === s.key ? '' : 'opacity-80 hover:opacity-100'}`}
            >
              <div className="flex items-center justify-between text-[12px]">
                <span className={`flex items-center gap-2 font-medium ${statusFilter === s.key ? 'text-ink-900' : 'text-ink-700'}`}>
                  <span className={`h-1.5 w-1.5 rounded-full ${s.dot}`} />
                  {s.label}
                </span>
                <span className="tabular-nums text-ink-500">{s.n}</span>
              </div>
              <div className="mt-1 h-1 overflow-hidden rounded-full bg-black/[0.06]">
                <div className={`h-full rounded-full ${s.strip} ${EASE} duration-700`} style={{ width: `${s.pct}%` }} />
              </div>
            </button>
          ))}
        </div>

        <div className="mt-6 space-y-2.5 border-t border-black/[0.07] pt-5">
          <select className={selectCls} value={floorFilter} onChange={(e) => setFloorFilter(e.target.value)}>
            <option value="all">Tất cả tầng</option>
            {(floors ?? []).map((f) => <option key={f.floor} value={f.floor}>Tầng {f.floor}</option>)}
          </select>
          <select className={selectCls} value={typeFilter} onChange={(e) => setTypeFilter(e.target.value)}>
            <option value="all">Tất cả loại phòng</option>
            {MOCK_ROOM_TYPES.map((t) => <option key={t} value={t}>{t}</option>)}
          </select>
          <button
            onClick={load}
            className={`w-full rounded-full py-2 text-[13px] font-semibold text-ink-700 ring-1 ring-black/10 ${EASE} hover:bg-white hover:ring-black/20 active:scale-[0.98]`}
          >
            Làm mới dữ liệu
          </button>
        </div>
      </aside>

      {/* Mặt cắt tòa nhà: trục thang máy bên trái, tầng cao trên cùng, sảnh dưới cùng */}
      <div className="mt-10 lg:mt-2">
        {floors === null && (
          <div className="overflow-hidden rounded-xl ring-1 ring-black/10">
            <div className="grid grid-cols-2 sm:grid-cols-3 lg:grid-cols-4">
              {Array.from({ length: 8 }).map((_, j) => (
                <div key={j} className="-mb-px -mr-px h-32 animate-pulse border border-black/[0.06] bg-cream-50" />
              ))}
            </div>
          </div>
        )}

        {visibleFloors.map((f, fi) => (
          <section key={f.floor} className="mb-10">
            {/* Số tầng in mờ khổ lớn kiểu editorial */}
            <div className="flex items-end gap-4">
              <span className="select-none font-display text-7xl font-semibold leading-[0.8] text-ink-900/[0.08]">
                0{f.floor}
              </span>
              <div className="pb-1">
                <h2 className="font-display text-lg font-semibold italic leading-none">Tầng {f.floor}</h2>
                <p className="mt-1.5 text-[10px] font-bold uppercase tracking-[0.22em] text-ink-500">{f.rooms.length} phòng</p>
              </div>
            </div>
            <div className="mt-4 overflow-hidden rounded-xl bg-white ring-1 ring-black/10">
              <div className="grid grid-cols-2 sm:grid-cols-3 lg:grid-cols-4 2xl:grid-cols-5">
                {f.rooms.map((room, idx) => (
                  <RoomTile key={room.roomId} room={room} delay={fi * 120 + idx * 50} onOpen={setOpenRoom} />
                ))}
              </div>
            </div>
          </section>
        ))}

        {floors !== null && visibleFloors.length === 0 && (
          <div className="rounded-2xl border border-dashed border-black/10 bg-white/60 p-10 text-center">
            <p className="font-display text-lg italic text-ink-700">Không có phòng nào khớp bộ lọc</p>
            <p className="mt-1 text-[13px] text-ink-500">Thử bỏ bớt điều kiện lọc ở cột bên trái.</p>
          </div>
        )}
      </div>

      <RoomDrawer room={openRoom} onClose={() => setOpenRoom(null)} />
    </div>
  )
}
