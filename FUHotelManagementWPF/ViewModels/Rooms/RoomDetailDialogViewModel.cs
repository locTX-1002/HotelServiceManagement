using System.Collections.Generic;
using BusinessObjects.Entities;
using FUHotelManagementWPF.MvvmCore;
using Services;

namespace FUHotelManagementWPF.ViewModels.Rooms
{
    /// <summary>Dialog xem chi tiet phong: gallery anh chuyen qua lai + thong tin day du.</summary>
    public class RoomDetailDialogViewModel : ViewModelBase
    {
        private readonly List<string> _images;
        private int _currentIndex;

        public Room Room { get; }

        public string RoomTitle => $"Phòng {Room.RoomNumber}";
        public string StatusText => RoomService.RoomStatusText(Room.Status);
        public string FloorText => $"Tầng {Room.Floor}";
        public string TypeName => Room.RoomType?.TypeName ?? string.Empty;
        public string CapacityText => $"{Room.RoomType?.Capacity ?? 0} khách";
        public string PriceText => $"{Room.RoomType?.BasePrice ?? 0:N0} đ/đêm";
        public string ActiveText => Room.IsActive ? "Đang dùng" : "Ngừng dùng";
        public string? TypeDescription => Room.RoomType?.Description;
        public bool HasDescription => !string.IsNullOrWhiteSpace(TypeDescription);

        public string CurrentImage => _images[_currentIndex];
        public string Counter => $"{_currentIndex + 1} / {_images.Count}";

        public RelayCommand PrevCommand { get; }
        public RelayCommand NextCommand { get; }

        public RoomDetailDialogViewModel(Room room)
        {
            Room = room;
            _images = RoomImages.Gallery(TypeName);

            PrevCommand = new RelayCommand(_ => Move(-1));
            NextCommand = new RelayCommand(_ => Move(1));
        }

        private void Move(int delta)
        {
            // Xoay vong: het anh cuoi quay ve anh dau
            _currentIndex = (_currentIndex + delta + _images.Count) % _images.Count;
            OnPropertyChanged(nameof(CurrentImage));
            OnPropertyChanged(nameof(Counter));
        }
    }
}
