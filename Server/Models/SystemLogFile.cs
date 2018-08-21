using System;

namespace Justin.Updater.Server.Models
{
    public class SystemLogFile
    {
        public string Name { get; set; }
        public long Size { get; set; }
        public DateTime CreationTime { get; set; }
        public DateTime LastWriteTime { get; set; }
    }
}