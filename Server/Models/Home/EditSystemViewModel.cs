using System.Collections.Generic;

namespace Justin.Updater.Server.Models.Home
{
    public class EditSystemViewModel
    {
        public int EditSystemId { get; set; }
        public IEnumerable<System> Systems { get; set; }
    }
}