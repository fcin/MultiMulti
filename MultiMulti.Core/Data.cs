using System;
using System.Collections.Generic;

namespace MultiMulti.Core
{
    public class Data
    {
        public Guid Id { get; set; }
        public DateTime Added { get; set; }
        public IEnumerable<IEnumerable<int>> Values { get; set; }
        public IEnumerable<int> SelectedNumbers { get; set; }
    }
}
