using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;




namespace SachaBarber.Reactive
{

    public class WeakSubscription<T> : IDisposable, IObserver<T>
    {
        private readonly WeakReference reference;
        private readonly IDisposable subscription;
        private bool disposed;

        public WeakSubscription(IObservable<T> observable, IObserver<T> observer)
        {
            this.reference = new WeakReference(observer);
            this.subscription = observable.Subscribe(this);
        }

        void IObserver<T>.OnCompleted()
        {
            var observer = (IObserver<T>)this.reference.Target;
            if (observer != null)
                observer.OnCompleted();
            else
                this.Dispose();
        }

        void IObserver<T>.OnError(Exception error)
        {
            var observer = (IObserver<T>)this.reference.Target;
            if (observer != null)
                observer.OnError(error);
            else
                this.Dispose();
        }

        void IObserver<T>.OnNext(T value)
        {
            var observer = (IObserver<T>)this.reference.Target;
            if (observer != null)
                observer.OnNext(value);
            else
                this.Dispose();
        }

        public void Dispose()
        {
            if (!this.disposed)
            {
                this.disposed = true;
                this.subscription.Dispose();
            }
        }
    }

    public class ItemPropertyChangedEvent<TSender>
    {
        public TSender Sender
        {
            get;
            set;
        }
        public System.Reflection.PropertyInfo Property
        {
            get;
            set;
        }
        public bool HasOld
        {
            get;
            set;
        }
        public object OldValue
        {
            get;
            set;
        }
        public object NewValue
        {
            get;
            set;
        }
    }

    public class ItemPropertyChangedEvent<TSender, TProperty>
    {
        public TSender Sender
        {
            get;
            set;
        }
        public System.Reflection.PropertyInfo Property
        {
            get;
            set;
        }
        public bool HasOld
        {
            get;
            set;
        }
        public TProperty OldValue
        {
            get;
            set;
        }
        public TProperty NewValue
        {
            get;
            set;
        }
    }

    public class ItemChanged<T>
    {
        public T Item
        {
            get;
            set;
        }
        public bool Added
        {
            get;
            set;
        }

        public NotifyCollectionChangedEventArgs EventArgs
        {
            get;
            set;
        }
    }




    /// <summary>
    /// This class contains a mixture of my own and Nicolas Doriers extension methods which he has kindly let
    /// me use. Nicolas documents his extensions in this article:
    /// http://www.codeproject.com/Articles/731032/The-best-of-reactive-framework-extensions
    /// </summary>
    public static class ReactiveExtensions
    {
        /// <summary>
        /// An exponential back off strategy which starts with 1 second and then 4, 9, 16...
        /// </summary>
        public static readonly Func<int, TimeSpan> ExponentialBackoff = n => TimeSpan.FromSeconds(Math.Pow(n, 2));


public static IObservable<T> OnSubscribe<T>(this IObservable<T> source, Action OnSubscribeAction)
        {
            return Observable.Create<T>(
               observer =>
                   {
                       if (OnSubscribeAction != null)
                       {
                           try
                           {
                               OnSubscribeAction();
                           }
                           catch (Exception e)
                           {
                               Console.WriteLine(e);
                           }
                       }
                       source.Subscribe(observer);
                       return Disposable.Empty;
                   }
           );
        }


        public static IObservable<T> OnDispose<T>(this IObservable<T> source, Action OnDisposeAction)
        {
            return Observable.Create<T>(
               observer =>
               {
                   source.Subscribe(observer);
                   return Disposable.Create( 
                       ()=>
                        {
                            if (OnDisposeAction != null)
                            {
                                try
                                {
                                    OnDisposeAction();
                                }
                                catch (Exception e)
                                {
                                    Console.WriteLine(e);
                                }
                            }
                        });
               }
           );
        }

        public static void Dump<T>(this IObservable<T> source, string name)
        {
            source.Subscribe(
            i => Console.WriteLine("{0}-->{1}", name, i),
            ex => Console.WriteLine("{0} failed-->{1}", name, ex.Message),
            () => Console.WriteLine("{0} completed", name));
        }


        public static IObservable<Unit> AsUnit<TValue>(this IObservable<TValue> source)
        {
            return source.Select(x => new Unit());
        }


     
        public static IObservable<TItem> ObserveWeakly<TItem>(this IObservable<TItem> source)
        {
            return Observable.Create<TItem>(obs =>
            {
                var weakSubscription = new WeakSubscription<TItem>(source, obs);
                return () =>
                {
                    weakSubscription.Dispose();
                };
            });
        }


