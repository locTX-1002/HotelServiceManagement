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

// Số ngày trong dải [from..to] tính cả 2 đầu
const daysBetween = (from, to) => {
  const [y1, m1, d1] = from.split('-').map(Number)
  const [y2, m2, d2] = to.split('-').map(Number)
  return Math.max(Math.round((Date.UTC(y2, m2 - 1, d2) - Date.UTC(y1, m1 - 1, d1)) / 86400000) + 1, 1)
}

// GET /api/reports/revenue?fromDate=&toDate= -> { fromDate, toDate, roomRevenue, serviceRevenue, paymentRevenue, totalRevenue }
// Tổng hợp cả kỳ, scale theo số ngày cho có cảm giác thật.
export const mockRevenueSummary = (from, to) => {
  const n = daysBetween(from, to)
  const roomRevenue = 1_650_000 * n
  const serviceRevenue = 240_000 * n
  const total = roomRevenue + serviceRevenue
  return { fromDate: from, toDate: to, roomRevenue, serviceRevenue, paymentRevenue: total, totalRevenue: total }
}

// GET /api/reports/occupancy -> { totalRooms, occupiedRooms, reservedRooms, occupancyRate, byFloor: [...] }
// Suy từ MOCK_ROOM_MAP để khớp sơ đồ phòng.
export const mockOccupancySnapshot = () => {
  const byFloor = MOCK_ROOM_MAP.map((f) => {
    const totalRooms = f.rooms.length
    const occupiedRooms = f.rooms.filter((r) => r.status === 'Occupied').length
    const reservedRooms = f.rooms.filter((r) => r.status === 'Reserved').length
    const occupancyRate = totalRooms ? Math.round(((occupiedRooms + reservedRooms) / totalRooms) * 100) : 0
    return { floor: f.floor, totalRooms, occupiedRooms, reservedRooms, occupancyRate }
  })
  const totalRooms = byFloor.reduce((s, f) => s + f.totalRooms, 0)
  const occupiedRooms = byFloor.reduce((s, f) => s + f.occupiedRooms, 0)
  const reservedRooms = byFloor.reduce((s, f) => s + f.reservedRooms, 0)
  const occupancyRate = totalRooms ? Math.round(((occupiedRooms + reservedRooms) / totalRooms) * 100) : 0
  return { totalRooms, occupiedRooms, reservedRooms, occupancyRate, byFloor }
}

// GET /api/reservations -> [{ id, bookingCode, guestName, guestPhoneNumber, roomNumber, roomTypeName, checkInDate, checkOutDate, status }]
// status là SỐ theo enum backend: Pending=0, Confirmed=1, Cancelled=2, CheckedIn=3, Completed=4
export const MOCK_RESERVATIONS = [
  { id: 1, bookingCode: 'BK202607-0012', guestName: 'Trần Thị Bích', guestPhoneNumber: '0905123456', roomNumber: '104', roomTypeName: 'Standard', checkInDate: '2026-07-09', checkOutDate: '2026-07-11', status: 1 },
  { id: 2, bookingCode: 'BK202607-0014', guestName: 'Phạm Minh Dũng', guestPhoneNumber: '0912987654', roomNumber: '302', roomTypeName: 'Suite', checkInDate: '2026-07-09', checkOutDate: '2026-07-12', status: 0 },
  { id: 3, bookingCode: 'BK202607-0008', guestName: 'Nguyễn Văn An', guestPhoneNumber: '0987654321', roomNumber: '102', roomTypeName: 'Standard', checkInDate: '2026-07-07', checkOutDate: '2026-07-09', status: 3 },
  { id: 4, bookingCode: 'BK202607-0009', guestName: 'Lê Hoàng Cường', guestPhoneNumber: '0934567890', roomNumber: '203', roomTypeName: 'Deluxe', checkInDate: '2026-07-08', checkOutDate: '2026-07-09', status: 3 },
  { id: 5, bookingCode: 'BK202607-0005', guestName: 'Võ Thị Hoa', guestPhoneNumber: '0978112233', roomNumber: '201', roomTypeName: 'Deluxe', checkInDate: '2026-07-02', checkOutDate: '2026-07-05', status: 4 },
  { id: 6, bookingCode: 'BK202607-0003', guestName: 'Đặng Quốc Bảo', guestPhoneNumber: '0901445566', roomNumber: '301', roomTypeName: 'Family Room', checkInDate: '2026-07-01', checkOutDate: '2026-07-03', status: 2 },
]

