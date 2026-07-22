using System;
using BusinessObjects.Common;
using BusinessObjects.Enums;

namespace BusinessObjects.Entities
{
    public class Promotion : BaseEntity
    {
        // Lưu chuẩn hoá UPPERCASE để so khớp lúc áp dụng không phụ thuộc collation của DB.
        public string Code { get; set; } = string.Empty;
        public string? Description { get; set; }
        public PromotionType Type { get; set; }
        public decimal Value { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public bool IsActive { get; set; } = true;
    }
}
