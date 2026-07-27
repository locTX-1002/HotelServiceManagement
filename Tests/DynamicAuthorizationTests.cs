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

    private sealed class FakeApprovalRepository(ApprovalRequest request)
        : IApprovalRequestRepository
    {
        public bool Saved { get; private set; }
        public Task<List<ApprovalRequest>> GetPendingAsync() => Task.FromResult(new List<ApprovalRequest> { request });
        public Task<List<ApprovalRequest>> SearchAsync(ApprovalRequestStatus status, ApprovalRequestType? type,
            string? requesterKeyword, DateTime? fromDate, DateTime? toDate)
            => Task.FromResult(new List<ApprovalRequest> { request });
        public Task<Dictionary<(ApprovalRequestType Type, int TargetId), string>>
            GetTargetDisplayNamesAsync(IReadOnlyCollection<ApprovalRequest> requests)
            => Task.FromResult(new Dictionary<(ApprovalRequestType Type, int TargetId), string>
            {
                [(request.RequestType, request.TargetId)] = "Test target"
            });
        public Task<ApprovalRequest?> GetByIdAsync(int id) => Task.FromResult<ApprovalRequest?>(request);
        public Task<ApprovalRequest?> GetByIdForReviewAsync(int id) => Task.FromResult<ApprovalRequest?>(request);
        public Task<bool> HasPendingAsync(ApprovalRequestType type, int targetId) => Task.FromResult(false);
        public Task SaveAsync(ApprovalRequest value, bool add) { Saved = true; return Task.CompletedTask; }
        public Task AddAuditAsync(AuditLog log) => Task.CompletedTask;
        public Task<T> ExecuteSerializableAsync<T>(Func<Task<T>> operation) => operation();
    }

    private sealed class FakeAuthorizationRepository(List<Permission> permissions)
        : IAuthorizationRepository
    {
        public bool Saved { get; private set; }
        public Task<List<Role>> GetRolesWithPermissionsAsync() => Task.FromResult(new List<Role>());
        public Task<List<Permission>> GetPermissionsAsync() => Task.FromResult(permissions);
        public Task<bool> ReplaceRolePermissionsAsync(int roleId, IReadOnlyCollection<int> permissionIds)
        {
            Saved = true;
            return Task.FromResult(true);
        }
    }
}
