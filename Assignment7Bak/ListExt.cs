using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Assignment
{
    public static class ListExt
    {
        public static void Resize<T>(this List<T> list, int size)
        {
            if (size < 0) throw new ArgumentOutOfRangeException("size");
            if (list.Count == size) return;

            if (list.Count > size)
            {
                list.RemoveRange(size - 1, size - list.Count);
            }

            if (list.Count < size)
            {
                var r = new T[size - list.Count];
                list.AddRange(r);
            }
        }

        public static void Fill<T>(this List<T> list, T value)
        {
            for (int i = 0; i < list.Count; i++)
            {
                list[i] = value;
            }
        }

        public static void Fill<T>(this List<T> list, Func<T> funcCreateValue)
        {
            for (int i = 0; i < list.Count; i++)
            {
                list[i] = funcCreateValue();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Empty<T>(this List<T> list)
        {
            return !list.Any();
        }
    }
}
