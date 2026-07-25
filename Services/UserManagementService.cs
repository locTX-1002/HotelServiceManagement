using BusinessObjects;
using BusinessObjects.Entities;
using Repositories;
namespace Services;

public sealed class UserManagementService : IUserManagementService
{
    /// <summary>
    /// Vai tro Quan tri vien KHONG duoc cap qua giao dien. Tai khoan Admin duy nhat
    /// do cau hinh trien khai tao ra (EnsureBootstrapAdminAsync); app khong cho tao
    /// them Admin, khong cho gan vai tro Admin cho ai, va khong cho sua/khoa tai
    /// khoan Admin dang co. Neu khong chan, bat ky Admin nao cung tu nhan ban duoc
    /// quyen cao nhat va khong con cach nao thu hoi tu trong app.
    /// </summary>
    public const string AdminRoleName = RoleNames.Admin;

    private readonly IUserRepository _r; public UserManagementService() : this(new UserRepository()) { }
    public UserManagementService(IUserRepository r) => _r = r;
    private static bool IsAdmin => AppSession.RoleName == AdminRoleName;

    public async Task<ServiceResult<List<User>>> GetAllAsync() => !IsAdmin ? ServiceResult<List<User>>.Failure("Chỉ Quản trị viên được quản lý nhân viên.") : ServiceResult<List<User>>.Success(await _r.GetAllAsync());

    /// <summary>Vai tro duoc phep gan qua giao dien - lay tu DB, tru Admin.</summary>
    public async Task<ServiceResult<List<Role>>> GetAssignableRolesAsync()
    {
        if (!IsAdmin) return ServiceResult<List<Role>>.Failure("Chỉ Quản trị viên được quản lý nhân viên.");
        var roles = await _r.GetRolesAsync();
        return ServiceResult<List<Role>>.Success(roles.Where(x => x.RoleName != AdminRoleName).ToList());
    }

    public async Task<ServiceResult<User>> CreateAsync(string name, string email, string password, int roleId)
    {
        if (!IsAdmin) return ServiceResult<User>.Failure("Chỉ Quản trị viên được tạo tài khoản.");
        var error = Validate(name, email, password);
        if (error != null) return ServiceResult<User>.Failure(error);
        var role = await _r.GetRoleAsync(roleId);
        if (role == null) return ServiceResult<User>.Failure("Vai trò không tồn tại.");
        if (role.RoleName == AdminRoleName)
            return ServiceResult<User>.Failure("Không thể tạo tài khoản Quản trị viên từ ứng dụng. Tài khoản này do cấu hình triển khai tạo ra.");
        var n = email.Trim().ToLowerInvariant();
        if (await _r.EmailExistsAsync(n)) return ServiceResult<User>.Failure("Email đã tồn tại.");
        var x = new User { FullName = name.Trim(), Email = n, PasswordHash = BCrypt.Net.BCrypt.HashPassword(password), RoleId = roleId, IsActive = true };
        await _r.SaveAsync(x, true); x.Role = role;
        return ServiceResult<User>.Success(x, "Đã tạo tài khoản.");
    }

    public async Task<ServiceResult<User>> UpdateAsync(int id, string name, string email, int roleId)
    {
        if (!IsAdmin) return ServiceResult<User>.Failure("Chỉ Quản trị viên được sửa tài khoản.");
        var error = Validate(name, email, null);
        if (error != null) return ServiceResult<User>.Failure(error);
        var x = await _r.GetByIdAsync(id);
        if (x == null) return ServiceResult<User>.Failure("Không tìm thấy tài khoản.");
        if (x.Role?.RoleName == AdminRoleName)
            return ServiceResult<User>.Failure("Không thể sửa tài khoản Quản trị viên.");
        var role = await _r.GetRoleAsync(roleId);
        if (role == null) return ServiceResult<User>.Failure("Vai trò không tồn tại.");
        if (role.RoleName == AdminRoleName)
            return ServiceResult<User>.Failure("Không thể gán vai trò Quản trị viên cho tài khoản khác.");
        var n = email.Trim().ToLowerInvariant();
        if (await _r.EmailExistsAsync(n, id)) return ServiceResult<User>.Failure("Email đã tồn tại.");
        x.FullName = name.Trim(); x.Email = n; x.RoleId = roleId;
        await _r.SaveAsync(x, false); x.Role = role;
        return ServiceResult<User>.Success(x, "Đã cập nhật tài khoản.");
    }

    public async Task<ServiceResult<User>> SetActiveAsync(int id, bool active)
    {
        if (!IsAdmin) return ServiceResult<User>.Failure("Chỉ Quản trị viên được khoá tài khoản.");
        if (AppSession.CurrentUser?.Id == id && !active) return ServiceResult<User>.Failure("Không thể tự khoá tài khoản đang đăng nhập.");
        var x = await _r.GetByIdAsync(id);
        if (x == null) return ServiceResult<User>.Failure("Không tìm thấy tài khoản.");
        if (x.Role?.RoleName == AdminRoleName)
            return ServiceResult<User>.Failure("Không thể khoá tài khoản Quản trị viên.");
        x.IsActive = active;
        await _r.SaveAsync(x, false);
        return ServiceResult<User>.Success(x, active ? "Đã mở khoá tài khoản." : "Đã khoá tài khoản.");
    }

    public async Task<ServiceResult> ResetPasswordAsync(int id, string password)
    {
        if (!IsAdmin) return ServiceResult.Failure("Chỉ Quản trị viên được đặt lại mật khẩu.");
        var passwordError = PasswordPolicy.Validate(password);
        if (passwordError != null) return ServiceResult.Failure(passwordError);
        var x = await _r.GetByIdAsync(id);
        if (x == null) return ServiceResult.Failure("Không tìm thấy tài khoản.");
        if (x.Role?.RoleName == AdminRoleName)
            return ServiceResult.Failure("Không thể đặt lại mật khẩu tài khoản Quản trị viên. Đổi trong cấu hình triển khai.");
        x.PasswordHash = BCrypt.Net.BCrypt.HashPassword(password);
        await _r.SaveAsync(x, false);
        return ServiceResult.Success("Đã đặt lại mật khẩu.");
    }

    private static string? Validate(string name, string email, string? password)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Trim().Length > 100) return "Họ tên không hợp lệ.";
        // Dung InputPolicy chung ca app thay vi MailAddress: MailAddress chap nhan
        // ca "mana@nn." nen du lieu kieu do da lot vao DB truoc day.
        var emailError = InputPolicy.ValidateEmail(email, required: true);
        if (emailError != null) return emailError;
        return password == null ? null : PasswordPolicy.Validate(password);
    }
}
