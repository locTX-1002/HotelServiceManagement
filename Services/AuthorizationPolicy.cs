namespace Services;

internal static class AuthorizationPolicy
{
    public static bool CanManageRooms => AppSession.RoleName is "Admin" or "Manager";
    public static bool CanOperateFrontDesk => AppSession.RoleName is "Admin" or "Manager" or "Receptionist";
    public static bool CanManageServiceCatalog => AppSession.RoleName is "Admin" or "Manager";
    public static bool CanCreateServiceOrder => CanOperateFrontDesk;
    public static bool CanProcessServiceOrder => AppSession.RoleName is "Admin" or "Manager" or "ServiceStaff";
}
