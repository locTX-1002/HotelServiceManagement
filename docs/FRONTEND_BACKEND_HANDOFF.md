# Bàn giao backend WPF cho frontend

## 1. Nguyên tắc tích hợp

Frontend WPF chỉ gọi các interface trong project `Services`.

```text
View → ViewModel → Service interface → Repository → DAO → SQL Server
```

Không gọi trực tiếp Repository, DAO hoặc `HotelDbContext` từ ViewModel/code-behind.
Các command có I/O phải dùng `AsyncRelayCommand` và luôn kiểm tra `ServiceResult.Ok`.

```csharp
var result = await _service.SomeActionAsync(...);
if (!result.Ok)
{
    Notify.Error(result.Message);
    return;
}
```

## 2. Cấu hình chạy lần đầu

Sao chép `FUHotelManagementWPF/appsettings.Local.example.json` thành
`FUHotelManagementWPF/appsettings.Local.json`, sau đó cấu hình connection string và mật khẩu
Admin riêng. Không commit file local.

`BootstrapAdmin.Password` phải có tối thiểu 8 ký tự, gồm chữ hoa, chữ thường, chữ số và ký tự
đặc biệt. Backend hash BCrypt trước khi lưu database. Các tài khoản demo mặc định bị khóa.

## 3. Service theo màn hình

| Màn hình/luồng | Interface frontend sử dụng |
|---|---|
| Đăng nhập nhân viên | `IAuthService` |
| Phòng/loại phòng | `IRoomService`, `IRoomTypeService` |
| Khách hàng | `IGuestService` |
| Đặt phòng | `IReservationService` |
| Check-in/check-out | `IStayService` |
| Danh mục/đơn dịch vụ | `IServiceCatalogService`, `IServiceOrderService` |
| Hóa đơn | `IInvoiceService` |
| Thanh toán | `IPaymentService` |
| Phụ thu/khuyến mãi | `ISurchargeService`, `IPromotionService` |
| Báo cáo | `IReportService` |
| Nhân viên | `IUserManagementService` |
| Buồng phòng | `IHousekeepingRequestService` |
| Tài khoản khách | `IGuestAccountService` |

## 4. Luồng check-out bắt buộc

1. Hoàn tất hoặc hủy mọi đơn dịch vụ `Pending/Processing`.
2. Gọi `IInvoiceService.PrepareAsync(stayId, promotionCode)`.
3. Hiển thị tổng hóa đơn, đã thanh toán và số tiền còn lại.
4. Gọi `IPaymentService.RecordAsync(...)` cho đến khi hóa đơn `Paid`.
5. Gọi `IStayService.CheckOutAsync(stayId)`.

Backend sẽ từ chối check-out nếu hóa đơn chưa `Paid` hoặc còn đơn dịch vụ chưa đóng, kể cả khi
frontend hiển thị dữ liệu cũ.

## 5. Thanh toán

- `Cash`: `transactionId` phải là `null`.
- `BankTransfer`: bắt buộc có `transactionId`, tối đa 100 ký tự và không được trùng.
- Một hóa đơn được thanh toán nhiều lần nhưng tổng payment `Completed` không vượt tổng hóa đơn.
- Chỉ Admin/Manager được gọi `IPaymentService.VoidAsync(paymentId)`.
- Sau record/void, tải lại `IPaymentService.GetSummaryAsync(invoiceId)`.

Không tích hợp webhook SePay trong phiên bản desktop hiện tại. Chuyển khoản được nhân viên xác
nhận thủ công.

## 6. Trạng thái nghiệp vụ

### Đơn dịch vụ

```text
Pending → Processing → Completed
   └───────────────→ Cancelled
Processing ────────→ Cancelled
```

### Housekeeping

```text
Pending → Acknowledged → Completed
   └───────────────→ Cancelled
Acknowledged ──────→ Cancelled
```

Không cho frontend tự gán trạng thái ngoài các method service.

## 7. Phân quyền cần phản ánh trên UI

- Admin: quản lý nhân viên, toàn bộ nghiệp vụ và void payment.
- Manager: vận hành, báo cáo, promotion/phụ thu và void payment.
- Receptionist: khách hàng, đặt phòng, check-in/out, hóa đơn và ghi nhận thanh toán.
- ServiceStaff: xử lý đơn dịch vụ và housekeeping.

Frontend có thể ẩn/disable nút theo `AppSession.RoleName`, nhưng backend vẫn là nơi quyết định
quyền cuối cùng.

## 8. Báo cáo và CSV

- `GetRevenueAsync(from, to)` trả báo cáo doanh thu và danh sách ngày giảm dần.
- `GetOccupancyAsync()` trả công suất phòng.
- `ExportRevenueCsvAsync(from, to)` trả nội dung CSV dạng `string`.
- ViewModel dùng Save File Dialog và tự ghi chuỗi CSV ra file; Service không mở dialog.

## 9. Xử lý lỗi và tải lại dữ liệu

- `Ok == false`: giữ form/màn hình hiện tại và hiển thị `Message`.
- Không hiển thị exception SQL hoặc connection string cho người dùng.
- Nếu thông báo yêu cầu tải lại do dữ liệu thay đổi đồng thời, refresh stay/hóa đơn rồi cho thao
  tác lại.
- Sau create/update/cancel/payment/check-in/check-out thành công, tải lại collection hoặc entity
  liên quan thay vì tự đoán trạng thái phía UI.

## 10. Trạng thái bàn giao

- Backend Release build: 0 warning, 0 error.
- 19/19 backend tests thành công.
- EF model đồng bộ với migration hiện tại.
- CI build và chạy backend tests trên pull request.

Chi tiết kiến trúc và quy tắc đầy đủ xem `docs/BACKEND_WPF.md`; quy ước giao diện xem
`docs/FRONTEND_WPF.md` và `docs/QUY_UOC_GIAO_DIEN.md`.
