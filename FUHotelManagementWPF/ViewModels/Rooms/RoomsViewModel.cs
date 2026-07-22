using System.Threading.Tasks;
using FUHotelManagementWPF.MvvmCore;

namespace FUHotelManagementWPF.ViewModels.Rooms
{
    /// <summary>
    /// Module "So do phong": 3 tab (so do / danh sach phong / loai phong).
    /// Moi tab la 1 ViewModel con; sau moi thao tac ghi, con goi RefreshAllAsync
    /// de ca 3 tab dong bo du lieu.
    /// </summary>
    public class RoomsViewModel : ViewModelBase
    {
        public RoomMapViewModel Map { get; }
        public RoomListViewModel RoomList { get; }
        public RoomTypeListViewModel RoomTypeList { get; }

        public RoomsViewModel()
        {
            Map = new RoomMapViewModel(RefreshAllAsync);
            RoomList = new RoomListViewModel(RefreshAllAsync);
            RoomTypeList = new RoomTypeListViewModel(RefreshAllAsync);
            _ = RefreshAllAsync();
        }

        public Task RefreshAllAsync()
            => Task.WhenAll(Map.LoadAsync(), RoomList.LoadAsync(), RoomTypeList.LoadAsync());
    }
}
