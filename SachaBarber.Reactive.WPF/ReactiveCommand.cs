using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace SachaBarber.Reactive.WPF
{
    public class ReactiveCommand : ICommand, IReactiveCommand, IDisposable
    {
        private Subject<object> commandExecutedSubject = new Subject<object>();
        private Subject<Exception> commandExeceptionsSubjectStream = new Subject<Exception>();

        private IObservable<bool> canExecuteObsPred;
        private bool canExecuteLatest = true;
        private CompositeDisposable disposables = new CompositeDisposable();

        public ReactiveCommand(IObservable<bool> predicate, bool initialCondition)
        {


            if (predicate != null)
            {
                this.canExecuteObsPred = predicate;
                SetupSubscriptions();
            }
            RaiseCanExecute(initialCondition);
        }


        private void RaiseCanExecute(bool value)
        {
            canExecuteLatest = value;
            this.raiseCanExecuteChanged(EventArgs.Empty);
        }


        public ReactiveCommand()
        {
            RaiseCanExecute(true);
        }


        private void SetupSubscriptions()
        {

            disposables = new CompositeDisposable();
            disposables.Add(this.canExecuteObsPred.Subscribe(
                //OnNext
                x =>
                {
                    canExecuteLatest = x;
                    Scheduler.Immediate.Schedule(() => this.raiseCanExecuteChanged(EventArgs.Empty));
                },
                //onError
                commandExeceptionsSubjectStream.OnNext
            ));
        }



        public void AddPredicate(IObservable<bool> predicate)
        {
            canExecuteObsPred = this.canExecuteObsPred.CombineLatest(predicate, (a, b) => a && b).DistinctUntilChanged();
            SetupSubscriptions();

            //this fires when the HasStuff 2nd predicate changes value, but the overall CombinLatest doesn't fire
            predicate.Subscribe(x =>
            {
                bool hasStuff = x;
            });

        }

        bool ICommand.CanExecute(object parameter)
        {
            return canExecuteLatest;
        }

        public event EventHandler CanExecuteChanged;

        public void Execute(object parameter)
        {
            commandExecutedSubject.OnNext(parameter);
        }




        public IObservable<object> CommandExecutedStream
        {
            get { return this.commandExecutedSubject.AsObservable(); }
        }

        public IObservable<Exception> CommandExeceptionsStream
        {
            get { return this.commandExeceptionsSubjectStream.AsObservable(); }
        }


        protected virtual void raiseCanExecuteChanged(EventArgs e)
        {
            var handler = this.CanExecuteChanged;

            if (handler != null)
            {
                handler(this, e);
            }
        }

        public void Dispose()
        {
            disposables.Dispose();
        }
    }
}
