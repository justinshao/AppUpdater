using System.Web.Mvc;
using System.Web.Routing;

namespace Justin.Updater.Server
{
    public class LoginRequiredAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            if (filterContext.IsChildAction) return; // 忽略子action的验证，不必要。因为所有的验证在父action已经做过处理

            if (SessionInfo.Emp == null)
            {
                var req = filterContext.HttpContext.Request;
                if (req.IsAjaxRequest()) // 对于ajax请求返回json格式的消息
                {
                    filterContext.Result = new JsonResult
                    {
                        Data = new {
                            Ok = false,
                            Message = "未登录"
                        },
                        ContentType = null,
                        ContentEncoding = null,
                        JsonRequestBehavior = JsonRequestBehavior.AllowGet
                    };
                }
                else
                {
                    filterContext.Result = new RedirectToRouteResult("Login", new RouteValueDictionary(new { from = req.RawUrl }));
                }
            }
        }
    }
}
