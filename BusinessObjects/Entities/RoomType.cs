using System.Collections.Generic;
using BusinessObjects.Common;

namespace BusinessObjects.Entities
{
    public class RoomType : BaseEntity
    {
        public string TypeName { get; set; } = string.Empty;
        public int Capacity { get; set; }
        public decimal BasePrice { get; set; }
        public string? Description { get; set; }
        public bool IsActive { get; set; } = true;

        public virtual ICollection<Room> Rooms { get; set; } = new List<Room>();
    }
}
