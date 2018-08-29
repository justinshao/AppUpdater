using System;

namespace Justin.Updater.Shared
{
    public class Config
    {
        /// <summary>
        /// 更新检测频率（秒）
        /// </summary>
        public int DetectInterval { get; set; }

        /// <summary>
        /// 更新提示频率（分钟）
        /// </summary>
        public int PromptInterval { get; set; }

        /// <summary>
        /// 更新程序心跳频率（秒）
        /// </summary>
        public int PingInterval { get; set; }

        /// <summary>
        /// 启用更新检测
        /// </summary>
        public bool DetectEnabled { get; set; }

        /// <summary>
        /// 是否强制自动更新
        /// </summary>
        public bool ForceUpdate { get; set; }

        /// <summary>
        /// 是否保持更新程序程序长时间运行（即使主程序关闭）
        /// </summary>
        public bool KeepUpdaterRunning { get; set; }

        /// <summary>
        /// 主程序长时间运行
        /// </summary>
        public bool KeepAppRunning { get; set; }
    }
}
