import { useEffect, useMemo, useRef, useState } from 'react'
import { EASE } from '../utils/ui'
import { useNavigate } from 'react-router-dom'
import client, { isBackendMissing } from '../api/client'
import { ROOM_STATUS, formatVnd } from '../utils/roomStatus'
import { MOCK_ROOM_MAP } from '../mock/hotelMock'
import { normalizeRoom } from '../utils/apiShape'
import { roomImage } from '../utils/roomImages'
import ErrorState from '../components/ErrorState'
import { canAccess } from '../utils/roles'
import { getUser } from '../utils/session'

const dateLabel = new Intl.DateTimeFormat('vi-VN', { weekday: 'long', day: '2-digit', month: 'long' }).format(new Date())

// 'Nguyễn Văn An' -> 'Nguyễn V. An' - viết tắt tên đệm, không cắt cụt tên
const shortName = (name) => {
  const p = name.trim().split(/\s+/)
  if (p.length <= 2) return name
  return `${p[0]} ${p.slice(1, -1).map((x) => x[0] + '.').join(' ')} ${p[p.length - 1]}`
}

/* Nội dung 2 dòng dưới của thẻ theo trạng thái: dòng chính + dòng thời gian */
const tileInfo = (room) => {
  switch (room.status) {
    // API thật chưa trả guestName/checkOutAt/eta -> phải có chữ thay thế, không để dòng tên trống
    case 'Occupied':
      return { main: room.guestName ? shortName(room.guestName) : 'Có khách', sub: room.checkOutAt ? `CO ${room.checkOutAt}` : 'đang ở', subCls: 'text-rose-700' }
    case 'Reserved':
      return { main: room.guestName ? shortName(room.guestName) : 'Đã có đặt phòng', sub: room.eta ? `Đến ${room.eta}` : 'chờ nhận phòng', subCls: 'text-sky-700' }
    case 'Cleaning':
      return { main: 'Đang dọn phòng', sub: room.cleaningEta ? `~ ${room.cleaningEta}` : 'sắp xong', subCls: 'text-amber-700' }
    case 'Maintenance':
      return { main: 'Bảo trì', sub: 'tạm khóa', subCls: 'text-ink-500' }
    default:
      return { main: 'Sẵn sàng đón khách', sub: formatVnd(room.basePrice), subCls: 'text-ink-500' }
  }
}

const TILE_TINT = {
  Available: 'bg-white ring-1 ring-black/[0.07]',
  Reserved: 'bg-sky-50 ring-1 ring-sky-600/15',
  Occupied: 'bg-rose-50 ring-1 ring-rose-600/15',
  Cleaning: 'bg-amber-50 ring-1 ring-amber-600/15',
}

/* Thẻ phòng hình vòm kiểu thẻ chìa khóa - ảnh phòng nằm trong ô cửa vòm */
function RoomTile({ room, imgIdx = 0, delay, onOpen }) {
  const s = ROOM_STATUS[room.status] ?? ROOM_STATUS.Maintenance
  const info = tileInfo(room)
  const maintenance = room.status === 'Maintenance'
  return (
    <button
      onClick={() => onOpen(room)}
      style={{ animationDelay: `${delay}ms` }}
      className={`card-rise group rounded-t-[999px] rounded-b-2xl p-2.5 pb-4 text-center outline-none ${EASE} focus-visible:ring-2 focus-visible:ring-brand-500/60 ${
        maintenance
          ? 'border-2 border-dashed border-black/15 opacity-55 hover:opacity-80'
          : `${TILE_TINT[room.status]} shadow-soft hover:-translate-y-1 hover:shadow-lift`
      }`}
    >
      {/* Ô cửa vòm: viền thẻ (nền nhuộm trạng thái) làm khung quanh ảnh */}
      <div className="relative">
        <div className="overflow-hidden rounded-t-[999px] rounded-b-lg">
          <img
            src={roomImage(room.typeName, imgIdx)}
            alt={room.typeName}
            loading="lazy"
            className={`h-24 w-full object-cover ${EASE} duration-700 group-hover:scale-[1.07] ${maintenance ? 'grayscale' : ''}`}
          />
        </div>
        <span className={`absolute -bottom-1 left-1/2 h-2.5 w-2.5 -translate-x-1/2 rounded-full ring-2 ring-white ${s.dot}`} />
      </div>
      <p className="mt-3 font-display text-[1.7rem] font-semibold leading-none tabular-nums tracking-tight">{room.roomNumber}</p>
      <p className="mt-1.5 text-[9px] font-bold uppercase tracking-[0.24em] text-ink-500">{room.typeName}</p>
      <div className="mx-5 mt-2.5 border-t border-dotted border-ink-500/30" />
      <p className="mt-2 truncate px-1 text-[13px] font-semibold text-ink-700" title={room.guestName ?? info.main}>{info.main}</p>
      <p className={`mt-0.5 text-[12px] font-semibold tabular-nums ${info.subCls}`}>{info.sub}</p>
    </button>
  )
}

