import { useEffect, useRef, useState } from 'react'
import client, { apiError } from '../api/client'
import { normalizeHousekeepingStatus } from '../utils/apiShape'
import { EASE } from '../utils/ui'

const POLL_INTERVAL_MS = 15000

// Chuong thong bao yeu cau don phong tu khach - chi Admin/Manager/Receptionist thay (khop quyen BE),
// dung polling dinh ky don gian thay vi WebSocket/SignalR - du dung cho quy mo do an, khong them ha
// tang moi.
export default function HousekeepingBell() {
  const [requests, setRequests] = useState([])
  const [open, setOpen] = useState(false)
  const [error, setError] = useState('')
  const busyRef = useRef({})
  const containerRef = useRef(null)

  const fetchRequests = () => {
    client
      .get('/api/housekeeping-requests')
      .then((res) => {
        setRequests(res.data ?? [])
        setError('')
      })
      .catch((err) => setError(apiError(err)))
  }

  useEffect(() => {
    fetchRequests()
    const timer = setInterval(fetchRequests, POLL_INTERVAL_MS)
    return () => clearInterval(timer)
  }, [])

  // Bấm ra ngoài thì đóng dropdown
  useEffect(() => {
    if (!open) return
    const onClickOutside = (e) => {
      if (containerRef.current && !containerRef.current.contains(e.target)) setOpen(false)
    }
    document.addEventListener('mousedown', onClickOutside)
    return () => document.removeEventListener('mousedown', onClickOutside)
  }, [open])

  const act = (id, action) => {
    if (busyRef.current[id]) return
    busyRef.current[id] = true
    client
      .patch(`/api/housekeeping-requests/${id}/${action}`)
      .then(() => fetchRequests())
      .catch((err) => setError(apiError(err)))
      .finally(() => {
        busyRef.current[id] = false
      })
  }

  const pendingCount = requests.filter((r) => normalizeHousekeepingStatus(r.status) === 'Pending').length

  return (
    <div className="relative" ref={containerRef}>
      <button
        type="button"
        onClick={() => setOpen((v) => !v)}
        aria-label="Yêu cầu dọn phòng"
        title="Yêu cầu dọn phòng"
        className={`relative flex h-9 w-9 items-center justify-center rounded-full text-ink-500 ring-1 ring-black/10 ${EASE} hover:bg-white hover:text-ink-900`}
      >
        <svg width="15" height="15" viewBox="0 0 16 16" fill="none" aria-hidden>
          <path
            d="M8 1.5C5.8 1.5 4.2 3.3 4.2 5.5c0 3-1.5 4-1.5 5h10.6c0-1-1.5-2-1.5-5 0-2.2-1.6-4-3.8-4z"
            stroke="currentColor" strokeWidth="1.3" strokeLinecap="round" strokeLinejoin="round"
          />
          <path d="M6.5 13a1.6 1.6 0 003 0" stroke="currentColor" strokeWidth="1.3" strokeLinecap="round" />
        </svg>
        {pendingCount > 0 && (
          <span className="absolute -right-0.5 -top-0.5 flex h-4 min-w-4 items-center justify-center rounded-full bg-rose-600 px-1 text-[9px] font-bold text-white">
            {pendingCount}
          </span>
        )}
      </button>

      {open && (
        <div className="card-rise absolute right-0 top-11 z-30 w-80 rounded-2xl bg-cream-50 p-3 shadow-lift ring-1 ring-black/[0.06]">
          <p className="px-2 py-1 text-[11px] font-bold uppercase tracking-wider text-ink-500">Yêu cầu dọn phòng</p>
          {error && <p className="px-2 py-1 text-[12px] text-amber-800">{error}</p>}
          {requests.length === 0 && !error && (
            <p className="px-2 py-3 text-[12px] text-ink-500">Không có yêu cầu nào đang chờ.</p>
          )}
          <div className="max-h-80 space-y-1.5 overflow-y-auto">
            {requests.map((r) => {
              const status = normalizeHousekeepingStatus(r.status)
              return (
                <div key={r.id} className="rounded-xl bg-white p-3 ring-1 ring-black/[0.05]">
                  <div className="flex items-center justify-between gap-2">
                    <span className="text-[13px] font-bold">Phòng {r.roomNumber}</span>
                    <span
                      className={`rounded-full px-2 py-0.5 text-[10px] font-bold uppercase tracking-wide ${
                        status === 'Pending'
                          ? 'bg-rose-50 text-rose-700 ring-1 ring-rose-600/15'
                          : 'bg-sky-50 text-sky-700 ring-1 ring-sky-600/15'
                      }`}
                    >
                      {status === 'Pending' ? 'Mới' : 'Đang xử lý'}
                    </span>
                  </div>
                  <p className="mt-0.5 text-[12px] text-ink-500">{r.guestName}</p>
                  {r.note && <p className="mt-1 text-[12px] italic text-ink-700">"{r.note}"</p>}
                  <div className="mt-2 flex justify-end gap-1.5">
                    {status === 'Pending' && (
                      <button
                        type="button"
                        onClick={() => act(r.id, 'acknowledge')}
                        className={`rounded-full px-3 py-1 text-[11px] font-bold text-ink-700 ring-1 ring-black/10 ${EASE} hover:bg-cream-50`}
                      >
                        Xác nhận
                      </button>
                    )}
                    <button
                      type="button"
                      onClick={() => act(r.id, 'complete')}
                      className={`rounded-full bg-brand-600 px-3 py-1 text-[11px] font-bold text-white ${EASE} hover:bg-brand-700`}
                    >
                      Hoàn tất
                    </button>
                  </div>
                </div>
              )
            })}
          </div>
        </div>
      )}
    </div>
  )
}
