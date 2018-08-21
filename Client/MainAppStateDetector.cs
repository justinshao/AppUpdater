using Justin.Serialization.Json;
using System;
using System.IO;

namespace Justin.Updater.Client
{
    class MainAppStateDetector : BackgroundTask
    {
        private readonly LocalRunInfo localRunInfo;
        private readonly string mainAppPath;
        private UpdateUrlInfo updateUrlInfo;
        private readonly string clientId;
        private Config config;

        public MainAppStateDetector(string mainAppPath, LocalRunInfo localRunInfo, Config config)
            : base(config.PingInterval * 1000, true)
        {
            this.localRunInfo = localRunInfo;
            this.mainAppPath = mainAppPath;
            this.updateUrlInfo = UpdateUrlInfo.Parse(localRunInfo.UpdateUrl);
            this.clientId = Util.GetClientId(localRunInfo.ClientId);
            this.config = config;
        }

        public event Action<MainAppStateDetector, LocalRunInfo> OnMainAppsClose;
        public event Action<ClientCommand, LocalRunInfo> OnCommandRequest;

        public override void Do()
        {
            var active = UpdateHelper.IsAppRunning(mainAppPath);

            if(config.KeepUpdaterRunning)
            {
                ClientCommand cmd = Ping(active);
                if(cmd != null)
                {
                    OnCommandRequest?.Invoke(cmd, localRunInfo);
                }
            }

            if (!active)
            {
                if(IsStarted)
                {
                    OnMainAppsClose?.Invoke(this, localRunInfo);
                }
            }
        }

        private ClientCommand Ping(bool active)
        {
            var url = $"{updateUrlInfo.Host}/api/Ping/{updateUrlInfo.SystemId}?clientId={clientId}&active={active}";

            try
            {
                using (var resp = Util.CreateHttpRequest(url).GetResponse())
                {
                    using (StreamReader reader = new StreamReader(resp.GetResponseStream()))
                    {
                        var cmd = reader.ReadToEnd() ?? string.Empty;

                        return new JavaScriptSerializer().Deserialize<ClientCommand>(cmd);
                    }
                }
            }
            catch
            {
                return null;
            }
        }

        protected override void OnError(Exception ex)
        {
            LogHelper.LogError("检测主程序运行状态出错", ex);
        }
    }
}
