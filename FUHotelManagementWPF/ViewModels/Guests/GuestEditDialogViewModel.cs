using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using BusinessObjects.Entities;
using BusinessObjects.Enums;
using FUHotelManagementWPF.MvvmCore;
using Services;

namespace FUHotelManagementWPF.ViewModels.Guests
{
    /// <summary>
    /// Dialog them/sua ho so khach - theo mau RoomEditDialogViewModel:
    /// ValidatableViewModelBase (loi theo o) + banner loi nghiep vu + AsyncRelayCommand luu.
    /// </summary>
    public class GuestEditDialogViewModel : ValidatableViewModelBase
    {
        private readonly IGuestService _guestService = new GuestService();
        private readonly Guest? _existing;

        public event Action<bool>? RequestClose;

        public string Title => _existing == null ? "Thêm khách hàng" : $"Sửa hồ sơ: {_existing.FullName}";

        private string _fullName = string.Empty;
        public string FullName
        {
            get => _fullName;
            set => SetProperty(ref _fullName, value);
        }

        private string _phoneNumber = string.Empty;
        public string PhoneNumber
        {
            get => _phoneNumber;
            set => SetProperty(ref _phoneNumber, value);
        }

        private string _email = string.Empty;
        public string Email
        {
            get => _email;
            set => SetProperty(ref _email, value);
        }

        private string _identityNumber = string.Empty;
        public string IdentityNumber
        {
            get => _identityNumber;
            set => SetProperty(ref _identityNumber, value);
        }

        public List<TagOption> TagOptions { get; } =
        [
            new(GuestTag.None, "Thường"),
            new(GuestTag.Vip, "VIP"),
            new(GuestTag.Blacklisted, "Hạn chế (blacklist)"),
        ];

        private TagOption _selectedTag;
        public TagOption SelectedTag
        {
            get => _selectedTag;
            set
            {
                if (SetProperty(ref _selectedTag, value))
                {
                    OnPropertyChanged(nameof(ShowTagNote));
                }
            }
        }

        /// <summary>Ghi chu nhan chi co nghia khi khach duoc gan VIP/Han che.</summary>
        public bool ShowTagNote => SelectedTag.Tag != GuestTag.None;

        private string _tagNote = string.Empty;
        public string TagNote
        {
            get => _tagNote;
            set => SetProperty(ref _tagNote, value);
        }

        private string? _errorMessage;
        public string? ErrorMessage
        {
            get => _errorMessage;
            set => SetProperty(ref _errorMessage, value);
        }

        private bool _isBusy;
        public bool IsBusy
        {
            get => _isBusy;
            set => SetProperty(ref _isBusy, value);
        }

        public AsyncRelayCommand SaveCommand { get; }

        public GuestEditDialogViewModel(Guest? existing)
        {
            _existing = existing;
            _selectedTag = TagOptions[0];

            if (existing != null)
            {
                _fullName = existing.FullName;
                _phoneNumber = existing.PhoneNumber;
                _email = existing.Email ?? string.Empty;
                _identityNumber = existing.IdentityNumber ?? string.Empty;
                _selectedTag = TagOptions.FirstOrDefault(o => o.Tag == existing.Tag) ?? TagOptions[0];
                _tagNote = existing.TagNote ?? string.Empty;
            }

            SaveCommand = new AsyncRelayCommand(SaveAsync, _ => !IsBusy);
        }

        private async Task SaveAsync(object? _)
        {
            ClearAllErrors();
            ErrorMessage = null;

            if (string.IsNullOrWhiteSpace(FullName))
            {
                AddError(nameof(FullName), "Chưa nhập họ tên.");
            }

            // SDT la khoa nhan dien khach - bat buoc va phai dung dang so VN (9-11 chu so)
            var phone = PhoneNumber.Trim();
            if (string.IsNullOrWhiteSpace(phone))
            {
                AddError(nameof(PhoneNumber), "Chưa nhập số điện thoại.");
            }
            else if (!Regex.IsMatch(phone, @"^0\d{8,10}$"))
            {
                AddError(nameof(PhoneNumber), "Số điện thoại phải bắt đầu bằng 0 và có 9-11 chữ số.");
            }

            var email = Email.Trim();
            if (email.Length > 0 && !Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
            {
                AddError(nameof(Email), "Email không hợp lệ.");
            }

            var identity = IdentityNumber.Trim();
            if (identity.Length > 0 && !Regex.IsMatch(identity, @"^\d{9}(\d{3})?$"))
            {
                AddError(nameof(IdentityNumber), "CCCD/CMND phải gồm 9 hoặc 12 chữ số.");
            }

            if (HasErrors)
            {
                return;
            }

            IsBusy = true;
            try
            {
                var tagNote = ShowTagNote && !string.IsNullOrWhiteSpace(TagNote) ? TagNote.Trim() : null;
                var result = _existing == null
                    ? await _guestService.CreateAsync(
                        FullName.Trim(), email.Length > 0 ? email : null, phone,
                        identity.Length > 0 ? identity : null, SelectedTag.Tag, tagNote)
                    : await _guestService.UpdateAsync(
                        _existing.Id, FullName.Trim(), email.Length > 0 ? email : null, phone,
                        identity.Length > 0 ? identity : null, SelectedTag.Tag, tagNote);

                if (result.Ok)
                {
                    Notify.Success(result.Message);
                    RequestClose?.Invoke(true);
                }
                else
                {
                    ErrorMessage = result.Message;
                }
            }
            catch (Exception)
            {
                ErrorMessage = "Không lưu được. Kiểm tra kết nối SQL Server rồi thử lại.";
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
}
