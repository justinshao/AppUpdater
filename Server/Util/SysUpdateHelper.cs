using ICSharpCode.SharpZipLib.Zip;
using Newtonsoft.Json;
using Justin.Updater.Server.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Justin.Updater.Shared;

namespace Justin.Updater.Server
{
    static class SysUpdateHelper
    {
        /// <summary>
        /// 已安装更新包对应的后缀名
        /// </summary>
        public static readonly string InstalledPkgExt = ".$install";
        /// <summary>
        /// 安装时新增文件对应的备份文件后缀名
        /// </summary>
        public static readonly string AddedFileExt = ".$add";
        /// <summary>
        /// 以该后缀名结束的上传文件代表要删除服务器上对应的文件
        /// </summary>
        public static readonly string InstallDeleteFileExt = ".$del";
        /// <summary>
        /// 锁定更新包的后缀名
        /// </summary>
        public static readonly string SystemLockFileExt = ".$lock";
        
        /// <summary>
        /// 未安装的更新包名规则
        /// </summary>
        private static readonly Regex rUnInstalledPkgName = new Regex(@"^\$(\d+)\-(.+\.zip)$");
        /// <summary>
        /// 匹配已安装的更新包名
        /// </summary>
        private static readonly Regex rInstalledPkgName = new Regex(string.Format(@"^\$(\d+)\-(.*)\.(\d+){0}$", Regex.Escape(InstalledPkgExt)));

        #region 更新系统增删改查
        public static IList<Models.System> GetSystems()
        {
            var systemsFile = GetSystemsFile();

            if(File.Exists(systemsFile))
            {
                var systems = JsonConvert.DeserializeObject<List<Models.System>>(File.ReadAllText(systemsFile, Encoding.UTF8));
                
                systems.ForEach(s => s.UpdateDetectEnabled = UpdateDetectEnabled(s.Id));

                return systems;
            }
            else
            {
                return new List<Models.System>();
            }
        }

        public static void SaveSystems(IList<Models.System> systems)
        {
            var systemsFile = GetSystemsFile();

            var fileInfo = new FileInfo(systemsFile);

            if(!fileInfo.Directory.Exists)
            {
                fileInfo.Directory.Create();
            }

            File.WriteAllText(systemsFile, JsonConvert.SerializeObject(systems,
                new JsonSerializerSettings
                {
                    Formatting = Formatting.Indented,
                }), Encoding.UTF8);
        }

        internal static void SaveSystemConfig(int id, Config config)
        {
            SerializeConfig(id, config);
        }

        public static Models.System GetSystem(int id)
        {
            return GetSystems().FirstOrDefault(s => s.Id == id);
        }
        public static void AddSystem(string name, int empId)
        {
            var systems = GetSystems();
            var maxId = systems.Any() ? systems.Max(s => s.Id) : 0;
            systems.Add(new Models.System { Id = maxId + 1, Name = name });

            SaveSystems(systems);
        }
        public static void SaveSystem(int id, string name, int empId)
        {
            var systems = GetSystems();
            var system = systems.FirstOrDefault(s => s.Id == id);

            if(system == null)
            {
                return;
            }

            system.Name = name;

            SaveSystems(systems);
        }
        public static void DeleteSystem(int systemId)
        {
            var runInfoFile = GetSystemRunInfoFile(systemId);
            IOHelper.DeleteFile(runInfoFile);

            var runDir = EnsureCreateSystemRunDir(systemId).Dir;
            IOHelper.DeleteDir(runDir);

            var uploadDir = EnsureCreateSystemUploadDir(systemId).Dir;
            IOHelper.DeleteDir(uploadDir);
            
            var backupDir = EnsureCreateSystemBackupDir(systemId).Dir;
            IOHelper.DeleteDir(backupDir);


            var systems = GetSystems();
            var system = systems.First(s => s.Id == systemId);

            if(system == null)
            {
                return;
            }

            systems.Remove(system);
            SaveSystems(systems);
        }
        #endregion

        #region 更新检测相关支持接口
        /// <summary>
        /// 是否启用了更新检测
        /// </summary>
        /// <param name="systemId"></param>
        /// <returns></returns>
        public static bool UpdateDetectEnabled(int systemId)
        {
            var tagFile = GetUpdateDetectTagFile(systemId);

            return File.Exists(tagFile);
        }

        /// <summary>
        /// 客户端是否启用更新检测
        /// </summary>
        /// <param name="systemId">系统id</param>
        /// <param name="enabled">是否启用更新检测</param>
        public static void UpdateDetect(int systemId, bool enabled)
        {
            if(enabled)
            {
                if(!UpdateDetectEnabled(systemId))
                {
                    EnableSystemUpdateDetect(systemId);
                }
            }
            else
            {
                DisableSystemUpdateDetect(systemId);
            }
        } 

