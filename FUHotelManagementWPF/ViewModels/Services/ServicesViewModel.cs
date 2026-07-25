using System.Threading.Tasks;
using FUHotelManagementWPF.MvvmCore;

namespace FUHotelManagementWPF.ViewModels.Services
{
    /// <summary>
    /// Module "Dich vu": 2 tab.
    ///   - Danh muc: nhom dich vu va cac mon (Admin/Manager sua, nguoi khac chi xem).
    ///   - Goi dich vu: chon phong dang co khach o roi goi mon cho ho.
    /// Sua danh muc xong phai nap lai ca tab Goi dich vu, khong thi danh sach mon
    /// ben do van con mon vua bi ngung ban.
    /// </summary>
    public class ServicesViewModel : ViewModelBase
    {
        public ServiceCatalogViewModel Catalog { get; }
        public ServiceOrderViewModel Orders { get; }

        public ServicesViewModel()
        {
            Catalog = new ServiceCatalogViewModel(RefreshAllAsync);
            Orders = new ServiceOrderViewModel();
            _ = RefreshAllAsync();
        }

        public Task RefreshAllAsync() => Task.WhenAll(Catalog.LoadAsync(), Orders.LoadAsync());
    }
}
