using BusinessObjects;
using BusinessObjects.Entities;
using BusinessObjects.Enums;
using DataAccessObjects;
using Microsoft.EntityFrameworkCore;
using Services;

namespace HotelManagement.Tests;

public class ApprovalConcurrencyTests
{
    [DbFact]
    public async Task TwoConcurrentApprovals_SameRequest_BusinessActionRunsOnce()
    {
        var requester = await TestUsers.GetAsync(RoleNames.Receptionist);
        var reviewer = await TestUsers.GetAsync(RoleNames.Manager);
        int roomId;
        int requestId;

        await using (var setup = HotelDbContextFactory.Create())
        {
            var roomTypeId = await setup.RoomTypes.Where(x => x.IsActive)
                .Select(x => x.Id).FirstAsync();
            var room = new Room
            {
                RoomNumber = $"T{Guid.NewGuid():N}"[..12].ToUpperInvariant(),
                Floor = 99,
                RoomTypeId = roomTypeId,
                Status = RoomStatus.Available,
                IsActive = true
            };
            setup.Rooms.Add(room);
            await setup.SaveChangesAsync();
            roomId = room.Id;

            var request = new ApprovalRequest
            {
                RequestType = ApprovalRequestType.RoomMaintenance,
                TargetId = roomId,
                Reason = "Kiểm thử chống duyệt trùng",
                Status = ApprovalRequestStatus.Pending,
                RequestedByUserId = requester.Id,
                RequestedAt = DateTime.Now
            };
            setup.ApprovalRequests.Add(request);
            await setup.SaveChangesAsync();
            requestId = request.Id;
        }

        try
        {
            AppSession.SignIn(reviewer);
            var first = new ApprovalService();
            var second = new ApprovalService();

            var results = await Task.WhenAll(
                first.ReviewAsync(requestId, approve: true, reviewNote: null),
                second.ReviewAsync(requestId, approve: true, reviewNote: null));

            Assert.Single(results, x => x.Ok);
            Assert.Single(results, x => !x.Ok);

            await using var verify = HotelDbContextFactory.Create();
            var request = await verify.ApprovalRequests.AsNoTracking()
                .SingleAsync(x => x.Id == requestId);
            var room = await verify.Rooms.AsNoTracking().SingleAsync(x => x.Id == roomId);

            Assert.Equal(ApprovalRequestStatus.Approved, request.Status);
            Assert.Equal(reviewer.Id, request.ReviewedByUserId);
            Assert.Equal(RoomStatus.Maintenance, room.Status);
            Assert.Single(await verify.AuditLogs.AsNoTracking()
                .Where(x => x.EntityType == nameof(ApprovalRequest)
                    && x.EntityId == requestId
                    && x.ActionCode == "approval.approve"
                    && x.Succeeded)
                .ToListAsync());
        }
        finally
        {
            await CleanupAsync(requestId, roomId);
            await TestUsers.CleanupAsync();
        }
    }

    [DbFact]
    public async Task ApprovedBusinessActionFailure_RequestRemainsPending()
    {
        var requester = await TestUsers.GetAsync(RoleNames.Receptionist);
        var reviewer = await TestUsers.GetAsync(RoleNames.Manager);
        int requestId;

        await using (var setup = HotelDbContextFactory.Create())
        {
            var request = new ApprovalRequest
            {
                RequestType = ApprovalRequestType.RoomMaintenance,
                TargetId = int.MaxValue,
                Reason = "Đối tượng không tồn tại để kiểm thử rollback",
                Status = ApprovalRequestStatus.Pending,
                RequestedByUserId = requester.Id,
                RequestedAt = DateTime.Now
            };
            setup.ApprovalRequests.Add(request);
            await setup.SaveChangesAsync();
            requestId = request.Id;
        }

        try
        {
            AppSession.SignIn(reviewer);
            var result = await new ApprovalService()
                .ReviewAsync(requestId, approve: true, reviewNote: null);

            Assert.False(result.Ok);

            await using var verify = HotelDbContextFactory.Create();
            var request = await verify.ApprovalRequests.AsNoTracking()
                .SingleAsync(x => x.Id == requestId);
            Assert.Equal(ApprovalRequestStatus.Pending, request.Status);
            Assert.Null(request.ReviewedByUserId);
            Assert.Null(request.ReviewedAt);
        }
        finally
        {
            await CleanupAsync(requestId, null);
            await TestUsers.CleanupAsync();
        }
    }


    [DbFact]
    public async Task HistorySearch_FiltersRequester_AndReturnsReadableTargetName()
    {
        var requester = await TestUsers.GetAsync(RoleNames.Receptionist);
        var reviewer = await TestUsers.GetAsync(RoleNames.Manager);
        int roomId;
        int requestId;
        string roomNumber;

        await using (var setup = HotelDbContextFactory.Create())
        {
            var roomTypeId = await setup.RoomTypes.Where(x => x.IsActive)
                .Select(x => x.Id).FirstAsync();
            roomNumber = $"S{Guid.NewGuid():N}"[..12].ToUpperInvariant();
            var room = new Room
            {
                RoomNumber = roomNumber,
                Floor = 98,
                RoomTypeId = roomTypeId,
                Status = RoomStatus.Available,
                IsActive = true
            };
            setup.Rooms.Add(room);
            await setup.SaveChangesAsync();
            roomId = room.Id;

            var request = new ApprovalRequest
            {
                RequestType = ApprovalRequestType.RoomMaintenance,
                TargetId = roomId,
                Reason = "Kiểm thử tìm người gửi và tên đối tượng",
                Status = ApprovalRequestStatus.Pending,
                RequestedByUserId = requester.Id,
                RequestedAt = DateTime.Now
            };
            setup.ApprovalRequests.Add(request);
            await setup.SaveChangesAsync();
            requestId = request.Id;
        }

        try
        {
            AppSession.SignIn(reviewer);
            var result = await new ApprovalService().SearchAsync(
                ApprovalRequestStatus.Pending,
                ApprovalRequestType.RoomMaintenance,
                requester.FullName);

            Assert.True(result.Ok, result.Message);
            Assert.NotNull(result.Data);
            var item = Assert.Single(result.Data!, x => x.Request.Id == requestId);
            Assert.Equal($"Phòng {roomNumber}", item.TargetDisplayName);
        }
        finally
        {
            await CleanupAsync(requestId, roomId);
            await TestUsers.CleanupAsync();
        }
    }

    private static async Task CleanupAsync(int requestId, int? roomId)
    {
        await using var db = HotelDbContextFactory.Create();
        var audits = await db.AuditLogs.Where(x => x.EntityType == nameof(ApprovalRequest)
            && x.EntityId == requestId).ToListAsync();
        db.AuditLogs.RemoveRange(audits);

        var request = await db.ApprovalRequests.FindAsync(requestId);
        if (request != null) db.ApprovalRequests.Remove(request);

        if (roomId.HasValue)
        {
            var room = await db.Rooms.FindAsync(roomId.Value);
            if (room != null) db.Rooms.Remove(room);
        }
        await db.SaveChangesAsync();
    }
}
