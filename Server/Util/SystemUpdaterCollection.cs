using Justin.Updater.Shared;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Justin.Updater.Server.Util
{
    internal class SystemUpdaterCollection
    {
        private static ConcurrentDictionary<int, ConcurrentDictionary<string, ClientApp>> systemUpdaters
            = new ConcurrentDictionary<int, ConcurrentDictionary<string, ClientApp>>();

        public static void Active(int systemId, string clientId)
        {
            if(TryGetClient(systemId, clientId, out ClientApp client))
            {
                client.LastActive = DateTime.Now;
            }
            else
            {
                AddClient(systemId, clientId, new ClientApp { ClientId = clientId, LastActive = DateTime.Now });
            }
        }

        public static void Inactive(int systemId, string clientId, int overtime)
        {
            if (TryGetClient(systemId, clientId, out ClientApp client))
            {

            }
            else
            {
                AddClient(systemId, clientId, new ClientApp { ClientId = clientId, LastActive = DateTime.Now.AddSeconds(-overtime) });
            }
        }

        public static void Clear(int systemId)
        {
            if(systemUpdaters.TryGetValue(systemId, out ConcurrentDictionary<string, ClientApp> apps))
            {
                apps.Clear();
            }
        }

        public static IEnumerable<ClientApp> GetClientApps(int systemId)
        {
            if(systemUpdaters.TryGetValue(systemId, out ConcurrentDictionary<string, ClientApp> updaters))
            {
                foreach (var u in updaters)
                {
                    yield return u.Value;
                }
            }
        }

        private static bool TryGetClient(int systemId, string clientId, out ClientApp client)
        {
            client = null;

            if (systemUpdaters.TryGetValue(systemId, out ConcurrentDictionary<string, ClientApp> updaters))
            {
                return updaters.TryGetValue(clientId, out client);
            }

            return false;
        }

        private static void AddClient(int systemId, string clientId, ClientApp client)
        {
            if (systemUpdaters.TryGetValue(systemId, out ConcurrentDictionary<string, ClientApp> updaters))
            {
                updaters[clientId] = client;
            }
            else
            {
                systemUpdaters[systemId] = new ConcurrentDictionary<string, ClientApp>()
                { [clientId] = client };
            }
        }
    }

    public class ClientApp
    {
        public string ClientId { get; set; }
        public DateTime LastActive { get; set; }
        public ClientCommand Command { get; set; }
    }
}