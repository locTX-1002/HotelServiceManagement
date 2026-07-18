// Phiên đăng nhập của KHÁCH (guest portal) - tách hoàn toàn khỏi utils/session.js của nhân viên,
// dùng key localStorage riêng để 1 máy có thể vừa đăng nhập nhân viên vừa đăng nhập khách cùng lúc
// (demo trên cùng trình duyệt) mà không đá phiên của nhau.
export const getGuestToken = () => localStorage.getItem('guestToken')
export const getGuestRefreshToken = () => localStorage.getItem('guestRefreshToken')

export const getGuest = () => {
  try {
    return JSON.parse(localStorage.getItem('guest'))
  } catch {
    return null
  }
}

export const saveGuestSession = (data) => {
  localStorage.setItem('guestToken', data.accessToken)
  localStorage.setItem('guestRefreshToken', data.refreshToken)
  localStorage.setItem('guest', JSON.stringify({
    guestId: data.guestId,
    fullName: data.fullName,
    phoneNumber: data.phoneNumber,
  }))
}

export const clearGuestSession = () => {
  localStorage.removeItem('guestToken')
  localStorage.removeItem('guestRefreshToken')
  localStorage.removeItem('guest')
}
