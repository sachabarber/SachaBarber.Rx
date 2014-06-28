using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using NUnit.Framework;

namespace SachaBarber.Reactice.Test
{
    public class SpyObserver<T> : IObserver<T>
    {

        #region IObserver<ItemPropertyChangedEvent<User>> Members

        public void OnCompleted()
        {

        }

        public void OnError(Exception error)
        {

        }

        int _Fired;
        public void OnNext(T value)
        {
            LastEvent = value;
            _Fired++;
        }
        public void Reset()
        {
            _Fired = 0;
            LastEvent = default(T);
        }

        [DebuggerHidden]
        public void AssertFired(int count)
        {
            Assert.IsTrue(_Fired == count, "Should fired " + count + " times");
        }
        public T LastEvent
        {
            get;
            set;
        }

        #endregion
    }
}
