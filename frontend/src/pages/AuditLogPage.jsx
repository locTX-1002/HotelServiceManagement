import { useEffect, useMemo, useState } from 'react'
import { EASE, inputCls, labelCls } from '../utils/ui'
import PageHero from '../components/PageHero'
import client, { isBackendMissing } from '../api/client'
import EmptyState from '../components/EmptyState'
import ErrorState from '../components/ErrorState'
import { MOCK_AUDIT_LOGS } from '../mock/hotelMock'
import { fmtDateTime } from '../utils/dates'

const PAGE_SIZE = 12

// Nhãn + màu theo loại hành động - khớp spec docs/API_AUDIT_LOG.md
const ACTION_META = {
  Login: { label: 'Đăng nhập', cls: 'bg-stone-100 text-stone-600 ring-stone-500/15' },
  Create: { label: 'Tạo mới', cls: 'bg-emerald-50 text-emerald-700 ring-emerald-600/15' },
  Update: { label: 'Cập nhật', cls: 'bg-sky-50 text-sky-700 ring-sky-600/15' },
  Delete: { label: 'Xóa / tắt', cls: 'bg-rose-50 text-rose-700 ring-rose-600/15' },
  StatusChange: { label: 'Đổi trạng thái', cls: 'bg-amber-50 text-amber-800 ring-amber-600/15' },
}

// Tên đối tượng tiếng Việt cho cột "Đối tượng" - entity lạ thì hiện nguyên tên
const ENTITY_LABEL = {
  User: 'Người dùng', Room: 'Phòng', RoomType: 'Loại phòng', Reservation: 'Đặt phòng',
  Stay: 'Lượt lưu trú', Invoice: 'Hoá đơn', Payment: 'Thanh toán', Promotion: 'Khuyến mãi',
  ServiceItem: 'Dịch vụ', ServiceOrder: 'Đơn dịch vụ', SurchargeItem: 'Phụ thu', Guest: 'Khách',
}

