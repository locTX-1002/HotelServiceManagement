using System.Data;
using BusinessObjects.Entities;
using DataAccessObjects;
using Repositories;

namespace Services;

public sealed class GuestAccountService : IGuestAccountService
{
    private readonly IGuestAccountRepository _accounts;
    private readonly IGuestRepository _guests;

    public GuestAccountService() : this(new GuestAccountRepository(), new GuestRepository()) { }
    public GuestAccountService(IGuestAccountRepository a, IGuestRepository g) { _accounts = a; _guests = g; }

    /// <summary>
    /// Lấy danh sách tất cả tài khoản khách hàng.
    /// </summary>
    public async Task<ServiceResult<List<GuestAccount>>> GetAllAsync()
    {
        try
        {
            var list = await _accounts.GetAllAsync();
            return ServiceResult<List<GuestAccount>>.Success(list ?? new List<GuestAccount>());
        }
        catch (Exception ex)
        {
            return ServiceResult<List<GuestAccount>>.Failure($"Lỗi khi tải danh sách tài khoản khách: {ex.Message}");
        }
    }

    /// <summary>
    /// Khách vãng lai tự đăng ký tài khoản trực tuyến.
    /// </summary>
    public async Task<ServiceResult<GuestAccount>> RegisterAsync(string fullName, string phoneNumber,
        string? email, string password, string confirmPassword)
    {
        fullName = (fullName ?? string.Empty).Trim();
        phoneNumber = NormalizePhone(phoneNumber);
        email = Normalize(email);

        if (string.IsNullOrWhiteSpace(fullName)) return ServiceResult<GuestAccount>.Failure("Chưa nhập họ tên.");
        if (fullName.Length > 100) return ServiceResult<GuestAccount>.Failure("Họ tên tối đa 100 ký tự.");

        var phoneError = InputPolicy.ValidatePhone(phoneNumber, required: true);
        if (phoneError != null) return ServiceResult<GuestAccount>.Failure(phoneError);
        if (email is { Length: > 150 }) return ServiceResult<GuestAccount>.Failure("Email tối đa 150 ký tự.");
        var emailError = InputPolicy.ValidateEmail(email, required: false);
        if (emailError != null) return ServiceResult<GuestAccount>.Failure(emailError);

        var passwordError = PasswordPolicy.Validate(password);
        if (passwordError != null) return ServiceResult<GuestAccount>.Failure(passwordError);
        if (!string.Equals(password, confirmPassword, StringComparison.Ordinal))
            return ServiceResult<GuestAccount>.Failure("Mật khẩu xác nhận không khớp.");

        return await HotelDbContextFactory.ExecuteInTransactionAsync(IsolationLevel.Serializable, async () =>
        {
            var existingGuest = await _guests.GetByPhoneAsync(phoneNumber);
            if (existingGuest != null)
            {
                var existingAccount = await _accounts.GetByGuestIdAsync(existingGuest.Id);
                return existingAccount != null
                    ? ServiceResult<GuestAccount>.Failure("Số điện thoại này đã có tài khoản. Vui lòng đăng nhập.")
                    : ServiceResult<GuestAccount>.Failure("Số điện thoại này đã có hồ sơ tại khách sạn. Vui lòng liên hệ lễ tân để xác minh và cấp tài khoản.");
            }

            var guest = new Guest
            {
                FullName = fullName,
                PhoneNumber = phoneNumber,
                Email = email,
                IdentityNumber = null,
                Tag = BusinessObjects.Enums.GuestTag.None,
                TagNote = null
            };
            await _guests.AddAsync(guest);

            var account = new GuestAccount
            {
                GuestId = guest.Id,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
                CreatedAt = DateTime.Now
            };
            await _accounts.SaveAsync(account, true);
            account.Guest = guest;
            return ServiceResult<GuestAccount>.Success(account, "Đăng ký thành công. Bạn có thể đặt phòng ngay.");
        });
    }

