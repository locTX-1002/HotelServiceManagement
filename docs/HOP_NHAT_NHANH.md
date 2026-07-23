# Hợp nhất `loc/wpf-app` với `develop` — cần Phát chốt giúp

> Gửi Phát. Ngày 23/07/2026.
> Không phải tranh code, chỉ là hai nhánh đi song song nên có chỗ trùng, cần
> thống nhất giữ bản nào rồi ghép lại cho gọn.

## 1. Chuyện đã xảy ra

Hai nhánh cùng chạy một lúc mà không ai biết bên kia làm gì:

| Nhánh | Người | Nội dung |
|---|---|---|
| `develop` | Phát | Toàn bộ tầng service backend — 14 interface, có cả Hoá đơn, Thanh toán, Khuyến mãi, Dịch vụ, Housekeeping, Báo cáo, Người dùng |
| `loc/wpf-app` | Lộc | Toàn bộ giao diện — Trang chủ, Khách hàng, Đặt phòng, Lịch phòng, Check-in/Check-out (hơn `develop` 29 commit) |

Hai bên **bổ sung cho nhau là chính**. Chỉ có 4 chuỗi bị trùng vì cả hai cùng
cần chúng để làm phần của mình.

## 2. Việc gấp trước đã: file phân công đang sai

File `PHAN_CONG_FRONTEND_MOI.md` ghi phần frontend còn thiếu gồm **Khách hàng,
Đặt phòng, Check-in/check-out** và giao cho Phúc.

Ba màn đó đã làm xong rồi, đang nằm trên `loc/wpf-app`. File đó soi trạng thái
từ `develop` nên không thấy 29 commit chưa merge.

Kiểm chứng bằng lệnh:

```bash
git ls-tree -r --name-only origin/develop | grep "Views/"
```

```bash
git ls-tree -r --name-only origin/loc/wpf-app | grep "Views/"
```

**Nhờ mọi người nhắn Phúc dừng ba màn đó lại trước khi bạn ấy bắt tay vào.**
Phần backend trong file phân công thì đúng, không phải sửa.

## 3. Bốn chuỗi bị trùng

Cả hai bản đều chạy được, chỉ khác chữ ký và khác thứ có sẵn.

### IReservationService

| Bản Lộc | Bản Phát |
|---|---|
| `GetAllAsync()` | `GetAllAsync()` |
| **`GetAvailableRoomsAsync(checkIn, checkOut)`** | — |
| `CreateAsync(...)` | — |
| `UpdateAsync(...)` | — |
| `ConfirmAsync(id)` | `ConfirmAsync(id)` |
| `CancelAsync(id)` | `CancelAsync(id)` |
| `NoShowAsync(id)` | — |

`GetAvailableRoomsAsync` là hàm tìm phòng trống theo khoảng ngày. Ba màn đang
gọi nó: Đặt phòng, tab Lịch phòng, và thanh tra cứu ở Trang chủ.

### IStayService

| Bản Lộc | Bản Phát |
|---|---|
| **`GetArrivalsAsync()`** | — |
| `GetActiveAsync()` | `GetActiveAsync()` |
| `CheckInAsync(reservationId, userId)` | `CheckInAsync(reservationId, actualCheckIn?)` |
| `CheckOutAsync(stayId, userId)` | `CheckOutAsync(stayId, actualCheckOut?)` |
| **`ExtendAsync(stayId, newCheckOut)`** | — |

`GetArrivalsAsync` là danh sách khách cần check-in hôm nay. `ExtendAsync` là gia
hạn khi khách muốn ở thêm.

### IGuestService

| Bản Lộc | Bản Phát |
|---|---|
| `SearchAsync(keyword)` | `SearchAsync(keyword)` |
| — | `GetAllAsync()` |
| `GetByIdAsync(id)` | — |
| **`FindExactAsync(cccdHoacSdt)`** | — |
| `CreateAsync(...)` / `UpdateAsync(...)` | — |
| — | **`DeleteAsync(id)`** |

`FindExactAsync` dùng ở bước đầu của dialog đặt phòng: gõ CCCD hoặc số điện
thoại để tra đúng một khách, không ra thì hiện form tạo khách mới.

### ISurchargeService

