using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using NUnit.Framework;
using System.Collections.ObjectModel;

namespace SachaBarber.Reactice.Test
{
    public class User : INotifyPropertyChanged
    {
        public User()
        {
            Friends = new ObservableCollection<User>();
        }

        public User AddFriend(string friend)
        {
            var f = new User()
            {
                FirstName = friend
            };
            _Friends.Add(f);
            return f;
        }
        private ObservableCollection<User> _Friends;
        public ObservableCollection<User> Friends
        {
            get
            {
                return _Friends;
            }
            set
            {
                if (value != _Friends)
                {
                    _Friends = value;
                    if (_PropertyChanged != null)
                        _PropertyChanged(this, new PropertyChangedEventArgs("Friends"));
                }
            }
        }
        private int _Age;
        public int Age
        {
            get
            {
                return _Age;
            }
            set
            {
                if (value != _Age)
                {
                    _Age = value;
                    if (_PropertyChanged != null)
                        _PropertyChanged(this, new PropertyChangedEventArgs("Age"));
                }
            }
        }
        private string _LastName;
        public string LastName
        {
            get
            {
                return _LastName;
            }
            set
            {
                if (value != _LastName)
                {
                    _LastName = value;
                    if (_PropertyChanged != null)
                        _PropertyChanged(this, new PropertyChangedEventArgs("LastName"));
                }
            }
        }
        private string _FirstName;
        public string FirstName
        {
            get
            {
                return _FirstName;
            }
            set
            {
                if (value != _FirstName)
                {
                    _FirstName = value;
                    if (_PropertyChanged != null)
                        _PropertyChanged(this, new PropertyChangedEventArgs("FirstName"));
                }
            }
        }

        [DebuggerHidden]
        public void AssertSubscribers(int count)
        {
            Assert.AreEqual(count, _subs);
        }

        int _subs;

        #region INotifyPropertyChanged Members

        PropertyChangedEventHandler _PropertyChanged;
        public event PropertyChangedEventHandler PropertyChanged
        {
            add
            {
                _subs++;
                _PropertyChanged += value;
            }
            remove
            {
                _subs--;
                _PropertyChanged -= value;
            }
        }

        #endregion
    }
}
