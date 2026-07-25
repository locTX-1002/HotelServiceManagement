namespace Services;

public static class PasswordPolicy
{ public static string? Validate(string password) { if (string.IsNullOrWhiteSpace(password) || password.Length < 8) return "Mật khẩu phải có ít nhất 8 ký tự."; if (!password.Any(char.IsUpper) || !password.Any(char.IsLower) || !password.Any(char.IsDigit) || !password.Any(ch => !char.IsLetterOrDigit(ch))) return "Mật khẩu phải có chữ hoa, chữ thường, chữ số và ký tự đặc biệt."; return null; } }
