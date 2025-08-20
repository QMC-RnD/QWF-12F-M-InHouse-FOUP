using System;
using System.Threading.Tasks;

namespace Fate.Wpf.MVVM
{
    public class ViewModelBase : PropertyChangedBase, IWindow
    {
        private Action _close;
        private Action<bool> _isEnabled;

        public bool? WindowResult { get; private set; }

        public virtual Task OnLoadedAsync()
        {
            return Task.CompletedTask;
        }

        public virtual Task<bool> CanCloseAsync()
        {
            return Task.FromResult(true);
        }

        public void Close(bool? windowResult = null)
        {
            WindowResult = windowResult;
            _close();
        }

        public void IsEnabled(bool enable)
        {
            _isEnabled(enable);
        }

        void IWindow.RegisterViewClose(Action close)
        {
            _close = close;
        }

        void IWindow.RegisterViewIsEnabled(Action<bool> isEnabled)
        {
            _isEnabled = isEnabled;
        }
    }
}
