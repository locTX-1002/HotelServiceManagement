import { useEffect, useState } from 'react'
import { useNavigate, useSearchParams } from 'react-router-dom'
import client, { isBackendMissing } from '../api/client'
import ErrorState from '../components/ErrorState'
import { useToast } from '../components/toastContext'
import { MOCK_INVOICE } from '../mock/hotelMock'
import { fmtDateTime } from '../utils/dates'
import { formatVnd } from '../utils/roomStatus'

const EASE = 'transition-all duration-300 ease-[cubic-bezier(0.32,0.72,0,1)]'

const INVOICE_STATUS = {
  Unpaid: { label: 'Chưa thanh toán', badge: 'bg-rose-50 text-rose-700 ring-1 ring-rose-600/15' },
  PartiallyPaid: { label: 'Thanh toán một phần', badge: 'bg-amber-50 text-amber-800 ring-1 ring-amber-600/15' },
  Paid: { label: 'Đã thanh toán', badge: 'bg-emerald-50 text-emerald-700 ring-1 ring-emerald-600/15' },
  Cancelled: { label: 'Đã hủy', badge: 'bg-stone-100 text-stone-600 ring-1 ring-stone-500/15' },
}

const PAYMENT_METHODS = [
  { value: 'Cash', label: 'Tiền mặt' },
  { value: 'BankTransfer', label: 'Chuyển khoản' },
  { value: 'Card', label: 'Thẻ' },
]

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

