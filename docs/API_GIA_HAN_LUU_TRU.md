# API cần bổ sung — Gia hạn lưu trú (ở thêm)

> Frontend đã dựng sẵn (trang Check-in/Check-out, tab **Đang ở** → nút **Gia hạn**). Hiện gọi endpoint dưới
> đây; backend chưa có nên FE báo lỗi rõ ("Máy chủ chưa hỗ trợ gia hạn…"), **không giả vờ thành công**.
> Khi backend làm xong đúng contract này thì tính năng tự chạy, FE không phải sửa.

## Vì sao cần endpoint mới (không tái dùng được cái có sẵn)

- `PUT /api/reservations/{id}` **không dùng được**: `ReservationService.UpdateAsync` chặn
  `"Cancelled, checked-in or completed reservation cannot be updated"` và
  `"Reservation that already has a stay cannot be updated"` → không sửa được đơn đang ở.
- `StaysController` hiện chỉ có check-in / check-out / GET active — **không có** đường gia hạn.

## Endpoint

```
PATCH /api/stays/{id}/extend
[Authorize(Roles = "Admin,Manager,Receptionist")]
```

### Request body

```jsonc
{
  "extendType": "Hour" | "HalfDay" | "Day",  // loại gia hạn
  "amount": 1                                  // số lượng (vd 2 giờ, 1 ngày)
}
```

FE gửi đúng 3 mức: `{Hour,1}`, `{HalfDay,1}`, `{Day,1}` (xem `EXTEND_OPTIONS` trong
`frontend/src/pages/CheckInOutPage.jsx`). Backend nên chấp nhận `amount` bất kỳ ≥ 1 để linh hoạt.

### Xử lý phía backend (đề xuất)

1. Load `Stay` theo `id`, phải đang `StayStatus.Active` — không thì trả 409 "Lượt lưu trú không còn hoạt động".
2. Tính mốc trả mới:
   - `Hour`  → cộng `amount` giờ vào giờ trả dự kiến hiện tại.
   - `HalfDay` → cộng `amount × 12` giờ (hoặc nửa đơn giá đêm — tuỳ chính sách).
   - `Day`   → cộng `amount` ngày.
   - Mốc trả dự kiến hiện đang lấy từ `Reservation.CheckOutDate` (FE map là `plannedCheckOut`).
     → Cần một chỗ lưu mốc trả mới. Hai cách:
     - (a) Cập nhật `Reservation.CheckOutDate` (đơn giản nhất, khớp cách check-out tính số đêm), **hoặc**
     - (b) Thêm cột `Stay.PlannedCheckOut` (chuẩn hơn, tách khỏi Reservation) — cần migration.
3. Tính **phí gia hạn** theo loại phòng (đây là lý do làm ở BE thay vì FE):
   - Cần đơn giá theo giờ / nửa ngày. Gợi ý thêm field `RoomType.HourlyRate` (+ migration), hoặc suy ra
     từ `BasePrice` (vd giờ = `BasePrice / 24`, nửa ngày = `BasePrice / 2`, ngày = `BasePrice`).
4. Ghi nhận khoản phí để **cộng vào hoá đơn lúc check-out**. Tái dùng cơ chế `Surcharge` sẵn có là gọn nhất:
   tạo 1 dòng `Surcharge` (hoặc field riêng `Invoice.ExtraStayCharge` + migration). Nếu dùng Surcharge thì
   check-out hiện tại tự cộng vào `Invoice.SurchargeAmount` — FE đã hiển thị.
5. Phòng vẫn giữ `RoomStatus.Occupied`, Stay vẫn `Active` (KHÔNG check-out).

### Response (đề xuất)

```jsonc
{
  "stayId": 1,
  "newPlannedCheckOut": "2026-07-21T18:00:00",  // để FE cập nhật cột "Trả dự kiến"
  "extraCharge": 150000                          // phí gia hạn vừa ghi nhận
}
```

FE hiện chỉ cần biết gọi thành công (2xx) để toast + reload danh sách đang ở; các field trên dùng để
hiển thị đẹp hơn, có cũng tốt.

## Kiểm thử nhanh sau khi làm xong

1. Đăng nhập lễ tân → Check-in/Check-out → tab **Đang ở** → **Gia hạn** một phòng → chọn "Cả ngày" → Gia hạn.
2. Cột "Trả dự kiến" của phòng đó lùi ra 1 ngày; badge "Quá giờ" (nếu có) biến mất.
3. Check-out phòng đó → hoá đơn có thêm khoản phí gia hạn.
