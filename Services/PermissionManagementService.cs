using BusinessObjects;
using BusinessObjects.Entities;
using Repositories;

namespace Services;

public sealed class PermissionManagementService : IPermissionManagementService
{
    private readonly IAuthorizationRepository _repository;
    public PermissionManagementService() : this(new AuthorizationRepository()) { }
    public PermissionManagementService(IAuthorizationRepository repository) => _repository = repository;

    public async Task<ServiceResult<List<Role>>> GetRolesAsync()
        => !AuthorizationPolicy.CanManagePermissions
            ? ServiceResult<List<Role>>.Failure("Bạn không có quyền cấu hình phân quyền.")
            : ServiceResult<List<Role>>.Success(await _repository.GetRolesWithPermissionsAsync());

    public async Task<ServiceResult<List<Permission>>> GetPermissionsAsync()
        => !AuthorizationPolicy.CanManagePermissions
            ? ServiceResult<List<Permission>>.Failure("Bạn không có quyền cấu hình phân quyền.")
            : ServiceResult<List<Permission>>.Success(await _repository.GetPermissionsAsync());

    public async Task<ServiceResult> SaveRolePermissionsAsync(
        int roleId, IReadOnlyCollection<int> permissionIds)
    {
        if (!AuthorizationPolicy.CanManagePermissions)
            return ServiceResult.Failure("Bạn không có quyền cấu hình phân quyền.");
        var permissions = await _repository.GetPermissionsAsync();
        var selected = permissions.Where(x => permissionIds.Contains(x.Id))
            .Select(x => x.PermissionCode).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var conflict = ValidateSeparation(selected);
        if (conflict != null) return ServiceResult.Failure(conflict);
        return await _repository.ReplaceRolePermissionsAsync(roleId, permissionIds)
            ? ServiceResult.Success("Đã lưu quyền. Người dùng thuộc vai trò này cần đăng nhập lại.")
            : ServiceResult.Failure("Không lưu được quyền vì vai trò hoặc quyền không hợp lệ.");
    }

    internal static string? ValidateSeparation(IReadOnlySet<string> selected)
    {
        var businessPermissions = selected.Where(x =>
            !x.StartsWith("user.", StringComparison.OrdinalIgnoreCase)
            && !x.StartsWith("permission.", StringComparison.OrdinalIgnoreCase)
            && !x.StartsWith("audit.", StringComparison.OrdinalIgnoreCase)).ToList();
        if ((selected.Contains(PermissionCodes.UserManage)
             || selected.Contains(PermissionCodes.PermissionManage))
            && businessPermissions.Count > 0)
            return "Vai trò quản trị hệ thống không được đồng thời giữ quyền nghiệp vụ.";

        var conflicts = new (string Request, string Approve, string Message)[]
        {
            (PermissionCodes.ReservationCancelRequest, PermissionCodes.ReservationCancelApprove, "Không thể vừa yêu cầu vừa duyệt huỷ đặt phòng."),
            (PermissionCodes.InvoiceDiscountRequest, PermissionCodes.InvoiceDiscountApprove, "Không thể vừa yêu cầu vừa duyệt giảm giá."),
            (PermissionCodes.InvoiceCancelRequest, PermissionCodes.InvoiceCancelApprove, "Không thể vừa yêu cầu vừa duyệt huỷ hoá đơn."),
            (PermissionCodes.PaymentVoidRequest, PermissionCodes.PaymentVoidApprove, "Không thể vừa yêu cầu vừa duyệt huỷ giao dịch."),
            (PermissionCodes.RoomMaintenanceRequest, PermissionCodes.RoomMaintenanceApprove, "Không thể vừa yêu cầu vừa duyệt bảo trì phòng.")
        };
        foreach (var conflict in conflicts)
            if (selected.Contains(conflict.Request) && selected.Contains(conflict.Approve))
                return conflict.Message;

        if (selected.Contains(PermissionCodes.PaymentRecord)
            && selected.Contains(PermissionCodes.PaymentVoidApprove))
            return "Người ghi nhận thanh toán không được đồng thời duyệt huỷ giao dịch.";
        if (selected.Contains(PermissionCodes.InvoicePrepare)
            && selected.Contains(PermissionCodes.InvoiceDiscountApprove))
            return "Người lập hoá đơn không được đồng thời duyệt giảm giá.";
        return null;
    }
}
