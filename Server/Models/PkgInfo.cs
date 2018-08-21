using System;

namespace Justin.Updater.Server.Models
{
    public class PkgInfo
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public DateTime UploadOrInstallTime { get; set; }
        public bool Installed { get; set; }
        public long Size { get; set; }
    }
}