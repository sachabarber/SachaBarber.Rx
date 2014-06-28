using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SachaBarber.Reactive
{
    public interface IEventMessager
    {
        IObservable<T> Observe<T>();
        void Publish<T>(T @event);
    }
}
