# Frontend WPF cho FU Hotel Management

## 1. Phạm vi

Frontend là ứng dụng desktop trong project `FUHotelManagementWPF`.
Giao diện được xây dựng bằng XAML và MVVM.

```text
View (XAML) ↔ ViewModel → Service
```

ViewModel gọi service trực tiếp trong cùng tiến trình ứng dụng.

## 2. Cấu trúc project

```text
FUHotelManagementWPF/
├── Assets/             Hình ảnh và tài nguyên nhúng
├── MvvmCore/           Base ViewModel, command, navigation, notification
├── Themes/             Màu, typography và style control
├── ViewModels/         Trạng thái và hành vi giao diện
├── Views/              UserControl và dialog XAML
├── App.xaml            ResourceDictionary dùng toàn ứng dụng
├── LoginWindow.xaml    Cửa sổ đăng nhập
├── MainWindow.xaml     Khung chính và sidebar
└── SplashWindow.xaml   Trạng thái chuẩn bị database
```

## 3. Thành phần giao diện WPF

| Nhu cầu | Thành phần WPF |
|---|---|
| Màn hình module | `UserControl` XAML |
| Trạng thái màn hình | Property trong ViewModel |
| Hành động người dùng | `ICommand`/`AsyncRelayCommand` |
| Điều hướng | `NavigationService` + `ContentControl` |
| Gọi nghiệp vụ | Service trong cùng solution |
| Dữ liệu hiển thị | Row model, form model hoặc service result |
| Phiên đăng nhập | `AppSession` |
| Hộp thoại | WPF/HandyControl dialog |
| Thông báo | `Notify.Success/Error/Warning/Info` |
| Bảng dữ liệu | `DataGrid` |
| Trạng thái tải | HandyControl `LoadingCircle` |
| Validation | `INotifyDataErrorInfo` |
| Cấu hình | `appsettings.json` và `appsettings.Local.json` |

## 4. Quy tắc MVVM

### View

- View chỉ mô tả bố cục, binding và hành vi giao diện.
- Không truy cập database hoặc repository.
- Không chứa business rule.
- Hạn chế code-behind; chỉ dùng cho hành vi thuần UI không thể biểu diễn hợp lý bằng binding.
- Không tự tạo View trong ViewModel.

### ViewModel

- Kế thừa `ViewModelBase`.
- Form kế thừa `ValidatableViewModelBase`.
- Property cập nhật UI qua `SetProperty`/`INotifyPropertyChanged`.
- Thao tác I/O dùng `AsyncRelayCommand`.
- Không dùng `.Result`, `.Wait()` hoặc truy vấn đồng bộ làm khóa UI thread.
- Không hiển thị `MessageBox` cho feedback CRUD thông thường.
- Không tham chiếu `HotelDbContext`, DAO hoặc repository implementation.

### Code-behind

Được phép:

- `InitializeComponent()`.
- Đóng/mở cửa sổ theo lifecycle.
- Focus, drag window hoặc hành vi visual đặc thù.

Không được phép:

- Gọi database.
- Tính hóa đơn.
- Kiểm tra trùng reservation.
- Phân quyền nghiệp vụ.
- Viết toàn bộ CRUD trong click handler.

## 5. Navigation

Khung chính hiển thị ViewModel hiện tại bằng `ContentControl`. View được ánh xạ qua:

```text
Views/ViewMappings.xaml
```

Khi thêm module:

1. Tạo ViewModel.
2. Tạo `UserControl` tương ứng.
3. Thêm `DataTemplate` vào `ViewMappings.xaml`.
4. Đổi factory trong `MainViewModel` từ `PlaceholderViewModel` sang ViewModel thật.
5. Dùng `NavigationService.NavigateTo("Tên module")` khi chuyển màn từ module khác.

Không dùng `Frame`, URI route hoặc tự `new UserControl` để điều hướng.

## 6. Command và trạng thái bất đồng bộ

