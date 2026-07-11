import { useEffect, useRef } from 'react'

const EASE = 'transition-all duration-300 ease-[cubic-bezier(0.32,0.72,0,1)]'

// Ngăn kéo trượt phải dùng chung cho form quản trị - cùng chuyển động
// với RoomDrawer của sơ đồ phòng. Esc hoặc bấm nền mờ để đóng.
export default function SlideOver({ open, eyebrow, title, onClose, children }) {
  const panelRef = useRef(null)

  // Esc để đóng + nhốt Tab trong panel (handler cần onClose nên tách riêng)
  useEffect(() => {
    if (!open) return
    const onKey = (e) => {
      if (e.key === 'Escape') return onClose()
      // Nhốt Tab trong panel: tới cuối vòng về đầu và ngược lại, không thoát ra sau lưng modal
      if (e.key === 'Tab') {
        const items = panelRef.current?.querySelectorAll('button, input, select, textarea, [href]')
        if (!items || items.length === 0) return
        const list = [...items].filter((el) => !el.disabled)
        const first = list[0]
        const last = list[list.length - 1]
        if (e.shiftKey && document.activeElement === first) { e.preventDefault(); last.focus() }
        else if (!e.shiftKey && document.activeElement === last) { e.preventDefault(); first.focus() }
      }
    }
    window.addEventListener('keydown', onKey)
    return () => window.removeEventListener('keydown', onKey)
  }, [open, onClose])

  // Auto-focus ô đầu CHỈ khi mở drawer - không phụ thuộc onClose, tránh re-arm timer
  // mỗi lần re-render (bug: đang gõ ô giá thì focus nhảy về ô Tên sau 350ms)
  useEffect(() => {
    if (!open) return
    const timer = setTimeout(() => {
      panelRef.current?.querySelector('input, select, textarea')?.focus()
    }, 350)
    return () => clearTimeout(timer)
  }, [open])

  return (
    <>
      <div
        onClick={onClose}
        className={`fixed inset-0 z-30 bg-ink-900/30 ${EASE} ${open ? 'opacity-100' : 'pointer-events-none opacity-0'}`}
      />
      <aside
        ref={panelRef}
        // inert khi đóng: panel chỉ trượt ra ngoài màn hình chứ vẫn trong DOM,
        // không có inert thì Tab vẫn lọt vào các ô nhập vô hình (kể cả ô mật khẩu)
        inert={!open || undefined}
        className={`fixed inset-y-0 right-0 z-40 flex w-full max-w-sm flex-col bg-cream-50 shadow-lift ${EASE} duration-500 ${
          open ? 'translate-x-0' : 'translate-x-full'
        }`}
      >
        <div className="flex items-start justify-between gap-3 border-b border-black/[0.06] px-5 py-4">
          <div>
            {eyebrow && <p className="font-display text-[13px] italic text-brand-600">{eyebrow}</p>}
            <p className="mt-0.5 font-display text-2xl font-semibold tracking-tight">{title}</p>
          </div>
          <button
            onClick={onClose}
            aria-label="Đóng"
            className={`flex h-8 w-8 shrink-0 items-center justify-center rounded-full bg-white text-sm font-bold text-ink-700 ring-1 ring-black/10 ${EASE} hover:bg-cream-100`}
          >
            ✕
          </button>
        </div>
        <div className="flex-1 overflow-y-auto p-5">{children}</div>
      </aside>
    </>
  )
}
