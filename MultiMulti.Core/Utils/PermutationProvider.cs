using System.Collections.Generic;
using System.Linq;

namespace MultiMulti.Core.Utils
{
    public class PermutationProvider
    {
        public IEnumerable<IEnumerable<T>> GetPermutations<T>(IEnumerable<T> items, int count)
        {
            var index = 0;
            foreach (var item in items)
            {
                if (count == 1)
                    yield return new T[] { item };
                else
                {
                    foreach (var result in GetPermutations(items.Skip(index + 1), count - 1))
                        yield return new T[] { item }.Concat(result);
                }

                ++index;
            }
        }
    }
}
