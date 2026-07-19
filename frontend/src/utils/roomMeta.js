// Thông tin hiển thị theo loại phòng - kiểu booking engine (sức chứa, diện tích, giường, tiện nghi,
// mô tả ngắn dùng cho tooltip hover + modal chi tiết)
export const ROOM_META = {
  Standard: {
    capacity: 2, area: 22, bed: '1 giường đôi',
    amenities: ['Wifi miễn phí', 'Điều hòa', 'TV màn phẳng'],
    desc: 'Phòng gọn cho 1–2 khách, cửa sổ hướng phố, đủ tiện nghi cơ bản cho chuyến đi ngắn.',
  },
  Deluxe: {
    capacity: 2, area: 28, bed: '1 giường lớn',
    amenities: ['Wifi miễn phí', 'Điều hòa', 'Minibar', 'View thành phố'],
    desc: 'Rộng hơn Standard với giường lớn, minibar và cửa sổ lớn nhìn ra thành phố.',
  },
  Suite: {
    capacity: 4, area: 45, bed: '1 giường lớn + sofa bed',
    amenities: ['Wifi miễn phí', 'Điều hòa', 'Minibar', 'Bồn tắm', 'Phòng khách riêng'],
    desc: 'Phòng khách riêng tách biệt khu ngủ, bồn tắm nằm — hợp kỳ nghỉ dài ngày hoặc đi cùng gia đình nhỏ.',
  },
  'Family Room': {
    capacity: 6, area: 56, bed: '2 giường lớn',
    amenities: ['Wifi miễn phí', 'Điều hòa', 'Minibar', 'Góc trẻ em', 'Ban công'],
    desc: '2 giường lớn, góc chơi trẻ em và ban công — thiết kế cho nhóm 4–6 người ở thoải mái.',
  },
}

export const roomMeta = (typeName) => ROOM_META[typeName] ?? ROOM_META.Standard
