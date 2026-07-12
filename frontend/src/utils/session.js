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

// Chuẩn hoá phản hồi đăng nhập từ nhiều dạng shape có thể gặp:
// - phẳng: { accessToken, userId, fullName, email, role } (backend hiện tại)
// - lồng:  { token, user: { userId, fullName, role } }     (nếu nhóm đổi sau này)
// Lấy được cả hai để FE không vỡ dù backend đi hướng nào.
export const readAuthResponse = (data) => {
  const d = data ?? {}
  const token = d.token ?? d.accessToken ?? null
  const user =
    d.user ??
    (d.userId != null || d.fullName
      ? { userId: d.userId, fullName: d.fullName, email: d.email, role: d.role }
      : null)
  return { token, user }
}

export const clearSession = () => {
  localStorage.removeItem('token')
  localStorage.removeItem('user')
}
