import { useEffect, useState } from 'react'
import client from '../api/client'

// Trang giữ chỗ - style đồng bộ motif vòm, sẽ được thay bằng UI thật theo Task Sheet
export default function Placeholder({ title, owner, day }) {
  const [health, setHealth] = useState(null)

  useEffect(() => {
    client.get('/health').then((res) => setHealth(res.data)).catch(() => setHealth(null))
  }, [])

  return (
    <div className="flex flex-col items-center rounded-2xl border border-dashed border-black/10 bg-white/60 px-6 py-16 text-center">
      <span className="h-12 w-9 rounded-t-full rounded-b-md border-2 border-dashed border-brand-600/30" />
      <p className="mt-5 font-display text-2xl font-semibold tracking-tight">{title}</p>
      <p className="mt-2 max-w-sm text-[13px] leading-relaxed text-ink-500">
        Màn hình này đang được xây — task <span className="font-semibold text-ink-700">{day}</span> do{' '}
        <span className="font-semibold text-ink-700">{owner}</span> phụ trách theo bảng phân công.
      </p>
      <p className="mt-5 text-[11px] font-medium">
        {health ? (
          <span className="rounded-full bg-emerald-50 px-2.5 py-1 text-emerald-700 ring-1 ring-emerald-600/15">Backend đã kết nối</span>
        ) : (
          <span className="rounded-full bg-stone-100 px-2.5 py-1 text-ink-500 ring-1 ring-stone-500/15">Backend chưa chạy</span>
        )}
      </p>
    </div>
  )
}
