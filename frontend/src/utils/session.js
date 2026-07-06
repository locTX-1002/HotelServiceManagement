// Phiên đăng nhập trong localStorage - dùng chung cho LoginPage, ProtectedRoute, MainLayout.
// Key 'token' phải khớp với interceptor trong api/client.js.
export const getToken = () => localStorage.getItem('token')

export const getUser = () => {
  try {
    return JSON.parse(localStorage.getItem('user'))
  } catch {
    return null
  }
}

export const saveSession = (token, user) => {
  localStorage.setItem('token', token)
  localStorage.setItem('user', JSON.stringify(user ?? null))
}

export const clearSession = () => {
  localStorage.removeItem('token')
  localStorage.removeItem('user')
}

// Phiên xem thử giao diện khi backend chưa có API login (dev only). Token giả này
// sẽ bị 401 ngay khi backend bật [Authorize] -> interceptor tự đưa về /login.
// Role Admin để dev xem được mọi màn hình, không bị lớp chặn theo vai trò giấu mất.
export const startDemoSession = () =>
  saveSession('demo-token', { fullName: 'Admin Demo', role: 'Admin', isDemo: true })
