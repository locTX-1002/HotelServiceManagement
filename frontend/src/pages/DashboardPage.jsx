import { useEffect, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import client from '../api/client'
import { formatVnd } from '../utils/roomStatus'
import { MOCK_DASHBOARD } from '../mock/hotelMock'

const EASE = 'transition-all duration-300 ease-[cubic-bezier(0.32,0.72,0,1)]'
const eyebrow = 'text-[10px] font-bold uppercase tracking-[0.3em] text-brand-600'

const todayLabel = new Intl.DateTimeFormat('vi-VN', { weekday: 'long', day: '2-digit', month: '2-digit' }).format(new Date())

function Panel({ title, hint, children }) {
  return (
    <div className="rounded-2xl bg-white p-6 ring-1 ring-black/5 shadow-soft">
      <div className="flex items-baseline justify-between">
        <h2 className="font-display text-lg font-semibold">{title}</h2>
        {hint && <p className="text-[11px] text-ink-500">{hint}</p>}
      </div>
      <div className="mt-4">{children}</div>
    </div>
  )
}

export default function DashboardPage() {
  const navigate = useNavigate()
  const [data, setData] = useState(null)
  const [usingMock, setUsingMock] = useState(false)

  useEffect(() => {
    client
      .get('/api/reports/dashboard')
      .then((res) => { setData({ ...MOCK_DASHBOARD, ...res.data }); setUsingMock(false) })
      .catch(() => { setData(MOCK_DASHBOARD); setUsingMock(true) })
  }, [])

  if (!data) {
    return (
      <div className="mx-auto max-w-6xl">
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
    { label: 'doanh thu hôm nay', value: formatVnd(data.todayRevenue), wide: true },
  ]
  const maxRevenue = Math.max(...data.revenue7d.map((d) => d.amount))

  return (
    <div className="mx-auto max-w-6xl">
      <div className="flex flex-wrap items-end justify-between gap-4">
        <div>
          <p className="font-display text-[15px] italic text-brand-600">ca làm việc hôm nay</p>
          <h1 className="mt-1 font-display text-4xl font-semibold tracking-tight">Tổng quan</h1>
          <p className="mt-1 text-sm capitalize text-ink-500">{todayLabel}</p>
        </div>
        <div className="flex items-center gap-2.5">
          {usingMock && (
            <p className="mr-2 flex items-center gap-1.5 text-[11px] font-medium text-ink-500">
              <span className="h-1.5 w-1.5 animate-pulse rounded-full bg-amber-500" /> dữ liệu mẫu, chờ API
            </p>
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

      {/* Dải KPI - 1 khối chia hairline, đồng bộ với sơ đồ phòng */}
      <div className="mt-7 grid grid-cols-2 overflow-hidden rounded-2xl bg-white ring-1 ring-black/5 shadow-soft sm:grid-cols-5 sm:divide-x sm:divide-black/5">
        {stats.map((st) => (
          <div key={st.label} className={`px-5 py-4 ${st.wide ? 'col-span-2 sm:col-span-1' : ''}`}>
            <p className="font-display text-2xl font-semibold tabular-nums leading-none">{st.value}</p>
            <p className="mt-1.5 text-[11px] font-medium text-ink-500">{st.label}</p>
          </div>
        ))}
      </div>

      <div className="mt-6 grid gap-5 lg:grid-cols-2">
        {/* Khách đến hôm nay */}
        <Panel title="Khách đến hôm nay" hint={`${data.arrivals.length} lượt check-in`}>
          <div className="divide-y divide-black/[0.05]">
            {data.arrivals.map((a) => (
              <div key={a.bookingCode} className="flex items-center justify-between gap-3 py-3">
                <div className="min-w-0">
                  <p className="truncate text-sm font-semibold">{a.guestName}</p>
                  <p className="mt-0.5 text-[11px] uppercase tracking-[0.14em] text-ink-500">
                    Phòng {a.roomNumber} · {a.typeName} · dự kiến {a.eta}
                  </p>
                </div>
                <span className="shrink-0 rounded-full bg-sky-50 px-2.5 py-1 text-[11px] font-semibold text-sky-700 ring-1 ring-sky-600/15">
                  Chờ check-in
                </span>
              </div>
            ))}
            {data.arrivals.length === 0 && <p className="py-6 text-center text-sm italic text-ink-500">Không có lượt đến nào hôm nay.</p>}
          </div>
        </Panel>

        {/* Khách đi hôm nay */}
        <Panel title="Khách trả phòng hôm nay" hint={`${data.departures.length} lượt check-out`}>
          <div className="divide-y divide-black/[0.05]">
            {data.departures.map((d) => (
              <div key={d.bookingCode} className="flex items-center justify-between gap-3 py-3">
                <div className="min-w-0">
                  <p className="truncate text-sm font-semibold">{d.guestName}</p>
                  <p className="mt-0.5 text-[11px] uppercase tracking-[0.14em] text-ink-500">
                    Phòng {d.roomNumber} · {d.nights} đêm · tạm tính {formatVnd(d.amountDue)}
                  </p>
                </div>
                <span className="shrink-0 rounded-full bg-rose-50 px-2.5 py-1 text-[11px] font-semibold text-rose-700 ring-1 ring-rose-600/15">
                  Chờ check-out
                </span>
              </div>
            ))}
            {data.departures.length === 0 && <p className="py-6 text-center text-sm italic text-ink-500">Không có lượt trả phòng nào hôm nay.</p>}
          </div>
        </Panel>
      </div>

      {/* Doanh thu 7 ngày - bar chart CSS thuần */}
      <div className="mt-5">
        <Panel title="Doanh thu 7 ngày gần nhất" hint={`cao nhất ${formatVnd(maxRevenue)}`}>
          <div className="flex h-36 items-end gap-3 sm:gap-5">
            {data.revenue7d.map((d, i) => {
              const last = i === data.revenue7d.length - 1
              return (
                <div key={i} className="group flex flex-1 flex-col items-center gap-2">
                  <p className={`text-[10px] tabular-nums text-ink-500 opacity-0 ${EASE} group-hover:opacity-100`}>
                    {Math.round(d.amount / 1000)}k
                  </p>
                  <div
                    className={`w-full max-w-12 rounded-t-md ${EASE} ${last ? 'bg-brand-600' : 'bg-ink-900/15 group-hover:bg-ink-900/30'}`}
                    style={{ height: `${Math.max((d.amount / maxRevenue) * 100, 6)}%` }}
                  />
                  <p className={`text-[11px] font-medium ${last ? 'font-bold text-ink-900' : 'text-ink-500'}`}>{d.day}</p>
                </div>
              )
            })}
          </div>
        </Panel>
      </div>
    </div>
  )
}
