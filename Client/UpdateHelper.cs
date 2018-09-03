using Justin.Serialization.Json;
using Justin.Updater.Shared;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;

namespace Justin.Updater.Client
{
    internal static class UpdateHelper
    {
        public static event Action<string> OnStatus;
        public static event Action<double> OnProgress;
        public static event Action<LocalRunInfo> OnBeginUpdate;
        public static event Action<LocalRunInfo> OnNotSpecifyUpdateUrl;
        public static event Action<LocalRunInfo, Exception> OnError;
        public static event Action<LocalRunInfo, IEnumerable<UpdateRunFile>, List<string>, List<string>, bool, bool, string> OnComplete;
        public static event Action<LocalRunInfo> OnInvalidUpdateUrl;
        public static event Action<LocalRunInfo> OnMainAppNotExists;
        public static event Action<LocalRunInfo, string> OnMainAppStarted;
        public static event Action<LocalRunInfo> OnMainAppAlreadyRunning;
        public static event Action<string> OnProcessFile;
        public static event Func<string, Exception, bool> OnConfirmFileError;

        public static void Update(string msg = null, int delay = 0)
        {
            ThreadPool.QueueUserWorkItem(_ => {
                if(delay > 0)
                {
                    Thread.Sleep(delay);
                }

                UpdateInternal(msg);
            }, msg);
        }
        private static void UpdateInternal(string msg)
        {
            LocalRunInfo localRunInfo = null;
            UpdateUrlInfo updateUrlInfo = null;

            try
            {
                OnStatus?.Invoke("正在读取本地配置...");
                localRunInfo = GetLocalRunInfo();

                OnStatus?.Invoke("正在验证更新地址...");
                if (string.IsNullOrEmpty(localRunInfo.UpdateUrl))
                {
                    OnNotSpecifyUpdateUrl?.Invoke(localRunInfo); return;
                }
                
                if ((updateUrlInfo = UpdateUrlInfo.Parse(localRunInfo.UpdateUrl)) == null)
                {
                    OnInvalidUpdateUrl?.Invoke(localRunInfo); return;
                }

                OnStatus?.Invoke("正在读取服务器...");
                var host = updateUrlInfo.Host;
                var systemId = updateUrlInfo.SystemId;
                var remoteRunInfo = GetRemoteRunInfo(GetRemoteInfoUrl(host, systemId));

                OnStatus?.Invoke("正在检测差异...");
                var initStart = (localRunInfo.RunFiles.Count == 0);
                var fileDiffResults = CalcDiff(localRunInfo, remoteRunInfo).ToList();
                var filesToUpdate = fileDiffResults.Where(f => f.Status == UpdateRunFileStatus.Update || f.Status == UpdateRunFileStatus.Delete).ToList();
                var progress = 0;
                var total = filesToUpdate.Count;

                if(total > 0)
                {
                    OnStatus?.Invoke("正在检测运行程序...");
                    if (IsThisAppRunning() || 
                        (!string.IsNullOrEmpty(localRunInfo.AppRunCmd) && IsMainAppRunning(localRunInfo)))
                    {
                        OnMainAppAlreadyRunning?.Invoke(localRunInfo);

                        return;
                    }
                }

                OnStatus?.Invoke("开始更新程序...");
                OnBeginUpdate?.Invoke(localRunInfo);

                var runDir = Path.GetDirectoryName(Application.ExecutablePath);
                var thisExeName = Path.GetFileName(Application.ExecutablePath);
                var runBat = false;
                var runFileResults = new DicIgnoreCase<string>();
                var failedUpdateFiles = new List<string>();
                var failedDeleteFiles = new List<string>();

                foreach (var file in filesToUpdate)
                {
                    if(Constants.BatFile.Equals(file.Path, StringComparison.OrdinalIgnoreCase))
                    {
                        runBat = true;
                    }

                    if (file.Status == UpdateRunFileStatus.Update)
                    {
                        var url = GetRunFileUrl(host, systemId, file.Path);
                        var localPath = Path.Combine(runDir, file.Path);
                        var updateSelf = file.Path.Equals(thisExeName, StringComparison.OrdinalIgnoreCase); // 自更新

                        OnProcessFile?.Invoke("正在安装文件：" + localPath);
                        var success = DownloadRunFile(url, localPath, possibllyInUse: updateSelf);

                        if (!success)
                        {
                            failedUpdateFiles.Add(file.Path);

                            // 对于更新失败的文件（非新增），新的配置文件应该仍包含更新前的文件标识
                            if(!string.IsNullOrEmpty(file.OldTag))
                            {
                                runFileResults.Add(file.Path, file.OldTag);
                            }
                        }
                        else
                        {
                            runFileResults.Add(file.Path, file.NewTag);
                        }
                    }
                    else if(file.Status == UpdateRunFileStatus.Delete)
                    { // 删除文件
                        var localPath = Path.Combine(runDir, file.Path);

                        OnProcessFile?.Invoke("正在删除文件：" + localPath);
                        if(!DeleteRunFile(localPath))
                        {
                            failedDeleteFiles.Add(file.Path);

                            // 对于删除失败的文件，新的配置文件应该仍包含旧的文件标识
                            runFileResults.Add(file.Path, file.OldTag);
                        }
                    }

                    OnProgress?.Invoke((++progress) * 1.0 / total);
                }

                foreach (var f in fileDiffResults.Where(f => f.Status == UpdateRunFileStatus.NotModified))
                {
                    runFileResults.Add(f.Path, f.OldTag);
                }

                localRunInfo.Ver = remoteRunInfo.Ver;
                localRunInfo.RunFiles = runFileResults;
                
                OnComplete?.Invoke(localRunInfo, fileDiffResults, failedUpdateFiles, failedDeleteFiles, initStart, runBat, msg);
            }
            catch (Exception ex)
            {
                if (localRunInfo != null)
                {
                    LogHelper.LogErrorToServer($"客户端更新失败（{msg}）", ex, localRunInfo);
                }

                OnError?.Invoke(localRunInfo, ex);
            }
        }

