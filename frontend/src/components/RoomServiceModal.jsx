import { useEffect, useRef, useState } from 'react'
import guestClient, { apiError } from '../api/guestClient'
import { EASE } from '../utils/ui'
import { formatVnd } from '../utils/roomStatus'

const HK_TYPES = [
  { value: 'Cleaning', label: 'Dọn phòng', desc: 'Nhân viên lên dọn phòng' },
  { value: 'ExtraTowels', label: 'Thêm khăn', desc: 'Khăn tắm / khăn mặt' },
  { value: 'ExtraWater', label: 'Thêm nước', desc: 'Nước suối miễn phí' },
  { value: 'Other', label: 'Việc khác', desc: 'Ghi rõ ở ô ghi chú' },
]

// Bang "Dich vu phong" - gom yeu cau don phong + goi do an/dich vu vao 1 cho. Truoc day 2 khoi nay
// bung thang trong the dat phong lam the cao gap doi, keo anh phong ben trai gian theo va trong roi.
export default function RoomServiceModal({ stay, onClose }) {
  const [tab, setTab] = useState('housekeeping')

  // --- Yeu cau don phong ---
  const [hkType, setHkType] = useState('Cleaning')
  const [hkNote, setHkNote] = useState('')
  const [hkSent, setHkSent] = useState('')
  const [hkError, setHkError] = useState('')
  const hkBusyRef = useRef(false)

  // --- Goi do & dich vu ---
  const [catalog, setCatalog] = useState([])
  const [catalogLoading, setCatalogLoading] = useState(false)
  const [catalogError, setCatalogError] = useState('')
  const [qty, setQty] = useState({})
  const [orderSent, setOrderSent] = useState('')
  const [orderError, setOrderError] = useState('')
  const orderBusyRef = useRef(false)

  useEffect(() => {
    const onKey = (e) => e.key === 'Escape' && onClose()
    window.addEventListener('keydown', onKey)
    return () => window.removeEventListener('keydown', onKey)
  }, [onClose])

  // Chi tai danh muc khi khach thuc su mo tab "Do & dich vu" - khong tai san luc mo bang
  useEffect(() => {
    if (tab !== 'service' || catalog.length > 0 || catalogLoading) return
    setCatalogLoading(true)
    guestClient
      .get('/api/guest/service-items')
      .then((res) => setCatalog(res.data ?? []))
      .catch((err) => setCatalogError(apiError(err)))
      .finally(() => setCatalogLoading(false))
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [tab])

  const sendHousekeeping = () => {
    if (hkBusyRef.current) return
    hkBusyRef.current = true
    setHkError('')
    guestClient
      .post('/api/guest/me/housekeeping-requests', { requestType: hkType, note: hkNote.trim() || undefined })
      .then(() => setHkSent(hkType))
      .catch((err) => setHkError(apiError(err)))
      .finally(() => { hkBusyRef.current = false })
  }

  // Tinh trong functional updater de bam +/- lien tuc khong mat lan bam nao
  const bump = (id, d) => setQty((prev) => ({ ...prev, [id]: Math.max(0, (prev[id] ?? 0) + d) }))

  const lines = catalog.map((i) => ({ ...i, quantity: qty[i.id] ?? 0 })).filter((l) => l.quantity > 0)
  const total = lines.reduce((s, l) => s + l.unitPrice * l.quantity, 0)

  const sendOrder = () => {
    if (orderBusyRef.current) return
    if (lines.length === 0) return setOrderError('Chọn ít nhất 1 món.')
    orderBusyRef.current = true
    setOrderError('')
    guestClient
      .post('/api/guest/me/service-orders', {
        items: lines.map((l) => ({ serviceItemId: l.id, quantity: l.quantity })),
      })
      .then((res) => { setOrderSent(formatVnd(res.data.totalAmount)); setQty({}) })
      .catch((err) => setOrderError(apiError(err)))
      .finally(() => { orderBusyRef.current = false })
  }

  const tabCls = (key) =>
    `flex-1 rounded-full px-4 py-2 text-[13px] font-bold ${EASE} ${
      tab === key ? 'bg-ink-900 text-cream-50' : 'text-ink-500 hover:text-ink-900'
    }`

  return (
    <div className="fixed inset-0 z-50 flex items-end justify-center px-4 pb-4 sm:items-center">
      <div onClick={onClose} className="absolute inset-0 bg-ink-900/40" />
      <div className="card-rise relative flex max-h-[88vh] w-full max-w-lg flex-col rounded-2xl bg-cream-50 shadow-lift">
        <div className="flex items-start justify-between gap-3 border-b border-black/[0.06] px-6 py-4">
          <div>
            <p className="font-display text-lg font-semibold">Dịch vụ phòng</p>
            <p className="mt-0.5 text-[12px] text-ink-500">
              Phòng {stay.roomNumber} · {stay.roomTypeName}
            </p>
          </div>
          <button
            type="button"
            onClick={onClose}
            className="flex h-8 w-8 shrink-0 items-center justify-center rounded-full text-sm font-bold text-ink-500 hover:bg-black/[0.05] hover:text-ink-900"
            aria-label="Đóng"
          >
            ✕
          </button>
        </div>

        <div className="px-6 pt-4">
          <div className="flex gap-1 rounded-full bg-white p-1 ring-1 ring-black/10">
            <button type="button" onClick={() => setTab('housekeeping')} className={tabCls('housekeeping')}>
              Dọn phòng
            </button>
            <button type="button" onClick={() => setTab('service')} className={tabCls('service')}>
              Đồ &amp; dịch vụ
            </button>
          </div>
        </div>

        <div className="min-h-0 flex-1 overflow-y-auto px-6 py-5">
          {tab === 'housekeeping' && (
            hkSent ? (
              <div className="rounded-xl bg-emerald-50 p-5 text-center ring-1 ring-emerald-600/15">
                <p className="font-display text-lg font-semibold text-emerald-800">Đã gửi yêu cầu</p>
                <p className="mt-1 text-[13px] text-emerald-700">
                  {HK_TYPES.find((t) => t.value === hkSent)?.label} — lễ tân sẽ xử lý sớm nhất.
                </p>
                <button
                  type="button"
                  onClick={() => { setHkSent(''); setHkNote('') }}
                  className={`mt-4 rounded-full px-4 py-2 text-[12px] font-semibold text-ink-700 ring-1 ring-black/10 ${EASE} hover:bg-white`}
                >
                  Gửi yêu cầu khác
                </button>
              </div>
            ) : (
              <>
                <p className="text-[11px] font-bold uppercase tracking-[0.14em] text-ink-500">Bạn cần gì?</p>
                <div className="mt-3 grid gap-2 sm:grid-cols-2">
                  {HK_TYPES.map((t) => (
                    <button
                      key={t.value}
                      type="button"
                      onClick={() => setHkType(t.value)}
                      className={`rounded-xl p-3.5 text-left ring-1 ${EASE} ${
                        hkType === t.value
                          ? 'bg-brand-50 ring-2 ring-brand-600/50'
                          : 'bg-white ring-black/[0.07] hover:ring-black/20'
                      }`}
                    >
                      <p className="text-[13px] font-bold">{t.label}</p>
                      <p className="mt-0.5 text-[11px] text-ink-500">{t.desc}</p>
                    </button>
                  ))}
                </div>
                <textarea
                  rows={2}
                  maxLength={300}
                  className="mt-3 w-full rounded-xl border border-black/15 bg-white px-3.5 py-2.5 text-[13px] outline-none placeholder:text-ink-500/40 focus:border-brand-500 focus:ring-2 focus:ring-brand-500/20"
                  placeholder="Ghi chú thêm cho lễ tân (không bắt buộc)"
                  value={hkNote}
                  onChange={(e) => setHkNote(e.target.value)}
                />
                {hkError && <p className="mt-2 text-[12px] font-medium text-amber-800">{hkError}</p>}
              </>
            )
          )}

          {tab === 'service' && (
            <>
              {catalogLoading && <p className="text-[13px] text-ink-500">Đang tải danh mục…</p>}
              {catalogError && <p className="text-[13px] font-medium text-amber-800">{catalogError}</p>}
              {orderSent && (
                <div className="mb-3 rounded-xl bg-emerald-50 px-4 py-3 ring-1 ring-emerald-600/15">
                  <p className="text-[13px] font-semibold text-emerald-800">
                    Đã gửi đơn — tổng {orderSent}
                  </p>
                  <p className="mt-0.5 text-[12px] text-emerald-700">Tiền dịch vụ sẽ cộng vào hoá đơn khi trả phòng.</p>
                </div>
              )}
              {!catalogLoading && !catalogError && catalog.length === 0 && (
                <p className="text-[13px] text-ink-500">Hiện chưa có dịch vụ nào.</p>
              )}
              <div className="space-y-2">
                {catalog.map((item) => {
                  const n = qty[item.id] ?? 0
                  return (
                    <div
                      key={item.id}
                      className={`flex items-center gap-3 rounded-xl px-3.5 py-2.5 ring-1 ${EASE} ${
                        n > 0 ? 'bg-brand-50 ring-brand-600/25' : 'bg-white ring-black/[0.06]'
                      }`}
                    >
                      <div className="min-w-0 flex-1">
                        <p className="truncate text-[13px] font-semibold">{item.serviceName}</p>
                        <p className="text-[11px] tabular-nums text-ink-500">{formatVnd(item.unitPrice)}</p>
                      </div>
                      {n > 0 && (
                        <span className="text-[12px] font-bold tabular-nums text-brand-700">
                          {formatVnd(item.unitPrice * n)}
                        </span>
                      )}
                      <div className="flex shrink-0 items-center gap-1 rounded-full bg-white ring-1 ring-black/10">
                        <button
                          type="button"
                          onClick={() => bump(item.id, -1)}
                          disabled={n === 0}
                          className="px-2.5 py-1 text-sm font-bold text-ink-500 hover:text-ink-900 disabled:opacity-30"
                        >
                          −
                        </button>
                        <span className="w-5 text-center text-[13px] font-bold tabular-nums">{n}</span>
                        <button
                          type="button"
                          onClick={() => bump(item.id, 1)}
                          className="px-2.5 py-1 text-sm font-bold text-ink-500 hover:text-ink-900"
                        >
                          +
                        </button>
                      </div>
                    </div>
                  )
                })}
              </div>
              {orderError && <p className="mt-2 text-[12px] font-medium text-amber-800">{orderError}</p>}
            </>
          )}
        </div>

        {/* Chan bang: nut gui doi theo tab dang mo */}
        {tab === 'housekeeping' && !hkSent && (
          <div className="border-t border-black/[0.06] px-6 py-4">
            <button
              type="button"
              onClick={sendHousekeeping}
              className={`w-full rounded-full bg-brand-600 py-3 text-[13px] font-bold text-white ${EASE} hover:bg-brand-700 active:scale-[0.99]`}
            >
              Gửi yêu cầu
            </button>
          </div>
        )}
        {tab === 'service' && catalog.length > 0 && (
          <div className="flex items-center justify-between gap-4 border-t border-black/[0.06] px-6 py-4">
            <div>
              <p className="text-[11px] uppercase tracking-wider text-ink-500">Tạm tính</p>
              <p className="font-display text-lg font-semibold tabular-nums">{formatVnd(total)}</p>
            </div>
            <button
              type="button"
              onClick={sendOrder}
              disabled={lines.length === 0}
              className={`rounded-full bg-brand-600 px-7 py-3 text-[13px] font-bold text-white ${EASE} hover:bg-brand-700 active:scale-[0.99] disabled:opacity-40`}
            >
              Gửi đơn
            </button>
          </div>
        )}
      </div>
    </div>
  )
}
