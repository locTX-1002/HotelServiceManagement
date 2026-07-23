# Phân công module — đồ án PRN212 nhóm 2 SE1919

Tài liệu này chia phần việc còn lại của app WPF thành 4 gói độc lập. Mỗi gói là
một lát cắt dọc trọn vẹn: từ DAO chạm database lên tới màn hình. Nhận gói nào
thì làm trọn gói đó, không phải chờ ai.

## Tình hình hiện tại

| Module | Trạng thái | Người làm |
|---|---|---|
| Khung app (login, shell, theme, điều hướng) | xong | Lộc |
| Sơ đồ phòng + Danh sách phòng + Loại phòng | xong | Lộc |
| Đặt phòng | xong | Lộc |
| Check-in / Check-out | xong | Lộc |
| Khách hàng | xong | Lộc |
| **Dịch vụ** | **chưa làm** | **gói A** |
| **Hoá đơn & Thanh toán** | **chưa làm** | **gói B** |
| **Báo cáo** | **chưa làm** | **gói C** |
| **Người dùng & phân quyền** | **chưa làm** | **gói D** |
| Trang chủ | đang thiết kế | Lộc |

Đề xuất người nhận (Lộc chốt lại): Phúc → A, Phát → B, Khoa → C, Tú → D.
Gói D nhẹ nhất nên kèm thêm việc test tổng thể ở cuối dự án.

## Trước khi viết dòng code đầu tiên

Đọc 3 file này, không đọc thì code sẽ bị trả về ở bước review:

- [README.md](../README.md) — mục "Cách cắm 1 module vào khung (5 bước)" và bảng
  "Chuẩn code bắt buộc"
- [docs/FRONTEND_WPF.md](FRONTEND_WPF.md) — chuẩn viết ViewModel và View
- [docs/QUY_UOC_GIAO_DIEN.md](QUY_UOC_GIAO_DIEN.md) — bo góc 4px, màu lấy từ
  token, định dạng danh sách

Luồng Login (`LoginViewModel` + `LoginWindow.xaml`) là bài mẫu có đủ mọi chuẩn.
Module Khách hàng (`GuestsViewModel` + `GuestsView.xaml` + `GuestEditDialog`) là
bài mẫu CRUD đầy đủ nhất — copy cấu trúc từ đó cho nhanh.

## Nguyên tắc chung

**Nhánh và commit**

- Mỗi người làm trên nhánh riêng, cắt từ `develop`: `phuc/dich-vu`,
  `phat/hoa-don`, `khoa/bao-cao`, `tu/nguoi-dung`
- Commit bằng tiếng Việt, **một commit làm một việc**. Sửa lỗi và thêm tính năng
  là hai commit khác nhau
- Xong gói thì mở PR vào `develop`, **không tự merge**, để Lộc review

**Database**

Bốn gói này **không cần tạo migration**. Toàn bộ 18 entity đã có sẵn trong
`BusinessObjects/Entities` và bảng đã tồn tại trong database. Nếu thấy thiếu
cột thì **báo nhóm trước**, đừng tự `dotnet ef migrations add` — hai người tạo
migration cùng lúc là hỏng cả nhánh, gỡ rất mệt.

**Bất biến phải giữ**

Mọi thao tác ghi (đọc lên, sửa, lưu xuống) phải nằm gọn trong **một
`HotelDbContext` duy nhất**. Entity có cột `RowVersion` chống ghi đè; tách ra
hai context là dính lỗi concurrency lúc chạy thật.

**Lấy logic nghiệp vụ từ bản web**

Bản web cũ đã được kiểm thử kỹ, nghiệp vụ port sang chứ không nghĩ lại từ đầu.
Code còn nguyên ở tag `v1.0-web`, xem bằng lệnh:

```bash
git show v1.0-web:backend/HotelServiceManagement.Infrastructure/Services/InvoiceService.cs
```

Đường dẫn cụ thể ghi trong từng gói bên dưới. Đọc để lấy **quy tắc nghiệp vụ**,
không copy nguyên si — bản web có DTO, controller, JWT, còn bên này gọi service
thẳng từ ViewModel.

## Gói A — Dịch vụ

