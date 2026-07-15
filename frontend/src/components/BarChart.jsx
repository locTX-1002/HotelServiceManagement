// Biểu đồ cột SVG thuần, không phụ thuộc thư viện ngoài - đủ cho doanh thu theo ngày (vertical, nhiều điểm)
// và công suất theo tầng (horizontal, ít điểm). Tooltip dùng <title> gốc của SVG, không cần JS định vị.
export default function BarChart({ data, color = 'var(--color-brand-600)', formatValue = String, orientation = 'vertical', height = 200, emptyText = 'Chưa có dữ liệu' }) {
  const max = Math.max(...data.map((d) => d.value), 0)

  if (data.length === 0 || max === 0) {
    return <p className="py-8 text-center text-[13px] italic text-ink-500">{emptyText}</p>
  }

  if (orientation === 'horizontal') {
    return (
      <div className="space-y-2.5">
        {data.map((d) => (
          <div key={d.label} className="flex items-center gap-2.5 text-[12px]">
            <span className="w-16 shrink-0 font-semibold text-ink-700">{d.label}</span>
            <div className="h-2.5 flex-1 overflow-hidden rounded-full bg-cream-200">
              <div
                className="h-full rounded-full transition-all duration-300"
                style={{ width: `${(d.value / max) * 100}%`, backgroundColor: color }}
                title={formatValue(d.value)}
              />
            </div>
            <span className="w-14 shrink-0 text-right font-semibold tabular-nums text-ink-700">{formatValue(d.value)}</span>
          </div>
        ))}
      </div>
    )
  }

  const barWidth = 100 / data.length

  return (
    <svg viewBox={`0 0 100 ${height}`} preserveAspectRatio="none" className="w-full" style={{ height }}>
      {data.map((d, i) => {
        const barH = (d.value / max) * (height - 24)
        return (
          <g key={d.label}>
            <rect
              x={`${i * barWidth + barWidth * 0.15}%`}
              y={height - 20 - barH}
              width={`${barWidth * 0.7}%`}
              height={barH}
              rx={2}
              fill={color}
            >
              <title>{`${d.label}: ${formatValue(d.value)}`}</title>
            </rect>
            <text
              x={`${i * barWidth + barWidth / 2}%`}
              y={height - 6}
              textAnchor="middle"
              fontSize="7"
              fill="var(--color-ink-500)"
            >
              {d.label}
            </text>
          </g>
        )
      })}
    </svg>
  )
}
