using System.Data;
using BusinessObjects.Entities;
using BusinessObjects.Enums;
using Microsoft.EntityFrameworkCore;

namespace DataAccessObjects;

public sealed class ApprovalRequestDAO
{
    private static readonly Lazy<ApprovalRequestDAO> LazyInstance = new(() => new ApprovalRequestDAO());
    public static ApprovalRequestDAO Instance => LazyInstance.Value;
    private ApprovalRequestDAO() { }

    public Task<T> ExecuteSerializableAsync<T>(Func<Task<T>> operation)
        => HotelDbContextFactory.ExecuteInTransactionAsync(IsolationLevel.Serializable, operation);

    public async Task<List<ApprovalRequest>> GetPendingAsync()
        => await SearchAsync(ApprovalRequestStatus.Pending, null, null, null, null);

    public async Task<List<ApprovalRequest>> SearchAsync(
        ApprovalRequestStatus status,
        ApprovalRequestType? type,
        string? requesterKeyword,
        DateTime? fromDate,
        DateTime? toDate)
    {
        await using var db = HotelDbContextFactory.Create();
        var query = db.ApprovalRequests.AsNoTracking()
            .Include(x => x.RequestedByUser)
            .Include(x => x.RequestedByGuest)
            .Include(x => x.ReviewedByUser)
            .Where(x => x.Status == status);

        if (type.HasValue)
        {
            query = query.Where(x => x.RequestType == type.Value);
        }

        if (!string.IsNullOrWhiteSpace(requesterKeyword))
        {
            var keyword = requesterKeyword.Trim();
            query = query.Where(x =>
                (x.RequestedByUser != null
                    && (x.RequestedByUser.FullName.Contains(keyword)
                        || x.RequestedByUser.Email.Contains(keyword)))
                || (x.RequestedByGuest != null
                    && (x.RequestedByGuest.FullName.Contains(keyword)
                        || x.RequestedByGuest.PhoneNumber.Contains(keyword)
                        || (x.RequestedByGuest.Email != null
                            && x.RequestedByGuest.Email.Contains(keyword)))));
        }

        if (fromDate.HasValue)
        {
            var from = fromDate.Value.Date;
            query = query.Where(x => x.RequestedAt >= from);
        }

        if (toDate.HasValue)
        {
            var toExclusive = toDate.Value.Date.AddDays(1);
            query = query.Where(x => x.RequestedAt < toExclusive);
        }

        var requests = status == ApprovalRequestStatus.Pending
            ? await query.OrderBy(x => x.RequestedAt).ToListAsync()
            : await query.OrderByDescending(x => x.ReviewedAt ?? x.RequestedAt).ToListAsync();

        return requests;
    }

