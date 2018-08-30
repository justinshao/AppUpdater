using System;
using System.IO;

namespace Justin.Updater.Client
{
    class UpdateDetector : BackgroundTask
    {
        private UpdateUrlInfo updateUrlInfo;
        private string currVer;
        private DateTime promptTime = DateTime.MinValue;

        public UpdateDetector(int interval, UpdateUrlInfo updateUrlInfo)
            : base(interval, true)
        {
            this.updateUrlInfo = updateUrlInfo;
        }
        
        public event Action<UpdateDetector> OnNotifyUpdate;
        
        private string GetCurrentVer()
        {
            string url = $"{updateUrlInfo.Host}/api/DetectVer/{updateUrlInfo.SystemId}";

            return Util.GetHttpResponseString(url) ?? string.Empty;
        }

        public void DelayPrompt(int delay)
        {
            this.promptTime = DateTime.Now.AddMilliseconds(delay);
        }

        public override void Do()
        {
            var newVer = GetCurrentVer();

            if (!string.IsNullOrEmpty(newVer) && !newVer.Equals(currVer))
            {
                currVer = newVer;

                if (IsStarted)
                {
                    OnNotifyUpdate?.Invoke(this);
                }
            }
            else if(promptTime != DateTime.MinValue && DateTime.Now > promptTime)
            {
                promptTime = DateTime.MinValue;

                if (IsStarted)
                {
                    OnNotifyUpdate?.Invoke(this);
                }
            }
        }

        protected override void OnStarting()
        {
            currVer = GetCurrentVer();
        }
        protected override void OnError(Exception ex)
        {
            LogHelper.LogError("更新检测出错", ex);
        }
        
    }
}
