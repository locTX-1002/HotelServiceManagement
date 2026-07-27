using BusinessObjects;
using BusinessObjects.Entities;
using BusinessObjects.Enums;
using Repositories;
using Services;

namespace HotelManagement.Tests;

public class DynamicAuthorizationTests
{
    [Fact]
    public void PermissionComesFromDatabaseMapping_NotRoleName()
    {
        TestUsers.SignInWithPermissions(PermissionCodes.ReportView);

        Assert.Equal("TestRole", AppSession.RoleName);
        Assert.True(AuthorizationPolicy.CanViewReports);
        Assert.False(AuthorizationPolicy.CanManageUsers);
    }

    [Fact]
    public void InactivePermission_IsNotLoadedIntoSession()
    {
        var permission = new Permission
        {
            Id = 1,
            PermissionCode = PermissionCodes.PaymentRecord,
            IsActive = false
        };
        var role = new Role
        {
            RoleName = "Custom",
            RolePermissions =
            [
                new RolePermission { Permission = permission, IsAllowed = true }
            ]
        };
        AppSession.SignIn(new User { Id = 10, Role = role });

        Assert.False(AuthorizationPolicy.CanRecordPayment);
    }

    [Fact]
    public async Task Requester_CannotApproveOwnRequest()
    {
        TestUsers.SignInWithPermissions(
            PermissionCodes.PaymentVoidRequest,
            PermissionCodes.PaymentVoidApprove);
        var request = new ApprovalRequest
        {
            Id = 7,
            RequestType = ApprovalRequestType.PaymentVoid,
            TargetId = 99,
            RequestedByUserId = AppSession.CurrentUser!.Id,
            Status = ApprovalRequestStatus.Pending,
            Reason = "Giao dịch nhập nhầm"
        };
        var repository = new FakeApprovalRepository(request);
        var service = new ApprovalService(repository, null!, null!, null!);

        var result = await service.ReviewAsync(request.Id, approve: true, reviewNote: null);

        Assert.False(result.Ok);
        Assert.Contains("không được tự phê duyệt", result.Message);
        Assert.False(repository.Saved);
    }

    [Fact]
    public async Task RoleCannotBothRecordAndApprovePaymentVoid()
    {
        TestUsers.SignInWithPermissions(PermissionCodes.PermissionManage);
        var permissions = new List<Permission>
        {
            new() { Id = 1, PermissionCode = PermissionCodes.PaymentRecord, IsActive = true },
            new() { Id = 2, PermissionCode = PermissionCodes.PaymentVoidApprove, IsActive = true }
        };
        var repository = new FakeAuthorizationRepository(permissions);

        var result = await new PermissionManagementService(repository)
            .SaveRolePermissionsAsync(2, [1, 2]);

        Assert.False(result.Ok);
        Assert.Contains("ghi nhận thanh toán", result.Message);
        Assert.False(repository.Saved);
    }

    /// <summary>
    /// Man cau hinh quyen la man duy nhat sua duoc bang chinh quyen permission.manage.
    /// Go quyen do khoi vai tro cuoi cung giu no la khong ai mo lai duoc, phai vao SQL go tay.
    /// </summary>
    [Fact]
    public async Task GoQuyenCauHinhKhoiVaiTroCuoiCung_BiChan()
    {
        TestUsers.SignInWithPermissions(PermissionCodes.PermissionManage);
        var config = new Permission { Id = 1, PermissionCode = PermissionCodes.PermissionManage, IsActive = true };
        var view = new Permission { Id = 2, PermissionCode = PermissionCodes.UserView, IsActive = true };
        var admin = new Role
        {
            Id = 1, RoleName = RoleNames.Admin, DisplayName = "Quản trị viên",
            IsSystemRole = true, IsActive = true,
            RolePermissions = [new RolePermission { Permission = config, IsAllowed = true }]
        };
        var repository = new FakeAuthorizationRepository([config, view], [admin]);

        // Chi giu lai user.view, bo permission.manage
        var result = await new PermissionManagementService(repository).SaveRolePermissionsAsync(1, [2]);

        Assert.False(result.Ok);
        Assert.Contains("cấu hình phân quyền", result.Message);
        Assert.False(repository.Saved);
    }

