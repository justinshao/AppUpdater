using Justin.Updater.Server.Models.Home;
using Justin.Updater.Server.Util;
using System;
using System.IO;
using System.Linq;
using System.Web.Mvc;

namespace Justin.Updater.Server.Controllers
{
    public class HomeController : BaseController
    {
        [HttpGet]
        [LoginRequired]
        public ActionResult Index()
        {
            return RedirectToAction(nameof(SystemList));
        }

        [HttpGet]
        [LoginRequired]
        public ActionResult SystemList()
        {
            var model = new SystemListViewModel
            {
                Systems = SysUpdateHelper.GetSystems(),
            };

            return View(model);
        }

        [HttpGet]
        [LoginRequired]
        public ActionResult System(int id, int p = 1)
        {
            var system = SysUpdateHelper.GetSystem(id);

            if(system == null)
            {
                return NotFound();
            }

            var perPage = 10;
            var pageResult = SysUpdateHelper.GetSystemPkgs(id, p, perPage);

            var model = new SystemViewModel
            {
                System = system,
                Packages = pageResult.Item1,
                TotalPage = (int)Math.Ceiling((pageResult.Item2 * 1.0 / perPage)),
                CurrPage = p,
            };

            return View(model);
        }

        [HttpGet]
        [LoginRequired]
        public ActionResult Config(int id)
        {
            var system = SysUpdateHelper.GetSystem(id);

            if(system == null)
            {
                return NotFound();
            }

            var config = SysUpdateHelper.GetSystemConfig(id);
            
            ViewBag.Title = $"系统更新设置-{system.Name}";
            ViewBag.SystemId = id;

            return View(config);
        }

        [HttpPost]
        [LoginRequired]
        public ActionResult Config(int id, Config config)
        {
            SysUpdateHelper.UpdateDetect(id, enabled: config.DetectEnabled);
            SysUpdateHelper.SaveSystemConfig(id, config);

            return RedirectToAction(nameof(SystemList));
        }

        [HttpGet]
        [LoginRequired]
        public ActionResult Log(int id, int p = 1, DateTime? date = null)
        {
            var system = SysUpdateHelper.GetSystem(id);

            if(system == null)
            {
                return NotFound();
            }

            if(date == null)
            {
                date = DateTime.Now;
            }

            var perPage = 50;
            var pageResult = SysUpdateHelper.GetSystemLogFiles(id, p, perPage, date.Value);

            var model = new SystemLogViewModel
            {
                System = system,
                Date = date.Value,
                LogFiles = pageResult.Item1,
                TotalPage = (int)Math.Ceiling((pageResult.Item2 * 1.0 / perPage)),
                CurrPage = p,
            };

            return View(model);
        }

        /// <summary>
        /// 删除日志
        /// </summary>
        /// <param name="id"></param>
        /// <param name="clientId"></param>
        /// <returns></returns>
        [HttpGet]
        [LoginRequired]
        public ActionResult DeleteLog(int id, string fileName, DateTime? date)
        {
            var file = SysUpdateHelper.GetSystemLogFile(id, date ?? DateTime.Now, fileName);

            if(file != null && file.Exists)
            {
                file.Delete();
            }

            return RedirectToAction(nameof(Log), new { id, date });
        }

        /// <summary>
        /// 删除所有客户端日志
        /// </summary>
        /// <param name="id"></param>
        /// <param name="date"></param>
        /// <returns></returns>
        [HttpGet]
        [LoginRequired]
        public ActionResult DeleteAllLog(int id, DateTime? date)
        {
            var dir = LogHelper.GetSystemClientLogDir(id, date ?? DateTime.Now);

            if(Directory.Exists(dir))
            {
                Directory.Delete(dir, true);
            }

            return RedirectToAction(nameof(Log), new { id, date });
        }

        [HttpGet]
        [LoginRequired]
        public ActionResult LogContent(int id, string fileName, DateTime? date)
        {
            var system = SysUpdateHelper.GetSystem(id);

            if (system == null)
            {
                return NotFound();
            }

            var file = SysUpdateHelper.GetSystemLogFile(id, date ?? DateTime.Now, fileName);
            
            if(file == null)
            {
                return NotFound();
            }

            using (var reader = file.OpenText())
            {
                ViewBag.LogContent = reader.ReadToEnd();

                return View();
            }
        }

