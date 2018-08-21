using System.Configuration;
using System.Web;

namespace Justin.Updater.Server
{
    static class AppSettingHelper
    {
        public static string User
        {
            get
            {
                return ConfigurationManager.AppSettings["User"];
            }
        }

        public static string Password
        {
            get
            {
                return ConfigurationManager.AppSettings["Password"];
            }
        }
        
        public static string UpdateDir
        {
            get
            {
                var dir = ConfigurationManager.AppSettings["UpdateDir"];

                if(string.IsNullOrEmpty(dir))
                {
                    dir = HttpContext.Current.Server.MapPath("~/UpdateContainer");
                }

                return dir;
            }
        }
    }
}