// Ảnh theo loại phòng, có biến thể theo roomId để phòng cùng loại không trùng ảnh.
// File local trong public/img - demo không cần mạng.
// Vị trí 0 là ảnh đặc trưng của loại phòng, các vị trí sau dùng ảnh biến thể chung.
// Truyền index = vị trí trong danh sách (không phải roomId) để 1 hàng không bao giờ trùng ảnh.
const POOL = {
  Standard: ['/img/standard.jpg', '/img/v1.jpg', '/img/v2.jpg', '/img/v3.jpg'],
  Deluxe: ['/img/deluxe.jpg', '/img/v1.jpg', '/img/v2.jpg', '/img/v3.jpg'],
  Suite: ['/img/suite.jpg', '/img/v1.jpg', '/img/v2.jpg', '/img/v3.jpg'],
  'Family Room': ['/img/family.jpg', '/img/v1.jpg', '/img/v2.jpg', '/img/v3.jpg'],
}

const POSITIONS = ['object-center', 'object-top', 'object-bottom']

export const roomImage = (typeName, index = 0) => {
  const pool = POOL[typeName] ?? POOL.Standard
  return pool[index % pool.length]
}

export const roomImagePosition = (index = 0) => POSITIONS[index % POSITIONS.length]
