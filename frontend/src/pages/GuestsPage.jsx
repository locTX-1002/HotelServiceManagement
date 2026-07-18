import { useEffect, useState } from 'react'
import { EASE, errorCls, inputCls, labelCls } from '../utils/ui'
import PageHero from '../components/PageHero'
import client, { isBackendMissing, apiError } from '../api/client'
import ErrorState from '../components/ErrorState'
import SlideOver from '../components/SlideOver'
import { useToast } from '../components/toastContext'
import { MOCK_GUESTS } from '../mock/hotelMock'
import { normalizeGuest, denormalizeGuestTag } from '../utils/apiShape'

// Nhãn + màu cho phân loại khách - VIP tô điểm, Blacklist cảnh báo, None không hiện gì
const GUEST_TAG = {
  Vip: { label: 'VIP', badge: 'bg-amber-50 text-amber-800 ring-1 ring-amber-600/20' },
  Blacklisted: { label: 'Cảnh báo', badge: 'bg-rose-50 text-rose-700 ring-1 ring-rose-600/20' },
}

// Quản lý hồ sơ khách - GET /api/guests?keyword= (tìm theo tên/sđt/CMND) + PUT /api/guests/{id}.
// Tạo khách mới thì nằm sẵn trong luồng tạo đặt phòng nên trang này chỉ xem + sửa.
export default function GuestsPage() {
  const toast = useToast()
  const [guests, setGuests] = useState(null)
  const [usingMock, setUsingMock] = useState(false)
  const [loadError, setLoadError] = useState(false)
  const [keyword, setKeyword] = useState('')
  const [retryTick, setRetryTick] = useState(0) // bấm Thử lại thì tăng để effect chạy lại với keyword cũ
  const [toEdit, setToEdit] = useState(null)
  const [form, setForm] = useState({ fullName: '', phoneNumber: '', identityNumber: '', email: '', tag: 'None', tagNote: '' })
  const [formError, setFormError] = useState('')
  const [saving, setSaving] = useState(false)

  // Tìm kiếm chạy phía máy chủ - gõ xong 350ms mới gọi, không bắn request mỗi phím.
  // stale = đã gõ keyword mới trong lúc request cũ còn bay -> response cũ về muộn không được đè kết quả mới
  useEffect(() => {
    let stale = false
    const timer = setTimeout(() => {
      setLoadError(false)
      client
        .get('/api/guests', { params: { keyword: keyword.trim() || undefined } })
        .then((res) => {
          if (stale) return
          setGuests(res.data.map(normalizeGuest)); setUsingMock(false)
        })
        .catch((err) => {
          if (stale) return
          if (isBackendMissing(err)) {
            const q = keyword.trim().toLowerCase()
            setGuests(MOCK_GUESTS.filter((g) => !q || g.fullName.toLowerCase().includes(q) || g.phoneNumber.includes(q)).map(normalizeGuest))
            setUsingMock(true)
          } else setLoadError(true) // lỗi thật: không che bằng mock
        })
    }, 350)
    return () => { stale = true; clearTimeout(timer) }
  }, [keyword, retryTick])

  const reload = () => setRetryTick((t) => t + 1)

  const openEdit = (g) => {
    setForm({
      fullName: g.fullName,
      phoneNumber: g.phoneNumber,
      identityNumber: g.identityNumber,
      email: g.email ?? '',
      tag: g.tag ?? 'None',
      tagNote: g.tagNote ?? '',
    })
    setFormError('')
    setToEdit(g)
  }

  const submit = (e) => {
    e.preventDefault()
    if (!form.fullName.trim() || !form.phoneNumber.trim() || !form.identityNumber.trim())
      return setFormError('Nhập đủ họ tên, số điện thoại và CMND/CCCD.')

    setFormError('')
    setSaving(true)
    client
      .put(`/api/guests/${toEdit.id}`, {
        fullName: form.fullName.trim(),
        phoneNumber: form.phoneNumber.trim(),
        identityNumber: form.identityNumber.trim(),
        email: form.email.trim() || null,
        tag: denormalizeGuestTag(form.tag),
        tagNote: form.tagNote.trim() || null,
      })
      .then(() => {
        toast.success(`Đã lưu hồ sơ ${form.fullName.trim()}`)
        setToEdit(null)
        reload() // nạp lại qua effect (giữ keyword hiện tại + có cờ stale)
      })
      .catch((err) => setFormError(apiError(err)))
      .finally(() => setSaving(false))
  }

  return (
    <div>
      <PageHero
        image="/img/family.jpg"
        kicker="lễ tân · hồ sơ khách"
        title="Danh sách khách"
        subtitle="Tra cứu và cập nhật thông tin khách — khách mới được tạo trong lúc đặt phòng."
      >
        {usingMock && (
          <span className="rounded-full bg-amber-50 px-2.5 py-1 text-[11px] font-bold text-amber-800 ring-1 ring-amber-600/20">
            Dữ liệu mẫu
          </span>
        )}
        <input
          className="w-56 rounded-full bg-white px-4 py-2 text-[13px] ring-1 ring-black/10 outline-none placeholder:text-ink-500/50 focus:ring-2 focus:ring-brand-500/40"
          placeholder="Tìm tên / SĐT / CMND…"
          value={keyword}
          onChange={(e) => setKeyword(e.target.value)}
        />
      </PageHero>

      {/* Bảng khách */}
      {guests === null && !loadError && (
        <div className="mt-6 space-y-3">
          {Array.from({ length: 5 }).map((_, i) => (
            <div key={i} className="h-16 animate-pulse rounded-2xl bg-cream-200" />
          ))}
        </div>
      )}

      {loadError && <div className="mt-6"><ErrorState onRetry={reload} /></div>}

      {!loadError && guests !== null && guests.length > 0 && (
        <div className="card-rise mt-6 bezel-shell">
          <div className="bezel-core overflow-hidden">
            <div className="overflow-x-auto">
              <table className="w-full min-w-[640px] text-left">
                <thead>
                  <tr className="border-b border-black/[0.06] text-[10px] font-bold uppercase tracking-[0.18em] text-ink-500">
                    <th className="px-5 py-3.5">Khách</th>
                    <th className="px-5 py-3.5">Số điện thoại</th>
                    <th className="px-5 py-3.5">CMND/CCCD</th>
                    <th className="px-5 py-3.5 text-right">Lượt đặt</th>
                    <th className="px-5 py-3.5 text-right">Thao tác</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-black/[0.05]">
                  {guests.map((g) => (
                    <tr key={g.id} className={`${EASE} hover:bg-cream-50/60`}>
                      <td className="px-5 py-3.5">
                        <p className="text-sm font-semibold">
                          {g.fullName}
                          {GUEST_TAG[g.tag] && (
                            <span
                              title={g.tagNote || undefined}
                              className={`ml-2 rounded-full px-2 py-0.5 text-[10px] font-bold ${GUEST_TAG[g.tag].badge}`}
                            >
                              {GUEST_TAG[g.tag].label}
                            </span>
                          )}
                        </p>
                        {g.email && <p className="text-[11px] text-ink-500">{g.email}</p>}
                      </td>
                      <td className="px-5 py-3.5 text-sm tabular-nums text-ink-700">{g.phoneNumber}</td>
                      <td className="px-5 py-3.5 text-sm tabular-nums text-ink-700">{g.identityNumber}</td>
                      <td className="px-5 py-3.5 text-right text-sm tabular-nums text-ink-700">{g.reservationCount ?? 0}</td>
                      <td className="px-5 py-3.5 text-right">
                        <button
                          onClick={() => openEdit(g)}
                          className={`rounded-full px-3.5 py-1.5 text-[12px] font-semibold text-ink-700 ring-1 ring-black/10 ${EASE} hover:bg-cream-100`}
                        >
                          Sửa
                        </button>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </div>
        </div>
      )}

      {!loadError && guests !== null && guests.length === 0 && (
        <div className="mt-6 flex flex-col items-center rounded-2xl border border-dashed border-black/10 bg-white/60 px-6 py-14">
          <span className="h-12 w-9 rounded-t-full rounded-b-md border-2 border-dashed border-brand-600/30" />
          <p className="mt-4 font-display text-lg italic text-ink-700">
            {keyword.trim() ? 'Không tìm thấy khách khớp từ khóa' : 'Chưa có khách nào'}
          </p>
          {keyword.trim() && (
            <button onClick={() => setKeyword('')} className="mt-2 text-[12px] font-bold uppercase tracking-wider text-brand-600 hover:underline">
              Xóa tìm kiếm
            </button>
          )}
        </div>
      )}

      {/* Form sửa hồ sơ khách */}
      <SlideOver
        open={toEdit !== null}
        eyebrow="chỉnh sửa"
        title={`Hồ sơ ${toEdit?.fullName ?? ''}`}
        onClose={() => setToEdit(null)}
      >
        <form onSubmit={submit} className="space-y-5">
          <div>
            <label htmlFor="guest-name" className={labelCls}>Họ tên *</label>
            <input
              id="guest-name"
              className={inputCls}
              value={form.fullName}
              onChange={(e) => setForm({ ...form, fullName: e.target.value })}
            />
          </div>
          <div>
            <label htmlFor="guest-phone" className={labelCls}>Số điện thoại *</label>
            <input
              id="guest-phone"
              className={inputCls}
              value={form.phoneNumber}
              onChange={(e) => setForm({ ...form, phoneNumber: e.target.value })}
            />
          </div>
          <div>
            <label htmlFor="guest-identity" className={labelCls}>CMND/CCCD *</label>
            <input
              id="guest-identity"
              className={inputCls}
              value={form.identityNumber}
              onChange={(e) => setForm({ ...form, identityNumber: e.target.value })}
            />
          </div>
          <div>
            <label htmlFor="guest-email" className={labelCls}>Email</label>
            <input
              id="guest-email"
              type="email"
              className={inputCls}
              placeholder="tuychon@gmail.com"
              value={form.email}
              onChange={(e) => setForm({ ...form, email: e.target.value })}
            />
          </div>
          <div>
            <label htmlFor="guest-tag" className={labelCls}>Phân loại khách</label>
            <select
              id="guest-tag"
              className={inputCls}
              value={form.tag}
              onChange={(e) => setForm({ ...form, tag: e.target.value, tagNote: e.target.value === 'None' ? '' : form.tagNote })}
            >
              <option value="None">Bình thường</option>
              <option value="Vip">VIP</option>
              <option value="Blacklisted">Cảnh báo / Blacklist</option>
            </select>
          </div>
          {form.tag !== 'None' && (
            <div>
              <label htmlFor="guest-tag-note" className={labelCls}>Ghi chú</label>
              <textarea
                id="guest-tag-note"
                rows={2}
                className={inputCls}
                placeholder={form.tag === 'Blacklisted' ? 'Lý do (vd: bùng phòng, gây rối...)' : 'Ghi chú thêm (không bắt buộc)'}
                value={form.tagNote}
                onChange={(e) => setForm({ ...form, tagNote: e.target.value })}
              />
            </div>
          )}

          {formError && (
            <p className={errorCls}>{formError}</p>
          )}

          <button
            type="submit"
            disabled={saving}
            className={`w-full rounded-full bg-brand-600 py-3 text-[13px] font-bold text-white ${EASE} hover:bg-brand-700 active:scale-[0.98] disabled:opacity-40`}
          >
            {saving ? 'Đang lưu…' : 'Lưu thay đổi'}
          </button>
        </form>
      </SlideOver>
    </div>
  )
}
