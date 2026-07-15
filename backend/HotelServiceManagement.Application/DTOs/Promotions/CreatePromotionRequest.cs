namespace HotelServiceManagement.Application.DTOs.Promotions;

public class CreatePromotionRequest
{
    public string Code { get; set; } = string.Empty;
    public string? Description { get; set; }

    // Chuỗi (Percentage/FixedAmount), không phải enum số - parse phía server, giống PaymentRequest.PaymentMethod.
    public string Type { get; set; } = string.Empty;
    public decimal Value { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public bool IsActive { get; set; } = true;
}
