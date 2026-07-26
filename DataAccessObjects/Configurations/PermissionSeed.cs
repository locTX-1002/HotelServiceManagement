using BusinessObjects;
using BusinessObjects.Entities;

namespace DataAccessObjects.Configurations;

internal static class PermissionSeed
{
    public static readonly Permission[] All =
    [
        P(1, PermissionCodes.UserView, "Xem nhân viên", "Người dùng"),
        P(2, PermissionCodes.UserManage, "Quản lý nhân viên", "Người dùng"),
        P(3, PermissionCodes.PermissionManage, "Cấu hình phân quyền", "Phân quyền"),
        P(4, PermissionCodes.AuditView, "Xem nhật ký hệ thống", "Nhật ký"),
        P(5, PermissionCodes.RoomView, "Xem phòng", "Phòng"),
        P(6, PermissionCodes.RoomManage, "Quản lý phòng", "Phòng"),
        P(7, PermissionCodes.RoomMaintenanceRequest, "Yêu cầu bảo trì phòng", "Phòng"),
        P(8, PermissionCodes.RoomMaintenanceApprove, "Duyệt bảo trì phòng", "Phòng"),
        P(9, PermissionCodes.ReservationView, "Xem đặt phòng", "Đặt phòng"),
        P(10, PermissionCodes.ReservationCreate, "Tạo đặt phòng", "Đặt phòng"),
        P(11, PermissionCodes.ReservationUpdate, "Cập nhật đặt phòng", "Đặt phòng"),
        P(12, PermissionCodes.ReservationCancelRequest, "Yêu cầu hủy đặt phòng", "Đặt phòng"),
        P(13, PermissionCodes.ReservationCancelApprove, "Duyệt hủy đặt phòng", "Đặt phòng"),
        P(14, PermissionCodes.StayCheckIn, "Nhận phòng", "Lượt ở"),
        P(15, PermissionCodes.StayExtend, "Gia hạn lượt ở", "Lượt ở"),
        P(16, PermissionCodes.StayCheckOut, "Trả phòng", "Lượt ở"),
        P(17, PermissionCodes.GuestView, "Xem khách hàng", "Khách hàng"),
        P(18, PermissionCodes.GuestManage, "Quản lý khách hàng", "Khách hàng"),
        P(19, PermissionCodes.ServiceCatalogManage, "Quản lý danh mục dịch vụ", "Dịch vụ"),
        P(20, PermissionCodes.ServiceOrderCreate, "Tạo đơn dịch vụ", "Dịch vụ"),
        P(21, PermissionCodes.ServiceOrderProcess, "Xử lý đơn dịch vụ", "Dịch vụ"),
        P(22, PermissionCodes.SurchargeCatalogManage, "Quản lý danh mục phụ thu", "Phụ thu"),
        P(23, PermissionCodes.SurchargeAdd, "Thêm phụ thu", "Phụ thu"),
        P(24, PermissionCodes.PromotionManage, "Quản lý khuyến mãi", "Khuyến mãi"),
        P(25, PermissionCodes.InvoiceView, "Xem hóa đơn", "Hóa đơn"),
        P(26, PermissionCodes.InvoicePrepare, "Lập và tính lại hóa đơn", "Hóa đơn"),
        P(27, PermissionCodes.InvoiceDiscountRequest, "Yêu cầu giảm giá", "Hóa đơn"),
        P(28, PermissionCodes.InvoiceDiscountApprove, "Duyệt giảm giá", "Hóa đơn"),
        P(29, PermissionCodes.InvoiceCancelRequest, "Yêu cầu hủy hóa đơn", "Hóa đơn"),
        P(30, PermissionCodes.InvoiceCancelApprove, "Duyệt hủy hóa đơn", "Hóa đơn"),
        P(31, PermissionCodes.PaymentRecord, "Ghi nhận thanh toán", "Thanh toán"),
        P(32, PermissionCodes.PaymentVoidRequest, "Yêu cầu hủy giao dịch", "Thanh toán"),
        P(33, PermissionCodes.PaymentVoidApprove, "Duyệt hủy giao dịch", "Thanh toán"),
        P(34, PermissionCodes.ReportView, "Xem báo cáo", "Báo cáo"),
        P(35, PermissionCodes.ReportExport, "Xuất báo cáo", "Báo cáo")
    ];

    private static Permission P(int id, string code, string name, string module) => new()
    {
        Id = id,
        PermissionCode = code,
        DisplayName = name,
        Module = module,
        Description = name,
        IsActive = true
    };
}
