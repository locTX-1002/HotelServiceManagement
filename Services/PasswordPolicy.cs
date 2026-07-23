namespace Services;

public static class PasswordPolicy
{ public static string? Validate(string password) { if (string.IsNullOrWhiteSpace(password) || password.Length < 8) return "Mat khau phai co it nhat 8 ky tu."; if (!password.Any(char.IsUpper) || !password.Any(char.IsLower) || !password.Any(char.IsDigit) || !password.Any(ch => !char.IsLetterOrDigit(ch))) return "Mat khau phai co chu hoa, chu thuong, chu so va ky tu dac biet."; return null; } }