// GET /api/stays/active -> [{ stayId, reservationId, bookingCode, guestName, roomNumber, actualCheckIn, plannedCheckOut, status }]
// Khớp 2 đặt phòng đã CheckedIn (status=3) ở trên - phòng 102 và 203.
export const MOCK_ACTIVE_STAYS = [
  { stayId: 1, reservationId: 3, bookingCode: 'BK202607-0008', guestName: 'Nguyễn Văn An', roomNumber: '102', actualCheckIn: '2026-07-07T14:10:00', plannedCheckOut: '2026-07-09T12:00:00', status: 'Active' },
  { stayId: 2, reservationId: 4, bookingCode: 'BK202607-0009', guestName: 'Lê Hoàng Cường', roomNumber: '203', actualCheckIn: '2026-07-08T13:40:00', plannedCheckOut: '2026-07-09T12:00:00', status: 'Active' },
]

// GET /api/invoices/stay/{stayId} -> { invoiceId, stayId, invoiceDate, roomCharge, serviceCharge, totalAmount, status }
// status đã là CHUỖI thật từ backend (invoice.Status.ToString()), không phải số như các enum khác.
export const MOCK_INVOICE = {
  invoiceId: 1,
  stayId: 1,
  invoiceDate: '2026-07-09T12:00:00',
  roomCharge: 200000,
  serviceCharge: 95000,
  totalAmount: 295000,
  status: 'Unpaid',
}

// GET /api/service-categories -> [{ id, categoryName, isActive }]
export const MOCK_SERVICE_CATEGORIES = [
  { id: 1, categoryName: 'Ăn uống', isActive: true },
  { id: 2, categoryName: 'Giặt ủi', isActive: true },
  { id: 3, categoryName: 'Spa & Giải trí', isActive: true },
]

// GET /api/service-items -> [{ id, serviceCategoryId, categoryName, serviceName, unitPrice, isAvailable }]
export const MOCK_SERVICE_ITEMS = [
  { id: 1, serviceCategoryId: 1, categoryName: 'Ăn uống', serviceName: 'Bữa sáng buffet', unitPrice: 80000, isAvailable: true },
  { id: 2, serviceCategoryId: 1, categoryName: 'Ăn uống', serviceName: 'Nước suối', unitPrice: 15000, isAvailable: true },
  { id: 3, serviceCategoryId: 1, categoryName: 'Ăn uống', serviceName: 'Cà phê', unitPrice: 25000, isAvailable: true },
  { id: 4, serviceCategoryId: 2, categoryName: 'Giặt ủi', serviceName: 'Giặt áo sơ mi', unitPrice: 20000, isAvailable: true },
  { id: 5, serviceCategoryId: 2, categoryName: 'Giặt ủi', serviceName: 'Giặt quần', unitPrice: 25000, isAvailable: true },
  { id: 6, serviceCategoryId: 3, categoryName: 'Spa & Giải trí', serviceName: 'Massage 60 phút', unitPrice: 350000, isAvailable: true },
]

// GET /api/service-orders -> [{ id, stayId, orderDate, status, totalAmount, details: [...] }]
// status là SỐ theo enum backend: Pending=0, Processing=1, Completed=2, Cancelled=3
// stayId=1 khớp MOCK_ACTIVE_STAYS ở trên (phòng 102 - Nguyễn Văn An)
export const MOCK_SERVICE_ORDERS = [
  {
    id: 1,
    stayId: 1,
    orderDate: '2026-07-08T09:00:00',
    status: 0,
    totalAmount: 95000,
    details: [
      { id: 1, serviceItemId: 1, serviceName: 'Bữa sáng buffet', quantity: 1, unitPrice: 80000, subtotal: 80000 },
      { id: 2, serviceItemId: 2, serviceName: 'Nước suối', quantity: 1, unitPrice: 15000, subtotal: 15000 },
    ],
  },
]

// GET /api/users -> [{ id, fullName, email, isActive, roleId, role }] (chỉ Admin)
export const MOCK_USERS = [
  { id: 1, fullName: 'Admin User', email: 'admin@hotel.com', isActive: true, roleId: 1, role: 'Admin' },
  { id: 2, fullName: 'Manager User', email: 'manager@hotel.com', isActive: true, roleId: 2, role: 'Manager' },
  { id: 3, fullName: 'Receptionist User', email: 'receptionist@hotel.com', isActive: true, roleId: 3, role: 'Receptionist' },
  { id: 4, fullName: 'Service Staff', email: 'service@hotel.com', isActive: false, roleId: 4, role: 'ServiceStaff' },
]

// GET /api/guests -> [{ id, fullName, email?, phoneNumber, identityNumber, reservationCount }]
export const MOCK_GUESTS = [
  { id: 1, fullName: 'Nguyễn Văn An', email: 'an.nguyen@gmail.com', phoneNumber: '0901234567', identityNumber: '079203001234', reservationCount: 2 },
  { id: 2, fullName: 'Trần Thị Bích', email: null, phoneNumber: '0912345678', identityNumber: '079203005678', reservationCount: 1 },
  { id: 3, fullName: 'Lê Hoàng Cường', email: 'cuong.le@gmail.com', phoneNumber: '0923456789', identityNumber: '079203009012', reservationCount: 1 },
]

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