    public async Task<Dictionary<(ApprovalRequestType Type, int TargetId), string>>
        GetTargetDisplayNamesAsync(IReadOnlyCollection<ApprovalRequest> requests)
    {
        if (requests.Count == 0) return [];

        await using var db = HotelDbContextFactory.Create();
        var names = new Dictionary<(ApprovalRequestType Type, int TargetId), string>();

        var reservationIds = requests
            .Where(x => x.RequestType == ApprovalRequestType.ReservationCancel)
            .Select(x => x.TargetId).Distinct().ToList();
        if (reservationIds.Count > 0)
        {
            var reservations = await db.Reservations.AsNoTracking()
                .Where(x => reservationIds.Contains(x.Id))
                .Select(x => new { x.Id, GuestName = x.Guest.FullName })
                .ToListAsync();
            foreach (var item in reservations)
                names[(ApprovalRequestType.ReservationCancel, item.Id)] = item.GuestName;
        }

        // InvoiceDiscount stores Stay.Id because the approved business action calls
        // ApplyApprovedDiscountAsync(stayId, ...). InvoiceCancel stores Invoice.Id.
        // Resolve them separately so the UI never looks up a Stay.Id in the Invoices table.
        var discountStayIds = requests
            .Where(x => x.RequestType == ApprovalRequestType.InvoiceDiscount)
            .Select(x => x.TargetId).Distinct().ToList();
        if (discountStayIds.Count > 0)
        {
            var stays = await db.Stays.AsNoTracking()
                .Where(x => discountStayIds.Contains(x.Id))
                .Select(x => new { x.Id, GuestName = x.Reservation.Guest.FullName })
                .ToListAsync();
            foreach (var item in stays)
                names[(ApprovalRequestType.InvoiceDiscount, item.Id)] = item.GuestName;
        }

        var invoiceIds = requests
            .Where(x => x.RequestType == ApprovalRequestType.InvoiceCancel)
            .Select(x => x.TargetId).Distinct().ToList();
        if (invoiceIds.Count > 0)
        {
            var invoices = await db.Invoices.AsNoTracking()
                .Where(x => invoiceIds.Contains(x.Id))
                .Select(x => new { x.Id, GuestName = x.Stay.Reservation.Guest.FullName })
                .ToListAsync();
            foreach (var item in invoices)
                names[(ApprovalRequestType.InvoiceCancel, item.Id)] = item.GuestName;
        }

        var paymentIds = requests
            .Where(x => x.RequestType == ApprovalRequestType.PaymentVoid)
            .Select(x => x.TargetId).Distinct().ToList();
        if (paymentIds.Count > 0)
        {
            var payments = await db.Payments.AsNoTracking()
                .Where(x => paymentIds.Contains(x.Id))
                .Select(x => new { x.Id, GuestName = x.Invoice.Stay.Reservation.Guest.FullName })
                .ToListAsync();
            foreach (var item in payments)
                names[(ApprovalRequestType.PaymentVoid, item.Id)] = item.GuestName;
        }

        var roomIds = requests
            .Where(x => x.RequestType == ApprovalRequestType.RoomMaintenance)
            .Select(x => x.TargetId).Distinct().ToList();
        if (roomIds.Count > 0)
        {
            var rooms = await db.Rooms.AsNoTracking()
                .Where(x => roomIds.Contains(x.Id))
                .Select(x => new { x.Id, x.RoomNumber })
                .ToListAsync();
            foreach (var item in rooms)
                names[(ApprovalRequestType.RoomMaintenance, item.Id)] = $"Phòng {item.RoomNumber}";
        }

        foreach (var request in requests)
        {
            var key = (request.RequestType, request.TargetId);
            if (!names.ContainsKey(key)) names[key] = "Đối tượng không còn tồn tại";
        }

        return names;
    }

    public async Task<ApprovalRequest?> GetByIdAsync(int id)
    {
        await using var db = HotelDbContextFactory.Create();
        return await db.ApprovalRequests.AsNoTracking()
            .Include(x => x.RequestedByUser)
            .Include(x => x.RequestedByGuest)
            .Include(x => x.ReviewedByUser)
            .FirstOrDefaultAsync(x => x.Id == id);
    }

    /// <summary>
    /// Doc request de review voi UPDLOCK + HOLDLOCK. Ham nay phai duoc goi ben trong
    /// ExecuteSerializableAsync. Manager thu hai se doi transaction thu nhat ket thuc,
    /// sau do doc lai trang thai moi thay vi cung thay Pending va thuc thi nghiep vu lan 2.
    /// </summary>
    public async Task<ApprovalRequest?> GetByIdForReviewAsync(int id)
    {
        await using var db = HotelDbContextFactory.Create();
        return await db.ApprovalRequests
            .FromSqlInterpolated($"SELECT * FROM [ApprovalRequests] WITH (UPDLOCK, ROWLOCK, HOLDLOCK) WHERE [Id] = {id}")
            .AsNoTracking()
            .FirstOrDefaultAsync();
    }

    public async Task<bool> HasPendingAsync(ApprovalRequestType type, int targetId)
    {
        await using var db = HotelDbContextFactory.Create();
        return await db.ApprovalRequests.AnyAsync(x =>
            x.RequestType == type && x.TargetId == targetId
            && x.Status == ApprovalRequestStatus.Pending);
    }

    public async Task<List<int>> GetPendingTargetIdsForGuestAsync(
        ApprovalRequestType type, int guestId)
    {
        await using var db = HotelDbContextFactory.Create();
        return await db.ApprovalRequests.AsNoTracking()
            .Where(x => x.RequestType == type
                        && x.RequestedByGuestId == guestId
                        && x.Status == ApprovalRequestStatus.Pending)
            .Select(x => x.TargetId)
            .Distinct()
            .ToListAsync();
    }

    public async Task SaveAsync(ApprovalRequest request, bool add)
    {
        await using var db = HotelDbContextFactory.Create();
        request.RequestedByUser = null;
        request.RequestedByGuest = null;
        request.ReviewedByUser = null;
        if (add) db.ApprovalRequests.Add(request); else db.ApprovalRequests.Update(request);
        await db.SaveChangesAsync();
    }

    public async Task AddAuditAsync(AuditLog log)
    {
        await using var db = HotelDbContextFactory.Create();
        db.AuditLogs.Add(log);
        await db.SaveChangesAsync();
    }
}
