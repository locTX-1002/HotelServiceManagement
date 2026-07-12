import axios from 'axios'
import { clearSession } from '../utils/session'

const client = axios.create({
  baseURL: import.meta.env.VITE_API_URL || 'http://localhost:5000',
})

// Gắn JWT vào mọi request sau khi login (localStorage key: token)
client.interceptors.request.use((config) => {
  const token = localStorage.getItem('token')
  if (token) config.headers.Authorization = `Bearer ${token}`
  return config
})

// Token hết hạn / không hợp lệ -> dọn phiên rồi quay về trang login
client.interceptors.response.use(
  (res) => res,
  (err) => {
    if (err.response?.status === 401 && window.location.pathname !== '/login') {
      clearSession()
      window.location.href = '/login'
    }
    return Promise.reject(err)
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
