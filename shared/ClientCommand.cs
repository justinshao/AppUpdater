using System;

namespace Justin.Updater.Shared
{
    public class ClientCommand
    {
        public ClientCommandType Type { get; set; }
        public string Args { get; set; }
    }

    public enum ClientCommandType
    {
        Start,
        Stop,
    }
}
