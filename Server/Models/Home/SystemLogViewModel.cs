using System;
using System.Collections.Generic;

namespace Justin.Updater.Server.Models.Home
{
    public class SystemLogViewModel
    {
        public System System { get; set; }
        public DateTime Date { get; set; }
        public IEnumerable<SystemLogFile> LogFiles { get; set; }
        public int TotalPage { get; set; }
        public int CurrPage { get; set; }
    }
}