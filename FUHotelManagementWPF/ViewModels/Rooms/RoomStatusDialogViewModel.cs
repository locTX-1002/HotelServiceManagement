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
        public string NoOptionMessage => _room.Status switch
        {
            RoomStatus.Reserved or RoomStatus.Occupied =>
                "Trạng thái này do luồng đặt phòng và check-in tự điều khiển — không đổi tay được.",
            RoomStatus.Maintenance =>
                "Chỉ Quản trị viên hoặc Quản lý được đưa phòng ra khỏi bảo trì.",
            _ => "Không có trạng thái nào chuyển được từ hiện trạng.",
        };

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