        /// <summary>
        /// 计算文件差异
        /// </summary>
        /// <returns></returns>
        private static IEnumerable<UpdateRunFile> CalcDiff(LocalRunInfo localRunInfo, RemoteRunInfo remoteRunInfo)
        {
            var remoteVer = remoteRunInfo.Ver;

            //if(!string.IsNullOrEmpty(remoteVer) && 
            //    !remoteVer.Equals(localRunInfo.Ver))
            //{ // 为提高效率，只有在版本不同时，才进行文件的差异比较

                var runDir = Path.GetDirectoryName(Application.ExecutablePath);
                var localRunFiles = localRunInfo.RunFiles;
                var remoteRunFiles = remoteRunInfo.RunFiles;

                foreach (var rf in remoteRunFiles)
                {

                // 本地不存在或者MD5不同
                var needUpdate = !localRunFiles.TryGetValue(rf.Key, out string localTag) ||
                        !rf.Value.Equals(localTag) ||
                        !File.Exists(Path.Combine(runDir, rf.Key));

                if (needUpdate)
                    { // update
                        yield return new UpdateRunFile
                        {
                            Path = rf.Key,
                            Status = (Constants.RunInfoJsonFile.Equals(rf.Key, StringComparison.OrdinalIgnoreCase) || 
                                      Constants.UpdateLogFile.Equals(rf.Key, StringComparison.OrdinalIgnoreCase)) ?
                                UpdateRunFileStatus.SkipUpdate : UpdateRunFileStatus.Update,
                            NewTag = rf.Value,
                            OldTag = localTag
                        };
                    }
                    else
                    {
                        yield return new UpdateRunFile
                        {
                            Path = rf.Key,
                            Status = UpdateRunFileStatus.NotModified,
                            OldTag = localTag,
                        };
                    }

                    if (localTag != null)
                        localRunFiles.Remove(rf.Key);
                }

                // 剩下的本地文件需要删除
                var thisExeName = Path.GetFileName(Application.ExecutablePath);
                foreach (var lf in localRunFiles)
                {
                    yield return new UpdateRunFile
                    {
                        Path = lf.Key,
                        Status = lf.Key.Equals(thisExeName, StringComparison.OrdinalIgnoreCase) ? 
                            UpdateRunFileStatus.SkipDelete : UpdateRunFileStatus.Delete,
                        OldTag = lf.Value,
                    };
                }
            //}
            //else
            //{
            //    foreach (var lf in localRunInfo.RunFiles)
            //    {
            //        yield return new UpdateRunFile
            //        {
            //            Path = lf.Key,
            //            Status = UpdateRunFileStatus.NotModified,
            //            Tag = lf.Value,
            //        };
            //    }
            //}
        }

