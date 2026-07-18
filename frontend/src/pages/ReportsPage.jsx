import { useEffect, useMemo, useState } from 'react'
import { EASE, inputCls, labelCls, openDatePicker } from '../utils/ui'
import PageHero from '../components/PageHero'
import client, { isBackendMissing } from '../api/client'
import ErrorState from '../components/ErrorState'
import { mockOccupancySnapshot, mockRevenueSummary } from '../mock/hotelMock'
import { addDays, fmtShort, localToday as today } from '../utils/dates'
import { formatVnd } from '../utils/roomStatus'
import { exportReportToExcel, exportReportToPdf } from '../utils/exportReport'
import BarChart from '../components/BarChart'

// Ngày đầu tháng hiện tại cho chip "Tháng này"
const monthStart = () => `${today().slice(0, 8)}01`

// Số liệu tài chính đọc rõ hơn ở kiểu chữ thẳng có tabular-nums, khác phần tiêu đề/eyebrow (vẫn dùng font-display serif)
const statNumCls = 'font-sans font-semibold tabular-nums leading-none text-ink-900'

// Công suất % tính từ số phòng (đang ở + đã đặt) / tổng - ổn định, không phụ thuộc
// field occupancyRate của backend (chưa rõ 0-1 hay 0-100)
const rateOf = (o) => (o?.totalRooms ? Math.round(((o.occupiedRooms + (o.reservedRooms ?? 0)) / o.totalRooms) * 100) : 0)

