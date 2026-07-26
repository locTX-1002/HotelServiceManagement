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
}
