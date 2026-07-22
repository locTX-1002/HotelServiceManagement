import { useEffect, useState } from 'react'
import { EASE } from '../utils/ui'
import { formatVnd } from '../utils/roomStatus'
import { roomImage } from '../utils/roomImages'
import { roomMeta } from '../utils/roomMeta'

// Modal "Thông tin phòng" theo dung bo cuc booking engine (mau Nesta): anh + dai thumbnail ben
// trai, ten/mo ta ben phai, tien nghi dang luoi ✓ ben duoi, hang nut o day. Dung chung cho ca
// trang Dat phong moi (co gia + nut chon) lan Dat phong cua toi (chi xem - khong truyen onSelect/
// gia/so phong trong thi cac phan do tu an).
export default function RoomTypeDetailModal({ rt, selected, onSelect, onClose, selectLabel }) {
  const meta = roomMeta(rt.roomTypeName)
  const [imgIdx, setImgIdx] = useState(0)
  const active = selected?.roomTypeId != null && selected.roomTypeId === rt.roomTypeId

  useEffect(() => {
    const onKey = (e) => e.key === 'Escape' && onClose()
    window.addEventListener('keydown', onKey)
    return () => window.removeEventListener('keydown', onKey)
  }, [onClose])

  return (
    <div className="fixed inset-0 z-50 flex items-end justify-center px-4 pb-4 sm:items-center">
      <div onClick={onClose} className="absolute inset-0 bg-ink-900/40" />
      <div className="card-rise relative max-h-[88vh] w-full max-w-2xl overflow-y-auto rounded-2xl bg-cream-50 shadow-lift">
        <div className="flex items-center justify-between border-b border-black/[0.06] px-6 py-4">
          <p className="font-display text-lg font-semibold">Thông tin phòng</p>
          <button
            type="button"
            onClick={onClose}
            className="flex h-8 w-8 items-center justify-center rounded-full text-sm font-bold text-ink-500 hover:bg-black/[0.05] hover:text-ink-900"
            aria-label="Đóng"
          >
            ✕
          </button>
        </div>

        <div className="grid gap-6 p-6 sm:grid-cols-2">
          {/* Cot trai: anh chinh + dai thumbnail doi goc nhin */}
          <div>
            <div className="group relative overflow-hidden rounded-xl">
              <img src={roomImage(rt.roomTypeName, imgIdx)} alt={rt.roomTypeName} className="aspect-[4/3] w-full object-cover" />
              <span className="absolute bottom-2.5 left-2.5 rounded-full bg-ink-900/70 px-2.5 py-0.5 text-[11px] font-semibold text-white">
                {imgIdx + 1}/4
              </span>
              <button
                type="button"
                onClick={() => setImgIdx((imgIdx + 3) % 4)}
                className="absolute left-2 top-1/2 flex h-8 w-8 -translate-y-1/2 items-center justify-center rounded-full bg-white/85 text-sm font-bold text-ink-700 opacity-0 backdrop-blur-sm group-hover:opacity-100"
                aria-label="Ảnh trước"
              >
                ‹
              </button>
              <button
                type="button"
                onClick={() => setImgIdx((imgIdx + 1) % 4)}
                className="absolute right-2 top-1/2 flex h-8 w-8 -translate-y-1/2 items-center justify-center rounded-full bg-white/85 text-sm font-bold text-ink-700 opacity-0 backdrop-blur-sm group-hover:opacity-100"
                aria-label="Ảnh sau"
              >
                ›
              </button>
            </div>
            <div className="mt-2 flex gap-2">
              {[0, 1, 2, 3].map((i) => (
                <button
                  key={i}
                  type="button"
                  onClick={() => setImgIdx(i)}
                  className={`h-14 flex-1 overflow-hidden rounded-lg ring-2 ${EASE} ${
                    i === imgIdx ? 'ring-brand-600' : 'ring-transparent opacity-60 hover:opacity-100'
                  }`}
                  aria-label={`Góc ảnh ${i + 1}`}
                >
                  <img src={roomImage(rt.roomTypeName, i)} alt="" className="h-full w-full object-cover" />
                </button>
              ))}
            </div>
          </div>

          {/* Cot phai: ten + thong so + mo ta */}
          <div>
            <div className="flex items-start justify-between gap-3">
              <p className="font-display text-2xl font-semibold">{rt.roomTypeName}</p>
              {rt.availableCount != null && (
                <span className="shrink-0 rounded-full bg-emerald-50 px-2.5 py-1 text-[11px] font-bold text-emerald-700 ring-1 ring-emerald-600/15">
                  Còn {rt.availableCount} phòng
                </span>
              )}
            </div>
            <p className="mt-2 text-[12px] text-ink-700">
              🛏 {meta.bed} &nbsp;&nbsp; ⛶ {meta.area} m² &nbsp;&nbsp; 👤 {meta.capacity} khách
            </p>
            {/* whitespace-pre-line: admin nhập mỗi tiện nghi/vật dụng một dòng thì hiện đúng xuống dòng,
                không bị dồn thành 1 đoạn. Rỗng thì rơi về mô tả mẫu theo hạng phòng. */}
            <p className="mt-3.5 whitespace-pre-line text-sm leading-relaxed text-ink-700">{rt.description || meta.desc}</p>
          </div>
        </div>

        <div className="px-6 pb-2">
          <p className="text-[13px] font-bold">Tiện ích</p>
          <div className="mt-2.5 grid grid-cols-2 gap-x-6 gap-y-2 sm:grid-cols-3">
            {meta.amenities.map((a) => (
              <p key={a} className="flex items-center gap-2 text-[12.5px] text-ink-700">
                <span className="font-bold text-emerald-600">✓</span>
                {a}
              </p>
            ))}
          </div>
        </div>

        <div className="flex flex-wrap items-center justify-between gap-3 border-t border-black/[0.06] px-6 py-4">
          {rt.basePrice != null ? (
            <p className="font-display text-xl font-semibold tabular-nums">
              {formatVnd(rt.basePrice)}
              <span className="font-sans text-[11px] font-normal text-ink-500"> / đêm</span>
            </p>
          ) : (
            <span />
          )}
          <div className="flex gap-2.5">
            <button
              type="button"
              onClick={onClose}
              className={`rounded-full px-5 py-2 text-[13px] font-semibold text-ink-700 ring-1 ring-black/10 ${EASE} hover:bg-white`}
            >
              Đóng
            </button>
            {onSelect && (
              <button
                type="button"
                onClick={() => {
                  onSelect(active ? null : rt)
                  onClose()
                }}
                className={`rounded-full px-5 py-2 text-[13px] font-bold ${EASE} active:scale-[0.98] ${
                  active ? 'bg-emerald-500 text-white' : 'bg-ink-900 text-cream-50 hover:bg-ink-700'
                }`}
              >
                {active ? '✓ Đã chọn' : (selectLabel ?? 'Chọn loại phòng này')}
              </button>
            )}
          </div>
        </div>
      </div>
    </div>
  )
}
