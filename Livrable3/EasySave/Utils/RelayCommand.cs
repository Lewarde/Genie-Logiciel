// RelayCommand.cs
using System;
using System.Windows.Input;

namespace EasySave.Commands // Or any namespace you prefer
{
    public class RelayCommand : ICommand
    {
        private readonly Action<object> _execute; // The method to execute
        private readonly Predicate<object> _canExecute; // Method that determines if the command can run

        // Constructor for commands that are always executable
        public RelayCommand(Action<object> execute) : this(execute, null)
        {
        }

        // Constructor with custom execute and canExecute logic
        public RelayCommand(Action<object> execute, Predicate<object> canExecute)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        // Constructor for parameterless execute method
        public RelayCommand(Action execute) : this(execute, null)
        {
        }

        // Constructor for parameterless execute and canExecute methods
        public RelayCommand(Action execute, Func<bool> canExecute)
            : this(o => execute(), o => canExecute == null || canExecute())
        {
            if (execute == null)
                throw new ArgumentNullException(nameof(execute));
        }

        // Called by WPF to check if the command can be executed
        public bool CanExecute(object parameter)
        {
            return _canExecute == null || _canExecute(parameter);
        }

        // Event raised when WPF should recheck command executability (e.g. UI refresh)
        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        // Called by WPF when the command is executed
        public void Execute(object parameter)
        {
            _execute(parameter);
        }

        // Manually force WPF to re-evaluate CanExecute
        public void RaiseCanExecuteChanged()
        {
            CommandManager.InvalidateRequerySuggested();
        }
    }
}
