import { useEffect, useMemo, useState } from 'react'
import client, { isBackendMissing } from '../api/client'
import ErrorState from '../components/ErrorState'
import { mockOccupancyRange, mockRevenueRange } from '../mock/hotelMock'
import { addDays, fmtShort, localToday as today } from '../utils/dates'
import { formatVnd } from '../utils/roomStatus'

const EASE = 'transition-all duration-300 ease-[cubic-bezier(0.32,0.72,0,1)]'
const inputCls =
  'w-full rounded-xl bg-white px-3.5 py-2.5 text-sm ring-1 ring-black/10 outline-none focus:ring-2 focus:ring-brand-500/40'
const labelCls = 'mb-1.5 block text-[12px] font-semibold text-ink-700'

const MAX_DAYS = 31

// Dải ngày [from..to] dạng chuỗi local - có chốt trên để vòng lặp không chạy vô hạn
const dayRange = (from, to) => {
  const out = []
  let d = from
  while (d <= to && out.length <= MAX_DAYS) {
    out.push(d)
    d = addDays(d, 1)
  }
  return out
}

// Ngày đầu tháng hiện tại cho chip "Tháng này"
const monthStart = () => `${today().slice(0, 8)}01`

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

export default function ReportsPage() {
  const [from, setFrom] = useState(addDays(today(), -6))
  const [to, setTo] = useState(today())
  const [revenue, setRevenue] = useState(null)
  const [occupancy, setOccupancy] = useState(null)
  const [usingMock, setUsingMock] = useState(false)
  const [loadError, setLoadError] = useState(false)
  const [retryTick, setRetryTick] = useState(0) // Thử lại giữ nguyên dải ngày đang chọn

  const days = useMemo(() => dayRange(from, to), [from, to])
  const rangeError = !from || !to
    ? 'Chọn đủ cả từ ngày và đến ngày.' // input date bị xóa trắng -> không được render Invalid Date
    : from > to
      ? 'Ngày bắt đầu phải trước ngày kết thúc.'
      : days.length > MAX_DAYS
        ? `Chọn tối đa ${MAX_DAYS} ngày để biểu đồ còn đọc được.`
        : ''

  useEffect(() => {
    if (rangeError) return
    setLoadError(false)
    setRevenue(null)
    setOccupancy(null)
    setUsingMock(false)
    // stale = đã đổi dải ngày trong lúc chờ mạng -> response cũ không được đè lên dữ liệu mới
    let stale = false
    const params = { from, to }
    // Gọi API thật; nếu máy chủ chưa sẵn sàng thì dùng số liệu mẫu
    client
      .get('/api/reports/revenue', { params })
      .then((res) => { if (!stale) setRevenue(res.data) })
      .catch((err) => {
        if (stale) return
        if (isBackendMissing(err)) { setRevenue(mockRevenueRange(dayRange(from, to))); setUsingMock(true) }
        else setLoadError(true) // lỗi thật: không che bằng mock
      })
    client
      .get('/api/reports/occupancy', { params })
      .then((res) => { if (!stale) setOccupancy(res.data) })
      .catch((err) => {
        if (stale) return
        if (isBackendMissing(err)) { setOccupancy(mockOccupancyRange(dayRange(from, to))); setUsingMock(true) }
        else setLoadError(true)
      })
    return () => { stale = true }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [from, to, retryTick])

  // Gộp 2 nguồn theo ngày cho bảng chi tiết - server trả thiếu ngày nào thì ô đó để trống.
  // slice(0,10) phòng backend trả date kèm giờ ('2026-07-06T00:00:00')
  const dkey = (d) => String(d).slice(0, 10)
  const rows = useMemo(
    () =>
      days.map((date) => ({
        date,
        rev: (revenue ?? []).find((r) => dkey(r.date) === date),
        occ: (occupancy ?? []).find((o) => dkey(o.date) === date),
      })),
    [days, revenue, occupancy],
  )

  const loading = !rangeError && !loadError && (revenue === null || occupancy === null)

  const totalRoom = rows.reduce((s, r) => s + (r.rev?.roomRevenue ?? 0), 0)
  const totalService = rows.reduce((s, r) => s + (r.rev?.serviceRevenue ?? 0), 0)
  const total = totalRoom + totalService
  const totalOccupied = rows.reduce((s, r) => s + (r.occ?.occupiedRooms ?? 0), 0)
  const totalCapacity = rows.reduce((s, r) => s + (r.occ?.totalRooms ?? 0), 0)
  const avgOccupancy = totalCapacity ? Math.round((totalOccupied / totalCapacity) * 100) : 0
  const maxDayTotal = Math.max(...rows.map((r) => (r.rev?.roomRevenue ?? 0) + (r.rev?.serviceRevenue ?? 0)), 1)

  const pct = (r) => (r.occ?.totalRooms ? Math.round((r.occ.occupiedRooms / r.occ.totalRooms) * 100) : 0)

  // Nhãn ngày dưới biểu đồ: dải dài chỉ ghi thưa cho khỏi dính chữ
  const labelStep = Math.ceil(days.length / 8)
  const showLabel = (i) => i === 0 || i === days.length - 1 || i % labelStep === 0

  const quickRanges = [
    { label: '7 ngày', from: addDays(today(), -6), to: today() },
    { label: '30 ngày', from: addDays(today(), -29), to: today() },
    { label: 'Tháng này', from: monthStart(), to: today() },
  ]

  return (
    <div>
      {/* Header + chọn dải ngày */}
      <div className="flex flex-wrap items-end justify-between gap-4">
        <div>
          <p className="font-display text-[15px] italic capitalize text-brand-600">quản lý · thống kê</p>
          <h1 className="mt-1 font-display text-4xl font-semibold tracking-tight">Báo cáo</h1>
          <p className="mt-1 text-sm text-ink-500">Doanh thu và công suất phòng theo dải ngày tùy chọn.</p>
        </div>
        <div className="flex flex-wrap items-end gap-3 rounded-2xl bg-white p-4 ring-1 ring-black/5 shadow-soft">
          <div>
            <label htmlFor="report-from" className={labelCls}>Từ ngày</label>
            <input id="report-from" type="date" className={inputCls} value={from} max={today()} onChange={(e) => setFrom(e.target.value)} />
          </div>
          <div>
            <label htmlFor="report-to" className={labelCls}>Đến ngày</label>
            <input id="report-to" type="date" className={inputCls} value={to} max={today()} onChange={(e) => setTo(e.target.value)} />
          </div>
          <div className="flex gap-2 pb-0.5">
            {quickRanges.map((q) => {
              const active = from === q.from && to === q.to
              return (
                <button
                  key={q.label}
                  onClick={() => { setFrom(q.from); setTo(q.to) }}
                  className={`rounded-full px-3 py-1.5 text-[12px] font-semibold ${EASE} active:scale-[0.97] ${
                    active ? 'bg-ink-900 text-cream-50' : 'text-ink-700 ring-1 ring-black/10 hover:bg-cream-100'
                  }`}
                >
                  {q.label}
                </button>
              )
            })}
          </div>
        </div>
      </div>

      <div className="mt-4 flex flex-wrap items-center gap-2.5">
        {usingMock && (
          <span className="rounded-full bg-amber-50 px-2.5 py-1 text-[11px] font-bold text-amber-800 ring-1 ring-amber-600/20">
            Dữ liệu mẫu
          </span>
        )}
        {rangeError && (
          <span className="rounded-full bg-amber-50 px-2.5 py-1 text-[11px] font-bold text-amber-800 ring-1 ring-amber-600/20">
            {rangeError}
          </span>
        )}
      </div>

      {loadError && (
        <div className="mt-6"><ErrorState onRetry={() => setRetryTick((t) => t + 1)} /></div>
      )}

      {loading && (
        <div className="mt-6">
          <div className="h-24 animate-pulse rounded-2xl bg-cream-200" />
          <div className="mt-5 grid gap-5 lg:grid-cols-2">
            <div className="h-64 animate-pulse rounded-2xl bg-cream-200" />
            <div className="h-64 animate-pulse rounded-2xl bg-cream-200" />
          </div>
        </div>
      )}

      {!rangeError && !loadError && !loading && (
        <>
          {/* KPI: 3 ô thường + ô tổng doanh thu tô nổi như trang Tổng quan */}
          <div className="card-rise mt-5 grid grid-cols-2 overflow-hidden rounded-2xl bg-white ring-1 ring-black/5 shadow-soft sm:grid-cols-4 sm:divide-x sm:divide-black/5">
            <div className="px-5 py-4">
              <p className="font-display text-2xl font-semibold tabular-nums leading-none">{days.length}</p>
              <p className="mt-1.5 text-[11px] font-medium text-ink-500">ngày trong kỳ</p>
            </div>
            <div className="px-5 py-4">
              <p className="font-display text-2xl font-semibold tabular-nums leading-none">{totalOccupied}</p>
              <p className="mt-1.5 text-[11px] font-medium text-ink-500">đêm phòng có khách</p>
            </div>
            <div className="px-5 py-4">
              <p className="font-display text-2xl font-semibold tabular-nums leading-none">{avgOccupancy}%</p>
              <p className="mt-1.5 text-[11px] font-medium text-ink-500">công suất trung bình</p>
            </div>
            <div className="col-span-2 bg-brand-50 px-5 py-4 sm:col-span-1">
              <p className="font-display text-2xl font-semibold tabular-nums leading-none text-brand-700">{formatVnd(total)}</p>
              <p className="mt-1.5 text-[11px] font-medium text-ink-500">tổng doanh thu kỳ này</p>
            </div>
          </div>

          <div className="mt-5 grid gap-5 lg:grid-cols-2">
            {/* Doanh thu theo ngày: cột chồng phòng (terracotta) + dịch vụ (nhạt) */}
            <Panel
              title="Doanh thu theo ngày"
              hint={`phòng ${formatVnd(totalRoom)} · dịch vụ ${formatVnd(totalService)}`}
            >
              <div className="mt-2 flex h-40 items-end gap-1.5 sm:gap-2">
                {rows.map((r, i) => {
                  const room = r.rev?.roomRevenue ?? 0
                  const service = r.rev?.serviceRevenue ?? 0
                  return (
                    <div key={r.date} className="group flex h-full flex-1 flex-col items-center justify-end gap-1.5">
                      <p className={`text-[10px] tabular-nums text-ink-500 opacity-0 ${EASE} group-hover:opacity-100`}>
                        {Math.round((room + service) / 1000)}k
                      </p>
                      <div className="flex w-full max-w-10 flex-col overflow-hidden rounded-t-md">
                        <div className={`w-full bg-brand-500/35 ${EASE}`} style={{ height: `${(service / maxDayTotal) * 128}px` }} />
                        <div className={`w-full bg-brand-600 ${EASE} group-hover:bg-brand-700`} style={{ height: `${Math.max((room / maxDayTotal) * 128, 2)}px` }} />
                      </div>
                      <p className="h-3.5 text-[10px] font-medium text-ink-500">{showLabel(i) ? fmtShort(r.date) : ''}</p>
                    </div>
                  )
                })}
              </div>
              <div className="mt-3 flex items-center gap-4 text-[11px] text-ink-500">
                <span className="flex items-center gap-1.5"><span className="h-2 w-2 rounded-sm bg-brand-600" /> Tiền phòng</span>
                <span className="flex items-center gap-1.5"><span className="h-2 w-2 rounded-sm bg-brand-500/35" /> Dịch vụ</span>
              </div>
            </Panel>

            {/* Công suất phòng: cột % + vạch trung bình */}
            <Panel title="Công suất phòng" hint={`trung bình ${avgOccupancy}%`}>
              <div className="relative mt-2 h-40">
                {/* Vạch công suất trung bình */}
                <div className="absolute inset-x-0 z-10 border-t border-dashed border-ink-500/40" style={{ bottom: `${(avgOccupancy / 100) * 128 + 20}px` }}>
                  <span className="absolute -top-2.5 right-0 bg-white px-1 text-[9px] font-bold uppercase tracking-wider text-ink-500">TB {avgOccupancy}%</span>
                </div>
                <div className="flex h-full items-end gap-1.5 sm:gap-2">
                  {rows.map((r, i) => (
                    <div key={r.date} className="group flex h-full flex-1 flex-col items-center justify-end gap-1.5">
                      <p className={`text-[10px] tabular-nums text-ink-500 opacity-0 ${EASE} group-hover:opacity-100`}>{pct(r)}%</p>
                      <div
                        className={`w-full max-w-10 rounded-t-md ${EASE} ${pct(r) >= avgOccupancy ? 'bg-sky-400/70 group-hover:bg-sky-500' : 'bg-cream-200 group-hover:bg-ink-900/20'}`}
                        style={{ height: `${Math.max((pct(r) / 100) * 128, 2)}px` }}
                      />
                      <p className="h-3.5 text-[10px] font-medium text-ink-500">{showLabel(i) ? fmtShort(r.date) : ''}</p>
                    </div>
                  ))}
                </div>
              </div>
            </Panel>
          </div>

          {/* Bảng chi tiết theo ngày */}
          <div className="card-rise mt-5 overflow-hidden rounded-2xl bg-white ring-1 ring-black/5 shadow-soft">
            <div className="overflow-x-auto">
            <table className="w-full min-w-[560px] text-left">
              <thead>
                <tr className="border-b border-black/[0.06] text-[10px] font-bold uppercase tracking-[0.18em] text-ink-500">
                  <th className="px-5 py-3.5">Ngày</th>
                  <th className="px-5 py-3.5 text-right">Tiền phòng</th>
                  <th className="px-5 py-3.5 text-right">Dịch vụ</th>
                  <th className="px-5 py-3.5 text-right">Tổng</th>
                  <th className="px-5 py-3.5 text-right">Công suất</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-black/[0.05]">
                {rows.map((r) => (
                  <tr key={r.date} className={`${EASE} hover:bg-cream-50/60`}>
                    <td className="px-5 py-3 text-sm font-semibold">{fmtShort(r.date)}</td>
                    <td className="px-5 py-3 text-right text-sm tabular-nums text-ink-700">
                      {r.rev ? formatVnd(r.rev.roomRevenue) : '—'}
                    </td>
                    <td className="px-5 py-3 text-right text-sm tabular-nums text-ink-700">
                      {r.rev ? formatVnd(r.rev.serviceRevenue) : '—'}
                    </td>
                    <td className="px-5 py-3 text-right text-sm font-semibold tabular-nums">
                      {r.rev ? formatVnd((r.rev.roomRevenue ?? 0) + (r.rev.serviceRevenue ?? 0)) : '—'}
                    </td>
                    <td className="px-5 py-3 text-right text-sm tabular-nums text-ink-700">
                      {r.occ ? `${pct(r)}% · ${r.occ.occupiedRooms}/${r.occ.totalRooms}` : '—'}
                    </td>
                  </tr>
                ))}
              </tbody>
              <tfoot>
                <tr className="border-t border-black/[0.08] bg-cream-50/60">
                  <td className="px-5 py-3.5 text-sm font-bold">Tổng kỳ</td>
                  <td className="px-5 py-3.5 text-right text-sm font-bold tabular-nums">{formatVnd(totalRoom)}</td>
                  <td className="px-5 py-3.5 text-right text-sm font-bold tabular-nums">{formatVnd(totalService)}</td>
                  <td className="px-5 py-3.5 text-right font-display text-base font-semibold tabular-nums text-brand-700">{formatVnd(total)}</td>
                  <td className="px-5 py-3.5 text-right text-sm font-bold tabular-nums">TB {avgOccupancy}%</td>
                </tr>
              </tfoot>
            </table>
            </div>
          </div>
        </>
      )}
    </div>
  )
}
