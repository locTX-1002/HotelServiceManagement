// Bảng màu 5 trạng thái phòng - dùng chung toàn dự án (docs/routes-colors.md)
export const ROOM_STATUS = {
  Available: { label: 'Available', color: 'bg-green-500', text: 'text-green-700', badge: 'bg-green-100 text-green-700' },
  Reserved: { label: 'Reserved', color: 'bg-blue-500', text: 'text-blue-700', badge: 'bg-blue-100 text-blue-700' },
  Occupied: { label: 'Occupied', color: 'bg-red-500', text: 'text-red-700', badge: 'bg-red-100 text-red-700' },
  Cleaning: { label: 'Cleaning', color: 'bg-orange-500', text: 'text-orange-700', badge: 'bg-orange-100 text-orange-700' },
  Maintenance: { label: 'Maintenance', color: 'bg-gray-500', text: 'text-gray-700', badge: 'bg-gray-200 text-gray-700' },
}

export const formatVnd = (amount) =>
  new Intl.NumberFormat('vi-VN', { style: 'currency', currency: 'VND', maximumFractionDigits: 0 }).format(amount ?? 0)
