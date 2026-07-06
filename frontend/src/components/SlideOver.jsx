import { useEffect } from 'react'

const EASE = 'transition-all duration-300 ease-[cubic-bezier(0.32,0.72,0,1)]'

// Ngăn kéo trượt phải dùng chung cho form quản trị - cùng chuyển động
// với RoomDrawer của sơ đồ phòng. Esc hoặc bấm nền mờ để đóng.
export default function SlideOver({ open, eyebrow, title, onClose, children }) {
  useEffect(() => {
    if (!open) return
    const onKey = (e) => e.key === 'Escape' && onClose()
    window.addEventListener('keydown', onKey)
    return () => window.removeEventListener('keydown', onKey)
  }, [open, onClose])

  return (
    <>
      <div
        onClick={onClose}
        className={`fixed inset-0 z-30 bg-ink-900/30 ${EASE} ${open ? 'opacity-100' : 'pointer-events-none opacity-0'}`}
      />
      <aside
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