        public static IObservable<ItemPropertyChangedEvent<TItem>> ObserverPropertyChanged<TItem>(this TItem target, string propertyName = null, bool fireCurrentValue = false) where TItem : INotifyPropertyChanged
        {
            if (propertyName == null && fireCurrentValue)
                throw new InvalidOperationException("You need to specify a propertyName if you want to fire the current value of your property");
         
            return Observable.Create<ItemPropertyChangedEvent<TItem>>(obs =>
            {
                Dictionary<PropertyInfo, object> oldValues = new Dictionary<PropertyInfo, object>();
                Dictionary<string, PropertyInfo> properties = new Dictionary<string, PropertyInfo>();
                PropertyChangedEventHandler handler = null;
                handler = (s, a) =>
                {
                    if (propertyName == null || propertyName == a.PropertyName)
                    {
                        PropertyInfo prop = null;
                        if (!properties.TryGetValue(a.PropertyName, out prop))
                        {
                            prop = typeof(TItem).GetProperty(a.PropertyName);
                            properties.Add(a.PropertyName, prop);
                        }
                        var change = new ItemPropertyChangedEvent<TItem>()
                        {
                            Sender = target,
                            Property = prop,
                            NewValue = prop.GetValue(target)
                        };
                        object oldValue = null;
                        if (oldValues.TryGetValue(prop, out oldValue))
                        {
                            change.HasOld = true;
                            change.OldValue = oldValue;
                            oldValues[prop] = change.NewValue;
                        }
                        else
                        {
                            oldValues.Add(prop, change.NewValue);
                        }
                        obs.OnNext(change);
                    }
                };
                target.PropertyChanged += handler;
                if (propertyName != null && fireCurrentValue)
                    handler(target, new PropertyChangedEventArgs(propertyName));

                return () =>
                {
                    target.PropertyChanged -= handler;
                };
            });
        }


        public static IObservable<ItemPropertyChangedEvent<TItem, TProperty>> ObserverPropertyChanged<TItem, TProperty>(this TItem target, Expression<Func<TItem, TProperty>> propertyName, bool fireCurrentValue = false) where TItem : INotifyPropertyChanged
        {
            var property = ExpressionExtensions.GetPropertyName(propertyName);
            return ObserverPropertyChanged(target, property, fireCurrentValue)
                   .Select(i => new ItemPropertyChangedEvent<TItem, TProperty>()
                   {
                       HasOld = i.HasOld,
                       NewValue = (TProperty)i.NewValue,
                       OldValue = i.OldValue == null ? default(TProperty) : (TProperty)i.OldValue,
                       Property = i.Property,
                       Sender = i.Sender
                   });
        }


        public static IObservable<ItemChanged<T>> ItemChanged<T>(this ObservableCollection<T> collection, bool fireForExisting = false)
        {
            var observable = Observable.Create<ItemChanged<T>>(obs =>
            {
                NotifyCollectionChangedEventHandler handler = null;
                handler = (s, a) =>
                {
                    if (a.NewItems != null)
                    {
                        foreach (var item in a.NewItems.OfType<T>())
                        {
                            obs.OnNext(new ItemChanged<T>()
                            {
                                Item = item,
                                Added = true,
                                EventArgs = a
                            });
                        }
                    }
                    if (a.OldItems != null)
                    {
                        foreach (var item in a.OldItems.OfType<T>())
                        {
                            obs.OnNext(new ItemChanged<T>()
                            {
                                Item = item,
                                Added = false,
                                EventArgs = a
                            });
                        }
                    }
                };
                collection.CollectionChanged += handler;
                return () =>
                {
                    collection.CollectionChanged -= handler;
                };
            });

            if (fireForExisting)
                observable = observable.StartWith(collection.Select(i => new ItemChanged<T>()
                {
                    Item = i,
                    Added = true,
                    EventArgs = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, i)
                }));
            return observable;
        }


