using BusinessObjects.Common;

namespace BusinessObjects.Entities;

public class SurchargeItem : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public string Unit { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public virtual ICollection<Surcharge> Surcharges { get; set; } = new List<Surcharge>();
}
