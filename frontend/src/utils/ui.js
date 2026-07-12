// Hằng dùng chung cho giao diện - gom từ các trang để không lặp lại mỗi file.
export const EASE = 'transition-all duration-300 ease-[cubic-bezier(0.32,0.72,0,1)]'

// Class ô nhập + nhãn dùng chung cho các form quản trị (phòng, loại phòng, khách, dịch vụ, người dùng...)
export const inputCls =
  'w-full rounded-xl bg-white px-3.5 py-2.5 text-sm ring-1 ring-black/10 outline-none placeholder:text-ink-500/50 focus:ring-2 focus:ring-brand-500/40'
export const labelCls = 'mb-1.5 block text-[12px] font-semibold text-ink-700'

// Khung báo lỗi vàng trong form
export const errorCls = 'rounded-lg bg-amber-50 px-3.5 py-2.5 text-[12px] font-medium text-amber-800 ring-1 ring-amber-600/15'

// 'Nguyễn Văn An' -> 'NA' cho avatar; tên 1 chữ thì lấy 2 ký tự đầu, không tên thì 'NV'
export const initials = (name) => {
  const p = (name ?? '').trim().split(/\s+/).filter(Boolean)
  if (p.length === 0) return 'NV'
  if (p.length === 1) return p[0].slice(0, 2).toUpperCase()
  return (p[0][0] + p[p.length - 1][0]).toUpperCase()
}
