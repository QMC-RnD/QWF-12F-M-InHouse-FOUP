using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace Fate.Wpf.MVVM
{
    public class RelayCommand<TResult> : INotifyPropertyChanged, ICommand
    {
        private readonly Func<object, TResult> _execute;
        private readonly Predicate<object> _canExecute;
        private TResult _result;

        public RelayCommand(Func<object, TResult> execute)
            : this(execute, null)
        {
        }

        public RelayCommand(Func<object, TResult> execute, Predicate<object> canExecute)
        {
            _execute = execute ?? throw new ArgumentException("execute");
            _canExecute = canExecute;
        }

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public TResult Result
        {
            get => _result;
            set
            {
                _result = value;
                OnPropertyChanged();
            }
        }

        [DebuggerStepThrough]
        public bool CanExecute(object parameter)
        {
            return _canExecute == null ? true : _canExecute(parameter);
        }

        public void Execute(object parameter)
        {
            Result = _execute(parameter);
        }

        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
