# Dashboard — API còn thiếu

Trang **Tổng quan** (`/dashboard`, `frontend/src/pages/DashboardPage.jsx`) gọi `GET /api/reports/dashboard`.
Response hiện tại (`DashboardReportResponse.cs`) chỉ đủ cho 4 ô KPI + tổng doanh thu:

```csharp
public class DashboardReportResponse
{
    public int TotalRooms { get; set; }
    public int AvailableRooms { get; set; }
    public int ReservedRooms { get; set; }
    public int OccupiedRooms { get; set; }
    public int TodayBookings { get; set; }
    public int ActiveStays { get; set; }
    public decimal TotalRevenue { get; set; }
}
```

3 khối "vận hành" bên dưới KPI (khách đến/đi hôm nay, doanh thu 7 ngày, cảnh báo) chưa có dữ liệu thật —
FE hiện đang **ẩn hẳn** các khối này khi dùng dữ liệu thật (chỉ hiện khi ở chế độ dữ liệu mẫu), kèm ghi chú
"API vận hành chưa hỗ trợ" ngay dưới ô KPI.

## 1. Khách đến hôm nay (arrivals)

**Field cần thêm:** `arrivals: ArrivalItem[]`

```csharp
public class ArrivalItem
{
    public string BookingCode { get; set; }
    public string GuestName { get; set; }
    public string RoomNumber { get; set; }
    public string TypeName { get; set; }
    public string Eta { get; set; }   // giờ dự kiến nhận phòng, vd "14:00"
}
```

**Nguồn dữ liệu:** `Reservations` có `CheckInDate` = hôm nay **và** `Status = Confirmed`.
Join `Room` (lấy `RoomNumber`) + `RoomType` (lấy `TypeName`) + `Guest` (lấy `GuestName`).

**FE dùng ở:** `DashboardPage.jsx` — panel "Khách đến hôm nay", nút "Check-in ngay" → `/checkin-checkout`.

## 2. Khách trả phòng hôm nay (departures)

**Field cần thêm:** `departures: DepartureItem[]`

```csharp
public class DepartureItem
{
    public string BookingCode { get; set; }
    public string GuestName { get; set; }
    public string RoomNumber { get; set; }
    public int Nights { get; set; }
    public decimal AmountDue { get; set; }   // tạm tính, chưa cần chính xác tuyệt đối
}
```

**Nguồn dữ liệu:** `Stays` có `PlannedCheckOut` = hôm nay **và** `Status = Active`.
`Nights` tính từ `Reservation.CheckInDate` → `PlannedCheckOut`. `AmountDue` tạm tính = giá phòng × số đêm +
dịch vụ đã dùng (có thể tái dùng logic đang có trong `InvoiceService`/`StayService` khi check-out).

**FE dùng ở:** `DashboardPage.jsx` — panel "Khách trả phòng hôm nay", nút "Check-out" → `/checkin-checkout`.

## 3. Doanh thu 7 ngày gần nhất (revenue7d)

**Field cần thêm:** `revenue7d: RevenueDayItem[]` (đúng 7 phần tử, từ 6 ngày trước → hôm nay)

```csharp
public class RevenueDayItem
{
    public string Day { get; set; }     // nhãn ngắn, vd "T2", "T3"... "CN"
    public decimal Amount { get; set; } // tổng payment đã thu trong ngày đó
}
```

**Nguồn dữ liệu:** Group `Payments` (`Status = Completed`) theo ngày (`PaymentDate.Date`), 7 ngày gần nhất kể
cả hôm nay. Tương tự cách `GetRevenueAsync` đang tính `PaymentRevenue`, chỉ khác là group theo từng ngày
thay vì gộp cả kỳ.

**FE dùng ở:** `DashboardPage.jsx` — biểu đồ cột "Doanh thu 7 ngày gần nhất".

## 4. Cần chú ý (alerts)

**Field cần thêm:** `alerts: AlertItem[]`

```csharp
public class AlertItem
{
    public int Id { get; set; }
    public string Text { get; set; }     // vd "Phòng 102 quá giờ check-out 25 phút"
    public string Action { get; set; }   // nhãn nút, vd "Check-out"
    public string To { get; set; }       // route FE điều hướng tới, vd "/checkin-checkout"
}
```

**Chưa làm được ngay** — khác 3 mục trên (chỉ là thiếu field), mục này còn thiếu **business rule**, cần
thống nhất trước khi code:

- Quá giờ check-out bao lâu thì tính là cảnh báo? (vd `PlannedCheckOut` đã qua > 15 phút mà `Stay.Status`
  vẫn `Active`)
- Dọn phòng "lâu hơn dự kiến" — hệ thống hiện có lưu thời điểm phòng chuyển sang `Cleaning` không? Ngưỡng
  bao nhiêu phút thì cảnh báo?
- Booking chưa xác nhận — `Reservation.Status = Pending` quá bao lâu thì lên cảnh báo?

**FE dùng ở:** `DashboardPage.jsx` — dải "Cần chú ý" màu vàng ngay dưới ô KPI.

## Ghi chú

- 3 mục đầu (arrivals/departures/revenue7d) dữ liệu đã có sẵn trong DB (`Reservations`, `Stays`,
  `Payments`), chỉ cần thêm query + map — không cần bảng/migration mới.
- Mục "alerts" cần chốt rule nghiệp vụ trước khi code, không chỉ là thiếu field.
- FE đã chuẩn bị sẵn UI cho cả 4 field này (ẩn có điều kiện khi field không tồn tại), thêm field vào
  response là hiện ra ngay, không cần đổi gì bên FE.
