import { useEffect, useRef, useState } from 'react'
import guestClient, { apiError } from '../../api/guestClient'
import { formatVnd } from '../../utils/roomStatus'
import { normalizeReservationStatus } from '../../utils/apiShape'
import { getGuest } from '../../utils/guestSession'
import { EASE } from '../../utils/ui'
import { roomImage } from '../../utils/roomImages'

const HK_TYPES = [
  { value: 'Cleaning', label: 'Dọn phòng' },
  { value: 'ExtraTowels', label: 'Thêm khăn' },
  { value: 'ExtraWater', label: 'Thêm nước' },
  { value: 'Other', label: 'Khác' },
]

// Nhãn + màu trạng thái đặt phòng - đúng theo dict RES_STATUS của ReservationsPage.jsx (trang nhân viên)
// để cùng 1 trạng thái luôn hiện cùng 1 màu/tên dù xem từ phía khách hay phía lễ tân.
const RES_STATUS = {
  Pending: { label: 'Chờ xác nhận', badge: 'bg-amber-50 text-amber-800 ring-1 ring-amber-600/15' },
  Confirmed: { label: 'Đã xác nhận', badge: 'bg-sky-50 text-sky-700 ring-1 ring-sky-600/15' },
  CheckedIn: { label: 'Đang lưu trú', badge: 'bg-emerald-50 text-emerald-700 ring-1 ring-emerald-600/15' },
  Completed: { label: 'Đã trả phòng', badge: 'bg-stone-100 text-stone-600 ring-1 ring-stone-500/15' },
  Cancelled: { label: 'Đã hủy', badge: 'bg-rose-50 text-rose-700 ring-1 ring-rose-600/15' },
  NoShow: { label: 'Không đến', badge: 'bg-orange-50 text-orange-700 ring-1 ring-orange-600/15' },
}

const formatDate = (d) => new Date(d).toLocaleDateString('vi-VN', { day: '2-digit', month: '2-digit', year: 'numeric' })

