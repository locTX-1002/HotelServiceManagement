import { BrowserRouter, Navigate, Route, Routes } from 'react-router-dom'
import { ToastProvider } from './components/ToastHost'
import MainLayout from './layouts/MainLayout'
import HomePage from './pages/HomePage'
import LoginPage from './pages/LoginPage'
import Placeholder from './pages/Placeholder'
import RoomMapPage from './pages/RoomMapPage'
import CreateReservationPage from './pages/CreateReservationPage'
import DashboardPage from './pages/DashboardPage'
import ReportsPage from './pages/ReportsPage'
import ReservationsPage from './pages/ReservationsPage'
import RoomPage from './pages/RoomPage'
import RoomTypePage from './pages/RoomTypePage'
import ProtectedRoute from './routes/ProtectedRoute'
import RequireRole from './routes/RequireRole'

export default function App() {
  return (
    <ToastProvider>
      <BrowserRouter>
        <Routes>
        <Route path="/" element={<HomePage />} />
        <Route path="/login" element={<LoginPage />} />
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
              <Route path="/checkin-checkout" element={<Placeholder title="Check-in / Check-out" />} />
              <Route path="/service-orders" element={<Placeholder title="Dịch vụ" />} />
              <Route path="/reports" element={<ReportsPage />} />
            </Route>
          </Route>
        </Route>
        <Route path="*" element={<Navigate to="/" replace />} />
        </Routes>
      </BrowserRouter>
    </ToastProvider>
  )
}
