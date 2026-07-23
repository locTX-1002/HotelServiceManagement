using BusinessObjects.Entities;
using BusinessObjects.Enums;

namespace FUHotelManagementWPF.ViewModels.Guests
{
    /// <summary>Dong hien thi khach hang trong DataGrid - bind thang, khong can converter.</summary>
    public class GuestRow
    {
        public Guest Guest { get; }

        public string EmailText => string.IsNullOrWhiteSpace(Guest.Email) ? "—" : Guest.Email!;
        public string IdentityText => string.IsNullOrWhiteSpace(Guest.IdentityNumber) ? "Chưa có" : Guest.IdentityNumber!;

        public string TagText => Guest.Tag switch
        {
            GuestTag.Vip => "VIP",
            GuestTag.Blacklisted => "Hạn chế",
            _ => "Thường",
        };

        public GuestRow(Guest guest)
        {
            Guest = guest;
        }
    }

    /// <summary>Lua chon nhan khach cho combobox trong dialog.</summary>
    public record TagOption(GuestTag Tag, string Label);
}
