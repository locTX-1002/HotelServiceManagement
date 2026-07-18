// Phiên đăng nhập trong localStorage - dùng chung cho LoginPage, ProtectedRoute, MainLayout.
// Key 'token' phải khớp với interceptor trong api/client.js.
export const getToken = () => localStorage.getItem('token')

// refreshToken sống lâu hơn nhiều so với access token (1-30 ngày tuỳ rememberMe) - api/client.js
// dùng nó để tự làm mới access token trong nền khi access token hết hạn giữa phiên.
export const getRefreshToken = () => localStorage.getItem('refreshToken')

export const getUser = () => {
  try {
    return JSON.parse(localStorage.getItem('user'))
  } catch {
    return null
  }
}

export const saveSession = (token, refreshToken, user) => {
  localStorage.setItem('token', token)
  if (refreshToken) localStorage.setItem('refreshToken', refreshToken)
  localStorage.setItem('user', JSON.stringify(user ?? null))
}

// Chuẩn hoá phản hồi đăng nhập/refresh từ nhiều dạng shape có thể gặp:
// - phẳng: { accessToken, refreshToken, userId, fullName, email, role } (backend hiện tại)
// - lồng:  { token, refreshToken, user: { userId, fullName, role } }     (nếu nhóm đổi sau này)
// Lấy được cả hai để FE không vỡ dù backend đi hướng nào.
export const readAuthResponse = (data) => {
  const d = data ?? {}
  const token = d.token ?? d.accessToken ?? null
  const refreshToken = d.refreshToken ?? null
  const user =
    d.user ??
    (d.userId != null || d.fullName
      ? { userId: d.userId, fullName: d.fullName, email: d.email, role: d.role }
      : null)
  return { token, refreshToken, user }
}

export const clearSession = () => {
  localStorage.removeItem('token')
  localStorage.removeItem('refreshToken')
  localStorage.removeItem('user')
}
