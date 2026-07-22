using System;
using System.Threading.Tasks;
using System.Windows.Input;

namespace FUHotelManagementWPF.MvvmCore
{
    /// <summary>
    /// ICommand cho hanh dong bat dong bo (goi DB, IO...). Tu khoa nut trong luc chay
    /// nen khong can them co IsBusy chi de chong bam doi. QUY UOC NHOM: moi nut co goi
    /// database deu dung lop nay, khong dung RelayCommand + .Result/.Wait() (dung hinh UI).
    /// </summary>
    public class AsyncRelayCommand : ICommand
    {
        private readonly Func<object?, Task> _execute;
        private readonly Predicate<object?>? _canExecute;
        private bool _isExecuting;

        public AsyncRelayCommand(Func<object?, Task> execute, Predicate<object?>? canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public bool CanExecute(object? parameter)
            => !_isExecuting && (_canExecute == null || _canExecute(parameter));

        // async void la dang chuan cho ICommand.Execute; ham async cua ViewModel
        // phai tu try/catch loi cua chinh no (xem LoginViewModel lam mau).
        public async void Execute(object? parameter)
        {
            _isExecuting = true;
            CommandManager.InvalidateRequerySuggested();
            try
            {
                await _execute(parameter);
            }
            finally
            {
                _isExecuting = false;
                CommandManager.InvalidateRequerySuggested();
            }
        }

        public event EventHandler? CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }
    }
}
