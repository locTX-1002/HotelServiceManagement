import axios from 'axios'
import { clearGuestSession, getGuestRefreshToken, getGuestToken, saveGuestSession } from '../utils/guestSession'

// Axios instance RIÊNG cho guest portal - tách khỏi api/client.js của nhân viên nên 2 interceptor
// không dẫm chân nhau, và lỗi 401 ở đây không bao giờ vô tình xoá phiên/điều hướng nhân viên.
const guestClient = axios.create({
  baseURL: import.meta.env.VITE_API_URL || 'http://localhost:5000',
})

guestClient.interceptors.request.use((config) => {
  const token = getGuestToken()
  if (token) config.headers.Authorization = `Bearer ${token}`
  return config
})

const goToGuestLogin = () => {
  clearGuestSession()
  if (window.location.pathname !== '/guest/dang-nhap') window.location.href = '/guest/dang-nhap'
}

// Cùng pattern subscriber-queue đã dùng cho api/client.js - chỉ 1 lần gọi /refresh dù nhiều request
// 401 gần như đồng thời.
let isRefreshing = false
let pendingRequests = []

const onRefreshSettled = (newToken, error) => {
  const queued = pendingRequests
  pendingRequests = []
  isRefreshing = false
  queued.forEach((cb) => cb(newToken, error))
}

const startRefresh = () => {
  const refreshToken = getGuestRefreshToken()
  if (!refreshToken) {
    onRefreshSettled(null, new Error('No refresh token available'))
    return
  }

  axios
    .post(`${guestClient.defaults.baseURL}/api/guest/auth/refresh`, { refreshToken })
    .then((res) => {
      if (!res.data?.accessToken) throw new Error('Refresh response missing token')
      saveGuestSession(res.data)
      onRefreshSettled(res.data.accessToken, null)
    })
    .catch((err) => onRefreshSettled(null, err))
}

guestClient.interceptors.response.use(
  (res) => res,
  (err) => {
    const originalRequest = err.config
    const isAuthEndpoint = originalRequest?.url?.includes('/api/guest/auth/')

    if (err.response?.status !== 401 || isAuthEndpoint || originalRequest?._retriedAfterRefresh) {
      if (err.response?.status === 401 && !isAuthEndpoint) goToGuestLogin()
      return Promise.reject(err)
    }

    originalRequest._retriedAfterRefresh = true

    const retryWithNewToken = new Promise((resolve, reject) => {
      pendingRequests.push((newToken, refreshErr) => {
        if (refreshErr) return reject(refreshErr)
        originalRequest.headers.Authorization = `Bearer ${newToken}`
        resolve(guestClient(originalRequest))
      })
    })

    if (!isRefreshing) {
      isRefreshing = true
      startRefresh()
    }

    return retryWithNewToken.catch((refreshErr) => {
      goToGuestLogin()
      return Promise.reject(refreshErr)
    })
  },
)

export const isBackendMissing = (err) => !err.response || err.response.status === 404
export const apiError = (err) =>
  isBackendMissing(err)
    ? 'Không kết nối được máy chủ. Vui lòng thử lại sau.'
    : err.response?.data?.message ?? 'Máy chủ báo lỗi. Thử lại sau ít phút.'

export default guestClient
