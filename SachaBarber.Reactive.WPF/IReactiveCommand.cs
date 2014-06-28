using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace SachaBarber.Reactive.WPF
{
    public interface IReactiveCommand : ICommand
    {
        IObservable<object> CommandExecutedStream { get; }
        IObservable<Exception> CommandExeceptionsStream { get; }
        void AddPredicate(IObservable<bool> predicate);
    }
}
