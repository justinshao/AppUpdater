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
            this.interval = Math.Max(interval, _sleep);
            this.restartOnError = restartOnError;
        }

        public event Action<BackgroundTask> OnStart;
        public event Action<BackgroundTask> OnStop;

        private static int defaultInterval = 5000;

        private int _sleep = 1000;
        private bool _starting = false;
        private int interval = defaultInterval;
        private bool restartOnError;

        protected int Interval { get { return interval; } }
        public bool IsStarted { get { return _starting; } }
        public void Start()
        {
            StartMain();

            OnStart?.Invoke(this);
        }
        public void Stop()
        {
            _starting = false;
        }
        private void StartMain()
        {
            ThreadPool.QueueUserWorkItem(MainTask);

            _starting = true;
        }
        private void MainTask(object state)
        {
            try
            {
                Starting();
                
                var pre = DateTime.Now;

                while (_starting)
                {
                    if(DateTime.Now >= pre.AddMilliseconds(interval))
                    {
                        Do();

                        pre = DateTime.Now;
                    }

                    Thread.Sleep(_sleep);
                }

                Stopping();

                OnStop?.Invoke(this);
            }
            catch (Exception ex)
            {
                OnError(ex);

                // 遇到未处理异常自动重启
                if (_starting && restartOnError)
                {
                    // 有可能的连续异常导致日志膨胀
                    Thread.Sleep(30 * 1000);

                    StartMain();
                }
            }
        }

        public abstract void Do();

        protected virtual void Starting() { }
        protected virtual void Stopping() { }
        protected virtual void OnError(Exception ex)
        {

        }
    }
}
