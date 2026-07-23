using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BusinessObjects.Entities;
using BusinessObjects.Enums;
using FUHotelManagementWPF.MvvmCore;
using Services;

namespace FUHotelManagementWPF.ViewModels.Guests
{
    public record GuestTagOption(GuestTag Tag, string Label);

    /// <summary>Dialog thêm/sửa khách hàng - theo mẫu chuẩn: header màu + validate theo ô.</summary>
    public class GuestEditDialogViewModel : ValidatableViewModelBase
    {
        private readonly IGuestService _service = new GuestService();
        private readonly Guest? _existing;

        public event Action<bool>? RequestClose;

        public bool IsEdit => _existing != null;
        public string Title => IsEdit ? $"Sửa khách {_existing!.FullName}" : "Thêm khách hàng mới";
        public string Subtitle => IsEdit
            ? "Cập nhật thông tin liên hệ và phân loại khách."
            : "Nhập thông tin khách để dùng cho đặt phòng.";

        private string _fullName = string.Empty;
        public string FullName
        {
            get => _fullName;
            set => SetProperty(ref _fullName, value);
        }

        private string _phone = string.Empty;
        public string Phone
        {
            get => _phone;
            set => SetProperty(ref _phone, value);
        }

        private string _identityNumber = string.Empty;
        public string IdentityNumber
        {
            get => _identityNumber;
            set => SetProperty(ref _identityNumber, value);
        }

        private string _email = string.Empty;
        public string Email
        {
            get => _email;
            set => SetProperty(ref _email, value);
        }

        public List<GuestTagOption> TagOptions { get; } =
        [
            new(GuestTag.None, "Bình thường"),
            new(GuestTag.Vip, "VIP"),
            new(GuestTag.Blacklisted, "Cảnh báo (blacklist)"),
        ];

        private GuestTagOption _selectedTag;
        public GuestTagOption SelectedTag
        {
            get => _selectedTag;
            set
            {
                if (SetProperty(ref _selectedTag, value))
                {
                    OnPropertyChanged(nameof(ShowTagNote));
                    OnPropertyChanged(nameof(TagNotePlaceholder));
                }
            }
        }

        public bool ShowTagNote => _selectedTag.Tag != GuestTag.None;
        public string TagNotePlaceholder => _selectedTag.Tag == GuestTag.Blacklisted
            ? "Lý do cảnh báo (VD: từng huỷ phòng nhiều lần, gây mất trật tự…)"
            : "Ghi chú ưu đãi cho khách VIP";

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
                _phone = existing.PhoneNumber;
                _identityNumber = existing.IdentityNumber ?? string.Empty;
                _email = existing.Email ?? string.Empty;
                _selectedTag = TagOptions.First(o => o.Tag == existing.Tag);
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
            if (string.IsNullOrWhiteSpace(Phone))
            {
                AddError(nameof(Phone), "Chưa nhập số điện thoại.");
            }
            if (string.IsNullOrWhiteSpace(IdentityNumber))
            {
                AddError(nameof(IdentityNumber), "Chưa nhập CCCD/CMND.");
            }
            if (HasErrors)
            {
                return;
            }

            IsBusy = true;
            try
            {
                var result = IsEdit
                    ? await _service.UpdateAsync(_existing!.Id, FullName, Phone, IdentityNumber, Email,
                        SelectedTag.Tag, TagNote)
                    : await _service.CreateAsync(FullName, Phone, IdentityNumber, Email);

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
