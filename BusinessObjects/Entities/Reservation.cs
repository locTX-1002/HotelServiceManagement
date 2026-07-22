using System;
using BusinessObjects.Common;
using BusinessObjects.Enums;

namespace BusinessObjects.Entities
{
    public class Reservation : BaseEntity
    {
        public string BookingCode { get; set; } = string.Empty;
        public int GuestId { get; set; }
        public virtual Guest Guest { get; set; } = null!;
        public int RoomId { get; set; }
        public virtual Room Room { get; set; } = null!;

        // Default 1 only protects existing/new database rows.
        // Request DTOs intentionally do not default this value so FE must send it explicitly.
        public int NumberOfGuests { get; set; } = 1;

        public DateTime CheckInDate { get; set; }
        public DateTime CheckOutDate { get; set; }
        public ReservationStatus Status { get; set; } = ReservationStatus.Pending;
        public string? SpecialRequests { get; set; }

        // Tiền cọc thu lúc đặt phòng (tuỳ chọn) - bất biến sau khi tạo, không có trong UpdateReservationRequest.
        public decimal? DepositAmount { get; set; }
        public PaymentMethod? DepositPaymentMethod { get; set; }
        public DateTime? DepositPaidAt { get; set; }

        public int? CreatedByUserId { get; set; }
        public virtual User? CreatedByUser { get; set; }

        // Navigation property for 0..1 relationship
        public virtual Stay? Stay { get; set; }

        // Concurrency token - chặn 2 request đồng thời (VD: check-in và hủy cùng lúc) ghi đè lên nhau
        // mà không hay biết. EF Core tự tăng giá trị này mỗi lần UPDATE, SaveChangesAsync ném
        // DbUpdateConcurrencyException nếu giá trị đã đổi kể từ lúc đọc.
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }
}