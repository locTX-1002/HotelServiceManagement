// Thông tin hiển thị theo loại phòng - kiểu booking engine (sức chứa, diện tích, giường, tiện nghi)
export const ROOM_META = {
  Standard: { capacity: 2, area: 22, bed: '1 giường đôi', amenities: ['Wifi miễn phí', 'Điều hòa', 'TV màn phẳng'] },
  Deluxe: { capacity: 2, area: 28, bed: '1 giường lớn', amenities: ['Wifi miễn phí', 'Điều hòa', 'Minibar', 'View thành phố'] },
  Suite: { capacity: 4, area: 45, bed: '1 giường lớn + sofa bed', amenities: ['Wifi miễn phí', 'Điều hòa', 'Minibar', 'Bồn tắm', 'Phòng khách riêng'] },
  'Family Room': { capacity: 6, area: 56, bed: '2 giường lớn', amenities: ['Wifi miễn phí', 'Điều hòa', 'Minibar', 'Góc trẻ em', 'Ban công'] },
}

export const roomMeta = (typeName) => ROOM_META[typeName] ?? ROOM_META.Standard
