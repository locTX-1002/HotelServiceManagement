// Dữ liệu mẫu theo đúng contract API - xóa dần khi backend hoàn thành
// GET /api/rooms/map -> [{ floor, rooms: [{ roomId, roomNumber, typeName, basePrice, status, guestName? }] }]
export const MOCK_ROOM_MAP = [
  {
    floor: 1,
    rooms: [
      { roomId: 1, roomNumber: '101', typeName: 'Standard', basePrice: 500000, status: 'Available' },
      { roomId: 2, roomNumber: '102', typeName: 'Standard', basePrice: 500000, status: 'Occupied', guestName: 'Nguyễn Văn An', checkOutAt: '12:00 mai' },
      { roomId: 3, roomNumber: '103', typeName: 'Deluxe', basePrice: 800000, status: 'Cleaning', cleaningEta: '25 phút' },
      { roomId: 4, roomNumber: '104', typeName: 'Standard', basePrice: 500000, status: 'Reserved', guestName: 'Trần Thị Bích', eta: '14:00' },
    ],
  },
  {
    floor: 2,
    rooms: [
      { roomId: 5, roomNumber: '201', typeName: 'Deluxe', basePrice: 800000, status: 'Available' },
      { roomId: 6, roomNumber: '202', typeName: 'Suite', basePrice: 1200000, status: 'Maintenance' },
      { roomId: 7, roomNumber: '203', typeName: 'Deluxe', basePrice: 800000, status: 'Occupied', guestName: 'Lê Hoàng Cường', checkOutAt: '09:30 mai' },
    ],
  },
  {
    floor: 3,
    rooms: [
      { roomId: 8, roomNumber: '301', typeName: 'Family Room', basePrice: 1500000, status: 'Available' },
      { roomId: 9, roomNumber: '302', typeName: 'Suite', basePrice: 1200000, status: 'Reserved', guestName: 'Phạm Minh Dũng', eta: '15:30' },
    ],
  },
]

export const MOCK_ROOM_TYPES = ['Standard', 'Deluxe', 'Suite', 'Family Room']

// GET /api/reservations/available-rooms -> [{ roomId, roomNumber, typeName, floor, basePrice }]
export const MOCK_AVAILABLE_ROOMS = [
  { roomId: 1, roomNumber: '101', typeName: 'Standard', floor: 1, basePrice: 500000 },
  { roomId: 5, roomNumber: '201', typeName: 'Deluxe', floor: 2, basePrice: 800000 },
  { roomId: 8, roomNumber: '301', typeName: 'Family Room', floor: 3, basePrice: 1500000 },
]

// GET /api/reports/dashboard + dữ liệu vận hành trong ngày cho trang Tổng quan
export const MOCK_DASHBOARD = {
  totalRooms: 9,
  availableRooms: 3,
  occupiedRooms: 2,
  todayBookings: 3,
  todayRevenue: 3280000,
  revenueDeltaPct: 12, // so với hôm qua
  alerts: [
    { id: 1, text: 'Phòng 102 quá giờ check-out 25 phút', action: 'Check-out', to: '/checkin-checkout' },
    { id: 2, text: 'Phòng 103 dọn lâu hơn dự kiến (35 phút)', action: 'Xem phòng', to: '/rooms/map' },
    { id: 3, text: 'Booking BK202607-0014 chưa xác nhận', action: 'Xem booking', to: '/reservations' },
  ],
  arrivals: [
    { bookingCode: 'BK202607-0012', guestName: 'Trần Thị Bích', roomNumber: '104', typeName: 'Standard', eta: '14:00' },
    { bookingCode: 'BK202607-0014', guestName: 'Phạm Minh Dũng', roomNumber: '302', typeName: 'Suite', eta: '15:30' },
  ],
  departures: [
    { bookingCode: 'BK202607-0008', guestName: 'Nguyễn Văn An', roomNumber: '102', typeName: 'Standard', nights: 2, amountDue: 1180000 },
    { bookingCode: 'BK202607-0009', guestName: 'Lê Hoàng Cường', roomNumber: '203', typeName: 'Deluxe', nights: 1, amountDue: 830000 },
  ],
  revenue7d: [
    { day: 'T3', amount: 1650000 },
    { day: 'T4', amount: 2470000 },
    { day: 'T5', amount: 1980000 },
    { day: 'T6', amount: 3740000 },
    { day: 'T7', amount: 4120000 },
    { day: 'CN', amount: 2890000 },
    { day: 'T2', amount: 3280000 },
  ],
}
