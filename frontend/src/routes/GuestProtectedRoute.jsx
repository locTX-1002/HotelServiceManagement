import { Navigate, Outlet, useLocation } from 'react-router-dom'
import { getGuestToken } from '../utils/guestSession'

// Bản sao của ProtectedRoute.jsx nhưng cho phiên khách (guestToken riêng) - không dùng chung vì
// ProtectedRoute kiểm tra token nhân viên, sẽ đá khách chưa đăng nhập nhân viên ra /login sai chỗ.
export default function GuestProtectedRoute() {
  const location = useLocation()

  if (!getGuestToken()) {
    return <Navigate to="/guest/dang-nhap" replace state={{ from: location.pathname + location.search }} />
  }
  return <Outlet />
}
