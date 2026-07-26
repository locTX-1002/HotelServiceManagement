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
}