        /// <summary>
        /// 更新检测
        /// </summary>
        /// <param name="systemId"></param>
        public static void TouchDetect(int systemId)
        {
            try
            {
                var config = GetSystemConfig(systemId);

                if (config.DetectEnabled)
                {
                    var tagFile = GetUpdateDetectTagFile(systemId);

                    File.AppendAllText(tagFile, DateTime.Now.ToString("yyyyMMddHHmmss"));
                }
            }
            catch (Exception ex)
            {
                LogHelper.LogError("Touch更新检测文件失败", ex);
            }
        }

        /// <summary>
        /// 获取更新检测当前版本
        /// </summary>
        /// <param name="systemId"></param>
        /// <returns></returns>
        public static string UpdateDetectVer(int systemId)
        {
            if(UpdateDetectEnabled(systemId))
            {
                var tagFile = GetUpdateDetectTagFile(systemId);

                using (StreamReader reader = new StreamReader(File.OpenRead(tagFile)))
                {
                    return reader.ReadLine();
                }
            }
            else
            {
                return string.Empty;
            }
        }

        public static Config GetSystemConfig(int id)
        {
            var config = DeserializeConfig(id);
            var defaultDetectInterval = 5;
            var defaultPromptInterval = 60;
            
            if (config == null)
            {
                return new Config
                {
                    DetectInterval = defaultDetectInterval,
                    PromptInterval = defaultPromptInterval,
                };
            }
            else
            {
                config.DetectInterval = config.DetectInterval <= 0 ? defaultDetectInterval : config.DetectInterval;
                config.PromptInterval = config.PromptInterval <= 0 ? defaultPromptInterval : config.PromptInterval;
            }

            return config;
        }
        #endregion

        #region 更新包管理相关接口
        /// <summary>
        /// 保存更新包
        /// </summary>
        /// <param name="systemId"></param>
        /// <param name="fileName"></param>
        /// <param name="uploadFile"></param>
        public static void SaveUploadPkg(int systemId, string fileName, Stream uploadFile)
        {
            string pkgPath = NewUploadPkgFile(systemId, fileName);

            try
            {
                using (Stream fs = File.Create(pkgPath))
                {
                    uploadFile.CopyTo(fs);
                }
            }
            catch
            {
                IOHelper.DeleteFile(pkgPath);

                throw;
            }
        }

        /// <summary>
        /// 安装更新包
        /// </summary>
        /// <param name="systemId"></param>
        /// <param name="pkgId"></param>
        public static void InstallPkg(int systemId, int pkgId)
        {
            bool infoUldated = false;

            try
            {
                var installedRunFiles = ExtractPkg(systemId, pkgId);

                UpdateSystemRunInfo(systemId, installedRunFiles);

                infoUldated = true;
                
                // 重命名更新包，变成安装状态
                IOHelper.MoveFile(GetUploadPkgFile(systemId, pkgId),
                    CreateInstalledPkgTagFile(systemId, pkgId));
            }
            catch
            {
                RestorePkg(systemId, pkgId, updateRunInfo: infoUldated);

                throw;
            }
        }
        private static IList<RunFile> ExtractPkg(int systemId, int pkgId)
        {
            string pkgPath = GetUploadPkgFile(systemId, pkgId);
            
            var runDirRet = EnsureCreateSystemRunDir(systemId);
            var runDir = runDirRet.Dir;
            var runDirEmpty = !runDirRet.Exists ||
                            (Directory.GetFiles(runDir).Count() == 0 &&
                            Directory.GetDirectories(runDir).Count() == 0);
            var backupDir = EnsureCreatePkgBackupDir(systemId, pkgId).Dir;

            var installedRunFileResults = new List<RunFile>();

            FastZipEvents extractEvents = new FastZipEvents
            {
                ConfirmFile = (sender, e) =>
                { // 解压一个文件前进行确认。这个功能是自己加的，原始版本没有这个功能。
                    
                    if (e.Name.EndsWith(InstallDeleteFileExt))
                    { // 代表要删除服务器上的文件
                        var path = e.Name.Substring(0, e.Name.Length - InstallDeleteFileExt.Length);
                        var runFile = Path.Combine(runDir, path);
                        var backupFile = Path.Combine(backupDir, path);

                        if (File.Exists(runFile))
                        {
                            IOHelper.MoveFile(runFile, backupFile);
                        }

                        installedRunFileResults.Add(new RunFile
                        {
                            Path = path,
                            Status = RunFileStatus.Delete,
                        });

                        e.Skip = true;
                    }
                },
                ProcessFile = (sender, e) =>
                { // 开始准备解压一个文件时调用
                    string src = Path.Combine(runDir, e.Name);
                    string dst = Path.Combine(backupDir, e.Name);

                    if (File.Exists(src))
                    { // 文件存在，备份
                        IOHelper.MoveFile(src, dst);
                    }
                    else
                    { // 文件不存在，产生一个表示还原的时候需要删除的标记文件
                        IOHelper.CreateEmptyFile(Path.Combine(backupDir, e.Name + AddedFileExt));
                    }
                },
                CompletedFile = (sender, e) =>
                { // 一个文件解压完成后调用
                    var file = Path.Combine(runDir, e.Name);
                    installedRunFileResults.Add(new RunFile
                    {
                        Path = e.Name,
                        Tag = CryptoHelper.MD5(File.OpenRead(file)),
                        Status = RunFileStatus.Update,
                    });
                },
                FileFailure = (sender, e) =>
                { // 解压一个文件失败时调用
                    e.ContinueRunning = false;

                    // 内部会吞噬异常，所以这边要重新抛出异常
                    throw e.Exception;
                },
            };

            (new FastZip(extractEvents)).ExtractZip(pkgPath, runDir, string.Empty);
            
            return installedRunFileResults;
        }

