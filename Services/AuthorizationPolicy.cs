using BusinessObjects;

namespace Services;

/// <summary>
/// Luat phan quyen dung chung cho CA hai tang: service dung de CHAN that su, giao dien dung
/// de an nut cho dung. De internal thi tang WPF khong doc duoc, moi ViewModel phai chep lai
/// dieu kien - chep xong quen bind la nut van hien roi bam vao bi tu choi (da xay ra).
/// Mo public de chi con MOT nguon su that.
///
/// An nut chi la trai nghiem: service van la lop chan cuoi cung.
/// </summary>
public static class AuthorizationPolicy
{
    public static bool HasPermission(string permissionCode) => AppSession.HasPermission(permissionCode);

    public static bool CanManageUsers => HasPermission(PermissionCodes.UserManage);
    public static bool CanManagePermissions => HasPermission(PermissionCodes.PermissionManage);
    public static bool CanViewAuditLog => HasPermission(PermissionCodes.AuditView);
    public static bool CanViewRooms => HasPermission(PermissionCodes.RoomView);
    public static bool CanManageRooms => HasPermission(PermissionCodes.RoomManage);
    public static bool CanOperateFrontDesk
        => HasPermission(PermissionCodes.ReservationCreate)
           || HasPermission(PermissionCodes.ReservationUpdate)
           || HasPermission(PermissionCodes.StayCheckIn)
           || HasPermission(PermissionCodes.StayCheckOut);
    public static bool CanManageGuests => HasPermission(PermissionCodes.GuestManage);
    public static bool CanManageServiceCatalog => HasPermission(PermissionCodes.ServiceCatalogManage);
    public static bool CanCreateServiceOrder => HasPermission(PermissionCodes.ServiceOrderCreate);
    public static bool CanProcessServiceOrder => HasPermission(PermissionCodes.ServiceOrderProcess);
    public static bool CanManageSurchargeCatalog => HasPermission(PermissionCodes.SurchargeCatalogManage);
    public static bool CanAddSurcharge => HasPermission(PermissionCodes.SurchargeAdd);
    public static bool CanManagePromotions => HasPermission(PermissionCodes.PromotionManage);
    public static bool CanViewInvoices => HasPermission(PermissionCodes.InvoiceView);
    public static bool CanPrepareInvoice => HasPermission(PermissionCodes.InvoicePrepare);
    public static bool CanRecordPayment => HasPermission(PermissionCodes.PaymentRecord);
    public static bool CanRequestPaymentVoid => HasPermission(PermissionCodes.PaymentVoidRequest);
    public static bool CanApprovePaymentVoid => HasPermission(PermissionCodes.PaymentVoidApprove);
    public static bool CanRequestInvoiceCancel => HasPermission(PermissionCodes.InvoiceCancelRequest);
    public static bool CanApproveInvoiceCancel => HasPermission(PermissionCodes.InvoiceCancelApprove);
    public static bool CanViewReports => HasPermission(PermissionCodes.ReportView);
    public static bool CanExportReports => HasPermission(PermissionCodes.ReportExport);

    /// <summary>
    /// Giam gia TU NHAP tren hoa don (khong qua ma khuyen mai). Chat hon lap hoa don:
    /// le tan van lap duoc hoa don va nhap ma khuyen mai, nhung tu go ra mot so tien
    /// giam bat ky thi phai la quan ly - vi khong co ma nao rang buoc, khong ai duyet,
    /// va he thong khong luu duoc ly do (cot PromotionCode chi 30 ky tu, nhom cam tu
    /// tao migration de them cot).
    /// </summary>
    public static bool CanRequestManualDiscount => HasPermission(PermissionCodes.InvoiceDiscountRequest);
    public static bool CanGiveManualDiscount => HasPermission(PermissionCodes.InvoiceDiscountApprove);
}