Quản lý danh mục dịch vụ (đồ ăn, giặt ủi, spa...) và gọi dịch vụ cho phòng đang
có khách ở.

**File phải tạo**

```
DataAccessObjects/ServiceCategoryDAO.cs
DataAccessObjects/ServiceItemDAO.cs
DataAccessObjects/ServiceOrderDAO.cs
Repositories/IServiceCatalogRepository.cs + ServiceCatalogRepository.cs
Repositories/IServiceOrderRepository.cs   + ServiceOrderRepository.cs
Services/IServiceCatalogService.cs        + ServiceCatalogService.cs
Services/IServiceOrderService.cs          + ServiceOrderService.cs
FUHotelManagementWPF/ViewModels/Services/ServicesViewModel.cs
FUHotelManagementWPF/ViewModels/Services/ServiceCatalogViewModel.cs
FUHotelManagementWPF/ViewModels/Services/ServiceOrderViewModel.cs
FUHotelManagementWPF/ViewModels/Services/ServiceItemEditDialogViewModel.cs
FUHotelManagementWPF/Views/Services/ServicesView.xaml (+ 2 view con)
FUHotelManagementWPF/Views/Dialogs/ServiceItemEditDialog.xaml
```

Thêm một dòng DataTemplate vào `Views/ViewMappings.xaml`, rồi trong
`MainViewModel` đổi factory của mục "Dịch vụ" từ `PlaceholderViewModel` sang
`ServicesViewModel`.

**Nghiệp vụ phải giữ**

- Tên danh mục không được trùng
- Danh mục còn món bên trong thì **không xoá hẳn**, chuyển `IsActive = false`
- Giá món phải lớn hơn 0
- Chỉ gọi dịch vụ được cho phòng **đang có khách lưu trú** (`Stay` chưa
  check-out). Phòng trống hoặc mới đặt chưa nhận thì không gọi được
- **Đơn giá phải chụp lại lúc gọi** vào `ServiceOrderDetail.UnitPrice`. Sau này
  đổi giá menu thì đơn cũ vẫn giữ giá cũ — đây là điểm dễ làm sai nhất của gói
  này
- Tổng tiền đơn = tổng của `số lượng × đơn giá đã chụp`
- Đơn đã tính vào hoá đơn thì không cho sửa hay huỷ

**Nguồn tham khảo**

```
v1.0-web:backend/HotelServiceManagement.Infrastructure/Services/ServiceCategoryService.cs
v1.0-web:backend/HotelServiceManagement.Infrastructure/Services/ServiceItemService.cs
v1.0-web:backend/HotelServiceManagement.Infrastructure/Services/ServiceOrderService.cs
```

**Nghiệm thu**

- [ ] Thêm / sửa / xoá danh mục và món, có dialog xác nhận trước khi xoá
- [ ] Gọi 2 món cho một phòng đang ở, tổng tiền cộng đúng
- [ ] Đổi giá món trong danh mục, mở lại đơn cũ thấy giá **không đổi**
- [ ] Thử gọi dịch vụ cho phòng trống, phải bị chặn kèm thông báo tiếng Việt
- [ ] Thử tạo danh mục trùng tên, phải bị chặn

## Gói B — Hoá đơn & Thanh toán

Lập hoá đơn cho khách đã trả phòng, thu tiền nhiều lần cho tới khi đủ.

**File phải tạo**

```
DataAccessObjects/InvoiceDAO.cs
DataAccessObjects/PaymentDAO.cs
Repositories/IInvoiceRepository.cs + InvoiceRepository.cs
Repositories/IPaymentRepository.cs + PaymentRepository.cs
Services/IInvoiceService.cs        + InvoiceService.cs
Services/IPaymentService.cs        + PaymentService.cs
FUHotelManagementWPF/ViewModels/Invoices/InvoicesViewModel.cs
FUHotelManagementWPF/ViewModels/Invoices/InvoiceDetailViewModel.cs
FUHotelManagementWPF/ViewModels/Invoices/PaymentDialogViewModel.cs
FUHotelManagementWPF/Views/Invoices/InvoicesView.xaml
FUHotelManagementWPF/Views/Dialogs/PaymentDialog.xaml
```

**Nghiệp vụ phải giữ**