    /// <summary>
    /// Nhân viên (Admin/Manager) tạo tài khoản Khách hàng trực tiếp từ màn Quản lý người dùng.
    /// Mặc định vai trò tự động gán là Khách hàng (Guest).
    /// </summary>
    public async Task<ServiceResult<GuestAccount>> CreateByStaffAsync(string fullName, string phoneNumber,
        string? email, string password, string confirmPassword)
    {
        if (!AuthorizationPolicy.CanManageUsers)
            return ServiceResult<GuestAccount>.Failure("Bạn không có quyền tạo tài khoản khách.");

        fullName = (fullName ?? string.Empty).Trim();
        phoneNumber = NormalizePhone(phoneNumber);
        email = Normalize(email);

        if (string.IsNullOrWhiteSpace(fullName)) return ServiceResult<GuestAccount>.Failure("Chưa nhập họ tên.");
        if (fullName.Length > 100) return ServiceResult<GuestAccount>.Failure("Họ tên tối đa 100 ký tự.");
        var phoneError = InputPolicy.ValidatePhone(phoneNumber, required: true);
        if (phoneError != null) return ServiceResult<GuestAccount>.Failure(phoneError);
        if (email is { Length: > 150 }) return ServiceResult<GuestAccount>.Failure("Email tối đa 150 ký tự.");
        var emailError = InputPolicy.ValidateEmail(email, required: false);
        if (emailError != null) return ServiceResult<GuestAccount>.Failure(emailError);
        var passwordError = PasswordPolicy.Validate(password);
        if (passwordError != null) return ServiceResult<GuestAccount>.Failure(passwordError);
        if (!string.Equals(password, confirmPassword, StringComparison.Ordinal))
            return ServiceResult<GuestAccount>.Failure("Mật khẩu xác nhận không khớp.");

        return await HotelDbContextFactory.ExecuteInTransactionAsync(IsolationLevel.Serializable, async () =>
        {
            var existingGuest = await _guests.GetByPhoneAsync(phoneNumber);
            if (existingGuest != null)
            {
                if (await _accounts.GetByGuestIdAsync(existingGuest.Id) != null)
                    return ServiceResult<GuestAccount>.Failure("Số điện thoại này đã có tài khoản.");

                var acc = new GuestAccount { GuestId = existingGuest.Id, PasswordHash = BCrypt.Net.BCrypt.HashPassword(password), CreatedAt = DateTime.Now };
                await _accounts.SaveAsync(acc, true);
                acc.Guest = existingGuest;
                return ServiceResult<GuestAccount>.Success(acc, $"Số điện thoại đã có hồ sơ — đã cấp tài khoản khách cho {existingGuest.FullName}.");
            }

            var guest = new Guest
            {
                FullName = fullName,
                PhoneNumber = phoneNumber,
                Email = email,
                IdentityNumber = null,
                Tag = BusinessObjects.Enums.GuestTag.None,
                TagNote = null
            };
            await _guests.AddAsync(guest);
            var account = new GuestAccount { GuestId = guest.Id, PasswordHash = BCrypt.Net.BCrypt.HashPassword(password), CreatedAt = DateTime.Now };
            await _accounts.SaveAsync(account, true);
            account.Guest = guest;
            return ServiceResult<GuestAccount>.Success(account, $"Đã tạo tài khoản khách cho {fullName}.");
        });
    }

    /// <summary>
    /// Cập nhật thông tin cơ bản của hồ sơ khách hàng.
    /// </summary>
    public async Task<ServiceResult> UpdateGuestInfoAsync(int guestId, string fullName, string phoneNumber, string? email)
    {
        var guest = await _guests.GetByIdAsync(guestId);
        if (guest == null) return ServiceResult.Failure("Không tìm thấy thông tin khách hàng.");

        fullName = (fullName ?? string.Empty).Trim();
        phoneNumber = NormalizePhone(phoneNumber);
        email = Normalize(email);

        if (string.IsNullOrWhiteSpace(fullName)) return ServiceResult.Failure("Chưa nhập họ tên.");
        var phoneError = InputPolicy.ValidatePhone(phoneNumber, required: true);
        if (phoneError != null) return ServiceResult.Failure(phoneError);

        guest.FullName = fullName;
        guest.PhoneNumber = phoneNumber;
        guest.Email = email;

        await _guests.UpdateAsync(guest);
        return ServiceResult.Success("Cập nhật thông tin khách hàng thành công.");
    }

