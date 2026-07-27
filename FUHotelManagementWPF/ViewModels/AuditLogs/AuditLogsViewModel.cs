using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using BusinessObjects.Entities;
using FUHotelManagementWPF.MvvmCore;
using Services;

namespace FUHotelManagementWPF.ViewModels.AuditLogs
{
    /// <summary>Mot muc trong o chon hanh dong. Ma rong = xem tat ca.</summary>
    public sealed record ActionOption(string Code, string Label);

    /// <summary>
    /// Module Nhat ky he thong (chi vai tro co quyen <c>audit.view</c>): tra cuu ai da lam gi,
    /// luc nao. Truoc day quyen nay da cap cho Quan tri vien nhung khong co man hinh nao doc.
    /// </summary>
    public class AuditLogsViewModel : ViewModelBase
    {
        private readonly IAuditLogService _service = new AuditLogService();

        public ObservableCollection<AuditLogRow> Rows { get; } = [];

        public bool HasPermission { get; } = AuthorizationPolicy.CanViewAuditLog;
        public bool NoPermission => !HasPermission;

        // ---- Bo loc ----

        /// <summary>Mac dinh 7 ngay gan nhat: mo man ra la thay ngay viec vua lam.</summary>
        private DateTime _fromDate = DateTime.Today.AddDays(-6);
        public DateTime FromDate
        {
            get => _fromDate;
            set => SetProperty(ref _fromDate, value);
        }

        private DateTime _toDate = DateTime.Today;
        public DateTime ToDate
        {
            get => _toDate;
            set => SetProperty(ref _toDate, value);
        }

        public ObservableCollection<ActionOption> ActionOptions { get; } =
        [
            new(string.Empty, "Tất cả hành động"),
        ];

        private ActionOption _selectedAction;
        public ActionOption SelectedAction
        {
            get => _selectedAction;
            set => SetProperty(ref _selectedAction, value);
        }

        public ObservableCollection<User> Actors { get; } = [];

        private User? _selectedActor;
        public User? SelectedActor
        {
            get => _selectedActor;
            set => SetProperty(ref _selectedActor, value);
        }

        // ---- Trang thai man hinh ----

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
            set
            {
                if (SetProperty(ref _errorMessage, value))
                {
                    OnPropertyChanged(nameof(HasError));
                    OnPropertyChanged(nameof(IsEmpty));
                }
            }
        }
        public bool HasError => !string.IsNullOrWhiteSpace(_errorMessage);

        public bool IsEmpty => HasPermission && !IsLoading && !HasError && Rows.Count == 0;

        public bool IsCapped => Rows.Count >= AuditLogService.MaxRows;

        public string CappedNoticeText => $"Hiển thị {AuditLogService.MaxRows} dòng gần nhất. Vui lòng thu hẹp khoảng ngày hoặc dùng bộ lọc để xem đầy đủ nhật ký.";

        public string TotalText => Rows.Count >= AuditLogService.MaxRows
            ? $"{AuditLogService.MaxRows} dòng (đã đạt giới hạn)"
            : (Rows.Count == 0 ? "Chưa có dòng nào" : $"{Rows.Count} dòng");

        public AsyncRelayCommand SearchCommand { get; }
        public RelayCommand ResetFilterCommand { get; }

        public AuditLogsViewModel()
        {
            _selectedAction = ActionOptions[0];
            SearchCommand = new AsyncRelayCommand(_ => LoadAsync());
            ResetFilterCommand = new RelayCommand(_ => ResetFilter());

            if (HasPermission)
            {
                _ = InitAsync();
            }
        }

        /// <summary>Do hai o chon truoc roi moi tim - de nguoi dung khong thay o rong luc dau.</summary>
        private async Task InitAsync()
        {
            await LoadFilterSourcesAsync();
            await LoadAsync();
        }

        private async Task LoadFilterSourcesAsync()
        {
            var codes = await _service.GetActionCodesAsync();
            if (codes.Ok && codes.Data != null)
            {
                foreach (var code in codes.Data)
                {
                    ActionOptions.Add(new ActionOption(code, new AuditLogRow(new AuditLog { ActionCode = code }).ActionText));
                }
            }

            // Nguoi thao tac lay tu chinh bang nhat ky: chi liet ke ai da tung de lai dau vet,
            // va khong doi them quyen quan ly nhan su.
            var actors = await _service.GetActorsAsync();
            if (actors.Ok && actors.Data != null)
            {
                foreach (var user in actors.Data)
                {
                    Actors.Add(user);
                }
            }
        }

        public async Task LoadAsync()
        {
            IsLoading = true;
            ErrorMessage = null;
            try
            {
                var result = await _service.SearchAsync(FromDate, ToDate, SelectedActor?.Id, SelectedAction.Code);
                Rows.Clear();
                if (result.Ok)
                {
                    foreach (var log in result.Data ?? [])
                    {
                        Rows.Add(new AuditLogRow(log));
                    }
                }
                else
                {
                    ErrorMessage = result.Message;
                }
            }
            catch (Exception)
            {
                Rows.Clear();
                ErrorMessage = "Không tải được nhật ký. Kiểm tra kết nối SQL Server rồi thử lại.";
            }
            finally
            {
                IsLoading = false;
                OnPropertyChanged(nameof(TotalText));
                OnPropertyChanged(nameof(IsEmpty));
                OnPropertyChanged(nameof(IsCapped));
            }
        }

        private void ResetFilter()
        {
            FromDate = DateTime.Today.AddDays(-6);
            ToDate = DateTime.Today;
            SelectedAction = ActionOptions[0];
            SelectedActor = null;
            _ = LoadAsync();
        }
    }
}
