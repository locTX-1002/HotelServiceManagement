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
4. Máy bạn không dùng `.\SQLEXPRESS`? → **đừng sửa file chung** — tạo file
   `FUHotelManagementWPF/appsettings.Local.json` (đã gitignore, tự override) với nội dung:

   ```json
   {
     "ConnectionStrings": {
       "FUHotelManagement": "Server=TEN_INSTANCE_CUA_BAN;Database=FUHotelManagementDB;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=True"
     }
   }
   ```

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
  - `Colors.xaml` — brush thương hiệu (nền đá `SurfaceBrush`/`SurfaceAltBrush`, chữ ink, nhấn xanh rêu `BrandBrush`) + trạng thái (`SuccessBrush`, `WarningBrush`, `DangerBrush` kèm bản `...Soft` làm nền badge)
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

### Cách cắm 1 module vào khung (5 bước, soi theo luồng Login)

1. Viết `XxxDAO` (Singleton) trong `DataAccessObjects` → `IXxxRepository`/`XxxRepository` → `XxxService` — **mọi hàm chạm DB đều `...Async`**
2. Viết `XxxViewModel` trong `ViewModels/` — form thì kế thừa `ValidatableViewModelBase`, không form thì `ViewModelBase`
3. Viết `XxxView.xaml` (UserControl) trong `Views/`
4. Thêm **1 dòng** DataTemplate vào `Views/ViewMappings.xaml`:
   `<DataTemplate DataType="{x:Type vm:XxxViewModel}"><views:XxxView /></DataTemplate>`
5. Trong `MainViewModel`, đổi factory của module mình từ `PlaceholderViewModel` sang ViewModel thật

Cần nhảy màn từ module khác? Gọi `NavigationService.NavigateTo("Hoá đơn")` — **cấm** tự `new` UserControl hay dùng `Frame`.

## Chuẩn code bắt buộc (đọc trước khi viết module)

| Việc | Chuẩn | Cấm |
|---|---|---|
| Gọi DB | `async/await` từ DAO lên tới ViewModel; nút bấm dùng `AsyncRelayCommand` (tự khoá khi đang chạy) | `.Result` / `.Wait()` / `RelayCommand` gọi DB đồng bộ (đứng hình UI) |
| Validate form | Kế thừa `ValidatableViewModelBase`, gọi `AddError("TenProperty", "lỗi")` → ô nhập **tự viền đỏ + tooltip** (binding thêm `ValidatesOnNotifyDataErrors=True`) | Mỗi người tự chế một kiểu báo lỗi |
| Thông báo | `Notify.Success/Error/Warning/Info` (toast góc màn hình) sau mỗi thao tác CRUD | `MessageBox` cho feedback thường (chỉ dành cho lỗi chết app lúc khởi động + dialog xác nhận xoá) |
| UI | Token trong `Themes/`, control phức tạp dùng HandyControl | Hardcode màu / FontSize |

Luồng Login (`LoginViewModel`) là bài mẫu có đủ cả 4 chuẩn trên — copy cấu trúc từ đó.

## Quy tắc migration (quan trọng — tránh vỡ khi 5 người cùng sửa DB)

- **CHỈ MỘT người giữ quyền tạo migration** (đề xuất: Phát — lead BE). 
- Ai cần thêm field/entity: sửa entity + configuration, **báo nhóm**, KHÔNG tự chạy `dotnet ef migrations add`.
- Người giữ quyền gộp các thay đổi entity theo lịch cố định (ví dụ cuối ngày) thành **một** migration:

  ```
  dotnet tool install --global dotnet-ef        # lần đầu
  dotnet ef migrations add TenMigration --project DataAccessObjects
  ```
- Không cần chạy `database update` tay — app tự `MigrateAsync()` lúc khởi động.
- Hai migration tạo song song trên 2 nhánh = xung đột model snapshot rất khó gỡ. Đừng.

## CI

Mỗi push lên `main` và mỗi PR đều được GitHub Actions build lại toàn solution
(`.github/workflows/build.yml`). **PR đỏ thì không merge.** Trước khi push hãy chạy
`dotnet build` ở máy mình.
