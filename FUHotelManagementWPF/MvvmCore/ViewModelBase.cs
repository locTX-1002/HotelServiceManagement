using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace FUHotelManagementWPF.MvvmCore
{
    /// <summary>
    /// Lớp cơ sở cho mọi ViewModel. Implement INotifyPropertyChanged để View tự
    /// cập nhật khi property thay đổi (cơ chế cốt lõi của data binding 2 chiều).
    /// </summary>
    public class ViewModelBase : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        /// <summary>
        /// Gán giá trị mới + raise PropertyChanged nếu giá trị thực sự thay đổi.
        /// [CallerMemberName] tự lấy tên property gọi nó → khỏi gõ "tên" thủ công.
        /// </summary>
        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? name = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(name);
            return true;
        }
    }
}
