using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Fate.Wpf.MVVM
{
    public interface IWindow
    {
        bool? WindowResult { get; }

        Task OnLoadedAsync();

        Task<bool> CanCloseAsync();

        void Close(bool? windowResult = null);

        void IsEnabled(bool enable);

        void RegisterViewClose(Action close);

        void RegisterViewIsEnabled(Action<bool> isEnabled);
    }
}
