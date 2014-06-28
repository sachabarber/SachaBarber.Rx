using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text;
using System.Threading.Tasks;

namespace SachaBarber.Reactive
{
    public sealed class EventMessager : IEventMessager
    {
        private readonly Dictionary<Type, object> subscriberLookup = new Dictionary<Type, object>();

        public IObservable<T> Observe<T>()
        {
            object subject;
            if (!subscriberLookup.TryGetValue(typeof(T), out subject))
            {
                subject = new Subject<T>();
                subscriberLookup.Add(typeof(T), subject);
            }
            return ((ISubject<T>)subject).AsObservable();
        }

        public void Publish<T>(T @event)
        {
            object subject;
            if (subscriberLookup.TryGetValue(@event.GetType(), out subject))
            {
                ((Subject<T>)subject).OnNext(@event);
            }
        }
    }
}