/* Ô ma "+" lấp chỗ trống cuối hàng */
function GhostTile({ onClick }) {
  return (
    <button
      onClick={onClick}
      title="Quản lý phòng"
      className={`rounded-t-[999px] rounded-b-2xl border-2 border-dashed border-black/10 px-4 pb-5 pt-9 text-center text-ink-500/40 ${EASE} hover:border-black/25 hover:text-ink-500`}
    >
      <p className="font-display text-4xl font-medium">+</p>
      <p className="mt-2 text-[9px] font-bold uppercase tracking-[0.24em]">Thêm phòng</p>
    </button>
  )
}

/* Drawer chi tiết - hành động theo ngữ cảnh trạng thái */
function RoomDrawer({ room, onClose }) {
  const navigate = useNavigate()
  useEffect(() => {
    const onKey = (e) => e.key === 'Escape' && onClose()
    window.addEventListener('keydown', onKey)
    return () => window.removeEventListener('keydown', onKey)
  }, [onClose])

  const actions = room
    ? {
        Available: [
          { label: 'Đặt phòng này', primary: true, onClick: () => navigate('/reservations/new') },
          { label: 'Chuyển sang bảo trì', note: 'chờ API PATCH status' },
        ],
        Reserved: [
          { label: 'Check-in khách', primary: true, onClick: () => navigate('/checkin-checkout') },
          { label: 'Hủy đặt phòng', note: 'sơ đồ chưa có mã đặt phòng - hủy ở trang Đặt phòng' },
        ],
        Occupied: [
          { label: 'Check-out', primary: true, onClick: () => navigate('/checkin-checkout') },
          { label: 'Thêm dịch vụ', onClick: () => navigate('/service-orders') },
        ],
        Cleaning: [{ label: 'Đánh dấu đã sạch', primary: true, note: 'chờ API PATCH status' }],
        Maintenance: [{ label: 'Mở lại phòng', primary: true, note: 'chờ API PATCH status' }],
      }[room.status] ?? []
    : []

  return (
    <>
      <div onClick={onClose}
        className={`fixed inset-0 z-30 bg-ink-900/30 ${EASE} ${room ? 'opacity-100' : 'pointer-events-none opacity-0'}`} />
      <aside className={`fixed inset-y-0 right-0 z-40 w-full max-w-sm bg-cream-50 shadow-lift ${EASE} duration-500 ${room ? 'translate-x-0' : 'translate-x-full'}`}>
        {room && (
          <div className="flex h-full flex-col">
            <div className="relative h-48 shrink-0 overflow-hidden">
              <img src={roomImage(room.typeName, 0)} alt={room.typeName} className="h-full w-full object-cover" />
              <div className="absolute inset-0 bg-gradient-to-t from-ink-900/70 via-transparent to-transparent" />
              <button onClick={onClose} aria-label="Đóng"
                className="absolute right-3 top-3 flex h-8 w-8 items-center justify-center rounded-full bg-white/85 text-sm font-bold text-ink-700 backdrop-blur-sm hover:bg-white">
                ✕
              </button>
              <div className="absolute bottom-3 left-4">
                <p className="font-display text-4xl font-semibold text-white">{room.roomNumber}</p>
                <p className="text-[12px] font-medium text-white/80">{room.typeName} · {formatVnd(room.basePrice)}/đêm</p>
              </div>
            </div>

            <div className="flex-1 space-y-4 overflow-y-auto p-5">
              <div className="flex items-center justify-between rounded-xl bg-white p-4 ring-1 ring-black/5">
                <div>
                  <p className="text-[10px] font-bold uppercase tracking-[0.2em] text-ink-500">Trạng thái</p>
                  <p className="mt-1 text-sm font-bold">{(ROOM_STATUS[room.status] ?? {}).label}</p>
                </div>
                <div className="text-right">
                  {/* Nhãn phải khớp giá trị thật đang có - API chưa trả checkOutAt/eta thì rơi về Giá/đêm */}
                  <p className="text-[10px] font-bold uppercase tracking-[0.2em] text-ink-500">
                    {room.status === 'Occupied' && room.checkOutAt ? 'Check-out'
                      : room.status === 'Reserved' && room.eta ? 'Dự kiến đến'
                        : room.status === 'Cleaning' && room.cleaningEta ? 'Còn lại'
                          : 'Giá / đêm'}
                  </p>
                  <p className="mt-1 text-sm font-bold tabular-nums">
                    {room.checkOutAt ?? room.eta ?? room.cleaningEta ?? formatVnd(room.basePrice)}
                  </p>
                </div>
              </div>

              {room.guestName && (
                <div className="rounded-xl bg-white p-4 ring-1 ring-black/5">
                  <p className="text-[10px] font-bold uppercase tracking-[0.2em] text-ink-500">Khách</p>
                  <p className="mt-1.5 font-display text-lg font-semibold">{room.guestName}</p>
                </div>
              )}

              <div className="rounded-xl bg-white p-4 ring-1 ring-black/5">
                <p className="text-[10px] font-bold uppercase tracking-[0.2em] text-ink-500">Thao tác</p>
                <div className="mt-3 space-y-2">
                  {actions.map((a) => (
                    <button
                      key={a.label}
                      onClick={a.onClick}
                      disabled={!a.onClick}
                      title={a.note}
                      className={`w-full rounded-full py-2.5 text-[13px] font-bold ${EASE} ${
                        a.primary
                          ? 'bg-brand-600 text-white hover:bg-brand-700 active:scale-[0.98] disabled:opacity-40'
                          : 'text-ink-700 ring-1 ring-black/10 hover:bg-cream-50 disabled:opacity-50'
                      }`}
                    >
                      {a.label}{!a.onClick && a.note ? ' · ' + a.note : ''}
                    </button>
                  ))}
                </div>
              </div>
            </div>
          </div>
        )}
      </aside>
    </>
  )
}

