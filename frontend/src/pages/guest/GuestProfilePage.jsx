import { useEffect, useState } from 'react'
import { EASE, errorCls } from '../../utils/ui'
import guestClient, { apiError } from '../../api/guestClient'
import { updateGuestFullName } from '../../utils/guestSession'

const inputCls =
  'w-full rounded-lg border border-black/15 bg-white px-3.5 py-3 text-sm outline-none placeholder:text-ink-500/40 focus:border-brand-500 focus:ring-2 focus:ring-brand-500/20'
const labelCls = 'mb-1.5 block text-[11px] font-bold uppercase tracking-[0.18em] text-ink-700'
const successCls = 'rounded-lg bg-emerald-50 px-3.5 py-2.5 text-[12px] font-medium text-emerald-800 ring-1 ring-emerald-600/15'

// Trang ho so cua khach - xem/sua ten+email, doi mat khau. SDT khong sua duoc o day vi la khoa
// khop du lieu voi dat phong ben le tan - doi tuy tien de gay lech ho so.
export default function GuestProfilePage() {
  const [profile, setProfile] = useState(null)
  const [loading, setLoading] = useState(true)
  const [loadError, setLoadError] = useState('')

  const [fullName, setFullName] = useState('')
  const [email, setEmail] = useState('')
  const [savingProfile, setSavingProfile] = useState(false)
  const [profileError, setProfileError] = useState('')
  const [profileSaved, setProfileSaved] = useState(false)

  const [currentPassword, setCurrentPassword] = useState('')
  const [newPassword, setNewPassword] = useState('')
  const [confirmPassword, setConfirmPassword] = useState('')
  const [savingPassword, setSavingPassword] = useState(false)
  const [passwordError, setPasswordError] = useState('')
  const [passwordSaved, setPasswordSaved] = useState(false)

  useEffect(() => {
    guestClient
      .get('/api/guest/me/profile')
      .then((res) => {
        setProfile(res.data)
        setFullName(res.data.fullName)
        setEmail(res.data.email ?? '')
      })
      .catch((err) => setLoadError(apiError(err)))
      .finally(() => setLoading(false))
  }, [])

  const saveProfile = (e) => {
    e.preventDefault()
    if (savingProfile) return
    const trimmedName = fullName.trim()
    if (!trimmedName) {
      setProfileError('Vui lòng nhập họ và tên.')
      return
    }
    setProfileError('')
    setProfileSaved(false)
    setSavingProfile(true)
    guestClient
      .put('/api/guest/me/profile', { fullName: trimmedName, email: email.trim() || null })
      .then((res) => {
        setProfile(res.data)
        updateGuestFullName(res.data.fullName)
        setProfileSaved(true)
      })
      .catch((err) => setProfileError(apiError(err)))
      .finally(() => setSavingProfile(false))
  }

  const savePassword = (e) => {
    e.preventDefault()
    if (savingPassword) return
    if (newPassword.length < 6) {
      setPasswordError('Mật khẩu mới phải có ít nhất 6 ký tự.')
      return
    }
    if (newPassword !== confirmPassword) {
      setPasswordError('Mật khẩu nhập lại không khớp.')
      return
    }
    setPasswordError('')
    setPasswordSaved(false)
    setSavingPassword(true)
    guestClient
      .post('/api/guest/me/change-password', {
        currentPassword: currentPassword || undefined,
        newPassword,
        confirmPassword,
      })
      .then(() => {
        setPasswordSaved(true)
        setCurrentPassword('')
        setNewPassword('')
        setConfirmPassword('')
        setProfile((prev) => (prev ? { ...prev, hasPassword: true } : prev))
      })
      .catch((err) => setPasswordError(apiError(err)))
      .finally(() => setSavingPassword(false))
  }

  if (loading) return <p className="text-sm text-ink-500">Đang tải…</p>
  if (loadError) return <p className={errorCls}>{loadError}</p>

  return (
    <div className="mx-auto max-w-3xl">
      <p className="font-display text-[13px] italic text-brand-600">hồ sơ của tôi</p>
      <h1 className="mt-1 font-display text-3xl font-semibold tracking-tight">Thông tin cá nhân</h1>
      <p className="mt-2 text-sm text-ink-500">Cập nhật tên hiển thị, email và mật khẩu đăng nhập.</p>

      <div className="mt-8 max-w-lg space-y-6">
        <form onSubmit={saveProfile} className="card-rise rounded-2xl bg-cream-50 p-5 ring-1 ring-black/[0.06]">
          <h2 className="font-display text-lg font-semibold">Thông tin liên hệ</h2>
          <div className="mt-4 space-y-4">
            <div>
              <label className={labelCls}>Số điện thoại</label>
              <input className={`${inputCls} bg-black/[0.03] text-ink-500`} value={profile.phoneNumber} disabled />
              <p className="mt-1.5 text-[11px] leading-relaxed text-ink-500/70">
                Số điện thoại là khóa khớp với hồ sơ đặt phòng ở lễ tân — liên hệ khách sạn nếu cần đổi.
              </p>
            </div>
            <div>
              <label className={labelCls}>Họ và tên</label>
              <input
                required
                className={inputCls}
                value={fullName}
                onChange={(e) => setFullName(e.target.value)}
              />
            </div>
            <div>
              <label className={labelCls}>Email</label>
              <input
                type="email"
                className={inputCls}
                placeholder="Dùng để nhận link đặt lại mật khẩu"
                value={email}
                onChange={(e) => setEmail(e.target.value)}
              />
            </div>

            {profileError && <p className={errorCls}>{profileError}</p>}
            {profileSaved && <p className={successCls}>Đã lưu thông tin.</p>}

            <button
              type="submit"
              disabled={savingProfile}
              className={`rounded-full bg-brand-600 px-6 py-2.5 text-[12px] font-bold uppercase tracking-wider text-white ${EASE} hover:bg-brand-700 active:scale-[0.98] disabled:opacity-50`}
            >
              {savingProfile ? 'Đang lưu…' : 'Lưu thay đổi'}
            </button>
          </div>
        </form>

        <form onSubmit={savePassword} className="card-rise rounded-2xl bg-cream-50 p-5 ring-1 ring-black/[0.06]">
          <h2 className="font-display text-lg font-semibold">Mật khẩu</h2>
          <p className="mt-1 text-[12px] text-ink-500">
            {profile.hasPassword
              ? 'Đổi mật khẩu đăng nhập bằng số điện thoại.'
              : 'Bạn đang chỉ đăng nhập bằng Google — đặt mật khẩu để có thể đăng nhập bằng số điện thoại nữa.'}
          </p>
          <div className="mt-4 space-y-4">
            {profile.hasPassword && (
              <div>
                <label className={labelCls}>Mật khẩu hiện tại</label>
                <input
                  type="password"
                  required
                  autoComplete="current-password"
                  className={inputCls}
                  value={currentPassword}
                  onChange={(e) => setCurrentPassword(e.target.value)}
                />
              </div>
            )}
            <div>
              <label className={labelCls}>{profile.hasPassword ? 'Mật khẩu mới' : 'Đặt mật khẩu'}</label>
              <input
                type="password"
                required
                autoComplete="new-password"
                className={inputCls}
                placeholder="Tối thiểu 6 ký tự"
                value={newPassword}
                onChange={(e) => setNewPassword(e.target.value)}
              />
            </div>
            <div>
              <label className={labelCls}>Nhập lại mật khẩu</label>
              <input
                type="password"
                required
                autoComplete="new-password"
                className={inputCls}
                value={confirmPassword}
                onChange={(e) => setConfirmPassword(e.target.value)}
              />
            </div>

            {passwordError && <p className={errorCls}>{passwordError}</p>}
            {passwordSaved && <p className={successCls}>Đã đổi mật khẩu.</p>}

            <button
              type="submit"
              disabled={savingPassword}
              className={`rounded-full bg-brand-600 px-6 py-2.5 text-[12px] font-bold uppercase tracking-wider text-white ${EASE} hover:bg-brand-700 active:scale-[0.98] disabled:opacity-50`}
            >
              {savingPassword ? 'Đang lưu…' : profile.hasPassword ? 'Đổi mật khẩu' : 'Đặt mật khẩu'}
            </button>
          </div>
        </form>
      </div>
    </div>
  )
}
