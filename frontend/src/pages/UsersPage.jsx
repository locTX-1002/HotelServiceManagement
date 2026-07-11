import { useEffect, useState } from 'react'
import client, { isBackendMissing } from '../api/client'
import ConfirmDialog from '../components/ConfirmDialog'
import ErrorState from '../components/ErrorState'
import SlideOver from '../components/SlideOver'
import { useToast } from '../components/toastContext'
import { MOCK_USERS } from '../mock/hotelMock'
import { ROLE_LABEL } from '../utils/roles'
import { getUser } from '../utils/session'

const EASE = 'transition-all duration-300 ease-[cubic-bezier(0.32,0.72,0,1)]'
const inputCls =
  'w-full rounded-xl bg-white px-3.5 py-2.5 text-sm ring-1 ring-black/10 outline-none placeholder:text-ink-500/50 focus:ring-2 focus:ring-brand-500/40'
const labelCls = 'mb-1.5 block text-[12px] font-semibold text-ink-700'

// roleId khớp seed backend: 1 Admin, 2 Manager, 3 Receptionist, 4 ServiceStaff
const ROLES = [
  { roleId: 1, name: 'Admin' },
  { roleId: 2, name: 'Manager' },
  { roleId: 3, name: 'Receptionist' },
  { roleId: 4, name: 'ServiceStaff' },
]

const EMPTY_FORM = { fullName: '', email: '', roleId: 3, password: '', confirmPassword: '' }

const apiError = (err) =>
  isBackendMissing(err)
    ? 'Không kết nối được máy chủ. Vui lòng thử lại sau.'
    : err.response?.data?.message ?? 'Máy chủ báo lỗi. Thử lại sau ít phút.'

