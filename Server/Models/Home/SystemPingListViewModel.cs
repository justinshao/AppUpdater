using Justin.Updater.Server.Util;
using System.Collections.Generic;

namespace Justin.Updater.Server.Models.Home
{
    public class SystemPingListViewModel
    {
        public System System { get; set; }
        public Config Config { get; set; }
        public IEnumerable<ClientApp> ClientApps { get; set; }
        public string ClientName { get; set; }
    }
}