
using System;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;

namespace Justin.Updater.Server
{
    public class Global : HttpApplication
    {
        protected void Application_Start(object sender, EventArgs e)
        {
            // 路由注册
            //AreaRegistration.RegisterAllAreas();
            RouteConfig.RegisterRoutes(RouteTable.Routes);

            // 去除一些暂时不必要的ValueProvider
            ValueProviderFactories.Factories.Clear();
            ValueProviderFactories.Factories.Add(new QueryStringValueProviderFactory());
            ValueProviderFactories.Factories.Add(new FormValueProviderFactory());
            ValueProviderFactories.Factories.Add(new RouteDataValueProviderFactory());
            ValueProviderFactories.Factories.Add(new JsonValueProviderFactory());
            ValueProviderFactories.Factories.Add(new HttpFileCollectionValueProviderFactory());
            //ValueProviderFactories.Factories.Add(new ChildActionValueProviderFactory());

            // 试图引擎只查找.cshtml文件
            ViewEngines.Engines.Clear();
            //ViewEngines.Engines.Add(new WebFormViewEngine());
            //ViewEngines.Engines.Add(new RazorViewEngine());
            ViewEngines.Engines.Add(new RazorViewEngine()
            {
                //AreaViewLocationFormats = new string[] { "~/Areas/{2}/Views/{1}/{0}.cshtml", "~/Areas/{2}/Views/Shared/{0}.cshtml" },
                //AreaMasterLocationFormats = new string[] { "~/Areas/{2}/Views/{1}/{0}.cshtml", "~/Areas/{2}/Views/Shared/{0}.cshtml" },
                //AreaPartialViewLocationFormats = new string[] { "~/Areas/{2}/Views/{1}/{0}.cshtml", "~/Areas/{2}/Views/Shared/{0}.cshtml" },
                ViewLocationFormats = new string[] { "~/Views/{1}/{0}.cshtml", "~/Views/Shared/{0}.cshtml" },
                MasterLocationFormats = new string[] { "~/Views/{1}/{0}.cshtml", "~/Views/Shared/{0}.cshtml" },
                PartialViewLocationFormats = new string[] { "~/Views/{1}/{0}.cshtml", "~/Views/Shared/{0}.cshtml" },
                FileExtensions = new string[] { "cshtml" },
            });
        }

        protected void Application_Error(object sender, EventArgs e)
        {
            LogHelper.LogError("服务器内部错误", Server.GetLastError().GetBaseException());
        }
    }
}