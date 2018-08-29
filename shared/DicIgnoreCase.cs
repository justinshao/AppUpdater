using System;
using System.Collections.Generic;

namespace Justin.Updater.Shared
{
    class DicIgnoreCase<T> : Dictionary<string, T>
    {
        public DicIgnoreCase() :
            base(StringComparer.Instance)
        { }
    }
    class StringComparer : IEqualityComparer<string>
    {
        internal static readonly StringComparer Instance = new StringComparer();

        public bool Equals(string x, string y)
        {
            if (ReferenceEquals(x, y))
                return true;

            if (x == null || y == null)
                return false;

            if (x.Length != y.Length)
                return false;

            return x.Equals(y, StringComparison.OrdinalIgnoreCase);
        }

        public int GetHashCode(string obj)
        {
            return obj.ToUpper().GetHashCode();
        }
    }
}
