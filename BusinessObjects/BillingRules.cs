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
        /// So dem tinh tien cua mot luot luu tru.
        ///
        /// Thu TOI THIEU so dem khach da dat: di som khong duoc tra lai tien nhung dem chua
        /// o (thong le khach san). O qua han thi tinh theo ngay roi di that. Tuc la lay moc
        /// xa hon trong hai moc, va it nhat luon la mot dem.
        /// </summary>
        /// <param name="actualCheckIn">Luc khach nhan phong that.</param>
        /// <param name="plannedCheckOut">Ngay tra phong ghi tren don dat.</param>
        /// <param name="leavingAt">Luc tinh tien: ngay tra thuc te, hoac hom nay neu con o.</param>
        public static int ChargeableNights(DateTime actualCheckIn, DateTime plannedCheckOut, DateTime leavingAt)
        {
            var until = leavingAt.Date > plannedCheckOut.Date ? leavingAt.Date : plannedCheckOut.Date;
            return Math.Max(1, (until - actualCheckIn.Date).Days);
        }

        /// <summary>So dem o qua so voi don - hien rieng de le tan giai thich duoc voi khach.</summary>
        public static int OverdueNights(DateTime plannedCheckOut, DateTime leavingAt)
            => Math.Max(0, (leavingAt.Date - plannedCheckOut.Date).Days);
    }
}
