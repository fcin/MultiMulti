using LiteDB;
using System;

namespace MultiMulti.Core
{
    public class Data
    {
        [BsonId]
        public int Id { get; set; }
        public DateTime Added { get; set; }
        public int[] Values { get; set; }
        public string[] Pairs { get; set; }
        public bool IsCustom { get; set; }
    }
}
