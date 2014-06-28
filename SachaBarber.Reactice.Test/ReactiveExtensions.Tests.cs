using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using NUnit.Framework;

using SachaBarber.Reactive;
using SachaBarber.Reactive.WPF;
using System.Windows.Controls;

namespace SachaBarber.Reactice.Test
{
    [TestFixture]
    public class ReactiveTests
    {
        [Test]
        public void CanWeaklyObserve()
        {
            SpyObserver<ItemPropertyChangedEvent<User>> spy = new SpyObserver<ItemPropertyChangedEvent<User>>();
            User user = new User();
            WeakReference spyRef = new WeakReference(spy);
            WeakReference userRef = new WeakReference(user);
            user.ObserverPropertyChanged().ObserveWeakly().Subscribe(spy);
            user.AssertSubscribers(1);
            user.Age = 50;
            spy.AssertFired(1);
            spy = null;
            GC.Collect();
            Assert.IsNull(spyRef.Target);
            user.Age = 200;
            user.AssertSubscribers(0);
            user = null;
            GC.Collect();
            Assert.IsNull(userRef.Target);
        }


        [Test]
        public void CanFireDependencyObject()
        {
            TextBox box = new TextBox();
            var spy = new SpyObserver<DependencyPropertyChangedEvent<string>>();
            var subscription =
                box.DependencyPropertyChanged<string>(TextBox.TextProperty)
                    .Subscribe(spy);

            box.Text = "newText";
            spy.AssertFired(1);
            Assert.AreEqual(box, spy.LastEvent.Sender);
            Assert.AreEqual(TextBox.TextProperty, spy.LastEvent.DependencyProperty);
            Assert.AreEqual("newText", spy.LastEvent.NewValue);
            Assert.AreEqual(false, spy.LastEvent.HasOld);
            Assert.AreEqual(null, spy.LastEvent.OldValue);

            box.Text = "hey";
            spy.AssertFired(2);
            Assert.AreEqual("hey", spy.LastEvent.NewValue);
            Assert.AreEqual(true, spy.LastEvent.HasOld);
            Assert.AreEqual("newText", spy.LastEvent.OldValue);

            subscription.Dispose();
            box.Text = "loo";
            spy.AssertFired(2);
        }


        [Test]
        public void CanObserveItems()
        {
            SpyObserver<ItemPropertyChangedEvent<User>> spy = new SpyObserver<ItemPropertyChangedEvent<User>>();
            User user = new User();
            var tom = user.AddFriend("Tom");
            user.Friends
                .ObserveInner(u => u.ObserverPropertyChanged())
                .Subscribe(spy);
            tom.AssertSubscribers(1);
            tom.Age = 50;
            user.Friends.Remove(tom);
            tom.AssertSubscribers(0);
            spy.AssertFired(1);
        }
    }
}
