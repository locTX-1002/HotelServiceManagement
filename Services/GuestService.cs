using System.Net.Mail;
using BusinessObjects.Entities;
using BusinessObjects.Enums;
using Microsoft.EntityFrameworkCore;
using Repositories;

namespace Services;

public sealed class GuestService : IGuestService
{
    private readonly IGuestRepository _repository;

    public GuestService() : this(new GuestRepository()) { }
    public GuestService(IGuestRepository repository) => _repository = repository;

    public Task<List<Guest>> GetAllAsync() => _repository.GetAllAsync();
    public Task<List<Guest>> SearchAsync(string keyword) => string.IsNullOrWhiteSpace(keyword)
        ? _repository.GetAllAsync() : _repository.SearchAsync(keyword);

    public async Task<ServiceResult<Guest>> CreateAsync(string fullName, string? email,
        string phoneNumber, string? identityNumber, GuestTag tag, string? tagNote)
    {
        if (!AuthorizationPolicy.CanOperateFrontDesk)
            return ServiceResult<Guest>.Failure("Bạn không có quyền tạo hồ sơ khách hàng.");
        var error = Validate(fullName, email, phoneNumber, identityNumber, tag, tagNote);
        if (error != null) return ServiceResult<Guest>.Failure(error);

        var identity = Normalize(identityNumber);
        if (identity != null && await _repository.IdentityNumberExistsAsync(identity))
            return ServiceResult<Guest>.Failure("Số giấy tờ đã tồn tại.");

        var guest = new Guest
        {
            FullName = fullName.Trim(),
            Email = Normalize(email),
            PhoneNumber = phoneNumber.Trim(),
            IdentityNumber = identity,
            Tag = tag,
            TagNote = tag == GuestTag.None ? null : Normalize(tagNote),
        };

        try
        {
            await _repository.AddAsync(guest);
        }
        catch (DbUpdateException)
        {
            if (identity != null && await _repository.IdentityNumberExistsAsync(identity))
                return ServiceResult<Guest>.Failure("Số giấy tờ đã tồn tại.");
            throw;
        }

        return ServiceResult<Guest>.Success(guest, "Đã tạo hồ sơ khách hàng.");
    }

    public async Task<ServiceResult<Guest>> UpdateAsync(int id, string fullName, string? email,
        string phoneNumber, string? identityNumber, GuestTag tag, string? tagNote)
    {
        if (!AuthorizationPolicy.CanOperateFrontDesk)
            return ServiceResult<Guest>.Failure("Bạn không có quyền sửa hồ sơ khách hàng.");
        var error = Validate(fullName, email, phoneNumber, identityNumber, tag, tagNote);
        if (error != null) return ServiceResult<Guest>.Failure(error);
        var guest = await _repository.GetByIdAsync(id);
        if (guest == null) return ServiceResult<Guest>.Failure("Không tìm thấy khách hàng.");

        var identity = Normalize(identityNumber);
        if (identity != null && await _repository.IdentityNumberExistsAsync(identity, id))
            return ServiceResult<Guest>.Failure("Số giấy tờ đã tồn tại.");

        guest.FullName = fullName.Trim();
        guest.Email = Normalize(email);
        guest.PhoneNumber = phoneNumber.Trim();
        guest.IdentityNumber = identity;
        guest.Tag = tag;
        guest.TagNote = tag == GuestTag.None ? null : Normalize(tagNote);
        await _repository.UpdateAsync(guest);
        return ServiceResult<Guest>.Success(guest, "Đã cập nhật hồ sơ khách hàng.");
    }

    public async Task<ServiceResult> DeleteAsync(int id)
    {
        if (!AuthorizationPolicy.CanOperateFrontDesk)
            return ServiceResult.Failure("Bạn không có quyền xoá hồ sơ khách hàng.");
        var guest = await _repository.GetByIdAsync(id);
        if (guest == null) return ServiceResult.Failure("Không tìm thấy khách hàng.");
        if (await _repository.HasReservationsAsync(id))
            return ServiceResult.Failure("Khách hàng đã có lịch sử đặt phòng nên không xoá được.");
        await _repository.DeleteAsync(guest);
        return ServiceResult.Success("Đã xoá hồ sơ khách hàng.");
    }

    private static string? Validate(string fullName, string? email, string phoneNumber,
        string? identityNumber, GuestTag tag, string? tagNote)
    {
        if (string.IsNullOrWhiteSpace(fullName)) return "Chưa nhập họ tên.";
        if (fullName.Trim().Length > 100) return "Họ tên tối đa 100 ký tự.";

        // Dung InputPolicy chung ca app. Truoc day chi kiem do dai + MailAddress, ma
        // MailAddress chap nhan ca "mana@nn." nen du lieu kieu do da lot vao DB.
        var phoneError = InputPolicy.ValidatePhone(phoneNumber, required: true);
        if (phoneError != null) return phoneError;

        if (!string.IsNullOrWhiteSpace(email) && email.Trim().Length > 150)
            return "Email tối đa 150 ký tự.";
        var emailError = InputPolicy.ValidateEmail(email, required: false);
        if (emailError != null) return emailError;

        var identityError = InputPolicy.ValidateIdentity(identityNumber, required: false);
        if (identityError != null) return identityError;
        if (!Enum.IsDefined(tag)) return "Nhóm khách hàng không hợp lệ.";
        if (!string.IsNullOrWhiteSpace(tagNote) && tagNote.Trim().Length > 300)
            return "Ghi chú tối đa 300 ký tự.";
        return null;
    }

    private static string? Normalize(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    public Task<Guest?> FindExactAsync(string idOrPhone) => _repository.FindExactAsync(idOrPhone);
}
