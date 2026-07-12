import { EASE } from '../utils/ui'

// Trạng thái lỗi thật từ máy chủ - khác với fallback mock khi backend chưa chạy
export default function ErrorState({ message, onRetry }) {
  return (
    <div className="flex flex-col items-center rounded-2xl bg-rose-50/60 px-6 py-10 ring-1 ring-rose-600/15">
      <span className="h-10 w-8 rounded-t-full rounded-b-md border-2 border-dashed border-rose-400/60" />
      <p className="mt-4 font-display text-lg italic text-ink-700">Không tải được dữ liệu</p>
      <p className="mt-1 max-w-sm text-center text-[13px] text-ink-500">
        {message ?? 'Máy chủ đang gặp sự cố. Thử lại sau ít phút hoặc báo cho quản trị viên.'}
      </p>
      {onRetry && (
        <button
          onClick={onRetry}
          className={`mt-5 rounded-full bg-ink-900 px-5 py-2 text-[13px] font-bold text-cream-50 ${EASE} hover:bg-ink-700 active:scale-[0.98]`}
        >
          Thử lại
        </button>
      )}
    </div>
  )
}
