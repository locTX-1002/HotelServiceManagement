namespace HotelServiceManagement.Application.DTOs.GuestAuth
{
    // Khach tu kich hoat dang nhap bang cach chung minh minh la chu 1 dat phong co that (BookingCode
    // + ho ten + SDT khop dung voi Reservation le tan da tao), khong dang ky doc lap kieu mang xa hoi.
    public class GuestRegisterRequest
    {
        public string BookingCode { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}
