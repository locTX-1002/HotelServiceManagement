// Ô trạng thái rỗng dùng chung cho các panel danh sách (đơn dịch vụ, hoá đơn...).
export default function EmptyState({ text }) {
  return (
    <div className="mt-6 flex flex-col items-center rounded-2xl border border-dashed border-black/10 bg-white/60 px-6 py-14">
      <span className="h-12 w-9 rounded-t-full rounded-b-md border-2 border-dashed border-brand-600/30" />
      <p className="mt-4 font-display text-lg italic text-ink-700">{text}</p>
    </div>
  )
}
