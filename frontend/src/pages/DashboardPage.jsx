import { useEffect, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import client, { isBackendMissing } from '../api/client'
import ErrorState from '../components/ErrorState'
import { formatVnd } from '../utils/roomStatus'
import { MOCK_DASHBOARD } from '../mock/hotelMock'

const EASE = 'transition-all duration-300 ease-[cubic-bezier(0.32,0.72,0,1)]'

const todayLabel = new Intl.DateTimeFormat('vi-VN', { weekday: 'long', day: '2-digit', month: '2-digit' }).format(new Date())

// 'Nguyễn Văn An' -> 'NA' cho avatar vòm
const initials = (name) => {
  const p = name.trim().split(/\s+/)
  return ((p[0]?.[0] ?? '') + (p[p.length - 1]?.[0] ?? '')).toUpperCase()
}

/* Avatar khung vòm - lặp motif thẻ chìa khóa của trang Sơ đồ phòng */
function ArchAvatar({ name, tint }) {
  return (
    <span className={`flex h-10 w-8 shrink-0 items-end justify-center rounded-t-full rounded-b-md pb-1 text-[11px] font-bold ${tint}`}>
      {initials(name)}
    </span>
  )
}

function Panel({ title, hint, children }) {
  return (
    <div className="rounded-2xl bg-white p-6 ring-1 ring-black/5 shadow-soft">
      <div className="flex items-baseline justify-between">
        <h2 className="font-display text-lg font-semibold">{title}</h2>
        {hint && <p className="text-[11px] text-ink-500">{hint}</p>}
      </div>
      <div className="mt-2">{children}</div>
    </div>
  )
}

/* Empty state có icon vòm mờ - không trông như lỗi */
function EmptyState({ text }) {
  return (
    <div className="flex flex-col items-center py-8">
      <span className="h-10 w-8 rounded-t-full rounded-b-md border-2 border-dashed border-black/15" />
      <p className="mt-3 text-[13px] italic text-ink-500">{text}</p>
    </div>
  )
}

/* Badge hành động: nghỉ hiện trạng thái, hover đổi thành lời gọi hành động */
function ActionBadge({ idle, hover, tint, onClick }) {
  return (
    <button
      onClick={onClick}
      className={`group/b shrink-0 rounded-full px-2.5 py-1 text-[11px] font-semibold ring-1 ${EASE} ${tint}`}
    >
      <span className="group-hover/b:hidden">{idle}</span>
      <span className="hidden group-hover/b:inline">{hover} →</span>
    </button>
  )
}

export default function DashboardPage() {
  const navigate = useNavigate()
  const [data, setData] = useState(null)
  const [usingMock, setUsingMock] = useState(false)
  const [loadError, setLoadError] = useState(false)

  const load = () => {
    setLoadError(false)
    client
      .get('/api/reports/dashboard')
      // API cấp KPI + tổng doanh thu (TotalRevenue = tổng payment đã thu); các panel vận hành
      // (khách đến/đi, cảnh báo, biểu đồ 7 ngày) chưa có endpoint nên vẫn là số minh hoạ.
      .then((res) => {
        const api = res.data ?? {}
        setData({ ...MOCK_DASHBOARD, ...api, todayRevenue: api.totalRevenue ?? api.todayRevenue ?? MOCK_DASHBOARD.todayRevenue })
        setUsingMock(false)
      })
      .catch((err) => {
        if (isBackendMissing(err)) { setData(MOCK_DASHBOARD); setUsingMock(true) }
        else setLoadError(true) // lỗi thật: không che bằng mock
      })
  }
  useEffect(load, [])

  if (loadError) {
    return (
      <div>
        <h1 className="font-display text-4xl font-semibold tracking-tight">Tổng quan</h1>
        <div className="mt-8"><ErrorState onRetry={load} /></div>
      </div>
    )
  }

  if (!data) {
    return (
      <div>
        <div className="h-8 w-56 animate-pulse rounded bg-cream-200" />
        <div className="mt-8 h-24 animate-pulse rounded-2xl bg-cream-200" />
        <div className="mt-6 grid gap-5 lg:grid-cols-2">
          <div className="h-64 animate-pulse rounded-2xl bg-cream-200" />
          <div className="h-64 animate-pulse rounded-2xl bg-cream-200" />
        </div>
      </div>
    )
  }

  const stats = [
    { label: 'tổng phòng', value: data.totalRooms },
    { label: 'phòng trống', value: data.availableRooms },
    { label: 'đang có khách', value: data.occupiedRooms },
    { label: 'booking hôm nay', value: data.todayBookings },
  ]
  const maxRevenue = Math.max(...data.revenue7d.map((d) => d.amount))
  const maxIdx = data.revenue7d.findIndex((d) => d.amount === maxRevenue)

  return (
    <div>
      <div className="flex flex-wrap items-end justify-between gap-4">
        <div>
          <p className="font-display text-[15px] italic capitalize text-brand-600">ca làm việc hôm nay</p>
          <h1 className="mt-1 font-display text-4xl font-semibold tracking-tight">Tổng quan</h1>
          <p className="mt-1 text-sm capitalize text-ink-500">{todayLabel}</p>
        </div>
        <div className="flex items-center gap-2.5">
          {usingMock && (
            <span className="mr-1 rounded-full bg-amber-50 px-2.5 py-1 text-[11px] font-bold text-amber-800 ring-1 ring-amber-600/20">
              dữ liệu mẫu, chờ API
            </span>
          )}
          <button onClick={() => navigate('/reservations/new')}
            className={`rounded-full bg-ink-900 px-5 py-2.5 text-[13px] font-bold text-cream-50 ${EASE} hover:bg-ink-700 active:scale-[0.98]`}>
            Tạo đặt phòng
          </button>
          <button onClick={() => navigate('/rooms/map')}
            className={`rounded-full px-5 py-2.5 text-[13px] font-semibold text-ink-700 ring-1 ring-black/10 ${EASE} hover:bg-white active:scale-[0.98]`}>
            Sơ đồ phòng
          </button>
        </div>
      </div>

      {/* KPI: 4 ô thường + ô doanh thu được tô nổi (phân cấp) - double-bezel cho chiều sâu */}
      <div className="mt-7 bezel-shell">
        <div className="bezel-core grid grid-cols-2 overflow-hidden sm:grid-cols-5 sm:divide-x sm:divide-black/[0.06]">
          {stats.map((st) => (
            <div key={st.label} className="px-5 py-4">
              <p className="font-display text-2xl font-semibold tabular-nums leading-none">{st.value}</p>
              <p className="mt-1.5 text-[11px] font-medium text-ink-500">{st.label}</p>
            </div>
          ))}
          <div className="col-span-2 bg-brand-50 px-5 py-4 sm:col-span-1">
            <p className="font-display text-2xl font-semibold tabular-nums leading-none text-brand-700">{formatVnd(data.todayRevenue)}</p>
            <p className="mt-1.5 flex items-center gap-2 text-[11px] font-medium text-ink-500">
              {usingMock ? 'doanh thu hôm nay' : 'tổng doanh thu đã thu'}
              {usingMock && typeof data.revenueDeltaPct === 'number' && (
                <span className={`font-bold ${data.revenueDeltaPct >= 0 ? 'text-emerald-700' : 'text-rose-700'}`}>
                  {data.revenueDeltaPct >= 0 ? '↑' : '↓'} {Math.abs(data.revenueDeltaPct)}% so hôm qua
                </span>
              )}
            </p>
          </div>
        </div>
      </div>

      {/* API vận hành (khách đến/đi, cảnh báo, biểu đồ 7 ngày) chưa có -> báo rõ đây là số minh hoạ */}
      {!usingMock && (
        <p className="mt-4 text-[11px] italic text-ink-500">
          Các mục vận hành bên dưới (khách đến/đi, cảnh báo, biểu đồ 7 ngày) là số liệu minh hoạ — API vận hành chưa hỗ trợ.
        </p>
      )}

      {/* Dải Cần chú ý - thứ khiến trang này "làm việc" */}
      {data.alerts?.length > 0 && (
        <div className="mt-4 rounded-xl bg-amber-50/80 px-5 py-3.5 ring-1 ring-amber-600/15">
          <div className="flex flex-wrap items-start gap-x-8 gap-y-2">
            <p className="pt-0.5 text-[10px] font-bold uppercase tracking-[0.22em] text-amber-800">Cần chú ý</p>
            <div className="flex-1 space-y-1.5">
              {data.alerts.map((a) => (
                <div key={a.id} className="flex flex-wrap items-baseline justify-between gap-x-4 gap-y-1">
                  <p className="flex items-baseline gap-2 text-[13px] font-medium text-ink-700">
                    <span className="inline-block h-1.5 w-1.5 translate-y-[-1px] rounded-full bg-amber-500" />
                    {a.text}
                  </p>
                  <button onClick={() => navigate(a.to)}
                    className="text-[11px] font-bold uppercase tracking-[0.12em] text-brand-700 underline-offset-4 hover:underline">
                    {a.action} →
                  </button>
                </div>
              ))}
            </div>
          </div>
        </div>
      )}

      <div className="mt-5 grid gap-5 lg:grid-cols-2">
        {/* Khách đến hôm nay */}
        <Panel title="Khách đến hôm nay" hint={`${data.arrivals.length} lượt check-in`}>
          <div className="divide-y divide-black/[0.05]">
            {data.arrivals.map((a) => (
              <div key={a.bookingCode} className="flex items-center gap-3.5 py-3">
                <ArchAvatar name={a.guestName} tint="bg-sky-50 text-sky-700 ring-1 ring-sky-600/15" />
                <div className="min-w-0 flex-1">
                  <p className="truncate text-sm font-semibold">{a.guestName}</p>
                  <p className="mt-0.5 text-[11px] uppercase tracking-[0.14em] text-ink-500">
                    Phòng {a.roomNumber} · {a.typeName} · dự kiến {a.eta}
                  </p>
                </div>
                <ActionBadge
                  idle="Chờ check-in" hover="Check-in ngay"
                  tint="bg-sky-50 text-sky-700 ring-sky-600/15 hover:bg-sky-600 hover:text-white hover:ring-sky-600"
                  onClick={() => navigate('/checkin-checkout')}
                />
              </div>
            ))}
            {data.arrivals.length === 0 && <EmptyState text="Không có lượt check-in nào hôm nay" />}
          </div>
        </Panel>

        {/* Khách trả phòng hôm nay */}
        <Panel title="Khách trả phòng hôm nay" hint={`${data.departures.length} lượt check-out`}>
          <div className="divide-y divide-black/[0.05]">
            {data.departures.map((d) => (
              <div key={d.bookingCode} className="flex items-center gap-3.5 py-3">
                <ArchAvatar name={d.guestName} tint="bg-rose-50 text-rose-700 ring-1 ring-rose-600/15" />
                <div className="min-w-0 flex-1">
                  <p className="truncate text-sm font-semibold">{d.guestName}</p>
                  <p className="mt-0.5 text-[11px] uppercase tracking-[0.14em] text-ink-500">
                    Phòng {d.roomNumber} · {d.nights} đêm · tạm tính {formatVnd(d.amountDue)}
                  </p>
                </div>
                <ActionBadge
                  idle="Chờ check-out" hover="Check-out"
                  tint="bg-rose-50 text-rose-700 ring-rose-600/15 hover:bg-rose-600 hover:text-white hover:ring-rose-600"
                  onClick={() => navigate('/checkin-checkout')}
                />
              </div>
            ))}
            {data.departures.length === 0 && <EmptyState text="Không có lượt trả phòng nào hôm nay" />}
          </div>
        </Panel>
      </div>

      {/* Doanh thu 7 ngày: cột hôm nay terracotta, còn lại be nhạt, đỉnh gắn nhãn */}
      <div className="mt-5">
        <Panel title="Doanh thu 7 ngày gần nhất">
          <div className="mt-2 flex h-36 items-end gap-3 sm:gap-5">
            {data.revenue7d.map((d, i) => {
              const last = i === data.revenue7d.length - 1
              const isMax = i === maxIdx
              return (
                <div key={i} className="group flex flex-1 flex-col items-center gap-2">
                  <p className={`text-[10px] tabular-nums ${isMax ? 'font-bold text-ink-700' : `text-ink-500 opacity-0 ${EASE} group-hover:opacity-100`}`}>
                    {Math.round(d.amount / 1000)}k
                  </p>
                  <div
                    className={`w-full max-w-12 rounded-t-md ${EASE} ${last ? 'bg-brand-600' : 'bg-cream-200 group-hover:bg-ink-900/20'}`}
                    style={{ height: `${Math.max((d.amount / maxRevenue) * 100, 6)}%` }}
                  />
                  <p className={`text-[11px] ${last ? 'font-bold text-ink-900' : 'font-medium text-ink-500'}`}>{d.day}</p>
                </div>
              )
            })}
          </div>
        </Panel>
      </div>
    </div>
  )
}
