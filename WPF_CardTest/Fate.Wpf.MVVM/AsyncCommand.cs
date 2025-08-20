using System;
using System.Threading;
using System.Threading.Tasks;

namespace Fate.Wpf.MVVM
{
    public class AsyncCommand
    {
        public static AsyncCommand<object> Create(Func<object, Task> command, Predicate<object> canExecute = null)
        {
            return new AsyncCommand<object>(
                async (token, param) =>
                {
                    await command(param);
                    return null;
                }, canExecute);
        }

        public static AsyncCommand<TResult> Create<TResult>(Func<object, Task<TResult>> command, Predicate<object> canExecute = null)
        {
            return new AsyncCommand<TResult>((token, param) => command(param), canExecute);
        }

        public static AsyncCommand<object> Create(Func<CancellationToken, object, Task> command, Predicate<object> canExecute = null)
        {
            return new AsyncCommand<object>(
                async (token, param) =>
                {
                    await command(token, param);
                    return null;
                }, canExecute);
        }

        public static AsyncCommand<TResult> Create<TResult>(Func<CancellationToken, object, Task<TResult>> command, Predicate<object> canExecute = null)
        {
            return new AsyncCommand<TResult>(command, canExecute);
        }
    }
}
