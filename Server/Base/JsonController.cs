using Newtonsoft.Json;
using System;
using System.Text;
using System.Web;
using System.Web.Mvc;

namespace Justin.Updater.Server.Controllers
{
    public abstract class JsonController : BaseController
    {
        #region JsonResult
        [NonAction]
        protected ActionResult Success()
        {
            return Success(null);
        }
        [NonAction]
        protected ActionResult Success(string message)
        {
            return Success(message, null);
        }
        [NonAction]
        protected ActionResult Success(object data)
        {
            return Success(null, data);
        }
        [NonAction]
        protected ActionResult Success(string message, object data)
        {
            return Json(new
            {
                Ok = true,
                Message = message,
                Data = data
            }, JsonRequestBehavior.AllowGet);
        }
        [NonAction]
        protected ActionResult Error()
        {
            return Error(null);
        }
        [NonAction]
        protected ActionResult Error(string message)
        {
            return Error(message, null);
        }
        [NonAction]
        protected ActionResult Error(object data)
        {
            return Error(null, data);
        }
        [NonAction]
        protected ActionResult Error(string message, object data)
        {
            return Json(new
            {
                Ok = false,
                Message = message,
                Data = data
            }, JsonRequestBehavior.AllowGet);
        }

        protected override JsonResult Json(object data, string contentType, Encoding contentEncoding, JsonRequestBehavior behavior)
        {
            return new BetterJsonResult(data)
            {
                ContentType = contentType,
                ContentEncoding = contentEncoding,
                JsonRequestBehavior = behavior,
            };
        }
        #endregion
    }

    class BetterJsonResult : JsonResult
    {
        public BetterJsonResult() { JsonRequestBehavior = JsonRequestBehavior.DenyGet; }
        public BetterJsonResult(object data) : this() { Data = data; }

        public override void ExecuteResult(ControllerContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException("context");
            }
            if ((this.JsonRequestBehavior == JsonRequestBehavior.DenyGet) && string.Equals(context.HttpContext.Request.HttpMethod, "GET", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("默认情况下Json返回值不允许Get类型的请求，请将 JsonRequestBehavior 设置为 JsonRequestBehavior.AllowGet。");
            }
            HttpResponseBase response = context.HttpContext.Response;
            if (!string.IsNullOrEmpty(this.ContentType))
            {
                response.ContentType = this.ContentType;
            }
            else
            {
                response.ContentType = "application/json";
            }
            if (this.ContentEncoding != null)
            {
                response.ContentEncoding = this.ContentEncoding;
            }
            if (this.Data != null)
            {
                response.Write(JsonConvert.SerializeObject(Data));
            }
        }
    }
}