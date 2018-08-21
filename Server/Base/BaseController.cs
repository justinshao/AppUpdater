using System.Web.Mvc;

namespace Justin.Updater.Server.Controllers
{
    /// <summary>
    /// 所有控制器都继承该类，方便后期扩展
    /// </summary>
    public abstract class BaseController : Controller
    {
        public LoginEmpInfo Oper { get { return SessionInfo.Emp; } }
    }
}