        /// <summary>
        /// 还原更新包
        /// </summary>
        /// <param name="systemId"></param>
        /// <param name="pkgId"></param>
        /// <param name="updateRunInfo"></param>
        public static void RestorePkg(int systemId, int pkgId, bool updateRunInfo = true)
        {
            var backupDir = EnsureCreatePkgBackupDir(systemId, pkgId).Dir;
            var runDir = EnsureCreateSystemRunDir(systemId).Dir;

            var restoreRunFileResults = new List<RunFile>();

            foreach (var f in new DirectoryInfo(backupDir).EnumerateFiles("*", SearchOption.AllDirectories))
            {
                if (f.Name.EndsWith(AddedFileExt))
                { // 需要删除的文件
                    var path = f.FullName.Substring(0, f.FullName.Length - AddedFileExt.Length)
                        .Substring(backupDir.Length + 1);
                    var dst = Path.Combine(runDir, path);

                    IOHelper.DeleteFile(dst);

                    if (updateRunInfo)
                    {
                        restoreRunFileResults.Add(new RunFile
                        {
                            Path = path,
                            Status = RunFileStatus.Delete,
                        });
                    }
                }
                else
                {
                    var path = f.FullName.Substring(backupDir.Length + 1);
                    var dst = Path.Combine(runDir, path);

                    IOHelper.MoveFile(f.FullName, dst);

                    if (updateRunInfo)
                    {
                        restoreRunFileResults.Add(new RunFile
                        {
                            Path = path,
                            Status = RunFileStatus.Update,
                            Tag = CryptoHelper.MD5(File.OpenRead(dst)),
                        });
                    }
                }
            }

            if (updateRunInfo)
            {
                UpdateSystemRunInfo(systemId, restoreRunFileResults);
            }

            // 删除备份目录及重命名更新包
            if(IsPkgInstalled(systemId, pkgId))
            {
                var uninstallPkg = Path.Combine(EnsureCreateSystemUploadDir(systemId).Dir,
                    string.Format("${0}-{1}", pkgId, GetPkgRawName(systemId, pkgId)));
                var installedPkg = GetInstalledPkgTagFile(systemId, pkgId);

                IOHelper.MoveFile(installedPkg, uninstallPkg);
            }
            IOHelper.DeleteDir(backupDir);
        }

        /// <summary>
        /// 删除更新包
        /// </summary>
        /// <param name="systemId"></param>
        /// <param name="pkgId"></param>
        public static void DeletePkg(int systemId, int pkgId)
        {
            if (IsPkgInstalled(systemId, pkgId))
            {
                IOHelper.DeleteFile(GetInstalledPkgTagFile(systemId, pkgId));
                IOHelper.DeleteDir(EnsureCreatePkgBackupDir(systemId, pkgId).Dir);
            }
            else
            {
                IOHelper.DeleteFile(GetUploadPkgFile(systemId, pkgId));
            }
        }

        /// <summary>
        /// 获取对应系统所有的更新包
        /// </summary>
        /// <param name="systemId"></param>
        /// <returns></returns>
        public static Tuple<IEnumerable<PkgInfo>, int> GetSystemPkgs(int systemId, int page, int perPage)
        {
            var pkgDir = EnsureCreateSystemUploadDir(systemId).Dir;

            var allPkgs = new DirectoryInfo(pkgDir).EnumerateFiles().ToList();

            var result = (from f in allPkgs
                    where f.Name.EndsWith(".zip") || f.Name.EndsWith(InstalledPkgExt)
                   select GetPkgInfo(f.FullName)).OrderByDescending(p => p.Id).Skip(perPage * (page - 1)).Take(perPage);

            return Tuple.Create(result, allPkgs.Count);
        }

