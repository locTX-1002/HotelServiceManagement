using System.ComponentModel.DataAnnotations;

namespace HotelServiceManagement.Application.DTOs.Housekeeping
{
    public class CreateHousekeepingRequestRequest
    {
        // Dang chuoi (giong pattern DepositPaymentMethod) - server tu parse qua Enum.TryParse,
        // rong/khong hop le thi mac dinh Other, khong bao gio 400 vi thieu truong nay.
        public string? RequestType { get; set; }

        [MaxLength(300)]
        public string? Note { get; set; }
    }
}
