using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace FUHotelManagementWPF.MvvmCore
{
    /// <summary>
    /// Nen validate CHUAN cua ca nhom (INotifyDataErrorInfo - co che validate chinh thong cua WPF).
    /// ViewModel co form ke thua lop nay; goi AddError("TenProperty", "loi...") thi o nhap binding
    /// vao property do TU DONG vien do + tooltip loi (style ngam trong Themes/Controls.xaml).
    /// Xem LoginViewModel lam mau truoc khi viet form moi.
    /// </summary>
    public class ValidatableViewModelBase : ViewModelBase, INotifyDataErrorInfo
    {
        private readonly Dictionary<string, List<string>> _errors = new();

        public bool HasErrors => _errors.Count > 0;

        public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged;

        public IEnumerable GetErrors(string? propertyName)
            => propertyName != null && _errors.TryGetValue(propertyName, out var list)
                ? list
                : Enumerable.Empty<string>();

        protected void AddError(string propertyName, string message)
        {
            if (!_errors.TryGetValue(propertyName, out var list))
            {
                list = [];
                _errors[propertyName] = list;
            }
            if (!list.Contains(message))
            {
                list.Add(message);
                ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));
            }
        }

        protected void ClearErrors(string propertyName)
        {
            if (_errors.Remove(propertyName))
            {
                ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));
            }
        }

        protected void ClearAllErrors()
        {
            var names = _errors.Keys.ToList();
            _errors.Clear();
            foreach (var name in names)
            {
                ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(name));
            }
        }
    }
}
