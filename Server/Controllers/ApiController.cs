using Justin.Updater.Server.Util;
using Justin.Updater.Shared;
using System;
using System.IO;
using System.Web;
using System.Web.Mvc;

namespace Justin.Updater.Server.Controllers
{
    public class ApiController : JsonController
    {
        private static readonly object s_opLock = new object();

        #region 服务端更新包相关接口
        /// <summary>
        /// 上传更新包
        /// </summary>
        /// <param name="id"></param>
        /// <param name="file"></param>
        /// <returns></returns>
        [HttpPost]
        [LoginRequired]
        public ActionResult UploadPkg(int id, HttpPostedFileBase file)
        {
            if (file == null)
                return Error("未指定任何文件");
            if (string.IsNullOrEmpty(file.FileName))
                return Error("更新包名为空");
            
            // 有的浏览器会将整个文件路径作为文件名
            var idx = file.FileName.LastIndexOf(Path.DirectorySeparatorChar);
            var pkgName = idx >= 0 ? file.FileName.Substring(idx + 1) : file.FileName;
            
            if (pkgName.StartsWith("$"))
                return Error("文件名不能以 $ 开头");
            if(pkgName.EndsWith(SysUpdateHelper.InstalledPkgExt))
                return Error($"文件名不能以 {SysUpdateHelper.InstalledPkgExt} 结尾");
            if (!pkgName.EndsWith(".zip"))
                return Error("文件名必须以 .zip 结尾");
            
            try
            {
                if (!TryLockOp(id))
                    return Error("当前有其他操作未完成，等待完成后再继续");

                DateTime start = DateTime.Now;

                SysUpdateHelper.SaveUploadPkg(id, pkgName, file.InputStream);

                var span = DateTime.Now - start;
                LogHelper.LogInfo($"更新包 system{id}/{pkgName} 上传成功。耗时： {span.Hours}小时{span.Minutes}分钟{span.Seconds}秒");

                return Success();
            }
            catch (Exception ex)
            {
                LogHelper.LogError($"更新包 system{id}/{pkgName} 上传失败", ex);

                return Error(ex.Message);
            }
            finally
            {
                UnLockOp(id);
            }
        }
        
        /// <summary>
        /// 安装更新包
        /// </summary>
        /// <param name="id"></param>
        /// <param name="pkgName"></param>
        /// <returns></returns>
        [HttpPost]
        [LoginRequired]
        public ActionResult InstallPkg(int id, int pkgId)
        {
            string pkgName = null;

            try
            {
                if (SysUpdateHelper.IsPkgInstalled(id, pkgId))
                    return Error("已安装");

                if (!TryLockOp(id))
                    return Error("当前有其他操作未完成，等待完成后再继续");

                DateTime start = DateTime.Now;
                pkgName = SysUpdateHelper.GetPkgRawName(id, pkgId);

                SysUpdateHelper.InstallPkg(id, pkgId);

                var span = DateTime.Now - start;
                LogHelper.LogInfo($"更新包 system{id}/{pkgName} 安装成功。耗时： {span.Hours}小时{span.Minutes}分钟{span.Seconds}秒");

                SysUpdateHelper.TouchDetect(id);

                return Success();
            }
            catch (Exception ex)
            {
                LogHelper.LogError($"更新包 system{id}/{pkgName} 安装失败", ex);

                return Error(ex.Message);
            }
            finally
            {
                UnLockOp(id);
            }
        }

        /// <summary>
        /// 还原更新包
        /// </summary>
        /// <param name="id"></param>
        /// <param name="pkgId"></param>
        /// <returns></returns>
        [HttpPost]
        [LoginRequired]
        public ActionResult RestorePkg(int id, int pkgId)
        {
            if (!SysUpdateHelper.IsPkgInstalled(id, pkgId))
                return Error("未安装该更新包");

            if (!SysUpdateHelper.IsLatestInstalledPkg(id, pkgId))
                return Error("不能跳过最近安装的更新包，如想还原该更新包，请依次还原最近的更新包");

            string pkgName = null;
            try
            {
                if (!TryLockOp(id))
                    return Error("当前有其他操作未完成，等待完成后再继续");

                DateTime start = DateTime.Now;
                pkgName = SysUpdateHelper.GetPkgRawName(id, pkgId);

                SysUpdateHelper.RestorePkg(id, pkgId);

                var span = DateTime.Now - start;
                LogHelper.LogInfo($"更新包 system{id}/{pkgName} 还原成功。耗时： {span.Hours}小时{span.Minutes}分钟{span.Seconds}秒");

                SysUpdateHelper.TouchDetect(id);

                return Success();
            }
            catch (Exception ex)
            {
                LogHelper.LogError($"更新包 system{id}/{pkgName} 还原失败", ex);

                return Error(ex.Message);
            }
            finally
            {
                UnLockOp(id);
            }
        }

