namespace Justin.Updater.Client
{
    static class Constants
    {
        /// <summary>
        /// 更新后运行的bat文件名
        /// </summary>
        public static readonly string BatFile = "$init.bat";
        /// <summary>
        /// 存放更新配置及文件版本的文件名
        /// </summary>
        public static readonly string RunInfoJsonFile = "$run.json";
        /// <summary>
        /// 日志文件名
        /// </summary>
        public static readonly string UpdateLogFile = "$update.log";
        /// <summary>
        /// 测试用
        /// </summary>
        public static readonly string UpdateErrorFile = "$update.err";
    }
}
