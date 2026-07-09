import { Component } from 'react'

const EASE = 'transition-all duration-300 ease-[cubic-bezier(0.32,0.72,0,1)]'

// Bắt lỗi runtime của cây con để 1 trang hỏng không làm trắng cả app.
// Error boundary bắt buộc là class component (React chưa có bản hook).
export default class ErrorBoundary extends Component {
  state = { error: null, prevKey: this.props.resetKey }

  static getDerivedStateFromError(error) {
    return { error }
  }

  // Đổi trang (resetKey đổi) thì xóa lỗi cũ để trang mới được render lại
  static getDerivedStateFromProps(props, state) {
    if (props.resetKey !== state.prevKey) {
      return { error: null, prevKey: props.resetKey }
    }
    return null
  }

  render() {
    if (!this.state.error) return this.props.children

    return (
      <div className="flex flex-col items-center rounded-2xl border border-dashed border-rose-600/20 bg-rose-50/50 px-6 py-16 text-center">
        <span className="flex h-12 w-9 items-end justify-center rounded-t-full rounded-b-md bg-rose-100 pb-1.5 text-lg font-bold text-rose-600 ring-1 ring-rose-600/15">
          !
        </span>
        <p className="mt-5 font-display text-2xl font-semibold tracking-tight">Đã có lỗi khi hiển thị trang này</p>
        <p className="mt-2 max-w-sm text-[13px] leading-relaxed text-ink-500">
          Màn hình gặp sự cố ngoài dự kiến. Thử tải lại — nếu vẫn lỗi, báo lại cho nhóm kỹ thuật kèm thao tác vừa làm.
        </p>
        <button
          onClick={() => window.location.reload()}
          className={`mt-6 rounded-full bg-ink-900 px-5 py-2.5 text-[13px] font-bold text-cream-50 ${EASE} hover:bg-ink-700 active:scale-[0.98]`}
        >
          Tải lại trang
        </button>
      </div>
    )
  }
}
