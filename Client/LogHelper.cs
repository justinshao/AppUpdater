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
                        writer.WriteLine($"[ERROR] {msg}");
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
                    writer.WriteLine($"[INFO] 版本 {ver} 更新日志：");

                    var indent = "   ";

                    int c1 = 0;
                    foreach (var f in updatedFiles.OrderBy(f => f.Status))
                    {
                        if (f.Status == UpdateRunFileStatus.Update)
                        {
                            writer.WriteLine($"{indent}{++c1}.更新文件 {f.Path}，Tag={f.OldTag ?? "null"} -> Tag={f.NewTag}");
                        }
                        else if (f.Status == UpdateRunFileStatus.Delete)
                        {
                            writer.WriteLine($"{indent}{++c1}.删除文件 {f.Path}，Tag={f.OldTag}");
                        }
                        else if (f.Status == UpdateRunFileStatus.SkipUpdate)
                        {
                            writer.WriteLine($"{indent}{++c1}.跳过更新文件 {f.Path}，Tag={f.OldTag}");
                        }
                        else if (f.Status == UpdateRunFileStatus.SkipDelete)
                        {
                            writer.WriteLine($"{indent}{++c1}.跳过删除文件 {f.Path}，Tag={f.OldTag}");
                        }
                    }

                    if (failedUpdateFiles.Count > 0)
                    {
                        writer.WriteLine();

                        int c2 = 0;
                        foreach (var f in failedUpdateFiles)
                        {
                            writer.WriteLine($"{indent}{++c2}.更新失败：{f}");
                        }
                    }

                    if (failedDeleteFiles.Count > 0)
                    {
                        writer.WriteLine();

                        int c3 = 0;
                        foreach (var f in failedDeleteFiles)
                        {
                            writer.WriteLine($"{indent}{++c3}.删除失败：{f}");
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

            Util.SendHttpRequest(url);
        }

        public static void LogErrorToServer(string msg, Exception ex, LocalRunInfo localRunInfo)
        {
            var updateUrlInfo = UpdateUrlInfo.Parse(localRunInfo.UpdateUrl);

            var error = $"{msg}：{ex.Message}{Environment.NewLine}{ex.StackTrace}";

            string url = $"{updateUrlInfo.Host}/api/LogError/{updateUrlInfo.SystemId}?clientId={Util.GetClientId(localRunInfo.ClientId)}&error={error}";

            Util.SendHttpRequest(url);
        }
    }
}