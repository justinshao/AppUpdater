using System.Collections.Generic;

namespace Justin.Updater.Server.Models.Home
{
    public class SystemViewModel
    {
        public System System { get; set; }
        public IEnumerable<PkgInfo> Packages { get; set; }
        public int TotalPage { get; set; }
        public int CurrPage { get; set; }
    }
}