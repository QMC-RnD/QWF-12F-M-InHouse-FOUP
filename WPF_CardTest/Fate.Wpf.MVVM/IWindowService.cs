using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace Fate.Wpf.MVVM
{
    public interface IWindowService
    {
        void Show<T>(IWindow dataContext)
            where T : Window, new();

        bool? ShowDialog<T>(IWindow dataContext)
            where T : Window, new();

        Task<bool?> ShowWindow<T>(IWindow dataContext, CancellationToken token)
            where T : Window, new();

        Task<bool?> ShowWindow<T>(IWindow dataContext, IWindow parentContext, CancellationToken token)
            where T : Window, new();
    }
}
