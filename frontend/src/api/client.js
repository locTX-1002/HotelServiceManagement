import axios from 'axios'

const client = axios.create({
  baseURL: import.meta.env.VITE_API_URL || 'http://localhost:5000',
})

// Gắn JWT vào mọi request sau khi login (localStorage key: token)
client.interceptors.request.use((config) => {
  const token = localStorage.getItem('token')
  if (token) config.headers.Authorization = `Bearer ${token}`
  return config
})

// Token hết hạn / không hợp lệ -> quay về trang login
client.interceptors.response.use(
  (res) => res,
  (err) => {
    if (err.response?.status === 401 && window.location.pathname !== '/login') {
      localStorage.removeItem('token')
      window.location.href = '/login'
    }
    return Promise.reject(err)
  },
)

// Backend chưa chạy / endpoint chưa code (network error hoặc 404) -> cho phép fallback mock.
// Còn 4xx/5xx thật thì KHÔNG được che bằng mock - phải hiện lỗi cho người dùng biết.
export const isBackendMissing = (err) => !err.response || err.response.status === 404

export default client
