using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Justin.Updater.Client
{
    static class LogHelper
    {
        private static object _lock = new object();

        private static string GetLogFile()
        {
            return Path.Combine(Environment.CurrentDirectory, Constants.UpdateLogFile);
        }

        public static void LogError(string msg, Exception ex)
        {
            lock (_lock)
            {
                try
                {
                    var logFile = GetLogFile();

                    using (StreamWriter writer = new StreamWriter(logFile, true))
                    {
                        writer.WriteLine(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                        writer.WriteLine("[ERROR] " + msg);
                        writer.WriteLine(ex.Message);
                        writer.WriteLine(ex.StackTrace);
                        writer.WriteLine();
                    }
                }
                catch (IOException)
                {
                    // ...
                } 
            }
        }

        public static void WriteUpdateLog(string ver, IEnumerable<UpdateRunFile> updatedFiles, List<string> failedUpdateFiles, List<string> failedDeleteFiles)
        {
            lock (_lock)
            {
                var logFile = GetLogFile();

                using (StreamWriter writer = new StreamWriter(logFile, true))
                {
                    writer.WriteLine(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                    writer.WriteLine(string.Format("[INFO] 版本 {0} 更新日志：", ver));

                    int c1 = 0;
                    foreach (var f in updatedFiles.OrderBy(f => f.Status))
                    {
                        if (f.Status == UpdateRunFileStatus.Update)
                        {
                            writer.WriteLine(string.Format("   {2}.更新文件 {0}，Tag={1} -> Tag={3}", f.Path, f.OldTag ?? "null", (++c1).ToString(), f.NewTag));
                        }
                        else if (f.Status == UpdateRunFileStatus.Delete)
                        {
                            writer.WriteLine(string.Format("   {2}.删除文件 {0}，Tag={1}", f.Path, f.OldTag, (++c1).ToString()));
                        }
                        else if (f.Status == UpdateRunFileStatus.SkipUpdate)
                        {
                            writer.WriteLine(string.Format("   {2}.跳过更新文件 {0}，Tag={1}", f.Path, f.OldTag, (++c1).ToString()));
                        }
                        else if (f.Status == UpdateRunFileStatus.SkipDelete)
                        {
                            writer.WriteLine(string.Format("   {2}.跳过删除文件 {0}，Tag={1}", f.Path, f.OldTag, (++c1).ToString()));
                        }
                    }

                    if (failedUpdateFiles.Count > 0)
                    {
                        writer.WriteLine();

                        int c2 = 0;
                        foreach (var f in failedUpdateFiles)
                        {
                            writer.WriteLine(string.Format("   {0}.更新失败： {1}", (++c2).ToString(), f));
                        }
                    }

                    if (failedDeleteFiles.Count > 0)
                    {
                        writer.WriteLine();

                        int c3 = 0;
                        foreach (var f in failedDeleteFiles)
                        {
                            writer.WriteLine(string.Format("   {0}.删除失败： {1}", (++c3).ToString(), f));
                        }
                    }

                    writer.WriteLine();
                } 
            }
        }

        public static void LogInfoToServer(string info, LocalRunInfo localRunInfo)
        {
            var updateUrlInfo = UpdateUrlInfo.Parse(localRunInfo.UpdateUrl);

            string url = $"{updateUrlInfo.Host}/api/LogInfo/{updateUrlInfo.SystemId}?clientId={Util.GetClientId(localRunInfo.ClientId)}&info={info}";

            try
            {
                using (var resp = Util.CreateHttpRequest(url).GetResponse())
                {
                }
            }
            catch
            {
            }
        }

        public static void LogErrorToServer(string msg, Exception ex, LocalRunInfo localRunInfo)
        {
            var updateUrlInfo = UpdateUrlInfo.Parse(localRunInfo.UpdateUrl);

            var error = $"{msg}：{ex.Message}\r\n{ex.StackTrace}";

            string url = $"{updateUrlInfo.Host}/api/LogError/{updateUrlInfo.SystemId}?clientId={Util.GetClientId(localRunInfo.ClientId)}&error={error}";

            try
            {
                using (var resp = Util.CreateHttpRequest(url).GetResponse())
                {
                }
            }
            catch
            {
            }
        }
    }
}