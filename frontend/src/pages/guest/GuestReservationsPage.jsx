import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import guestClient, { apiError } from '../../api/guestClient'
import { formatVnd } from '../../utils/roomStatus'
import { normalizeReservationStatus } from '../../utils/apiShape'
import { EASE } from '../../utils/ui'
import { roomImage } from '../../utils/roomImages'
import RoomTypeDetailModal from '../../components/RoomTypeDetailModal'
import ConfirmDialog from '../../components/ConfirmDialog'

// Nhãn + màu trạng thái - khớp dict RES_STATUS của trang nhân viên để cùng 1 trạng thái luôn hiện
// cùng tên/màu dù xem từ phía khách hay phía lễ tân.
const RES_STATUS = {
  Pending: { label: 'Chờ xác nhận', badge: 'bg-amber-50 text-amber-800 ring-1 ring-amber-600/15' },
  Confirmed: { label: 'Đã xác nhận', badge: 'bg-sky-50 text-sky-700 ring-1 ring-sky-600/15' },
  CheckedIn: { label: 'Đang lưu trú', badge: 'bg-emerald-50 text-emerald-700 ring-1 ring-emerald-600/15' },
  Completed: { label: 'Đã trả phòng', badge: 'bg-stone-100 text-stone-600 ring-1 ring-stone-500/15' },
  Cancelled: { label: 'Đã hủy', badge: 'bg-rose-50 text-rose-700 ring-1 ring-rose-600/15' },
  NoShow: { label: 'Không đến', badge: 'bg-orange-50 text-orange-700 ring-1 ring-orange-600/15' },
}

const formatDate = (d) => new Date(d).toLocaleDateString('vi-VN', { day: '2-digit', month: '2-digit', year: 'numeric' })