- Lệnh chỉ thay đổi UI tức thời có thể dùng `RelayCommand`.
- Lệnh gọi service/database phải dùng `AsyncRelayCommand`.
- Disable hành động khi command đang chạy.
- Không cho bấm gửi lặp nhiều lần.
- Hiển thị loading cho tác vụ có thời gian chờ nhận biết được.
- Bắt exception tại ranh giới phù hợp và chuyển thành thông báo thân thiện.

Mỗi màn dữ liệu phải có ba trạng thái:

- Loading.
- Empty.
- Error.

## 7. Validation

Form dùng `ValidatableViewModelBase` và `INotifyDataErrorInfo`.

```csharp
AddError(nameof(Email), "Email không hợp lệ");
```

Binding cần bật validation:

```xml
Text="{Binding Email, UpdateSourceTrigger=PropertyChanged,
       ValidatesOnNotifyDataErrors=True}"
```

- Validation định dạng đơn giản có thể nằm ở ViewModel.
- Business rule cần database phải nằm ở service.
- Không dùng nhiều cơ chế báo lỗi khác nhau cho từng module.

## 8. Thông báo và xác nhận

Dùng:

```csharp
Notify.Success("Lưu thành công");
Notify.Error("Không thể lưu dữ liệu");
Notify.Warning("Dữ liệu chưa đầy đủ");
Notify.Info("Đang xử lý");
```

`MessageBox` chỉ dành cho:

- Lỗi nghiêm trọng khi khởi động.
- Xác nhận hành động phá hủy như xóa/hủy.

Thông báo không được lộ exception kỹ thuật, SQL hoặc connection string cho người dùng.

## 9. Theme và style

Tài nguyên dùng chung nằm tại:

- `Themes/Colors.xaml`.
- `Themes/Typography.xaml`.
- `Themes/Controls.xaml`.

Quy tắc:

- Không hardcode mã màu trong module.
- Không hardcode `FontSize` nếu đã có typography token.
- Không tự tạo corner radius ngoài hệ thống đã thống nhất.
- Accent duy nhất là `BrandBrush`.
- Màu trạng thái chỉ dùng đúng ý nghĩa success/warning/danger/info.
- Control phức tạp ưu tiên HandyControl.
- Mỗi màn chỉ có một primary action.

Đọc thêm [Quy ước giao diện](QUY_UOC_GIAO_DIEN.md).

## 10. DataGrid và danh sách

- `RowHeight` tối thiểu 44.
- Không hiển thị grid line dày đặc.
- Tối đa khoảng 7 cột; thông tin chi tiết chuyển sang dialog/panel.
- Cột số, ngày, trạng thái phải căn chỉnh có chủ đích.
- Trạng thái dùng badge.
- Có tìm kiếm/lọc ở ViewModel và service phù hợp.
- Danh sách rỗng phải có hướng dẫn ngắn.
- Đang tải phải có loading indicator.

## 11. Dialog

- Dialog tạo/sửa dùng ViewModel riêng khi form phức tạp.
- Validate trước khi đóng.
- Disable nút lưu khi đang xử lý.
- Xóa/hủy cần xác nhận.
- Không đóng dialog nếu service trả lỗi.
- Sau khi thành công, tải lại hoặc cập nhật collection theo một chiến lược nhất quán.

### Dialog thanh toán

- Chỉ hiển thị hai lựa chọn: Tiền mặt và Chuyển khoản.
- Tiền mặt không hiển thị trường mã giao dịch.
- Chuyển khoản bắt buộc nhập mã giao dịch và có thể hiển thị QR theo số tiền còn lại.
- Hiển thị rõ tổng hóa đơn, đã thanh toán và số tiền còn lại.
- Không cho nhập số tiền lớn hơn số còn lại.
- Sau khi ghi nhận thành công, tải lại trạng thái `Unpaid/PartiallyPaid/Paid`.

## 12. Phân quyền giao diện

Vai trò hiện có:

- Admin.
- Manager.
- Receptionist.
- ServiceStaff.

## 13. Hợp đồng backend đã chốt để tích hợp

Frontend chỉ tham chiếu các interface trong project `Services`; không gọi Repository hoặc DAO.

