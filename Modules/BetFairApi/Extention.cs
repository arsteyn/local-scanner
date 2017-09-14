using System.Collections.Generic;
using System.Linq;

namespace BetFairApi
{
    using System;

    public static class Extention
    {
        public static void IfNotNull<T>(this T obj, Action<T> action)
        {
            if (obj.Null() || action.Null())
            {
                return;
            }

            action(obj);
        }

        public static void IfNull<T>(this T obj, Action<T> action)
        {
            if (obj.Null() && action.NotNull())
            {
                action(obj);
            }
        }

        public static bool NotNull(this object obj)
        {
            return obj != null;
        }

        public static bool Null(this object obj)
        {
            return obj == null;
        }

        public static List<List<T>> Split<T>(this IEnumerable<T> source, int splitSize)
        {
            return source
                .Select((x, i) => new { Index = i, Value = x })
                .GroupBy(x => x.Index / splitSize)
                .Select(x => x.Select(v => v.Value).ToList())
                .ToList();
        }
    }
}
