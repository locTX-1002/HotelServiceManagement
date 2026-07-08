// Trang giữ chỗ cho các chức năng đang phát triển - style đồng bộ motif vòm
export default function Placeholder({ title }) {
  return (
    <div className="flex flex-col items-center rounded-2xl border border-dashed border-black/10 bg-white/60 px-6 py-16 text-center">
      <span className="h-12 w-9 rounded-t-full rounded-b-md border-2 border-dashed border-brand-600/30" />
      <p className="mt-5 font-display text-2xl font-semibold tracking-tight">{title}</p>
      <p className="mt-2 max-w-sm text-[13px] leading-relaxed text-ink-500">
        Chức năng này đang được phát triển và sẽ sớm có mặt.
      </p>
    </div>
  )
}
