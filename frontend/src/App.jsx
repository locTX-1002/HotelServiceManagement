import { BrowserRouter, Navigate, Route, Routes } from 'react-router-dom'
import MainLayout from './layouts/MainLayout'
import HomePage from './pages/HomePage'
import LoginPage from './pages/LoginPage'
import Placeholder from './pages/Placeholder'
import RoomMapPage from './pages/RoomMapPage'
import CreateReservationPage from './pages/CreateReservationPage'

export default function App() {
  return (
    <BrowserRouter>
      <Routes>
        <Route path="/" element={<HomePage />} />
        <Route path="/login" element={<LoginPage />} />
        <Route element={<MainLayout />}>
          <Route path="/dashboard" element={<Placeholder title="Tổng quan" owner="Phúc" day="T3 07/07" />} />
          <Route path="/rooms/map" element={<RoomMapPage />} />
          <Route path="/rooms" element={<Placeholder title="Phòng & loại phòng" owner="Phúc" day="T3 07/07" />} />
          <Route path="/reservations" element={<Placeholder title="Danh sách đặt phòng" owner="Lộc" day="T3-T4" />} />
          <Route path="/reservations/new" element={<CreateReservationPage />} />
          <Route path="/checkin-checkout" element={<Placeholder title="Check-in / Check-out" owner="Lộc" day="T4 08/07" />} />
          <Route path="/service-orders" element={<Placeholder title="Dịch vụ" owner="Lộc" day="T5 09/07" />} />
          <Route path="/reports" element={<Placeholder title="Báo cáo" owner="Phúc" day="T5 09/07" />} />
        </Route>
        <Route path="*" element={<Navigate to="/" replace />} />
      </Routes>
    </BrowserRouter>
  )
}
