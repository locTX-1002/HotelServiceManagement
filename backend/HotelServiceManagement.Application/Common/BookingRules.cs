namespace HotelServiceManagement.Application.Common;

/// <summary>
/// Business rules thuần túy cho đặt phòng và tính tiền - không phụ thuộc EF Core
/// để unit test được độc lập. Service ở Application dùng lại các hàm này.
/// </summary>
public static class BookingRules
{
    /// <summary>
    /// BR03: hai khoảng ngày [aStart, aEnd) và [bStart, bEnd) chồng lấn nhau.
    /// Ngày trả phòng không tính là đêm ở, nên checkout ngày X và checkin ngày X
    /// trên cùng một phòng KHÔNG bị coi là trùng.
    /// </summary>
    public static bool IsOverlapping(DateTime aStart, DateTime aEnd, DateTime bStart, DateTime bEnd)
        => aStart < bEnd && aEnd > bStart;

    /// <summary>
    /// Số đêm ở: chênh lệch ngày, tối thiểu 1 đêm (check-in và check-out cùng ngày
    /// vẫn tính 1 đêm theo chính sách khách sạn).
    /// </summary>
    public static int CalculateNights(DateTime checkIn, DateTime checkOut)
        => Math.Max((checkOut.Date - checkIn.Date).Days, 1);

    /// <summary>BR07 (phần tiền phòng): số đêm × đơn giá.</summary>
    public static decimal CalculateRoomCharge(DateTime checkIn, DateTime checkOut, decimal basePricePerNight)
        => CalculateNights(checkIn, checkOut) * basePricePerNight;
}
