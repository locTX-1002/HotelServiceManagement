using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using BusinessObjects.Entities;
using FUHotelManagementWPF.MvvmCore;
using Services;

namespace FUHotelManagementWPF.ViewModels.CheckInOut
{
    /// <summary>Mot dong phu thu da ghi, hien trong bang ben duoi.</summary>
    public class SurchargeLine
    {
        public Surcharge Surcharge { get; }
        public string Name => Surcharge.SurchargeItem?.Name ?? string.Empty;
        public string QuantityText => $"{Surcharge.Quantity} {Surcharge.SurchargeItem?.Unit}".Trim();
        public string UnitPriceText => $"{Surcharge.UnitPriceSnapshot:N0} đ";
        public string SubtotalText => $"{Surcharge.Subtotal:N0} đ";

        public SurchargeLine(Surcharge surcharge) => Surcharge = surcharge;
    }

    /// <summary>
    /// Kiem do trong phong truoc khi cho khach tra: ghi lai thu khach lam hong hoac mat.
    /// Don gia duoc chup lai luc ghi nen sau nay bang gia doi cung khong anh huong dong da ghi.
    /// </summary>
    public class SurchargeDialogViewModel : ViewModelBase
    {
        private readonly ISurchargeService _service = new SurchargeService();
        private readonly int _stayId;

        public string RoomText { get; }
        public string GuestText { get; }

        public ObservableCollection<SurchargeItem> Items { get; } = [];
        public ObservableCollection<SurchargeLine> Lines { get; } = [];

        private SurchargeItem? _selectedItem;
        public SurchargeItem? SelectedItem
        {
            get => _selectedItem;
            set => SetProperty(ref _selectedItem, value);
        }

        private int _quantity = 1;
        public int Quantity
        {
            get => _quantity;
            set => SetProperty(ref _quantity, value);
        }

        public decimal Total => Lines.Sum(l => l.Surcharge.Subtotal);
        public string TotalText => $"{Total:N0} đ";
        public bool IsEmpty => Lines.Count == 0;

        private string? _errorMessage;
        public string? ErrorMessage
        {
            get => _errorMessage;
            set { if (SetProperty(ref _errorMessage, value)) { OnPropertyChanged(nameof(HasError)); } }
        }

        public bool HasError => !string.IsNullOrEmpty(_errorMessage);

        public AsyncRelayCommand AddCommand { get; }
        public AsyncRelayCommand RemoveCommand { get; }

        /// <summary>Tong phu thu tra ve cho man check-in/out cap nhat lai o tien.</summary>
        public event Action? Changed;

        public SurchargeDialogViewModel(Stay stay)
        {
            _stayId = stay.Id;
            RoomText = $"Phòng {stay.Reservation.Room?.RoomNumber} · {stay.Reservation.Room?.RoomType?.TypeName}";
            GuestText = stay.Reservation.Guest?.FullName ?? string.Empty;

            AddCommand = new AsyncRelayCommand(AddAsync);
            RemoveCommand = new AsyncRelayCommand(RemoveAsync);
            _ = LoadAsync();
        }

        public async Task LoadAsync()
        {
            try
            {
                var items = await _service.GetActiveItemsAsync();
                Items.Clear();
                foreach (var item in items)
                {
                    Items.Add(item);
                }
                SelectedItem ??= Items.FirstOrDefault();

                await ReloadLinesAsync();
            }
            catch (Exception)
            {
                ErrorMessage = "Không tải được bảng giá phụ thu.";
            }
        }

        private async Task ReloadLinesAsync()
        {
            var lines = await _service.GetForStayAsync(_stayId);
            Lines.Clear();
            foreach (var line in lines)
            {
                Lines.Add(new SurchargeLine(line));
            }
            OnPropertyChanged(nameof(Total));
            OnPropertyChanged(nameof(TotalText));
            OnPropertyChanged(nameof(IsEmpty));
            Changed?.Invoke();
        }

        private async Task AddAsync(object? _)
        {
            ErrorMessage = null;
            if (SelectedItem == null)
            {
                ErrorMessage = "Chọn một mục trong bảng giá trước.";
                return;
            }

            var result = await _service.AddAsync(
                _stayId, SelectedItem.Id, Quantity, AppSession.CurrentUser?.Id ?? 0);
            if (!result.Ok)
            {
                ErrorMessage = result.Message;
                return;
            }

            Notify.Success(result.Message);
            Quantity = 1;
            await ReloadLinesAsync();
        }

        private async Task RemoveAsync(object? parameter)
        {
            if (parameter is not SurchargeLine line)
            {
                return;
            }

            var confirm = MessageBox.Show(
                $"Xoá dòng phụ thu \"{line.Name}\" ({line.SubtotalText})?",
                "Xác nhận xoá", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (confirm != MessageBoxResult.Yes)
            {
                return;
            }

            var result = await _service.RemoveAsync(line.Surcharge.Id);
            if (result.Ok)
            {
                Notify.Success(result.Message);
                await ReloadLinesAsync();
            }
            else
            {
                ErrorMessage = result.Message;
            }
        }
    }
}
