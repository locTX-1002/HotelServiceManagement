using System.ComponentModel.DataAnnotations;

namespace HotelServiceManagement.Application.DTOs.Housekeeping
{
    public class CreateHousekeepingRequestRequest
    {
        [MaxLength(300)]
        public string? Note { get; set; }
    }
}
