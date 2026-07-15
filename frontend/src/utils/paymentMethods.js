// Danh sách phương thức thanh toán dùng chung - trước đây lặp lại trong InvoicePage,
// giờ tách ra để CreateReservationPage (form đặt cọc) dùng lại cùng danh sách.
export const PAYMENT_METHODS = [
  { value: 'Cash', label: 'Tiền mặt' },
  { value: 'BankTransfer', label: 'Chuyển khoản' },
  { value: 'Card', label: 'Thẻ' },
]
