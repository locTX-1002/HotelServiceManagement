import { useEffect } from 'react'
import { EASE } from '../utils/ui'

// tone: 'danger' (đỏ, mặc định - xóa/hủy) hoặc 'primary' (xanh sky - hành động bình thường như check-in)
const TONE = {
  danger: { icon: 'bg-rose-50 text-rose-600 ring-1 ring-rose-600/15', btn: 'bg-rose-600 hover:bg-rose-700' },
  primary: { icon: 'bg-sky-50 text-sky-600 ring-1 ring-sky-600/15', btn: 'bg-sky-600 hover:bg-sky-700' },
}

// Hộp thoại xác nhận cho hành động cần chốt lại trước khi thực hiện (xóa, hủy, check-in...).
// Esc hoặc bấm nền mờ để hủy; `error` hiện lỗi từ máy chủ ngay trong hộp thoại.
export default function ConfirmDialog({ open, title, message, confirmLabel = 'Xóa', busyLabel, tone = 'danger', busy = false, error = '', onConfirm, onCancel }) {
  const t = TONE[tone] ?? TONE.danger
  useEffect(() => {
    if (!open) return
    const onKey = (e) => e.key === 'Escape' && onCancel()
    window.addEventListener('keydown', onKey)
    return () => window.removeEventListener('keydown', onKey)
  }, [open, onCancel])

  return (
    // inert khi đóng: dialog chỉ ẩn bằng opacity nên không có inert thì Tab vẫn tới được nút Xóa vô hình
    <div inert={!open || undefined} className={`fixed inset-0 z-50 flex items-center justify-center px-6 ${open ? '' : 'pointer-events-none'}`}>
      <div onClick={onCancel} className={`absolute inset-0 bg-ink-900/30 ${EASE} ${open ? 'opacity-100' : 'opacity-0'}`} />
      <div
        className={`relative w-full max-w-sm rounded-2xl bg-cream-50 p-6 text-center shadow-lift ${EASE} ${
          open ? 'translate-y-0 opacity-100' : 'translate-y-3 opacity-0'
        }`}
      >
        {/* Icon vòm cảnh báo - lặp motif thẻ chìa khóa của toàn app */}
        <span className={`mx-auto flex h-10 w-8 items-end justify-center rounded-t-full rounded-b-md pb-1 text-sm font-bold ${t.icon}`}>
          !
        </span>
        <p className="mt-4 font-display text-xl font-semibold tracking-tight">{title}</p>
        <p className="mt-2 text-[13px] leading-relaxed text-ink-500">{message}</p>

        {error && (
          <p className="mt-3 rounded-lg bg-amber-50 px-3.5 py-2.5 text-left text-[12px] font-medium text-amber-800 ring-1 ring-amber-600/15">
            {error}
          </p>
        )}

        <div className="mt-6 flex justify-center gap-2.5">
          <button
            onClick={onCancel}
            className={`rounded-full px-5 py-2.5 text-[13px] font-semibold text-ink-700 ring-1 ring-black/10 ${EASE} hover:bg-white`}
          >
            Hủy
          </button>
          <button
            onClick={onConfirm}
            disabled={busy}
            className={`rounded-full px-5 py-2.5 text-[13px] font-bold text-white ${t.btn} ${EASE} active:scale-[0.98] disabled:opacity-40`}
          >
            {busy ? (busyLabel ?? 'Đang xóa…') : confirmLabel}
          </button>
        </div>
      </div>
    </div>
  )
}
