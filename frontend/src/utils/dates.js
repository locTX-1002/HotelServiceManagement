// Ngày LOCAL (không dùng toISOString - trả UTC, sau 0h giờ VN sẽ lệch về hôm qua)
const pad = (n) => String(n).padStart(2, '0')

export const localToday = () => {
  const d = new Date()
  return `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}`
}

export const addDays = (dateStr, n) => {
  const [y, m, day] = dateStr.split('-').map(Number)
  const d = new Date(y, m - 1, day + n)
  return `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}`
}

// '2026-07-06' -> '06 thg 7' - dạng chữ, không thể nhầm dd/mm với mm/dd
export const fmtShort = (dateStr) => {
  const [y, m, day] = dateStr.split('-').map(Number)
  return new Intl.DateTimeFormat('vi-VN', { day: '2-digit', month: 'short' }).format(new Date(y, m - 1, day))
}

// Chuỗi datetime ISO thật từ backend (có giờ) -> '06 thg 7, 14:05' theo giờ trình duyệt.
// Khác fmtShort: đây nhận Date/instant thật (check-in, check-out), không phải ngày dạng chuỗi 'YYYY-MM-DD'.
export const fmtDateTime = (iso) =>
  new Intl.DateTimeFormat('vi-VN', { day: '2-digit', month: 'short', hour: '2-digit', minute: '2-digit' }).format(new Date(iso))