        [HttpGet]
        [LoginRequired]
        public ActionResult DownloadLog(int id, string fileName, DateTime? date)
        {
            var system = SysUpdateHelper.GetSystem(id);

            if (system == null)
            {
                return NotFound();
            }

            var file = SysUpdateHelper.GetSystemLogFile(id, date ?? DateTime.Now, fileName);

            if (file == null)
            {
                return NotFound();
            }

            return File(file.FullName, "text/plain", file.Name);
        }

        [HttpGet]
        [LoginRequired]
        public ActionResult PingList(int id, string clientName, int p = 1, DateTime? date = null)
        {
            var system = SysUpdateHelper.GetSystem(id);

            if (system == null)
            {
                return NotFound();
            }

            var now = DateTime.Now;

            var config = SysUpdateHelper.GetSystemConfig(id);
            var clientApps = SystemUpdaterCollection.GetClientApps(id)
                .Where(c => string.IsNullOrEmpty(clientName) || c.ClientId.Contains(clientName));

            var model = new SystemPingListViewModel
            {
                ClientApps = clientApps,
                Config = config,
                System = system,
                ClientName = clientName
            };

            return View(model);
        }

        [HttpGet]
        [LoginRequired]
        public ActionResult Edit(int id)
        {
            var system = SysUpdateHelper.GetSystem(id);

            if(system == null)
            {
                return NotFound();
            }

            var model = new SystemEditViewModel
            {
                Mode = 2,
                SystemId = id,
                Name = system.Name,
            };

            return View("SystemEdit", model);
        }

        [HttpPost]
        [LoginRequired]
        public ActionResult Edit(int id, string name)
        {
            SysUpdateHelper.SaveSystem(id, name, Oper.Id);

            return RedirectToAction(nameof(SystemList));
        }

        [HttpGet]
        [LoginRequired]
        public ActionResult New()
        {
            var model = new SystemEditViewModel
            {
                Mode = 1,
                SystemId = 0,
                Name = string.Empty,
            };

            return View("SystemEdit", model);
        }

        [HttpPost]
        [LoginRequired]
        public ActionResult New(string name)
        {
            if(string.IsNullOrEmpty(name))
            {
                var model = new SystemEditViewModel
                {
                    Mode = 1,
                    SystemId = 0,
                    Name = string.Empty,
                };

                return View("SystemEdit", model);
            }
            else
            {
                SysUpdateHelper.AddSystem(name, Oper.Id);

                return RedirectToAction(nameof(SystemList));
            }
        }

        [HttpGet]
        [LoginRequired]
        public ActionResult DeleteConfirm(int id)
        {
            var model = new ConfirmViewModel
            {
                Message = "删除系统将同时删除和它相关的所有运行文件及更新包，是否继续？",
                OkUrl = Url.RouteUrl("Home", new { action = "Delete", id, }),
                CancelUrl = Url.RouteUrl("Home", new { action = "SystemList", }),
            };
            ViewBag.Title = "删除系统信息";

            return View("Confirm", model);
        }

        [HttpPost]
        [LoginRequired]
        public ActionResult Delete(int id)
        {
            SysUpdateHelper.DeleteSystem(id);

            return RedirectToAction(nameof(SystemList));
        }

        [HttpGet]
        [LoginRequired]
        public ActionResult UpdateDetect(int id, bool e)
        {
            SysUpdateHelper.UpdateDetect(id, enabled: e);

            return RedirectToAction(nameof(SystemList));
        }

        [HttpGet]
        public ActionResult Login()
        {
            var model = new LoginViewModel();

            return View(model);
        }

        [HttpPost]
        public ActionResult Login(LoginViewModel info)
        {
            try
            {
                SessionInfo.Emp = EmpHelper.Login(info.Code, info.Password);

                if (!string.IsNullOrEmpty(info.From))
                {
                    return Redirect(info.From);
                }
                else
                {
                    return RedirectToAction(nameof(Index));
                }
            }
            catch (Exception ex)
            {
                info.Error = ex.Message;

                return View(info);
            }
        }
    }
}