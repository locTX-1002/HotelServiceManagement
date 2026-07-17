using System.ComponentModel.DataAnnotations;

namespace HotelServiceManagement.Application.DTOs.Stays;

public class CheckOutRequest
{
    public List<CheckOutSurchargeRequest> Surcharges { get; set; } = new();
}

public class CheckOutSurchargeRequest
{
    [Range(1, int.MaxValue)]
    public int SurchargeItemId { get; set; }

    [Range(1, int.MaxValue)]
    public int Quantity { get; set; }
}
