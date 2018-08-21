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
            string url = string.Format("{0}/api/DetectVer/{1}",
                            updateUrlInfo.Host, updateUrlInfo.SystemId);

            using (var resp = Util.CreateHttpRequest(url).GetResponse())
            {
                using (StreamReader reader = new StreamReader(resp.GetResponseStream()))
                {
                    return reader.ReadToEnd() ?? string.Empty;
                }
            }
        }

        public void EnablePrompt(int timeout)
        {
            this.promptTime = DateTime.Now.AddMilliseconds(timeout);
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

        protected override void Starting()
        {
            currVer = GetCurrentVer();
        }
        protected override void OnError(Exception ex)
        {
            LogHelper.LogError("更新检测出错", ex);
        }
        
    }
}
