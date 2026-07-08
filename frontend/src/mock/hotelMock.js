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

// GET /api/room-types -> [{ roomTypeId, typeName, capacity, basePrice, isActive }] (khớp seed backend)
export const MOCK_ROOM_TYPES_FULL = [
  { roomTypeId: 1, typeName: 'Standard', capacity: 2, basePrice: 500000, isActive: true },
  { roomTypeId: 2, typeName: 'Deluxe', capacity: 2, basePrice: 800000, isActive: true },
  { roomTypeId: 3, typeName: 'Suite', capacity: 4, basePrice: 1200000, isActive: true },
  { roomTypeId: 4, typeName: 'Family Room', capacity: 6, basePrice: 1500000, isActive: true },
]

export const MOCK_ROOM_TYPES = MOCK_ROOM_TYPES_FULL.map((t) => t.typeName)

// GET /api/rooms -> [{ roomId, roomNumber, floor, roomTypeId, typeName, basePrice, status, isActive }]
// Suy từ MOCK_ROOM_MAP để 2 trang không bao giờ lệch nhau
export const MOCK_ROOMS = MOCK_ROOM_MAP.flatMap((f) =>
  f.rooms.map((r) => ({
    roomId: r.roomId,
    roomNumber: r.roomNumber,
    floor: f.floor,
    roomTypeId: MOCK_ROOM_TYPES_FULL.find((t) => t.typeName === r.typeName)?.roomTypeId ?? 1,
    typeName: r.typeName,
    basePrice: r.basePrice,
    status: r.status,
    isActive: true,
  })),
)

// Báo cáo: sinh số liệu mẫu ỔN ĐỊNH theo ngày (cùng ngày luôn ra cùng số)
// cho dải ngày bất kỳ, theo shape trong API_DOCS.
const seedOf = (dateStr) => dateStr.split('-').reduce((acc, part) => (acc * 31 + Number(part)) % 997, 7)

// GET /api/reports/revenue?from=&to= -> [{ date, roomRevenue, serviceRevenue }]
export const mockRevenueRange = (days) =>
  days.map((date) => {
    const s = seedOf(date)
    return { date, roomRevenue: 1200000 + (s % 7) * 450000, serviceRevenue: 150000 + (s % 5) * 120000 }
  })

// GET /api/reports/occupancy?from=&to= -> [{ date, occupiedRooms, totalRooms }]
export const mockOccupancyRange = (days) =>
  days.map((date) => {
    const s = seedOf(date)
    return { date, occupiedRooms: 3 + (s % 6), totalRooms: 9 }
  })

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
