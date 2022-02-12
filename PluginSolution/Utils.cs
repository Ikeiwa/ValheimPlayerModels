using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ValheimPlayerModels
{
    public static class Utils
    {
        public static void Resize<T>(this List<T> list, int sz, T c)
        {
            int count = list.Count;
            if (sz < count)
            {
                list.RemoveRange(sz, count - sz);
                return;
            }
            if (sz > count)
            {
                if (sz > list.Capacity)
                {
                    list.Capacity = sz;
                }
                list.AddRange(Enumerable.Repeat<T>(c, sz - count));
            }
        }

        public static void Resize<T>(this List<T> list, int sz) where T : new()
        {
            list.Resize(sz, Activator.CreateInstance<T>());
        }
	}
}
