using System.Text.RegularExpressions;

namespace Services;

/// <summary>
/// Kiem dinh dang cho cac o nhap dung chung ca app (email, so dien thoai, CCCD).
///
/// Dat o tang Services de CA ViewModel LAN service deu goi duoc mot cho: ViewModel
/// kiem truoc cho nguoi dung thay loi ngay, service kiem lai de du lieu xau khong
/// vao duoc database du goi tu dau. Truoc day moi form tu kiem "co rong khong" nen
/// email kieu "mana@nn." van luu duoc.
/// </summary>
public static class InputPolicy
{
    // Phai co it nhat mot ky tu sau dau cham cuoi cung -> chan "mana@nn."
    private static readonly Regex EmailPattern = new(
        @"^[^@\s]+@[^@\s.]+(\.[^@\s.]+)+$", RegexOptions.Compiled);

    // So Viet Nam: 10 chu so, bat dau bang 0.
    private static readonly Regex PhonePattern = new(@"^0\d{9}$", RegexOptions.Compiled);

    // CCCD 12 so hoac CMND 9 so.
    private static readonly Regex IdentityPattern = new(@"^(\d{9}|\d{12})$", RegexOptions.Compiled);

    /// <summary>Tra ve null neu hop le, nguoc lai tra ve cau loi tieng Viet.</summary>
    public static string? ValidateEmail(string? email, bool required)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return required ? "Chưa nhập email." : null;
        }
        return EmailPattern.IsMatch(email.Trim())
            ? null
            : "Email không đúng định dạng (ví dụ: ten@hotel.com).";
    }

    public static string? ValidatePhone(string? phone, bool required)
    {
        if (string.IsNullOrWhiteSpace(phone))
        {
            return required ? "Chưa nhập số điện thoại." : null;
        }
        var digits = phone.Trim().Replace(" ", string.Empty).Replace(".", string.Empty).Replace("-", string.Empty);
        return PhonePattern.IsMatch(digits)
            ? null
            : "Số điện thoại phải gồm 10 chữ số và bắt đầu bằng 0.";
    }

    public static string? ValidateIdentity(string? identity, bool required)
    {
        if (string.IsNullOrWhiteSpace(identity))
        {
            return required ? "Chưa nhập CCCD/CMND." : null;
        }
        return IdentityPattern.IsMatch(identity.Trim())
            ? null
            : "CCCD phải 12 số hoặc CMND phải 9 số.";
    }

    /// <summary>Yeu cau mat khau, viet ra de hien SAN tren form thay vi doi bam Luu moi bao.</summary>
    public const string PasswordHint =
        "Tối thiểu 8 ký tự, có chữ hoa, chữ thường, chữ số và ký tự đặc biệt.";
}
