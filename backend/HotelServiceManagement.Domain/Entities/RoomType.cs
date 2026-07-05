using HotelServiceManagement.Domain.Common;

namespace HotelServiceManagement.Domain.Entities;

public class RoomType : BaseAuditableEntity
{
    public int RoomTypeId { get; set; }
    public string TypeName { get; set; } = null!;
    public int Capacity { get; set; }
    public decimal BasePrice { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<Room> Rooms { get; set; } = new List<Room>();
}
