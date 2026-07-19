// Biểu đồ cột SVG thuần, không phụ thuộc thư viện ngoài - đủ cho doanh thu theo ngày (vertical, nhiều điểm)
// và công suất theo tầng (horizontal, ít điểm). Tooltip dùng <title> gốc của SVG, không cần JS định vị.
export default function BarChart({ data, color = 'var(--color-brand-600)', formatValue = String, orientation = 'vertical', height = 200, emptyText = 'Chưa có dữ liệu' }) {
  const max = Math.max(...data.map((d) => d.value), 0)

  // Không có điểm dữ liệu nào mới coi là "chưa có dữ liệu" - toàn số 0 (VD: doanh thu 0đ cả khoảng ngày)
  // vẫn là dữ liệu thật, phải vẽ biểu đồ (các cột phẳng) chứ không được báo nhầm là chưa có gì.
  if (data.length === 0) {
    return <p className="py-8 text-center text-[13px] italic text-ink-500">{emptyText}</p>
  }
  const ratio = (value) => (max === 0 ? 0 : value / max)

  if (orientation === 'horizontal') {
    return (
      <div className="space-y-2.5">
        {data.map((d) => (
          <div key={d.label} className="flex items-center gap-2.5 text-[12px]">
            <span className="w-16 shrink-0 font-semibold text-ink-700">{d.label}</span>
            <div className="h-2.5 flex-1 overflow-hidden rounded-full bg-cream-200">
              <div
                className="h-full rounded-full transition-all duration-300"
                style={{ width: `${ratio(d.value) * 100}%`, backgroundColor: color }}
                title={formatValue(d.value)}
              />
            </div>
            <span className="w-14 shrink-0 text-right font-semibold tabular-nums text-ink-700">{formatValue(d.value)}</span>
          </div>
        ))}
      </div>
    )
  }

  // HTML/CSS thuần thay vì SVG - SVG với viewBox 0-100 + preserveAspectRatio="none" kéo giãn text
  // không đều theo trục X khi container thực rộng hơn nhiều so với 100 đơn vị viewBox, làm nhãn ngày
  // chồng chéo lên nhau khi có nhiều cột (VD 7-30 ngày).
  //
  // Cột phải cao theo % nên MỌI cấp trên nó bắt buộc có chiều cao xác định, nếu không % không có gì để
  // quy chiếu -> trình duyệt tính ra auto -> div rỗng cao 0px và biểu đồ mất hình (đúng lỗi đã gặp).
  // Vì vậy: cột con dùng h-full, rồi bọc thanh trong 1 rãnh flex-1 relative và đặt thanh absolute
  // bottom-0 - lúc này % ăn theo chiều cao thật của rãnh, không phụ thuộc nhãn dài ngắn.
  //
  // Bọc overflow-x-auto: flex item KHÔNG tự xuống dòng khi chật (muốn vậy phải có flex-wrap), 30 ngày
  // trở lên là tràn ngang cả trang. Cho cuộn ngang trong khung, giống bảng công suất theo tầng bên dưới.
  return (
    <div className="overflow-x-auto">
      <div className="flex min-w-full items-end gap-2 sm:gap-3" style={{ height }}>
        {data.map((d) => (
          <div key={d.label} className="group flex h-full min-w-9 flex-1 flex-col items-center gap-1.5">
            <p className="whitespace-nowrap text-[10px] tabular-nums text-ink-500 opacity-0 transition-opacity duration-200 group-hover:opacity-100">
              {formatValue(d.value)}
            </p>
            <div className="relative w-full flex-1">
              <div
                className="absolute inset-x-0 bottom-0 mx-auto w-full max-w-12 rounded-t-md transition-all duration-300"
                style={{ height: `${Math.max(ratio(d.value) * 100, 6)}%`, backgroundColor: color }}
                title={`${d.label}: ${formatValue(d.value)}`}
              />
            </div>
            <p className="whitespace-nowrap text-center text-[10px] leading-tight text-ink-500">{d.label}</p>
          </div>
        ))}
      </div>
    </div>
  )
}