| Màn hình/luồng | Service chính | Lưu ý bắt buộc |
|---|---|---|
| Đặt phòng | `IReservationService` | Hiển thị `ServiceResult.Message` khi trùng lịch hoặc sai trạng thái |
| Check-in/check-out | `IStayService` | Check-out chỉ thành công khi đơn dịch vụ đã đóng và hóa đơn `Paid` |
| Dịch vụ | `IServiceCatalogService`, `IServiceOrderService` | Chỉ cho chuyển trạng thái theo luồng Pending → Processing → Completed |
| Hóa đơn | `IInvoiceService` | Nếu dữ liệu thay đổi đồng thời, tải lại stay/hóa đơn rồi thao tác lại |
| Thanh toán | `IPaymentService` | Cash không có transaction ID; BankTransfer bắt buộc có; chỉ Admin/Manager được void |
| Phụ thu/khuyến mãi | `ISurchargeService`, `IPromotionService` | Không sửa/xóa phụ thu sau khi đã có thanh toán |
| Báo cáo | `IReportService` | CSV được service trả về dạng chuỗi; ViewModel chọn đường dẫn và ghi file |
| Nhân viên | `IUserManagementService` | Chỉ Admin; mật khẩu phải đạt `PasswordPolicy` |
| Buồng phòng | `IHousekeepingRequestService` | ServiceStaff/Manager/Admin xử lý trạng thái |
| Tài khoản khách | `IGuestAccountService` | Mật khẩu không được giữ trong property hoặc log |

Mọi command thay đổi dữ liệu phải kiểm tra `ServiceResult.Ok`. Khi `Ok == false`, giữ nguyên
màn hình/form và hiển thị `Message`; không suy luận thành công từ việc không phát sinh exception.

Frontend có thể ẩn hoặc disable chức năng không được phép, nhưng service vẫn phải kiểm tra
quyền. Không coi việc ẩn nút là cơ chế bảo mật duy nhất.

## 14. Các module cần triển khai

Khung hiện đã có đăng nhập, navigation và module Phòng. Các module còn lại cần thay
`PlaceholderViewModel` bằng ViewModel thật:

- Đặt phòng.
- Check-in/Check-out.
- Khách hàng.
- Dịch vụ.
- Hóa đơn.
- Báo cáo.
- Người dùng.

Cấu trúc gợi ý cho một module:

```text
ViewModels/Xxx/
├── XxxListViewModel.cs
├── XxxEditDialogViewModel.cs
└── XxxRow.cs

Views/Xxx/
├── XxxListView.xaml
└── XxxListView.xaml.cs

Views/Dialogs/
├── XxxEditDialog.xaml
└── XxxEditDialog.xaml.cs
```

## 15. Accessibility và khả năng sử dụng

- Bảo đảm tab order hợp lý.
- Nút icon phải có tooltip hoặc accessible name.
- Không truyền đạt trạng thái chỉ bằng màu.
- Nội dung phải đọc được ở kích thước cửa sổ tối thiểu 1100×680.
- Không để text bị cắt khi nội dung dài.
- Focus phải quay về vị trí hợp lý sau khi đóng dialog.
- Kiểm tra thao tác chính bằng bàn phím.

## 16. Checklist trước khi mở PR

- [ ] Module chỉ sử dụng XAML, ViewModel và các service của solution.
- [ ] View chỉ bind; không chứa business rule.
- [ ] ViewModel chỉ gọi service.
- [ ] I/O dùng `AsyncRelayCommand`.
- [ ] Không dùng `.Result` hoặc `.Wait()`.
- [ ] Form dùng cơ chế validation chung.
- [ ] Có loading, empty và error state.
- [ ] Dùng theme token; không hardcode màu và cỡ chữ.
- [ ] Navigation dùng `NavigationService`.
- [ ] Đã thêm DataTemplate vào `ViewMappings.xaml`.
- [ ] Đã kiểm tra quyền hiển thị.
- [ ] Đã kiểm tra cửa sổ 1100×680 và keyboard navigation.
- [ ] Không có binding error trong Output window.
- [ ] `dotnet build FUHotelManagement.slnx` thành công.
