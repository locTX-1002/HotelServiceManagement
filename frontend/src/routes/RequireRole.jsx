import { Outlet, useLocation, useNavigate } from 'react-router-dom'
import { EASE } from '../utils/ui'
import { ROLE_LABEL, canAccess, homeFor } from '../utils/roles'
import { getUser } from '../utils/session'

// Chặn route theo vai trò (lớp UX - backend vẫn chặn thật bằng [Authorize]).
// Vào thẳng URL không thuộc quyền -> panel giải thích thay vì trang trắng.
export default function RequireRole() {
  const { pathname } = useLocation()
  const navigate = useNavigate()
  const user = getUser()

  if (canAccess(user?.role, pathname)) return <Outlet />

  return (
    <div className="flex flex-col items-center rounded-2xl border border-dashed border-black/10 bg-white/60 px-6 py-16 text-center">
      <span className="h-12 w-9 rounded-t-full rounded-b-md border-2 border-dashed border-brand-600/30" />
      <p className="mt-5 font-display text-2xl font-semibold tracking-tight">Khu vực này không thuộc vai trò của bạn</p>
      <p className="mt-2 max-w-sm text-[13px] leading-relaxed text-ink-500">
        Tài khoản <span className="font-semibold text-ink-700">{ROLE_LABEL[user?.role] ?? user?.role}</span> không có quyền
        truy cập trang này. Nếu cần, nhờ Quản trị viên cấp quyền.
      </p>
      <button
        onClick={() => navigate(homeFor(user?.role))}
        className={`mt-6 rounded-full bg-ink-900 px-5 py-2.5 text-[13px] font-bold text-cream-50 ${EASE} hover:bg-ink-700 active:scale-[0.98]`}
      >
        Về trang của tôi
      </button>
    </div>
  )
}
