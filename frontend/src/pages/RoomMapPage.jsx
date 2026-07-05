import { useEffect, useMemo, useState } from 'react'
import { useNavigate } from 'react-router-dom'
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

function RoomCard({ room, imgIdx, delay, onOpen }) {
  const dimmed = room.status === 'Maintenance' || room.status === 'Cleaning'
  return (
    <button
      onClick={() => onOpen(room)}
      style={{ animationDelay: `${delay}ms` }}
      className={`card-rise group overflow-hidden rounded-xl bg-white text-left ring-1 ring-black/5 ${EASE} hover:-translate-y-1 hover:shadow-lift hover:ring-brand-500/30 focus-visible:ring-2 focus-visible:ring-brand-500/60 outline-none`}
    >
      <div className="relative h-20 overflow-hidden">
        <img
          src={roomImage(room.typeName, imgIdx)}
          alt={`Phòng ${room.roomNumber} - ${room.typeName}`}
          loading="lazy"
          className={`h-full w-full object-cover ${roomImagePosition(imgIdx)} ${EASE} duration-700 group-hover:scale-[1.06] ${dimmed ? 'opacity-75 saturate-50' : ''}`}
        />
        <div className="absolute inset-0 bg-gradient-to-t from-ink-900/55 via-transparent to-transparent" />
        <div className="absolute right-1.5 top-1.5"><StatusFlag status={room.status} /></div>
        <p className="absolute bottom-1 left-2.5 font-display text-xl font-semibold text-white drop-shadow-sm">{room.roomNumber}</p>
      </div>
      <div className="px-3 py-2">
        <div className="flex items-baseline justify-between gap-2">
          <p className="truncate text-[11px] font-semibold text-ink-700">{room.typeName}</p>
          <p className="shrink-0 text-[11px] tabular-nums text-ink-500">
            {room.guestName
              ? <span className="italic">{room.guestName.split(' ').slice(-2).join(' ')}</span>
              : <>{formatVnd(room.basePrice)}<span className="opacity-60">/đêm</span></>}
          </p>
        </div>
      </div>
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
      <div className="relative mt-10 lg:mt-2">
        <div className="absolute bottom-0 left-[15px] top-3 w-px bg-black/10" />

        {floors === null && (
          <div className="space-y-6 pl-12">
            {[0, 1].map((i) => (
              <div key={i} className="grid grid-cols-2 gap-4 sm:grid-cols-3">
                {[0, 1, 2].map((j) => (
                  <div key={j} className="overflow-hidden rounded-xl bg-white ring-1 ring-black/5">
                    <div className="h-24 animate-pulse bg-cream-200" />
                    <div className="space-y-2 px-3.5 py-3">
                      <div className="h-3 w-2/3 animate-pulse rounded bg-cream-200" />
                    </div>
                  </div>
                ))}
              </div>
            ))}
          </div>
        )}

        {visibleFloors.map((f, fi) => (
          <section key={f.floor} className="relative mb-7 pl-12">
            <div className="absolute left-0 top-1 flex h-8 w-8 items-center justify-center rounded-full bg-ink-900 font-display text-[13px] font-semibold text-cream-50 ring-4 ring-cream-100">
              {f.floor}
            </div>
            <div className="mb-3 flex items-baseline gap-3">
              <h2 className="font-display text-lg font-semibold italic">Tầng {f.floor}</h2>
              <p className="text-[11px] tabular-nums text-ink-500">{f.rooms.length} phòng</p>
            </div>
            <div className="grid grid-cols-2 gap-3 sm:grid-cols-3 lg:grid-cols-4 2xl:grid-cols-5">
              {f.rooms.map((room, idx) => (
                <RoomCard key={room.roomId} room={room} imgIdx={idx} delay={fi * 120 + idx * 50} onOpen={setOpenRoom} />
              ))}
            </div>
          </section>
        ))}

        {floors !== null && visibleFloors.length === 0 && (
          <div className="ml-12 rounded-2xl border border-dashed border-black/10 bg-white/60 p-10 text-center">
            <p className="font-display text-lg italic text-ink-700">Không có phòng nào khớp bộ lọc</p>
            <p className="mt-1 text-[13px] text-ink-500">Thử bỏ bớt điều kiện lọc ở cột bên trái.</p>
          </div>
        )}
      </div>

      <RoomDrawer room={openRoom} onClose={() => setOpenRoom(null)} />
    </div>
  )
}
