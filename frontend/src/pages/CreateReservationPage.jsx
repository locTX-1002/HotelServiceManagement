import { useMemo, useState } from 'react'
import client from '../api/client'
import { formatVnd } from '../utils/roomStatus'
import { MOCK_AVAILABLE_ROOMS, MOCK_ROOM_TYPES } from '../mock/hotelMock'

const EASE = 'transition-all duration-300 ease-[cubic-bezier(0.32,0.72,0,1)]'
const inputCls =
  'w-full rounded-xl bg-white px-3.5 py-2.5 text-sm ring-1 ring-black/10 outline-none placeholder:text-ink-500/50 focus:ring-2 focus:ring-brand-500/40'
const labelCls = 'mb-1.5 block text-[12px] font-semibold text-ink-700'

const today = () => new Date().toISOString().slice(0, 10)
const addDays = (dateStr, n) => {
  const d = new Date(dateStr)
  d.setDate(d.getDate() + n)
  return d.toISOString().slice(0, 10)
}

export default function CreateReservationPage() {
  const [guest, setGuest] = useState({ fullName: '', phoneNumber: '', email: '', identityNumber: '' })
  const [checkIn, setCheckIn] = useState(today())
  const [checkOut, setCheckOut] = useState(addDays(today(), 1))
  const [roomType, setRoomType] = useState('all')
  const [results, setResults] = useState(null)
  const [selected, setSelected] = useState(null)
  const [usingMock, setUsingMock] = useState(false)
  const [message, setMessage] = useState(null)

  const nights = useMemo(() => {
    const ms = new Date(checkOut) - new Date(checkIn)
    return Math.max(Math.round(ms / 86400000), 0)
  }, [checkIn, checkOut])

  const search = () => {
    setSelected(null)
    setMessage(null)
    client
      .get('/api/reservations/available-rooms', { params: { checkIn, checkOut, roomType } })
      .then((res) => { setResults(res.data); setUsingMock(false) })
      .catch(() => {
        const list = MOCK_AVAILABLE_ROOMS.filter((r) => roomType === 'all' || r.typeName === roomType)
        setResults(list)
        setUsingMock(true)
      })
  }

  const submit = () => {
    if (!selected) return
    const payload = { ...guest, roomId: selected.roomId, checkInDate: checkIn, checkOutDate: checkOut }
    client
      .post('/api/reservations', payload)
      .then((res) => setMessage({ ok: true, text: `Tạo đặt phòng thành công. Mã booking: ${res.data.bookingCode ?? ''}` }))
      .catch(() => setMessage({ ok: false, text: 'API /api/reservations chưa sẵn sàng (task T3). Form và luồng chọn phòng đã hoạt động.' }))
  }

  const canSearch = guest.fullName.trim() && nights > 0

  return (
    <div className="mx-auto max-w-6xl">
      <h1 className="text-2xl font-extrabold tracking-tight">Tạo đặt phòng</h1>
      <p className="mt-1 text-sm text-ink-500">Nhập thông tin khách, chọn ngày ở rồi tìm phòng trống — tất cả trong một màn hình.</p>

      <div className="mt-6 grid gap-6 lg:grid-cols-5">
        {/* Cột trái: khách + ngày ở */}
        <div className="lg:col-span-3">
          <div className="rounded-2xl bg-white p-6 ring-1 ring-black/5 shadow-soft">
            <h2 className="text-sm font-bold uppercase tracking-[0.14em] text-ink-500">Thông tin khách</h2>
            <div className="mt-4 grid gap-4 sm:grid-cols-2">
              <div className="sm:col-span-2">
                <label className={labelCls}>Họ và tên *</label>
                <input className={inputCls} placeholder="Nguyễn Văn A" value={guest.fullName}
                  onChange={(e) => setGuest({ ...guest, fullName: e.target.value })} />
              </div>
              <div>
                <label className={labelCls}>Số điện thoại</label>
                <input className={inputCls} placeholder="09xx xxx xxx" value={guest.phoneNumber}
                  onChange={(e) => setGuest({ ...guest, phoneNumber: e.target.value })} />
              </div>
              <div>
                <label className={labelCls}>CMND / CCCD</label>
                <input className={inputCls} placeholder="0790xxxxxxxx" value={guest.identityNumber}
                  onChange={(e) => setGuest({ ...guest, identityNumber: e.target.value })} />
              </div>
              <div className="sm:col-span-2">
                <label className={labelCls}>Email</label>
                <input className={inputCls} placeholder="khach@email.com" value={guest.email}
                  onChange={(e) => setGuest({ ...guest, email: e.target.value })} />
              </div>
            </div>

            <h2 className="mt-7 text-sm font-bold uppercase tracking-[0.14em] text-ink-500">Ngày ở</h2>
            <div className="mt-4 grid gap-4 sm:grid-cols-3">
              <div>
                <label className={labelCls}>Nhận phòng</label>
                <input type="date" className={inputCls} value={checkIn} min={today()}
                  onChange={(e) => { setCheckIn(e.target.value); if (e.target.value >= checkOut) setCheckOut(addDays(e.target.value, 1)) }} />
              </div>
              <div>
                <label className={labelCls}>Trả phòng</label>
                <input type="date" className={inputCls} value={checkOut} min={addDays(checkIn, 1)}
                  onChange={(e) => setCheckOut(e.target.value)} />
              </div>
              <div>
                <label className={labelCls}>Loại phòng</label>
                <select className={inputCls} value={roomType} onChange={(e) => setRoomType(e.target.value)}>
                  <option value="all">Tất cả</option>
                  {MOCK_ROOM_TYPES.map((t) => <option key={t} value={t}>{t}</option>)}
                </select>
              </div>
            </div>

            <button
              onClick={search}
              disabled={!canSearch}
              className={`mt-6 inline-flex items-center gap-3 rounded-full bg-brand-600 py-2.5 pl-6 pr-2.5 text-sm font-semibold text-white ${EASE} hover:bg-brand-700 active:scale-[0.98] disabled:cursor-not-allowed disabled:opacity-40`}
            >
              Tìm phòng trống
              <span className={`flex h-7 w-7 items-center justify-center rounded-full bg-white/15 text-xs ${EASE} group-hover:translate-x-0.5`}>→</span>
            </button>
            {!canSearch && (
              <p className="mt-2 text-[12px] text-ink-500">Cần họ tên khách và ngày trả phòng sau ngày nhận phòng.</p>
            )}
          </div>
        </div>

        {/* Cột phải: phòng trống + tổng kết */}
        <div className="lg:col-span-2">
          <div className="rounded-2xl bg-white p-6 ring-1 ring-black/5 shadow-soft">
            <div className="flex items-center justify-between">
              <h2 className="text-sm font-bold uppercase tracking-[0.14em] text-ink-500">Phòng trống</h2>
              {nights > 0 && <span className="rounded-full bg-cream-200 px-2.5 py-1 text-[11px] font-semibold text-ink-700">{nights} đêm</span>}
            </div>

            {results === null && (
              <p className="mt-6 text-sm text-ink-500">Bấm "Tìm phòng trống" để xem danh sách phòng khả dụng trong khoảng ngày đã chọn.</p>
            )}
            {usingMock && results !== null && (
              <p className="mt-3 text-[11px] font-medium text-brand-700">Dữ liệu mẫu — chờ API available-rooms (T3).</p>
            )}
            {results !== null && results.length === 0 && (
              <p className="mt-6 text-sm text-ink-500">Không còn phòng trống phù hợp trong khoảng ngày này.</p>
            )}

            <div className="mt-4 space-y-3">
              {(results ?? []).map((room) => {
                const active = selected?.roomId === room.roomId
                return (
                  <button
                    key={room.roomId}
                    onClick={() => setSelected(room)}
                    className={`w-full rounded-xl p-3.5 text-left ring-1 ${EASE} ${
                      active ? 'bg-brand-50 ring-brand-600/40 shadow-soft' : 'bg-cream-50 ring-black/5 hover:ring-black/15'
                    }`}
                  >
                    <div className="flex items-center justify-between">
                      <p className="font-bold">Phòng {room.roomNumber}</p>
                      <span className={`h-4 w-4 rounded-full border-2 ${active ? 'border-brand-600 bg-brand-600' : 'border-black/20'}`} />
                    </div>
                    <p className="mt-0.5 text-[12px] text-ink-500">{room.typeName} · Tầng {room.floor}</p>
                    <p className="mt-1.5 text-[13px] font-semibold text-ink-700">{formatVnd(room.basePrice)} <span className="font-normal text-ink-500">/ đêm</span></p>
                  </button>
                )
              })}
            </div>
          </div>

          <div className="mt-4 rounded-2xl bg-ink-900 p-6 text-cream-50 shadow-soft">
            <h2 className="text-sm font-bold uppercase tracking-[0.14em] text-cream-50/60">Tổng kết</h2>
            <div className="mt-3 space-y-1.5 text-sm">
              <p className="flex justify-between"><span className="text-cream-50/70">Phòng</span><span className="font-semibold">{selected ? `${selected.roomNumber} · ${selected.typeName}` : '—'}</span></p>
              <p className="flex justify-between"><span className="text-cream-50/70">Số đêm</span><span className="font-semibold">{nights}</span></p>
              <p className="flex justify-between border-t border-white/10 pt-2 text-base"><span className="text-cream-50/70">Tạm tính</span><span className="font-extrabold">{selected ? formatVnd(selected.basePrice * nights) : '—'}</span></p>
            </div>
            <button
              onClick={submit}
              disabled={!selected}
              className={`mt-5 w-full rounded-full bg-brand-500 py-3 text-sm font-bold text-white ${EASE} hover:bg-brand-600 active:scale-[0.98] disabled:cursor-not-allowed disabled:opacity-30`}
            >
              Tạo đặt phòng
            </button>
            {message && (
              <p className={`mt-3 text-[12px] font-medium ${message.ok ? 'text-emerald-300' : 'text-amber-300'}`}>{message.text}</p>
            )}
          </div>
        </div>
      </div>
    </div>
  )
}