export default function RoomMapPage() {
  const navigate = useNavigate()
  const canManageRooms = canAccess(getUser()?.role, '/rooms')
  const [floors, setFloors] = useState(null)
  const [usingMock, setUsingMock] = useState(false)
  const [statusFilter, setStatusFilter] = useState('all')
  const [openRoom, setOpenRoom] = useState(null)
  const [updatedAt, setUpdatedAt] = useState(null)
  const [loadError, setLoadError] = useState(false)
  const timer = useRef(null)

  const load = () => {
    client
      .get('/api/rooms/map')
      .then((res) => {
        // Backend trả roomId dưới tên id, typeName dưới tên roomTypeName, status là số -> chuẩn hoá về đúng shape FE dùng
        const normalized = res.data.map((f) => ({ ...f, rooms: f.rooms.map(normalizeRoom) }))
        setFloors(normalized); setUsingMock(false); setLoadError(false); setUpdatedAt(new Date())
        // drawer đang mở thì cập nhật theo dữ liệu mới, tránh hiển thị trạng thái cũ
        setOpenRoom((cur) => (cur ? normalized.flatMap((f) => f.rooms).find((r) => r.roomId === cur.roomId) ?? cur : cur))
      })
      .catch((err) => {
        if (isBackendMissing(err)) {
          setFloors(MOCK_ROOM_MAP); setUsingMock(true); setLoadError(false)
        } else {
          setFloors([]); setLoadError(true) // lỗi thật: không che bằng mock
        }
        setUpdatedAt(new Date())
      })
  }

  // Tự làm mới mỗi 30s thay cho nút bấm tay
  useEffect(() => {
    load()
    timer.current = setInterval(load, 30000)
    return () => clearInterval(timer.current)
  }, [])

  const allRooms = useMemo(() => (floors ?? []).flatMap((f) => f.rooms), [floors])
  const count = (st) => allRooms.filter((r) => r.status === st).length
  const occupied = count('Occupied') + count('Reserved')

  const visibleFloors = useMemo(
    () =>
      (floors ?? [])
        .map((f) => ({ ...f, rooms: f.rooms.filter((r) => statusFilter === 'all' || r.status === statusFilter) }))
        .filter((f) => f.rooms.length > 0)
        .sort((a, b) => a.floor - b.floor),
    [floors, statusFilter],
  )

  const chips = [
    { key: 'all', label: 'Tất cả', n: allRooms.length },
    ...Object.entries(ROOM_STATUS)
      .map(([key, s]) => ({ key, label: s.label, n: count(key), dot: s.dot }))
      .filter((c) => c.n > 0),
  ]

  return (
    <div>
      {/* Header: ngày + tiêu đề trái, ô thống kê phải */}
      <div className="flex flex-wrap items-end justify-between gap-5">
        <div>
          <p className="font-display text-[15px] italic capitalize tracking-wide text-brand-600">{dateLabel}</p>
          <h1 className="mt-1 font-display text-4xl font-semibold tracking-tight">Sơ đồ phòng</h1>
        </div>
        <div className="bezel-shell">
          <div className="bezel-core flex divide-x divide-black/[0.06]">
            <div className="px-5 py-3 text-center">
              <p className="font-display text-2xl font-semibold tabular-nums leading-none">{allRooms.length}</p>
              <p className="mt-1 text-[9px] font-bold uppercase tracking-[0.2em] text-ink-500">Tổng</p>
            </div>
            <div className="px-5 py-3 text-center">
              <p className="font-display text-2xl font-semibold tabular-nums leading-none">{count('Available')}</p>
              <p className="mt-1 text-[9px] font-bold uppercase tracking-[0.2em] text-ink-500">Sẵn sàng</p>
            </div>
            <div className="px-5 py-3 text-center">
              <p className="font-display text-2xl font-semibold tabular-nums leading-none text-brand-600">
                {allRooms.length ? Math.round((occupied / allRooms.length) * 100) : 0}%
              </p>
              <p className="mt-1 text-[9px] font-bold uppercase tracking-[0.2em] text-ink-500">Lấp đầy</p>
            </div>
          </div>
        </div>
      </div>

      {/* Bộ lọc segmented: 1 thanh trắng, màu chỉ là chấm nhỏ, active nền đen */}
      <div className="mt-5 flex flex-wrap items-center gap-3">
        <div className="inline-flex flex-wrap items-center gap-0.5 rounded-full bg-white p-1 ring-1 ring-black/10 shadow-soft">
          {chips.map((c) => {
            const active = statusFilter === c.key
            return (
              <button
                key={c.key}
                onClick={() => setStatusFilter(c.key)}
                className={`flex items-center gap-1.5 rounded-full px-3.5 py-1.5 text-[12px] font-semibold ${EASE} active:scale-[0.97] ${
                  active ? 'bg-ink-900 text-cream-50' : 'text-ink-500 hover:text-ink-900'
                }`}
              >
                {c.dot && <span className={`h-1.5 w-1.5 rounded-full ${c.dot}`} />}
                {c.label}
                <span className={`tabular-nums font-medium ${active ? 'text-cream-50/60' : 'text-ink-500/50'}`}>{c.n}</span>
              </button>
            )
          })}
        </div>
        <div className="ml-auto flex items-center gap-2.5">
          {usingMock && (
            <span className="rounded-full bg-amber-50 px-2.5 py-1 text-[11px] font-bold text-amber-800 ring-1 ring-amber-600/20">
              Dữ liệu mẫu
            </span>
          )}
          {updatedAt && (
            <span className="text-[11px] tabular-nums text-ink-500">
              cập nhật {updatedAt.toLocaleTimeString('vi-VN', { hour: '2-digit', minute: '2-digit' })} · tự làm mới 30s
            </span>
          )}
        </div>
      </div>

      {/* Các tầng */}
      {floors === null && (
        <div className="mt-8 grid grid-cols-2 gap-4 sm:grid-cols-3 lg:grid-cols-4">
          {Array.from({ length: 4 }).map((_, i) => (
            <div key={i} className="h-48 animate-pulse rounded-t-[999px] rounded-b-2xl bg-cream-200" />
          ))}
        </div>
      )}

      {visibleFloors.map((f, fi) => (
        <section key={f.floor} className="mt-9">
          <div className="mb-4 flex items-baseline gap-3">
            <h2 className="font-display text-xl font-semibold italic">Tầng {f.floor}</h2>
            <p className="text-[10px] font-bold uppercase tracking-[0.22em] text-ink-500">· {f.rooms.length} phòng</p>
          </div>
          <div className="grid grid-cols-2 gap-4 sm:grid-cols-3 lg:grid-cols-4">
            {f.rooms.map((room, idx) => (
              <RoomTile key={room.roomId} room={room} imgIdx={idx} delay={fi * 100 + idx * 45} onOpen={setOpenRoom} />
            ))}
            {canManageRooms && statusFilter === 'all' && f.rooms.length % 4 !== 0 && <GhostTile onClick={() => navigate('/rooms')} />}
          </div>
        </section>
      ))}

      {loadError && (
        <div className="mt-8"><ErrorState onRetry={load} /></div>
      )}

      {!loadError && floors !== null && visibleFloors.length === 0 && (
        <div className="mt-10 rounded-2xl border border-dashed border-black/10 bg-white/60 p-10 text-center">
          <p className="font-display text-lg italic text-ink-700">Không có phòng nào ở trạng thái này</p>
          <button onClick={() => setStatusFilter('all')} className="mt-2 text-[12px] font-bold uppercase tracking-wider text-brand-600 hover:underline">
            Xem tất cả
          </button>
        </div>
      )}

      <RoomDrawer room={openRoom} onClose={() => setOpenRoom(null)} />
    </div>
  )
}
