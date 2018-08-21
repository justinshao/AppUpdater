using System.Web.Mvc;
using System.Web.Routing;

namespace Justin.Updater.Server
{
    public class RouteConfig
    {
        public static void RegisterRoutes(RouteCollection routes)
        {
            routes.IgnoreRoute("{resource}.axd/{*pathInfo}");
            
            routes.MapRoute(
                name: "Login",
                url: "login",
                defaults: new { controller = "Home", action = "Login", }
            );

            routes.MapRoute(
                name: "Api",
                url: "api/{action}/{id}",
                defaults: new { controller = "Api", id = UrlParameter.Optional, }
            );

            routes.MapRoute(
                name: "Home",
                url: "{action}/{id}",
                defaults: new { controller = "Home", id = UrlParameter.Optional, }
            );
            
            routes.MapRoute(
                name: "Default",
                url: "{controller}/{action}/{id}",
                defaults: new { controller = "Home", action = "SystemList", id = UrlParameter.Optional, }
            );
        }
    }
}
