// Kenh bao "du lieu vua thay doi" giua cac trang va chuong thong bao. Chuong poll 15s/lan nen
// sau khi le tan Xac nhan/Huy/Check-in..., cham do co the hien sai toi 15s - nguoi dung tuong
// thao tac khong an. Trang phat tin hieu qua day de chuong refetch ngay lap tuc.
const EVENT = 'hsms:data-changed'

export const notifyDataChanged = () => window.dispatchEvent(new Event(EVENT))

export const onDataChanged = (handler) => {
  window.addEventListener(EVENT, handler)
  return () => window.removeEventListener(EVENT, handler)
}
