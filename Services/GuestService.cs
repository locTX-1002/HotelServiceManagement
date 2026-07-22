using System.Linq;
using BusinessObjects.Entities;
using BusinessObjects.Enums;
using Repositories;

namespace Services
{
    /// <summary>Nghiệp vụ Khách hàng - port rule từ web: CCCD unique khi lễ tân tạo, tag VIP/Blacklist.</summary>
    public class GuestService : IGuestService
    {
        private readonly IGuestRepository _repo = new GuestRepository();

        public Task<List<Guest>> SearchAsync(string? keyword) => _repo.SearchAsync(keyword);
        public Task<Guest?> GetByIdAsync(int id) => _repo.GetByIdAsync(id);

        public async Task<Guest?> FindExactAsync(string idOrPhone)
        {
            var key = idOrPhone.Trim();
            if (string.IsNullOrEmpty(key))
            {
                return null;
            }
            var matches = await _repo.SearchAsync(key);
            return matches.FirstOrDefault(g => g.IdentityNumber == key || g.PhoneNumber == key);
        }

        public async Task<ServiceResult<Guest>> CreateAsync(string fullName, string phone, string? identityNumber, string? email)
        {
            var error = Validate(fullName, phone, identityNumber, requireIdentity: true);
            if (error != null)
            {
                return ServiceResult<Guest>.Failure(error);
            }

            var id = identityNumber!.Trim();
            if (await _repo.IdentityExistsAsync(id))
            {
                return ServiceResult<Guest>.Failure("CCCD/CMND đã tồn tại.");
            }

            var guest = new Guest
            {
                FullName = fullName.Trim(),
                PhoneNumber = phone.Trim(),
                IdentityNumber = id,
                Email = string.IsNullOrWhiteSpace(email) ? null : email.Trim(),
            };

            try
            {
                await _repo.AddAsync(guest);
            }
            catch (Microsoft.EntityFrameworkCore.DbUpdateException)
            {
                if (await _repo.IdentityExistsAsync(id))
                {
                    return ServiceResult<Guest>.Failure("CCCD/CMND đã tồn tại.");
                }
                throw;
            }
            return ServiceResult<Guest>.Success(guest, "Đã thêm khách hàng.");
        }

        public async Task<ServiceResult<Guest>> UpdateAsync(int id, string fullName, string phone,
            string? identityNumber, string? email, GuestTag tag, string? tagNote)
        {
            var error = Validate(fullName, phone, identityNumber, requireIdentity: true);
            if (error != null)
            {
                return ServiceResult<Guest>.Failure(error);
            }

            var guest = await _repo.GetByIdAsync(id);
            if (guest == null)
            {
                return ServiceResult<Guest>.Failure("Không tìm thấy khách hàng.");
            }

            var identity = identityNumber!.Trim();
            if (await _repo.IdentityExistsAsync(identity, excludeId: id))
            {
                return ServiceResult<Guest>.Failure("CCCD/CMND đã tồn tại.");
            }

            guest.FullName = fullName.Trim();
            guest.PhoneNumber = phone.Trim();
            guest.IdentityNumber = identity;
            guest.Email = string.IsNullOrWhiteSpace(email) ? null : email.Trim();
            guest.Tag = tag;
            // Tag về None thì xoá luôn ghi chú (port rule K từ web)
            guest.TagNote = tag == GuestTag.None || string.IsNullOrWhiteSpace(tagNote) ? null : tagNote.Trim();
            guest.Reservations = [];

            await _repo.UpdateAsync(guest);
            return ServiceResult<Guest>.Success(guest, "Đã cập nhật khách hàng.");
        }

        private static string? Validate(string fullName, string phone, string? identityNumber, bool requireIdentity)
        {
            if (string.IsNullOrWhiteSpace(fullName))
            {
                return "Chưa nhập họ tên khách.";
            }
            if (fullName.Trim().Length > 100)
            {
                return "Họ tên tối đa 100 ký tự.";
            }
            if (string.IsNullOrWhiteSpace(phone))
            {
                return "Chưa nhập số điện thoại.";
            }
            if (!phone.Trim().All(c => char.IsDigit(c) || c is '+' or ' '))
            {
                return "Số điện thoại chỉ gồm chữ số.";
            }
            if (requireIdentity && string.IsNullOrWhiteSpace(identityNumber))
            {
                return "Chưa nhập CCCD/CMND.";
            }
            return null;
        }
    }
}