        /// <summary>
        /// 产生系统运行目录所有文件对应的json文件
        /// </summary>
        /// <param name="systemId"></param>
        public static void GenerateSystemJsonFile(int systemId)
        {
            var runDir = EnsureCreateSystemRunDir(systemId).Dir;

            var updateFiles = from f in new DirectoryInfo(runDir).EnumerateFiles("*", SearchOption.AllDirectories)
                              select new RunFile
                              {
                                  Path = f.FullName.Substring(runDir.Length + 1),
                                  Status = RunFileStatus.Update,
                                  Tag = CryptoHelper.MD5(File.OpenRead(f.FullName)),
                              };

            UpdateSystemRunInfo(systemId, updateFiles, reset: true);
        }

        /// <summary>
        /// 获取系统运行文件路径
        /// </summary>
        /// <param name="systemId"></param>
        /// <param name="file"></param>
        /// <returns></returns>
        public static string GetSystemRunFile(int systemId, string file)
        {
            return Path.Combine(EnsureCreateSystemRunDir(systemId).Dir, file);
        }

        /// <summary>
        /// 获取系统运行信息json文件路径
        /// </summary>
        /// <param name="systemId"></param>
        /// <returns></returns>
        public static string GetSystemRunInfoFile(int systemId)
        {
            return Path.Combine(EnsureCreateRunInfoDir().Dir, "system" + systemId.ToString());
        }

        /// <summary>
        /// 获取系统系统配置json文件路径
        /// </summary>
        /// <param name="systemId"></param>
        /// <returns></returns>
        public static string GetSystemConfigFile(int systemId)
        {
            return Path.Combine(EnsureCreateConfigDir().Dir, "system" + systemId.ToString());
        }

        /// <summary>
        /// 获取客户端命令文件
        /// </summary>
        /// <param name="systemId"></param>
        /// <param name="clientId"></param>
        /// <returns></returns>
        public static string GetClientCommandFile(int systemId, string clientId)
        {
            var dir = EnsureCreateCommandDir(systemId, clientId);

            return Path.Combine(dir.Dir, clientId);
        }

        /// <summary>
        /// 判断是否是最近安装的更新包
        /// </summary>
        /// <param name="systemId"></param>
        /// <param name="pkgId"></param>
        /// <returns></returns>
        public static bool IsLatestInstalledPkg(int systemId, int pkgId)
        {
            var latestInstPkg = GetOrderedInstalledPkgs(systemId).FirstOrDefault();

            return latestInstPkg == null || latestInstPkg.StartsWith("$" + pkgId + "-");
        }

        /// <summary>
        /// 判断更新包是否已安装
        /// </summary>
        /// <param name="systemId"></param>
        /// <param name="pkgId"></param>
        /// <returns></returns>
        public static bool IsPkgInstalled(int systemId, int pkgId)
        {
            var tagFile = GetInstalledPkgTagFile(systemId, pkgId);

            return !string.IsNullOrEmpty(tagFile) && File.Exists(tagFile);
        }

        /// <summary>
        /// 获取原始上传的更新包名
        /// </summary>
        /// <param name="systemId"></param>
        /// <param name="pkgId"></param>
        /// <returns></returns>
        public static string GetPkgRawName(int systemId, int pkgId)
        {
            var uploadDir = EnsureCreateSystemUploadDir(systemId).Dir;

            if (IsPkgInstalled(systemId, pkgId))
            {
                var _r = string.Format(@"^\${0}\-(.*)\.\d+{1}$", pkgId.ToString(), Regex.Escape(InstalledPkgExt));
                Regex r = new Regex(_r);

                return (from f in new DirectoryInfo(uploadDir).EnumerateFiles()
                        where r.IsMatch(f.Name)
                        select r.Match(f.Name).Groups[1].Value).FirstOrDefault();
            }
            else
            {
                var _r = string.Format(@"^\${0}\-(.*\.zip)$", pkgId.ToString());
                Regex r = new Regex(_r);

                return (from f in new DirectoryInfo(uploadDir).EnumerateFiles()
                        where r.IsMatch(f.Name)
                        select r.Match(f.Name).Groups[1].Value).FirstOrDefault();
            }
        }
        