        public static string GetRunInfoJsonFile()
        {
            return Path.Combine(Environment.CurrentDirectory, Constants.RunInfoJsonFile);
        }
        public static string GetBatFile()
        {
            return Path.Combine(Environment.CurrentDirectory, Constants.BatFile);
        }
        
        public static void RunMainApp(LocalRunInfo localRunInfo, string msg = null)
        {
            try
            {
                var cmds = localRunInfo.AppRunCmd.Split(new char[] { ' ' },
                        StringSplitOptions.RemoveEmptyEntries);
                var exeDir = Path.GetFullPath(Path.GetDirectoryName(cmds[0]));
                var exeName = Path.GetFileName(cmds[0]);
                var fileName = Path.Combine(exeDir, exeName);

                if(!File.Exists(fileName))
                {
                    OnMainAppNotExists?.Invoke(localRunInfo); return;
                }

                var args = cmds.Skip(1).Concat(new string[] { "started_by_updater" })
                    .Aggregate(string.Empty, (_args, a) => _args += " " + a);

                RunFile(fileName, args);

                OnMainAppStarted?.Invoke(localRunInfo, msg);
            }
            catch (Exception ex)
            {
                LogHelper.LogErrorToServer($"程序启动失败（{msg}）", ex, localRunInfo);

                OnError?.Invoke(localRunInfo, ex);
            }
        }
        public static void RunBat()
        {
            var batFile = GetBatFile();

            if (File.Exists(batFile))
                RunFile(batFile, args: null, waitForExit: true);
        }
        private static void RunFile(string fileName, string args, bool waitForExit = false)
        {
            using (Process p = new Process())
            {
                p.StartInfo.WorkingDirectory = new FileInfo(fileName).DirectoryName;
                p.StartInfo.FileName = fileName;
                p.StartInfo.Arguments = args;
                p.Start();

                if(waitForExit)
                    p.WaitForExit();
            }
        }

        public static void SaveLocalRunInfo(LocalRunInfo localRunInfo)
        {
            var filePath = GetRunInfoJsonFile();
            var jss = new JavaScriptSerializer();

            File.WriteAllText(filePath, jss.Serialize(localRunInfo), Encoding.UTF8);
        }
        private static LocalRunInfo GetLocalRunInfo()
        {
            var jsonFile = GetRunInfoJsonFile();

            if (File.Exists(jsonFile))
            {
                var jss = new JavaScriptSerializer();
                var runInfo = jss.Deserialize<LocalRunInfo>(File.ReadAllText(jsonFile, Encoding.UTF8));
                
                runInfo.RunFiles = runInfo.RunFiles ?? new DicIgnoreCase<string>();

                return runInfo;
            }

            return new LocalRunInfo { Ver = string.Empty, RunFiles = new DicIgnoreCase<string>(), };
        }
        private static RemoteRunInfo GetRemoteRunInfo(string url)
        {
            var info = Util.GetHttpResponseString(url);

            return new JavaScriptSerializer().Deserialize<RemoteRunInfo>(info);
        }

