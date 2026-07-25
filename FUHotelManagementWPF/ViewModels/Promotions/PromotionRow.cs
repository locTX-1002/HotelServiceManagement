using System;
using BusinessObjects.Entities;
using BusinessObjects.Enums;

namespace FUHotelManagementWPF.ViewModels.Promotions
{
    /// <summary>
    /// Trang thai hien thi cua mot khuyen mai. Khong luu trong DB - tinh ra tu
    /// IsActive + khoang ngay, vi cung mot ban ghi hom nay "dang chay" nhung
    /// sang thang lai thanh "het han".
    /// </summary>
    public enum PromotionStatus
    {
        Running,
        Upcoming,
        Expired,
        Off,
    }

    /// <summary>Dong khuyen mai cho card-row + panel chi tiet.</summary>
    public class PromotionRow
    {
        public Promotion Promotion { get; }

        public PromotionRow(Promotion promotion) => Promotion = promotion;

        public bool IsPercentage => Promotion.Type == PromotionType.Percentage;

        // O vuong ben trai card-row: nhin phat biet ngay giam theo % hay theo so tien.
        public string TypeSymbol => IsPercentage ? "%" : "đ";

        public string TypeText => IsPercentage ? "Giảm theo phần trăm" : "Giảm số tiền cố định";

        // 0.## de bo phan thap phan thua (10 chu khong phai 10,00);
        // N0 de tien co dau cham nghin cho de doc (200.000 đ).
        public string ValueText => IsPercentage
            ? $"Giảm {Promotion.Value:0.##}%"
            : $"Giảm {Promotion.Value:N0} đ";

        public string StartText => Promotion.StartDate.ToString("dd/MM/yyyy");

        public string EndText => Promotion.EndDate.ToString("dd/MM/yyyy");

        public string DateRangeText => $"{StartText} → {EndText}";

        public string DescriptionText => string.IsNullOrWhiteSpace(Promotion.Description)
            ? "—"
            : Promotion.Description!;

        public PromotionStatus Status
        {
            get
            {
                // Da tat thu cong thi khoi xet ngay - nguoi dung can biet "no dang tat"
                // chu khong phai "no con han".
                if (!Promotion.IsActive)
                {
                    return PromotionStatus.Off;
                }

                var today = DateTime.Today;
                if (today < Promotion.StartDate.Date)
                {
                    return PromotionStatus.Upcoming;
                }
                if (today > Promotion.EndDate.Date)
                {
                    return PromotionStatus.Expired;
                }
                return PromotionStatus.Running;
            }
        }

        public string StatusText => Status switch
        {
            // "Dang chay" nghe nhu mot tien trinh may moc. Khuyen mai thi dung tu ve
            // hieu luc: dang ap dung / chua toi ngay / het han / da tat.
            PromotionStatus.Running => "Đang áp dụng",
            PromotionStatus.Upcoming => "Chưa tới ngày",
            PromotionStatus.Expired => "Hết hạn",
            _ => "Đã tắt",
        };
    }
}
