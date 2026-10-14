using System.Windows.Input;

namespace DCSRelay.ViewModels
{
    public class RelayCommand : ICommand
    {
        private readonly Action<object> _execute;
        public event EventHandler CanExecuteChanged;
        public RelayCommand(Action<object> execute) => _execute = execute;
        public bool CanExecute(object parameter) => true; // Always true for now
        public void Execute(object parameter) => _execute?.Invoke(parameter);
    }
}