        /// <summary>
        /// 删除更新包
        /// </summary>
        /// <param name="id"></param>
        /// <param name="pkgId"></param>
        /// <returns></returns>
        [HttpPost]
        [LoginRequired]
        public ActionResult DeletePkg(int id, int pkgId)
        {
            string pkgName = null;
            try
            {
                if (SysUpdateHelper.IsPkgInstalled(id, pkgId))
                    return Error("已安装的更新包不允许删除");

                if (!TryLockOp(id))
                    return Error("当前有其他操作未完成，等待完成后再继续");

                pkgName = SysUpdateHelper.GetPkgRawName(id, pkgId);

                SysUpdateHelper.DeletePkg(id, pkgId);

                LogHelper.LogInfo($"更新包 system{id}/{pkgName} 删除成功");

                return Success();
            }
            catch (Exception ex)
            {
                LogHelper.LogError($"更新包 system{id}/{pkgName} 删除失败", ex);

                return Error(ex.Message);
            }
            finally
            {
                UnLockOp(id);
            }
        }

        /// <summary>
        /// 手动更新系统运行信息
        /// </summary>
        /// <param name="systemId"></param>
        /// <returns></returns>
        [HttpPost]
        [LoginRequired]
        public ActionResult UpdateRunInfo(int id)
        {
            try
            {
                if (!TryLockOp(id))
                    return Error("当前有其他操作未完成，等待完成后再继续");

                SysUpdateHelper.GenerateSystemJsonFile(id);

                LogHelper.LogInfo($"更新系统运行信息 system{id} 成功");

                return Success();
            }
            catch (Exception ex)
            {
                LogHelper.LogError($"更新系统运行信息 system{id} 失败", ex);

                return Error(ex.Message);
            }
            finally
            {
                UnLockOp(id);
            }
        }
        
        /// <summary>
        /// 同一时刻只允许对同一个系统做一个操作
        /// </summary>
        /// <param name="systemId"></param>
        /// <returns></returns>
        private bool TryLockOp(int systemId)
        {
            lock (s_opLock)
            {
                if (!SysUpdateHelper.IsSystemLocked(systemId))
                {
                    SysUpdateHelper.LockSystemForOp(systemId);

                    return true;
                }

                return false;
            }
        }
        /// <summary>
        /// 解锁，允许后续操作
        /// </summary>
        /// <param name="systemId"></param>
        private void UnLockOp(int systemId)
        {
            lock (s_opLock)
            {
                SysUpdateHelper.UnLockSystemForOp(systemId);
            }
        }
        #endregion

        #region 客户端更新相关接口
        /// <summary>
        /// 客户端检测更新的地址
        /// </summary>
        /// <param name="id"></param>
        /// <param name="clientId"></param>
        /// <param name="msg">附加消息</param>
        /// <returns></returns>
        public ActionResult RunInfo(int id, string clientId)
        {
            clientId = clientId ?? Request.ServerVariables["REMOTE_ADDR"];

            var file = SysUpdateHelper.GetSystemRunInfoFile(id);
            if(!System.IO.File.Exists(file))
            {
                return Json(SysUpdateHelper.EmptyRunInfo, JsonRequestBehavior.AllowGet);
            }
            else
            {
                return File(file, "application/octet-stream");
            }
        }

