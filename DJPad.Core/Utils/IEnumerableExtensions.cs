namespace DJPad.Core.Utils
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public static class EnumerableExtensions
    {
        static public void ForEach<T>(this IEnumerable<T> collection, Action<T> action)
        {
            if (collection.Any())
            {
                foreach (var item in collection)
                {
                    action(item);
                }
            }
        }
    }
}
