using BusinessObjects;

namespace Services;

internal static class AuthorizationPolicy
{
    public static bool CanManageRooms => AppSession.RoleName is RoleNames.Admin or RoleNames.Manager;
    public static bool CanOperateFrontDesk => AppSession.RoleName is RoleNames.Admin or RoleNames.Manager or RoleNames.Receptionist;
    public static bool CanManageServiceCatalog => AppSession.RoleName is RoleNames.Admin or RoleNames.Manager;
    public static bool CanCreateServiceOrder => CanOperateFrontDesk;
    public static bool CanProcessServiceOrder => AppSession.RoleName is RoleNames.Admin or RoleNames.Manager or RoleNames.ServiceStaff;
}
