using System.ComponentModel.DataAnnotations;

namespace HotelServiceManagement.Application.DTOs.GuestAuth
{
    // Tu dang ky tu do bang SDT - khong con can chung minh so huu 1 dat phong cu the (xac minh danh
    // tinh that dien ra o quay le tan luc check-in, cong khach chi la lop tien ich).
    public class GuestRegisterRequest
    {
        public string FullName { get; set; } = string.Empty;
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}
