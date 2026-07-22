# Bàn giao thay đổi backend cho frontend

Ngày cập nhật: 22/07/2026
Nhánh triển khai: `be/phat`

## Việc frontend cần cập nhật

### 1. Trạng thái yêu cầu dọn phòng

Backend đã thêm `Cancelled` vào cuối enum. API hiện trả enum dưới dạng số, vì vậy cập nhật:

```js
const HOUSEKEEPING_STATUS_ORDER = ['Pending', 'Acknowledged', 'Completed', 'Cancelled']
```

Ánh xạ: `Pending = 0`, `Acknowledged = 1`, `Completed = 2`, `Cancelled = 3`.

### 2. Khách xem yêu cầu dọn phòng đã gửi

```http
GET /api/guest/me/housekeeping-requests
Authorization: Bearer <guest-token>
```

Trả về mảng yêu cầu thuộc lượt lưu trú đang hoạt động của chính khách, mới nhất trước. Mỗi phần tử có: `id`, `stayId`, `bookingCode`, `roomNumber`, `guestName`, `requestType`, `note`, `status`, `requestedAt`, `handledAt`, `handledByUserName`.

### 3. Khách xem đơn dịch vụ đã đặt

```http
GET /api/guest/me/service-orders
Authorization: Bearer <guest-token>
```

Trả về mảng đơn thuộc lượt lưu trú đang hoạt động của chính khách. Mỗi đơn có `id`, `stayId`, `orderDate`, `status`, `totalAmount`, `details`; mỗi chi tiết có `serviceItemId`, `serviceName`, `quantity`, `unitPrice`, `subtotal`.

### 4. Khách tự hủy đặt phòng

```http
PATCH /api/guest/me/reservations/{id}/cancel
Authorization: Bearer <guest-token>
```

- Chỉ hủy được đơn của tài khoản đang đăng nhập.
- Chỉ trạng thái `Pending` hoặc `Confirmed` được hủy.
- Đơn không thuộc khách được trả `404`; trạng thái không hợp lệ trả `409`.

### 5. Cập nhật riêng trạng thái phòng

```http
PATCH /api/rooms/{id}/status
Authorization: Bearer <staff-token>
Content-Type: application/json

{ "status": 0 }
```

Quyền: `Admin`, `Manager`, `Receptionist`, `ServiceStaff`.

Các chuyển đổi hợp lệ:

- `Cleaning (3) -> Available (0)`
- `Available (0) -> Cleaning (3)`
- `Available (0) <-> Maintenance (4)` chỉ dành cho `Admin`/`Manager`

Không dùng endpoint này để đặt `Reserved` hoặc `Occupied`. Khi hoàn tất yêu cầu loại `Cleaning`, backend cũng tự chuyển phòng từ `Cleaning` về `Available`.

### 6. Nhân viên xử lý yêu cầu dọn phòng

`ServiceStaff` hiện gọi được toàn bộ controller này:

```http
GET   /api/housekeeping-requests
GET   /api/housekeeping-requests?includeCompleted=true
PATCH /api/housekeeping-requests/{id}/acknowledge
PATCH /api/housekeeping-requests/{id}/complete
PATCH /api/housekeeping-requests/{id}/cancel
```

Mặc định `GET` chỉ trả yêu cầu đang mở. `includeCompleted=true` trả cả lịch sử hoàn tất/hủy.

### 7. Hủy hóa đơn

```http
PATCH /api/invoices/{id}/cancel
Authorization: Bearer <admin-or-manager-token>
```

Chỉ `Admin`/`Manager` được gọi. Hóa đơn đã có khoản thanh toán `Completed` sẽ bị chặn với `409`.

### 8. Email đăng ký

Trường `email` trong request đăng ký khách giờ là bắt buộc và phải đúng định dạng email. Frontend cần hiển thị trường này là bắt buộc và xử lý lỗi validation `400`.

## Phạm vi chưa triển khai

BE-8 (void/hoàn khoản thanh toán đã ghi nhận) chưa được triển khai vì yêu cầu nguồn xác định đây là giới hạn có thể chấp nhận của đồ án và chưa chốt quy tắc kế toán. Không xóa payment để sửa sai vì sẽ mất dấu vết kiểm toán.
