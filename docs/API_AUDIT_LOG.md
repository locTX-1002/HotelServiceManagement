# API cần bổ sung — Nhật ký hoạt động (Audit Log)

> Frontend đã dựng sẵn trang **Nhật ký hoạt động** (`/audit-logs`, menu Quản lý, chỉ Admin thấy).
> Hiện gọi `GET /api/audit-logs`; backend chưa có nên trang chạy dữ liệu mẫu (badge "Dữ liệu mẫu").
> Backend làm xong đúng contract này thì trang tự chạy thật, FE không phải sửa.

## Hiện trạng backend (đã khảo sát 21/07)

- **Chưa có** bảng/entity audit nào. Chỉ có vài cột rời rạc `CreatedByUserId`, `CheckedInByUserId`,
  `CheckedOutByUserId`, `ReceivedByUserId`, `HandledByUserId` trên các entity nghiệp vụ — biết "ai tạo"
  nhưng **không có mốc thời gian**, và UPDATE/DELETE/đổi trạng thái không được ghi lại ở đâu.
- Helper `GetCurrentUserId()` đang bị copy-paste ở ~7 controller — làm audit là dịp gom về 1 chỗ.
- Pipeline `Program.cs` chưa có middleware tùy biến nào → chèn audit middleware rất gọn.

## 1. Entity + migration

```csharp
public class AuditLog : BaseEntity
{
    public int? UserId { get; set; }          // null nếu không xác định được
    public string UserName { get; set; }      // snapshot - user đổi tên/xóa vẫn đọc được log cũ
    public string Role { get; set; }
    public string Action { get; set; }        // Login | Create | Update | Delete | StatusChange
    public string EntityName { get; set; }    // "Reservation", "Room", ...
    public int? EntityId { get; set; }
    public string Description { get; set; }   // mô tả ngắn, tiếng Việt càng tốt (FE hiện thẳng)
    public string? IpAddress { get; set; }
    public DateTime Timestamp { get; set; }
}
```

`DbSet<AuditLog>` + `IEntityTypeConfiguration` (index theo `Timestamp` DESC) + `dotnet ef migrations add AddAuditLogs`.

## 2. Cách ghi log (đề xuất mức đồ án — đơn giản, đủ điểm)

Không cần override SaveChanges phức tạp. Ghi **thủ công tại các service** cho các thao tác chính:

- Login thành công (`AuthService`)
- Create/Update/Delete ở: Reservation, Room, RoomType, User, Promotion, ServiceItem, SurchargeItem, Guest
- Đổi trạng thái: duyệt đơn, check-in, check-out, đổi trạng thái phòng, thu tiền

Viết 1 helper `IAuditLogger.LogAsync(userId, action, entityName, entityId, description)` đăng ký DI,
gọi 1 dòng ở cuối mỗi thao tác thành công. (Nâng cao hơn: middleware log mọi POST/PUT/PATCH/DELETE —
được điểm cộng nhưng không bắt buộc.)

## 3. Endpoint

```
GET /api/audit-logs
[Authorize(Roles = "Admin")]
```

**Response — chọn 1 trong 2, FE nhận được cả hai:**

```jsonc
// Cách 1 (đơn giản): mảng thẳng, mới nhất trước, giới hạn 500 dòng gần nhất
[ { "id": 18, "timestamp": "2026-07-21T16:45:00", "userId": 1, "userName": "Admin User",
    "role": "Admin", "action": "Update", "entityName": "RoomType", "entityId": 2,
    "description": "Sửa loại phòng Deluxe: đổi mô tả tiện nghi", "ipAddress": "192.168.1.10" } ]

// Cách 2 (chuẩn hơn): phân trang server
{ "items": [ ...như trên... ], "totalCount": 1234, "page": 1, "pageSize": 50 }
```

FE hiện lọc (từ khóa / loại hành động / khoảng ngày) và phân trang **client-side** trên danh sách trả về,
nên cách 1 là đủ chạy. Nếu làm cách 2 kèm query `?page=&pageSize=&action=&from=&to=` thì ghi chú lại để
FE chuyển sang đẩy filter xuống server sau.

**`action` phải là 1 trong:** `Login`, `Create`, `Update`, `Delete`, `StatusChange`
(FE map màu + nhãn tiếng Việt theo đúng 5 giá trị này — giá trị lạ vẫn hiện nhưng không có màu riêng).

## 4. Kiểm thử nhanh sau khi làm xong

1. Đăng nhập admin → làm vài thao tác (sửa loại phòng, duyệt đơn, check-in).
2. Mở menu **Quản lý → Nhật ký hoạt động**: các thao tác vừa làm hiện đúng thứ tự mới nhất trước,
   badge "Dữ liệu mẫu" biến mất.
3. Đăng nhập manager/lễ tân → gõ thẳng `/audit-logs` → bị chặn "Khu vực này không thuộc vai trò của bạn";
   gọi API trực tiếp phải nhận 403.