// Quản trị tài khoản nhân viên (chỉ Admin) - đủ 5 thao tác của /api/users:
// danh sách, tạo, sửa, khóa/mở (PATCH status), cấp lại mật khẩu (PATCH reset-password).
export default function UsersPage() {
  const toast = useToast()
  const me = getUser()
  const [users, setUsers] = useState(null)
  const [usingMock, setUsingMock] = useState(false)
  const [loadError, setLoadError] = useState(false)
  const [drawer, setDrawer] = useState(null) // { mode: 'create' | 'edit' | 'reset', item? }
  const [form, setForm] = useState(EMPTY_FORM)
  const [formError, setFormError] = useState('')
  const [saving, setSaving] = useState(false)
  const [toToggle, setToToggle] = useState(null)
  const [toggling, setToggling] = useState(false)
  const [toggleError, setToggleError] = useState('')

  const load = () => {
    setLoadError(false)
    client
      .get('/api/users')
      .then((res) => { setUsers(res.data); setUsingMock(false) })
      .catch((err) => {
        if (isBackendMissing(err)) { setUsers(MOCK_USERS); setUsingMock(true) }
        else setLoadError(true) // lỗi thật: không che bằng mock
      })
  }
  useEffect(load, [])

  const openCreate = () => { setForm(EMPTY_FORM); setFormError(''); setDrawer({ mode: 'create' }) }
  const openEdit = (u) => {
    setForm({ fullName: u.fullName, email: u.email, roleId: u.roleId, password: '', confirmPassword: '' })
    setFormError('')
    setDrawer({ mode: 'edit', item: u })
  }
  const openReset = (u) => {
    setForm({ ...EMPTY_FORM, roleId: u.roleId })
    setFormError('')
    setDrawer({ mode: 'reset', item: u })
  }

  const validate = () => {
    if (drawer.mode !== 'reset') {
      if (!form.fullName.trim()) return 'Nhập họ tên.'
      if (!form.email.trim()) return 'Nhập email.'
      const dup = (users ?? []).some(
        (u) => u.email.trim().toLowerCase() === form.email.trim().toLowerCase() && u.id !== drawer?.item?.id,
      )
      if (dup) return 'Email này đã có tài khoản.'
      // Tự hạ quyền = hệ thống có thể mất Admin cuối cùng, không ai vào /users sửa lại được
      if (drawer.mode === 'edit' && drawer.item.id === me?.userId && Number(form.roleId) !== drawer.item.roleId)
        return 'Không thể tự đổi vai trò của chính mình.'
    }
    if (drawer.mode !== 'edit') {
      if (form.password.length < 6) return 'Mật khẩu phải từ 6 ký tự.'
      if (form.password !== form.confirmPassword) return 'Xác nhận mật khẩu chưa khớp.'
    }
    return ''
  }

  const submit = (e) => {
    e.preventDefault()
    const msg = validate()
    if (msg) return setFormError(msg)

    setFormError('')
    setSaving(true)
    const req =
      drawer.mode === 'create'
        ? client.post('/api/users', {
            fullName: form.fullName.trim(),
            email: form.email.trim(),
            password: form.password,
            confirmPassword: form.confirmPassword,
            roleId: Number(form.roleId),
          })
        : drawer.mode === 'edit'
          ? client.put(`/api/users/${drawer.item.id}`, {
              fullName: form.fullName.trim(),
              email: form.email.trim(),
              roleId: Number(form.roleId),
            })
          : client.patch(`/api/users/${drawer.item.id}/reset-password`, {
              newPassword: form.password,
              confirmPassword: form.confirmPassword,
            })
    req
      .then(() => {
        toast.success(
          drawer.mode === 'create'
            ? `Đã tạo tài khoản ${form.fullName.trim()}`
            : drawer.mode === 'edit'
              ? `Đã lưu tài khoản ${form.fullName.trim()}`
              : `Đã cấp lại mật khẩu cho ${drawer.item.fullName}`,
        )
        setDrawer(null)
        load()
      })
      .catch((err) => setFormError(apiError(err)))
      .finally(() => setSaving(false))
  }

  // Khóa / mở tài khoản - PATCH /api/users/{id}/status { isActive }
  const confirmToggle = () => {
    setToggleError('')
    setToggling(true)
    client
      .patch(`/api/users/${toToggle.id}/status`, { isActive: !toToggle.isActive })
      .then(() => {
        toast.success(toToggle.isActive ? `Đã khóa ${toToggle.fullName}` : `Đã mở khóa ${toToggle.fullName}`)
        setToToggle(null)
        load()
      })
      .catch((err) => setToggleError(apiError(err)))
      .finally(() => setToggling(false))
  }

  const mode = drawer?.mode
  const drawerTitle =
    mode === 'create' ? 'Thêm tài khoản' : mode === 'edit' ? `Sửa ${drawer.item.fullName}` : `Cấp lại mật khẩu`

  return (
    <div>
      {/* Header */}
      <div className="flex flex-wrap items-end justify-between gap-4">
        <div>
          <p className="font-display text-[15px] italic capitalize text-brand-600">quản trị · tài khoản</p>
          <h1 className="mt-1 font-display text-4xl font-semibold tracking-tight">Người dùng</h1>
          <p className="mt-1 text-sm text-ink-500">Tài khoản nhân viên và vai trò truy cập — khóa thay vì xóa để giữ lịch sử thao tác.</p>
        </div>
        <div className="flex items-center gap-2.5">
          {usingMock && (
            <span className="rounded-full bg-amber-50 px-2.5 py-1 text-[11px] font-bold text-amber-800 ring-1 ring-amber-600/20">
              Dữ liệu mẫu
            </span>
          )}
          <button
            onClick={openCreate}
            className={`rounded-full bg-ink-900 px-5 py-2.5 text-[13px] font-bold text-cream-50 ${EASE} hover:bg-ink-700 active:scale-[0.98]`}
          >
            + Thêm tài khoản
          </button>
        </div>
      </div>

      {/* Bảng tài khoản */}
      {users === null && !loadError && (
        <div className="mt-6 space-y-3">
          {Array.from({ length: 4 }).map((_, i) => (
            <div key={i} className="h-16 animate-pulse rounded-2xl bg-cream-200" />
          ))}
        </div>
      )}

      {loadError && <div className="mt-6"><ErrorState onRetry={load} /></div>}

      {!loadError && users !== null && users.length > 0 && (
        <div className="card-rise mt-6 bezel-shell">
          <div className="bezel-core overflow-hidden">
            <div className="overflow-x-auto">
              <table className="w-full min-w-[680px] text-left">
                <thead>
                  <tr className="border-b border-black/[0.06] text-[10px] font-bold uppercase tracking-[0.18em] text-ink-500">
                    <th className="px-5 py-3.5">Nhân viên</th>
                    <th className="px-5 py-3.5">Vai trò</th>
                    <th className="px-5 py-3.5">Trạng thái</th>
                    <th className="px-5 py-3.5 text-right">Thao tác</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-black/[0.05]">
                  {users.map((u) => {
                    const isSelf = u.id === me?.userId
                    return (
                      <tr key={u.id} className={`${EASE} hover:bg-cream-50/60`}>
                        <td className="px-5 py-3.5">
                          <p className="text-sm font-semibold">
                            {u.fullName}
                            {isSelf && <span className="ml-2 rounded-full bg-brand-50 px-2 py-0.5 text-[10px] font-bold text-brand-700 ring-1 ring-brand-600/15">bạn</span>}
                          </p>
                          <p className="text-[11px] text-ink-500">{u.email}</p>
                        </td>
                        <td className="px-5 py-3.5">
                          <span className="text-sm font-semibold">{ROLE_LABEL[u.role] ?? u.role}</span>
                        </td>
                        <td className="px-5 py-3.5">
                          {u.isActive ? (
                            <span className="rounded-full bg-emerald-50 px-2.5 py-1 text-[11px] font-semibold text-emerald-700 ring-1 ring-emerald-600/15">Hoạt động</span>
                          ) : (
                            <span className="rounded-full bg-stone-100 px-2.5 py-1 text-[11px] font-semibold text-stone-600 ring-1 ring-stone-500/15">Đã khóa</span>
                          )}
                        </td>
                        <td className="px-5 py-3.5 text-right">
                          <button
                            onClick={() => openEdit(u)}
                            className={`rounded-full px-3.5 py-1.5 text-[12px] font-semibold text-ink-700 ring-1 ring-black/10 ${EASE} hover:bg-cream-100`}
                          >
                            Sửa
                          </button>
                          <button
                            onClick={() => openReset(u)}
                            className={`ml-2 rounded-full px-3.5 py-1.5 text-[12px] font-semibold text-ink-700 ring-1 ring-black/10 ${EASE} hover:bg-cream-100`}
                          >
                            Cấp lại MK
                          </button>
                          {/* Không cho tự khóa chính mình - mất luôn quyền vào hệ thống */}
                          {!isSelf && (
                            <button
                              onClick={() => { setToggleError(''); setToToggle(u) }}
                              className={`ml-2 rounded-full px-3.5 py-1.5 text-[12px] font-semibold ring-1 ${EASE} ${
                                u.isActive
                                  ? 'text-rose-700 ring-rose-600/20 hover:bg-rose-50'
                                  : 'text-emerald-700 ring-emerald-600/20 hover:bg-emerald-50'
                              }`}
                            >
                              {u.isActive ? 'Khóa' : 'Mở khóa'}
                            </button>
                          )}
                        </td>
                      </tr>
                    )
                  })}
                </tbody>
              </table>
            </div>
          </div>
        </div>
      )}

      {!loadError && users !== null && users.length === 0 && (
        <div className="mt-6 flex flex-col items-center rounded-2xl border border-dashed border-black/10 bg-white/60 px-6 py-14">
          <span className="h-12 w-9 rounded-t-full rounded-b-md border-2 border-dashed border-brand-600/30" />
          <p className="mt-4 font-display text-lg italic text-ink-700">Chưa có tài khoản nào</p>
          <button onClick={openCreate} className="mt-2 text-[12px] font-bold uppercase tracking-wider text-brand-600 hover:underline">
            Thêm tài khoản đầu tiên
          </button>
        </div>
      )}

      {/* Form thêm / sửa / cấp lại mật khẩu */}
      <SlideOver
        open={drawer !== null}
        eyebrow={mode === 'create' ? 'thêm mới' : mode === 'edit' ? 'chỉnh sửa' : 'bảo mật'}
        title={drawerTitle}
        onClose={() => setDrawer(null)}
      >
        <form onSubmit={submit} className="space-y-5">
          {mode === 'reset' && (
            <p className="text-[13px] text-ink-500">
              Đặt mật khẩu mới cho <span className="font-semibold text-ink-700">{drawer.item.fullName}</span> ({drawer.item.email}).
              Nhân viên nên tự đổi lại sau khi đăng nhập.
            </p>
          )}

          {mode !== 'reset' && (
            <>
              <div>
                <label htmlFor="user-name" className={labelCls}>Họ tên *</label>
                <input
                  id="user-name"
                  className={inputCls}
                  placeholder="Nguyễn Văn A"
                  value={form.fullName}
                  onChange={(e) => setForm({ ...form, fullName: e.target.value })}
                />
              </div>
              <div>
                <label htmlFor="user-email" className={labelCls}>Email đăng nhập *</label>
                <input
                  id="user-email"
                  type="email"
                  className={inputCls}
                  placeholder="nhanvien@hotel.com"
                  value={form.email}
                  onChange={(e) => setForm({ ...form, email: e.target.value })}
                />
              </div>
              <div>
                <label htmlFor="user-role" className={labelCls}>Vai trò *</label>
                <select
                  id="user-role"
                  className={`${inputCls} disabled:opacity-50`}
                  value={form.roleId}
                  disabled={mode === 'edit' && drawer.item.id === me?.userId}
                  onChange={(e) => setForm({ ...form, roleId: e.target.value })}
                >
                  {ROLES.map((r) => (
                    <option key={r.roleId} value={r.roleId}>{ROLE_LABEL[r.name] ?? r.name}</option>
                  ))}
                </select>
                {mode === 'edit' && drawer.item.id === me?.userId && (
                  <p className="mt-1.5 text-[12px] text-ink-500">Không thể tự đổi vai trò của chính mình.</p>
                )}
              </div>
            </>
          )}

          {mode !== 'edit' && (
            <>
              <div>
                <label htmlFor="user-password" className={labelCls}>{mode === 'reset' ? 'Mật khẩu mới *' : 'Mật khẩu *'}</label>
                <input
                  id="user-password"
                  type="password"
                  autoComplete="new-password"
                  className={inputCls}
                  value={form.password}
                  onChange={(e) => setForm({ ...form, password: e.target.value })}
                />
              </div>
              <div>
                <label htmlFor="user-confirm" className={labelCls}>Nhập lại mật khẩu *</label>
                <input
                  id="user-confirm"
                  type="password"
                  autoComplete="new-password"
                  className={inputCls}
                  value={form.confirmPassword}
                  onChange={(e) => setForm({ ...form, confirmPassword: e.target.value })}
                />
              </div>
            </>
          )}

          {formError && (
            <p className="rounded-lg bg-amber-50 px-3.5 py-2.5 text-[12px] font-medium text-amber-800 ring-1 ring-amber-600/15">{formError}</p>
          )}

          <button
            type="submit"
            disabled={saving}
            className={`w-full rounded-full bg-brand-600 py-3 text-[13px] font-bold text-white ${EASE} hover:bg-brand-700 active:scale-[0.98] disabled:opacity-40`}
          >
            {saving ? 'Đang lưu…' : mode === 'create' ? 'Tạo tài khoản' : mode === 'edit' ? 'Lưu thay đổi' : 'Cấp lại mật khẩu'}
          </button>
        </form>
      </SlideOver>

      {/* Xác nhận khóa / mở khóa */}
      <ConfirmDialog
        open={toToggle !== null}
        title={toToggle?.isActive ? `Khóa tài khoản ${toToggle?.fullName ?? ''}?` : `Mở khóa ${toToggle?.fullName ?? ''}?`}
        message={
          toToggle?.isActive
            ? 'Tài khoản bị khóa sẽ không đăng nhập được nữa cho tới khi mở lại. Lịch sử thao tác vẫn được giữ nguyên.'
            : 'Tài khoản sẽ đăng nhập lại được với mật khẩu hiện có.'
        }
        confirmLabel={toToggle?.isActive ? 'Khóa tài khoản' : 'Mở khóa'}
        busyLabel="Đang xử lý…"
        tone={toToggle?.isActive ? 'danger' : 'primary'}
        busy={toggling}
        error={toggleError}
        onConfirm={confirmToggle}
        onCancel={() => setToToggle(null)}
      />
    </div>
  )
}
