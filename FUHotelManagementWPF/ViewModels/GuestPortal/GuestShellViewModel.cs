using System;
using System.Collections.Generic;
using FUHotelManagementWPF.MvvmCore;
using Services;

namespace FUHotelManagementWPF.ViewModels.GuestPortal;

/// <summary>Mot muc trong sidebar cua khach: icon + ten + ham tao ViewModel.</summary>
public record GuestMenuItem(string Icon, string Title, Func<ViewModelBase> CreateViewModel);

/// <summary>
/// Khung khu "Phong cua toi" - ban rut gon cua MainViewModel danh cho KHACH tu dang nhap.
/// Tach rieng thay vi dung chung MainViewModel vi khach khong co vai tro nhan vien:
/// gop chung se phai rai if(IsGuest) khap noi, de lo nham man quan tri.
/// </summary>
public class GuestShellViewModel : ViewModelBase
{
    /// <summary>View lang nghe de quay ve man dang nhap.</summary>
    public event Action? LoggedOut;

    public string GuestName => AppSession.CurrentGuest?.Guest?.FullName ?? string.Empty;

    public string AvatarInitial
        => string.IsNullOrWhiteSpace(GuestName) ? "?" : GuestName.Trim()[..1].ToUpper();

    /// <summary>Khach VIP duoc bao ngay tren thanh dau de biet minh dang co uu dai.</summary>
    public bool IsVip => AppSession.CurrentGuest?.Guest?.Tag == BusinessObjects.Enums.GuestTag.Vip;

    public List<GuestMenuItem> Menu { get; }

    private GuestMenuItem _selected;
    public GuestMenuItem Selected
    {
        get => _selected;
        set
        {
            if (value != null && SetProperty(ref _selected, value))
            {
                Current = value.CreateViewModel();
                OnPropertyChanged(nameof(Breadcrumb));
            }
        }
    }

    private ViewModelBase? _current;
    /// <summary>Man dang mo - ContentControl tu tra ra View qua ViewMappings.xaml.</summary>
    public ViewModelBase? Current
    {
        get => _current;
        private set => SetProperty(ref _current, value);
    }

    public string Breadcrumb => $"Phòng của tôi  /  {Selected.Title}";

    public RelayCommand LogoutCommand { get; }

    public GuestShellViewModel()
    {
        Menu =
        [
            new("", "Đặt phòng của tôi", () => new MyReservationsViewModel()),
            new("", "Dịch vụ phòng", () => new MyServicesViewModel()),
            new("", "Hoá đơn của tôi", () => new MyInvoicesViewModel()),
            new("", "Hồ sơ của tôi", () => new MyProfileViewModel()),
        ];

        _selected = Menu[0];
        _current = _selected.CreateViewModel();

        LogoutCommand = new RelayCommand(_ =>
        {
            AppSession.SignOut();
            LoggedOut?.Invoke();
        });
    }
}
