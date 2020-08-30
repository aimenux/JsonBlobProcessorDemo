using System;
using System.Collections.Generic;

namespace App
{
    public static class Extensions
    {
        public static IEnumerable<IEnumerable<TSource>> Batch<TSource>(this IEnumerable<TSource> source, int size)
        {
            var count = 0;
            TSource[] bucket = null;

            foreach (var item in source)
            {
                bucket ??= new TSource[size];

                bucket[count++] = item;

                if (count != size)
                {
                    continue;
                }

                yield return bucket;

                bucket = null;
                count = 0;
            }

            if (bucket == null || count <= 0)
            {
                yield break;
            }

            Array.Resize(ref bucket, count);
            yield return bucket;
        }
    }
}