    /// <summary>Con vai tro khac giu quyen cau hinh thi go duoc, khong chan oan.</summary>
    [Fact]
    public async Task GoQuyenCauHinhKhiVaiTroKhacVanGiu_ChoPhep()
    {
        TestUsers.SignInWithPermissions(PermissionCodes.PermissionManage);
        var config = new Permission { Id = 1, PermissionCode = PermissionCodes.PermissionManage, IsActive = true };
        var view = new Permission { Id = 2, PermissionCode = PermissionCodes.UserView, IsActive = true };
        var admin = new Role
        {
            Id = 1, RoleName = RoleNames.Admin, DisplayName = "Quản trị viên",
            IsSystemRole = true, IsActive = true,
            RolePermissions = [new RolePermission { Permission = config, IsAllowed = true }]
        };
        var phu = new Role
        {
            Id = 5, RoleName = "AdminPhu", DisplayName = "Quản trị phụ", IsActive = true,
            RolePermissions = [new RolePermission { Permission = config, IsAllowed = true }]
        };
        var repository = new FakeAuthorizationRepository([config, view], [admin, phu]);

        var result = await new PermissionManagementService(repository).SaveRolePermissionsAsync(1, [2]);

        Assert.True(result.Ok);
        Assert.True(repository.Saved);
    }

    [Fact]
    public async Task VaiTroHeThongBoHetQuyen_BiChan()
    {
        TestUsers.SignInWithPermissions(PermissionCodes.PermissionManage);
        var config = new Permission { Id = 1, PermissionCode = PermissionCodes.PermissionManage, IsActive = true };
        var admin = new Role
        {
            Id = 1, RoleName = RoleNames.Admin, DisplayName = "Quản trị viên",
            IsSystemRole = true, IsActive = true,
            RolePermissions = [new RolePermission { Permission = config, IsAllowed = true }]
        };
        var letan = new Role
        {
            Id = 3, RoleName = RoleNames.Receptionist, DisplayName = "Lễ tân",
            IsSystemRole = true, IsActive = true, RolePermissions = []
        };
        var repository = new FakeAuthorizationRepository([config], [admin, letan]);

        var result = await new PermissionManagementService(repository).SaveRolePermissionsAsync(3, []);

        Assert.False(result.Ok);
        Assert.Contains("ít nhất một quyền", result.Message);
        Assert.False(repository.Saved);
    }

    private sealed class FakeApprovalRepository(ApprovalRequest request)
        : IApprovalRequestRepository
    {
        public bool Saved { get; private set; }
        public Task<List<ApprovalRequest>> GetPendingAsync() => Task.FromResult(new List<ApprovalRequest> { request });
        public Task<ApprovalRequest?> GetByIdAsync(int id) => Task.FromResult<ApprovalRequest?>(request);
        public Task<bool> HasPendingAsync(ApprovalRequestType type, int targetId) => Task.FromResult(false);
        public Task SaveAsync(ApprovalRequest value, bool add) { Saved = true; return Task.CompletedTask; }
        public Task AddAuditAsync(AuditLog log) => Task.CompletedTask;
    }

    private sealed class FakeAuthorizationRepository(List<Permission> permissions, List<Role>? roles = null)
        : IAuthorizationRepository
    {
        public bool Saved { get; private set; }
        public Task<List<Role>> GetRolesWithPermissionsAsync() => Task.FromResult(roles ?? []);
        public Task<List<Permission>> GetPermissionsAsync() => Task.FromResult(permissions);
        public Task<bool> ReplaceRolePermissionsAsync(int roleId, IReadOnlyCollection<int> permissionIds)
        {
            Saved = true;
            return Task.FromResult(true);
        }
    }
}