        public static void LockSystemForOp(int systemId)
        {
            var lockFile = GetSystemLockFile(systemId);

            IOHelper.CreateEmptyFile(lockFile);
        }
        public static void UnLockSystemForOp(int systemId)
        {
            var lockFile = GetSystemLockFile(systemId);

            IOHelper.DeleteFile(lockFile);
        }
        public static bool IsSystemLocked(int systemId)
        {
            var lockFile = GetSystemLockFile(systemId);

            return File.Exists(lockFile);
        }

        public static SystemRunInfo EmptyRunInfo
        {
            get { return new SystemRunInfo { Ver = string.Empty, RunFiles = new DicIgnoreCase<string>(), }; }
        }
        #endregion

        /// <summary>
        /// 获取系统运行信息
        /// </summary>
        /// <param name="systemId"></param>
        /// <returns></returns>
        public static SystemRunInfo GetSystemRunInfo(int systemId)
        {
            return DeserializeRunInfo(systemId);
        }

        /// <summary>
        /// 获取系统日志文件
        /// </summary>
        /// <param name="systemId"></param>
        /// <param name="page"></param>
        /// <param name="perPage"></param>
        /// <param name="date"></param>
        /// <returns></returns>
        public static Tuple<IEnumerable<SystemLogFile>, int> GetSystemLogFiles(int systemId, int page, int perPage, DateTime date)
        {
            var logDir = LogHelper.GetSystemClientLogDir(systemId, date);

            IEnumerable<SystemLogFile> logFiles = null;
            int count = 0;

            if (!Directory.Exists(logDir))
            {
                logFiles = new SystemLogFile[] { };
                count = 0;
            }
            else
            {
                var allFiles = new DirectoryInfo(logDir).GetFiles("*.log", SearchOption.TopDirectoryOnly);

                logFiles = from f in allFiles.OrderBy(f => f.Name)
                       .Skip(perPage * (page - 1)).Take(perPage)
                           select new SystemLogFile
                           {
                               Name = f.Name,
                               Size = f.Length,
                               CreationTime = f.CreationTime,
                               LastWriteTime = f.LastWriteTime
                           };
                count = allFiles.Count();
            }

            return Tuple.Create(logFiles, count);
        }

        /// <summary>
        /// 获取系统日志文件
        /// </summary>
        /// <param name="systemId"></param>
        /// <param name="date"></param>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public static FileInfo GetSystemLogFile(int systemId, DateTime date, string fileName)
        {
            var logDir = LogHelper.GetSystemClientLogDir(systemId, date);
            var file = Path.Combine(logDir, fileName);

            if (!File.Exists(file))
            {
                return null;
            }

            return new FileInfo(file);
        }

        /// <summary>
        /// 设置命令
        /// </summary>
        /// <param name="id"></param>
        /// <param name="clientId"></param>
        /// <param name="command"></param>
        public static void SetCommand(int id, string clientId, ClientCommand command)
        {
            var cmdFile = GetClientCommandFile(id, clientId);

            File.WriteAllText(cmdFile, JsonConvert.SerializeObject(command,
                new JsonSerializerSettings
                {
                    Formatting = Formatting.Indented,
                }), Encoding.UTF8);
        }

        /// <summary>
        /// 清除客户端命令
        /// </summary>
        /// <param name="id"></param>
        /// <param name="clientId"></param>
        public static void ClearCommand(int id, string clientId)
        {
            var cmdFile = GetClientCommandFile(id, clientId);

            IOHelper.DeleteFile(cmdFile);
        }

        /// <summary>
        /// 获取客户端命令
        /// </summary>
        /// <param name="id"></param>
        /// <param name="clientId"></param>
        /// <returns></returns>
        public static ClientCommand GetCommand(int id, string clientId)
        {
            var jsonFile = GetClientCommandFile(id, clientId);

            if (!File.Exists(jsonFile))
                return null;

            return JsonConvert.DeserializeObject<ClientCommand>(File.ReadAllText(jsonFile, Encoding.UTF8));
        }

        #region 私有辅助函数
        /// <summary>
        /// 根据系统id和更新包名，返回新增的上传包
        /// </summary>
        /// <param name="systemId"></param>
        /// <param name="fileName"></param>
        /// <returns></returns>
        private static string NewUploadPkgFile(int systemId, string fileName)
        {
            // 上传包保存的名字为 $+递增的序号+fileName

            var dir = EnsureCreateSystemUploadDir(systemId).Dir;
            var maxid = GetMaxPkgId(systemId);

            return Path.Combine(dir, "$" + (++maxid).ToString() + "-" + fileName);
        }

