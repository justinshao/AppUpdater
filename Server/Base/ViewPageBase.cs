using System.Web.Mvc;

namespace Justin.Updater.Server.Views
{
    /// <summary>
    /// 所有View都集成此基类，方便后期可能的扩展
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class ViewPageBase<T> : WebViewPage<T>
    {
    }
}
