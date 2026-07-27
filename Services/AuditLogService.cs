using BusinessObjects.Entities;
using Repositories;

namespace Services;

/// <summary>
/// Tra cuu nhat ky he thong. Chi vai tro co quyen <c>audit.view</c> doc duoc - mac dinh
/// la Quan tri vien.
/// </summary>
public sealed class AuditLogService : IAuditLogService
{
    /// <summary>
    /// Tran so dong tra ve. Mot man hinh khong ai doc het 500 dong; lay khong gioi han
    /// thi den cuoi ky bang nay vai chuc nghin dong, mo man la treo.
    /// </summary>
    private const int MaxRows = 500;

    private readonly IAuditLogRepository _repository;
    public AuditLogService() : this(new AuditLogRepository()) { }
    public AuditLogService(IAuditLogRepository repository) => _repository = repository;

    public async Task<ServiceResult<List<AuditLog>>> SearchAsync(
        DateTime from, DateTime to, int? userId, string? actionCode)
    {
        if (!AuthorizationPolicy.CanViewAuditLog)
            return ServiceResult<List<AuditLog>>.Failure("Bạn không có quyền xem nhật ký hệ thống.");
        if (from.Date > to.Date)
            return ServiceResult<List<AuditLog>>.Failure("Ngày bắt đầu phải trước ngày kết thúc.");

        // to.Date.AddDays(1) de lay tron ca ngay cuoi, khong bo sot viec lam buoi chieu.
        var rows = await _repository.SearchAsync(from.Date, to.Date.AddDays(1), userId, actionCode, MaxRows);
        return ServiceResult<List<AuditLog>>.Success(rows);
    }

    public async Task<ServiceResult<List<string>>> GetActionCodesAsync()
        => !AuthorizationPolicy.CanViewAuditLog
            ? ServiceResult<List<string>>.Failure("Bạn không có quyền xem nhật ký hệ thống.")
            : ServiceResult<List<string>>.Success(await _repository.GetActionCodesAsync());
}
