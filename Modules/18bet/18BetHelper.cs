using System;
using System.Collections.Generic;
using System.Linq;

namespace Bet18
{
    public static class Bet18Helper
    {
        public static IList<T> Clone<T>(this IList<T> listToClone) where T : ICloneable
        {
            return listToClone.Select(item => (T)item.Clone()).ToList();
        }
    }
}
