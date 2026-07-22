using BusinessObjects.Common;

namespace BusinessObjects.Entities;

public class Surcharge : BaseEntity
{
    public int StayId { get; set; }
    public virtual Stay Stay { get; set; } = null!;
    public int SurchargeItemId { get; set; }
    public virtual SurchargeItem SurchargeItem { get; set; } = null!;
    public int Quantity { get; set; }
    public decimal UnitPriceSnapshot { get; set; }
    public decimal Subtotal { get; set; }
    public int? CreatedByUserId { get; set; }
    public virtual User? CreatedByUser { get; set; }
    public DateTime CreatedAt { get; set; }
}
