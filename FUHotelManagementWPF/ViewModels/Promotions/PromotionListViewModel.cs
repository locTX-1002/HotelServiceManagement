using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using BusinessObjects.Entities;
using FUHotelManagementWPF.MvvmCore;
using FUHotelManagementWPF.ViewModels.Rooms;
using FUHotelManagementWPF.Views.Dialogs;
using Services;

namespace FUHotelManagementWPF.ViewModels.Promotions
{
    /// <summary>Bo loc nhanh tren toolbar (chip Tat ca / Dang chay / Het han).</summary>
    public enum PromotionFilter
    {
        All,
        Running,
        Expired,
    }

    /// <summary>Module Khuyen mai: danh sach card-row + tim kiem + loc nhanh + them/sua popup.</summary>
    public class PromotionListViewModel : ViewModelBase
    {
        private readonly IPromotionService _service = new PromotionService();

        // Giu ban goc tai ve mot lan; tim kiem va loc chay ngay tren bo nho vi
        // IPromotionService chi co GetAllAsync (khong co ham search rieng)
        // -> khoi ban DB moi lan go phim.
        private List<Promotion> _all = [];

        public ObservableCollection<PromotionRow> Rows { get; } = [];

        private string _searchText = string.Empty;
        public string SearchText
        {
            get => _searchText;
            set { if (SetProperty(ref _searchText, value)) { ApplyFilter(); } }
        }

        private PromotionFilter _filter = PromotionFilter.All;
        public PromotionFilter Filter
        {
            get => _filter;
            set { if (SetProperty(ref _filter, value)) { ApplyFilter(); } }
        }

        private PromotionRow? _selectedRow;
        public PromotionRow? SelectedRow
        {
            get => _selectedRow;
            set { if (SetProperty(ref _selectedRow, value)) { OnPropertyChanged(nameof(HasSelection)); } }
        }
        public bool HasSelection => _selectedRow != null;

        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            set { if (SetProperty(ref _isLoading, value)) { OnPropertyChanged(nameof(IsEmpty)); } }
        }

        private string? _errorMessage;
        public string? ErrorMessage
        {
            get => _errorMessage;
            set { if (SetProperty(ref _errorMessage, value)) { OnPropertyChanged(nameof(IsEmpty)); } }
        }

        // Rong chi tinh khi da tai xong VA khong co loi - de 3 trang thai khong de len nhau.
        public bool IsEmpty => !IsLoading && ErrorMessage == null && Rows.Count == 0;

        public string TotalText
        {
            get
            {
                var running = Rows.Count(r => r.Status == PromotionStatus.Running);
                return $"{Rows.Count} khuyến mãi · {running} đang áp dụng";
            }
        }

        // Service cung chan quyen, nhung an nut truoc cho do bam vao roi bi tu choi.
        public bool CanManage => AppSession.RoleName is "Admin" or "Manager";

        public RelayCommand AddCommand { get; }
        public RelayCommand EditCommand { get; }
        public RelayCommand SetFilterCommand { get; }
        public AsyncRelayCommand ReloadCommand { get; }

        public PromotionListViewModel()
        {
            AddCommand = new RelayCommand(_ => OpenDialog(null), _ => CanManage);
            EditCommand = new RelayCommand(_ => OpenDialog(SelectedRow?.Promotion),
                                           _ => CanManage && SelectedRow != null);
            SetFilterCommand = new RelayCommand(p => { if (p is PromotionFilter f) { Filter = f; } });
            ReloadCommand = new AsyncRelayCommand(_ => LoadAsync());
            _ = LoadAsync();
        }

        public async Task LoadAsync()
        {
            IsLoading = true;
            ErrorMessage = null;
            try
            {
                var list = await _service.GetAllAsync();
                // Sap xep: moi tao / moi bat dau len truoc cho de theo doi.
                _all = list.OrderByDescending(p => p.StartDate).ThenBy(p => p.Code).ToList();
                ApplyFilter();
            }
            catch (Exception)
            {
                _all = [];
                Rows.Clear();
                ErrorMessage = "Không tải được danh sách khuyến mãi. Kiểm tra kết nối SQL Server rồi thử lại.";
            }
            finally
            {
                IsLoading = false;
            }
        }

        // Loc tren bo nho: tim theo ma hoac mo ta, roi lay theo chip dang chon.
        private void ApplyFilter()
        {
            var keyword = SearchText.Trim();
            var keepId = SelectedRow?.Promotion.Id;

            var query = _all.Select(p => new PromotionRow(p));

            if (keyword.Length > 0)
            {
                query = query.Where(r =>
                    r.Promotion.Code.Contains(keyword, StringComparison.OrdinalIgnoreCase)
                    || (r.Promotion.Description ?? string.Empty)
                        .Contains(keyword, StringComparison.OrdinalIgnoreCase));
            }

            query = Filter switch
            {
                PromotionFilter.Running => query.Where(r => r.Status == PromotionStatus.Running),
                PromotionFilter.Expired => query.Where(r => r.Status == PromotionStatus.Expired),
                _ => query,
            };

            Rows.Clear();
            foreach (var row in query)
            {
                Rows.Add(row);
            }

            SelectedRow = Rows.FirstOrDefault(r => r.Promotion.Id == keepId);
            OnPropertyChanged(nameof(TotalText));
            OnPropertyChanged(nameof(IsEmpty));
        }

        private async void OpenDialog(Promotion? existing)
        {
            if (!CanManage)
            {
                Notify.Warning("Chỉ Quản trị viên hoặc Quản lý mới sửa được khuyến mãi.");
                return;
            }

            var dialog = new PromotionEditDialog(new PromotionEditDialogViewModel(existing))
            {
                Owner = RoomMapViewModel.ActiveWindow(),
            };
            if (dialog.ShowDialog() == true)
            {
                await LoadAsync();
            }
        }
    }
}