        private static bool DownloadRunFile(string url, string localPath, bool possibllyInUse = false)
        {
            var req = Util.CreateHttpRequest(url);

            try
            {
                if(localPath.EndsWith(Constants.UpdateErrorFile, StringComparison.OrdinalIgnoreCase))
                {
                    throw new Exception($"无效文件：{localPath}");
                }

                using (var resp = req.GetResponse())
                {
                    if(possibllyInUse)
                    {
                        using (var src = resp.GetResponseStream())
                        {
                            IOHelper.WriteFilePossibllyInUse(localPath, src);
                        }
                    }
                    else
                    {
                        using (Stream src = resp.GetResponseStream(), dst = IOHelper.Create(localPath))
                        {
                            src.CopyTo(dst);
                        }
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                var msg = string.Format("更新 {0} 出错。", localPath);

                LogHelper.LogError(msg, ex);

                if(!OnConfirmFileError(msg, ex))
                {
                    Environment.Exit(0);
                }

                return false;
            }
            finally
            {
                req?.Abort();
            }
        }
        private static bool DeleteRunFile(string localPath)
        {
            try
            {
                IOHelper.DeleteFile(localPath);

                return true;
            }
            catch (Exception ex)
            {
                if(!OnConfirmFileError.Invoke(string.Format("删除 {0} 出错。", localPath), ex))
                {
                    Environment.Exit(0);
                }

                return false;
            }
        }
        
        private static string GetRemoteInfoUrl(string host, string systemId)
        {
            return $"{host}/api/RunInfo/{systemId}";
        }
        private static string GetRunFileUrl(string host, string systemId, string path)
        {
            return $"{host}/api/RunFile/{systemId}?path={path}";
        }

        public static bool IsThisAppRunning()
        {
            var thisApp = Process.GetCurrentProcess();
            var thisFileName = thisApp.MainModule.FileName;

            return GetProcessesByAppPath(thisFileName, thisApp.ProcessName)
                .Any(p => p.Id != thisApp.Id);
        }
        private static bool IsMainAppRunning(LocalRunInfo localRunInfo)
        {
            var cmds = localRunInfo.AppRunCmd.Split(new char[] { ' ' },
                        StringSplitOptions.RemoveEmptyEntries);

            return IsAppRunning(cmds[0]);
        }
        public static bool IsAppRunning(string appPath)
        {
            return GetProcessesByAppPath(appPath).Any();
        }
        public static void KillRunningApps(string appPath)
        {
            foreach (var p in GetProcessesByAppPath(appPath))
            {
                p.Kill();
            }
        }
        private static IEnumerable<Process> GetProcessesByAppPath(string appPath, string requiredProcessName = null)
        {
            var exeFilePath = Path.GetFullPath(appPath); // 配置的启动程序路径
            var exeFileName = Path.GetFileName(appPath); // 配置的启动程序文件名
            var idx = exeFileName.LastIndexOf('.');
            var exeName = idx < 0 ? exeFileName.Substring(0) : exeFileName.Substring(0, idx); // 进程名字

            return Process.GetProcessesByName(exeName)
                .Where(p => 
                    (requiredProcessName == null || requiredProcessName.Equals(p.ProcessName, StringComparison.OrdinalIgnoreCase))
                && p.MainModule.FileName.Equals(exeFilePath, StringComparison.OrdinalIgnoreCase));
        }
        
        public static Config GetSystemConfig(string host, string systemId)
        {
            var url = $"{host}/api/GetSystemConfig/{systemId}";
            var config = Util.GetHttpResponseString(url);

            return new JavaScriptSerializer().Deserialize<Config>(config);
        }
    }
    
    class LocalRunInfo
    {
        public string Ver { get; set; }
        public string ClientId { get; set; }
        public string UpdateUrl { get; set; }
        public string AppRunCmd { get; set; }
        public DicIgnoreCase<string> RunFiles { get; set; }
    }
    class RemoteRunInfo
    {
        public string Ver { get; set; }
        public DicIgnoreCase<string> RunFiles { get; set; }
    }
    class UpdateRunFile
    {
        public UpdateRunFileStatus Status;
        public string Path;
        public string NewTag;
        public string OldTag;
    }
    enum UpdateRunFileStatus
    {
        Update = 1,
        Delete = 2,
        SkipUpdate = 3,
        SkipDelete = 4,
        NotModified = 5,
    }
    class UpdateUrlInfo
    {
        // host/system/systemId
        private static Regex r = new Regex(@"^(?<host>(http|https)://[a-zA-Z0-9\.]+(:\d+)?)/(?<systemId>\d+)$",
            RegexOptions.IgnoreCase);

        public static UpdateUrlInfo Parse(string url)
        {
            if (r.IsMatch(url))
            {
                var m = r.Matches(url)[0];

                return new UpdateUrlInfo
                {
                    Host = m.Groups["host"].Value,
                    SystemId = m.Groups["systemId"].Value,
                };
            }

            return null;
        }

        public string Host { get; set; }
        public string SystemId { get; set; }
    }
}
