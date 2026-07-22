using BusinessObjects.Common;
using BusinessObjects.Enums;

namespace BusinessObjects.Entities
{
    // Yeu cau don phong khach tu goi tu guest portal - gan voi Stay (chi hop le khi dang Active),
    // khong tinh phi nen KHONG dung lai ServiceOrder (von danh cho dich vu co gia, cong vao hoa don).
    public class HousekeepingRequest : BaseEntity
    {
        public int StayId { get; set; }
        public virtual Stay Stay { get; set; } = null!;
        public HousekeepingRequestType RequestType { get; set; } = HousekeepingRequestType.Other;
        public string? Note { get; set; }
        public HousekeepingRequestStatus Status { get; set; } = HousekeepingRequestStatus.Pending;
        public DateTime RequestedAt { get; set; }
        public DateTime? HandledAt { get; set; }
        public int? HandledByUserId { get; set; }
        public virtual User? HandledByUser { get; set; }
    }
}