// Nhật ký hoạt động (chỉ Admin): ai làm gì, lúc nào, trên đối tượng nào.
// BE chưa có bảng AuditLogs + endpoint -> chạy dữ liệu mẫu; contract ở docs/API_AUDIT_LOG.md.
export default function AuditLogPage() {
  const [logs, setLogs] = useState(null)
  const [usingMock, setUsingMock] = useState(false)
  const [loadError, setLoadError] = useState(false)

  const [q, setQ] = useState('')
  const [action, setAction] = useState('all')
  const [from, setFrom] = useState('')
  const [to, setTo] = useState('')
  const [page, setPage] = useState(1)

  const load = () => {
    setLoadError(false)
    client
      .get('/api/audit-logs')
      .then((res) => {
        // BE có thể trả mảng thẳng hoặc { items, totalCount } (nếu làm phân trang server) - nhận cả 2
        setLogs(Array.isArray(res.data) ? res.data : res.data?.items ?? [])
        setUsingMock(false)
      })
      .catch((err) => {
        if (isBackendMissing(err)) { setLogs(MOCK_AUDIT_LOGS); setUsingMock(true) }
        else setLoadError(true) // lỗi thật: không che bằng mock
      })
  }
  useEffect(load, [])

  // Lọc client-side: nhật ký đồ án cỡ vài trăm dòng là cùng, chưa cần đẩy filter xuống server
  const filtered = useMemo(() => {
    const kw = q.trim().toLowerCase()
    return (logs ?? []).filter((l) => {
      if (action !== 'all' && l.action !== action) return false
      if (from && String(l.timestamp).slice(0, 10) < from) return false
      if (to && String(l.timestamp).slice(0, 10) > to) return false
      if (kw && !`${l.userName} ${l.description} ${l.entityName}`.toLowerCase().includes(kw)) return false
      return true
    })
  }, [logs, q, action, from, to])

  // Đổi bộ lọc thì quay về trang 1, không thì đứng ở trang không còn tồn tại
  useEffect(() => { setPage(1) }, [q, action, from, to])

  const totalPages = Math.max(1, Math.ceil(filtered.length / PAGE_SIZE))
  const pageRows = filtered.slice((page - 1) * PAGE_SIZE, page * PAGE_SIZE)

  return (
    <div>
      <PageHero
        image="/img/v3.jpg"
        kicker="quản trị · giám sát thao tác"
        title="Nhật ký hoạt động"
        subtitle="Toàn bộ thao tác của nhân viên trong hệ thống: ai làm gì, lúc nào, trên đối tượng nào."
      >
        {usingMock && (
          <span className="rounded-full bg-amber-50 px-2.5 py-1 text-[11px] font-bold text-amber-800 ring-1 ring-amber-600/20">
            Dữ liệu mẫu
          </span>
        )}
      </PageHero>

      {/* Bộ lọc */}
      <div className="mt-5 flex flex-wrap items-end gap-3">
        <div className="min-w-52 flex-1">
          <label htmlFor="al-q" className={labelCls}>Tìm kiếm</label>
          <input
            id="al-q"
            className={inputCls}
            placeholder="Tên nhân viên, nội dung thao tác…"
            value={q}
            onChange={(e) => setQ(e.target.value)}
          />
        </div>
        <div>
          <label htmlFor="al-action" className={labelCls}>Hành động</label>
          <select id="al-action" className={inputCls} value={action} onChange={(e) => setAction(e.target.value)}>
            <option value="all">Tất cả</option>
            {Object.entries(ACTION_META).map(([k, m]) => <option key={k} value={k}>{m.label}</option>)}
          </select>
        </div>
        <div>
          <label htmlFor="al-from" className={labelCls}>Từ ngày</label>
          <input id="al-from" type="date" className={inputCls} value={from} onChange={(e) => setFrom(e.target.value)} />
        </div>
        <div>
          <label htmlFor="al-to" className={labelCls}>Đến ngày</label>
          <input id="al-to" type="date" className={inputCls} value={to} onChange={(e) => setTo(e.target.value)} />
        </div>
      </div>

      {loadError ? (
        <div className="mt-6"><ErrorState onRetry={load} /></div>
      ) : logs === null ? (
        <div className="mt-6 space-y-3">
          {Array.from({ length: 4 }).map((_, i) => (
            <div key={i} className="h-14 animate-pulse rounded-2xl bg-cream-200" />
          ))}
        </div>
      ) : filtered.length === 0 ? (
        <EmptyState text="Không có hoạt động nào khớp bộ lọc." />
      ) : (
        <>
          <div className="mt-6 overflow-x-auto rounded-2xl bg-cream-50 ring-1 ring-black/[0.06]">
            <table className="min-w-full text-left">
              <thead>
                <tr className="border-b border-black/[0.06] text-[10px] font-bold uppercase tracking-[0.18em] text-ink-500">
                  <th className="px-5 py-3.5">Thời gian</th>
                  <th className="px-5 py-3.5">Người thực hiện</th>
                  <th className="px-5 py-3.5">Hành động</th>
                  <th className="px-5 py-3.5">Đối tượng</th>
                  <th className="px-5 py-3.5">Chi tiết</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-black/[0.05]">
                {pageRows.map((l) => {
                  const meta = ACTION_META[l.action] ?? { label: l.action, cls: 'bg-stone-100 text-stone-600 ring-stone-500/15' }
                  return (
                    <tr key={l.id} className={`${EASE} hover:bg-cream-50/60`}>
                      <td className="whitespace-nowrap px-5 py-3.5 text-sm tabular-nums text-ink-700">{fmtDateTime(l.timestamp)}</td>
                      <td className="px-5 py-3.5">
                        <p className="text-sm font-semibold">{l.userName}</p>
                        <p className="text-[11px] text-ink-500">{l.role}</p>
                      </td>
                      <td className="px-5 py-3.5">
                        <span className={`whitespace-nowrap rounded-full px-2.5 py-1 text-[11px] font-bold uppercase tracking-wide ring-1 ${meta.cls}`}>
                          {meta.label}
                        </span>
                      </td>
                      <td className="whitespace-nowrap px-5 py-3.5 text-sm text-ink-700">
                        {ENTITY_LABEL[l.entityName] ?? l.entityName}
                        {l.entityId != null && <span className="text-[11px] text-ink-500"> #{l.entityId}</span>}
                      </td>
                      <td className="px-5 py-3.5 text-sm text-ink-700">{l.description}</td>
                    </tr>
                  )
                })}
              </tbody>
            </table>
          </div>

          {/* Phân trang */}
          <div className="mt-4 flex items-center justify-between">
            <p className="text-[12px] text-ink-500">
              {filtered.length} hoạt động · trang {page}/{totalPages}
            </p>
            <div className="flex gap-2">
              <button
                type="button"
                onClick={() => setPage((p) => Math.max(1, p - 1))}
                disabled={page <= 1}
                className={`rounded-full px-4 py-1.5 text-[12px] font-semibold text-ink-700 ring-1 ring-black/10 ${EASE} hover:bg-white disabled:opacity-40`}
              >
                ‹ Trước
              </button>
              <button
                type="button"
                onClick={() => setPage((p) => Math.min(totalPages, p + 1))}
                disabled={page >= totalPages}
                className={`rounded-full px-4 py-1.5 text-[12px] font-semibold text-ink-700 ring-1 ring-black/10 ${EASE} hover:bg-white disabled:opacity-40`}
              >
                Sau ›
              </button>
            </div>
          </div>
        </>
      )}
    </div>
  )
}
