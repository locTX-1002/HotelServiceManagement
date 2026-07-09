import { Navigate, Outlet, useLocation } from 'react-router-dom'
import { getToken } from '../utils/session'

// Chặn toàn bộ khu vực nghiệp vụ khi chưa đăng nhập.
// Nhớ lại trang đang đứng (kèm query) để login xong quay về đúng chỗ.
export default function ProtectedRoute() {
  const location = useLocation()

  if (!getToken()) {
    return <Navigate to="/login" replace state={{ from: location.pathname + location.search }} />
  }
  return <Outlet />
}
