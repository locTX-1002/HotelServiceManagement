import { Link } from 'react-router-dom'
import { EASE } from '../utils/ui'

// Tab chuyen nhanh giua 2 cong dang nhap - 2 form von giong nhau nen khach rat de go nham vao form
// nhan vien (va nguoc lai). Tab neu ro dang dung o cong nao, bam 1 phat de doi sang cong kia.
export default function PortalSwitch({ active }) {
  const tabs = [
    { key: 'guest', label: 'Khách lưu trú', to: '/guest/dang-nhap' },
    { key: 'staff', label: 'Nhân viên', to: '/login' },
  ]
  return (
    <div className="flex rounded-full bg-black/[0.06] p-1">
      {tabs.map((t) =>
        t.key === active ? (
          <span
            key={t.key}
            className="flex-1 rounded-full bg-white px-4 py-2 text-center text-[12px] font-bold text-ink-900 shadow-soft"
          >
            {t.label}
          </span>
        ) : (
          <Link
            key={t.key}
            to={t.to}
            className={`flex-1 rounded-full px-4 py-2 text-center text-[12px] font-semibold text-ink-500 ${EASE} hover:text-ink-900`}
          >
            {t.label}
          </Link>
        ),
      )}
    </div>
  )
}
