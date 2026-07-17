using System.ComponentModel.DataAnnotations;

namespace HotelServiceManagement.Application.DTOs.Surcharges;

public class SurchargeItemRequest
{
    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required, MaxLength(20)]
    public string Unit { get; set; } = string.Empty;

    [Range(typeof(decimal), "0.01", "9999999999999999")]
    public decimal UnitPrice { get; set; }

    public bool IsActive { get; set; } = true;
}
