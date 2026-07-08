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
