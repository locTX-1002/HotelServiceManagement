import { useEffect } from 'react'

const EASE = 'transition-all duration-300 ease-[cubic-bezier(0.32,0.72,0,1)]'

// Hộp thoại xác nhận cho hành động không hoàn tác được (xóa).
// Esc hoặc bấm nền mờ để hủy; `error` hiện lỗi từ máy chủ ngay trong hộp thoại.
export default function ConfirmDialog({ open, title, message, confirmLabel = 'Xóa', busy = false, error = '', onConfirm, onCancel }) {
  useEffect(() => {
    if (!open) return
    const onKey = (e) => e.key === 'Escape' && onCancel()
    window.addEventListener('keydown', onKey)
    return () => window.removeEventListener('keydown', onKey)
  }, [open, onCancel])

  return (
    <div className={`fixed inset-0 z-50 flex items-center justify-center px-6 ${open ? '' : 'pointer-events-none'}`}>
      <div onClick={onCancel} className={`absolute inset-0 bg-ink-900/30 ${EASE} ${open ? 'opacity-100' : 'opacity-0'}`} />
      <div
        className={`relative w-full max-w-sm rounded-2xl bg-cream-50 p-6 text-center shadow-lift ${EASE} ${
          open ? 'translate-y-0 opacity-100' : 'translate-y-3 opacity-0'
        }`}
      >
        {/* Icon vòm cảnh báo - lặp motif thẻ chìa khóa của toàn app */}
        <span className="mx-auto flex h-10 w-8 items-end justify-center rounded-t-full rounded-b-md bg-rose-50 pb-1 text-sm font-bold text-rose-600 ring-1 ring-rose-600/15">
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
            className={`rounded-full bg-rose-600 px-5 py-2.5 text-[13px] font-bold text-white ${EASE} hover:bg-rose-700 active:scale-[0.98] disabled:opacity-40`}
          >
            {busy ? 'Đang xóa…' : confirmLabel}
          </button>
        </div>
      </div>
    </div>
  )
}
