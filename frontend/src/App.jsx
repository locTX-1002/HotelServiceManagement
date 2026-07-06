import { BrowserRouter, Navigate, Route, Routes } from 'react-router-dom'
import MainLayout from './layouts/MainLayout'
import HomePage from './pages/HomePage'
import LoginPage from './pages/LoginPage'
import Placeholder from './pages/Placeholder'
import RoomMapPage from './pages/RoomMapPage'
import CreateReservationPage from './pages/CreateReservationPage'
import DashboardPage from './pages/DashboardPage'
import ReportsPage from './pages/ReportsPage'
import RoomPage from './pages/RoomPage'
import RoomTypePage from './pages/RoomTypePage'
import ProtectedRoute from './routes/ProtectedRoute'

export default function App() {
  return (
    <BrowserRouter>
      <Routes>
        <Route path="/" element={<HomePage />} />
        <Route path="/login" element={<LoginPage />} />
        {/* Khu nghiệp vụ: bắt buộc có phiên đăng nhập */}
        <Route element={<ProtectedRoute />}>
          <Route element={<MainLayout />}>
            <Route path="/dashboard" element={<DashboardPage />} />
            <Route path="/rooms/map" element={<RoomMapPage />} />
            <Route path="/rooms" element={<RoomPage />} />
            <Route path="/rooms/types" element={<RoomTypePage />} />
            <Route path="/reservations" element={<Placeholder title="Danh sách đặt phòng" owner="Lộc" day="T3-T4" />} />
            <Route path="/reservations/new" element={<CreateReservationPage />} />
            <Route path="/checkin-checkout" element={<Placeholder title="Check-in / Check-out" owner="Lộc" day="T4 08/07" />} />
            <Route path="/service-orders" element={<Placeholder title="Dịch vụ" owner="Lộc" day="T5 09/07" />} />
            <Route path="/reports" element={<ReportsPage />} />
          </Route>
        </Route>
        <Route path="*" element={<Navigate to="/" replace />} />
      </Routes>
    </BrowserRouter>
  )
}