        /// <summary>
        /// 获取更新包的最大id
        /// </summary>
        /// <param name="systemId"></param>
        /// <returns></returns>
        private static int GetMaxPkgId(int systemId)
        {
            Regex r = new Regex(@"^\$(\d+)\-(.+)$"); // eg. $12-HIS5.zip, $12-HIS5.zip
            var dir = EnsureCreateSystemUploadDir(systemId).Dir;

            return (from f in new DirectoryInfo(dir).EnumerateFiles()
                    where r.IsMatch(f.Name)
                    let id = Convert.ToInt32(r.Match(f.Name).Groups[1].Value)
                    orderby id descending
                    select id).FirstOrDefault();
        }

        /// <summary>
        /// 根据系统id和更新包id获取更新包路径
        /// </summary>
        /// <param name="systemId"></param>
        /// <param name="pkgId"></param>
        /// <returns></returns>
        private static string GetUploadPkgFile(int systemId, int pkgId)
        {
            var uploadDir = EnsureCreateSystemUploadDir(systemId).Dir;
            var _r = string.Format(@"^\${0}\-(.*)\.zip$", pkgId.ToString());
            Regex r = new Regex(_r);

            return (from f in new DirectoryInfo(uploadDir).EnumerateFiles()
                    where r.IsMatch(f.Name)
                    select f.FullName).FirstOrDefault();
        }

        /// <summary>
        /// 创建并返回更新包上传保存路径
        /// </summary>
        /// <param name="systemId"></param>
        /// <returns></returns>
        private static DirCreateResult EnsureCreateSystemUploadDir(int systemId)
        {
            string dir = Path.Combine(AppSettingHelper.UpdateDir, "Upload", "System" + systemId.ToString());

            return IOHelper.EnsureCreateDir(dir);
        }

        /// <summary>
        /// 获取更新包“锁”文件
        /// </summary>
        /// <param name="systemId"></param>
        /// <param name="pkgId"></param>
        /// <returns></returns>
        private static string GetPkgLockFile(int systemId, int pkgId)
        {
            var uploadDir = EnsureCreateSystemUploadDir(systemId).Dir;

            return Path.Combine(uploadDir, pkgId + SystemLockFileExt);
        }

        /// <summary>
        /// 获取更新包安装标志文件
        /// </summary>
        /// <param name="systemId"></param>
        /// <param name="pkgId"></param>
        /// <returns></returns>
        private static string GetInstalledPkgTagFile(int systemId, int pkgId)
        {
            var uploadDir = EnsureCreateSystemUploadDir(systemId).Dir;
            var _r = string.Format(@"^\${0}\-.*\.\d+{1}$", pkgId.ToString(), Regex.Escape(InstalledPkgExt));
            Regex r = new Regex(_r);

            return (from f in new DirectoryInfo(uploadDir).EnumerateFiles()
                    where r.IsMatch(f.Name)
                    select f.FullName).FirstOrDefault();
        }

        /// <summary>
        /// 创建并返回系统运行路径
        /// </summary>
        /// <param name="systemId"></param>
        /// <returns></returns>
        private static DirCreateResult EnsureCreateSystemRunDir(int systemId)
        {
            string dir = Path.Combine(AppSettingHelper.UpdateDir,
                "Run", "System" + systemId.ToString());

            return IOHelper.EnsureCreateDir(dir);
        }

        /// <summary>
        /// 创建并返回更新包备份路径
        /// </summary>
        /// <param name="systemId"></param>
        /// <param name="pkgId"></param>
        /// <returns></returns>
        private static DirCreateResult EnsureCreatePkgBackupDir(int systemId, int pkgId)
        {
            var dir = Path.Combine(EnsureCreateSystemBackupDir(systemId).Dir, pkgId.ToString());

            return IOHelper.EnsureCreateDir(dir);
        }

        /// <summary>
        /// 更新系统运行信息
        /// </summary>
        /// <param name="systemId"></param>
        /// <param name="updateFiles"></param>
        /// <param name="reset"></param>
        private static void UpdateSystemRunInfo(int systemId, IEnumerable<RunFile> updateFiles, bool reset = false)
        {
            SystemRunInfo newRunInfo = null;

            if(!reset)
            {
                newRunInfo = DeserializeRunInfo(systemId);
            }
            
            if(newRunInfo == null)
            {
                newRunInfo = new SystemRunInfo
                {
                    RunFiles = new DicIgnoreCase<string>(),
                };
            }

            var runFiles = newRunInfo.RunFiles;
            foreach (var f in updateFiles)
            {
                if (f.Status == RunFileStatus.Update)
                {
                    runFiles[f.Path] = f.Tag;
                }
                else
                { // delete
                    runFiles.Remove(f.Path);
                }
            }

            newRunInfo.Ver = DateTime.Now.ToString("yyyyMMddHHmmss");

            SerializeRunInfo(systemId, newRunInfo);
        }