        /// <summary>
        /// Provides ability to retry with incremental back off. Example shown below, where Friends in ObservableCollection, and User in also INPC
        /// </summary>
        /// <code>
        ///    user.Friends
        ///        .ObserveInner(u => u.ObserverPropertyChanged())
        ///        .Subscribe(spy);
        /// </code>
        public static IObservable<TObserved> ObserveInner<TItem, TObserved>(this ObservableCollection<TItem> collection, Func<TItem, IObservable<TObserved>> observe)
        {
            return Observable.Create<TObserved>(obs =>
            {
                Dictionary<TItem, IDisposable> subscriptions = new Dictionary<TItem, IDisposable>();
                var mainSubscription =
                    collection.ItemChanged(true)
                       .Subscribe(change =>
                       {
                           IDisposable subscription = null;
                           subscriptions.TryGetValue(change.Item, out subscription);
                           if (change.Added)
                           {
                               if (subscription == null)
                               {
                                   subscription = observe(change.Item).Subscribe(obs);
                                   subscriptions.Add(change.Item, subscription);
                               }
                           }
                           else
                           {
                               if (subscription != null)
                               {
                                   subscriptions.Remove(change.Item);
                                   subscription.Dispose();
                               }
                           }
                       });
                return () =>
                {
                    mainSubscription.Dispose();
                    foreach (var subscription in subscriptions)
                        subscription.Value.Dispose();
                };
            });

        }


        /// <summary>
        /// Provides ability to retry with incremental back off. Example shown below
        /// </summary>
        /// <code>
        /// private static bool shouldThrow = true;
        ///     static void Main(string[] args)
        ///     {
        ///         Generate().RetryWithBackoffStrategy(3, MyRxExtensions.ExponentialBackoff,
        ///             ex =>
        ///             {
        ///                 return ex is NullReferenceException;
        ///             }, Scheduler.TaskPool)
        ///             .Subscribe(
        ///                 OnNext,
        ///                 OnError
        ///             );
        ///         Console.ReadLine();
        ///     }
        ///
        ///     private static void OnNext(int val)
        ///     {
        ///         Console.WriteLine("subscriber value is {0} which was seen on threadId:{1}", 
        ///             val, Thread.CurrentThread.ManagedThreadId);
        ///     }
        ///
        ///     private static void OnError(Exception ex)
        ///     {
        ///         Console.WriteLine("subscriber bad {0}, which was seen on threadId:{1}", 
        ///             ex.GetType(),
        ///             Thread.CurrentThread.ManagedThreadId);
        ///     }
        ///
        ///
        ///
        ///     static IObservable<int> Generate()
        ///     {
        ///         return Observable.Create<int>(
        ///             o =>
        ///             {
        ///
        ///                 Scheduler.TaskPool.Schedule(() =>
        ///                     {
        ///
        ///                         if (shouldThrow)
        ///                         {
        ///                             shouldThrow = false;
        ///                             Console.WriteLine("ON ERROR NullReferenceException");
        ///                             o.OnError(new NullReferenceException("Throwing"));
        ///                         }
        ///                         Console.WriteLine("Invoked on threadId:{0}", 
        ///                             Thread.CurrentThread.ManagedThreadId);

        ///                         Console.WriteLine("On nexting 1");
        ///                         o.OnNext(1);
        ///                         Console.WriteLine("On nexting 2");
        ///                         o.OnNext(2);
        ///                         Console.WriteLine("On nexting 3");
        ///                         o.OnNext(3);
        ///                         o.OnCompleted();
        ///                         Console.WriteLine("On complete");
        ///                         Console.WriteLine("Finished on threadId:{0}", 
        ///                             Thread.CurrentThread.ManagedThreadId);
        ///
        ///                     });
        ///
        ///                 return () => { };
        ///             });
        ///
        ///     }
        ///
        /// }
        /// </code>
        public static IObservable<T> RetryWithBackoffStrategy<T>(
            this IObservable<T> source,
            int retryCount = 3,
            Func<int, TimeSpan> strategy = null,
            Func<Exception, bool> retryOnError = null,
            IScheduler scheduler = null)
        {
            strategy = strategy ?? ReactiveExtensions.ExponentialBackoff;

            int attempt = 0;

            return Observable.Defer(() =>
            {
                return ((++attempt == 1) ? source : source.DelaySubscription(strategy(attempt - 1), scheduler))
                    .Select(item => new Tuple<bool, T, Exception>(true, item, null))
                    .Catch<Tuple<bool, T, Exception>, Exception>(e =>

                        retryOnError(e)
                        ? Observable.Throw<Tuple<bool, T, Exception>>(e)
                        : Observable.Return(new Tuple<bool, T, Exception>(false, default(T), e)));
            })
            .Retry(retryCount)
            .SelectMany(t => t.Item1
                ? Observable.Return(t.Item2)
                : Observable.Throw<T>(t.Item3));
        }

    }
}
