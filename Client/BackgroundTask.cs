using System;
using System.Threading;

namespace Justin.Updater.Client
{
    abstract class BackgroundTask
    {
        public BackgroundTask(int interval)
            : this(interval, false)
        {
        }

        public BackgroundTask(int interval, bool restartOnError)
        {
            this.Interval = Math.Max(interval, _sleep);
            this._restartOnError = restartOnError;
        }

        public event Action<BackgroundTask> OnStart;
        public event Action<BackgroundTask> OnStop;

        private static readonly int defaultInterval = 5000;

        private readonly int _sleep = 1000;
        private readonly bool _restartOnError;

        protected int Interval { get; } = defaultInterval;
        public bool IsStarted { get; private set; } = false;

        public void Start()
        {
            StartMain();

            OnStart?.Invoke(this);
        }
        public void Stop()
        {
            IsStarted = false;
        }
        private void StartMain()
        {
            ThreadPool.QueueUserWorkItem(_ => {
                IsStarted = true;

                MainTask();
            });
        }
        private void MainTask()
        {
            try
            {
                OnStarting();
                
                var pre = DateTime.Now.AddMilliseconds(-Interval);

                while (IsStarted)
                {
                    if(DateTime.Now >= pre.AddMilliseconds(Interval))
                    {
                        Do();

                        pre = DateTime.Now;
                    }

                    Thread.Sleep(_sleep);
                }

                OnStopping();

                OnStop?.Invoke(this);
            }
            catch (Exception ex)
            {
                OnError(ex);

                // 遇到未处理异常自动重启
                if (IsStarted && _restartOnError)
                {
                    // 有可能的连续异常导致日志膨胀
                    Thread.Sleep(30 * 1000);

                    StartMain();
                }
            }
        }

        public abstract void Do();

        protected virtual void OnStarting() { }
        protected virtual void OnStopping() { }
        protected virtual void OnError(Exception ex)
        {

        }
    }
}
