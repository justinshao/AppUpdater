using System;
using System.Collections.Generic;

namespace Justin.Updater.Shared
{
    class DicIgnoreCase<T> : Dictionary<string, T>
    {
        public DicIgnoreCase() :
            base(StringComparer.OrdinalIgnoreCase)
        { }
    }
}