    public async Task<ServiceResult<GuestAccount>> ActivateAsync(int guestId, string password)
    {
        if (!AuthorizationPolicy.CanManageGuests)
            return ServiceResult<GuestAccount>.Failure("Bạn không có quyền cấp tài khoản cho khách.");
        var error = PasswordPolicy.Validate(password);
        if (error != null) return ServiceResult<GuestAccount>.Failure(error);
        if (await _accounts.GetByGuestIdAsync(guestId) != null)
            return ServiceResult<GuestAccount>.Failure("Khách đã có tài khoản.");
        var guest = await _guests.GetByIdAsync(guestId);
        if (guest == null) return ServiceResult<GuestAccount>.Failure("Không tìm thấy khách hàng.");
        var x = new GuestAccount { GuestId = guestId, PasswordHash = BCrypt.Net.BCrypt.HashPassword(password), CreatedAt = DateTime.Now };
        await _accounts.SaveAsync(x, true);
        x.Guest = guest;
        return ServiceResult<GuestAccount>.Success(x, "Đã cấp tài khoản cho khách.");
    }

    public async Task<ServiceResult<GuestAccount>> LoginAsync(string phone, string password)
    {
        var x = await _accounts.GetByPhoneAsync(phone.Trim());
        if (x?.PasswordHash == null || !BCrypt.Net.BCrypt.Verify(password, x.PasswordHash))
            return ServiceResult<GuestAccount>.Failure("Số điện thoại hoặc mật khẩu không đúng.");
        if (!x.IsActive)
            return ServiceResult<GuestAccount>.Failure("Tài khoản đã bị khoá. Vui lòng liên hệ lễ tân.");
        x.LastLoginAt = DateTime.Now;
        var guest = x.Guest;
        x.Guest = null!;
        await _accounts.SaveAsync(x, false);
        x.Guest = guest;
        return ServiceResult<GuestAccount>.Success(x);
    }

    public async Task<ServiceResult> ChangePasswordAsync(int guestId, string current, string next)
    {
        var error = PasswordPolicy.Validate(next);
        if (error != null) return ServiceResult.Failure(error);
        var x = await _accounts.GetByGuestIdAsync(guestId);
        if (x?.PasswordHash == null || !BCrypt.Net.BCrypt.Verify(current, x.PasswordHash))
            return ServiceResult.Failure("Mật khẩu hiện tại không đúng.");
        x.PasswordHash = BCrypt.Net.BCrypt.HashPassword(next);
        x.Guest = null!;
        await _accounts.SaveAsync(x, false);
        return ServiceResult.Success("Đã đổi mật khẩu.");
    }

    public async Task<ServiceResult> SetActiveAsync(int guestId, bool active)
    {
        if (!AuthorizationPolicy.CanManageGuests && !AuthorizationPolicy.CanManageUsers)
            return ServiceResult.Failure("Bạn không có quyền khoá/mở khoá tài khoản khách.");

        var account = await _accounts.GetByGuestIdAsync(guestId);
        if (account == null) return ServiceResult.Failure("Khách hàng chưa có tài khoản.");

        account.IsActive = active;
        await _accounts.SaveAsync(account, false);

        var guestName = account.Guest?.FullName ?? $"GuestId={guestId}";
        await AuditTrail.WriteAsync(active ? "guest_account.unlock" : "guest_account.lock",
            nameof(GuestAccount), guestId, null, guestName);

        return ServiceResult.Success(active ? "Đã mở khoá tài khoản khách hàng." : "Đã khoá tài khoản khách hàng.");
    }

    public async Task<ServiceResult> ResetPasswordByAdminAsync(int guestId, string newPassword)
    {
        if (!AuthorizationPolicy.CanManageGuests && !AuthorizationPolicy.CanManageUsers)
            return ServiceResult.Failure("Bạn không có quyền đặt lại mật khẩu cho khách.");

        var error = PasswordPolicy.Validate(newPassword);
        if (error != null) return ServiceResult.Failure(error);

        var account = await _accounts.GetByGuestIdAsync(guestId);
        if (account == null) return ServiceResult.Failure("Khách hàng chưa có tài khoản.");

        account.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
        await _accounts.SaveAsync(account, false);

        var guestName = account.Guest?.FullName ?? $"GuestId={guestId}";
        await AuditTrail.WriteAsync("guest_account.reset_password",
            nameof(GuestAccount), guestId, null, guestName);

        return ServiceResult.Success("Đã đặt lại mật khẩu cho khách hàng.");
    }

    private static string NormalizePhone(string? value)
        => (value ?? string.Empty).Trim().Replace(" ", string.Empty).Replace(".", string.Empty).Replace("-", string.Empty);

    private static string? Normalize(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}