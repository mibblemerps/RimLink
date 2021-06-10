using System.Collections.Generic;

namespace RimLink.Util
{
    public static class ListUtil
    {
        /// <summary>
        /// Remove all objects given in the toRemove list.
        /// </summary>
        /// <param name="list">List to remove elements from</param>
        /// <param name="toRemove">List of objects to remove</param>
        /// <returns>Number of elements removed</returns>
        public static int RemoveAll<T>(this List<T> list, IEnumerable<T> toRemove)
        {
            int i = 0;
            foreach (T obj in toRemove)
            {
                if (list.Remove(obj))
                    i++;
            }

            return i;
        }
    }
}
