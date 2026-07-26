using BusinessObjects;
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
        if (!Enum.IsDefined(type)) return ServiceResult<HousekeepingRequest>.Failure("Loại yêu cầu không hợp lệ.");
        if (!string.IsNullOrWhiteSpace(note) && note.Trim().Length > 300) return ServiceResult<HousekeepingRequest>.Failure("Ghi chú tối đa 300 ký tự.");
        if (!await _repository.IsStayActiveAsync(stayId)) return ServiceResult<HousekeepingRequest>.Failure("Chỉ lượt lưu trú đang hoạt động mới tạo yêu cầu được.");
        var request = new HousekeepingRequest { StayId = stayId, RequestType = type, Note = string.IsNullOrWhiteSpace(note) ? null : note.Trim(), Status = HousekeepingRequestStatus.Pending, RequestedAt = DateTime.Now };
        await _repository.SaveAsync(request, true);
        return ServiceResult<HousekeepingRequest>.Success(request, "Đã tạo yêu cầu buồng phòng.");
    }

    public async Task<ServiceResult<HousekeepingRequest>> ChangeStatusAsync(int id, HousekeepingRequestStatus target)
    {
        if (!AuthorizationPolicy.CanProcessServiceOrder) return ServiceResult<HousekeepingRequest>.Failure("Bạn không có quyền xử lý yêu cầu.");
        var request = await _repository.GetByIdAsync(id);
        if (request == null) return ServiceResult<HousekeepingRequest>.Failure("Không tìm thấy yêu cầu.");
        var allowed = (request.Status, target) switch
        {
            (HousekeepingRequestStatus.Pending, HousekeepingRequestStatus.Acknowledged) => true,
            (HousekeepingRequestStatus.Pending, HousekeepingRequestStatus.Cancelled) => true,
            (HousekeepingRequestStatus.Acknowledged, HousekeepingRequestStatus.Completed) => true,
            (HousekeepingRequestStatus.Acknowledged, HousekeepingRequestStatus.Cancelled) => true,
            _ => false
        };
        if (!allowed) return ServiceResult<HousekeepingRequest>.Failure("Chuyển trạng thái yêu cầu không hợp lệ.");
        request.Status = target;
        request.HandledByUserId = AppSession.CurrentUser?.Id;
        request.HandledAt = target is HousekeepingRequestStatus.Completed or HousekeepingRequestStatus.Cancelled ? DateTime.Now : null;
        await _repository.SaveAsync(request, false);
        return ServiceResult<HousekeepingRequest>.Success(request, "Đã cập nhật yêu cầu buồng phòng.");
    }
}
