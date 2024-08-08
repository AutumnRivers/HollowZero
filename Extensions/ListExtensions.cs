using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HollowZero
{
    public static class ListExtensions
    {
        public static T GetRandom<T>(this List<T> e)
        {
            Random random = new Random();
            int index = random.Next(e.Count());

            return e[index];
        }

        public static bool TryFind<T>(this List<T> e, Func<T, bool> predicate, out T item)
        {
            if(e.Any(predicate))
            {
                item = e.FirstOrDefault(predicate);
                return true;
            } else
            {
                item = default;
                return false;
            }
        }

        public static T GetRandom<T>(this IEnumerable<T> e)
        {
            if (!e.Any()) return default;
            Random random = new Random();
            int index = random.Next(e.Count());
            return e.ElementAtOrDefault(index);
        }
    }
}
