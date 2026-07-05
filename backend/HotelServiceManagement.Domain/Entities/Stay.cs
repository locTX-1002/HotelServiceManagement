using HotelServiceManagement.Domain.Common;
using HotelServiceManagement.Domain.Enums;

namespace HotelServiceManagement.Domain.Entities;

public class Stay : BaseAuditableEntity
{
    public int StayId { get; set; }
    public int ReservationId { get; set; }
    public DateTime ActualCheckIn { get; set; }
    public DateTime? ActualCheckOut { get; set; }
    public StayStatus Status { get; set; } = StayStatus.Active;

    public Reservation Reservation { get; set; } = null!;
    public ICollection<ServiceOrder> ServiceOrders { get; set; } = new List<ServiceOrder>();
    public Invoice? Invoice { get; set; }
}
