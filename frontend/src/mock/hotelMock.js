// Dữ liệu mẫu theo đúng contract API - xóa dần khi backend hoàn thành
// GET /api/rooms/map -> [{ floor, rooms: [{ roomId, roomNumber, typeName, basePrice, status, guestName? }] }]
export const MOCK_ROOM_MAP = [
  {
    floor: 1,
    rooms: [
      { roomId: 1, roomNumber: '101', typeName: 'Standard', basePrice: 500000, status: 'Available' },
      { roomId: 2, roomNumber: '102', typeName: 'Standard', basePrice: 500000, status: 'Occupied', guestName: 'Nguyễn Văn An' },
      { roomId: 3, roomNumber: '103', typeName: 'Deluxe', basePrice: 800000, status: 'Cleaning' },
      { roomId: 4, roomNumber: '104', typeName: 'Standard', basePrice: 500000, status: 'Reserved', guestName: 'Trần Thị Bích' },
    ],
  },
  {
    floor: 2,
    rooms: [
      { roomId: 5, roomNumber: '201', typeName: 'Deluxe', basePrice: 800000, status: 'Available' },
      { roomId: 6, roomNumber: '202', typeName: 'Suite', basePrice: 1200000, status: 'Maintenance' },
      { roomId: 7, roomNumber: '203', typeName: 'Deluxe', basePrice: 800000, status: 'Occupied', guestName: 'Lê Hoàng Cường' },
    ],
  },
  {
    floor: 3,
    rooms: [
      { roomId: 8, roomNumber: '301', typeName: 'Family Room', basePrice: 1500000, status: 'Available' },
      { roomId: 9, roomNumber: '302', typeName: 'Suite', basePrice: 1200000, status: 'Reserved', guestName: 'Phạm Minh Dũng' },
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
