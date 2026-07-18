// Chuẩn hoá response backend về đúng shape FE dùng (theo API_DOCS).
// Backend hiện trả field lệch tên/kiểu -> gom việc map về 1 chỗ, các trang không phải biết.
// Các phép map đều phòng thủ (??): nếu sau này backend sửa cho khớp, code vẫn chạy.

// Thứ tự PHẢI khớp enum RoomStatus của backend: Available=0, Reserved=1, Occupied=2, Cleaning=3, Maintenance=4
const ROOM_STATUS_ORDER = ['Available', 'Reserved', 'Occupied', 'Cleaning', 'Maintenance']

// Backend serialize enum thành SỐ (thiếu JsonStringEnumConverter) -> đổi về chuỗi trạng thái.
// Nếu đã là chuỗi thì giữ nguyên (phòng khi backend bật enum-as-string sau này).
export const normalizeStatus = (s) =>
  typeof s === 'number' ? ROOM_STATUS_ORDER[s] ?? 'Maintenance' : s

// Chiều ngược khi FE GỬI lên: backend nhận enum dạng SỐ (chuỗi bị 400 khi thiếu converter).
// Gửi số an toàn cả 2 chiều - nếu sau này backend bật converter, số vẫn hợp lệ.
export const denormalizeStatus = (s) => {
  const i = ROOM_STATUS_ORDER.indexOf(s)
  return i >= 0 ? i : 0
}

// GET /api/room-types: backend trả { id, ... } -> FE cần roomTypeId
export const normalizeRoomType = (t) => ({ ...t, roomTypeId: t.roomTypeId ?? t.id })

// GET /api/rooms và /api/rooms/map: backend trả { id, roomTypeName, status: số }
// -> FE cần { roomId, typeName, status: chuỗi }
export const normalizeRoom = (r) => ({
  ...r,
  roomId: r.roomId ?? r.id,
  typeName: r.typeName ?? r.roomTypeName,
  status: normalizeStatus(r.status),
})

// GET /api/reservations/available-rooms: giống room nhưng không có status
export const normalizeAvailableRoom = (r) => ({
  ...r,
  roomId: r.roomId ?? r.id,
  typeName: r.typeName ?? r.roomTypeName,
})

// Thứ tự PHẢI khớp enum ReservationStatus của backend: Pending=0, Confirmed=1, Cancelled=2, CheckedIn=3, Completed=4, NoShow=5
// NoShow PHẢI ở cuối - chèn giữa sẽ làm lệch số của mọi trạng thái phía sau.
const RESERVATION_STATUS_ORDER = ['Pending', 'Confirmed', 'Cancelled', 'CheckedIn', 'Completed', 'NoShow']

export const normalizeReservationStatus = (s) =>
  typeof s === 'number' ? RESERVATION_STATUS_ORDER[s] ?? 'Pending' : s

// Chiều FE GỬI lên (PUT /api/reservations/{id}): backend nhận enum dạng SỐ
export const denormalizeReservationStatus = (s) => {
  const i = RESERVATION_STATUS_ORDER.indexOf(s)
  return i >= 0 ? i : 0
}

// GET /api/reservations: backend trả { id, roomTypeName, status: số } -> FE cần { reservationId, typeName, status: chuỗi }
export const normalizeReservation = (r) => ({
  ...r,
  reservationId: r.reservationId ?? r.id,
  typeName: r.typeName ?? r.roomTypeName,
  status: normalizeReservationStatus(r.status),
})

// Thứ tự PHẢI khớp enum ServiceOrderStatus của backend: Pending=0, Processing=1, Completed=2, Cancelled=3
const SERVICE_ORDER_STATUS_ORDER = ['Pending', 'Processing', 'Completed', 'Cancelled']

export const normalizeServiceOrderStatus = (s) =>
  typeof s === 'number' ? SERVICE_ORDER_STATUS_ORDER[s] ?? 'Pending' : s

export const denormalizeServiceOrderStatus = (s) => {
  const i = SERVICE_ORDER_STATUS_ORDER.indexOf(s)
  return i >= 0 ? i : 0
}

// GET /api/service-orders: status là số -> chuẩn hoá về chuỗi, giữ nguyên shape còn lại
export const normalizeServiceOrder = (o) => ({ ...o, status: normalizeServiceOrderStatus(o.status) })

// Thứ tự PHẢI khớp enum GuestTag của backend: None=0, Vip=1, Blacklisted=2
const GUEST_TAG_ORDER = ['None', 'Vip', 'Blacklisted']

export const normalizeGuestTag = (t) =>
  typeof t === 'number' ? GUEST_TAG_ORDER[t] ?? 'None' : t

export const denormalizeGuestTag = (t) => {
  const i = GUEST_TAG_ORDER.indexOf(t)
  return i >= 0 ? i : 0
}

// GET /api/guests: tag là số -> chuẩn hoá về chuỗi, giữ nguyên shape còn lại
export const normalizeGuest = (g) => ({ ...g, tag: normalizeGuestTag(g.tag) })

// Thứ tự PHẢI khớp enum HousekeepingRequestStatus của backend: Pending=0, Acknowledged=1, Completed=2
const HOUSEKEEPING_STATUS_ORDER = ['Pending', 'Acknowledged', 'Completed']

export const normalizeHousekeepingStatus = (s) =>
  typeof s === 'number' ? HOUSEKEEPING_STATUS_ORDER[s] ?? 'Pending' : s
