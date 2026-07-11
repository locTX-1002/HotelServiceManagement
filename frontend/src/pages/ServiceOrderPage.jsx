import { useEffect, useMemo, useState } from 'react'
import client, { isBackendMissing } from '../api/client'
import ConfirmDialog from '../components/ConfirmDialog'
import ErrorState from '../components/ErrorState'
import SlideOver from '../components/SlideOver'
import { useToast } from '../components/toastContext'
import { MOCK_ACTIVE_STAYS, MOCK_SERVICE_ITEMS, MOCK_SERVICE_ORDERS } from '../mock/hotelMock'
import { denormalizeServiceOrderStatus, normalizeServiceOrder } from '../utils/apiShape'
import { fmtDateTime } from '../utils/dates'
import { formatVnd } from '../utils/roomStatus'

const EASE = 'transition-all duration-300 ease-[cubic-bezier(0.32,0.72,0,1)]'

const ORDER_STATUS = {
  Pending: { label: 'Chờ xử lý', badge: 'bg-amber-50 text-amber-800 ring-1 ring-amber-600/15' },
  Processing: { label: 'Đang xử lý', badge: 'bg-sky-50 text-sky-700 ring-1 ring-sky-600/15' },
  Completed: { label: 'Hoàn thành', badge: 'bg-emerald-50 text-emerald-700 ring-1 ring-emerald-600/15' },
  Cancelled: { label: 'Đã hủy', badge: 'bg-rose-50 text-rose-700 ring-1 ring-rose-600/15' },
}

const apiError = (err) =>
  isBackendMissing(err)
    ? 'Không kết nối được máy chủ. Vui lòng thử lại sau.'
    : err.response?.data?.message ?? 'Máy chủ báo lỗi. Thử lại sau ít phút.'

const EmptyState = ({ text }) => (
  <div className="mt-6 flex flex-col items-center rounded-2xl border border-dashed border-black/10 bg-white/60 px-6 py-14">
    <span className="h-12 w-9 rounded-t-full rounded-b-md border-2 border-dashed border-brand-600/30" />
    <p className="mt-4 font-display text-lg italic text-ink-700">{text}</p>
  </div>
)

