import { useCallback, useMemo, useRef, useState } from 'react'
import { EASE } from '../utils/ui'
import { ToastContext } from './toastContext'

// Toast thông báo ngắn (thành công / lỗi) - tự biến mất sau 3.2s.
// Dùng: const toast = useToast(); toast.success('Đã lưu').
export function ToastProvider({ children }) {
  const [toasts, setToasts] = useState([])
  const idRef = useRef(0)

  const remove = useCallback((id) => setToasts((list) => list.filter((t) => t.id !== id)), [])

  const push = useCallback(
    (type, message) => {
      const id = ++idRef.current
      setToasts((list) => [...list, { id, type, message }])
      setTimeout(() => remove(id), 3200)
    },
    [remove],
  )

  // push ổn định (useCallback) nên api cũng ổn định -> không làm con render lại vô cớ
  const api = useMemo(() => ({ success: (m) => push('success', m), error: (m) => push('error', m) }), [push])

  const tone = {
    success: 'bg-emerald-50 text-emerald-800 ring-emerald-600/15',
    error: 'bg-rose-50 text-rose-800 ring-rose-600/15',
  }
  const dot = { success: 'bg-emerald-500', error: 'bg-rose-500' }

  return (
    <ToastContext.Provider value={api}>
      {children}
      {/* Ngăn xếp toast góc dưới phải, trên mọi lớp khác (drawer z-40) */}
      <div className="pointer-events-none fixed inset-x-0 bottom-0 z-[60] flex flex-col items-center gap-2 p-4 sm:items-end sm:pr-6">
        {toasts.map((t) => (
          <div
            key={t.id}
            className={`card-rise pointer-events-auto flex max-w-sm items-center gap-3 rounded-full px-4 py-2.5 text-[13px] font-semibold shadow-lift ring-1 ${EASE} ${tone[t.type]}`}
          >
            <span className={`h-2 w-2 shrink-0 rounded-full ${dot[t.type]}`} />
            <span>{t.message}</span>
            <button
              onClick={() => remove(t.id)}
              aria-label="Đóng"
              className="ml-1 shrink-0 text-current/60 hover:text-current"
            >
              ✕
            </button>
          </div>
        ))}
      </div>
    </ToastContext.Provider>
  )
}
