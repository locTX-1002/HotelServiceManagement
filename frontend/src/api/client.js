import axios from 'axios'
import { clearSession, getRefreshToken, readAuthResponse, saveSession } from '../utils/session'

const client = axios.create({
  baseURL: import.meta.env.VITE_API_URL || 'http://localhost:5000',
})

// Gắn JWT vào mọi request sau khi login (localStorage key: token)
client.interceptors.request.use((config) => {
  const token = localStorage.getItem('token')
  if (token) config.headers.Authorization = `Bearer ${token}`
  return config
})

const goToLogin = () => {
  clearSession()
  if (window.location.pathname !== '/login') window.location.href = '/login'
}

// Khoá đồng bộ theo hàng đợi subscriber (không phải chỉ 1 biến Promise dùng chung) - nhiều request
// 401 đến gần như cùng lúc (VD load nhiều panel song song khi access token vừa hết hạn) chỉ được
// kích hoạt đúng 1 lần gọi /refresh; các request đến SAU trong lúc đang refresh phải xếp hàng chờ
// đúng kết quả đó thay vì tự đọc lại refreshToken và gọi /refresh của riêng mình - refresh token bị
// xoay vòng (vô hiệu hoá) sau mỗi lần dùng nên gọi /refresh trùng nhau chắc chắn có lần bị 401 oan.
let isRefreshing = false
let pendingRequests = []

const onRefreshSettled = (newToken, error) => {
  const queued = pendingRequests
  pendingRequests = []
  isRefreshing = false
  queued.forEach((cb) => cb(newToken, error))
}

const startRefresh = () => {
  const refreshToken = getRefreshToken()
  if (!refreshToken) {
    onRefreshSettled(null, new Error('No refresh token available'))
    return
  }

  // Dùng axios gốc (không phải client) để tránh vướng chính 2 interceptor này - request interceptor
  // gắn access token cũ (vô nghĩa với /refresh) và response interceptor tự gọi lại refresh nếu 401.
  axios
    .post(`${client.defaults.baseURL}/api/auth/refresh`, { refreshToken })
    .then((res) => {
      const { token, refreshToken: newRefreshToken, user } = readAuthResponse(res.data)
      if (!token) throw new Error('Refresh response missing token')
      saveSession(token, newRefreshToken, user)
      onRefreshSettled(token, null)
    })
    .catch((err) => onRefreshSettled(null, err))
}

// Access token hết hạn giữa phiên (401) -> tự làm mới trong nền rồi thử lại đúng request đó, người
// dùng không hề hay biết. Chỉ khi refresh token cũng hết hạn/bị thu hồi mới thực sự đăng xuất.
client.interceptors.response.use(
  (res) => res,
  (err) => {
    const originalRequest = err.config
    const isAuthEndpoint = originalRequest?.url?.includes('/api/auth/login') || originalRequest?.url?.includes('/api/auth/refresh')

    if (err.response?.status !== 401 || isAuthEndpoint || originalRequest?._retriedAfterRefresh) {
      if (err.response?.status === 401 && !isAuthEndpoint) goToLogin()
      return Promise.reject(err)
    }

    originalRequest._retriedAfterRefresh = true

    const retryWithNewToken = new Promise((resolve, reject) => {
      pendingRequests.push((newToken, refreshErr) => {
        if (refreshErr) return reject(refreshErr)
        originalRequest.headers.Authorization = `Bearer ${newToken}`
        resolve(client(originalRequest))
      })
    })

    if (!isRefreshing) {
      isRefreshing = true
      startRefresh()
    }

    return retryWithNewToken.catch((refreshErr) => {
      goToLogin()
      return Promise.reject(refreshErr)
    })
  },
)

// Backend chưa chạy / endpoint chưa code (network error hoặc 404) -> cho phép fallback mock.
// Còn 4xx/5xx thật thì KHÔNG được che bằng mock - phải hiện lỗi cho người dùng biết.
export const isBackendMissing = (err) => !err.response || err.response.status === 404

// Thông báo lỗi chung cho các trang: backend chưa chạy -> báo mất kết nối; lỗi thật -> hiện message máy chủ.
export const apiError = (err) =>
  isBackendMissing(err)
    ? 'Không kết nối được máy chủ. Vui lòng thử lại sau.'
    : err.response?.data?.message ?? 'Máy chủ báo lỗi. Thử lại sau ít phút.'

export default client
