# Backend handoff: Dashboard và Phụ thu / Đền bù

Ngày cập nhật: 17/07/2026  
Nhánh triển khai: `be/phat`

## 1. Trạng thái

Backend đã triển khai các contract frontend yêu cầu:

- Dashboard trả thêm `arrivals`, `departures`, `revenue7d`, `alerts`.
- CRUD danh mục phụ thu: GET, POST, PUT.
- Check-out nhận danh sách phụ thu và trả `totalSurchargeCharges`.
- Hóa đơn trả `surchargeAmount` và `surcharges[]`.
- Tổng hóa đơn và khuyến mãi đã tính cả phụ thu.
- Migration tạo hai bảng phụ thu, thêm cột hóa đơn và seed 8 món mẫu.

Backend build thành công, không có warning/error. Toàn bộ 11 test hiện có đều pass.

## 2. Migration cần chạy

Migration mới:

```text
20260717135244_AddSurchargesAndDashboardSupport
```

Chạy từ thư mục `backend`:

```bash
dotnet tool restore
dotnet tool run dotnet-ef database update \
  --project HotelServiceManagement.Infrastructure \
  --startup-project HotelServiceManagement.Api
```

Migration thực hiện:

- Thêm bảng `SurchargeItems`.
- Thêm bảng `Surcharges`.
- Thêm `Invoices.SurchargeAmount decimal(18,2)` mặc định `0`.
- Seed 8 món phụ thu mẫu.

## 3. Dashboard

### GET `/api/reports/dashboard`

Yêu cầu đăng nhập. Response có cấu trúc:

```json
{
  "totalRooms": 20,
  "availableRooms": 8,
  "reservedRooms": 4,
  "occupiedRooms": 8,
  "todayBookings": 3,
  "activeStays": 8,
  "totalRevenue": 12500000,
  "arrivals": [
    {
      "bookingCode": "BK-001",
      "guestName": "Nguyễn Văn A",
      "roomNumber": "102",
      "typeName": "Deluxe",
      "eta": "14:00"
    }
  ],
  "departures": [
    {
      "bookingCode": "BK-002",
      "guestName": "Trần Thị B",
      "roomNumber": "205",
      "nights": 2,
      "amountDue": 1350000
    }
  ],
  "revenue7d": [
    { "day": "T6", "amount": 0 },
    { "day": "T7", "amount": 850000 },
    { "day": "CN", "amount": 1200000 },
    { "day": "T2", "amount": 0 },
    { "day": "T3", "amount": 950000 },
    { "day": "T4", "amount": 2100000 },
    { "day": "T5", "amount": 1750000 }
  ],
  "alerts": []
}
```

Quy tắc dữ liệu:

- `arrivals`: reservation có `CheckInDate` hôm nay và trạng thái `Confirmed`.
- `eta`: dùng giờ trong `CheckInDate`; nếu thời gian là `00:00` thì trả giờ check-in mặc định `14:00`.
- `departures`: stay `Active` có `Reservation.CheckOutDate` hôm nay.
- `amountDue`: tạm tính tiền phòng theo số đêm cộng các service order không bị hủy; chưa trừ tiền cọc/khuyến mãi.
- `revenue7d`: luôn đúng 7 phần tử, từ 6 ngày trước đến hôm nay; ngày không có payment trả `amount: 0`.
- Doanh thu ngày lấy payment có trạng thái `Completed`.
- `alerts` hiện trả mảng rỗng vì chưa có rule nghiệp vụ và timestamp cần thiết. FE có thể giữ logic ẩn panel khi mảng rỗng.

## 4. Danh mục phụ thu

### GET `/api/surcharge-items`

Quyền: mọi tài khoản đã đăng nhập.

```json
[
  {
    "id": 1,
    "name": "Khăn tắm",
    "unitPrice": 80000,
    "unit": "cái",
    "isActive": true
  }
]
```

### POST `/api/surcharge-items`

Quyền: `Admin`, `Manager`.

```json
{
  "name": "Khăn tắm",
  "unit": "cái",
  "unitPrice": 80000,
  "isActive": true
}
```

Response `201 Created`, body là item vừa tạo có `id`.

### PUT `/api/surcharge-items/{id}`

Quyền: `Admin`, `Manager`. Body giống POST. Dùng PUT với `isActive: false` để ngừng sử dụng; không có DELETE.

Validation:

- `name` bắt buộc, tối đa 100 ký tự và không được trùng.
- `unit` bắt buộc, tối đa 20 ký tự.
- `unitPrice` phải lớn hơn 0.

## 5. Check-out kèm phụ thu

### POST `/api/stays/{stayId}/check-out`

Quyền: `Admin`, `Manager`, `Receptionist`.

Không có phụ thu:

```json
{}
```

Có phụ thu:

```json
{
  "surcharges": [
    { "surchargeItemId": 4, "quantity": 2 },
    { "surchargeItemId": 1, "quantity": 1 }
  ]
}
```

Response:

```json
{
  "stayId": 10,
  "actualCheckOut": "2026-07-17T08:30:00Z",
  "totalRoomCharges": 1200000,
  "totalServiceCharges": 95000,
  "totalSurchargeCharges": 480000,
  "totalAmount": 1775000,
  "isSuccess": true,
  "message": "Check-out successful. Invoice has been generated."
}
```

Backend tự đọc giá hiện tại trong `SurchargeItems`, lưu `UnitPriceSnapshot`, tính `Subtotal` và không nhận giá từ frontend.

Request bị từ chối nếu:

- Item không tồn tại.
- Item đã `isActive: false`.
- `quantity <= 0`.
- Một `surchargeItemId` xuất hiện nhiều lần trong cùng request.

## 6. Hóa đơn

Áp dụng cho:

- GET `/api/invoices/{id}`
- GET `/api/invoices/stay/{stayId}`
- POST `/api/invoices/stay/{stayId}`

Response bổ sung:

```json
{
  "invoiceId": 20,
  "stayId": 10,
  "invoiceDate": "2026-07-17T08:30:00Z",
  "roomCharge": 1200000,
  "serviceCharge": 95000,
  "surchargeAmount": 120000,
  "surcharges": [
    {
      "name": "Khăn tắm",
      "quantity": 1,
      "unitPrice": 80000,
      "subtotal": 80000
    },
    {
      "name": "Dép đi trong phòng",
      "quantity": 1,
      "unitPrice": 40000,
      "subtotal": 40000
    }
  ],
  "discountAmount": 0,
  "promotionCode": null,
  "totalAmount": 1415000,
  "status": "Unpaid"
}
```

Công thức:

```text
subtotal = roomCharge + serviceCharge + surchargeAmount
totalAmount = subtotal - discountAmount
```

Khi gọi lại POST invoice để áp promotion, backend vẫn tải các dòng phụ thu và tính discount trên subtotal có phụ thu; tiền phụ thu không bị mất khỏi hóa đơn.

## 7. Tên field frontend cần giữ chính xác

| Màn hình | Field |
|---|---|
| Biên nhận ngay sau check-out | `totalSurchargeCharges` |
| Trang hóa đơn | `surchargeAmount` |
| Chi tiết hóa đơn | `surcharges[]` |
| Dashboard | `arrivals`, `departures`, `revenue7d`, `alerts` |

Không cần frontend gửi `unitPrice`, `subtotal`, `createdByUserId` hoặc `createdAt` khi check-out.