function Panel({ title, hint, children }) {
  return (
    <div className="rounded-2xl bg-white p-6 ring-1 ring-black/5 shadow-soft">
      <div className="flex items-baseline justify-between gap-3">
        <h2 className="font-display text-xl font-semibold">{title}</h2>
        {hint && <p className="text-[11px] text-ink-500">{hint}</p>}
      </div>
      <div className="mt-3">{children}</div>
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
  const [customOpen, setCustomOpen] = useState(false) // ô Từ ngày/Đến ngày chỉ hiện khi bấm "Tùy chỉnh", đỡ chiếm chỗ mặc định
  const [exporting, setExporting] = useState('') // 'excel' | 'pdf' | '' - khoá đúng 1 nút đang xuất, nút còn lại vẫn bấm được

  const rangeError = !from || !to
    ? 'Chọn đủ cả từ ngày và đến ngày.' // input date bị xóa trắng
    : from > to
      ? 'Ngày bắt đầu phải trước ngày kết thúc.'
      : ''

  useEffect(() => {
    if (rangeError) return
    setLoadError(false)
    setRevenue(null)
    setOccupancy(null)
    setUsingMock(false)
    // stale = đã đổi dải ngày trong lúc chờ mạng -> response cũ không được đè lên dữ liệu mới
    let stale = false
    // Doanh thu theo dải ngày (backend nhận fromDate/toDate)
    client
      .get('/api/reports/revenue', { params: { fromDate: from, toDate: to } })
      .then((res) => { if (!stale) setRevenue(res.data) })
      .catch((err) => {
        if (stale) return
        if (isBackendMissing(err)) { setRevenue(mockRevenueSummary(from, to)); setUsingMock(true) }
        else setLoadError(err.response?.data?.message ?? true) // lỗi thật: không che bằng mock
      })
    // Công suất là ảnh chụp hiện tại - không theo dải ngày
    client
      .get('/api/reports/occupancy')
      .then((res) => { if (!stale) setOccupancy(res.data) })
      .catch((err) => {
        if (stale) return
        if (isBackendMissing(err)) { setOccupancy(mockOccupancySnapshot()); setUsingMock(true) }
        else setLoadError(err.response?.data?.message ?? true)
      })
    return () => { stale = true }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [from, to, retryTick])

  const loading = !rangeError && !loadError && (revenue === null || occupancy === null)

  // Doanh thu (tổng hợp cả kỳ)
  const roomRevenue = revenue?.roomRevenue ?? 0
  const serviceRevenue = revenue?.serviceRevenue ?? 0
  const totalRevenue = revenue?.totalRevenue ?? roomRevenue + serviceRevenue
  const revBase = Math.max(roomRevenue + serviceRevenue, 1)

  // Công suất hiện tại + theo tầng
  const floors = useMemo(
    () => [...(occupancy?.byFloor ?? [])].sort((a, b) => a.floor - b.floor),
    [occupancy],
  )
  const overallRate = rateOf(occupancy)
  const available = (occupancy?.totalRooms ?? 0) - (occupancy?.occupiedRooms ?? 0) - (occupancy?.reservedRooms ?? 0)

  const quickRanges = [
    { key: '7d', label: '7 ngày', from: addDays(today(), -6), to: today() },
    { key: '30d', label: '30 ngày', from: addDays(today(), -29), to: today() },
    { key: 'month', label: 'Tháng này', from: monthStart(), to: today() },
  ]
  const activePreset = quickRanges.find((q) => q.from === from && q.to === to)

  const canExport = !loading && !rangeError && !loadError
  const doExportExcel = () => {
    setExporting('excel')
    Promise.resolve(exportReportToExcel({ from, to, revenue, occupancy })).finally(() => setExporting(''))
  }
  const doExportPdf = () => {
    setExporting('pdf')
    Promise.resolve(exportReportToPdf({ from, to, revenue, occupancy })).finally(() => setExporting(''))
  }

  return (
    <div>
      <PageHero
        image="/img/v3.jpg"
        kicker="quản lý · thống kê"
        title="Báo cáo"
        subtitle="Doanh thu theo dải ngày và công suất phòng hiện tại."
      />

      {/* Bộ lọc dải ngày: gom nút nhanh + tùy chỉnh vào 1 thanh dạng segmented, ô Từ/Đến ngày chỉ hiện khi cần */}
      <div className="mt-5 flex flex-wrap items-start gap-3">
        <div className="inline-flex flex-wrap items-center gap-0.5 rounded-full bg-white p-1 ring-1 ring-black/10 shadow-soft">
          {quickRanges.map((q) => (
            <button
              key={q.key}
              onClick={() => { setFrom(q.from); setTo(q.to); setCustomOpen(false) }}
              className={`rounded-full px-3.5 py-1.5 text-[12px] font-semibold ${EASE} active:scale-[0.97] ${
                activePreset?.key === q.key && !customOpen ? 'bg-ink-900 text-cream-50' : 'text-ink-700 hover:bg-cream-100'
              }`}
            >
              {q.label}
            </button>
          ))}
          <button
            onClick={() => setCustomOpen((o) => !o)}
            className={`flex items-center gap-1 rounded-full px-3.5 py-1.5 text-[12px] font-semibold ${EASE} active:scale-[0.97] ${
              customOpen || (!activePreset && !customOpen) ? 'bg-ink-900 text-cream-50' : 'text-ink-700 hover:bg-cream-100'
            }`}
          >
            Tùy chỉnh
            <svg width="8" height="8" viewBox="0 0 9 9" fill="none" className={`${EASE} ${customOpen ? 'rotate-180' : ''}`} aria-hidden>
              <path d="M1.5 3L4.5 6L7.5 3" stroke="currentColor" strokeWidth="1.4" strokeLinecap="round" strokeLinejoin="round" />
            </svg>
          </button>
        </div>

        {customOpen && (
          <div className="card-rise flex flex-wrap items-end gap-3 rounded-2xl bg-white p-3.5 ring-1 ring-black/5 shadow-soft">
            <div>
              <label htmlFor="report-from" className={labelCls}>Từ ngày</label>
              <input id="report-from" type="date" className={inputCls} value={from} max={today()} onClick={openDatePicker} onChange={(e) => setFrom(e.target.value)} />
            </div>
            <div>
              <label htmlFor="report-to" className={labelCls}>Đến ngày</label>
              <input id="report-to" type="date" className={inputCls} value={to} max={today()} onClick={openDatePicker} onChange={(e) => setTo(e.target.value)} />
            </div>
          </div>
        )}
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
        <div className="ml-auto flex items-center gap-2">
          <button
            onClick={doExportExcel}
            disabled={!canExport || exporting !== ''}
            className={`rounded-full px-3.5 py-1.5 text-[12px] font-semibold text-ink-700 ring-1 ring-black/10 ${EASE} hover:bg-cream-100 disabled:opacity-40`}
          >
            {exporting === 'excel' ? 'Đang xuất…' : 'Xuất Excel'}
          </button>
          <button
            onClick={doExportPdf}
            disabled={!canExport || exporting !== ''}
            className={`rounded-full px-3.5 py-1.5 text-[12px] font-semibold text-ink-700 ring-1 ring-black/10 ${EASE} hover:bg-cream-100 disabled:opacity-40`}
          >
            {exporting === 'pdf' ? 'Đang xuất…' : 'Xuất PDF'}
          </button>
        </div>
      </div>

      {loadError && (
        <div className="mt-6"><ErrorState message={typeof loadError === 'string' ? loadError : undefined} onRetry={() => setRetryTick((t) => t + 1)} /></div>
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
          {/* KPI doanh thu kỳ: phòng + dịch vụ + đã thu + tổng (tô nổi) - double-bezel */}
          <div className="card-rise mt-5 bezel-shell">
            <div className="bezel-core grid grid-cols-2 overflow-hidden sm:grid-cols-4 sm:divide-x sm:divide-black/[0.06]">
              <div className="px-5 py-4">
                <p className={`${statNumCls} text-3xl`}>{formatVnd(roomRevenue)}</p>
                <p className="mt-1.5 text-[11px] font-medium text-ink-500">tiền phòng</p>
              </div>
              <div className="px-5 py-4">
                <p className={`${statNumCls} text-3xl`}>{formatVnd(serviceRevenue)}</p>
                <p className="mt-1.5 text-[11px] font-medium text-ink-500">dịch vụ</p>
              </div>
              <div className="px-5 py-4">
                <p className={`${statNumCls} text-3xl`}>{formatVnd(revenue?.paymentRevenue ?? totalRevenue)}</p>
                <p className="mt-1.5 text-[11px] font-medium text-ink-500">đã thu</p>
              </div>
              <div className="col-span-2 bg-brand-50 px-5 py-4 sm:col-span-1">
                <p className={`${statNumCls} text-3xl text-brand-700`}>{formatVnd(totalRevenue)}</p>
                <p className="mt-1.5 text-[11px] font-medium text-ink-500">tổng doanh thu kỳ này</p>
              </div>
            </div>
          </div>

          <div className="mt-5 grid gap-5 lg:grid-cols-2">
            {/* Cơ cấu doanh thu: phòng vs dịch vụ */}
            <Panel title="Cơ cấu doanh thu" hint={`${fmtShort(from)} → ${fmtShort(to)}`}>
              <div className="flex h-3.5 w-full overflow-hidden rounded-full bg-cream-200">
                <div className={`bg-brand-600 ${EASE}`} style={{ width: `${(roomRevenue / revBase) * 100}%` }} />
                <div className={`bg-brand-500/40 ${EASE}`} style={{ width: `${(serviceRevenue / revBase) * 100}%` }} />
              </div>
              <div className="mt-4 space-y-2.5">
                <div className="flex items-center justify-between text-sm">
                  <span className="flex items-center gap-2 text-ink-700"><span className="h-2.5 w-2.5 rounded-sm bg-brand-600" /> Tiền phòng</span>
                  <span className="font-semibold tabular-nums">{formatVnd(roomRevenue)} <span className="text-[11px] font-normal text-ink-500">· {Math.round((roomRevenue / revBase) * 100)}%</span></span>
                </div>
                <div className="flex items-center justify-between text-sm">
                  <span className="flex items-center gap-2 text-ink-700"><span className="h-2.5 w-2.5 rounded-sm bg-brand-500/40" /> Dịch vụ</span>
                  <span className="font-semibold tabular-nums">{formatVnd(serviceRevenue)} <span className="text-[11px] font-normal text-ink-500">· {Math.round((serviceRevenue / revBase) * 100)}%</span></span>
                </div>
                <div className="flex items-center justify-between border-t border-black/[0.06] pt-2.5 text-sm">
                  <span className="font-semibold text-ink-900">Tổng</span>
                  <span className={`${statNumCls} text-lg text-brand-700`}>{formatVnd(totalRevenue)}</span>
                </div>
              </div>
            </Panel>

            {/* Công suất hiện tại: vòng số lớn + phân rã trạng thái */}
            <Panel title="Công suất phòng" hint="ảnh chụp hiện tại">
              <div className="flex items-center gap-6">
                <div className="shrink-0 text-center">
                  <p className={`${statNumCls} text-5xl text-brand-600`}>{overallRate}%</p>
                  <p className="mt-1 text-[11px] uppercase tracking-[0.14em] text-ink-500">lấp đầy</p>
                </div>
                <div className="flex-1 space-y-2 text-sm">
                  <div className="flex items-center justify-between"><span className="flex items-center gap-2 text-ink-700"><span className="h-2.5 w-2.5 rounded-full bg-rose-500" /> Đang ở</span><span className="font-semibold tabular-nums">{occupancy?.occupiedRooms ?? 0}</span></div>
                  <div className="flex items-center justify-between"><span className="flex items-center gap-2 text-ink-700"><span className="h-2.5 w-2.5 rounded-full bg-sky-500" /> Đã đặt</span><span className="font-semibold tabular-nums">{occupancy?.reservedRooms ?? 0}</span></div>
                  <div className="flex items-center justify-between"><span className="flex items-center gap-2 text-ink-700"><span className="h-2.5 w-2.5 rounded-full bg-emerald-500" /> Còn trống</span><span className="font-semibold tabular-nums">{Math.max(available, 0)}</span></div>
                  <div className="flex items-center justify-between border-t border-black/[0.06] pt-2"><span className="font-semibold text-ink-900">Tổng phòng</span><span className="font-semibold tabular-nums">{occupancy?.totalRooms ?? 0}</span></div>
                </div>
              </div>
            </Panel>
          </div>

          {/* Doanh thu theo ngày - biểu đồ cột, trục đầy đủ mọi ngày trong kỳ kể cả ngày 0 đồng.
              Không còn panel "Công suất theo tầng" dạng biểu đồ cạnh đây nữa - trùng lặp hoàn toàn
              với bảng chi tiết theo tầng ngay bên dưới (cùng % lấp đầy, bảng còn có số phòng chính
              xác), gọn trang cho quản lý đỡ phải đọc 2 chỗ 1 số liệu. */}
          <div className="mt-5">
            <Panel title="Doanh thu theo ngày" hint={`${fmtShort(from)} → ${fmtShort(to)}`}>
              <BarChart
                data={(revenue?.byDay ?? []).map((d) => ({ label: fmtShort(String(d.date).slice(0, 10)), value: d.totalRevenue }))}
                formatValue={formatVnd}
                orientation="vertical"
                emptyText="Chưa có doanh thu trong kỳ này"
              />
            </Panel>
          </div>

          {/* Công suất theo tầng */}
          <div className="card-rise mt-5 bezel-shell">
            <div className="bezel-core overflow-hidden">
              <div className="overflow-x-auto">
                <table className="w-full min-w-[560px] text-left">
                  <thead>
                    <tr className="border-b border-black/[0.06] text-[10px] font-bold uppercase tracking-[0.18em] text-ink-500">
                      <th className="px-5 py-3.5">Tầng</th>
                      <th className="px-5 py-3.5 text-right">Tổng phòng</th>
                      <th className="px-5 py-3.5 text-right">Đang ở</th>
                      <th className="px-5 py-3.5 text-right">Đã đặt</th>
                      <th className="px-5 py-3.5">Công suất</th>
                    </tr>
                  </thead>
                  <tbody className="divide-y divide-black/[0.05]">
                    {floors.map((f) => {
                      const r = rateOf(f)
                      return (
                        <tr key={f.floor} className={`${EASE} hover:bg-cream-50/60`}>
                          <td className="px-5 py-3 text-sm font-semibold">Tầng {f.floor}</td>
                          <td className="px-5 py-3 text-right text-sm tabular-nums text-ink-700">{f.totalRooms}</td>
                          <td className="px-5 py-3 text-right text-sm tabular-nums text-ink-700">{f.occupiedRooms}</td>
                          <td className="px-5 py-3 text-right text-sm tabular-nums text-ink-700">{f.reservedRooms ?? 0}</td>
                          <td className="px-5 py-3">
                            <div className="flex items-center gap-2.5">
                              <div className="h-2 flex-1 overflow-hidden rounded-full bg-cream-200">
                                <div className={`h-full rounded-full bg-brand-500 ${EASE}`} style={{ width: `${r}%` }} />
                              </div>
                              <span className="w-9 text-right text-[12px] font-semibold tabular-nums text-ink-700">{r}%</span>
                            </div>
                          </td>
                        </tr>
                      )
                    })}
                    {floors.length === 0 && (
                      <tr><td colSpan={5} className="px-5 py-10 text-center text-[13px] italic text-ink-500">Chưa có dữ liệu công suất theo tầng</td></tr>
                    )}
                  </tbody>
                  <tfoot>
                    <tr className="border-t border-black/[0.08] bg-cream-50/60">
                      <td className="px-5 py-3.5 text-sm font-bold">Toàn khách sạn</td>
                      <td className="px-5 py-3.5 text-right text-sm font-bold tabular-nums">{occupancy?.totalRooms ?? 0}</td>
                      <td className="px-5 py-3.5 text-right text-sm font-bold tabular-nums">{occupancy?.occupiedRooms ?? 0}</td>
                      <td className="px-5 py-3.5 text-right text-sm font-bold tabular-nums">{occupancy?.reservedRooms ?? 0}</td>
                      <td className="px-5 py-3.5 text-sm font-bold tabular-nums text-brand-700">{overallRate}%</td>
                    </tr>
                  </tfoot>
                </table>
              </div>
            </div>
          </div>
        </>
      )}
    </div>
  )
}
