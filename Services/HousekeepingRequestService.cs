using BusinessObjects.Entities;
using BusinessObjects.Enums;
using Repositories;
namespace Services;

public sealed class HousekeepingRequestService : IHousekeepingRequestService
{
    private readonly IHousekeepingRequestRepository _repository;
    public HousekeepingRequestService() : this(new HousekeepingRequestRepository()) { }
    public HousekeepingRequestService(IHousekeepingRequestRepository repository) => _repository = repository;
    public Task<List<HousekeepingRequest>> GetAllAsync() => _repository.GetAllAsync();

    public async Task<ServiceResult<HousekeepingRequest>> CreateAsync(int stayId, HousekeepingRequestType type, string? note)
    {
        if (!Enum.IsDefined(type)) return ServiceResult<HousekeepingRequest>.Failure("Loai yeu cau khong hop le.");
        if (!string.IsNullOrWhiteSpace(note) && note.Trim().Length > 300) return ServiceResult<HousekeepingRequest>.Failure("Ghi chu toi da 300 ky tu.");
        if (!await _repository.IsStayActiveAsync(stayId)) return ServiceResult<HousekeepingRequest>.Failure("Chi stay dang hoat dong moi tao duoc yeu cau.");
        var request = new HousekeepingRequest { StayId = stayId, RequestType = type, Note = string.IsNullOrWhiteSpace(note) ? null : note.Trim(), Status = HousekeepingRequestStatus.Pending, RequestedAt = DateTime.Now };
        await _repository.SaveAsync(request, true);
        return ServiceResult<HousekeepingRequest>.Success(request, "Da tao yeu cau buong phong.");
    }

    public async Task<ServiceResult<HousekeepingRequest>> ChangeStatusAsync(int id, HousekeepingRequestStatus target)
    {
        if (AppSession.RoleName is not ("Admin" or "Manager" or "ServiceStaff")) return ServiceResult<HousekeepingRequest>.Failure("Ban khong co quyen xu ly yeu cau.");
        var request = await _repository.GetByIdAsync(id);
        if (request == null) return ServiceResult<HousekeepingRequest>.Failure("Khong tim thay yeu cau.");
        var allowed = (request.Status, target) switch
        {
            (HousekeepingRequestStatus.Pending, HousekeepingRequestStatus.Acknowledged) => true,
            (HousekeepingRequestStatus.Pending, HousekeepingRequestStatus.Cancelled) => true,
            (HousekeepingRequestStatus.Acknowledged, HousekeepingRequestStatus.Completed) => true,
            (HousekeepingRequestStatus.Acknowledged, HousekeepingRequestStatus.Cancelled) => true,
            _ => false
        };
        if (!allowed) return ServiceResult<HousekeepingRequest>.Failure("Chuyen trang thai yeu cau khong hop le.");
        request.Status = target;
        request.HandledByUserId = AppSession.CurrentUser?.Id;
        request.HandledAt = target is HousekeepingRequestStatus.Completed or HousekeepingRequestStatus.Cancelled ? DateTime.Now : null;
        await _repository.SaveAsync(request, false);
        return ServiceResult<HousekeepingRequest>.Success(request, "Da cap nhat yeu cau buong phong.");
    }
}