export default function InvoicePage() {
  const [searchParams] = useSearchParams()
  const navigate = useNavigate()
  const toast = useToast()
  const stayId = searchParams.get('stayId')
  const roomNumber = searchParams.get('roomNumber')
  const guestName = searchParams.get('guestName')

  const [invoice, setInvoice] = useState(null)
  const [notFound, setNotFound] = useState(false)
  const [loadError, setLoadError] = useState(false)
  const [usingMock, setUsingMock] = useState(false)
  const [creating, setCreating] = useState(false)

  const [amount, setAmount] = useState('')
  const [method, setMethod] = useState('Cash')
  const [paying, setPaying] = useState(false)
  const [payError, setPayError] = useState('')
  const [lastPayment, setLastPayment] = useState(null)

  const load = () => {
    if (!stayId) return
    setLoadError(false)
    setNotFound(false)
    client
      .get(`/api/invoices/stay/${stayId}`)
      .then((res) => {
        setInvoice(res.data)
        setUsingMock(false)
        // Chỉ tự điền đủ số tiền khi hoá đơn chưa có đồng nào - PartiallyPaid không biết chính xác còn thiếu bao nhiêu
        // (backend không trả tổng đã trả), để trống buộc lễ tân xác nhận lại với khách.
        setAmount(res.data.status === 'Unpaid' ? String(res.data.totalAmount) : '')
      })
      .catch((err) => {
        if (err.response?.status === 404) setNotFound(true)
        else if (isBackendMissing(err)) { setInvoice(MOCK_INVOICE); setUsingMock(true); setAmount(String(MOCK_INVOICE.totalAmount)) }
        else setLoadError(true)
      })
  }

  useEffect(load, [stayId])

  const createInvoice = () => {
    setCreating(true)
    client
      .post(`/api/invoices/stay/${stayId}`)
      .then(() => { toast.success('Đã tạo hoá đơn'); load() })
      .catch((err) => toast.error(apiError(err)))
      .finally(() => setCreating(false))
  }

  const submitPayment = () => {
    setPayError('')
    const amt = Number(amount)
    if (!amt || amt <= 0) { setPayError('Nhập số tiền hợp lệ.'); return }
    setPaying(true)
    client
      .post('/api/payments', { invoiceId: invoice.invoiceId, amount: amt, paymentMethod: method })
      .then((res) => {
        toast.success(`Đã ghi nhận thanh toán ${formatVnd(amt)}`)
        setLastPayment(res.data)
        load()
      })
      .catch((err) => setPayError(apiError(err)))
      .finally(() => setPaying(false))
  }

  if (!stayId) {
    return (
      <div>
        <h1 className="font-display text-4xl font-semibold tracking-tight">Hoá đơn &amp; thanh toán</h1>
        <EmptyState text="Chưa chọn hoá đơn nào. Vào từ nút &quot;Ghi nhận thanh toán&quot; ngay sau khi check-out." />
        <div className="mt-4 flex justify-center">
          <button onClick={() => navigate('/checkin-checkout')} className={`text-[12px] font-bold uppercase tracking-wider text-brand-600 ${EASE} hover:underline`}>
            Về Check-in / Check-out →
          </button>
        </div>
      </div>
    )
  }

  const status = invoice ? (INVOICE_STATUS[invoice.status] ?? INVOICE_STATUS.Unpaid) : null
  const canPay = invoice && invoice.status !== 'Paid' && invoice.status !== 'Cancelled'

  return (
    <div className="mx-auto max-w-lg">
      <div>
        <p className="font-display text-[15px] italic capitalize text-brand-600">lễ tân · thu tiền khách</p>
        <h1 className="mt-1 font-display text-4xl font-semibold tracking-tight">Hoá đơn &amp; thanh toán</h1>
        {(roomNumber || guestName) && (
          <p className="mt-1 text-sm text-ink-500">Phòng {roomNumber} · {guestName}</p>
        )}
        {usingMock && (
          <span className="mt-2 inline-block rounded-full bg-amber-50 px-2.5 py-1 text-[11px] font-bold text-amber-800 ring-1 ring-amber-600/20">
            Dữ liệu mẫu
          </span>
        )}
      </div>

      {loadError && <div className="mt-6"><ErrorState onRetry={load} /></div>}

      {notFound && !loadError && (
        <div className="mt-6 flex flex-col items-center rounded-2xl border border-dashed border-black/10 bg-white/60 px-6 py-14">
          <span className="h-12 w-9 rounded-t-full rounded-b-md border-2 border-dashed border-brand-600/30" />
          <p className="mt-4 font-display text-lg italic text-ink-700">Chưa có hoá đơn cho lượt ở này</p>
          <button
            onClick={createInvoice}
            disabled={creating}
            className={`mt-4 rounded-full bg-brand-600 px-5 py-2.5 text-[13px] font-bold text-white ${EASE} hover:bg-brand-700 active:scale-[0.98] disabled:opacity-40`}
          >
            {creating ? 'Đang tạo…' : 'Tạo hoá đơn'}
          </button>
        </div>
      )}

      {invoice && !loadError && (
        <div className="mt-6 card-rise bezel-shell">
          <div className="bezel-core px-6 py-7">
            <div className="flex items-center justify-between">
              <p className="text-[11px] font-bold uppercase tracking-[0.18em] text-ink-500">
                Hoá đơn #{invoice.invoiceId} · {fmtDateTime(invoice.invoiceDate)}
              </p>
              <span className={`rounded-full px-2.5 py-1 text-[11px] font-semibold ${status.badge}`}>{status.label}</span>
            </div>

            <div className="mt-4 space-y-2 rounded-xl bg-white p-4 ring-1 ring-black/5">
              <div className="flex items-center justify-between text-[13px]">
                <span className="text-ink-500">Tiền phòng</span>
                <span className="font-semibold tabular-nums">{formatVnd(invoice.roomCharge)}</span>
              </div>
              <div className="flex items-center justify-between text-[13px]">
                <span className="text-ink-500">Tiền dịch vụ</span>
                <span className="font-semibold tabular-nums">{formatVnd(invoice.serviceCharge)}</span>
              </div>
              <div className="flex items-center justify-between border-t border-black/[0.06] pt-2 text-sm">
                <span className="font-semibold text-ink-900">Tổng cộng</span>
                <span className="font-display text-lg font-semibold tabular-nums text-brand-700">{formatVnd(invoice.totalAmount)}</span>
              </div>
            </div>

            {lastPayment && (
              <div className="mt-4 rounded-xl bg-emerald-50 p-4 text-[12px] text-emerald-800 ring-1 ring-emerald-600/15">
                Đã thu {formatVnd(lastPayment.amount)} · {PAYMENT_METHODS.find((m) => m.value === lastPayment.paymentMethod)?.label ?? lastPayment.paymentMethod} · mã giao dịch {lastPayment.transactionId}
              </div>
            )}

            {!canPay && (
              <p className="mt-5 text-center text-[13px] italic text-ink-500">
                {invoice.status === 'Paid' ? 'Hoá đơn đã thanh toán đủ.' : 'Hoá đơn đã hủy, không thể thu thêm.'}
              </p>
            )}

            {canPay && (
              <div className="mt-5 border-t border-black/[0.06] pt-5">
                <p className="text-[11px] font-bold uppercase tracking-[0.18em] text-ink-500">Ghi nhận thanh toán</p>
                {invoice.status === 'PartiallyPaid' && (
                  <p className="mt-1 text-[12px] text-amber-700">Đã trả một phần - kiểm tra lại số tiền còn thiếu với khách trước khi nhập.</p>
                )}
                <div className="mt-3 flex items-end gap-2">
                  <div className="flex-1">
                    <label className="mb-1 block text-[10px] font-bold uppercase tracking-[0.16em] text-ink-500">Số tiền</label>
                    <input
                      type="number"
                      min={1}
                      className="w-full rounded-lg bg-cream-50 px-3 py-2.5 text-sm tabular-nums ring-1 ring-black/10 outline-none focus:ring-2 focus:ring-brand-500/40"
                      value={amount}
                      onChange={(e) => setAmount(e.target.value)}
                    />
                  </div>
                  <div>
                    <label className="mb-1 block text-[10px] font-bold uppercase tracking-[0.16em] text-ink-500">Hình thức</label>
                    <select
                      className="rounded-lg bg-cream-50 px-3 py-2.5 text-sm ring-1 ring-black/10 outline-none focus:ring-2 focus:ring-brand-500/40"
                      value={method}
                      onChange={(e) => setMethod(e.target.value)}
                    >
                      {PAYMENT_METHODS.map((m) => <option key={m.value} value={m.value}>{m.label}</option>)}
                    </select>
                  </div>
                </div>

                {payError && (
                  <p className="mt-3 rounded-lg bg-amber-50 px-3.5 py-2.5 text-[12px] font-medium text-amber-800 ring-1 ring-amber-600/15">{payError}</p>
                )}

                <button
                  onClick={submitPayment}
                  disabled={paying}
                  className={`mt-4 w-full rounded-full bg-brand-600 py-2.5 text-[13px] font-bold text-white ${EASE} hover:bg-brand-700 active:scale-[0.98] disabled:opacity-40`}
                >
                  {paying ? 'Đang ghi nhận…' : 'Xác nhận thu tiền'}
                </button>
              </div>
            )}
          </div>
        </div>
      )}
    </div>
  )
}