// Danh sách đặt phòng của khách - thuần tra cứu. Các thao tác lúc đang ở (gọi dọn phòng, gọi đồ)
// nằm ở ô "Dịch vụ phòng" trên trang chủ, không nhồi vào từng thẻ nữa: thẻ bị kéo cao gấp đôi làm
// ảnh phòng bên trái phải giãn theo và mất tỉ lệ.
export default function GuestReservationsPage() {
  const [reservations, setReservations] = useState([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')
  const [detailRoom, setDetailRoom] = useState(null)
  // Huy dat phong: mo ConfirmDialog vi day la thao tac pha huy, khong hoan tac
  const [toCancel, setToCancel] = useState(null)
  const [cancelling, setCancelling] = useState(false)
  const [cancelError, setCancelError] = useState('')

  const load = () => {
    guestClient
      .get('/api/guest/me/reservations')
      .then((res) => setReservations(res.data ?? []))
      .catch((err) => setError(apiError(err)))
      .finally(() => setLoading(false))
  }

  useEffect(() => { load() }, [])

  const confirmCancel = () => {
    if (cancelling || !toCancel) return
    setCancelling(true)
    setCancelError('')
    // BE-4: chi huy duoc don thuoc chinh tai khoan nay va dang Pending/Confirmed - backend chan lai neu khong.
    guestClient
      .patch(`/api/guest/me/reservations/${toCancel.id}/cancel`)
      .then(() => { setToCancel(null); load() })
      .catch((err) => setCancelError(apiError(err)))
      .finally(() => setCancelling(false))
  }

  return (
    <div className="mx-auto max-w-3xl">
      <div className="flex flex-wrap items-end justify-between gap-4">
        <div>
          <p className="font-display text-[14px] italic text-brand-600">lịch sử &amp; lịch sắp tới</p>
          <h1 className="mt-1 font-display text-3xl font-semibold tracking-tight">Đặt phòng của tôi</h1>
        </div>
        <Link
          to="/guest/dat-phong-moi"
          className={`rounded-full bg-ink-900 px-5 py-2.5 text-[13px] font-bold text-cream-50 ${EASE} hover:bg-ink-700 active:scale-[0.98]`}
        >
          + Đặt phòng mới
        </Link>
      </div>

      {loading && <p className="mt-8 text-sm text-ink-500">Đang tải…</p>}
      {error && (
        <p className="mt-8 rounded-lg bg-amber-50 px-3.5 py-2.5 text-[12px] font-medium text-amber-800 ring-1 ring-amber-600/15">{error}</p>
      )}

      {!loading && !error && reservations.length === 0 && (
        <div className="mt-8 rounded-2xl bg-cream-50 p-10 text-center ring-1 ring-black/[0.06]">
          <p className="text-sm text-ink-500">Chưa có đặt phòng nào gắn với tài khoản này.</p>
          <Link
            to="/guest/dat-phong-moi"
            className={`mt-4 inline-block rounded-full bg-brand-600 px-6 py-2.5 text-[13px] font-bold text-white ${EASE} hover:bg-brand-700`}
          >
            Tìm phòng ngay
          </Link>
        </div>
      )}

      <div className="mt-8 space-y-4">
        {reservations.map((r) => {
          const s = RES_STATUS[normalizeReservationStatus(r.status)] ?? RES_STATUS.Pending
          return (
            <div key={r.id} className="card-rise overflow-hidden rounded-2xl bg-cream-50 ring-1 ring-black/[0.06]">
              {/* Chiều cao thẻ cố định ở màn rộng: mọi thẻ bằng nhau nên khung ảnh bên trái luôn
                  cùng một tỉ lệ, không còn cảnh thẻ nào nhiều chữ hơn thì ảnh bị kéo dài ra */}
              <div className="flex flex-col sm:h-52 sm:flex-row">
                <div className="h-36 shrink-0 sm:h-auto sm:w-44">
                  <img
                    src={roomImage(r.roomTypeName, 0)}
                    alt={r.roomTypeName}
                    className="h-full w-full object-cover"
                    loading="lazy"
                  />
                </div>
                <div className="min-w-0 flex-1 overflow-hidden p-5">
                  <div className="flex flex-wrap items-center justify-between gap-2">
                    <div>
                      <p className="text-[10px] font-bold uppercase tracking-[0.18em] tabular-nums text-ink-500">{r.bookingCode}</p>
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

                  <div className="mt-3 flex flex-wrap items-center gap-2">
                    <button
                      type="button"
                      onClick={() => setDetailRoom(r.roomTypeName)}
                      className={`rounded-full px-4 py-1.5 text-[12px] font-semibold text-ink-700 ring-1 ring-black/10 ${EASE} hover:bg-white`}
                    >
                      Xem chi tiết phòng
                    </button>
                    {/* Đang ở thì cho lối tắt sang ô Dịch vụ phòng - để cùng hàng, không thêm chiều cao thẻ */}
                    {normalizeReservationStatus(r.status) === 'CheckedIn' && (
                      <Link
                        to="/guest/dashboard"
                        className={`rounded-full bg-brand-600 px-4 py-1.5 text-[12px] font-bold text-white ${EASE} hover:bg-brand-700`}
                      >
                        Dịch vụ phòng →
                      </Link>
                    )}
                    {/* Chỉ huỷ được khi chưa nhận phòng (Chờ xác nhận / Đã xác nhận) - cùng hàng, giữ chiều cao thẻ */}
                    {['Pending', 'Confirmed'].includes(normalizeReservationStatus(r.status)) && (
                      <button
                        type="button"
                        onClick={() => { setCancelError(''); setToCancel(r) }}
                        className={`rounded-full px-4 py-1.5 text-[12px] font-semibold text-rose-700 ring-1 ring-rose-600/20 ${EASE} hover:bg-rose-50`}
                      >
                        Huỷ đặt phòng
                      </button>
                    )}
                  </div>

                  {/* Thông tin phụ gộp 1 dòng, cắt bớt nếu dài - giữ chiều cao thẻ ổn định */}
                  {(r.specialRequests || r.depositAmount != null) && (
                    <p className="mt-3 truncate text-[12px] text-ink-500">
                      {r.depositAmount != null && (
                        <span className="font-semibold text-emerald-700">Đã cọc {formatVnd(r.depositAmount)}</span>
                      )}
                      {r.depositAmount != null && r.specialRequests && ' · '}
                      {r.specialRequests && <span className="italic" title={r.specialRequests}>{r.specialRequests}</span>}
                    </p>
                  )}
                </div>
              </div>
            </div>
          )
        })}
      </div>

      {detailRoom && <RoomTypeDetailModal rt={{ roomTypeName: detailRoom }} onClose={() => setDetailRoom(null)} />}

      <ConfirmDialog
        open={toCancel !== null}
        title={`Huỷ đặt phòng ${toCancel?.bookingCode ?? ''}?`}
        message={`Lượt đặt phòng ${toCancel?.roomNumber ?? ''} (${formatDate(toCancel?.checkInDate ?? new Date())}) sẽ chuyển sang Đã huỷ. Hành động không hoàn tác được.`}
        confirmLabel="Huỷ đặt phòng"
        busyLabel="Đang huỷ…"
        busy={cancelling}
        error={cancelError}
        onConfirm={confirmCancel}
        onCancel={() => { if (!cancelling) setToCancel(null) }}
      />
    </div>
  )
}
