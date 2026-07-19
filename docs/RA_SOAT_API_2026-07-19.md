# Rà soát API — 19/07/2026

Đối chiếu toàn bộ endpoint backend với các lời gọi thật trong `frontend/src`.

| Chỉ số | Số lượng |
|---|---|
| Tổng endpoint backend (19 controller) | **82** |
| Đã được frontend gọi | **72** |
| Chưa được gọi | **10** |
| Lời gọi API trong frontend | 89 |

Kết luận nhanh: **không có endpoint nào bị bỏ quên do thiếu sót**. 10 endpoint chưa gọi đều cùng một loại (lấy chi tiết 1 bản ghi) và không cần thiết với cách frontend đang làm việc. Ngược lại, có **4 khoảng trống nghiệp vụ** cần cân nhắc bổ sung API.

---

## 1. Endpoint chưa được frontend gọi (10)

Tất cả đều là `GET /{id}` — lấy chi tiết một bản ghi:

| Endpoint | Controller |
|---|---|
| `GET /api/guests/{id}` | Guests |
| `GET /api/invoices/{id}` | Invoices |
| `GET /api/promotions/{id}` | Promotions |
| `GET /api/reservations/{id}` | Reservations |
| `GET /api/room-types/{id}` | RoomTypes |
| `GET /api/rooms/{id}` | Rooms |
| `GET /api/service-items/{id}` | ServiceItems |
| `GET /api/service-orders/{id}` | ServiceOrders |
| `GET /api/surcharge-items/{id}` | SurchargeItems |
| `GET /api/users/{id}` | Users |

**Vì sao chưa dùng:** mọi màn hình đều tải danh sách trước (`GET /api/rooms`, `GET /api/reservations`…) rồi giữ object trong state React. Khi mở form sửa hay mở drawer chi tiết, frontend lấy thẳng object đã có trong danh sách — không cần gọi lại server cho từng bản ghi. Riêng hoá đơn thì frontend dùng `GET /api/invoices/stay/{stayId}` (tra theo lượt lưu trú) thay vì tra theo id hoá đơn.

**Đề xuất: giữ nguyên, không xoá.** Đây là bộ CRUD chuẩn REST, dùng được khi test bằng Swagger/Postman, và sẽ cần tới nếu sau này làm trang chi tiết mở bằng link trực tiếp (ví dụ `/reservations/15` mở thẳng từ URL, lúc đó không có sẵn danh sách trong state).

---

## 2. API còn thiếu

### 2.1 Khách không tự huỷ được đặt phòng của mình — *nên bổ sung*

Cổng khách hiện có: đặt phòng (`POST /api/guest/reservations`), xem đơn của mình (`GET /api/guest/me/reservations`) — nhưng **không có endpoint huỷ**. Khách đặt nhầm ngày phải gọi điện nhờ lễ tân huỷ hộ.

Đề xuất: `PATCH /api/guest/me/reservations/{id}/cancel` — chỉ cho huỷ đơn thuộc chính tài khoản đó và đang ở trạng thái `Pending` hoặc `Confirmed`, tái dùng `ReservationService.CancelAsync` sẵn có.

### 2.2 Khách không xem được trạng thái yêu cầu dọn phòng — *nên bổ sung*

Có `POST /api/guest/me/housekeeping-requests` để gửi yêu cầu, nhưng không có `GET` tương ứng. Khách bấm "Gọi dọn phòng" xong không biết lễ tân đã tiếp nhận hay hoàn tất chưa, dễ bấm lại nhiều lần.

Đề xuất: `GET /api/guest/me/housekeeping-requests` trả về các yêu cầu của lượt lưu trú hiện tại kèm trạng thái.

### 2.3 Khách không xem được đơn dịch vụ đã đặt — *nên bổ sung*

Tương tự: có `POST /api/guest/me/service-orders` nhưng không có `GET`. Khách gọi đồ ăn xong không xem lại được đã gọi những gì, tạm tính bao nhiêu tiền — trong khi đây chính là thông tin họ sẽ phải trả lúc check-out.

Đề xuất: `GET /api/guest/me/service-orders`.

### 2.4 Trạng thái `InvoiceStatus.Cancelled` không có API nào set được — *cần quyết định*

Enum `InvoiceStatus` có 4 giá trị `Unpaid / PartiallyPaid / Paid / Cancelled`, nhưng:

- `InvoiceService` chỉ tính ra 3 giá trị đầu từ tổng số tiền đã thanh toán.
- `PaymentService.cs:61` chỉ **đọc** `Cancelled` để chặn thu tiền vào hoá đơn đã huỷ.
- Frontend cũng có nhánh `status !== 'Cancelled'` để ẩn nút thu tiền.

Nghĩa là cả hệ thống đã chuẩn bị sẵn cho hoá đơn bị huỷ, nhưng **không có đường nào tạo ra trạng thái đó**. Chọn một trong hai:

- Thêm `PATCH /api/invoices/{id}/cancel` (quyền Admin/Manager) cho tình huống xuất nhầm hoá đơn — hoặc
- Bỏ giá trị `Cancelled` khỏi enum và dọn các nhánh kiểm tra thừa ở cả hai phía.

> Lưu ý nếu bỏ: `InvoiceStatus` được lưu xuống DB dạng chuỗi nên xoá giá trị cuối danh sách là an toàn, không làm lệch dữ liệu cũ.

### 2.5 Không sửa/hoàn được khoản thanh toán đã ghi nhận — *ghi nhận, có thể để sau*

`PaymentsController` chỉ có `POST /api/payments`. Lễ tân gõ nhầm số tiền thì không có cách sửa hay hoàn (refund) trong hệ thống. Phạm vi đồ án có thể chấp nhận, nhưng nên nêu rõ giới hạn này khi báo cáo thay vì để người chấm tự phát hiện.

---

## 3. Những chỗ *không* phải thiếu API

Để khỏi bị hiểu nhầm khi rà lại:

- **Không có `DELETE` cho khuyến mãi / dịch vụ / phụ thu** — cố ý. Các danh mục này dùng cờ bật/tắt (`isActive`) qua `PUT`, để giữ lịch sử hoá đơn cũ đã tham chiếu tới chúng.
- **Không có `DELETE` cho người dùng** — cố ý. Tài khoản bị khoá qua `PATCH /api/users/{id}/status`, giữ lại lịch sử thao tác.
- **Không có API huỷ đơn dịch vụ riêng** — đã có, dùng chung `PATCH /api/service-orders/{id}` với trạng thái `Cancelled`.
- **`POST /api/auth/refresh` và `POST /api/guest/auth/refresh`** — có được gọi, nằm trong interceptor tự làm mới token ở `api/client.js` và `api/guestClient.js`.

---

## 4. Cách kiểm tra lại

Danh sách endpoint được bóc từ các attribute `[HttpGet/Post/Put/Patch/Delete]` trong `backend/HotelServiceManagement.Api/Controllers/*.cs`, đối chiếu với mọi lời gọi `client.*` / `guestClient.*` trong `frontend/src` (đã bỏ comment và chuẩn hoá tham số động `${id}` trước khi so khớp).

Kiểm tra nhanh một endpoint bất kỳ có được gọi hay không:

```bash
grep -rn "api/reservations" frontend/src --include="*.js*"
```
