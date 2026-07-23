using BusinessObjects.Entities;
using Repositories;
namespace Services;

public sealed class GuestAccountService : IGuestAccountService
{
    private readonly IGuestAccountRepository _accounts; private readonly IGuestRepository _guests; public GuestAccountService() : this(new GuestAccountRepository(), new GuestRepository()) { }
    public GuestAccountService(IGuestAccountRepository a, IGuestRepository g) { _accounts = a; _guests = g; }
    public async Task<ServiceResult<GuestAccount>> ActivateAsync(int guestId, string password) { if (!AuthorizationPolicy.CanOperateFrontDesk) return ServiceResult<GuestAccount>.Failure("Ban khong co quyen kich hoat tai khoan khach."); var error = PasswordPolicy.Validate(password); if (error != null) return ServiceResult<GuestAccount>.Failure(error); if (await _accounts.GetByGuestIdAsync(guestId) != null) return ServiceResult<GuestAccount>.Failure("Khach da co tai khoan."); var guest = await _guests.GetByIdAsync(guestId); if (guest == null) return ServiceResult<GuestAccount>.Failure("Khong tim thay khach hang."); var x = new GuestAccount { GuestId = guestId, PasswordHash = BCrypt.Net.BCrypt.HashPassword(password), CreatedAt = DateTime.Now }; await _accounts.SaveAsync(x, true); x.Guest = guest; return ServiceResult<GuestAccount>.Success(x, "Da kich hoat tai khoan khach."); }
    public async Task<ServiceResult<GuestAccount>> LoginAsync(string phone, string password) { var x = await _accounts.GetByPhoneAsync(phone.Trim()); if (x?.PasswordHash == null || !BCrypt.Net.BCrypt.Verify(password, x.PasswordHash)) return ServiceResult<GuestAccount>.Failure("So dien thoai hoac mat khau khong dung."); x.LastLoginAt = DateTime.Now; var guest = x.Guest; x.Guest = null!; await _accounts.SaveAsync(x, false); x.Guest = guest; return ServiceResult<GuestAccount>.Success(x); }
    public async Task<ServiceResult> ChangePasswordAsync(int guestId, string current, string next) { var error = PasswordPolicy.Validate(next); if (error != null) return ServiceResult.Failure(error); var x = await _accounts.GetByGuestIdAsync(guestId); if (x?.PasswordHash == null || !BCrypt.Net.BCrypt.Verify(current, x.PasswordHash)) return ServiceResult.Failure("Mat khau hien tai khong dung."); x.PasswordHash = BCrypt.Net.BCrypt.HashPassword(next); x.Guest = null!; await _accounts.SaveAsync(x, false); return ServiceResult.Success("Da doi mat khau."); }
}