- Chỉ lập hoá đơn cho lượt ở **đã check-out** và **chưa có hoá đơn**
- Tiền phòng = số đêm × giá loại phòng. Số đêm tính bằng
  `(ngày trả − ngày nhận).Days`, **tối thiểu là 1** (khách ở nửa ngày vẫn tính
  một đêm)
- Tiền dịch vụ = tổng các đơn dịch vụ đã hoàn tất của lượt ở đó
- Tổng hoá đơn = tiền phòng + tiền dịch vụ
- **Chặn thu vượt số tiền còn lại.** Đây là lỗi nghiêm trọng nhất từng gặp ở bản
  web: bấm nhanh hai lần thu được gấp đôi. Phải đọc số đã thu, kiểm tra, và lưu
  **trong cùng một context**, đồng thời nút bấm dùng `AsyncRelayCommand` (tự
  khoá khi đang chạy)
- Trạng thái suy ra từ tổng đã thu: chưa thu đồng nào là `Unpaid`, thu một phần
  là `PartiallyPaid`, thu đủ là `Paid`
- Hoá đơn đã có phiếu thu thì không cho xoá

**Nguồn tham khảo**

```
v1.0-web:backend/HotelServiceManagement.Infrastructure/Services/InvoiceService.cs
v1.0-web:backend/HotelServiceManagement.Infrastructure/Services/PaymentService.cs
```

**Nghiệm thu**

- [ ] Check-out một phòng rồi lập hoá đơn, tiền phòng và tiền dịch vụ cộng đúng
- [ ] Thu làm 2 lần, trạng thái chuyển `Unpaid` → `PartiallyPaid` → `Paid`
- [ ] Thử thu nhiều hơn số còn lại, phải bị chặn
- [ ] **Bấm liên tục 5 lần vào nút thu tiền**, kiểm tra database chỉ ghi nhận
      đúng một phiếu
- [ ] Thử lập hoá đơn lần hai cho cùng lượt ở, phải bị chặn

## Gói C — Báo cáo

Thống kê doanh thu theo khoảng ngày. **Đây là yêu cầu cứng của đề bài** nên phải
làm đúng chữ, đặc biệt là phần sắp xếp.

**File phải tạo**

```
DataAccessObjects/ReportDAO.cs
Services/IReportService.cs + ReportService.cs
FUHotelManagementWPF/ViewModels/Reports/ReportsViewModel.cs
FUHotelManagementWPF/Views/Reports/ReportsView.xaml
```

Gói này không cần repository riêng — query tổng hợp đọc thẳng qua DAO là đủ.

**Nghiệp vụ phải giữ**

- Chọn khoảng ngày từ / đến. Nếu ngày bắt đầu lớn hơn ngày kết thúc thì báo lỗi,
  không cho chạy
- Bảng doanh thu theo ngày, **sắp xếp giảm dần theo doanh thu** — chữ trong đề,
  không được đổi thành sắp xếp theo ngày
- Ngày trong khoảng mà không phát sinh doanh thu vẫn phải hiện một dòng giá trị
  0, không được nhảy cóc
- Bốn thẻ chỉ số phía trên: tổng doanh thu, tiền phòng, tiền dịch vụ, công suất
  phòng
- Công suất = số đêm-phòng đã bán chia cho (số phòng × số ngày trong khoảng),
  hiện dưới dạng phần trăm
- Chỉ tính hoá đơn đã phát sinh trong khoảng, không tính hoá đơn đã huỷ

**Nguồn tham khảo**

```
v1.0-web:backend/HotelServiceManagement.Infrastructure/Services/ReportService.cs
```

**Nghiệm thu**

- [ ] Chọn khoảng 7 ngày, bảng hiện đủ 7 dòng kể cả ngày doanh thu bằng 0
- [ ] Kiểm tra mắt thường: dòng doanh thu cao nhất nằm trên cùng
- [ ] Nhập ngày bắt đầu sau ngày kết thúc, phải báo lỗi rõ ràng
- [ ] Đối chiếu tổng doanh thu với tổng cột trong bảng, phải khớp
- [ ] Chọn khoảng ngày không có dữ liệu, màn hình không được vỡ hay trống trơn
      không lời giải thích

