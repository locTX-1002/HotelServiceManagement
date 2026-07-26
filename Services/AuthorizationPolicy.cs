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
    public static bool CanManageRooms => AppSession.RoleName is RoleNames.Admin or RoleNames.Manager;
    public static bool CanOperateFrontDesk => AppSession.RoleName is RoleNames.Admin or RoleNames.Manager or RoleNames.Receptionist;
    public static bool CanManageServiceCatalog => AppSession.RoleName is RoleNames.Admin or RoleNames.Manager;
    public static bool CanCreateServiceOrder => CanOperateFrontDesk;
    public static bool CanProcessServiceOrder => AppSession.RoleName is RoleNames.Admin or RoleNames.Manager or RoleNames.ServiceStaff;

    /// <summary>
    /// Giam gia TU NHAP tren hoa don (khong qua ma khuyen mai). Chat hon lap hoa don:
    /// le tan van lap duoc hoa don va nhap ma khuyen mai, nhung tu go ra mot so tien
    /// giam bat ky thi phai la quan ly - vi khong co ma nao rang buoc, khong ai duyet,
    /// va he thong khong luu duoc ly do (cot PromotionCode chi 30 ky tu, nhom cam tu
    /// tao migration de them cot).
    /// </summary>
    public static bool CanGiveManualDiscount => AppSession.RoleName is RoleNames.Admin or RoleNames.Manager;
}