export default function ServiceOrderPage() {
  const toast = useToast()

  const [stays, setStays] = useState(null)
  const [staysUsingMock, setStaysUsingMock] = useState(false)
  const [staysError, setStaysError] = useState(false)

  const [items, setItems] = useState([])
  const [orders, setOrders] = useState(null)
  const [ordersUsingMock, setOrdersUsingMock] = useState(false)

  const [openStay, setOpenStay] = useState(null)
  const [cart, setCart] = useState([])
  const [pickItemId, setPickItemId] = useState('')
  const [pickQty, setPickQty] = useState(1)
  const [submitting, setSubmitting] = useState(false)
  const [submitError, setSubmitError] = useState('')

  const [toCancel, setToCancel] = useState(null)
  const [cancelling, setCancelling] = useState(false)
  const [cancelError, setCancelError] = useState('')
  const [changingId, setChangingId] = useState(null)

  const loadStays = () => {
    setStaysError(false)
    client
      .get('/api/stays/active')
      .then((res) => {
        setStays(res.data); setStaysUsingMock(false)
        // Khách vừa check-out từ nơi khác (RoomMap tự làm mới 30s) -> đóng panel, không cho tạo đơn cho stay đã đóng
        setOpenStay((cur) => (cur && !res.data.some((s) => s.stayId === cur.stayId) ? null : cur))
      })
      .catch((err) => {
        if (isBackendMissing(err)) { setStays(MOCK_ACTIVE_STAYS); setStaysUsingMock(true) }
        else setStaysError(true)
      })
  }

  const loadItems = () => {
    client
      .get('/api/service-items')
      .then((res) => setItems(res.data.filter((i) => i.isAvailable)))
      .catch((err) => {
        if (isBackendMissing(err)) setItems(MOCK_SERVICE_ITEMS)
        else toast.error(apiError(err)) // lỗi thật: không âm thầm để trống danh sách dịch vụ
      })
  }

  const loadOrders = () => {
    client
      .get('/api/service-orders')
      .then((res) => { setOrders(res.data.map(normalizeServiceOrder)); setOrdersUsingMock(false) })
      .catch((err) => {
        if (isBackendMissing(err)) { setOrders(MOCK_SERVICE_ORDERS.map(normalizeServiceOrder)); setOrdersUsingMock(true) }
      })
  }

  useEffect(() => { loadStays(); loadItems(); loadOrders() }, [])

  const ordersForStay = useMemo(() => {
    if (!openStay || !orders) return []
    return orders.filter((o) => o.stayId === openStay.stayId).sort((a, b) => String(b.orderDate).localeCompare(String(a.orderDate)))
  }, [orders, openStay])

  const pendingCountByStay = useMemo(() => {
    const m = {}
    for (const o of orders ?? []) {
      if (o.status === 'Pending' || o.status === 'Processing') m[o.stayId] = (m[o.stayId] ?? 0) + 1
    }
    return m
  }, [orders])

  const openStayPanel = (stay) => {
    setOpenStay(stay)
    setCart([])
    setPickItemId('')
    setPickQty(1)
    setSubmitError('')
  }

  const addToCart = () => {
    const item = items.find((i) => i.id === Number(pickItemId))
    if (!item || pickQty <= 0) return
    setCart((cur) => {
      const existing = cur.find((c) => c.serviceItemId === item.id)
      if (existing) {
        return cur.map((c) => (c.serviceItemId === item.id ? { ...c, quantity: c.quantity + pickQty } : c))
      }
      return [...cur, { serviceItemId: item.id, serviceName: item.serviceName, unitPrice: item.unitPrice, quantity: pickQty }]
    })
    setPickItemId('')
    setPickQty(1)
  }

  const removeFromCart = (serviceItemId) => setCart((cur) => cur.filter((c) => c.serviceItemId !== serviceItemId))

  const cartTotal = cart.reduce((sum, c) => sum + c.unitPrice * c.quantity, 0)

  const submitOrder = () => {
    if (cart.length === 0) return
    setSubmitError('')
    setSubmitting(true)
    client
      .post('/api/service-orders', {
        stayId: openStay.stayId,
        details: cart.map((c) => ({ serviceItemId: c.serviceItemId, quantity: c.quantity })),
      })
      .then(() => {
        toast.success(`Đã tạo đơn dịch vụ cho phòng ${openStay.roomNumber}`)
        setCart([])
        loadOrders()
      })
      .catch((err) => setSubmitError(apiError(err)))
      .finally(() => setSubmitting(false))
  }

  const changeStatus = (order, status) => {
    if (changingId) return // chặn bấm nhanh 2 nút trong lúc PATCH trước chưa xong
    setChangingId(order.id)
    client
      .patch(`/api/service-orders/${order.id}`, { status: denormalizeServiceOrderStatus(status) })
      .then(() => { toast.success(`Đơn #${order.id}: ${ORDER_STATUS[status].label}`); loadOrders() })
      .catch((err) => toast.error(apiError(err)))
      .finally(() => setChangingId(null))
  }

  const confirmCancel = () => {
    setCancelError('')
    setCancelling(true)
    client
      .patch(`/api/service-orders/${toCancel.id}`, { status: denormalizeServiceOrderStatus('Cancelled') })
      .then(() => { toast.success(`Đã hủy đơn #${toCancel.id}`); setToCancel(null); loadOrders() })
      .catch((err) => setCancelError(apiError(err)))
      .finally(() => setCancelling(false))
  }

  const grouped = useMemo(() => {
    const byCategory = {}
    for (const i of items) {
      const key = i.categoryName || 'Khác'
      if (!byCategory[key]) byCategory[key] = []
      byCategory[key].push(i)
    }
    return byCategory
  }, [items])

  return (
    <div>
      <div className="flex flex-wrap items-end justify-between gap-4">
        <div>
          <p className="font-display text-[15px] italic capitalize text-brand-600">dịch vụ · khách đang ở</p>
          <h1 className="mt-1 font-display text-4xl font-semibold tracking-tight">Đơn dịch vụ</h1>
          <p className="mt-1 text-sm text-ink-500">Gọi dịch vụ cho khách đang ở và theo dõi trạng thái đơn.</p>
        </div>
        {staysUsingMock && (
          <span className="rounded-full bg-amber-50 px-2.5 py-1 text-[11px] font-bold text-amber-800 ring-1 ring-amber-600/20">
            Dữ liệu mẫu
          </span>
        )}
      </div>

      {stays === null && !staysError && (
        <div className="mt-6 grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
          {Array.from({ length: 3 }).map((_, i) => <div key={i} className="h-32 animate-pulse rounded-2xl bg-cream-200" />)}
        </div>
      )}

      {staysError && <div className="mt-6"><ErrorState onRetry={loadStays} /></div>}

      {!staysError && stays !== null && stays.length === 0 && <EmptyState text="Không có khách nào đang ở" />}

      {!staysError && stays !== null && stays.length > 0 && (
        <div className="mt-6 grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
          {stays.map((s) => {
            const pending = pendingCountByStay[s.stayId] ?? 0
            return (
              <button
                key={s.stayId}
                onClick={() => openStayPanel(s)}
                className={`card-rise rounded-2xl bg-white p-5 text-left ring-1 ring-black/5 shadow-soft ${EASE} hover:-translate-y-1 hover:shadow-lift`}
              >
                <div className="flex items-start justify-between gap-3">
                  <div>
                    <p className="font-display text-2xl font-semibold tabular-nums">{s.roomNumber}</p>
                    <p className="mt-0.5 text-sm font-semibold text-ink-700">{s.guestName}</p>
                    <p className="mt-1 text-[11px] tabular-nums text-ink-500">{s.bookingCode}</p>
                  </div>
                  {pending > 0 && (
                    <span className="shrink-0 rounded-full bg-amber-50 px-2.5 py-1 text-[11px] font-bold text-amber-800 ring-1 ring-amber-600/20">
                      {pending} đơn
                    </span>
                  )}
                </div>
                <p className="mt-4 text-[12px] font-bold uppercase tracking-[0.14em] text-brand-600">Gọi dịch vụ →</p>
              </button>
            )
          })}
        </div>
      )}

      <SlideOver open={openStay !== null} eyebrow={openStay?.bookingCode} title={`Phòng ${openStay?.roomNumber ?? ''}`} onClose={() => setOpenStay(null)}>
        {openStay && (
          <div className="space-y-6">
            {/* Đơn đã có */}
            <div>
              <p className="text-[10px] font-bold uppercase tracking-[0.2em] text-ink-500">Đơn dịch vụ</p>
              {ordersUsingMock && <p className="mt-1 text-[11px] text-amber-700">dữ liệu mẫu, chờ API</p>}
              {ordersForStay.length === 0 ? (
                <p className="mt-2 text-[13px] italic text-ink-500">Chưa có đơn nào</p>
              ) : (
                <div className="mt-2 space-y-2.5">
                  {ordersForStay.map((o) => {
                    const st = ORDER_STATUS[o.status] ?? ORDER_STATUS.Pending
                    return (
                      <div key={o.id} className="rounded-xl bg-white p-3.5 ring-1 ring-black/5">
                        <div className="flex items-center justify-between gap-2">
                          <p className="text-[12px] font-bold tabular-nums text-ink-500">#{o.id} · {fmtDateTime(o.orderDate)}</p>
                          <span className={`rounded-full px-2.5 py-0.5 text-[11px] font-semibold ${st.badge}`}>{st.label}</span>
                        </div>
                        <div className="mt-2 space-y-1">
                          {o.details.map((d) => (
                            <p key={d.id} className="flex justify-between text-[12px] text-ink-700">
                              <span>{d.serviceName} × {d.quantity}</span>
                              <span className="tabular-nums">{formatVnd(d.subtotal)}</span>
                            </p>
                          ))}
                        </div>
                        <p className="mt-2 flex justify-between border-t border-black/[0.06] pt-2 text-[13px] font-bold">
                          <span>Tổng</span>
                          <span className="tabular-nums text-brand-700">{formatVnd(o.totalAmount)}</span>
                        </p>
                        {(o.status === 'Pending' || o.status === 'Processing') && (
                          <div className="mt-2.5 flex flex-wrap gap-1.5">
                            {o.status === 'Pending' && (
                              <button
                                onClick={() => changeStatus(o, 'Processing')}
                                disabled={changingId === o.id}
                                className={`rounded-full bg-sky-50 px-3 py-1 text-[11px] font-bold text-sky-700 ring-1 ring-sky-600/15 ${EASE} hover:bg-sky-600 hover:text-white disabled:opacity-40`}
                              >
                                Bắt đầu xử lý
                              </button>
                            )}
                            <button
                              onClick={() => changeStatus(o, 'Completed')}
                              disabled={changingId === o.id}
                              className={`rounded-full bg-emerald-50 px-3 py-1 text-[11px] font-bold text-emerald-700 ring-1 ring-emerald-600/15 ${EASE} hover:bg-emerald-600 hover:text-white disabled:opacity-40`}
                            >
                              Hoàn thành
                            </button>
                            <button
                              onClick={() => setToCancel(o)}
                              disabled={changingId === o.id}
                              className={`rounded-full px-3 py-1 text-[11px] font-bold text-rose-700 ring-1 ring-rose-600/20 ${EASE} hover:bg-rose-50 disabled:opacity-40`}
                            >
                              Hủy
                            </button>
                          </div>
                        )}
                      </div>
                    )
                  })}
                </div>
              )}
            </div>

            {/* Tạo đơn mới */}
            <div className="rounded-xl bg-white p-4 ring-1 ring-black/5">
              <p className="text-[10px] font-bold uppercase tracking-[0.2em] text-ink-500">Thêm dịch vụ</p>
              <div className="mt-3 flex items-end gap-2">
                <div className="flex-1">
                  <select
                    className="w-full rounded-lg bg-cream-50 px-3 py-2.5 text-sm ring-1 ring-black/10 outline-none focus:ring-2 focus:ring-brand-500/40"
                    value={pickItemId}
                    onChange={(e) => setPickItemId(e.target.value)}
                  >
                    <option value="">Chọn dịch vụ…</option>
                    {Object.entries(grouped).map(([cat, list]) => (
                      <optgroup key={cat} label={cat}>
                        {list.map((i) => <option key={i.id} value={i.id}>{i.serviceName} · {formatVnd(i.unitPrice)}</option>)}
                      </optgroup>
                    ))}
                  </select>
                </div>
                <input
                  type="number"
                  min={1}
                  className="w-16 rounded-lg bg-cream-50 px-2 py-2.5 text-center text-sm ring-1 ring-black/10 outline-none focus:ring-2 focus:ring-brand-500/40"
                  value={pickQty}
                  onChange={(e) => setPickQty(Math.max(1, Number(e.target.value) || 1))}
                />
                <button
                  onClick={addToCart}
                  disabled={!pickItemId}
                  className={`shrink-0 rounded-lg bg-ink-900 px-4 py-2.5 text-[13px] font-bold text-cream-50 ${EASE} hover:bg-ink-700 disabled:opacity-30`}
                >
                  Thêm
                </button>
              </div>

              {cart.length > 0 && (
                <div className="mt-4 space-y-1.5 border-t border-black/[0.06] pt-3">
                  {cart.map((c) => (
                    <div key={c.serviceItemId} className="flex items-center justify-between text-[13px]">
                      <span className="text-ink-700">{c.serviceName} × {c.quantity}</span>
                      <div className="flex items-center gap-2">
                        <span className="tabular-nums font-semibold">{formatVnd(c.unitPrice * c.quantity)}</span>
                        <button onClick={() => removeFromCart(c.serviceItemId)} className="text-ink-500/60 hover:text-rose-600">✕</button>
                      </div>
                    </div>
                  ))}
                  <div className="flex items-center justify-between border-t border-black/[0.06] pt-2 text-sm font-bold">
                    <span>Tổng cộng</span>
                    <span className="tabular-nums text-brand-700">{formatVnd(cartTotal)}</span>
                  </div>
                </div>
              )}

              {submitError && (
                <p className="mt-3 rounded-lg bg-amber-50 px-3 py-2 text-[12px] font-medium text-amber-800 ring-1 ring-amber-600/15">{submitError}</p>
              )}

              <button
                onClick={submitOrder}
                disabled={cart.length === 0 || submitting}
                className={`mt-4 w-full rounded-full bg-brand-600 py-2.5 text-[13px] font-bold text-white ${EASE} hover:bg-brand-700 active:scale-[0.98] disabled:opacity-30`}
              >
                {submitting ? 'Đang tạo đơn…' : 'Tạo đơn dịch vụ'}
              </button>
            </div>
          </div>
        )}
      </SlideOver>

      <ConfirmDialog
        open={toCancel !== null}
        title={`Hủy đơn dịch vụ #${toCancel?.id ?? ''}?`}
        message="Đơn dịch vụ sẽ chuyển sang trạng thái Đã hủy, không tính vào hoá đơn khi check-out."
        confirmLabel="Hủy đơn"
        busyLabel="Đang hủy…"
        busy={cancelling}
        error={cancelError}
        onConfirm={confirmCancel}
        onCancel={() => setToCancel(null)}
      />
    </div>
  )
}
