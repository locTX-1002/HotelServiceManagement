using System.Collections.Generic;
using BusinessObjects.Common;
using BusinessObjects.Enums;

namespace BusinessObjects.Entities
{
    public class Room : BaseEntity
    {
        public string RoomNumber { get; set; } = string.Empty;
        public int Floor { get; set; }
        public int RoomTypeId { get; set; }
        public RoomStatus Status { get; set; } = RoomStatus.Available;
        public bool IsActive { get; set; } = true;

        public virtual RoomType RoomType { get; set; } = null!;
        public virtual ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();
    }
}
