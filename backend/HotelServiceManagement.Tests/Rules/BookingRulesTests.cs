using HotelServiceManagement.Application.Common;

namespace HotelServiceManagement.Tests.Rules;

public class BookingRulesTests
{
    private static readonly DateTime D10 = new(2026, 7, 10);
    private static readonly DateTime D12 = new(2026, 7, 12);
    private static readonly DateTime D14 = new(2026, 7, 14);
    private static readonly DateTime D16 = new(2026, 7, 16);

    // ===== BR03: chống đặt phòng trùng khoảng ngày =====

    [Fact]
    public void Overlap_TrungHoanToan_PhaiBiChan()
        => Assert.True(BookingRules.IsOverlapping(D10, D14, D10, D14));

    [Fact]
    public void Overlap_TrungMotPhan_BookingMoiBatDauTruocKhiBookingCuKetThuc_PhaiBiChan()
        => Assert.True(BookingRules.IsOverlapping(D10, D14, D12, D16));

    [Fact]
    public void Overlap_BookingMoiNamGonBenTrong_PhaiBiChan()
        => Assert.True(BookingRules.IsOverlapping(D10, D16, D12, D14));

    [Fact]
    public void Overlap_BookingMoiBaoTrumBookingCu_PhaiBiChan()
        => Assert.True(BookingRules.IsOverlapping(D12, D14, D10, D16));

    [Fact]
    public void Overlap_CheckoutNgayX_CheckinNgayX_KhongTrung_ChoPhep()
        => Assert.False(BookingRules.IsOverlapping(D10, D12, D12, D14));

    [Fact]
    public void Overlap_HaiKhoangTachBiet_ChoPhep()
        => Assert.False(BookingRules.IsOverlapping(D10, D12, D14, D16));

    // ===== Số đêm và tiền phòng (BR07 phần room charge) =====

    [Theory]
    [InlineData("2026-07-10", "2026-07-12", 2)]
    [InlineData("2026-07-10", "2026-07-11", 1)]
    [InlineData("2026-07-10", "2026-07-10", 1)] // cùng ngày vẫn tính 1 đêm
    public void CalculateNights_TinhDungSoDem(string checkIn, string checkOut, int expected)
        => Assert.Equal(expected, BookingRules.CalculateNights(DateTime.Parse(checkIn), DateTime.Parse(checkOut)));

    [Fact]
    public void CalculateRoomCharge_2Dem_Gia500k_Ra1Trieu()
        => Assert.Equal(1_000_000m, BookingRules.CalculateRoomCharge(D10, D12, 500_000m));

    [Fact]
    public void CalculateRoomCharge_GioLeTrongNgay_VanTinhTronDem()
    {
        var checkIn = new DateTime(2026, 7, 10, 14, 0, 0);
        var checkOut = new DateTime(2026, 7, 12, 11, 30, 0);
        Assert.Equal(1_000_000m, BookingRules.CalculateRoomCharge(checkIn, checkOut, 500_000m));
    }
}
