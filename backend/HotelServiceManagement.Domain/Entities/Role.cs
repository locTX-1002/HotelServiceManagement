using System.Collections.Generic;
using HotelServiceManagement.Domain.Common;

namespace HotelServiceManagement.Domain.Entities
{
    public class Role : BaseEntity
    {
        public string RoleName { get; set; } = string.Empty;

        // Navigation property
        public virtual ICollection<User> Users { get; set; } = new List<User>();
    }
}
