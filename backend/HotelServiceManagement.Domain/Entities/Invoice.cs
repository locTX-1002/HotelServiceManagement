using HotelServiceManagement.Domain.Common;
using HotelServiceManagement.Domain.Enums;

namespace HotelServiceManagement.Domain.Entities;

public class Invoice : BaseAuditableEntity
{
    public int InvoiceId { get; set; }
    public int StayId { get; set; }
    public decimal RoomCharge { get; set; }
    public decimal ServiceCharge { get; set; }
    public decimal TotalAmount { get; set; }
    public InvoiceStatus Status { get; set; } = InvoiceStatus.Unpaid;

    public Stay Stay { get; set; } = null!;
    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
}
