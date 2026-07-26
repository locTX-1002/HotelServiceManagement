using System;

namespace BusinessObjects
{
    /// <summary>
    /// Quy tac tinh tien dung chung cho ca tang nghiep vu lan tang giao dien.
    ///
    /// Cach tinh so dem truoc day duoc viet lai o BA cho: InvoiceService (de lap hoa don),
    /// CheckInOutViewModel (de bao truoc cho le tan), InvoicesViewModel (de tam tinh). Ba
    /// ban sao thi truoc sau gi cung lech nhau, va da lech that: man Check-in/out bao 4 dem
    /// trong khi hoa don in ra 1 dem. Sua mot cho quen hai cho kia lai lech kieu khac.
    ///
    /// De o day de ca ba goi chung mot ham.
    /// </summary>
    public static class BillingRules
    {
        /// <summary>
        /// So dem THU TIEN: dem tu luc khach nhan phong that den luc roi di that, it nhat
        /// mot dem.
        ///
        /// Truoc day quy tac la "thu toi thieu so dem da dat, di som khong hoan tien" - phong
        /// bi giu cho thi khach chiu. Nghe xuoi tren giay, nhung ra con so that thi khong cai
        /// duoc voi khach: dat 4 dem roi o mot dem van phai tra 4.800.000 d. Gio khach o bao
        /// nhieu dem tra bay nhieu.
        ///
        /// O QUA HAN tu dong dung vi ngay roi di that xa hon ngay tra tren don.
        /// </summary>
        /// <param name="actualCheckIn">Luc khach nhan phong that.</param>
        /// <param name="leavingAt">Luc roi di that; con dang o thi la hom nay.</param>
        public static int ChargeableNights(DateTime actualCheckIn, DateTime leavingAt)
            => Math.Max(1, (leavingAt.Date - actualCheckIn.Date).Days);

        /// <summary>
        /// So dem DU KIEN cho ca ky - dung de bao truoc cho le tan va khach, KHONG phai so
        /// len hoa don. Gia dinh khach o het don: lay moc xa hon giua ngay tra tren don va
        /// hom nay (o qua han thi du kien phai tang theo).
        /// </summary>
        public static int EstimatedNights(DateTime actualCheckIn, DateTime plannedCheckOut, DateTime today)
        {
            var until = today.Date > plannedCheckOut.Date ? today.Date : plannedCheckOut.Date;
            return Math.Max(1, (until - actualCheckIn.Date).Days);
        }

        /// <summary>So dem o qua so voi don - hien rieng de le tan giai thich duoc voi khach.</summary>
        public static int OverdueNights(DateTime plannedCheckOut, DateTime leavingAt)
            => Math.Max(0, (leavingAt.Date - plannedCheckOut.Date).Days);
    }
}