export default function GuestDashboardPage() {
  const guest = getGuest()
  const [reservations, setReservations] = useState([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')
  // Ghi lai id da goi don phong thanh cong trong phien nay - gia tri la loai da gui (Cleaning/...),
  // hien "Đã gửi yêu cầu (loại)" thay vi cho bam lai.
  const [housekeepingSent, setHousekeepingSent] = useState({})
  const [housekeepingError, setHousekeepingError] = useState({})
  const [housekeepingType, setHousekeepingType] = useState({})
  const [housekeepingNote, setHousekeepingNote] = useState({})
  // Ref dong bo (khac state cap nhat bat dong bo) chan spam-click, dung pattern da dung o cac trang nhan vien.
  const housekeepingBusyRef = useRef({})

  // Danh muc dich vu - chi can tai 1 lan (khong gan voi tung the dat phong), chi tai khi co it nhat
  // 1 dat phong dang CheckedIn (khong tai thua khi khach chua/khong con luu tru).
  const [serviceCatalog, setServiceCatalog] = useState([])
  const [serviceCatalogLoading, setServiceCatalogLoading] = useState(false)
  const [serviceCatalogError, setServiceCatalogError] = useState('')
  const [serviceQuantities, setServiceQuantities] = useState({})
  const [serviceOrderError, setServiceOrderError] = useState({})
  const [serviceOrderSuccess, setServiceOrderSuccess] = useState({})
  const serviceOrderBusyRef = useRef({})

  useEffect(() => {
    guestClient
      .get('/api/guest/me/reservations')
      .then((res) => setReservations(res.data ?? []))
      .catch((err) => setError(apiError(err)))
      .finally(() => setLoading(false))
  }, [])

  useEffect(() => {
    const hasCheckedIn = reservations.some((r) => normalizeReservationStatus(r.status) === 'CheckedIn')
    if (!hasCheckedIn || serviceCatalog.length > 0 || serviceCatalogLoading) return
    setServiceCatalogLoading(true)
    guestClient
      .get('/api/guest/service-items')
      .then((res) => setServiceCatalog(res.data ?? []))
      .catch((err) => setServiceCatalogError(apiError(err)))
      .finally(() => setServiceCatalogLoading(false))
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [reservations])

  const requestHousekeeping = (reservationId) => {
    if (housekeepingBusyRef.current[reservationId]) return
    housekeepingBusyRef.current[reservationId] = true
    setHousekeepingError((prev) => ({ ...prev, [reservationId]: '' }))
    const requestType = housekeepingType[reservationId] ?? 'Cleaning'
    const note = (housekeepingNote[reservationId] ?? '').trim()
    guestClient
      .post('/api/guest/me/housekeeping-requests', { requestType, note: note || undefined })
      .then(() => setHousekeepingSent((prev) => ({ ...prev, [reservationId]: requestType })))
      .catch((err) => setHousekeepingError((prev) => ({ ...prev, [reservationId]: apiError(err) })))
      .finally(() => {
        housekeepingBusyRef.current[reservationId] = false
      })
  }

  // Tinh gia tri moi BEN TRONG functional updater (khong doc serviceQuantities tu closure ben
  // ngoai) - bam +/- lien tuc nhanh truoc khi React kip re-render se khong bi mat lan bam nao.
  const bumpServiceQty = (itemId, delta) =>
    setServiceQuantities((prev) => ({ ...prev, [itemId]: Math.max(0, (prev[itemId] ?? 0) + delta) }))

  const submitServiceOrder = (reservationId) => {
    if (serviceOrderBusyRef.current[reservationId]) return
    const items = Object.entries(serviceQuantities)
      .filter(([, qty]) => qty > 0)
      .map(([serviceItemId, quantity]) => ({ serviceItemId: Number(serviceItemId), quantity }))

    if (items.length === 0) {
      setServiceOrderError((prev) => ({ ...prev, [reservationId]: 'Chọn ít nhất 1 dịch vụ.' }))
      return
    }

    serviceOrderBusyRef.current[reservationId] = true
    setServiceOrderError((prev) => ({ ...prev, [reservationId]: '' }))
    setServiceOrderSuccess((prev) => ({ ...prev, [reservationId]: '' }))
    guestClient
      .post('/api/guest/me/service-orders', { items })
      .then((res) => {
        setServiceOrderSuccess((prev) => ({
          ...prev,
          [reservationId]: `Đã gửi yêu cầu, tổng ${formatVnd(res.data.totalAmount)}`,
        }))
        setServiceQuantities({})
      })
      .catch((err) => setServiceOrderError((prev) => ({ ...prev, [reservationId]: apiError(err) })))
      .finally(() => {
        serviceOrderBusyRef.current[reservationId] = false
      })
  }

  return (
    <div className="mx-auto max-w-3xl">
      {/* Hero anh thay cho tieu de nen trang - theo mau template booking, dong bo voi hero cua
          trang Dat phong moi */}
      <div className="relative h-40 overflow-hidden rounded-2xl sm:h-48">
        <img src="/img/v1.jpg" alt="" className="h-full w-full object-cover" />
        <div className="absolute inset-0 bg-gradient-to-r from-ink-900/75 to-ink-900/15" />
        <div className="absolute left-7 top-1/2 -translate-y-1/2 text-white">
          <p className="font-display text-[14px] italic text-white/80">xin chào</p>
          <h1 className="mt-1 font-display text-3xl font-semibold tracking-tight">{guest?.fullName ?? 'Khách lưu trú'}</h1>
          <p className="mt-1.5 text-[13px] text-white/70">Danh sách đặt phòng của bạn tại khách sạn.</p>
        </div>
      </div>

      {loading && <p className="mt-8 text-sm text-ink-500">Đang tải…</p>}
      {error && (
        <p className="mt-8 rounded-lg bg-amber-50 px-3.5 py-2.5 text-[12px] font-medium text-amber-800 ring-1 ring-amber-600/15">
          {error}
        </p>
      )}

      {!loading && !error && reservations.length === 0 && (
        <p className="mt-8 text-sm text-ink-500">Chưa có đặt phòng nào gắn với tài khoản này.</p>
      )}

      <div className="mt-8 space-y-4">
        {reservations.map((r) => {
          const s = RES_STATUS[normalizeReservationStatus(r.status)] ?? RES_STATUS.Pending
          return (
            <div key={r.id} className="card-rise overflow-hidden rounded-2xl bg-cream-50 ring-1 ring-black/[0.06]">
              {/* Card ngang kieu danh sach booking engine: anh loai phong ben trai, noi dung ben phai */}
              <div className="flex flex-col sm:flex-row">
              <div className="relative h-32 shrink-0 sm:h-auto sm:w-44">
                <img
                  src={roomImage(r.roomTypeName, 0)}
                  alt={r.roomTypeName}
                  className="absolute inset-0 h-full w-full object-cover"
                  loading="lazy"
                />
              </div>
              <div className="flex-1 p-5">
              <div className="flex flex-wrap items-center justify-between gap-2">
                <div>
                  <p className="text-[10px] font-bold uppercase tracking-[0.18em] text-ink-500 tabular-nums">{r.bookingCode}</p>
                  <p className="mt-0.5 font-display text-xl font-semibold">Phòng {r.roomNumber} · {r.roomTypeName}</p>
                </div>
                <span className={`rounded-full px-3 py-1 text-[11px] font-bold uppercase tracking-wide ${s.badge}`}>
                  {s.label}
                </span>
              </div>
              <div className="mt-3 grid grid-cols-2 gap-3 text-[13px] text-ink-700 sm:grid-cols-3">
                <div>
                  <p className="text-[10px] font-bold uppercase tracking-wider text-ink-500">Nhận phòng</p>
                  <p className="mt-0.5">{formatDate(r.checkInDate)}</p>
                </div>
                <div>
                  <p className="text-[10px] font-bold uppercase tracking-wider text-ink-500">Trả phòng</p>
                  <p className="mt-0.5">{formatDate(r.checkOutDate)}</p>
                </div>
                <div>
                  <p className="text-[10px] font-bold uppercase tracking-wider text-ink-500">Số khách</p>
                  <p className="mt-0.5">{r.numberOfGuests} người</p>
                </div>
              </div>
              {r.specialRequests && (
                <p className="mt-3 text-[12px] italic text-ink-500">Yêu cầu đặc biệt: {r.specialRequests}</p>
              )}
              {r.depositAmount != null && (
                <p className="mt-1 text-[12px] text-emerald-700">Đã đặt cọc: {formatVnd(r.depositAmount)}</p>
              )}

              {normalizeReservationStatus(r.status) === 'CheckedIn' && (
                <div className="mt-4 space-y-4 border-t border-black/[0.06] pt-4">
                  <div>
                    <p className="text-[11px] font-bold uppercase tracking-wider text-ink-500">Gọi dọn phòng</p>
                    {housekeepingSent[r.id] ? (
                      <p className="mt-2 text-[12px] font-semibold text-emerald-700">
                        ✓ Đã gửi yêu cầu ({HK_TYPES.find((t) => t.value === housekeepingSent[r.id])?.label ?? 'Khác'})
                        {' '}— lễ tân sẽ xử lý sớm nhất.
                      </p>
                    ) : (
                      <>
                        <div className="mt-2 flex flex-wrap gap-1.5">
                          {HK_TYPES.map((t) => (
                            <button
                              key={t.value}
                              type="button"
                              onClick={() => setHousekeepingType((prev) => ({ ...prev, [r.id]: t.value }))}
                              className={`rounded-full px-3 py-1.5 text-[11px] font-bold ${EASE} ${
                                (housekeepingType[r.id] ?? 'Cleaning') === t.value
                                  ? 'bg-brand-600 text-white'
                                  : 'bg-white text-ink-700 ring-1 ring-black/10 hover:bg-cream-50'
                              }`}
                            >
                              {t.label}
                            </button>
                          ))}
                        </div>
                        <textarea
                          rows={2}
                          maxLength={300}
                          className="mt-2 w-full rounded-lg border border-black/15 bg-white px-3 py-2 text-[12px] outline-none placeholder:text-ink-500/40 focus:border-brand-500 focus:ring-2 focus:ring-brand-500/20"
                          placeholder="Ghi chú thêm (không bắt buộc)"
                          value={housekeepingNote[r.id] ?? ''}
                          onChange={(e) => setHousekeepingNote((prev) => ({ ...prev, [r.id]: e.target.value }))}
                        />
                        <button
                          type="button"
                          onClick={() => requestHousekeeping(r.id)}
                          className="mt-2 rounded-full bg-brand-600 px-4 py-2 text-[12px] font-bold uppercase tracking-wider text-white transition-all duration-300 ease-[cubic-bezier(0.32,0.72,0,1)] hover:bg-brand-700 active:scale-[0.98]"
                        >
                          Gửi yêu cầu
                        </button>
                        {housekeepingError[r.id] && (
                          <p className="mt-2 text-[12px] font-medium text-amber-800">{housekeepingError[r.id]}</p>
                        )}
                      </>
                    )}
                  </div>

                  <div>
                    <p className="text-[11px] font-bold uppercase tracking-wider text-ink-500">Đặt thêm dịch vụ</p>
                    {serviceCatalogLoading && <p className="mt-2 text-[12px] text-ink-500">Đang tải danh mục…</p>}
                    {serviceCatalogError && (
                      <p className="mt-2 text-[12px] font-medium text-amber-800">{serviceCatalogError}</p>
                    )}
                    {!serviceCatalogLoading && !serviceCatalogError && serviceCatalog.length > 0 && (
                      <>
                        <div className="mt-2 grid grid-cols-1 gap-2 sm:grid-cols-2">
                          {serviceCatalog.map((item) => (
                            <div
                              key={item.id}
                              className="flex items-center justify-between gap-2 rounded-lg bg-white px-3 py-2 ring-1 ring-black/[0.05]"
                            >
                              <div>
                                <p className="text-[12px] font-semibold">{item.serviceName}</p>
                                <p className="text-[11px] text-ink-500">{formatVnd(item.unitPrice)}</p>
                              </div>
                              <div className="flex items-center gap-2">
                                <button
                                  type="button"
                                  onClick={() => bumpServiceQty(item.id, -1)}
                                  className={`flex h-6 w-6 items-center justify-center rounded-full text-[13px] font-bold text-ink-700 ring-1 ring-black/10 ${EASE} hover:bg-cream-50`}
                                >
                                  −
                                </button>
                                <span className="w-4 text-center text-[12px] font-semibold tabular-nums">
                                  {serviceQuantities[item.id] ?? 0}
                                </span>
                                <button
                                  type="button"
                                  onClick={() => bumpServiceQty(item.id, 1)}
                                  className={`flex h-6 w-6 items-center justify-center rounded-full text-[13px] font-bold text-ink-700 ring-1 ring-black/10 ${EASE} hover:bg-cream-50`}
                                >
                                  +
                                </button>
                              </div>
                            </div>
                          ))}
                        </div>
                        <button
                          type="button"
                          onClick={() => submitServiceOrder(r.id)}
                          className="mt-2 rounded-full bg-brand-600 px-4 py-2 text-[12px] font-bold uppercase tracking-wider text-white transition-all duration-300 ease-[cubic-bezier(0.32,0.72,0,1)] hover:bg-brand-700 active:scale-[0.98]"
                        >
                          Gửi yêu cầu
                        </button>
                        {serviceOrderError[r.id] && (
                          <p className="mt-2 text-[12px] font-medium text-amber-800">{serviceOrderError[r.id]}</p>
                        )}
                        {serviceOrderSuccess[r.id] && (
                          <p className="mt-2 text-[12px] font-semibold text-emerald-700">✓ {serviceOrderSuccess[r.id]}</p>
                        )}
                      </>
                    )}
                  </div>
                </div>
              )}
              </div>
              </div>
            </div>
          )
        })}
      </div>
    </div>
  )
}
