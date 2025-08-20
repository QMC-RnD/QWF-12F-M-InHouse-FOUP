using System.Threading.Tasks;
using System.Windows.Input;

namespace Fate.Wpf.MVVM
{
    public interface IAsyncCommand : ICommand
    {
        Task ExecuteAsync(object parameter);
    }
}