        /// <summary>
        /// 系统运行文件
        /// </summary>
        /// <param name="id">系统id</param>
        /// <param name="path">文件相对路径</param>
        /// <returns></returns>
        public ActionResult RunFile(int id, string path)
        {
            // 主程序如果是x64平台，运行环境及更新程序都是x64的，不用做特殊处理
            // 主程序如果是x86平台，那么更新程序可以是x86和x64，此时就需要利用更新程序的目标平台下载对应的文件，默认x86，对应的x64版本文件名需要以.$x64结尾
            // 更新的时候，如果更新程序同时存在两个目标平台，那么需要上传1个文件的2个版本

            var isX64 = ("x64" == Request.Headers["ProcessPlatform"]);
            var runFile = SysUpdateHelper.GetSystemRunFile(id, path, isX64);

            if (!System.IO.File.Exists(runFile))
                return NotFound();

            return File(runFile, "application/octet-stream");
        }

        /// <summary>
        /// 系统当前版本
        /// </summary>
        /// <param name="id">系统id</param>
        /// <returns></returns>
        public ActionResult DetectVer(int id)
        {
            var ver = SysUpdateHelper.UpdateDetectVer(id);

            return Content(ver ?? string.Empty, "text/plain");
        }
        
        /// <summary>
        /// 取系统配置
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet]
        public ActionResult GetSystemConfig(int id)
        {
            var config = SysUpdateHelper.GetSystemConfig(id);

            return Json(config, JsonRequestBehavior.AllowGet);
        }

        /// <summary>
        /// 客户端日志
        /// </summary>
        /// <param name="clientId"></param>
        /// <param name="info"></param>
        /// <returns></returns>
        [HttpGet]
        public ActionResult LogInfo(int id, string clientId, string info)
        {
            clientId = string.IsNullOrEmpty(clientId) ? (Request.ServerVariables["REMOTE_ADDR"] ?? "unknown") : clientId;

            LogHelper.LogClientInfo(id, clientId, info);

            return Success();
        }

        /// <summary>
        /// 客户端日志
        /// </summary>
        /// <param name="clientId"></param>
        /// <param name="error"></param>
        /// <returns></returns>
        [HttpGet]
        public ActionResult LogError(int id, string clientId, string error)
        {
            clientId = string.IsNullOrEmpty(clientId) ? (Request.ServerVariables["REMOTE_ADDR"] ?? "unknown") : clientId;

            LogHelper.LogClientError(id, clientId, error);

            return Success();
        }

        /// <summary>
        /// 更新客户端ping
        /// </summary>
        /// <param name="id"></param>
        /// <param name="clientId"></param>
        /// <param name="active"></param>
        /// <returns></returns>
        [HttpGet]
        public ActionResult Ping(int id, string clientId, bool active)
        {
            if(active)
            {
                SystemUpdaterCollection.Active(id, clientId);
            }
            else
            {
                var config = SysUpdateHelper.GetSystemConfig(id);

                SystemUpdaterCollection.Inactive(id, clientId, config.PingInterval);
            }

            var file = SysUpdateHelper.GetClientCommandFile(id, clientId);
            if (!System.IO.File.Exists(file))
            {
                return Content(string.Empty);
            }
            else
            {
                var cmd = System.IO.File.ReadAllText(file);

                IOHelper.DeleteFile(file);

                return Content(cmd, "application/octet-stream");
            }
        }

        /// <summary>
        /// 清除Ping list
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult ClearPing(int id)
        {
            try
            {
                SystemUpdaterCollection.Clear(id);

                return Success();
            }
            catch (Exception ex)
            {
                return Error(ex.Message);
            }
        }

        /// <summary>
        /// 发送命令
        /// </summary>
        /// <param name="id"></param>
        /// <param name="clientId"></param>
        /// <param name="command"></param>
        /// <returns></returns>
        [HttpPost]
        [LoginRequired]
        public ActionResult SendCommand(int id, string clientId, ClientCommand command)
        {
            try
            {
                SysUpdateHelper.SetCommand(id, clientId, command);

                return Success();
            }
            catch (Exception ex)
            {
                return Error(ex.Message);
            }
        }

        /// <summary>
        /// 清除命令
        /// </summary>
        /// <param name="id"></param>
        /// <param name="clientId"></param>
        /// <returns></returns>
        [HttpPost]
        [LoginRequired]
        public ActionResult ClearCommand(int id, string clientId)
        {
            SysUpdateHelper.ClearCommand(id, clientId);

            return Success();
        }
        #endregion
    }
}