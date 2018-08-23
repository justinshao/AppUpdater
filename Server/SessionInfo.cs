using System;
using System.Web;

namespace Justin.Updater.Server
{
    /// <summary>
    /// 会话信息
    /// </summary>
    public static class SessionInfo
    {
        private const string _SessionKey_Emp = "emp";

        /// <summary>
        /// 登录人员
        /// </summary>
        public static LoginEmpInfo Emp
        {
            get { return HttpContext.Current.Session[_SessionKey_Emp] as LoginEmpInfo; }
            set { HttpContext.Current.Session[_SessionKey_Emp] = value; }
        }
    }

    [Serializable]
    public class LoginEmpInfo
    {
        public int Id { get; set; }
        public string Code { get; set; }
        public string Name { get; set; }
        public string Password { get; set; }
    }
}
