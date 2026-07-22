using System;
using System.Windows.Input;

namespace FUHotelManagementWPF.MvvmCore
{
    /// <summary>
    /// Triển khai ICommand đa dụng — để bind Button.Command vào method trong ViewModel.
    /// Thay thế cho event Click ở code-behind (đúng tinh thần MVVM).
    /// </summary>
    public class RelayCommand : ICommand
    {
        private readonly Action<object?> _execute;
        private readonly Predicate<object?>? _canExecute;

        public RelayCommand(Action<object?> execute, Predicate<object?>? canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public bool CanExecute(object? parameter) => _canExecute == null || _canExecute(parameter);

        public void Execute(object? parameter) => _execute(parameter);

        // WPF tự hỏi lại CanExecute mỗi khi UI có thay đổi (focus, input...).
        public event EventHandler? CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }
    }
}