        /// <summary>
        /// 创建并返回保存系统运行信息的路径
        /// </summary>
        /// <returns></returns>
        private static DirCreateResult EnsureCreateRunInfoDir()
        {
            var dir = Path.Combine(AppSettingHelper.UpdateDir, "RunInfo");

            return IOHelper.EnsureCreateDir(dir);
        }

        /// <summary>
        /// 创建并返回保存系统配置的路径
        /// </summary>
        /// <returns></returns>
        private static DirCreateResult EnsureCreateConfigDir()
        {
            var dir = Path.Combine(AppSettingHelper.UpdateDir, "Config");

            return IOHelper.EnsureCreateDir(dir);
        }

        /// <summary>
        /// 创建并返回保存保存客户端命令的目录
        /// </summary>
        /// <param name="systemId"></param>
        /// <param name="clientId"></param>
        /// <returns></returns>
        private static DirCreateResult EnsureCreateCommandDir(int systemId, string clientId)
        {
            var dir = Path.Combine(AppSettingHelper.UpdateDir, "Command", $"System{systemId}");

            return IOHelper.EnsureCreateDir(dir);
        }

        /// <summary>
        /// 获取存储系统信息的文件
        /// </summary>
        /// <returns></returns>
        private static string GetSystemsFile()
        {
            return Path.Combine(AppSettingHelper.UpdateDir, "systems.json");
        }

        /// <summary>
        /// 取得最近安装的更新包的安装序号
        /// </summary>
        /// <param name="systemId"></param>
        /// <returns></returns>
        private static int GetMaxInstalledPkgSeq(int systemId)
        {
            var uploadDir = EnsureCreateSystemUploadDir(systemId).Dir;

            return (from f in new DirectoryInfo(uploadDir).EnumerateFiles()
                    where f.Name.EndsWith(InstalledPkgExt)
                    let seq = Convert.ToInt32(f.Name.Split('.').Reverse().ElementAt(1))
                    orderby seq descending
                    select seq).FirstOrDefault();
        }

        /// <summary>
        /// 获取新建的安装标记文件路径
        /// </summary>
        /// <param name="systemId"></param>
        /// <param name="pkgId"></param>
        /// <returns></returns>
        private static string CreateInstalledPkgTagFile(int systemId, int pkgId)
        {
            // 具体表示为：$pkgId-pkgName.安装序号.$install
            var max_seq = GetMaxInstalledPkgSeq(systemId);
            var uploadDir = EnsureCreateSystemUploadDir(systemId).Dir;
            var pkgFile = GetUploadPkgFile(systemId, pkgId);

            return Path.Combine(uploadDir,
                new FileInfo(pkgFile).Name + "." + (max_seq + 1).ToString() + InstalledPkgExt);
        }
        
        /// <summary>
        /// 返回按照最近安装顺序排序的更新包安装标志文件
        /// </summary>
        /// <param name="systemId"></param>
        /// <returns></returns>
        private static IEnumerable<string> GetOrderedInstalledPkgs(int systemId)
        {
            var uploadDir = EnsureCreateSystemUploadDir(systemId).Dir;

            return (from f in new DirectoryInfo(uploadDir).EnumerateFiles()
                    where rInstalledPkgName.IsMatch(f.Name)
                    let installSeq = Convert.ToInt32(rInstalledPkgName.Match(f.Name).Groups[3].Value)
                    orderby installSeq descending
                    select f.Name);
        }

        /// <summary>
        /// 创建并返回系统备份目录
        /// </summary>
        /// <param name="systemId"></param>
        /// <returns></returns>
        private static DirCreateResult EnsureCreateSystemBackupDir(int systemId)
        {
            var dir = Path.Combine(AppSettingHelper.UpdateDir, "Backup",
                "System" + systemId.ToString());

            return IOHelper.EnsureCreateDir(dir);
        } 
        
        /// <summary>
        /// 获取更新包信息
        /// </summary>
        /// <param name="pkgPath"></param>
        /// <returns></returns>
        private static PkgInfo GetPkgInfo(string pkgPath)
        {
            var fileInfo = new FileInfo(pkgPath);
            if(fileInfo.Name.EndsWith(InstalledPkgExt))
            {
                var m = rInstalledPkgName.Match(fileInfo.Name);

                return new PkgInfo
                {
                    Id = Convert.ToInt32(m.Groups[1].Value),
                    Name = m.Groups[2].Value,
                    Installed = true,
                    Size = fileInfo.Length,
                    UploadOrInstallTime = fileInfo.CreationTime,
                };
            }
            else
            {
                var m = rUnInstalledPkgName.Match(fileInfo.Name);

                return new PkgInfo
                {
                    Id = Convert.ToInt32(m.Groups[1].Value),
                    Name = m.Groups[2].Value,
                    Installed = false,
                    Size = fileInfo.Length,
                    UploadOrInstallTime = fileInfo.CreationTime,
                };
            }
        }

