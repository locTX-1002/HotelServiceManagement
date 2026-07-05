import { BrowserRouter, Navigate, Route, Routes } from 'react-router-dom'
import MainLayout from './layouts/MainLayout'
import LoginPage from './pages/LoginPage'
import Placeholder from './pages/Placeholder'

export default function App() {
  return (
    <BrowserRouter>
      <Routes>
        <Route path="/login" element={<LoginPage />} />
        <Route element={<MainLayout />}>
          <Route path="/dashboard" element={<Placeholder title="Dashboard" owner="Phúc" day="T2 06/07" />} />
          <Route path="/rooms/map" element={<Placeholder title="Visual Room Map" owner="Tú" day="T2 06/07" />} />
          <Route path="/rooms" element={<Placeholder title="Room / Room Type Management" owner="Khoa" day="T3 07/07" />} />
          <Route path="/reservations" element={<Placeholder title="Reservations" owner="Tú" day="T3 07/07" />} />
          <Route path="/checkin-checkout" element={<Placeholder title="Check-in / Check-out" owner="Khoa" day="T4 08/07" />} />
          <Route path="/service-orders" element={<Placeholder title="Service Orders" owner="Tú" day="T4 08/07" />} />
          <Route path="/reports" element={<Placeholder title="Reports" owner="Khoa" day="T5 09/07" />} />
        </Route>
        <Route path="*" element={<Navigate to="/login" replace />} />
      </Routes>
    </BrowserRouter>
  )
}
