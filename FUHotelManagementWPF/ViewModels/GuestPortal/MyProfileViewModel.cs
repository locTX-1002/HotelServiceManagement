using System;
using System.Globalization;
using System.Threading.Tasks;
using BusinessObjects.Entities;
using BusinessObjects.Enums;
using FUHotelManagementWPF.MvvmCore;
using Services;

namespace FUHotelManagementWPF.ViewModels.GuestPortal
{
    /// <summary>
    /// Man "Ho so cua toi" cua khu Phong cua toi: the thong tin (chi doc, lay tu phien
    /// dang nhap) + the doi mat khau. Khong goi service nao de lay ho so vi khach khong
    /// co vai tro nhan vien - moi ham lay danh sach khach deu kiem quyen va se chan lai;
    /// thong tin can hien da nam san trong AppSession.CurrentGuest.
    /// </summary>
    public class MyProfileViewModel : ViewModelBase
    {
        private readonly IGuestAccountService _accountService = new GuestAccountService();

        /// <summary>View lang nghe de xoa trang 3 o PasswordBox sau khi doi thanh cong.</summary>
        public event Action? PasswordAccepted;

        private readonly GuestAccount? _account = AppSession.CurrentGuest;

        private Guest? Profile => _account?.Guest;

        /// <summary>Het phien (bi dang xuat o noi khac) thi khong co gi de hien - View bao lai.</summary>
        public bool HasProfile => Profile != null;

        public string FullName => Profile?.FullName ?? string.Empty;

        public string Initial => string.IsNullOrWhiteSpace(FullName)
            ? "?" : FullName.Trim()[..1].ToUpper();

        public string PhoneText => string.IsNullOrWhiteSpace(Profile?.PhoneNumber)
            ? "Chưa có" : Profile!.PhoneNumber;

        public string EmailText => string.IsNullOrWhiteSpace(Profile?.Email)
            ? "Chưa có" : Profile!.Email!;

        public string IdentityText => string.IsNullOrWhiteSpace(Profile?.IdentityNumber)
            ? "Chưa có" : Profile!.IdentityNumber!;

        public bool IsVip => Profile?.Tag == GuestTag.Vip;

        public string VipNoteText => "Bạn được giảm 10% trên mọi hoá đơn.";

        public string CreatedAtText => _account == null
            ? "—"
            : _account.CreatedAt.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);

        /// <summary>
        /// PasswordPolicy chi tra ve cau loi khi sai chu khong co san mo ta yeu cau, nen
        /// dong huong dan nay viet tay theo dung luat trong PasswordPolicy.Validate -
        /// sua luat ben do thi sua ca dong nay.
        /// </summary>
        public string PasswordHint =>
            "Ít nhất 8 ký tự, có chữ hoa, chữ thường, chữ số và ký tự đặc biệt. Ví dụ: Hotel@2026";

        private bool _isBusy;
        public bool IsBusy
        {
            get => _isBusy;
            set => SetProperty(ref _isBusy, value);
        }

        private string? _errorMessage;
        public string? ErrorMessage
        {
            get => _errorMessage;
            set { if (SetProperty(ref _errorMessage, value)) { OnPropertyChanged(nameof(HasError)); } }
        }

        public bool HasError => !string.IsNullOrWhiteSpace(_errorMessage);

        /// <summary>
        /// Ba chuoi mat khau do code-behind doc thang tu PasswordBox truyen sang (PasswordBox
        /// khong binding duoc). Khong luu vao property nao de mat khau khong lo ra binding/log.
        /// </summary>
        public async Task ChangePasswordAsync(string currentPassword, string newPassword, string confirmPassword)
        {
            if (IsBusy)
            {
                return;
            }

            ErrorMessage = null;

            if (AppSession.CurrentGuestId == null)
            {
                ErrorMessage = "Phiên đăng nhập đã kết thúc. Hãy đăng nhập lại rồi thử lần nữa.";
                return;
            }
            if (string.IsNullOrWhiteSpace(currentPassword)
                || string.IsNullOrWhiteSpace(newPassword)
                || string.IsNullOrWhiteSpace(confirmPassword))
            {
                ErrorMessage = "Hãy nhập đủ cả ba ô mật khẩu.";
                return;
            }
            if (newPassword != confirmPassword)
            {
                ErrorMessage = "Mật khẩu mới và ô xác nhận chưa khớp nhau.";
                return;
            }

            IsBusy = true;
            try
            {
                // Service tu kiem tra mat khau hien tai co dung khong va mat khau moi
                // du manh chua, tra ve cau tieng Viet - hien nguyen van len banner.
                var result = await _accountService.ChangePasswordAsync(
                    AppSession.CurrentGuestId.Value, currentPassword, newPassword);

                if (result.Ok)
                {
                    Notify.Success(result.Message);
                    PasswordAccepted?.Invoke();
                }
                else
                {
                    ErrorMessage = result.Message;
                }
            }
            catch (Exception)
            {
                ErrorMessage = "Không đổi được mật khẩu. Kiểm tra kết nối SQL Server rồi thử lại.";
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
}
