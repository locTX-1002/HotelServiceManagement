# FU Hotel Management (WPF)

Ứng dụng desktop quản lý khách sạn — bài nhóm **PRN212**, nhóm 2 SE1919.
Stack: **C# .NET 10 · WPF (MVVM) · EF Core · SQL Server**, kiến trúc 3 lớp + Repository + Singleton đúng yêu cầu đề.

## Yêu cầu máy

- Windows 10/11
- .NET 10 SDK
- SQL Server Express (instance mặc định `.\SQLEXPRESS`)
- Visual Studio 2022 trở lên — mở file `FUHotelManagement.slnx`

## Chạy lần đầu (không phải setup database tay)

1. Clone repo → mở `FUHotelManagement.slnx`
2. Đặt **FUHotelManagementWPF** làm Startup Project → F5
3. Lần chạy đầu app tự tạo database `FUHotelManagementDB` + seed dữ liệu mẫu
   (màn splash "Đang chuẩn bị cơ sở dữ liệu…" hiện vài giây)
4. Máy bạn không dùng `.\SQLEXPRESS`? → sửa chuỗi kết nối trong
   `FUHotelManagementWPF/appsettings.json`

### Tài khoản seed

| Email | Mật khẩu | Vai trò |
|---|---|---|
| admin@hotel.com | `Admin123!` | Admin |
| manager@hotel.com | `Manager123!` | Manager |
| receptionist@hotel.com | `Receptionist123!` | Receptionist |
| service@hotel.com | `Service123!` | ServiceStaff |

## Kiến trúc 3 lớp

```
BusinessObjects       entity + enum (không phụ thuộc project nào)
DataAccessObjects     DbContext, cấu hình EF + seed, DAO (Singleton), migration
Repositories          interface + implementation bọc DAO
Services              nghiệp vụ (AuthService, AppSession, DatabaseService)
FUHotelManagementWPF  WPF MVVM — chỉ tham chiếu Services + BusinessObjects
```

Luồng mẫu có sẵn để soi theo khi làm module mới: **Đăng nhập**

```
UserDAO (Singleton) → UserRepository → AuthService → LoginViewModel → LoginWindow
```

## Quy ước UI — bắt buộc, để 5 module ra cùng một kiểu

- Design tokens nằm ở `FUHotelManagementWPF/Themes/`:
  - `Colors.xaml` — brush thương hiệu (kem/ink/terracotta) + trạng thái (`SuccessBrush`, `WarningBrush`, `DangerBrush` kèm bản `...Soft` làm nền badge)
  - `Typography.xaml` — 4 cấp chữ: `PageTitleText`, `SectionTitleText`, `BodyText`, `CaptionText` (+ `FieldLabel`, `IconText`)
  - `Controls.xaml` — `PrimaryButton`, `GhostButton`, `Card`, `BadgeSuccess/Warning/Danger/Info/Neutral`, `NavItem`; **TextBox/PasswordBox đã được style ngầm toàn app**, cứ dùng thẳng
- **KHÔNG hardcode mã màu hay FontSize trong module.** Thiếu màu/kiểu → thêm token trước rồi mới dùng.
- Control phức tạp (DataGrid, ComboBox, DatePicker, thông báo Growl…) dùng **HandyControl**
  (`xmlns:hc="https://handyorg.github.io/handycontrol"`) — đã tự ăn màu thương hiệu.
- Trạng thái hiển thị bằng badge:
  ```xml
  <Border Style="{StaticResource BadgeSuccess}"><TextBlock Text="Trống" /></Border>
  ```

## Phân chia 5 module gợi ý

| # | Module | Phạm vi |
|---|---|---|
| 1 | Phòng | CRUD Loại phòng + Phòng (popup dialog + xác nhận xoá), sơ đồ phòng |
| 2 | Đặt phòng | Tạo/sửa/huỷ đặt phòng, check-in / check-out |
| 3 | Khách & Người dùng | CRUD khách; quản lý tài khoản nhân viên (Admin) |
| 4 | Dịch vụ | Danh mục dịch vụ + gọi dịch vụ cho phòng đang ở |
| 5 | Hoá đơn & Báo cáo | Tính tiền, thanh toán; **báo cáo theo khoảng ngày, sắp xếp giảm dần** (yêu cầu cứng của đề) |

Mỗi module lặp đúng chuỗi của luồng Login: DAO (Singleton) → Repository → Service →
ViewModel → UserControl (tự tạo thư mục `Views/`), rồi cắm vào vùng nội dung MainWindow
(thay placeholder "Đang phát triển").

## Khi cần thêm / đổi bảng

```
dotnet tool install --global dotnet-ef        # lần đầu
dotnet ef migrations add TenMigration --project DataAccessObjects
```

Không cần chạy `database update` tay — app tự `Migrate()` lúc khởi động.