        private static void DeletePkgBackupDir(int systemId, int pkgId)
        {
            var backupDir = EnsureCreatePkgBackupDir(systemId, pkgId).Dir;

            IOHelper.DeleteDir(backupDir);
        }

        private static DirCreateResult EnsureCreateSystemLockDir()
        {
            var dir = Path.Combine(AppSettingHelper.UpdateDir, "Lock");

            return IOHelper.EnsureCreateDir(dir);
        }
        private static string GetSystemLockFile(int systemId)
        {
            return Path.Combine(EnsureCreateSystemLockDir().Dir,
                string.Format("system{0}" + SystemLockFileExt, systemId));
        }

        private static void SerializeRunInfo(int systemId, SystemRunInfo runInfo)
        {
            var jsonFile = GetSystemRunInfoFile(systemId);

            //using (var writer = new BinaryWriter(File.Create(jsonFile), Encoding.UTF8))
            //{
            //    writer.Write(runInfo.Ver);
            //    writer.Write(runInfo.RunFiles.Count);

            //    foreach (var e in runInfo.RunFiles)
            //    {
            //        writer.Write(e.Key);
            //        writer.Write(e.Value);
            //    }
            //}

            File.WriteAllText(jsonFile, JsonConvert.SerializeObject(runInfo, 
                new JsonSerializerSettings
                {
                    Formatting = Formatting.Indented,
                }), Encoding.UTF8);
        }
        private static SystemRunInfo DeserializeRunInfo(int systemId)
        {
            var jsonFile = GetSystemRunInfoFile(systemId);

            if (!File.Exists(jsonFile))
                return null;

            //var runInfo = new SystemRunInfo();

            //using (var reader = new BinaryReader(File.OpenRead(jsonFile), Encoding.UTF8))
            //{
            //    runInfo.Ver = reader.ReadString();

            //    var count = reader.ReadInt32();
            //    var runFiles = new Dictionary<string, string>();

            //    for (int i = 0; i < count; i++)
            //    {
            //        runFiles.Add(reader.ReadString(), reader.ReadString());
            //    }

            //    runInfo.RunFiles = runFiles;
            //}

            return JsonConvert.DeserializeObject<SystemRunInfo>(File.ReadAllText(jsonFile, Encoding.UTF8));
        }

        private static void SerializeConfig(int systemId, Config config)
        {
            var jsonFile = GetSystemConfigFile(systemId);

            File.WriteAllText(jsonFile, JsonConvert.SerializeObject(config, 
                new JsonSerializerSettings
                {
                    Formatting = Formatting.Indented,
                }), Encoding.UTF8);
        }
        private static Config DeserializeConfig(int systemId)
        {
            var jsonFile = GetSystemConfigFile(systemId);

            if (!File.Exists(jsonFile))
                return null;

            return JsonConvert.DeserializeObject<Config>(File.ReadAllText(jsonFile, Encoding.UTF8));
        }

        /// <summary>
        /// 创建更新检测文件夹
        /// </summary>
        /// <returns></returns>
        private static DirCreateResult EnsureCreateUpdateDetectDir()
        {
            var dir = Path.Combine(AppSettingHelper.UpdateDir, "UpdateDetect");

            return IOHelper.EnsureCreateDir(dir);
        }
        /// <summary>
        /// 获取更新检测标记文件
        /// </summary>
        /// <param name="systemId"></param>
        /// <returns></returns>
        private static string GetUpdateDetectTagFile(int systemId)
        {
            return Path.Combine(EnsureCreateUpdateDetectDir().Dir, systemId.ToString());
        }
        /// <summary>
        /// 启用自动更新
        /// </summary>
        /// <param name="systemId"></param>
        private static void EnableSystemUpdateDetect(int systemId)
        {
            var tagFile = GetUpdateDetectTagFile(systemId);

            IOHelper.CreateEmptyFile(tagFile);
        }
        /// <summary>
        /// 禁用自动更新
        /// </summary>
        /// <param name="systemId"></param>
        private static void DisableSystemUpdateDetect(int systemId)
        {
            var tagFile = GetUpdateDetectTagFile(systemId);

            IOHelper.DeleteFile(tagFile);
        }
        #endregion
    }

    class SystemRunInfo
    {
        public string Ver { get; set; }
        public DicIgnoreCase<string> RunFiles { get; set; }
    }
    class RunFile
    {
        public string Path { get; set; }
        public string Tag { get; set; }
        public RunFileStatus Status { get; set; }
    }
    enum RunFileStatus
    {
        Update = 1,
        Delete = 2,
    }
}