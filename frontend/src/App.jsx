import { BrowserRouter, Navigate, Route, Routes } from 'react-router-dom'
import { ToastProvider } from './components/ToastHost'
import MainLayout from './layouts/MainLayout'
import HomePage from './pages/HomePage'
import LoginPage from './pages/LoginPage'
import ResetPasswordPage from './pages/ResetPasswordPage'
import RoomMapPage from './pages/RoomMapPage'
import CreateReservationPage from './pages/CreateReservationPage'
import CheckInOutPage from './pages/CheckInOutPage'
import DashboardPage from './pages/DashboardPage'
import GuestsPage from './pages/GuestsPage'
import ReportsPage from './pages/ReportsPage'
import ReservationsPage from './pages/ReservationsPage'
import RoomPage from './pages/RoomPage'
import RoomTypePage from './pages/RoomTypePage'
import ServiceCatalogPage from './pages/ServiceCatalogPage'
import ServiceOrderPage from './pages/ServiceOrderPage'
import SurchargeCatalogPage from './pages/SurchargeCatalogPage'
import PromotionCatalogPage from './pages/PromotionCatalogPage'
import InvoicePage from './pages/InvoicePage'
import UsersPage from './pages/UsersPage'
import ProtectedRoute from './routes/ProtectedRoute'
import RequireRole from './routes/RequireRole'
import GuestProtectedRoute from './routes/GuestProtectedRoute'
import GuestLayout from './layouts/GuestLayout'
import GuestLoginPage from './pages/guest/GuestLoginPage'
import GuestRegisterPage from './pages/guest/GuestRegisterPage'
import GuestResetPasswordPage from './pages/guest/GuestResetPasswordPage'
import GuestResetPasswordWithTokenPage from './pages/guest/GuestResetPasswordWithTokenPage'
import GuestHomePage from './pages/guest/GuestHomePage'
import GuestReservationsPage from './pages/guest/GuestReservationsPage'
import GuestProfilePage from './pages/guest/GuestProfilePage'
import GuestBookingPage from './pages/guest/GuestBookingPage'

export default function App() {
  return (
    <ToastProvider>
      <BrowserRouter>
        <Routes>
        <Route path="/" element={<HomePage />} />
        <Route path="/login" element={<LoginPage />} />
        <Route path="/reset-password" element={<ResetPasswordPage />} />
        {/* Cổng thông tin khách - tách hoàn toàn khỏi khu nhân viên bên dưới, session/API riêng */}
        <Route path="/guest/dang-nhap" element={<GuestLoginPage />} />
        <Route path="/guest/dang-ky" element={<GuestRegisterPage />} />
        <Route path="/guest/quen-mat-khau" element={<GuestResetPasswordPage />} />
        <Route path="/guest/dat-lai-mat-khau" element={<GuestResetPasswordWithTokenPage />} />
        <Route element={<GuestProtectedRoute />}>
          <Route element={<GuestLayout />}>
            <Route path="/guest/dashboard" element={<GuestHomePage />} />
            <Route path="/guest/dat-phong-cua-toi" element={<GuestReservationsPage />} />
            <Route path="/guest/dat-phong-moi" element={<GuestBookingPage />} />
            <Route path="/guest/ho-so" element={<GuestProfilePage />} />
          </Route>
        </Route>
        {/* Khu nghiệp vụ: bắt buộc có phiên đăng nhập + đúng quyền vai trò */}
        <Route element={<ProtectedRoute />}>
          <Route element={<MainLayout />}>
            <Route element={<RequireRole />}>
              <Route path="/dashboard" element={<DashboardPage />} />
              <Route path="/rooms/map" element={<RoomMapPage />} />
              <Route path="/rooms" element={<RoomPage />} />
              <Route path="/rooms/types" element={<RoomTypePage />} />
              <Route path="/reservations" element={<ReservationsPage />} />
              <Route path="/reservations/new" element={<CreateReservationPage />} />
              <Route path="/checkin-checkout" element={<CheckInOutPage />} />
              <Route path="/guests" element={<GuestsPage />} />
              <Route path="/service-orders" element={<ServiceOrderPage />} />
              <Route path="/service-items" element={<ServiceCatalogPage />} />
              <Route path="/surcharge-items" element={<SurchargeCatalogPage />} />
              <Route path="/invoices" element={<InvoicePage />} />
              <Route path="/promotions" element={<PromotionCatalogPage />} />
              <Route path="/reports" element={<ReportsPage />} />
              <Route path="/users" element={<UsersPage />} />
            </Route>
          </Route>
        </Route>
        <Route path="*" element={<Navigate to="/" replace />} />
        </Routes>
      </BrowserRouter>
    </ToastProvider>
  )
}
