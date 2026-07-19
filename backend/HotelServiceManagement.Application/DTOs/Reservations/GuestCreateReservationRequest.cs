using System.ComponentModel.DataAnnotations;

namespace HotelServiceManagement.Application.DTOs.Reservations;

// Khach tu dat phong online - chon loai phong (khong chon phong cu the), he thong tu tim 1 phong
// trong loai do con trong cho khoang ngay yeu cau. GuestId/Status luon do server tu gan (Pending),
// khong nhan tu client - tranh khach tu xac nhan dat phong cua chinh minh hoac gia mao GuestId khac.
public class GuestCreateReservationRequest
{
    [Range(1, int.MaxValue, ErrorMessage = "RoomTypeId must be greater than 0.")]
    public int RoomTypeId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "NumberOfGuests must be at least 1.")]
    public int NumberOfGuests { get; set; }

    public DateTime CheckInDate { get; set; }
    public DateTime CheckOutDate { get; set; }

    [MaxLength(500)]
    public string? SpecialRequests { get; set; }
}
