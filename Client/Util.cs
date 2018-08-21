using System;
using System.Management;
using System.Net;

namespace Justin.Updater.Client
{
    class Util
    {
        public static HttpWebRequest CreateHttpRequest(string url)
        {
            var req = (HttpWebRequest)WebRequest.Create(url);

            req.Proxy = null;
            req.KeepAlive = false;

            return req;
        }

        public static string GetClientId(string fallback = null)
        {
            var mac = GetMacAddr();

            if(mac == null)
            {
                return fallback;
            }
            else
            {
                if(string.IsNullOrEmpty(fallback))
                {
                    return mac;
                }
                else
                {
                    return $"{fallback}（{mac}）";
                }
            }
        }

        private static string GetMacAddr()
        {
            try
            {
                string madAddr = null;

                foreach (ManagementObject mo in new ManagementClass("Win32_NetworkAdapterConfiguration").GetInstances())
                {
                    using (var _mo = mo)
                    {
                        if (Convert.ToBoolean(mo["IPEnabled"]) == true)
                        {
                            madAddr = mo["MacAddress"].ToString();
                            madAddr = madAddr.Replace(':', '-');
                        }
                    }
                }

                return madAddr;
            }
            catch
            {
                return null;
            }
        }
    }
}
