using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace SachaBarber.Reactive.WPF
{

    public class DependencyPropertyChangedEvent<T>
    {

        public DependencyPropertyChangedEvent()
        {

        }

        public DependencyPropertyChangedEvent(DependencyObject obj, DependencyProperty property, DependencyPropertyChangedEvent<T> lastEvent)
        {
            DependencyProperty = property;
            Sender = obj;
            if (lastEvent == null)
            {
                HasOld = false;
            }
            else
            {
                HasOld = true;
                OldValue = lastEvent.NewValue;
            }
            NewValue = (T)obj.GetValue(property);
        }

        public DependencyProperty DependencyProperty
        {
            get;
            set;
        }

        public DependencyObject Sender
        {
            get;
            set;
        }

        public T NewValue
        {
            get;
            set;
        }
        public T OldValue
        {
            get;
            set;
        }

        public bool HasOld
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
    public static class WPFReactiveExtensions
    {
        /// <summary>
        /// Provides ability to retry with incremental back off. Example shown below, where Friends in ObservableCollection, and User in also INPC
        /// </summary>
        /// <code>
        /// TextBox box = new TextBox();
        /// box.DependencyPropertyChanged<Brush>(TextBox.BackgroundProperty)
        ///    .Subscribe(args =>
        ///    {
        ///        Brush brush = args.NewValue;
        ///    });
        ///</code>
        public static IObservable<DependencyPropertyChangedEvent<T>> DependencyPropertyChanged<T>(this DependencyObject obj, DependencyProperty property)
        {
            return Observable.Create<DependencyPropertyChangedEvent<T>>((obs) =>
            {
                DependencyPropertyDescriptor dpd = DependencyPropertyDescriptor.FromProperty(property, obj.GetType());
                DependencyPropertyChangedEvent<T> lastEvent = null;
                EventHandler handler = (sender, args) =>
                {
                    var oldValue = (T)obj.GetValue(property);
                    var evt = new DependencyPropertyChangedEvent<T>(obj, property, lastEvent);
                    lastEvent = evt;
                    obs.OnNext(evt);
                };
                dpd.AddValueChanged(obj, handler);

                return () =>
                {
                    dpd.RemoveValueChanged(obj, handler);
                };
            });
        }
    }
}
