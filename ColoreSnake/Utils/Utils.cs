using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ColoreSnake.Utils
{
    static class Utils
    {
        public static T PopRandomItem<T>(this List<T> self)
        {
            Random rnd = new Random();
            return PopItem(self, rnd.Next(self.Count));
        }

        public static T PopItem<T>(this List<T> self, int index)
        {
            T item = self[index];
            self.Remove(item);
            return item;
        }
    }
}
