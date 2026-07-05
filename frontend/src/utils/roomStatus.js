// Bảng màu 5 trạng thái phòng - tông pastel mềm, dùng chung toàn dự án
export const ROOM_STATUS = {
  Available: {
    label: 'Trống',
    dot: 'bg-emerald-500',
    badge: 'bg-emerald-50 text-emerald-700 ring-1 ring-emerald-600/15',
    strip: 'bg-emerald-400',
  },
  Reserved: {
    label: 'Đã đặt',
    dot: 'bg-sky-500',
    badge: 'bg-sky-50 text-sky-700 ring-1 ring-sky-600/15',
    strip: 'bg-sky-400',
  },
  Occupied: {
    label: 'Đang ở',
    dot: 'bg-rose-500',
    badge: 'bg-rose-50 text-rose-700 ring-1 ring-rose-600/15',
    strip: 'bg-rose-400',
  },
  Cleaning: {
    label: 'Dọn phòng',
    dot: 'bg-amber-500',
    badge: 'bg-amber-50 text-amber-700 ring-1 ring-amber-600/15',
    strip: 'bg-amber-400',
  },
  Maintenance: {
    label: 'Bảo trì',
    dot: 'bg-stone-400',
    badge: 'bg-stone-100 text-stone-600 ring-1 ring-stone-500/15',
    strip: 'bg-stone-400',
  },
}

export const formatVnd = (amount) =>
  new Intl.NumberFormat('vi-VN', { style: 'currency', currency: 'VND', maximumFractionDigits: 0 }).format(amount ?? 0)
