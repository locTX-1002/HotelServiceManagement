// Ảnh theo loại phòng - file local trong public/img để demo không cần mạng
const IMAGES = {
  Standard: '/img/standard.jpg',
  Deluxe: '/img/deluxe.jpg',
  Suite: '/img/suite.jpg',
  'Family Room': '/img/family.jpg',
}

export const roomImage = (typeName) => IMAGES[typeName] ?? '/img/standard.jpg'
