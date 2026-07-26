namespace BusinessObjects
{
    /// <summary>
    /// Ten ky thuat cua bon vai tro, dung chung cho ca bon tang.
    ///
    /// Truoc day bon chuoi nay duoc go tay o 65 cho trong 20 file. Go sai mot chu -
    /// "Reception" thay vi "Receptionist" - la mat quyen am tham: compiler khong bat,
    /// test khong do, chi den luc le tan bam nut moi bao khong co quyen. Khai bao const
    /// o day de sai chinh ta thanh loi bien dich.
    ///
    /// Phai la <c>const</c> chu khong phai <c>static readonly</c>: cac cho kiem quyen
    /// dung pattern matching (<c>is Admin or Manager</c>), ma pattern chi nhan hang so.
    ///
    /// Day la ten trong DB (bang Roles), KHONG phai ten hien cho nguoi dung. Ten tieng
    /// Viet ("Quan tri vien", "Le tan"...) nam o tang giao dien.
    /// </summary>
    public static class RoleNames
    {
        public const string Admin = "Admin";
        public const string Manager = "Manager";
        public const string Receptionist = "Receptionist";
        public const string ServiceStaff = "ServiceStaff";
    }
}
