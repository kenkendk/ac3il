using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AccCIL
{
    static public class LinqExtensions
    {
        static public IEnumerable<T> Descendants<T>(this IEnumerable<T> source, Func<T, IEnumerable<T>> DescendBy)
        {
            foreach (T value in source)
            {
                yield return value;

                foreach (T child in DescendBy(value).Descendants<T>(DescendBy))
                {
                    yield return child;
                }
            }
        }
    }
}
