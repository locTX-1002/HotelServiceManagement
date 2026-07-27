using BusinessObjects.Entities;
using Repositories;

namespace Services;

/// <summary>
/// Ghi nhat ky cho cac thao tac quan tri. Goi tu service, KHONG goi tu ViewModel:
/// ghi o ViewModel thi ai goi service bang duong khac se khong de lai dau vet.
///
/// Ghi nhat ky hong KHONG duoc lam hong nghiep vu: tai khoan da tao xong roi ma bao loi
/// vi khong ghi duoc log thi nguoi dung se tao lai lan nua. Nen nuot loi tai day.
/// </summary>
public static class AuditTrail
{
    private static IAuditLogRepository _repository = new AuditLogRepository();

    /// <summary>Doi kho luu - chi dung cho test, khong goi trong code chay that.</summary>
    internal static void UseRepository(IAuditLogRepository repository) => _repository = repository;

    public static async Task WriteAsync(string actionCode, string entityType, int? entityId,
        string? oldValues, string? newValues, bool succeeded = true)
    {
        try
        {
            await _repository.AddAsync(new AuditLog
            {
                UserId = AppSession.CurrentUser?.Id,
                ActionCode = actionCode,
                EntityType = entityType,
                EntityId = entityId,
                OldValues = Trim(oldValues),
                NewValues = Trim(newValues),
                CreatedAt = DateTime.Now,
                Succeeded = succeeded,
            });
        }
        catch (Exception)
        {
            // Mat mot dong nhat ky con hon hong ca thao tac nguoi dung vua lam.
        }
    }

    /// <summary>Cot OldValues/NewValues gioi han 4000 ky tu - cat truoc cho khoi loi luu.</summary>
    private static string? Trim(string? value)
        => value != null && value.Length > 4000 ? value[..4000] : value;
}
