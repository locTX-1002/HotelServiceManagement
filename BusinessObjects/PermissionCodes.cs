namespace BusinessObjects;

/// <summary>
/// Dinh danh on dinh cua cac quyen nghiep vu. Day chi la "ten khoa" de code yeu cau
/// mot quyen; viec vai tro nao co quyen nao duoc luu trong RolePermissions.
/// </summary>
public static class PermissionCodes
{
    public const string UserView = "user.view";
    public const string UserManage = "user.manage";
    public const string PermissionManage = "permission.manage";
    public const string AuditView = "audit.view";

    public const string RoomView = "room.view";
    public const string RoomManage = "room.manage";
    public const string RoomMaintenanceRequest = "room.maintenance.request";
    public const string RoomMaintenanceApprove = "room.maintenance.approve";

    public const string ReservationView = "reservation.view";
    public const string ReservationCreate = "reservation.create";
    public const string ReservationUpdate = "reservation.update";
    public const string ReservationCancelRequest = "reservation.cancel.request";
    public const string ReservationCancelApprove = "reservation.cancel.approve";

    public const string StayCheckIn = "stay.check_in";
    public const string StayExtend = "stay.extend";
    public const string StayCheckOut = "stay.check_out";

    public const string GuestView = "guest.view";
    public const string GuestManage = "guest.manage";

    public const string ServiceCatalogManage = "service.catalog.manage";
    public const string ServiceOrderCreate = "service.order.create";
    public const string ServiceOrderProcess = "service.order.process";

    public const string SurchargeCatalogManage = "surcharge.catalog.manage";
    public const string SurchargeAdd = "surcharge.add";
    public const string PromotionManage = "promotion.manage";

    public const string InvoiceView = "invoice.view";
    public const string InvoicePrepare = "invoice.prepare";
    public const string InvoiceDiscountRequest = "invoice.discount.request";
    public const string InvoiceDiscountApprove = "invoice.discount.approve";
    public const string InvoiceCancelRequest = "invoice.cancel.request";
    public const string InvoiceCancelApprove = "invoice.cancel.approve";

    public const string PaymentRecord = "payment.record";
    public const string PaymentVoidRequest = "payment.void.request";
    public const string PaymentVoidApprove = "payment.void.approve";

    public const string ReportView = "report.view";
    public const string ReportExport = "report.export";
}
