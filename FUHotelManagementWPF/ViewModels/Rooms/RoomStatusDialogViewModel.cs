using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BusinessObjects.Entities;
using BusinessObjects.Enums;
using FUHotelManagementWPF.MvvmCore;
using Services;

namespace FUHotelManagementWPF.ViewModels.Rooms
{
    /// <summary>
    /// Dialog doi trang thai van hanh cua phong theo dung transition rules:
    /// Trong <-> Dang don, Trong <-> Bao tri (chi Admin/Manager).
    /// Da dat / Dang o do luong dat phong va check-in tu dieu khien.
    /// </summary>
    public class RoomStatusDialogViewModel : ViewModelBase
    {
        private readonly IRoomService _roomService = new RoomService();
        private readonly Room _room;
        private readonly bool _canManageMaintenance;

        public event Action<bool>? RequestClose;

        public string RoomTitle => $"Phòng {_room.RoomNumber}";
        public string CurrentStatusText => RoomService.RoomStatusText(_room.Status);

        public List<StatusOption> Options { get; }
        public bool HasOptions => Options.Count > 0;

        // Khong chi noi "khong doi duoc" ma noi luon phai lam gi va o dau,
        // vi hai truong hop nay cach xu ly khac han nhau.
        public string NoOptionMessage => _room.Status switch
        {
            RoomStatus.Occupied =>
                "Phòng đang có khách ở. Muốn đổi trạng thái thì cho khách trả phòng trước, "
                + "phòng sẽ tự chuyển sang Đang dọn.",
            RoomStatus.Reserved =>
                "Phòng đang được giữ cho một đặt phòng. Muốn giải phóng thì huỷ đơn hoặc "
                + "đánh dấu khách không đến, phòng sẽ tự về Trống.",
            RoomStatus.Maintenance =>
                "Phòng đang bảo trì. Chỉ Quản trị viên hoặc Quản lý mới đưa phòng ra khỏi bảo trì được — "
                + "nhờ họ mở lại giúp.",
            _ => "Từ trạng thái hiện tại không chuyển sang trạng thái nào khác được.",
        };

        /// <summary>Man can sang de xu ly, tuy trang thai. Rong nghia la khong co loi tat.</summary>
        private string? NavigateTarget => _room.Status switch
        {
            RoomStatus.Occupied => "Check-in / Check-out",
            RoomStatus.Reserved => "Đặt phòng",
            _ => null,
        };

        public bool HasNavigate => NavigateTarget != null;
        public string NavigateLabel => _room.Status switch
        {
            RoomStatus.Occupied => "Sang màn Check-in/out",
            RoomStatus.Reserved => "Sang màn Đặt phòng",
            _ => string.Empty,
        };

        public RelayCommand NavigateCommand { get; }

        private StatusOption? _selectedOption;
        public StatusOption? SelectedOption
        {
            get => _selectedOption;
            set => SetProperty(ref _selectedOption, value);
        }

        private string? _errorMessage;
        public string? ErrorMessage
        {
            get => _errorMessage;
            set => SetProperty(ref _errorMessage, value);
        }

        public AsyncRelayCommand ConfirmCommand { get; }

        public RoomStatusDialogViewModel(Room room)
        {
            _room = room;
            _canManageMaintenance = AppSession.RoleName is "Admin" or "Manager";

            Options = _room.Status switch
            {
                RoomStatus.Available when _canManageMaintenance =>
                    [new(RoomStatus.Cleaning, "Chuyển sang Đang dọn"), new(RoomStatus.Maintenance, "Đưa vào Bảo trì")],
                RoomStatus.Available =>
                    [new(RoomStatus.Cleaning, "Chuyển sang Đang dọn")],
                RoomStatus.Cleaning =>
                    [new(RoomStatus.Available, "Dọn xong — trả phòng về Trống")],
                RoomStatus.Maintenance when _canManageMaintenance =>
                    [new(RoomStatus.Available, "Bảo trì xong — trả phòng về Trống")],
                _ => [],
            };
            _selectedOption = Options.Count > 0 ? Options[0] : null;

            NavigateCommand = new RelayCommand(_ =>
            {
                if (NavigateTarget is { } target)
                {
                    RequestClose?.Invoke(false);
                    NavigationService.NavigateTo(target);
                }
            });

            ConfirmCommand = new AsyncRelayCommand(ConfirmAsync, _ => HasOptions);
        }

        private async Task ConfirmAsync(object? _)
        {
            if (SelectedOption == null)
            {
                ErrorMessage = "Chưa chọn trạng thái muốn chuyển.";
                return;
            }

            try
            {
                var result = await _roomService.UpdateStatusAsync(
                    _room.Id, SelectedOption.Status, _canManageMaintenance);

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
                ErrorMessage = "Không đổi được trạng thái. Kiểm tra kết nối SQL Server.";
            }
        }
    }
}
