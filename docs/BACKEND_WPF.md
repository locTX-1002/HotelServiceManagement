# Backend cho ứng dụng FU Hotel Management WPF

## 1. Phạm vi

Trong phiên bản desktop, thuật ngữ **backend** chỉ các tầng xử lý dữ liệu và nghiệp vụ
chạy trong cùng tiến trình với ứng dụng WPF:

```text
BusinessObjects → DataAccessObjects → Repositories → Services
```

Các tầng được gọi trực tiếp từ ViewModel thông qua service trong cùng tiến trình desktop.
Không tạo thêm tầng giao tiếp mạng nội bộ giữa giao diện và nghiệp vụ.

## 2. Cấu trúc project

### Trạng thái triển khai

| Module | DAO | Repository | Service |
|---|---:|---:|---:|
| Đăng nhập/người dùng đăng nhập | Hoàn thành | Hoàn thành | Hoàn thành |
| Loại phòng | Hoàn thành | Hoàn thành | Hoàn thành |
| Phòng | Hoàn thành | Hoàn thành | Hoàn thành |
| Khách hàng | Hoàn thành | Hoàn thành | Hoàn thành |
| Đặt phòng | Hoàn thành | Hoàn thành | Hoàn thành |
| Check-in/check-out | Hoàn thành | Hoàn thành | Hoàn thành |
| Danh mục và gọi dịch vụ | Hoàn thành | Hoàn thành | Hoàn thành |
| Hóa đơn/thanh toán | Thanh toán hoàn thành | Thanh toán hoàn thành | Thanh toán hoàn thành |
| Báo cáo | Chưa triển khai | Chưa triển khai | Chưa triển khai |
| Quản lý tài khoản nhân viên | Một phần | Một phần | Chưa triển khai |

Bảng này phản ánh mã nguồn hiện tại, không phải danh sách chức năng đã hoàn thành ở giao diện.

### BusinessObjects

Chứa entity, enum và kiểu dữ liệu dùng chung.

- Không phụ thuộc project khác.
- Không chứa ViewModel, XAML, truy vấn EF hoặc logic giao diện.
- Entity phải mô tả đúng quan hệ và trạng thái nghiệp vụ.
- Giá trị tiền phải dùng `decimal`.
- Chuỗi, ngày giờ và navigation property phải khai báo nullable rõ ràng.

Các nhóm entity hiện có:

- Tài khoản: `User`, `Role`, `GuestAccount`.
- Khách và lưu trú: `Guest`, `Reservation`, `Stay`.
- Phòng: `Room`, `RoomType`.
- Dịch vụ: `ServiceCategory`, `ServiceItem`, `ServiceOrder`, `ServiceOrderDetail`.
- Tài chính: `Invoice`, `Payment`, `Surcharge`, `SurchargeItem`, `Promotion`.
- Vận hành: `HousekeepingRequest`.

### DataAccessObjects

Chứa `HotelDbContext`, EF Core configuration, migration, seed và DAO.

- Mọi thao tác I/O phải có API bất đồng bộ.
- DAO là nơi duy nhất trực tiếp truy cập `HotelDbContext`.
- DAO tuân theo Singleton theo yêu cầu kiến trúc của bài.
- Không đặt thông báo UI hoặc kiểm tra quyền hiển thị tại DAO.
- Query chỉ lấy dữ liệu cần thiết; tránh tải toàn bộ bảng để lọc trong bộ nhớ.
- Các thao tác nhiều bước phải cho phép service điều phối transaction.

Mỗi entity cần rà soát:

- Primary key và foreign key.
- Unique index cho mã phòng, email và các mã nghiệp vụ.
- Độ dài tối đa của chuỗi.
- `decimal` precision cho giá và tổng tiền.
- `DeleteBehavior` để không xóa dây chuyền dữ liệu lịch sử.
- Giá trị mặc định, trạng thái ban đầu và audit fields.

### Repositories

Repository bọc DAO và cung cấp hợp đồng truy cập dữ liệu cho service.

```text
IXxxRepository.cs
XxxRepository.cs
```

- Interface trả về `Task` hoặc `Task<T>`.
- Không trả EF `IQueryable` ra ViewModel.
- Không chứa XAML, navigation hoặc toast.
- Không sao chép business rule từ service xuống repository.
- Có phương thức tìm kiếm theo nghiệp vụ thay vì để caller tự ghép query tùy ý.

### Services

Service là nơi đặt business rule và điều phối repository.

- ViewModel chỉ làm việc với service.
- Kết quả thao tác dùng `ServiceResult` hoặc kiểu kết quả có thông báo rõ ràng.
- Kiểm tra quyền tại service, không chỉ ẩn nút trên giao diện.
- Dùng transaction cho check-in, check-out, lập hóa đơn và thanh toán.
- Không tham chiếu project WPF từ Services.

Các service cần có theo module:

- `AuthService` và `AppSession`.
- `RoomService`, `RoomTypeService`.
- `GuestService`.
- `ReservationService`.
- `StayService` hoặc `CheckInOutService`.
- `ServiceCatalogService`, `ServiceOrderService`.
- `InvoiceService`, `PaymentService`.
- `ReportService`.
- `UserManagementService`.
- Service cho promotion, surcharge và housekeeping nếu các chức năng được triển khai.

## 3. Luồng gọi chuẩn

```text
WPF View
  → ViewModel command
  → Service
  → Repository interface
  → Repository implementation
  → DAO
  → HotelDbContext
  → SQL Server
```

Ví dụ đăng nhập:

```text
LoginViewModel → AuthService → UserRepository → UserDAO → HotelDbContext
```

