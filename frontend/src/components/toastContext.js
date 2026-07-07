import { createContext, useContext } from 'react'

// Tách context + hook khỏi ToastHost.jsx để file component chỉ export component
// (yêu cầu của React Fast Refresh).
export const ToastContext = createContext(null)

// Ngoài ToastProvider vẫn gọi được nhưng không hiện gì (an toàn cho test/lỗi cây)
const NOOP = { success: () => {}, error: () => {} }
export const useToast = () => useContext(ToastContext) ?? NOOP
