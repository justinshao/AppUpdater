using Justin.Updater.Shared;
using System;
using System.IO;

namespace Justin.Updater.Server
{
    static class LogHelper
    {
        private static object _lock = new object();

        private static string GetLogDir()
        {
            return IOHelper.EnsureCreateDir(Path.Combine(AppSettingHelper.UpdateDir, "Log")).Dir;
        }

        private static string GetLogFile()
        {
            DateTime now = DateTime.Now;
            string logDir = IOHelper.EnsureCreateDir(Path.Combine(GetLogDir(), "Server")).Dir;
            string logFile = Path.Combine(logDir, now.ToString("yyyy-MM-dd") + ".log");

            return logFile;
        }

        private static string GetClientLogFile(int systemId, string clientId)
        {
            DateTime now = DateTime.Now;
            string logDir = IOHelper.EnsureCreateDir(GetSystemClientLogDir(systemId, now)).Dir;
            string logFile = Path.Combine(logDir, clientId + ".log");

            return logFile;
        }

        public static string GetSystemClientLogDir(int systemId, DateTime date)
        {
            return Path.Combine(GetLogDir(), $"Client{systemId}", date.ToString("yyyy-MM-dd"));
        }

        private static void LogErrorWithLock(string logFile, string msg, Exception ex)
        {
            lock (_lock)
            {
                LogError(logFile, msg, ex);
            }
        }
        private static void LogError(string logFile, string msg, Exception ex = null)
        {
            using (StreamWriter writer = new StreamWriter(logFile, true))
            {
                writer.WriteLine(DateTime.Now.ToString("HH:mm:ss fff"));
                writer.WriteLine("[ERROR] " + msg);
                if(ex != null)
                {
                    writer.WriteLine(ex.Message);
                    writer.WriteLine(ex.StackTrace);
                }
                writer.WriteLine();
            }
        }
        private static void LogInfoWithLock(string logFile, string msg)
        {
            lock (_lock)
            {
                LogInfo(logFile, msg);
            }
        }
        private static void LogInfo(string logFile, string msg)
        {
            using (StreamWriter writer = new StreamWriter(logFile, true))
            {
                writer.WriteLine(DateTime.Now.ToString("HH:mm:ss fff"));
                writer.WriteLine("[INFO] " + msg);
                writer.WriteLine();
            }
        }

        public static void LogError(string msg, Exception ex)
        {
            LogErrorWithLock(GetLogFile(), msg, ex);
        }
        public static void LogInfo(string msg)
        {
            LogInfoWithLock(GetLogFile(), msg);
        }
        public static void LogClientInfo(int systemId, string clientId, string msg)
        {
            LogInfo(GetClientLogFile(systemId, clientId), msg);
        }
        public static void LogClientError(int systemId, string clientId, string msg)
        {
            LogError(GetClientLogFile(systemId, clientId), msg);
        }
    }
}