| Bản Lộc | Bản Phát |
|---|---|
| `GetActiveItemsAsync()` | `GetItemsAsync()` |
| — | **`SaveItemAsync(...)`** — CRUD danh mục phụ thu |
| `GetForStayAsync(stayId)` | `GetByStayAsync(id)` |
| **`GetTotalsAsync(stayIds)`** | — |
| `AddAsync(stayId, itemId, qty, userId)` | `AddToStayAsync(stayId, itemId, qty)` |
| — | **`UpdateAsync(id, quantity)`** — có chặn khi hoá đơn đã thanh toán |
| `RemoveAsync(id)` | `DeleteAsync(id)` |

## 4. Điểm mấu chốt: 8 chuỗi mới của Phát không bị ảnh hưởng

Đã kiểm tra phụ thuộc của từng service mới:

```
InvoiceService              → IInvoiceRepository, IPromotionRepository
PaymentService              → IPaymentRepository
ReportService               → IReportRepository
ServiceOrderService         → IServiceCatalogRepository, IServiceOrderRepository
HousekeepingRequestService  → IHousekeepingRequestRepository
```

Không cái nào đụng tới Guest, Reservation, Stay hay Surcharge. Nghĩa là **8
chuỗi mới vào được nguyên vẹn dù chốt phương án nào** — đây là phần lớn công của
Phát và nó không bị đe doạ.

## 5. Ba phương án

### A. Giữ bản Lộc cho 4 chuỗi, lấy trọn 8 chuỗi mới của Phát

App chạy được ngay sau khi merge vì 9 ViewModel đang viết theo chữ ký đó.

Lộc sẽ **bổ sung những thứ hay của bản Phát vào bản mình**, không bỏ ý tưởng:

- `SaveItemAsync` — CRUD danh mục phụ thu
- `DeleteAsync` cho khách hàng
- Quy tắc **chặn sửa/xoá phụ thu khi hoá đơn đã thanh toán** — bản Lộc chưa có,
  đây là chỗ Phát nghĩ đúng hơn

Cái mất: phần code 4 chuỗi của Phát không được dùng.

### B. Giữ bản Phát, Lộc viết lại giao diện

Sửa khoảng 25 điểm gọi trong 9 ViewModel. Nhưng vẫn phải thêm
`GetAvailableRoomsAsync`, `GetArrivalsAsync`, `ExtendAsync`, `FindExactAsync`
vào bản Phát, vì thiếu chúng thì mất hẳn chức năng chứ không phải chỉ đổi tên hàm.

Tốn thời gian hơn hẳn mà đích đến gần như giống phương án A.

### C. Trộn từng chuỗi một

Ví dụ giữ Reservation và Stay của Lộc, lấy Surcharge của Phát. Linh hoạt nhất
nhưng phải sửa cả hai bên và dễ sót, nên chỉ nên làm nếu Phát thấy có chuỗi cụ
thể nào bản mình rõ ràng tốt hơn.

## 6. Đề xuất

Phương án **A**, vì lý do kỹ thuật chứ không phải vì bản ai hay hơn: giao diện
đã viết theo chữ ký đó rồi, và bản Lộc có sẵn ba hàm mà thiếu chúng thì app mất
chức năng thật. Đổi lại Lộc nhận việc bê các quy tắc tốt của Phát sang.

Nếu Phát thấy phương án khác hợp lý hơn thì nói, chưa merge gì cả — nhánh vẫn
đang nguyên vẹn hai bên.

## 7. Sau khi chốt

1. Merge `develop` vào `loc/wpf-app`, xử lý 12 file xung đột theo phương án đã chốt
2. Build và chạy thử toàn bộ luồng
3. Mở PR `loc/wpf-app → develop`, Phát review giúp phần service
4. Viết lại file phân công từ trạng thái đã ghép — lúc đó mới biết chính xác còn
   thiếu gì để chia cho Phúc, Khoa, Tú
5. Cuối kỳ merge `develop → main` một lần, vì `main` vẫn đang chứa app web cũ

Một chi tiết nhỏ khi merge: `appsettings.json` phải gộp tay. Bản `develop` có mục
`BootstrapAdmin`, bản `loc/wpf-app` có mục `Hotel` cho trang chủ. Hai mục không
xung khắc, chỉ là git không tự gộp được.