## Gói D — Người dùng & phân quyền

Quản lý tài khoản nhân viên và lọc thanh điều hướng theo vai trò.

**File phải tạo**

```
DataAccessObjects/UserDAO.cs        (mở rộng, hiện chỉ có GetActiveByEmailAsync)
Repositories/UserRepository.cs      (mở rộng)
Services/IUserService.cs + UserService.cs
FUHotelManagementWPF/ViewModels/Users/UsersViewModel.cs
FUHotelManagementWPF/ViewModels/Users/UserEditDialogViewModel.cs
FUHotelManagementWPF/Views/Users/UsersView.xaml
FUHotelManagementWPF/Views/Dialogs/UserEditDialog.xaml
```

**Nghiệp vụ phải giữ**

- Email không được trùng
- Mật khẩu **phải băm bằng BCrypt** lúc tạo và lúc đổi. Thư viện
  `BCrypt.Net-Next` đã có sẵn trong project, xem `AuthService` để biết cách dùng
- Không cho tự khoá chính tài khoản đang đăng nhập
- Không cho khoá hoặc xoá tài khoản Admin cuối cùng còn hoạt động
- Khoá tài khoản bằng `IsActive = false`, không xoá hẳn. Luồng đăng nhập đã lọc
  sẵn theo cờ này
- Lọc thanh điều hướng theo vai trò trong `MainViewModel`:

| Vai trò | Thấy những mục nào |
|---|---|
| Admin | đủ 9 mục |
| Manager | tất cả trừ Người dùng |
| Receptionist | Trang chủ, Sơ đồ phòng, Đặt phòng, Check-in/out, Khách hàng, Hoá đơn |
| ServiceStaff | Trang chủ, Sơ đồ phòng, Dịch vụ |

**Nguồn tham khảo**

```
v1.0-web:backend/HotelServiceManagement.Application/Services/UserService.cs
v1.0-web:backend/HotelServiceManagement.Infrastructure/Services/UserManagementService.cs
```

**Nghiệm thu**

- [ ] Tạo tài khoản mới, đăng xuất, đăng nhập lại được bằng tài khoản đó
- [ ] Đổi mật khẩu rồi đăng nhập bằng mật khẩu mới
- [ ] Mở database xem cột mật khẩu, phải là chuỗi băm chứ không phải chữ thường
- [ ] Khoá một tài khoản, tài khoản đó không đăng nhập được nữa
- [ ] Thử tự khoá chính mình, phải bị chặn
- [ ] Đăng nhập lần lượt 4 vai trò, đếm số mục trên thanh điều hướng khớp bảng
      trên

## Phần Lộc giữ

- Trang chủ (đang chờ chốt mẫu)
- Khung app, bộ màu, style dùng chung, review toàn bộ PR
- Năm việc vá còn tồn ở hai màn Phòng:
  1. Phòng đã ngừng dùng đang nhìn y hệt phòng đang dùng trên danh sách
  2. Lọc theo tầng và theo loại phòng
  3. Sắp xếp danh sách theo giá hoặc số phòng
  4. Bấm thẻ hạng phòng để xem danh sách phòng thuộc hạng đó
  5. Panel chi tiết hiện lịch sử đặt gần đây của phòng

## Thứ tự làm

Gói A, C, D làm song song được ngay từ bây giờ.

Gói B cần đơn dịch vụ của gói A để tính phần tiền dịch vụ. Nếu gói A chưa xong,
gói B cứ làm phần tiền phòng trước, để phần tiền dịch vụ trả về 0, ghép nốt khi
gói A merge xong. Đừng ngồi chờ.

## Kẹt ở đâu thì hỏi

Trước khi nhắn hỏi, thử ba bước này:

1. Mở module Khách hàng ra đối chiếu — cùng dạng CRUD, gần như bê nguyên được
2. Đọc lại bảng "Chuẩn code bắt buộc" trong README, đa số lỗi nằm ở đó
3. Chạy `dotnet build` đọc kỹ dòng lỗi đầu tiên, đừng đọc dòng cuối

Vẫn kẹt thì nhắn nhóm kèm **thông báo lỗi đầy đủ** và tên file, đừng chỉ nói
"em bị lỗi không chạy được".