Không được rút ngắn thành `ViewModel → DAO` hoặc `ViewModel → DbContext`.

## 4. Quy tắc nghiệp vụ chính

### Đặt phòng

- Ngày trả phải sau ngày nhận.
- Phòng phải hoạt động và thuộc loại phòng hợp lệ.
- Không cho phép hai reservation đang hiệu lực trùng khoảng thời gian của cùng phòng.
- Việc hủy phải tuân theo trạng thái hiện tại.
- Giá tại thời điểm đặt cần được lưu hoặc có quy tắc truy xuất lịch sử rõ ràng.

### Check-in và check-out

- Chỉ reservation hợp lệ mới được check-in.
- Không check-in một phòng đang có stay hoạt động.
- Check-in cập nhật reservation, stay và trạng thái phòng trong một transaction.
- Check-out phải tổng hợp tiền phòng, dịch vụ, phụ thu và khuyến mãi.
- Sau check-out, trạng thái phòng phải chuyển theo quy trình vận hành đã thống nhất.

### Dịch vụ

- Chỉ stay đang hoạt động mới được gọi dịch vụ.
- Giá dịch vụ trên chi tiết đơn phải phản ánh giá tại thời điểm gọi.
- Không xóa cứng dữ liệu dịch vụ đã xuất hiện trong hóa đơn.

### Hóa đơn và thanh toán

- Tiền tệ sử dụng `decimal`, không dùng `double`.
- Tổng hóa đơn được tính tại service và kiểm thử độc lập.
- Không cho phép tổng thanh toán vượt hoặc sai lệch số phải trả nếu không có quy tắc rõ ràng.
- Thanh toán thành công phải cập nhật payment, invoice và stay trong một transaction.

Phương thức thanh toán của ứng dụng desktop:

- `Cash`: tiền mặt, không có mã giao dịch.
- `BankTransfer`: chuyển khoản ngân hàng, bắt buộc nhập mã giao dịch.
- Không tích hợp xác nhận tự động qua webhook; nhân viên lễ tân xác nhận tiền đã nhận.
- Một hóa đơn có thể thanh toán nhiều lần.
- Tổng các payment `Completed` không được vượt `Invoice.TotalAmount`.
- Mã giao dịch chuyển khoản có unique index để không ghi nhận một giao dịch hai lần.
- Sau mỗi payment, trạng thái hóa đơn được cập nhật thành `PartiallyPaid` hoặc `Paid`.
- Chỉ cho phép check-out khi hóa đơn ở trạng thái `Paid`.

### Báo cáo

- Hỗ trợ khoảng ngày hợp lệ.
- Kết quả theo yêu cầu đề phải sắp xếp giảm dần.
- Quy ước rõ ràng đầu ngày, cuối ngày và timezone.
- Tổng hợp ở database/repository hoặc service; không tính trong XAML.

## 5. Đăng nhập và bảo mật

- Mật khẩu được hash bằng BCrypt.
- Không lưu mật khẩu plaintext hoặc đưa hash lên ViewModel.
- `AppSession` chỉ giữ thông tin người dùng cần thiết trong phiên chạy ứng dụng.
- Logout phải xóa session.
- Tài khoản bị khóa không được đăng nhập.
- Phân quyền phải được kiểm tra tại service.
- Không log mật khẩu, hash hoặc connection string.

Phiên đăng nhập được quản lý bởi `AppSession` vì giao diện và service chạy trong cùng tiến trình.

## 6. Cấu hình database

Cấu hình chung nằm tại:

```text
FUHotelManagementWPF/appsettings.json
```

Mỗi thành viên có thể tạo file không commit:

```text
FUHotelManagementWPF/appsettings.Local.json
```

Ví dụ:

```json
{
  "ConnectionStrings": {
    "FUHotelManagement": "Server=.\\SQLEXPRESS;Database=FUHotelManagementDB;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=True"
  }
}
```

Không đưa mật khẩu hoặc connection string cá nhân lên Git.

## 7. Migration

Chỉ một thành viên được phân công tạo migration để tránh xung đột model snapshot.

```powershell
dotnet ef migrations add TenMigration `
  --project DataAccessObjects `
  --startup-project FUHotelManagementWPF
```

Ứng dụng gọi migration khi khởi động thông qua `DatabaseService`. Trước khi merge:

1. Build solution.
2. Chạy ứng dụng với database trống.
3. Kiểm tra migration và seed hoàn thành.
4. Chạy lại để bảo đảm seed không tạo bản ghi trùng.

## 8. Testing backend

Cần kiểm thử tối thiểu:

- Hash và xác thực mật khẩu.
- Quyền theo vai trò.
- Khoảng ngày đặt phòng và kiểm tra trùng phòng.
- Chuyển trạng thái reservation/stay/room.
- Tính số đêm, tiền phòng, dịch vụ, phụ thu và khuyến mãi.
- Lập hóa đơn và thanh toán transaction.
- Báo cáo theo khoảng ngày và thứ tự giảm dần.
- Migration trên database trống và seed idempotent.

## 9. Checklist khi thêm module

- [ ] Entity và enum đã đúng nghiệp vụ.
- [ ] EF configuration đã đủ constraint.
- [ ] DAO có thao tác async cần thiết.
- [ ] Repository interface và implementation đã hoàn thành.
- [ ] Service chứa validation và business rule.
- [ ] ViewModel không truy cập DAO/DbContext.
- [ ] Có transaction cho thao tác nhiều bảng.
- [ ] Có unit test cho quy tắc quan trọng.
- [ ] Build và migration chạy thành công.
